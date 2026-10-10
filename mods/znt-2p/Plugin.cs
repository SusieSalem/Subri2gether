using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Znt2P
{
    // P2's hover/click code goes through the game's own highlight code, which also swaps the OS mouse cursor
    // icon (P1's). Ignore those calls while P2 is acting.
    [HarmonyPatch(typeof(CursorManager), "SetCursor", new[] { typeof(Texture2D), typeof(Vector2), typeof(CursorMode) })]
    internal static class CursorSuppressPatch
    {
        private static bool Prefix()
        {
            return !P2Controller.SuppressCursor;
        }
    }

    [BepInPlugin("com.local.znt2p", "ZNT 2 Player", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> startEnabled;
        private ConfigEntry<KeyCode> toggleKey;
        private ConfigEntry<float> cursorSpeed;
        private ConfigEntry<float> deadzone;
        private ConfigEntry<bool> logButtons;

        private bool active;
        private bool sdlOk;
        private Vector2 cursor; // Unity screen space: pixels, origin bottom-left
        private readonly bool[] prevButtons = new bool[Sdl.ButtonCount];
        private Texture2D white;
        private readonly P2Controller p2 = new P2Controller();
        private Color p2Color;
        private readonly HighlightRecolor recolor = new HighlightRecolor();
        private ConfigEntry<float> cameraPanSpeed;
        private float cursorImageScale = 0.6f;
        private readonly Frenzy frenzy = new Frenzy();
        private ConfigEntry<KeyCode> injectKey;
        private ConfigEntry<KeyCode> almanacKey;
        private ConfigEntry<int> injectCost;
        private bool showAlmanac;
        private string discoverText = "";
        private float discoverUntil;
        private readonly CameraAutoFit autoFit = new CameraAutoFit();
        private float nextErrorLog;
        private string toast = "";
        private float toastUntil;

        private void Awake()
        {
            startEnabled = Config.Bind("General", "EnabledAtStart", true, "Start with 2-player mode on.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F9, "Key that turns 2-player mode on/off.");
            cursorSpeed = Config.Bind("Player2", "CursorSpeed", 900f, "P2 cursor speed in pixels/second at full stick.");
            deadzone = Config.Bind("Player2", "Deadzone", 0.2f, "Stick deadzone (0..1).");
            logButtons = Config.Bind("Debug", "LogButtons", false, "Log P2 button presses (useful to verify the mapping).");
            ConfigEntry<string> colorCfg = Config.Bind("Player2", "CursorColor", "#FF2020", "P2 cursor/label color as a #RRGGBB hex string (default: bright red).");
            if (!ColorUtility.TryParseHtmlString(colorCfg.Value, out p2Color)) p2Color = new Color(0.486f, 0.796f, 0.169f, 1f);
            ConfigEntry<string> hlCfg = Config.Bind("Player2", "HighlightColor", "#FF2020", "Color of P2 hover highlight: sprite tint plus an outline frame (P1 keeps the game own highlight).");
            Color hl;
            p2.HighlightColor = ColorUtility.TryParseHtmlString(hlCfg.Value, out hl) ? hl : p2Color;
            p2.TintStrength = Config.Bind("Player2", "SpriteTintStrength", 0.45f, "How strongly P2's hover tints the character sprite (0 = not at all, 1 = full color; high values look dark).").Value;
            p2.RumbleScale = Config.Bind("Player2", "RumbleStrength", 1f, "Controller vibration strength multiplier (0 = off).").Value;
            p2.AssistRadiusPx = Config.Bind("Player2", "AimAssistRadius", 45f, "Screen pixels around the P2 cursor in which targets and clickable objects are picked up (0 = off).").Value;
            p2.AssistFriction = Config.Bind("Player2", "AimAssistFriction", 0.5f, "Cursor speed multiplier while over a target (1 = no slowdown).").Value;
            Fusion.Enabled = Config.Bind("Fusion", "Enabled", true, "Both players mutating the same zombie within the window fuses it and stacks further mutations onto it.").Value;
            Fusion.Window = Config.Bind("Fusion", "WindowSeconds", 6f, "Max seconds between the two players' hits for a fusion to start.").Value;
            Fusion.MaxStack = Mathf.Max(2, Config.Bind("Fusion", "MaxStack", 10, "Most mutations a fused zombie can hold.").Value);
            Fusion.SpeedPerRun = Config.Bind("Fusion", "SpeedPerRunner", 0.30f, "Speed added per Runner hit (diminishing: each further hit is worth 80% of the last).").Value;
            Fusion.ToughPerTank = Config.Bind("Fusion", "ToughnessPerTank", 0.60f, "Damage resistance added per Tank hit (1.6 = takes 1/1.6 of the damage).").Value;
            Fusion.DamagePerTank = Config.Bind("Fusion", "DamagePerTank", 0.50f, "Damage dealt added per Tank hit.").Value;
            Fusion.MaxSpeedMult = Config.Bind("Fusion", "MaxSpeedMultiplier", 3f, "Hard cap on a fused zombie's speed multiplier.").Value;
            Fusion.AlwaysRunOnSuperRunner = Config.Bind("Fusion", "SuperRunnerAlwaysRuns", true, "Two Runner hits keep the zombie sprinting instead of timing out.").Value;
            Fusion.ShowTags = Config.Bind("Fusion", "ShowTags", true, "Floating name + level tag above fused zombies. Hover one with either cursor for the full stat breakdown.").Value;
            Fusion.ShowStatsLine = Config.Bind("Fusion", "ShowStatsLine", true, "Second line on the tag with speed / defence / damage multipliers.").Value;
            Fusion.IsActive = () => active;
            injectKey = Config.Bind("Fusion", "InjectKey", KeyCode.I, "P1: press while the mouse is over a zombie to inject it into a random fusion (P2 uses the Y button).");
            injectCost = Config.Bind("Fusion", "InjectionCostDna", 5, "DNA an injection costs (shared pool). Pricier than a normal mutation on purpose.");
            almanacKey = Config.Bind("Fusion", "AlmanacKey", KeyCode.F10, "Opens the fusion almanac (P2: Minus/Back button).");
            Fusion.DiscoveredPath = Path.Combine(Paths.ConfigPath, "znt2p_discovered.txt");
            Fusion.LoadDiscovered();
            Fusion.OnDiscovered = delegate(string name)
            {
                discoverText = "NEW FUSION DISCOVERED: " + name.ToUpper();
                discoverUntil = Time.unscaledTime + 3.5f;
                Sdl.Rumble(0.6f, 1f, 250);
            };

            frenzy.Enabled = Config.Bind("Frenzy", "Enabled", true, "Feeding frenzy: both players mutate within a short window of each other for a bonus.").Value;
            frenzy.Window = Config.Bind("Frenzy", "WindowSeconds", 1.2f, "Max seconds between P1's and P2's mutation for it to count.").Value;
            frenzy.Cooldown = Config.Bind("Frenzy", "CooldownSeconds", 3f, "Minimum seconds between two frenzies.").Value;
            frenzy.BonusDna = Config.Bind("Frenzy", "BonusDna", 1, "DNA granted per frenzy (0 = just the flash).").Value;
            frenzy.IsActive = () => active;
            frenzy.Hook();
            cursorImageScale = Config.Bind("Player2", "CursorImageScale", 0.6f, "Size of the mutation cursor image next to the P2 cursor (1 = the same size as the mouse cursor).").Value;
            cameraPanSpeed = Config.Bind("Player2", "CameraPanSpeed", 1.4f, "Right-stick camera speed, in screen heights per second.");
            autoFit.MaxZoomOut = Config.Bind("Camera", "AutoZoomMax", 1.35f, "Max zoom-out multiplier when the two cursors are far apart (1 = off).").Value;
            autoFit.Threshold = Config.Bind("Camera", "AutoZoomStart", 0.35f, "Cursor distance (fraction of the screen diagonal) where zooming out starts.").Value;
            autoFit.Full = Config.Bind("Camera", "AutoZoomFull", 0.8f, "Cursor distance (fraction of the screen diagonal) where the maximum zoom-out is reached.").Value;
            autoFit.Smoothing = Config.Bind("Camera", "AutoZoomSmoothing", 3f, "How quickly the zoom follows (higher = snappier).").Value;

            ConfigEntry<string> p1Cfg = Config.Bind("Player1", "HighlightColor", "", "Optional color for the game own (P1) hover highlight while 2P is on. Empty = leave the game default (dark green).");
            Color p1c;
            recolor.Enabled = !string.IsNullOrEmpty(p1Cfg.Value) && ColorUtility.TryParseHtmlString(p1Cfg.Value, out p1c);
            if (recolor.Enabled) { ColorUtility.TryParseHtmlString(p1Cfg.Value, out p1c); recolor.P1Color = p1c; }

            // Install every patch class on its own so one that no longer matches the game can't take the plugin down.
            Harmony harmony = new Harmony("com.local.znt2p");
            foreach (System.Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
                try
                {
                    harmony.CreateClassProcessor(t).Patch();
                    Logger.LogInfo("Patched: " + t.Name);
                }
                catch (System.Exception e)
                {
                    Logger.LogError("Patch FAILED (feature disabled): " + t.Name + " - " + e.Message);
                }
            }

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            sdlOk = Sdl.Init(Path.Combine(dir, "SDL2.dll"));
            if (sdlOk) Logger.LogInfo("SDL2 initialized.");
            else Logger.LogError("SDL2 init failed: " + Sdl.LastError);

            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();

            active = startEnabled.Value;
            cursor = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Toast("ZNT 2P loaded. " + toggleKey.Value + " = toggle. 2P " + (active ? "ON" : "OFF"));
        }

        private void OnDestroy()
        {
            frenzy.Unhook();
            recolor.Restore();
            Sdl.Shutdown();
        }

        private void Toast(string msg)
        {
            toast = msg;
            toastUntil = Time.unscaledTime + 4f;
            Logger.LogInfo(msg);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value))
            {
                active = !active;
                if (!active)
                {
                    p2.Reset();
                    Fusion.Clear();
                }
                Toast("2-player mode " + (active ? "ON" : "OFF") + (active && !Sdl.Connected ? " (no controller found)" : ""));
            }
            recolor.Tick(active, Time.unscaledTime);
            Fusion.Tick(Time.unscaledTime);

            if (Input.GetKeyDown(almanacKey.Value)) showAlmanac = !showAlmanac;
            if (active && Input.GetKeyDown(injectKey.Value)) TryInject((Vector2)Input.mousePosition, 1, "P1");
            if (!active || !sdlOk) return;

            bool wasConnected = Sdl.Connected;
            Sdl.Update(Time.unscaledTime);
            if (Sdl.Connected != wasConnected)
                Toast(Sdl.Connected ? "P2 controller connected: " + Sdl.PadName : "P2 controller disconnected");

            if (!Sdl.Connected) return;

            Vector2 stick = new Vector2(Sdl.Axis(Sdl.AxisLeftX), -Sdl.Axis(Sdl.AxisLeftY));
            float mag = stick.magnitude;
            float dz = deadzone.Value;
            if (mag > dz)
            {
                float scaled = (mag - dz) / (1f - dz);
                stick = stick / mag * scaled;
                cursor += stick * cursorSpeed.Value * p2.SpeedMultiplier * Time.unscaledDeltaTime;
            }
            cursor.x = Mathf.Clamp(cursor.x, 0f, Screen.width - 1);
            cursor.y = Mathf.Clamp(cursor.y, 0f, Screen.height - 1);

            bool[] edge = new bool[Sdl.ButtonCount];
            for (int b = 0; b < Sdl.ButtonCount; b++)
            {
                bool now = Sdl.Button(b);
                edge[b] = now && !prevButtons[b];
                if (logButtons.Value && edge[b]) Logger.LogInfo("P2 button " + b + " pressed");
                prevButtons[b] = now;
            }

            if (edge[Sdl.ButtonBack]) showAlmanac = !showAlmanac;
            if (edge[Sdl.ButtonY]) TryInject(cursor, 2, "P2");

            try
            {
                // Right stick pans the shared camera.
                Vector2 pan = new Vector2(Sdl.Axis(Sdl.AxisRightX), -Sdl.Axis(Sdl.AxisRightY));
                float pm = pan.magnitude;
                if (pm > dz)
                    p2.PanCamera(pan / pm * ((pm - dz) / (1f - dz)), cameraPanSpeed.Value);

                // While an Overlord direction menu is open, D-pad picks the direction and B cancels.
                bool dLeft = edge[Sdl.ButtonDpadLeft], dRight = edge[Sdl.ButtonDpadRight];
                bool a = edge[Sdl.ButtonA], b = edge[Sdl.ButtonB];
                bool cancel = b || edge[Sdl.ButtonDpadDown];
                bool inBlocker = p2.HandleBlockerMenu(dLeft, dRight, cancel);
                if (inBlocker)
                {
                    dLeft = dRight = b = false;
                }

                // Fallbacks: D-pad left/right also cycle mutations; stick clicks jump to the next target.
                bool prev = edge[Sdl.ButtonLShoulder] || dLeft;
                bool next = edge[Sdl.ButtonRShoulder] || dRight;
                if (edge[Sdl.ButtonLStick]) p2.SnapToTarget(+1, ref cursor);
                if (edge[Sdl.ButtonRStick]) p2.SnapToTarget(-1, ref cursor);
                p2.Tick(cursor, a, b, prev, next);
            }
            catch (System.Exception e)
            {
                // Never let a P2 bug take the game down; report at most once every few seconds.
                if (Time.unscaledTime >= nextErrorLog)
                {
                    nextErrorLog = Time.unscaledTime + 5f;
                    Logger.LogError("P2 error: " + e);
                }
            }
        }

        // Injection: pay extra DNA to turn the zombie under the cursor straight into a random fusion.
        private void TryInject(Vector2 screen, int player, string who)
        {
            if (!ComponentSingleton<MutationSelector>.HasInstance()) return;
            MutationSelector sel = ComponentSingleton<MutationSelector>.Instance;

            CharacterSelection target = p2.ZombieAt(screen);
            if (target == null) { Toast(who + ": no zombie there to inject"); return; }

            int cost = injectCost.Value;
            if (!sel.InfiniteDna && sel.Dna < cost)
            {
                Toast(who + ": injection needs " + cost + " DNA (you have " + sel.Dna + ")");
                if (player == 2) Sdl.Rumble(0f, 0.3f, 80);
                return;
            }

            Character ch = target.Root.GetComponent<Character>();
            string result = ch != null ? Fusion.Inject(ch.Components.Mutation, player) : null;
            if (result == null) { Toast(who + ": that can't be injected"); return; }

            sel.ConsumeDna(cost);
            Toast(who + " injected: " + result + " (-" + cost + " DNA)");
            if (player == 2) Sdl.Rumble(1f, 0.8f, 200);
        }

        private void DrawAlmanac()
        {
            List<KeyValuePair<string, string>> all = new List<KeyValuePair<string, string>>(Fusion.AllRecipes);
            int found = 0;
            foreach (KeyValuePair<string, string> kv in all) if (Fusion.Discovered.Contains(kv.Value)) found++;

            const int cols = 3;
            int rows = Mathf.CeilToInt(all.Count / (float)cols);
            float colW = 230f, rowH = 15f;
            float w = colW * cols + 20f, h = rows * rowH + 56f;
            Rect panel = new Rect((Screen.width - w) * 0.5f, Mathf.Max(10f, (Screen.height - h) * 0.5f), w, h);

            GUI.color = new Color(0f, 0f, 0f, 0.9f);
            GUI.DrawTexture(panel, white);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 10f, panel.y + 6f, w, 20f), "FUSION ALMANAC   " + found + " / " + all.Count + " discovered");
            GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            GUI.Label(new Rect(panel.x + 10f, panel.y + 22f, w, 20f), "R=Run T=Tank C=Crawler S=Scream P=Spit J=Jump   (key: " + almanacKey.Value + " / Minus)");

            for (int i = 0; i < all.Count; i++)
            {
                int col = i / rows, row = i % rows;
                bool known = Fusion.Discovered.Contains(all[i].Value);
                GUI.color = known ? new Color(0.55f, 0.95f, 0.3f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
                string text = known ? all[i].Value + "  [" + all[i].Key + "]" : "???  [" + new string('?', all[i].Key.Length) + "]";
                GUI.Label(new Rect(panel.x + 10f + col * colW, panel.y + 44f + row * rowH, colW, rowH + 2f), text);
            }
            GUI.color = Color.white;
        }

        private void LateUpdate()
        {
            if (active) p2.ApplyTint();

            // Runs even when 2P is off so a zoomed-out camera eases back to normal.
            try
            {
                autoFit.Tick(active && sdlOk && Sdl.Connected, (Vector2)Input.mousePosition, cursor, Time.unscaledDeltaTime);
            }
            catch (System.Exception e)
            {
                if (Time.unscaledTime >= nextErrorLog)
                {
                    nextErrorLog = Time.unscaledTime + 5f;
                    Logger.LogError("Camera auto-fit error: " + e);
                }
            }
        }

        // Corner brackets: far less visual weight than a full box.
        private void DrawBrackets(Rect r, float len, float t)
        {
            float lx = Mathf.Min(len, r.width * 0.5f), ly = Mathf.Min(len, r.height * 0.5f);
            GUI.DrawTexture(new Rect(r.x, r.y, lx, t), white);
            GUI.DrawTexture(new Rect(r.x, r.y, t, ly), white);
            GUI.DrawTexture(new Rect(r.xMax - lx, r.y, lx, t), white);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, ly), white);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, lx, t), white);
            GUI.DrawTexture(new Rect(r.x, r.yMax - ly, t, ly), white);
            GUI.DrawTexture(new Rect(r.xMax - lx, r.yMax - t, lx, t), white);
            GUI.DrawTexture(new Rect(r.xMax - t, r.yMax - ly, t, ly), white);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.depth = -10000;
            if (Time.unscaledTime < toastUntil)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(10, 10, 520, 26), white);
                GUI.color = Color.white;
                GUI.Label(new Rect(16, 12, 510, 24), toast);
            }
            Fusion.DrawTags(white, (Vector2)Input.mousePosition, cursor, active && Sdl.Connected);

            if (Time.unscaledTime < discoverUntil)
            {
                float life = (discoverUntil - Time.unscaledTime) / 3.5f;
                GUIStyle ds = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 26 + (life > 0.9f ? 8 : 0),
                    fontStyle = FontStyle.Bold
                };
                Rect dr = new Rect(0f, Screen.height * 0.30f, Screen.width, 50f);
                float da = Mathf.Clamp01(life * 3f);
                GUI.color = new Color(0f, 0f, 0f, da);
                GUI.Label(new Rect(dr.x + 2f, dr.y + 2f, dr.width, dr.height), discoverText, ds);
                GUI.color = new Color(1f, 0.85f, 0.2f, da);
                GUI.Label(dr, discoverText, ds);
                GUI.color = Color.white;
            }
            if (showAlmanac) DrawAlmanac();

            if (Time.unscaledTime < frenzy.FlashUntil)
            {
                // Big centered banner that pops in and fades out.
                float life = (frenzy.FlashUntil - Time.unscaledTime) / 1.6f;
                float pop = 1f + 0.35f * Mathf.Clamp01((life - 0.85f) / 0.15f);
                int size = Mathf.RoundToInt(34f * pop);
                GUIStyle big = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = size,
                    fontStyle = FontStyle.Bold
                };
                Rect r = new Rect(0f, Screen.height * 0.18f, Screen.width, 60f);
                float a = Mathf.Clamp01(life * 2f);
                GUI.color = new Color(0f, 0f, 0f, a);
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), frenzy.FlashText, big);
                Color c = Color.Lerp(p2Color, new Color(0.486f, 0.796f, 0.169f, 1f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 18f));
                c.a = a;
                GUI.color = c;
                GUI.Label(r, frenzy.FlashText, big);
                GUI.color = Color.white;
            }
            if (!active || !Sdl.Connected) return;

            Rect hover;
            if (p2.GetHoverScreenRect(out hover))
            {
                hover = new Rect(hover.x - 3f, hover.y - 3f, hover.width + 6f, hover.height + 6f);
                float len = Mathf.Clamp(Mathf.Min(hover.width, hover.height) * 0.35f, 5f, 12f);
                // Soft glowing fill (gently pulsing) so the target reads as bright red, not dark.
                Color glow = p2.HighlightColor;
                glow.a = 0.14f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
                GUI.color = glow;
                GUI.DrawTexture(hover, white);

                GUI.color = Color.black; // dark underlay so the bright brackets read on any background
                DrawBrackets(new Rect(hover.x - 1f, hover.y - 1f, hover.width + 2f, hover.height + 2f), len + 2f, 5f);
                Color solid = p2.HighlightColor;
                solid.a = 1f;
                GUI.color = solid;
                DrawBrackets(hover, len, 3f);
                GUI.color = Color.white;
            }

            // IMGUI origin is top-left, Unity screen space is bottom-left.
            float x = cursor.x, y = Screen.height - cursor.y;

            // The selected mutation's own cursor image, hanging off the click point like an OS cursor.
            Texture2D cursorTex = p2.CurrentCursor;
            if (cursorTex != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(x, y, cursorTex.width * cursorImageScale, cursorTex.height * cursorImageScale), cursorTex, ScaleMode.StretchToFill, true);
            }

            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x - 12, y - 2, 24, 4), white);
            GUI.DrawTexture(new Rect(x - 2, y - 12, 4, 24), white);
            GUI.color = p2Color;
            GUI.DrawTexture(new Rect(x - 10, y - 1, 20, 2), white);
            GUI.DrawTexture(new Rect(x - 1, y - 10, 2, 20), white);
            GUI.color = Color.white;
            if (p2.StatusLabel != null)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                float ly = y + 8f + (cursorTex != null ? cursorTex.height * cursorImageScale : 0f);
                GUI.DrawTexture(new Rect(x + 14, ly, 330, 22), white);
                GUI.color = p2Color;
                GUI.Label(new Rect(x + 18, ly + 1f, 324, 22), p2.StatusLabel);
                GUI.color = Color.white;
            }
        }
    }
}
