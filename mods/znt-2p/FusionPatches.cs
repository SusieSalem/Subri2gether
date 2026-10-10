using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Znt2P
{
    // Records every mutation applied to a zombie, and remembers who applied it.
    [HarmonyPatch(typeof(Mutation), "Mutate")]
    internal static class FusionMutatePatch
    {
        private static void Prefix(Mutation __instance, ref KeyValuePair<CharacterMutation, CharacterMutation> __state)
        {
            __state = new KeyValuePair<CharacterMutation, CharacterMutation>(__instance.Transformation, __instance.Effect);
        }

        private static void Postfix(Mutation __instance, CharacterMutation mutation, KeyValuePair<CharacterMutation, CharacterMutation> __state)
        {
            // Mutate returns early when the mutation can't be used; only count it if something actually changed.
            bool changed = __instance.Transformation != __state.Key || __instance.Effect != __state.Value;
            if (changed) Fusion.OnMutated(__instance, mutation);
        }
    }

    // Tells MutationsConfig which zombie (and which player) is asking, so it can allow a same-type re-hit.
    [HarmonyPatch(typeof(Mutation), "CanMutateTo")]
    internal static class FusionCanMutateContextPatch
    {
        private static void Prefix(Mutation __instance)
        {
            Fusion.ContextMutation = __instance;
        }

        private static void Postfix()
        {
            Fusion.ContextMutation = null;
        }
    }

    [HarmonyPatch(typeof(MutationsConfig), "IsAllowedMutation")]
    internal static class FusionAllowedMutationPatch
    {
        private static void Postfix(MutationType newEffect, ref bool __result)
        {
            if (!__result && Fusion.ContextMutation != null && Fusion.AllowsRefuse(Fusion.ContextMutation, newEffect))
                __result = true;
        }
    }

    // Walk/run/sprint speed, scaled per zombie. The game snapshots the "default" speeds through GetSpeed when a
    // transformation starts, so that snapshot must see the raw values or the boost would leak into the defaults.
    [HarmonyPatch(typeof(Moveable), "GetSpeed")]
    internal static class FusionSpeedPatch
    {
        private static void Postfix(Moveable __instance, ref float __result)
        {
            __result *= Fusion.SpeedMultOf(__instance);
        }
    }

    [HarmonyPatch(typeof(CharacterMutation), "UpdateSpeeds")]
    internal static class FusionUpdateSpeedsPatch
    {
        private static void Prefix()
        {
            Fusion.BypassSpeedMult = true;
        }

        private static void Postfix()
        {
            Fusion.BypassSpeedMult = false;
        }
    }

    // Toughness (victim) and damage (attacker) layers.
    [HarmonyPatch(typeof(Health), "ReceiveDamage", new[] { typeof(float), typeof(Transform), typeof(DamageType), typeof(string) })]
    internal static class FusionDamagePatch
    {
        // Returning false skips the original method: the hit was dodged.
        private static bool Prefix(Health __instance, ref float amount, Transform attacker, DamageType dmgType)
        {
            return Fusion.AdjustDamage(__instance, attacker, dmgType, ref amount);
        }
    }

    // An Overlord handing out Run: zombies it commands inherit its fusion level.
    [HarmonyPatch(typeof(BlockerEffect), "MutateTarget")]
    internal static class FusionRallyPatch
    {
        private static void Prefix(BlockerEffect __instance)
        {
            Fusion.CurrentBlocker = __instance.BlockerBehaviour;
        }

        private static void Postfix()
        {
            Fusion.CurrentBlocker = null;
        }
    }
}
