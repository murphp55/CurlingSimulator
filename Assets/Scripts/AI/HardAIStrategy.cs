using CurlingSimulator.Input;

namespace CurlingSimulator.AI
{
    /// <summary>
    /// Hard AI: full intent selection via AIDirector, near-perfect accuracy, optimal sweeping.
    /// </summary>
    public class HardAIStrategy : BaseAIStrategy
    {
        protected override float AccuracyBias => 0.10f;
        protected override float SweepQuality => 1.00f;

        private readonly AIDirector _director = new AIDirector();

        public override ThrowData CalculateThrow(SheetState sheetState, ThrowIntent _)
        {
            ThrowIntent intent = _director.SelectIntent(sheetState);
            return base.CalculateThrow(sheetState, intent);
        }
    }
}
