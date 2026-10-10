using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Znt2P
{
    // Applies Player 2's cursor to the game by direct injection:
    //  - P2 has its own selected mutation (the game only has one global SelectedMutation, so we swap it in/out
    //    around every P2 action and restore P1's selection afterwards).
    //  - World clicks find a CharacterSelection under the cursor and call ReceiveMutation on it.
    //  - Clicks over uGUI elements are synthesised as pointer events (toolbar, menus).
    internal class P2Controller
    {
        private readonly Collider2D[] hits = new Collider2D[20];
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
        private PointerEventData pointerData;
        private EventSystem pointerSystem;
        private GameObject uiHover;

        private CharacterMutation p2Mutation;
        private CharacterSelection hoverTarget;

        private readonly Collider2D[] snapHits = new Collider2D[64];
        private StairBehaviour[] stairs;
        private ClickableObject[] clickables;
        private float nextInteractableScan;
        private GameObject hoverInteractable;
        private BlockerMenu blockerMenu;
        private float blockerWatchUntil;
        private static FieldInfo blockerButtonsField;

        // Aim assist: radius (screen px) in which targets/objects are picked up without being exactly under the
        // cursor, and how much the cursor slows down while over something grabbable.
        public float AssistRadiusPx = 45f;
        public float AssistFriction = 0.5f;
        public float SpeedMultiplier { get { return hoverTarget != null || hoverInteractable != null ? AssistFriction : 1f; } }
        private tk2dBaseSprite[] hoverSprites;
        private static FieldInfo highlightField;
        private static FieldInfo spritesField;

        // While true, CursorManager.SetCursor is ignored so P2 never changes P1's mouse cursor icon.
        public static bool SuppressCursor;

        // True only while P2's own mutation is being applied, so listeners can tell P2 from P1.
        public static bool InP2Apply;

        public string StatusLabel { get; private set; }

        // The mutation's own cursor image (same one the mouse cursor shows), or null for the plain crosshair.
        // Its top-left corner is the click point, exactly like an OS cursor with a zero hotspot.
        public Texture2D CurrentCursor { get; private set; }
        private bool cannotHover;
        public Color HighlightColor = new Color(0.486f, 0.796f, 0.169f, 1f);
        public float RumbleScale = 1f;
        public float TintStrength = 0.45f; // 0 = no sprite tint, 1 = full highlight color

        private void Buzz(float low, float high, int ms)
        {
            if (RumbleScale > 0f) Sdl.Rumble(low * RumbleScale, high * RumbleScale, ms);
        }

        private static MutationSelector Selector
        {
            get { return ComponentSingleton<MutationSelector>.HasInstance() ? ComponentSingleton<MutationSelector>.Instance : null; }
        }

        public void Reset()
        {
            ClearHover();
            ClearUiHover();
            p2Mutation = null;
            StatusLabel = null;
        }

        // cursor: Unity screen space. a/b/prev/next: button press edges this frame.
        public void Tick(Vector2 cursor, bool a, bool b, bool prev, bool next)
        {
            SuppressCursor = true;
            Fusion.CurrentPlayer = 2; // validity checks made here are P2's (fusions need the *other* player's hit)
            try { TickInner(cursor, a, b, prev, next); }
            finally
            {
                SuppressCursor = false;
                Fusion.CurrentPlayer = 1;
            }
        }

        // Jumps the cursor to the next valid target (for P2's mutation) to the right (dir>0) or left (dir<0).
        public void SnapToTarget(int dir, ref Vector2 cursor)
        {
            MutationSelector sel = Selector;
            if (sel == null || p2Mutation == null || !ComponentSingleton<CharacterSelectionManager>.HasInstance()
                || !ComponentSingleton<CameraManager>.HasInstance())
            {
                Buzz(0f, 0.2f, 60);
                return;
            }
            Camera cam = ComponentSingleton<CameraManager>.Instance.MainCamera;
            if (cam == null) return;

            Vector3 lo = cam.ScreenToWorldPoint(new Vector3(0f, 0f, 0f));
            Vector3 hi = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0f));
            int n = Physics2D.OverlapAreaNonAlloc(lo, hi, snapHits, ComponentSingleton<CharacterSelectionManager>.Instance.ClicLayer);

            List<CharacterSelection> found = new List<CharacterSelection>();
            SuppressCursor = true;
            Fusion.CurrentPlayer = 2;
            CharacterMutation saved = sel.SelectedMutation;
            sel.SelectMutation(p2Mutation);
            for (int i = 0; i < n; i++)
            {
                CharacterSelection cs = snapHits[i].GetComponent<CharacterSelection>();
                if (cs != null && !found.Contains(cs) && cs.CanBeSelected()) found.Add(cs);
            }
            RestoreSelection(sel, saved);
            Fusion.CurrentPlayer = 1;
            SuppressCursor = false;

            if (found.Count == 0) { Buzz(0f, 0.2f, 60); return; }
            found.Sort((x, y) => x.Center.x.CompareTo(y.Center.x));

            float cx = cam.ScreenToWorldPoint(new Vector3(cursor.x, cursor.y, 0f)).x;
            const float eps = 0.05f;
            CharacterSelection pick = null;
            if (dir > 0)
            {
                for (int i = 0; i < found.Count && pick == null; i++)
                    if (found[i].Center.x > cx + eps) pick = found[i];
                if (pick == null) pick = found[0];
            }
            else
            {
                for (int i = found.Count - 1; i >= 0 && pick == null; i--)
                    if (found[i].Center.x < cx - eps) pick = found[i];
                if (pick == null) pick = found[found.Count - 1];
            }

            Vector3 sp = cam.WorldToScreenPoint(pick.Center);
            cursor = new Vector2(Mathf.Clamp(sp.x, 0f, Screen.width - 1), Mathf.Clamp(sp.y, 0f, Screen.height - 1));
            Buzz(0.1f, 0.25f, 35);
        }

        // Screen-space (IMGUI, top-left origin) rectangle around whatever P2 is hovering, for drawing a bright frame.
        // The game's tint only multiplies the sprite color so it can never look bright; the frame can.
        public bool GetHoverScreenRect(out Rect rect)
        {
            rect = default(Rect);
            Bounds bounds;
            if (hoverTarget != null && hoverTarget.Collider != null)
            {
                // Prefer the sprite's visible extent; the selection collider is much larger than the character.
                bool have = false;
                bounds = hoverTarget.Collider.bounds;
                if (hoverSprites != null)
                {
                    for (int i = 0; i < hoverSprites.Length; i++)
                    {
                        if (hoverSprites[i] == null) continue;
                        Renderer r = hoverSprites[i].GetComponent<Renderer>();
                        if (r == null || !r.enabled) continue;
                        if (!have) { bounds = r.bounds; have = true; }
                        else bounds.Encapsulate(r.bounds);
                    }
                }
                if (!have) bounds.size = bounds.size * 0.6f;
            }
            else if (hoverInteractable != null)
            {
                Collider2D c = hoverInteractable.GetComponent<Collider2D>();
                if (c == null) return false;
                bounds = c.bounds;
            }
            else
            {
                return false;
            }
            if (!ComponentSingleton<CameraManager>.HasInstance()) return false;
            Camera cam = ComponentSingleton<CameraManager>.Instance.MainCamera;
            if (cam == null) return false;

            Vector3 a = cam.WorldToScreenPoint(bounds.min);
            Vector3 b = cam.WorldToScreenPoint(bounds.max);
            float x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x);
            float y0 = Mathf.Min(a.y, b.y), y1 = Mathf.Max(a.y, b.y);
            rect = new Rect(x0, Screen.height - y1, x1 - x0, y1 - y0);
            return true;
        }

        // Call from LateUpdate so the P2 tint wins over the game's own highlight on a shared target.
        public void ApplyTint()
        {
            if (hoverSprites == null) return;
            // A multiply-tint can only darken a sprite, so a pure red tint reads as dark/muddy. Blend toward white
            // instead; the bright frame in OnGUI carries the "this is P2's" signal.
            Color tint = Color.Lerp(Color.white, HighlightColor, TintStrength);
            for (int i = 0; i < hoverSprites.Length; i++)
                if (hoverSprites[i] != null) hoverSprites[i].color = tint;
        }

        private void ResolveHoverSprites()
        {
            hoverSprites = null;
            if (hoverTarget == null) return;
            if (highlightField == null)
            {
                highlightField = typeof(CharacterSelection).GetField("highlight", BindingFlags.Instance | BindingFlags.NonPublic);
                spritesField = typeof(SpriteHighlight).GetField("Sprites", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            if (highlightField == null || spritesField == null) return;
            SpriteHighlight h = highlightField.GetValue(hoverTarget) as SpriteHighlight;
            if (h != null) hoverSprites = spritesField.GetValue(h) as tk2dBaseSprite[];
        }

        private void TickInner(Vector2 cursor, bool a, bool b, bool prev, bool next)
        {
            CurrentCursor = null;
            if (EventSystem.current == null)
            {
                ClearHover();
                ClearUiHover();
                StatusLabel = null;
                return;
            }

            // MutationSelector only exists inside levels; menus still get UI clicks.
            MutationSelector sel = Selector;
            if (sel != null)
            {
                if (p2Mutation != null && (!sel.IsAvailable(p2Mutation.Type) || !p2Mutation.UseDNA && sel.GetCount(p2Mutation.Type) <= 0))
                    p2Mutation = null;

                if (prev) { CycleMutation(sel, -1); Buzz(0f, 0.15f, 30); }
                if (next) { CycleMutation(sel, +1); Buzz(0f, 0.15f, 30); }
                if (b) { p2Mutation = null; ClearHover(); }

                bool blockerOpen = blockerMenu != null && blockerMenu.gameObject.activeInHierarchy;
                if (!blockerOpen)
                    StatusLabel = p2Mutation != null
                        ? p2Mutation.Type + (sel.HasCount(p2Mutation.Type) ? " x" + sel.GetCount(p2Mutation.Type) : "")
                        : "(no mutation - L/R to pick)";
            }
            else
            {
                p2Mutation = null;
                StatusLabel = null;
            }

            GameObject ui = RaycastUi(cursor);
            UpdateUiHover(ui, cursor);
            if (ui != null)
            {
                ClearHover();
                if (a) ClickUi(cursor, ui, sel);
                return;
            }

            if (sel == null) return;
            UpdateHover(cursor, sel);

            if (p2Mutation != null)
            {
                Texture2D tex = null;
                if (hoverTarget != null) tex = p2Mutation.CanMutateCursor;
                else if (cannotHover) tex = p2Mutation.CannotMutateCursor;
                CurrentCursor = tex != null ? tex : p2Mutation.DefaultCursor;
            }

            if (a)
            {
                if (hoverTarget != null)
                {
                    ApplyMutation(hoverTarget, sel);
                    Buzz(0.7f, 0.5f, 140);
                }
                else if (hoverInteractable != null)
                {
                    hoverInteractable.SendMessage("OnMouseUpAsButton", SendMessageOptions.DontRequireReceiver);
                    Buzz(0.4f, 0.4f, 80);
                }
                else
                {
                    Buzz(0f, 0.3f, 70); // nothing valid under the cursor
                }
            }
        }

        private void CycleMutation(MutationSelector sel, int dir)
        {
            List<CharacterMutation> all = sel.GetAvailableMutations();
            if (all.Count == 0) { p2Mutation = null; return; }
            int start = p2Mutation == null ? (dir > 0 ? -1 : 0) : all.IndexOf(p2Mutation);
            for (int i = 1; i <= all.Count; i++)
            {
                int idx = ((start + dir * i) % all.Count + all.Count) % all.Count;
                // Skip entries that are available but currently unusable (no uses left / not enough DNA).
                if (sel.CanUse((MutationType)0, all[idx]) || all[idx].UseDNA)
                {
                    p2Mutation = all[idx];
                    ClearHover();
                    return;
                }
            }
        }

        private PointerEventData GetPointerData(Vector2 cursor)
        {
            if (pointerData == null || pointerSystem != EventSystem.current)
            {
                pointerSystem = EventSystem.current;
                pointerData = new PointerEventData(pointerSystem);
            }
            pointerData.Reset();
            pointerData.position = cursor;
            pointerData.button = PointerEventData.InputButton.Left;
            return pointerData;
        }

        // Returns the topmost UI object under the cursor that should block clicks, or null.
        private GameObject RaycastUi(Vector2 cursor)
        {
            raycastResults.Clear();
            EventSystem.current.RaycastAll(GetPointerData(cursor), raycastResults);
            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject go = raycastResults[i].gameObject;
                if (go == null) continue;
                if (go.CompareTag("IgnoreUiRaycast")) continue;
                if (go.layer == LayerMask.NameToLayer("Ignore Raycast")) continue;
                return go;
            }
            return null;
        }

        private void ClickUi(Vector2 cursor, GameObject go, MutationSelector sel)
        {
            PointerEventData pd = GetPointerData(cursor);
            pd.pointerPressRaycast = new RaycastResult { gameObject = go };
            pd.pointerCurrentRaycast = pd.pointerPressRaycast;
            pd.pressPosition = cursor;
            pd.pointerPress = go;

            CharacterMutation before = sel != null ? sel.SelectedMutation : null;
            ExecuteEvents.ExecuteHierarchy(go, pd, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(go, pd, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(go, pd, ExecuteEvents.pointerClickHandler);

            // If the click picked a mutation on the shared toolbar, it belongs to P2, not P1.
            // (The click may also have unloaded the scene, so re-check the selector.)
            if (sel != null && Selector == sel)
            {
                CharacterMutation after = sel.SelectedMutation;
                if (after != before)
                {
                    if (after != null) p2Mutation = after;
                    RestoreSelection(sel, before);
                }
            }
        }

        // Gives menu buttons their hover state under the P2 cursor.
        private void UpdateUiHover(GameObject ui, Vector2 cursor)
        {
            if (ui == uiHover) return;
            ClearUiHover();
            uiHover = ui;
            if (uiHover != null)
                ExecuteEvents.ExecuteHierarchy(uiHover, GetPointerData(cursor), ExecuteEvents.pointerEnterHandler);
        }

        private void ClearUiHover()
        {
            if (uiHover != null && EventSystem.current != null)
                ExecuteEvents.ExecuteHierarchy(uiHover, GetPointerData(Vector2.zero), ExecuteEvents.pointerExitHandler);
            uiHover = null;
        }

        private void UpdateHover(Vector2 cursor, MutationSelector sel)
        {
            CharacterSelection newTarget = null;
            GameObject newInteractable = null;
            if (ComponentSingleton<CharacterSelectionManager>.HasInstance() && ComponentSingleton<CameraManager>.HasInstance())
            {
                CharacterSelectionManager mgr = ComponentSingleton<CharacterSelectionManager>.Instance;
                Camera cam = ComponentSingleton<CameraManager>.Instance.MainCamera;
                if (cam != null && mgr.enabled)
                {
                    Vector3 world = cam.ScreenToWorldPoint(new Vector3(cursor.x, cursor.y, 0f));
                    // Aim assist: also accept things within a few screen pixels of the cursor.
                    float radius = AssistRadiusPx * 2f * cam.orthographicSize / Mathf.Max(1, Screen.height);
                    if (p2Mutation != null && CharacterSelectionManager.AllowSelection)
                        newTarget = FindTarget(world, radius, mgr.ClicLayer, sel);
                    if (newTarget == null)
                        newInteractable = FindInteractable(world, radius);
                }
            }

            if (newTarget != hoverTarget)
            {
                ClearHover();
                hoverTarget = newTarget;
                ResolveHoverSprites();
            }
            if (newInteractable != hoverInteractable)
            {
                ClearInteractableHover();
                hoverInteractable = newInteractable;
                if (hoverInteractable != null)
                {
                    // Only ClickableObject gets enter/exit; StairBehaviour's would toggle P1's selection.
                    if (hoverInteractable.GetComponent<ClickableObject>() != null)
                        hoverInteractable.SendMessage("OnMouseEnter", SendMessageOptions.DontRequireReceiver);
                    Buzz(0f, 0.1f, 20);
                }
            }
            if (hoverTarget != null)
            {
                CharacterMutation saved = sel.SelectedMutation;
                sel.SelectMutation(p2Mutation);
                hoverTarget.OnMouseOverObject();
                RestoreSelection(sel, saved);
            }
        }

        private CharacterSelection FindTarget(Vector3 world, float radius, LayerMask layer, MutationSelector sel)
        {
            cannotHover = false;
            int n = Physics2D.OverlapPointNonAlloc(world, hits, layer);
            bool characterUnderPoint = false;
            for (int i = 0; i < n && !characterUnderPoint; i++)
                characterUnderPoint = hits[i].GetComponent<CharacterSelection>() != null;

            if (n == 0 && radius > 0f)
                n = Physics2D.OverlapCircleNonAlloc(world, radius, hits, layer);
            if (n == 0) return null;

            CharacterSelection best = null;
            float bestDist = float.MaxValue;
            CharacterMutation saved = sel.SelectedMutation;
            sel.SelectMutation(p2Mutation);
            for (int i = 0; i < n; i++)
            {
                CharacterSelection cs = hits[i].GetComponent<CharacterSelection>();
                if (cs == null || !cs.CanBeSelected()) continue;
                float d = (world - cs.Center).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = cs; }
            }
            RestoreSelection(sel, saved);
            cannotHover = best == null && characterUnderPoint; // a character is right there but can't take this mutation
            return best;
        }

        // Levers, stair switches and other click-activated level objects use Unity's real-mouse events,
        // so P2 has to trigger them by hand.
        private GameObject FindInteractable(Vector3 world, float radius)
        {
            // A physics overlap query would have to wade through every tile collider, so look at the actual
            // interactive objects instead (cached; levels don't spawn these often).
            if (Time.unscaledTime >= nextInteractableScan || stairs == null)
            {
                nextInteractableScan = Time.unscaledTime + 0.5f;
                stairs = Object.FindObjectsOfType<StairBehaviour>();
                clickables = Object.FindObjectsOfType<ClickableObject>();
            }

            GameObject best = null;
            float bestDist = float.MaxValue;
            float reach = radius * 1.5f; // switches are small; be a little more generous than for characters
            Vector2 w = world;

            for (int i = 0; i < stairs.Length; i++)
            {
                StairBehaviour s = stairs[i];
                if (s == null || !s.isActiveAndEnabled || !s.IsActive) continue;
                Consider(s.gameObject, w, reach, ref best, ref bestDist);
            }
            for (int i = 0; i < clickables.Length; i++)
            {
                ClickableObject c = clickables[i];
                if (c == null || !c.isActiveAndEnabled || !c.IsActive) continue;
                Consider(c.gameObject, w, reach, ref best, ref bestDist);
            }
            return best;
        }

        private static void Consider(GameObject go, Vector2 world, float reach, ref GameObject best, ref float bestDist)
        {
            Collider2D col = go.GetComponent<Collider2D>();
            if (col == null || !col.enabled) return;
            float d = col.OverlapPoint(world) ? 0f : ((Vector2)col.bounds.ClosestPoint(world) - world).magnitude;
            if (d <= reach && d < bestDist) { bestDist = d; best = go; }
        }

        private void ClearInteractableHover()
        {
            if (hoverInteractable != null && hoverInteractable.GetComponent<ClickableObject>() != null)
                hoverInteractable.SendMessage("OnMouseExit", SendMessageOptions.DontRequireReceiver);
            hoverInteractable = null;
        }

        // The zombie nearest a screen point (used for injections by either player), or null.
        public CharacterSelection ZombieAt(Vector2 screen)
        {
            if (!ComponentSingleton<CharacterSelectionManager>.HasInstance() || !ComponentSingleton<CameraManager>.HasInstance())
                return null;
            Camera cam = ComponentSingleton<CameraManager>.Instance.MainCamera;
            if (cam == null) return null;

            LayerMask layer = ComponentSingleton<CharacterSelectionManager>.Instance.ClicLayer;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            float radius = AssistRadiusPx * 2f * cam.orthographicSize / Mathf.Max(1, Screen.height);
            int n = Physics2D.OverlapPointNonAlloc(world, hits, layer);
            if (n == 0 && radius > 0f) n = Physics2D.OverlapCircleNonAlloc(world, radius, hits, layer);

            CharacterSelection best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                CharacterSelection cs = hits[i].GetComponent<CharacterSelection>();
                if (cs == null) continue;
                Character ch = cs.Root.GetComponent<Character>();
                if (ch == null || !(ch.Behaviour is ZombieBehaviour)) continue;
                float d = (world - cs.Center).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = cs; }
            }
            return best;
        }

        // Overlord (Blocker) opens a Left / Right / Cancel menu after the mutation is applied.
        // While it is open the D-pad picks the direction. Returns true if the input was consumed.
        public bool HandleBlockerMenu(bool left, bool right, bool cancel)
        {
            if (blockerMenu == null || !blockerMenu.gameObject.activeInHierarchy)
            {
                blockerMenu = null;
                if (Time.unscaledTime < blockerWatchUntil)
                {
                    BlockerMenu[] menus = Object.FindObjectsOfType<BlockerMenu>();
                    for (int i = 0; i < menus.Length; i++)
                    {
                        if (menus[i].gameObject.activeInHierarchy) { blockerMenu = menus[i]; blockerWatchUntil = 0f; break; }
                    }
                }
                if (blockerMenu == null) return false;
            }

            StatusLabel = "OVERLORD: D-pad < > = direction, B = cancel";
            if (blockerButtonsField == null)
                blockerButtonsField = typeof(BehaviourMenu<ZombieBehaviour>).GetField("Buttons", BindingFlags.Instance | BindingFlags.NonPublic);
            UnityEngine.UI.Button[] buttons = blockerButtonsField != null ? blockerButtonsField.GetValue(blockerMenu) as UnityEngine.UI.Button[] : null;
            if (buttons != null && buttons.Length >= 3)
            {
                if (left) buttons[0].onClick.Invoke();
                else if (right) buttons[1].onClick.Invoke();
                else if (cancel) buttons[2].onClick.Invoke();
                if (left || right || cancel) Buzz(0.5f, 0.4f, 100);
            }
            return true;
        }

        // Pans the shared camera with the right stick (stick in -1..1, y up).
        public void PanCamera(Vector2 stick, float orthoSizesPerSecond)
        {
            if (stick == Vector2.zero) return;
            if (!ComponentSingleton<CameraScroll>.HasInstance() || !ComponentSingleton<CameraManager>.HasInstance()) return;
            CameraScroll scroll = ComponentSingleton<CameraScroll>.Instance;
            if (!scroll.AllowScrolling || GameManager.Instance == null || GameManager.Instance.Paused || GameManager.InCutscene()) return;
            Camera cam = ComponentSingleton<CameraManager>.Instance.MainCamera;
            if (cam == null) return;

            Vector3 delta = new Vector3(stick.x, stick.y, 0f) * (cam.orthographicSize * orthoSizesPerSecond * Time.unscaledDeltaTime);
            scroll.Position += delta;
            scroll.EnforceCameraBounds();
            if (CameraScroll.OnCameraMoved != null) CameraScroll.OnCameraMoved();
        }

        private void ApplyMutation(CharacterSelection target, MutationSelector sel)
        {
            if (p2Mutation == null || target == null) return;
            CharacterMutation saved = sel.SelectedMutation;
            sel.SelectMutation(p2Mutation);
            if (p2Mutation.Type == MutationType.Overlord) blockerWatchUntil = Time.unscaledTime + 3f;
            InP2Apply = true;
            try { target.ReceiveMutation(true); }
            finally { InP2Apply = false; }
            // The game deselects when the last use is consumed; mirror that for P2.
            if (!p2Mutation.UseDNA && sel.GetCount(p2Mutation.Type) <= 0)
                p2Mutation = null;
            RestoreSelection(sel, saved);
            ClearHover();
        }

        private void ClearHover()
        {
            ClearInteractableHover();
            hoverSprites = null;
            if (hoverTarget != null)
            {
                bool wasSuppressed = SuppressCursor;
                SuppressCursor = true;
                try { hoverTarget.OnMouseExitObject(); }
                finally { SuppressCursor = wasSuppressed; }
                hoverTarget = null;
            }
        }

        private static void RestoreSelection(MutationSelector sel, CharacterMutation saved)
        {
            if (sel.SelectedMutation == saved) return;
            if (saved != null && !sel.IsAvailable(saved.Type)) saved = null;
            sel.SelectMutation(saved);
        }
    }
}
