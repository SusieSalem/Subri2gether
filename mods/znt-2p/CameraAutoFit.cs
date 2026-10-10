using UnityEngine;

namespace Znt2P
{
    // Zooms the shared camera out while the two cursors are far apart on screen, and back in when they are close.
    //
    // The player can still zoom with the mouse wheel: any zoom change we did not make ourselves is treated as the
    // new "base" zoom, and our extra zoom-out is applied on top of it as a multiplier.
    internal class CameraAutoFit
    {
        public float MaxZoomOut = 1.35f;   // 1 = feature off
        public float Threshold = 0.35f;    // cursor distance (fraction of screen diagonal) where zooming out starts
        public float Full = 0.8f;          // ... and where the maximum zoom-out is reached
        public float Smoothing = 3f;       // higher = snappier

        private CameraZoom zoomRef;
        private float baseSize;
        private float lastSet = -1f;
        private float appliedMult = 1f;
        private float mult = 1f;

        private void Reset()
        {
            baseSize = 0f;
            lastSet = -1f;
            appliedMult = 1f;
            mult = 1f;
        }

        public void Tick(bool enabled, Vector2 p1, Vector2 p2, float dt)
        {
            if (!ComponentSingleton<CameraZoom>.HasInstance())
            {
                zoomRef = null;
                Reset();
                return;
            }
            CameraZoom zoom = ComponentSingleton<CameraZoom>.Instance;
            if (zoom != zoomRef) // new scene / level: forget the old multiplier
            {
                zoomRef = zoom;
                Reset();
            }
            if (zoom.Cameras == null || zoom.Cameras.Count == 0 || zoom.Cameras[0] == null) return;

            bool blocked = !zoom.AllowZooming || GameManager.Instance == null || GameManager.InCutscene();
            float target = 1f;
            if (enabled && !blocked && MaxZoomOut > 1f)
            {
                float diag = Mathf.Sqrt((float)Screen.width * Screen.width + (float)Screen.height * Screen.height);
                float d = diag > 0f ? Mathf.Clamp01((p1 - p2).magnitude / diag) : 0f;
                float t = Mathf.Clamp01((d - Threshold) / Mathf.Max(0.01f, Full - Threshold));
                target = Mathf.Lerp(1f, MaxZoomOut, t);
            }

            float current = zoom.Cameras[0].orthographicSize;
            if (lastSet < 0f || Mathf.Abs(current - lastSet) > 0.0005f)
                baseSize = current / Mathf.Max(0.01f, appliedMult); // zoom was changed by someone else

            mult += (target - mult) * (1f - Mathf.Exp(-Smoothing * dt));
            if (target == 1f && Mathf.Abs(mult - 1f) < 0.002f) mult = 1f;

            float want = baseSize * mult;
            if (Mathf.Abs(want - current) > 0.001f)
            {
                zoom.SetZoom(want);
                if (ComponentSingleton<CameraScroll>.HasInstance())
                    ComponentSingleton<CameraScroll>.Instance.EnforceCameraBounds();
            }

            float actual = zoom.Cameras[0].orthographicSize;
            lastSet = actual;
            appliedMult = baseSize > 0.0001f ? actual / baseSize : 1f;
        }
    }
}
