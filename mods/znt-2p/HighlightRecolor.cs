using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Znt2P
{
    // Recolors the game's own (P1) hover highlight so it differs from P2's. Only active while 2P mode is on;
    // the original colors are restored when it is switched off.
    internal class HighlightRecolor
    {
        private static readonly FieldInfo ColorField =
            typeof(SpriteHighlight).GetField("Color", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private readonly Dictionary<SpriteHighlight, Color> originals = new Dictionary<SpriteHighlight, Color>();
        private readonly List<SpriteHighlight> dead = new List<SpriteHighlight>();
        private float nextScan;

        public bool Enabled;
        public Color P1Color = new Color(0.84f, 0.15f, 0.24f, 1f);

        public void Tick(bool active, float now)
        {
            if (ColorField == null) return;
            if (!active || !Enabled)
            {
                Restore();
                return;
            }
            if (now < nextScan) return;
            nextScan = now + 1f; // pooled characters spawn at any time, so rescan periodically

            CharacterHighlight[] all = Object.FindObjectsOfType<CharacterHighlight>();
            for (int i = 0; i < all.Length; i++)
            {
                CharacterHighlight h = all[i];
                if (!originals.ContainsKey(h)) originals[h] = (Color)ColorField.GetValue(h);
                ColorField.SetValue(h, P1Color);
            }

            dead.Clear();
            foreach (KeyValuePair<SpriteHighlight, Color> kv in originals)
                if (kv.Key == null) dead.Add(kv.Key);
            for (int i = 0; i < dead.Count; i++) originals.Remove(dead[i]);
        }

        public void Restore()
        {
            if (originals.Count == 0) return;
            foreach (KeyValuePair<SpriteHighlight, Color> kv in originals)
                if (kv.Key != null) ColorField.SetValue(kv.Key, kv.Value);
            originals.Clear();
            nextScan = 0f;
        }
    }
}
