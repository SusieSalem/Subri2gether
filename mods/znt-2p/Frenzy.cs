using System;
using UnityEngine;

namespace Znt2P
{
    // "Feeding frenzy": when both players apply a mutation within a short window of each other, the horde gets a
    // bonus. It rewards actually coordinating.
    //
    // The game raises MutationSelector.OnMutationApplied for every consumed mutation regardless of who did it, so
    // P2's applications are tagged with P2Controller.InP2Apply and everything else counts as P1.
    internal class Frenzy
    {
        public float Window = 1.2f;     // max seconds between the two players' mutations
        public float Cooldown = 3f;     // minimum seconds between two frenzies
        public int BonusDna = 1;        // DNA granted per frenzy (0 = flash only)
        public bool Enabled = true;

        public float FlashUntil { get; private set; }
        public string FlashText { get; private set; }

        private float lastP1 = -100f;
        private float lastP2 = -100f;
        private float cooldownUntil;
        private bool hooked;

        public void Hook()
        {
            if (hooked) return;
            MutationSelector.OnMutationApplied = (Action<MutationType>)Delegate.Combine(
                MutationSelector.OnMutationApplied, new Action<MutationType>(OnApplied));
            hooked = true;
        }

        public void Unhook()
        {
            if (!hooked) return;
            MutationSelector.OnMutationApplied = (Action<MutationType>)Delegate.Remove(
                MutationSelector.OnMutationApplied, new Action<MutationType>(OnApplied));
            hooked = false;
        }

        // Plugin sets this to "2P mode currently on" so frenzy never fires in plain single-player.
        public Func<bool> IsActive = () => true;

        private void OnApplied(MutationType type)
        {
            if (!Enabled || !IsActive()) return;
            float now = Time.unscaledTime;
            if (P2Controller.InP2Apply) lastP2 = now; else lastP1 = now;

            if (now < cooldownUntil) return;
            if (Mathf.Abs(lastP1 - lastP2) > Window) return;

            cooldownUntil = now + Cooldown;
            lastP1 = lastP2 = -100f;

            string text = "FEEDING FRENZY!";
            if (BonusDna > 0 && ComponentSingleton<MutationSelector>.HasInstance())
            {
                ComponentSingleton<MutationSelector>.Instance.GainDna(BonusDna);
                text += "  +" + BonusDna + " DNA";
            }
            FlashText = text;
            FlashUntil = now + 1.6f;
            Sdl.Rumble(1f, 1f, 220);
        }
    }
}
