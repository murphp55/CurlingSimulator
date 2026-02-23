using CurlingSimulator.Input;

namespace CurlingSimulator.AI
{
    /// <summary>
    /// Easy AI: always attempts a draw, high inaccuracy, never sweeps.
    /// </summary>
    public class EasyAIStrategy : BaseAIStrategy
    {
        protected override float AccuracyBias => 0.85f;
        protected override float SweepQuality => 0f;

        public override ThrowData CalculateThrow(SheetState sheetState, ThrowIntent intent)
            => base.CalculateThrow(sheetState, ThrowIntent.Draw);
    }
}
