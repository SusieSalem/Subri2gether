using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Znt2P
{
    internal class FusionHit
    {
        public MutationType Type;
        public int Player; // 1 = mouse player, 2 = controller player, 3 = an Overlord (blocker) rallying others
        public float Time;
    }

    internal class FusionState
    {
        public Mutation Mutation;
        public Character Character;
        public Moveable Mover;
        public readonly List<FusionHit> Hits = new List<FusionHit>();
        public bool Fused;

        // Computed layers.
        public float SpeedMult = 1f;
        public float ToughMult = 1f;
        public float DamageMult = 1f;
        public float Regen;      // fraction of max HP restored per second
        public float Dodge;      // chance to ignore a hit entirely
        public float Thorns;     // fraction of damage taken that is reflected at the attacker
        public float Leech;      // fraction of damage dealt that heals the attacker
        public bool AlwaysRun;
        public string Name = "";
        public string BaseName = "";   // name without the tier prefix
        public string Signature = "";  // unique behaviour id, empty = stats only
        public bool Injected;          // created by an injection (no transformation/effect needed to persist)
        public Color Tint = Color.white;

        // Signature-ability bookkeeping.
        public float NextPulse;
        public float AirTime;

        // Original look, so it can be restored when the fusion ends.
        public bool TintApplied;
        public Color OriginalDefault;
        public SpriteHighlight Highlight;
        public tk2dBaseSprite[] Sprites;
        public Color[] OriginalSpriteColors;
    }

    // Fusion engine: when both players mutate the same zombie within a short window, the zombie "fuses" and keeps
    // a stack of every mutation applied to it. The game only supports one transformation + one effect per zombie,
    // so the stack is tracked here and applied as layered modifiers on top of whatever the game says the zombie is.
    internal static class Fusion
    {
        // --- tunables (bound to the config by the plugin) ---
        public static bool Enabled = true;
        public static float Window = 6f;
        public static int MaxStack = 10;
        public static float SpeedPerRun = 0.30f;
        public static float ToughPerTank = 0.60f;
        public static float DamagePerTank = 0.50f;
        public static float MaxSpeedMult = 3f;
        public static bool AlwaysRunOnSuperRunner = true;
        public static bool ShowTags = true;
        public static bool ShowStatsLine = true;
        public static Func<bool> IsActive = () => true;

        // --- context set by the patches / P2 controller ---
        public static int CurrentPlayer = 1;               // who is currently asking "can I mutate this?"
        public static CharacterBehaviour CurrentBlocker;   // set while an Overlord applies its target mutation
        public static Mutation ContextMutation;            // zombie being evaluated by Mutation.CanMutateTo
        public static bool BypassSpeedMult;                // true while the game snapshots "default" speeds
        private static bool inReflect;                     // guards thorns damage from recursing

        public static readonly Dictionary<Mutation, FusionState> States = new Dictionary<Mutation, FusionState>();
        private static readonly Dictionary<Moveable, FusionState> ByMover = new Dictionary<Moveable, FusionState>();
        private static readonly List<Mutation> dead = new List<Mutation>();

        private static FieldInfo mutationCharacterField;
        private static FieldInfo selectionHighlightField;
        private static FieldInfo highlightSpritesField;
        private static FieldInfo runTimerField;
        private static float nextCleanup;
        private static float lastTick;

        public static bool Active { get { return Enabled && IsActive() && States.Count > 0; } }

        // --- discovery log (persisted) ---
        public static readonly HashSet<string> Discovered = new HashSet<string>();
        public static string DiscoveredPath;
        public static Action<string> OnDiscovered;

        public static IEnumerable<KeyValuePair<string, string>> AllRecipes { get { return Recipes; } }

        public static void LoadDiscovered()
        {
            try
            {
                if (DiscoveredPath != null && System.IO.File.Exists(DiscoveredPath))
                    foreach (string line in System.IO.File.ReadAllLines(DiscoveredPath))
                        if (line.Trim().Length > 0) Discovered.Add(line.Trim());
            }
            catch (Exception) { }
        }

        private static void SaveDiscovered()
        {
            try
            {
                if (DiscoveredPath != null) System.IO.File.WriteAllLines(DiscoveredPath, new List<string>(Discovered).ToArray());
            }
            catch (Exception) { }
        }

        private static MutationType TypeOfLetter(char c)
        {
            switch (c)
            {
                case 'R': return MutationType.Run;
                case 'T': return MutationType.Tank;
                case 'C': return MutationType.Crawler;
                case 'S': return MutationType.Scream;
                case 'P': return MutationType.Spit;
                default: return MutationType.Jump;
            }
        }

        // Injection: turns this zombie straight into a random fusion. Returns the fusion's name (or null).
        public static string Inject(Mutation m, int player)
        {
            if (!Enabled || m == null) return null;
            Character character = CharacterOf(m);
            if (character == null || !(character.Behaviour is ZombieBehaviour)) return null;

            List<string> keys = new List<string>(Recipes.Keys);
            string key = keys[UnityEngine.Random.Range(0, keys.Count)];

            FusionState s;
            if (!States.TryGetValue(m, out s))
            {
                s = new FusionState { Mutation = m, Character = character, Mover = character.Components.Mover };
                States[m] = s;
                if (s.Mover != null) ByMover[s.Mover] = s;
            }
            s.Hits.Clear();
            float now = Time.unscaledTime;
            for (int i = 0; i < key.Length; i++)
                s.Hits.Add(new FusionHit { Type = TypeOfLetter(key[i]), Player = player, Time = now });
            if (key.Length == 1) // single-ingredient recipes need a double to count
                s.Hits.Add(new FusionHit { Type = TypeOfLetter(key[0]), Player = player, Time = now });
            s.Fused = true;
            s.Injected = true;
            Recompute(s);
            Apply(s);
            return s.Name;
        }

        // ---------------------------------------------------------------- recipe table
        // Key = ingredient letters in fixed order R(un) T(ank) C(rawler) S(cream) P(spit) J(ump).
        private static readonly Dictionary<string, string> Recipes = new Dictionary<string, string>
        {
            { "R", "Super Runner" }, { "T", "Juggernaut" }, { "C", "Spider Lord" },
            { "S", "Death Wail" }, { "P", "Acid Fountain" }, { "J", "Pogo Fiend" },

            { "RT", "Berserker" }, { "RC", "Spider Runner" }, { "RS", "Banshee" },
            { "RP", "Toxic Sprinter" }, { "RJ", "Leaper" }, { "TC", "Brute Crawler" },
            { "TS", "Siren Brute" }, { "TP", "Acid Brute" }, { "TJ", "Wrecking Ball" },
            { "CS", "Night Shrieker" }, { "CP", "Venom Crawler" }, { "CJ", "Wall Stalker" },
            { "SP", "Plague Choir" }, { "SJ", "Screaming Leaper" }, { "PJ", "Spore Leaper" },

            { "RTC", "Apex Predator" }, { "RTS", "Warcry Berserker" }, { "RTP", "Toxic Berserker" },
            { "RTJ", "Rampage" }, { "RCS", "Phantom" }, { "RCP", "Plague Spider" },
            { "RCJ", "Hunter" }, { "RSP", "Plague Banshee" }, { "RSJ", "Harpy" },
            { "RPJ", "Toxic Pouncer" }, { "TCS", "Dread Crawler" }, { "TCP", "Corrosive Brute" },
            { "TCJ", "Siege Crawler" }, { "TSP", "Plague Colossus" }, { "TSJ", "Quake Brute" },
            { "TPJ", "Acid Meteor" }, { "CSP", "Hive Mother" }, { "CSJ", "Shadow Leaper" },
            { "CPJ", "Venom Stalker" }, { "SPJ", "Miasma Harpy" },

            { "RTCS", "Nightmare" }, { "RTCP", "Pestilence" }, { "RTCJ", "Executioner" },
            { "RTSP", "Plague Titan" }, { "RTSJ", "Wrath" }, { "RTPJ", "Toxic Rampage" },
            { "RCSP", "Plague Wraith" }, { "RCSJ", "Reaper" }, { "RCPJ", "Venom Hunter" },
            { "RSPJ", "Storm Harpy" }, { "TCSP", "Hive Colossus" }, { "TCSJ", "Siege Shrieker" },
            { "TCPJ", "Acid Siege" }, { "TSPJ", "Cataclysm" }, { "CSPJ", "Broodmother" },

            { "RTCSP", "Death Bringer" }, { "RTCSJ", "Harbinger" }, { "RTCPJ", "Blight Lord" },
            { "RTSPJ", "Calamity" }, { "RCSPJ", "Plague Reaper" }, { "TCSPJ", "Doom Colossus" },

            { "RTCSPJ", "Patient Zero" },
        };

        // ---------------------------------------------------------------- recording

        public static void OnMutated(Mutation m, CharacterMutation applied)
        {
            if (!Enabled || !IsActive() || m == null || applied == null) return;

            Character character = CharacterOf(m);
            if (character == null) return;

            // Only zombies fuse; humans (contamination targets) and everything else stay vanilla.
            if (!(character.Behaviour is ZombieBehaviour)) return;

            int player = CurrentBlocker != null ? 3 : (P2Controller.InP2Apply ? 2 : 1);
            float now = Time.unscaledTime;

            FusionState s;
            if (!States.TryGetValue(m, out s))
            {
                s = new FusionState { Mutation = m, Character = character, Mover = character.Components.Mover };
                States[m] = s;
                if (s.Mover != null) ByMover[s.Mover] = s;
            }

            FusionHit last = s.Hits.Count > 0 ? s.Hits[s.Hits.Count - 1] : null;
            bool crossPlayer = last != null && last.Player != player && now - last.Time <= Window;

            if (s.Fused || crossPlayer)
            {
                s.Hits.Add(new FusionHit { Type = applied.Type, Player = player, Time = now });
                s.Fused = true;
            }
            else
            {
                // A lone hit replaces the zombie as in vanilla, so no fusion.
                s.Hits.Clear();
                s.Hits.Add(new FusionHit { Type = applied.Type, Player = player, Time = now });
            }

            // Overlord rally: a fused Overlord that hands out Run makes the zombies it commands Super Runners.
            if (CurrentBlocker != null && applied.Type == MutationType.Run)
            {
                FusionState blocker;
                if (States.TryGetValue(CurrentBlocker.Mutation, out blocker) && blocker.Fused && Count(blocker, MutationType.Run) >= 1)
                {
                    int level = Mathf.Max(2, Math.Min(MaxStack, blocker.Hits.Count));
                    s.Hits.Clear();
                    for (int i = 0; i < level; i++)
                        s.Hits.Add(new FusionHit { Type = MutationType.Run, Player = 3, Time = now });
                    s.Fused = true;
                }
            }

            while (s.Hits.Count > MaxStack) s.Hits.RemoveAt(0);

            Recompute(s);
            Apply(s);
        }

        // Lets the same type be applied again (Run+Run, Tank+Tank, ...) which vanilla forbids, but only when it would
        // complete or grow a fusion: the other player hit within the window, or the zombie is already fused.
        public static bool AllowsRefuse(Mutation m, MutationType newType)
        {
            if (!Enabled || !IsActive() || m == null) return false;
            if (newType != MutationType.Run && newType != MutationType.Tank && newType != MutationType.Crawler && newType != MutationType.Scream)
                return false;
            if (m.TransformationType != newType && m.EffectType != newType) return false;

            FusionState s;
            if (!States.TryGetValue(m, out s) || s.Hits.Count == 0) return false;
            if (s.Fused) return true;

            FusionHit last = s.Hits[s.Hits.Count - 1];
            return last.Player != CurrentPlayer && Time.unscaledTime - last.Time <= Window;
        }

        // ---------------------------------------------------------------- stat layers

        private static int Count(FusionState s, MutationType t)
        {
            int n = 0;
            for (int i = 0; i < s.Hits.Count; i++) if (s.Hits[i].Type == t) n++;
            return n;
        }

        // Diminishing returns: each extra hit of the same kind is worth 80% of the previous one.
        private static float D(int n)
        {
            float sum = 0f, f = 1f;
            for (int i = 0; i < n; i++) { sum += f; f *= 0.8f; }
            return sum;
        }

        private static void Recompute(FusionState s)
        {
            if (!s.Fused)
            {
                s.SpeedMult = s.ToughMult = s.DamageMult = 1f;
                s.Regen = s.Dodge = s.Thorns = s.Leech = 0f;
                s.AlwaysRun = false;
                s.Name = "";
                return;
            }

            int run = Count(s, MutationType.Run), tank = Count(s, MutationType.Tank), crawl = Count(s, MutationType.Crawler);
            int scream = Count(s, MutationType.Scream), jump = Count(s, MutationType.Jump), spit = Count(s, MutationType.Spit);
            int over = Count(s, MutationType.Overlord);
            int other = s.Hits.Count - (run + tank + crawl + scream + jump + spit + over);

            // Every distinct ingredient beyond the first makes all layers a bit stronger (synergy).
            int kinds = (run > 0 ? 1 : 0) + (tank > 0 ? 1 : 0) + (crawl > 0 ? 1 : 0) + (scream > 0 ? 1 : 0)
                        + (spit > 0 ? 1 : 0) + (jump > 0 ? 1 : 0) + (over > 0 ? 1 : 0);
            float syn = 1f + 0.08f * Mathf.Max(0, kinds - 1);

            float spd = SpeedPerRun * D(run) + 0.08f * D(crawl) + 0.06f * D(jump);
            float tough = ToughPerTank * D(tank) + 0.15f * D(crawl) + 0.10f * D(over) + 0.08f * D(run)
                          + 0.05f * (D(scream) + D(spit) + D(jump)) + 0.05f * other;
            float dmg = DamagePerTank * D(tank) + 0.20f * D(run) + 0.15f * D(crawl) + 0.10f * D(spit);

            s.SpeedMult = Mathf.Min(MaxSpeedMult, 1f + spd * syn);
            s.ToughMult = 1f + tough * syn;
            s.DamageMult = 1f + dmg * syn;
            s.Regen = Mathf.Min(0.08f, (0.010f * D(tank) + 0.006f * D(over) + 0.004f * D(crawl)) * syn);
            s.Dodge = Mathf.Min(0.5f, (0.03f * D(run) + 0.05f * D(scream) + 0.04f * D(jump) + 0.02f * D(crawl)) * syn);
            s.Thorns = Mathf.Min(1f, (0.05f * D(scream) + 0.10f * D(spit) + 0.05f * D(tank)) * syn);
            s.Leech = Mathf.Min(0.5f, (0.03f * D(spit) + 0.02f * D(run) + 0.03f * D(crawl)) * syn);
            s.AlwaysRun = AlwaysRunOnSuperRunner && run >= 2;

            s.Name = NameFor(s, run, tank, crawl, scream, spit, jump, over);

            // Signature abilities and the small stat tweaks that go with them.
            switch (s.BaseName)
            {
                case "Berserker": case "Warcry Berserker": case "Toxic Berserker": s.Signature = "rage"; break;
                case "Juggernaut": s.Signature = "stone"; break;
                case "Acid Brute": s.Signature = "corrosive"; s.Thorns = Mathf.Min(1f, s.Thorns * 2f); break;
                case "Toxic Sprinter": s.Signature = "trail"; break;
                case "Wrecking Ball": s.Signature = "quake"; break;
                case "Reaper": s.Signature = "execute"; break;
                case "Banshee": case "Plague Banshee": s.Signature = "pulse"; break;
                default: s.Signature = ""; break;
            }

            // Discovery: first time this recipe is ever made.
            if (!Discovered.Contains(s.BaseName))
            {
                Discovered.Add(s.BaseName);
                SaveDiscovered();
                if (OnDiscovered != null) OnDiscovered(s.BaseName);
            }

            // Tint: weighted mix of each ingredient's color, kept light so zombies stay readable.
            Color mix = Color.black;
            float w = 0f;
            Accumulate(ref mix, ref w, D(run), new Color(1f, 0.55f, 0.15f));
            Accumulate(ref mix, ref w, D(tank), new Color(0.65f, 0.35f, 1f));
            Accumulate(ref mix, ref w, D(crawl), new Color(0.2f, 0.9f, 0.8f));
            Accumulate(ref mix, ref w, D(scream), new Color(1f, 0.9f, 0.2f));
            Accumulate(ref mix, ref w, D(spit), new Color(0.5f, 1f, 0.2f));
            Accumulate(ref mix, ref w, D(jump), new Color(0.3f, 0.6f, 1f));
            Accumulate(ref mix, ref w, D(over), new Color(1f, 0.3f, 0.3f));
            Color avg = w > 0f ? mix / w : Color.white;
            s.Tint = Color.Lerp(Color.white, avg, 0.65f);
            s.Tint.a = 1f;
        }

        private static void Accumulate(ref Color mix, ref float w, float weight, Color c)
        {
            if (weight <= 0f) return;
            mix += c * weight;
            w += weight;
        }

        private static string TierPrefix(int hits)
        {
            if (hits >= 10) return "Omega ";
            if (hits >= 8) return "Mythic ";
            if (hits >= 6) return "Alpha ";
            if (hits >= 4) return "Elite ";
            return "";
        }

        private static string NameFor(FusionState s, int run, int tank, int crawl, int scream, int spit, int jump, int over)
        {
            string key = (run > 0 ? "R" : "") + (tank > 0 ? "T" : "") + (crawl > 0 ? "C" : "")
                         + (scream > 0 ? "S" : "") + (spit > 0 ? "P" : "") + (jump > 0 ? "J" : "");

            string baseName;
            if (over > 0)
            {
                if (key.Length == 0) baseName = "War Overlord";
                else if (key == "R") baseName = "Rally Overlord";
                else baseName = "Warlord " + (Recipes.ContainsKey(key) ? Recipes[key] : "Fusion");
            }
            else if (!Recipes.TryGetValue(key, out baseName))
            {
                baseName = "Fusion";
            }
            s.BaseName = baseName;
            return TierPrefix(s.Hits.Count) + baseName;
        }

        // ---------------------------------------------------------------- applying

        private static void Apply(FusionState s)
        {
            if (s.Character == null) return;

            // Speed: GetSpeed is patched to scale by SpeedMult; refresh the live value if the zombie is moving.
            Moveable mover = s.Mover;
            if (mover != null && s.Fused && mover.CurrentSpeed != MoveSpeed.None
                && (mover.State == MoveableState.Moving || mover.State == MoveableState.Climbing))
            {
                mover.SetSpeed(mover.CurrentSpeed, forceSpeed: true);
            }

            // "Always running": the Runner effect normally times out; keep it going.
            if (s.AlwaysRun && s.Mutation.EffectType == MutationType.Run)
                ExtendRun(s.Character.Behaviour as ZombieBehaviour);

            ApplyVisuals(s);
        }

        private static void ExtendRun(ZombieBehaviour zb)
        {
            if (zb == null) return;
            if (runTimerField == null)
                runTimerField = typeof(ZombieBehaviour).GetField("RunTimer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (runTimerField == null) return;
            Timer t = (Timer)runTimerField.GetValue(zb);
            t.Start(600f);
            runTimerField.SetValue(zb, t);
        }

        private static void ApplyVisuals(FusionState s)
        {
            if (s.Highlight == null)
            {
                if (selectionHighlightField == null)
                {
                    selectionHighlightField = typeof(CharacterSelection).GetField("highlight", BindingFlags.Instance | BindingFlags.NonPublic);
                    highlightSpritesField = typeof(SpriteHighlight).GetField("Sprites", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }
                CharacterSelection sel = s.Character.Components.Selection;
                if (sel == null || selectionHighlightField == null || highlightSpritesField == null) return;
                s.Highlight = selectionHighlightField.GetValue(sel) as SpriteHighlight;
                if (s.Highlight == null) return;
                s.Sprites = highlightSpritesField.GetValue(s.Highlight) as tk2dBaseSprite[];
            }

            if (!s.Fused)
            {
                RestoreVisuals(s);
                return;
            }
            if (!s.TintApplied)
            {
                s.OriginalDefault = s.Highlight.DefaultColor;
                if (s.Sprites != null)
                {
                    s.OriginalSpriteColors = new Color[s.Sprites.Length];
                    for (int i = 0; i < s.Sprites.Length; i++)
                        s.OriginalSpriteColors[i] = s.Sprites[i] != null ? s.Sprites[i].color : Color.white;
                }
                s.TintApplied = true;
            }
            // DefaultColor is what the game's highlight returns to after a hover, so the tint survives hovering.
            s.Highlight.DefaultColor = s.Tint;
            if (s.Sprites != null)
                for (int i = 0; i < s.Sprites.Length; i++)
                    if (s.Sprites[i] != null) s.Sprites[i].color = s.Tint;
        }

        private static void RestoreVisuals(FusionState s)
        {
            if (!s.TintApplied) return;
            if (s.Highlight != null) s.Highlight.DefaultColor = s.OriginalDefault;
            if (s.Sprites != null && s.OriginalSpriteColors != null)
                for (int i = 0; i < s.Sprites.Length && i < s.OriginalSpriteColors.Length; i++)
                    if (s.Sprites[i] != null) s.Sprites[i].color = s.OriginalSpriteColors[i];
            s.TintApplied = false;
        }

        // ---------------------------------------------------------------- queries used by the patches

        public static float SpeedMultOf(Moveable m)
        {
            if (BypassSpeedMult || m == null || ByMover.Count == 0) return 1f;
            FusionState s;
            return ByMover.TryGetValue(m, out s) && s.Fused ? s.SpeedMult : 1f;
        }

        private static FusionState StateOf(Component c)
        {
            if (c == null || States.Count == 0) return null;
            Character ch = c.GetComponentInParent<Character>();
            if (ch == null || ch.Components == null || ch.Components.Mutation == null) return null;
            FusionState s;
            return States.TryGetValue(ch.Components.Mutation, out s) ? s : null;
        }

        // Applies the damage layers to a hit. Returns false if the hit should be skipped entirely (dodged).
        //   amount        in: raw damage, out: adjusted damage
        //   attacker      whoever dealt it (may be null)
        public static bool AdjustDamage(Health victim, Transform attacker, DamageType type, ref float amount)
        {
            if (inReflect || !Enabled || !IsActive() || States.Count == 0) return true;
            FusionState a = StateOf(attacker);
            FusionState v = StateOf(victim);

            if (v != null && v.Fused && v.Dodge > 0f && UnityEngine.Random.value < v.Dodge)
                return false; // dodged

            if (a != null && a.Fused)
            {
                amount *= a.DamageMult;
                // Berserker rage: hits 50% harder once it is below half health.
                if (a.Signature == "rage" && a.Character != null && a.Character.Behaviour != null)
                {
                    Health ah = a.Character.Behaviour.Health;
                    if (ah != null && ah.MaxHp > 0f && ah.Hp / ah.MaxHp < 0.5f) amount *= 1.5f;
                }
            }
            if (v != null && v.Fused)
            {
                amount /= Mathf.Max(0.1f, v.ToughMult);
                // Juggernaut stone skin: no single hit can take more than a quarter of its health.
                if (v.Signature == "stone" && victim.MaxHp > 0f) amount = Mathf.Min(amount, victim.MaxHp * 0.25f);
            }

            // Thorns and leech work off the damage that actually lands.
            if (amount > 0f)
            {
                if (v != null && v.Fused && v.Thorns > 0f && attacker != null)
                {
                    Health back = attacker.GetComponentInChildren<Health>();
                    if (back != null && back != victim && !back.IsDead)
                    {
                        inReflect = true;
                        try { back.ReceiveDamage(amount * v.Thorns, victim.transform, type); }
                        finally { inReflect = false; }
                    }
                }
                if (a != null && a.Fused && a.Leech > 0f)
                {
                    Health own = a.Character != null && a.Character.Behaviour != null ? a.Character.Behaviour.Health : null;
                    if (own != null && !own.IsDead && own.Hp < own.MaxHp)
                        own.SetHp(own.MaxHp, Mathf.Min(own.MaxHp, own.Hp + amount * a.Leech));
                }
            }
            return true;
        }

        // ---------------------------------------------------------------- upkeep

        private static Character CharacterOf(Mutation m)
        {
            if (mutationCharacterField == null)
                mutationCharacterField = typeof(Mutation).GetField("character", BindingFlags.Instance | BindingFlags.NonPublic);
            return mutationCharacterField != null ? mutationCharacterField.GetValue(m) as Character : m.GetComponent<Character>();
        }

        public static void Tick(float now)
        {
            if (States.Count == 0) { lastTick = now; return; }

            // Regeneration, every frame (cheap: only fused zombies).
            float dt = Mathf.Clamp(now - lastTick, 0f, 0.25f);
            lastTick = now;
            foreach (KeyValuePair<Mutation, FusionState> kv in States)
            {
                FusionState s = kv.Value;
                if (!s.Fused || s.Regen <= 0f || s.Character == null || s.Character.Behaviour == null) continue;
                Health h = s.Character.Behaviour.Health;
                if (h == null || h.IsDead || h.Hp >= h.MaxHp) continue;
                h.SetHp(h.MaxHp, Mathf.Min(h.MaxHp, h.Hp + h.MaxHp * s.Regen * dt));
            }

            foreach (KeyValuePair<Mutation, FusionState> kv in States)
            {
                FusionState s = kv.Value;
                if (!s.Fused || s.Signature.Length == 0 || s.Character == null || !s.Character.gameObject.activeInHierarchy) continue;
                try { SignatureTick(s, now, dt); }
                catch (Exception) { s.Signature = ""; } // never let a signature break the game
            }

            if (now < nextCleanup) return;
            nextCleanup = now + 0.4f;

            dead.Clear();
            foreach (KeyValuePair<Mutation, FusionState> kv in States)
            {
                Mutation m = kv.Key;
                FusionState st = kv.Value;
                bool idle = m != null && m.Transformation == null && m.Effect == null && !st.Injected;
                bool gone = m == null || !m.gameObject.activeInHierarchy || idle;
                if (gone) dead.Add(m);
            }
            for (int i = 0; i < dead.Count; i++) Remove(dead[i]);
        }

        // ---------------------------------------------------------------- signature abilities

        // Damages (or executes) humans around a fused zombie. Flagged so our own damage layers don't re-scale it.
        private static void HitHumansNear(FusionState s, float radius, float damage, bool executeWounded)
        {
            Transform self = s.Character.transform;
            Collider2D[] hits = Physics2D.OverlapCircleAll(self.position, radius);
            DamageType type = s.Character.Behaviour.Attacker != null ? s.Character.Behaviour.Attacker.DamageType : DamageType.Bite;
            HashSet<Character> done = new HashSet<Character>();

            bool was = inReflect;
            inReflect = true;
            try
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    Character c = hits[i].GetComponentInParent<Character>();
                    if (c == null || c == s.Character || !done.Add(c)) continue;
                    if (!(c.Behaviour is HumanBehaviour)) continue;
                    Health h = c.Behaviour.Health;
                    if (h == null || h.IsDead) continue;

                    if (executeWounded)
                    {
                        if (h.MaxHp > 0f && h.Hp / h.MaxHp < 0.3f) h.Kill(self, type);
                    }
                    else
                    {
                        h.ReceiveDamage(damage, self, type);
                    }
                }
            }
            finally { inReflect = was; }
        }

        private static void SignatureTick(FusionState s, float now, float dt)
        {
            Moveable mv = s.Mover;
            switch (s.Signature)
            {
                case "trail": // Toxic Sprinter: acid left behind while it runs
                    if (now >= s.NextPulse && mv != null && mv.State == MoveableState.Moving)
                    {
                        s.NextPulse = now + 0.5f;
                        HitHumansNear(s, 1.4f, 4f, false);
                    }
                    break;
                case "pulse": // Banshee: sonic pulse
                    if (now >= s.NextPulse)
                    {
                        s.NextPulse = now + 4f;
                        HitHumansNear(s, 4f, 6f, false);
                    }
                    break;
                case "execute": // Reaper: finishes off wounded humans
                    if (now >= s.NextPulse)
                    {
                        s.NextPulse = now + 0.3f;
                        HitHumansNear(s, 1.4f, 0f, true);
                    }
                    break;
                case "quake": // Wrecking Ball: shockwave on landing from a real jump/fall
                    if (mv != null)
                    {
                        if (!mv.IsGrounded) s.AirTime += dt;
                        else
                        {
                            if (s.AirTime > 0.35f) HitHumansNear(s, 3.2f, 25f, false);
                            s.AirTime = 0f;
                        }
                    }
                    break;
            }
        }

        private static void Remove(Mutation m)
        {
            FusionState s;
            if (m != null && States.TryGetValue(m, out s))
            {
                RestoreVisuals(s);
                if (s.Mover != null) ByMover.Remove(s.Mover);
                // Re-sync the live speed now that the multiplier is gone.
                s.Fused = false;
                if (s.Mover != null && s.Mover.CurrentSpeed != MoveSpeed.None && s.Mover.State == MoveableState.Moving)
                    s.Mover.SetSpeed(s.Mover.CurrentSpeed, forceSpeed: true);
            }
            States.Remove(m);
        }

        public static void Clear()
        {
            if (States.Count == 0) return;
            List<Mutation> keys = new List<Mutation>(States.Keys);
            for (int i = 0; i < keys.Count; i++) Remove(keys[i]);
            States.Clear();
            ByMover.Clear();
        }

        // ---------------------------------------------------------------- display

        private static string StatsLine(FusionState s)
        {
            string line = "SPD x" + s.SpeedMult.ToString("0.0") + "  DEF x" + s.ToughMult.ToString("0.0")
                          + "  DMG x" + s.DamageMult.ToString("0.0");
            return line;
        }

        private static string Short(MutationType t)
        {
            switch (t)
            {
                case MutationType.Run: return "Run";
                case MutationType.Tank: return "Tank";
                case MutationType.Crawler: return "Crawl";
                case MutationType.Scream: return "Scream";
                case MutationType.Spit: return "Spit";
                case MutationType.Jump: return "Jump";
                case MutationType.Overlord: return "Overlord";
                case MutationType.Explode: return "Boom";
                case MutationType.Sacrifice: return "Sacrifice";
                case MutationType.Contamination: return "Infect";
                default: return t.ToString();
            }
        }

        private static Color PipColor(MutationType t)
        {
            switch (t)
            {
                case MutationType.Run: return new Color(1f, 0.55f, 0.15f);
                case MutationType.Tank: return new Color(0.65f, 0.35f, 1f);
                case MutationType.Crawler: return new Color(0.2f, 0.9f, 0.8f);
                case MutationType.Scream: return new Color(1f, 0.9f, 0.2f);
                case MutationType.Spit: return new Color(0.5f, 1f, 0.2f);
                case MutationType.Jump: return new Color(0.3f, 0.6f, 1f);
                case MutationType.Overlord: return new Color(1f, 0.3f, 0.3f);
                default: return Color.white;
            }
        }

        private static string SignatureText(string sig)
        {
            switch (sig)
            {
                case "rage": return "Rage: +50% damage below half health";
                case "stone": return "Stone skin: one hit can't take over 25% HP";
                case "corrosive": return "Corrosive: thorns doubled";
                case "trail": return "Acid trail: hurts humans it runs past";
                case "pulse": return "Sonic pulse: hurts humans nearby every 4s";
                case "execute": return "Execute: kills wounded humans on touch";
                case "quake": return "Quake: shockwave when it lands";
                default: return null;
            }
        }

        private static string Who(int p)
        {
            return p == 1 ? "P1" : (p == 2 ? "P2" : "Ovl");
        }

        private static Rect ScreenRect(Camera cam, FusionState s)
        {
            Bounds b;
            CharacterSelection sel = s.Character.Components.Selection;
            if (sel != null && sel.Collider != null) b = sel.Collider.bounds;
            else b = new Bounds(s.Character.transform.position, new Vector3(1f, 2f, 0f));
            Vector3 lo = cam.WorldToScreenPoint(b.min), hi = cam.WorldToScreenPoint(b.max);
            float x0 = Mathf.Min(lo.x, hi.x), x1 = Mathf.Max(lo.x, hi.x);
            float y0 = Mathf.Min(lo.y, hi.y), y1 = Mathf.Max(lo.y, hi.y);
            return new Rect(x0, Screen.height - y1, x1 - x0, y1 - y0);
        }

        // Floating "name + stats" tags above fused zombies, plus a full breakdown when a cursor is over one.
        // cursors are in Unity screen space (origin bottom-left).
        public static void DrawTags(Texture2D white, Vector2 mouse, Vector2 pad, bool padActive)
        {
            if (!ShowTags || !Enabled || !IsActive() || States.Count == 0) return;
            if (!ComponentSingleton<CameraManager>.HasInstance()) return;
            Camera cam = ComponentSingleton<CameraManager>.Instance.MainCamera;
            if (cam == null) return;

            FusionState hovered = null;
            Rect hoveredRect = default(Rect);
            Vector2 mouseGui = new Vector2(mouse.x, Screen.height - mouse.y);
            Vector2 padGui = new Vector2(pad.x, Screen.height - pad.y);

            foreach (KeyValuePair<Mutation, FusionState> kv in States)
            {
                FusionState s = kv.Value;
                if (s.Character == null || !s.Character.gameObject.activeInHierarchy) continue;

                // Open fusion window: one hit so far, hint that the other player can complete it.
                if (!s.Fused)
                {
                    if (s.Hits.Count == 1)
                    {
                        float left = Window - (Time.unscaledTime - s.Hits[0].Time);
                        if (left > 0f)
                        {
                            Rect hb = ScreenRect(cam, s);
                            string txt = "FUSE! " + Mathf.CeilToInt(left) + "s";
                            Rect hr = new Rect(hb.center.x - 40f, hb.y - 20f, 80f, 17f);
                            GUI.color = new Color(0f, 0f, 0f, 0.7f);
                            GUI.DrawTexture(hr, white);
                            GUI.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.2f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f));
                            GUI.Label(new Rect(hr.x + 6f, hr.y, hr.width, hr.height), txt);
                        }
                    }
                    continue;
                }
                if (string.IsNullOrEmpty(s.Name)) continue;

                Rect body = ScreenRect(cam, s);
                if (body.Contains(mouseGui) || (padActive && body.Contains(padGui)))
                {
                    hovered = s;
                    hoveredRect = body;
                }

                float w = Mathf.Max(96f, s.Name.Length * 7.2f + 44f);
                float h = ShowStatsLine ? 32f : 18f;
                Rect r = new Rect(body.center.x - w * 0.5f, body.y - h - 4f, w, h);

                GUI.color = new Color(0f, 0f, 0f, 0.78f);
                GUI.DrawTexture(r, white);
                Color c = s.Tint;
                c.a = 1f;
                GUI.color = c;
                GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), white);
                GUI.Label(new Rect(r.x + 4f, r.y, r.width - 4f, 18f), s.Name + "  Lv" + s.Hits.Count);

                // One colored pip per ingredient, so the recipe reads at a glance.
                for (int pi = 0; pi < s.Hits.Count; pi++)
                {
                    GUI.color = PipColor(s.Hits[pi].Type);
                    GUI.DrawTexture(new Rect(r.x + 4f + pi * 8f, r.y - 5f, 6f, 5f), white);
                }
                GUI.color = c;
                if (ShowStatsLine)
                {
                    GUI.color = new Color(0.92f, 0.92f, 0.92f, 1f);
                    GUI.Label(new Rect(r.x + 4f, r.y + 14f, r.width, 18f), StatsLine(s));
                }
            }

            if (hovered != null) DrawBreakdown(white, hovered, hoveredRect);
            GUI.color = Color.white;
        }

        private static void DrawBreakdown(Texture2D white, FusionState s, Rect body)
        {
            List<string> lines = new List<string>();
            lines.Add(s.Name);
            lines.Add("Level " + s.Hits.Count + " / " + MaxStack);

            string stack = "";
            for (int i = 0; i < s.Hits.Count; i++)
                stack += (i > 0 ? " > " : "") + Short(s.Hits[i].Type) + "(" + Who(s.Hits[i].Player) + ")";
            lines.Add(stack);
            lines.Add("");
            lines.Add("Speed       x" + s.SpeedMult.ToString("0.00") + (s.AlwaysRun ? "  (always running)" : ""));
            lines.Add("Toughness   x" + s.ToughMult.ToString("0.00") + "  (takes " + Mathf.RoundToInt(100f / s.ToughMult) + "% damage)");
            lines.Add("Damage      x" + s.DamageMult.ToString("0.00"));
            if (s.Regen > 0f) lines.Add("Regen       " + (s.Regen * 100f).ToString("0.0") + "% HP / sec");
            if (s.Dodge > 0f) lines.Add("Dodge       " + Mathf.RoundToInt(s.Dodge * 100f) + "%");
            if (s.Thorns > 0f) lines.Add("Thorns      " + Mathf.RoundToInt(s.Thorns * 100f) + "% reflected");
            if (s.Leech > 0f) lines.Add("Leech       " + Mathf.RoundToInt(s.Leech * 100f) + "% of damage healed");
            string sigText = SignatureText(s.Signature);
            if (sigText != null) { lines.Add(""); lines.Add("* " + sigText); }

            float width = 250f;
            foreach (string l in lines) width = Mathf.Max(width, l.Length * 6.6f + 16f);
            float height = lines.Count * 15f + 10f;

            float x = body.xMax + 8f;
            if (x + width > Screen.width) x = Mathf.Max(4f, body.x - width - 8f);
            float y = Mathf.Clamp(body.y, 4f, Screen.height - height - 4f);
            Rect panel = new Rect(x, y, width, height);

            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(panel, white);
            Color c = s.Tint;
            c.a = 1f;
            GUI.color = c;
            GUI.DrawTexture(new Rect(panel.x, panel.y, 3f, panel.height), white);
            for (int i = 0; i < lines.Count; i++)
            {
                GUI.color = i == 0 ? c : new Color(0.92f, 0.92f, 0.92f, 1f);
                GUI.Label(new Rect(panel.x + 8f, panel.y + 4f + i * 15f, panel.width - 10f, 18f), lines[i]);
            }
        }
    }
}
