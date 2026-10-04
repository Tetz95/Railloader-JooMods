using System.Collections.Generic;
using HarmonyLib;
using Track.Signals;
using UnityEngine;

namespace SignalsEverywhere.Signals;

/// <summary>
/// Some built-in intermediates (TV-WH) sit on the CTC feature object instead of a module, so they can't be patched,
/// and they are a parent of every block in their feature. Two things go wrong when a patch adds an interlocking next
/// to one, or takes a block out of an intermediate:
/// - the intermediate still names the old interlocking's signal as its next signal, although that interlocking no
///   longer has the intermediate's end block as an outlet, so aspect checks on its signals throw;
/// - a block takes the intermediate above it as its own, and a block with an intermediate never carries a traffic
///   direction, so the new interlocking's signals clear without a route and nothing stops opposing moves.
/// After building, relink such intermediates and detach blocks the intermediate doesn't list. The base game has
/// neither case, so built-in signals are unaffected.
/// </summary>
public static class IntermediateRepair
{
    private static readonly Serilog.ILogger logger = Serilog.Log.ForContext(typeof(IntermediateRepair));

    public static void Apply()
    {
        var interlockings = Object.FindObjectsOfType<CTCInterlocking>(true);
        var signals = Object.FindObjectsOfType<CTCSignal>(true);
        foreach (var intermediate in Object.FindObjectsOfType<CTCIntermediate>(true))
        {
            Relink(intermediate, CTCDirection.Left, interlockings, signals);
            Relink(intermediate, CTCDirection.Right, interlockings, signals);
        }

        // Blocks in modules that were reused woke up before their intermediate was patched.
        foreach (var block in Object.FindObjectsOfType<CTCBlock>(true))
            Detach(block);
    }

    public static void Detach(CTCBlock block)
    {
        var intermediate = block.Intermediate;
        if (intermediate == null || intermediate.blocks == null || intermediate.blocks.Contains(block))
            return;

        AccessTools.PropertySetter(typeof(CTCBlock), nameof(CTCBlock.Intermediate)).Invoke(block, new object?[] { null });
        logger.Information($"Block {block.id} isn't one of intermediate {intermediate.name}'s blocks; it now carries its own traffic direction");
    }

    private static void Relink(CTCIntermediate intermediate, CTCDirection direction, CTCInterlocking[] interlockings, CTCSignal[] signals)
    {
        var next = direction == CTCDirection.Left ? intermediate.nextSignalLeft : intermediate.nextSignalRight;
        var end = intermediate.BlockAtEnd(direction);
        // Signals may not be active yet, so look up their interlocking the way they will.
        var current = next == null ? null : next.GetComponentInParent<CTCInterlocking>(true);
        if (end == null || current == null || HasOutlet(current, end))
            return;

        var found = new List<CTCSignal>();
        foreach (var interlocking in interlockings)
        {
            if (interlocking == current || !HasOutlet(interlocking, end))
                continue;
            foreach (var signal in signals)
            {
                if (signal.direction == direction && signal.GetComponentInParent<CTCInterlocking>(true) == interlocking)
                    found.Add(signal);
            }
        }

        if (found.Count != 1)
        {
            logger.Warning($"Intermediate {intermediate.name}: {direction} end block {end.id} isn't an outlet of {current.id} " +
                           $"(next signal {next!.id}); found {found.Count} replacement signals, leaving it");
            return;
        }

        if (direction == CTCDirection.Left)
            intermediate.nextSignalLeft = found[0];
        else
            intermediate.nextSignalRight = found[0];
        logger.Information($"Intermediate {intermediate.name}: {direction} next signal {next!.id} ({current.id}) -> {found[0].id}, " +
                           $"whose interlocking has end block {end.id} as an outlet");
    }

    private static bool HasOutlet(CTCInterlocking interlocking, CTCBlock block)
    {
        foreach (var outlet in interlocking.outlets)
        {
            if (outlet.blocks != null && outlet.blocks.Contains(block))
                return true;
        }
        return false;
    }
}
