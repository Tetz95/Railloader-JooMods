using System.Collections.Generic;
using System.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using Track;
using Track.Signals;
using UnityEngine;
using UnityEngine.Assertions;

namespace SignalsEverywhere.Signals;

public class SerializedCTCPredicateSignal : SerializedCTCSignal
{
    public struct HeadPredicates
    {
        public List<Predicate> Predicates { get; set; }
        public string? NextCtcSignal { get; set; }
        
        public HeadPredicates() {}

        public HeadPredicates(CTCPredicateSignal.HeadPredicates predicates)
        {
            Predicates = predicates.predicates.Select(p => new Predicate(p)).ToList();
            NextCtcSignal = predicates.nextSignal?.id;
        }
        
        public CTCPredicateSignal.HeadPredicates Apply(CTCPatchingContext ctx, CTCPredicateSignalCrossoverExtension.HeadCrossoverPredicates? extensionHeads)
        {
            CTCPredicateSignal.HeadPredicates headPredicates = new();
            headPredicates.predicates = new();
            foreach (var predicate in Predicates)
            {
                headPredicates.predicates.Add(predicate.Apply(ctx, extensionHeads));
            }
            headPredicates.nextSignal = ctx.GetSignal(NextCtcSignal);
            return headPredicates;
        }
    }

    public struct Predicate
    {
        public CTCPredicateSignal.PredicateType Type { get; set; }
        public string? SwitchNode { get; set; }
        public SwitchSetting? SwitchSetting { get; set; }
        public List<string>? Blocks { get; set; }
        public string? Interlocking { get; set; }
        public SignalDirection? Direction { get; set; }
        public string? CrossoverGroup { get; set; }
        
        public Predicate() {}

        public Predicate(CTCPredicateSignal.Predicate predicate)
        {
            Type = predicate.type;
            SwitchNode = predicate.switchNode?.id;
            SwitchSetting = predicate.switchSetting;
            Blocks = predicate.blocks?.Select(b => b.id).ToList();
            Interlocking = predicate.interlocking?.id;
            Direction = predicate.direction;
        }

        public CTCPredicateSignal.Predicate? Apply(CTCPatchingContext ctx, CTCPredicateSignalCrossoverExtension.HeadCrossoverPredicates? extensionHeads)
        {
            CTCPredicateSignal.Predicate predicate = new CTCPredicateSignal.Predicate();
            predicate.type = Type;
            predicate.switchNode = SwitchNode != null ? Graph.Shared.GetNode(SwitchNode) : null;
            predicate.switchSetting = SwitchSetting ?? Track.Signals.SwitchSetting.Normal;
            predicate.blocks = ctx.GetBlocks(Blocks);
            predicate.interlocking = Interlocking != null ? ctx.Interlockings[Interlocking] : null;
            predicate.direction = Direction ?? SignalDirection.Left;
            
            if (extensionHeads != null)
            {
                extensionHeads.predicates.Add(new CTCPredicateSignalCrossoverExtension.CrossoverPredicate
                {
                    original = predicate,
                    crossoverGroupId = CrossoverGroup ?? ""
                });
            }
            
            return predicate;
        }
    }
    
    public List<HeadPredicates> Heads { get; set; }

    public SerializedCTCPredicateSignal() {}
    
    public SerializedCTCPredicateSignal(CTCPredicateSignal signal) : base(signal)
    {
        Heads = signal.heads.Select(h => new HeadPredicates(h)).ToList();
    }

    public void CreateFor(GameObject parent, CTCPatchingContext ctx)
    {
        GameObject prefab = SignalPrefabStore.Shared.GetSignalType(ModelType, HeadConfiguration);
        CTCPredicateSignal signal = Object.Instantiate(prefab, parent.transform, false).AddComponent<CTCPredicateSignal>();

        if (!signal)
        {
            throw new SCPatchingException("Unable to instantiate new signal");
        }
        base.ApplyTo(signal, ctx);
        ApplyTo(signal, ctx);
        ctx.PredicateSignals[signal.id] = signal;
        signal.gameObject.SetActive(true);
    }

    public void ApplyTo(CTCPredicateSignal signal, CTCPatchingContext ctx)
    {
        var coPresent = signal.GetComponentInParent<CTCCrossover>(true) != null;
        var extension = signal.gameObject.GetComponent<CTCPredicateSignalCrossoverExtension>();
        if (coPresent && extension == null)
        {
            Log.Information("SerializedCTCPredicateSignal: ApplyTo with crossover");
            extension = signal.gameObject.AddComponent<CTCPredicateSignalCrossoverExtension>();
        }
        extension?.heads.Clear();
        
        signal.heads = new();
        foreach (var head in Heads)
        {
            if (extension != null)
            {
                var extensionHeads = new CTCPredicateSignalCrossoverExtension.HeadCrossoverPredicates();
                extension.heads.Add(extensionHeads);
                signal.heads.Add(head.Apply(ctx, extensionHeads));
            }
            else
            {
                signal.heads.Add(head.Apply(ctx, null));
            }
        }
    }
}