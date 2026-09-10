using System;
using Game.State;
using HarmonyLib;
using Serilog;
using SignalsEverywhere.Signals;
using Track.Signals;

namespace SignalsEverywhere.Patches;

[HarmonyPatch(typeof(CTCPredicateSignal))]
[HarmonyPatchCategory("SignalsEverywhere")]
public class CTCPredicateSignal_Patch
{
    [HarmonyPatch("OnEnable")]
    [HarmonyPrefix]
    public static bool OnEnable_Prefix(CTCPredicateSignal __instance)
    {
        var extension = __instance.GetComponent<CTCPredicateSignalCrossoverExtension>();
        var storage = __instance.GetComponentInParent<SignalStorage>(true);
        if (extension == null || storage == null)
            return true;
        
        OnEnable_Base(__instance);
        if (!StateManager.IsHost)
            return false;
        
        Log.Information("CTCPredicateSignal_Patch: OnEnable");
        if (extension.heads != null)
        {
            foreach (var head in extension.heads)
            {
                if (head.predicates == null) continue;
                foreach (var predicate in head.predicates)
                {
                    if (predicate.original == null) continue;
                    switch (predicate.original.type)
                    {
                        case CTCPredicateSignal.PredicateType.Switch:
                            if (predicate.original.switchNode != null)
                                __instance.UpdatePredicateSignalOnChange<SwitchSetting>(storage.ObserveSwitchPosition, predicate.original.switchNode.id);
                            continue;
                        case CTCPredicateSignal.PredicateType.Block:
                            if (predicate.original.blocks != null)
                            {
                                foreach (var bl in predicate.original.blocks)
                                {
                                    if (bl != null)
                                        __instance.UpdatePredicateSignalOnChange<bool>(storage.ObserveBlockOccupancy, bl.id);
                                }
                            }
                            continue;
                        case CTCPredicateSignal.PredicateType.InterlockingTrafficDirection:
                        case CTCPredicateSignal.PredicateType.InterlockingTrafficDirectionIsNot:
                            if (predicate.crossoverGroupId != null)
                                __instance.UpdatePredicateSignalOnChange<CTCTrafficFilter>(storage.ObserveCrossoverGroupDirection, predicate.crossoverGroupId);
                            if (predicate.original.switchNode != null)
                                __instance.UpdatePredicateSignalOnChange<SwitchSetting>(storage.ObserveSwitchPosition, predicate.original.switchNode.id);
                            continue;
                        default:
                            continue;
                    }
                }
            }
        }

        if (__instance.heads != null)
        {
            foreach (var originalHeads in __instance.heads)
            {
                if (originalHeads.nextSignal != null)
                    __instance.UpdatePredicateSignalOnChange<SignalAspect>(storage.ObserveSignalAspect, originalHeads.nextSignal.id);
            }
        }

        return false;
    }

    [HarmonyPatch(typeof(CTCSignal), "OnEnable")]
    [HarmonyReversePatch]
    public static void OnEnable_Base(CTCSignal instance)
    {
        throw new NotImplementedException("It's a stub");
    }

    [HarmonyPatch("CalculateAspect")]
    [HarmonyPrefix]
    public static bool CalculateAspect_Prefix(CTCPredicateSignal __instance, ref SignalAspect __result, ref int stopReason)
    {
        var extension = __instance.GetComponent<CTCPredicateSignalCrossoverExtension>();
        if (extension == null || extension.heads.Count == 0)
            return true;

        // If it's already not stop, we might need to downgrade it if crossover predicates are not satisfied.
        // But the original CalculateAspect already calculated based on its predicates.
        // We need to re-evaluate each head.
        
        SignalAspect head0 = SignalAspect.Stop;
        SignalAspect head1 = SignalAspect.Stop;
        SignalAspect head2 = SignalAspect.Stop;

        for (int i = 0; i < __instance.heads.Count; i++)
        {
            var head = __instance.heads[i];
            bool extensionSatisfied = extension.IsSatisfied(__instance, i);

            SignalAspect aspect = SignalAspect.Stop;
            if (extensionSatisfied)
            {
                aspect = (UnityEngine.Object) head.nextSignal == (UnityEngine.Object) null || !head.nextSignal.isActiveAndEnabled 
                    ? SignalAspect.Approach 
                    : (AspectDisplayedBySignal(__instance, head.nextSignal) != SignalAspect.Stop ? SignalAspect.Clear : SignalAspect.Approach);
            }

            if (i == 0) head0 = aspect;
            else if (i == 1) head1 = aspect;
            else if (i == 2) head2 = aspect;
        }

        __result = SignalAspectForHeads(head0, head1, head2);
        stopReason = 0; // CTCSignal.StopReason.None
        return false;
    }

    private static SignalAspect SignalAspectForHeads(
        SignalAspect head0,
        SignalAspect head1,
        SignalAspect head2)
    {
        if (head0 == SignalAspect.Approach)
            return SignalAspect.Approach;
        if (head0 == SignalAspect.Clear)
            return SignalAspect.Clear;
        if (head1 == SignalAspect.Approach)
            return SignalAspect.DivergingApproach;
        if (head1 == SignalAspect.Clear)
            return SignalAspect.DivergingClear;
        if (head2 == SignalAspect.Approach || head2 == SignalAspect.Clear)
            return SignalAspect.Restricting;
        return SignalAspect.Stop;
    }

    private static FastInvokeHandler AspectDisplayedBySignalInvoker = MethodInvoker.GetHandler(AccessTools.Method(typeof(CTCSignal), "AspectDisplayedBySignal", new[] { typeof(CTCSignal) }));

    private static SignalAspect AspectDisplayedBySignal(CTCPredicateSignal instance, CTCSignal signal)
    {
        return (SignalAspect)AspectDisplayedBySignalInvoker(instance, signal);
    }
}
