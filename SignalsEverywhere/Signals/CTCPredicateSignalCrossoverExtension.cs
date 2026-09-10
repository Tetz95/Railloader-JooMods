using System;
using System.Collections.Generic;
using Track.Signals;
using UnityEngine;

namespace SignalsEverywhere.Signals;

public class CTCPredicateSignalCrossoverExtension : MonoBehaviour
{
    [Serializable]
    public class CrossoverPredicate
    {
        public CTCPredicateSignal.Predicate original;
        public string? crossoverGroupId;
    }

    [Serializable]
    public class HeadCrossoverPredicates
    {
        public List<CrossoverPredicate> predicates = new();
    }

    public List<HeadCrossoverPredicates> heads = new();

    public bool IsSatisfied(CTCPredicateSignal signal, int headIndex)
    {
        if (heads == null || headIndex < 0 || headIndex >= heads.Count)
            return true;
        
        var head = heads[headIndex];
        if (head == null || head.predicates == null)
            return true;

        CTCCrossover co = signal.GetComponentInParent<CTCCrossover>();
        if (co == null)
            return true;
        
        var storage = co.GetComponentInParent<SignalStorage>();
        if (storage == null)
            return true;
        
        foreach (var predicate in head.predicates)
        {
            if (predicate == null || predicate.original == null)
                continue;

            if (!IsSatisfied(storage, co, signal, predicate))
                return false;
        }
        return true;
    }

    private bool MatchSwitchSetting(CTCPredicateSignal.Predicate predicate)
    {
        if (predicate == null || predicate.switchNode == null)
            return true;
        return (predicate.switchNode.isThrown ? SwitchSetting.Reversed : SwitchSetting.Normal) ==
               predicate.switchSetting;
    }

    private bool IsSatisfied(SignalStorage storage, CTCCrossover co, CTCPredicateSignal signal, CrossoverPredicate predicate)
    {
        if (predicate == null || predicate.original == null)
            return true;

        switch (predicate.original.type)
        {
            case CTCPredicateSignal.PredicateType.Switch:
                return MatchSwitchSetting(predicate.original);
            case CTCPredicateSignal.PredicateType.Block:
                return predicate.original.blocks == null || predicate.original.blocks.TrueForAll(b => b != null && !b.IsOccupied);
            case CTCPredicateSignal.PredicateType.InterlockingTrafficDirection:
            {
                if (storage.SystemMode == SystemMode.ABS || predicate.crossoverGroupId == null) return true;
                var currentDirection = storage.GetCrossoverGroupDirection(predicate.crossoverGroupId);
                var expectedDirection = CTCCrossover.AsFilter(predicate.original.direction);
                return currentDirection == expectedDirection;
            }
            case CTCPredicateSignal.PredicateType.InterlockingTrafficDirectionIsNot:
            {
                if (storage.SystemMode == SystemMode.ABS || predicate.crossoverGroupId == null) return true;
                var currentDirection = storage.GetCrossoverGroupDirection(predicate.crossoverGroupId);
                var expectedDirection = CTCCrossover.AsFilter(predicate.original.direction);
                if (predicate.original.switchNode != null && !MatchSwitchSetting(predicate.original)) return false;
                return currentDirection != expectedDirection;
            }
            case CTCPredicateSignal.PredicateType.AlwaysFalse:
                return false;
            default:
                return true;
        }
    }
}
