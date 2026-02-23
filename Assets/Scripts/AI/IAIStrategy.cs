using CurlingSimulator.Core;
using CurlingSimulator.Input;

namespace CurlingSimulator.AI
{
    /// <summary>
    /// Strategy interface for AI throw and sweep decisions.
    /// All difficulty tiers implement this; AIInputProvider uses only this interface.
    /// </summary>
    public interface IAIStrategy
    {
        /// <summary>Computes a ThrowData for the given sheet situation and intent.</summary>
        ThrowData CalculateThrow(SheetState sheetState, ThrowIntent intent);

        /// <summary>Returns per-frame sweep intensity 0–1 while a stone is in motion.</summary>
        SweepData CalculateSweep(StoneState movingStone, SheetState sheetState);
    }
}
