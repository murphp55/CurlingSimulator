using CurlingSimulator.Input;

namespace CurlingSimulator.AI
{
    /// <summary>
    /// Medium AI: uses basic intent selection (Draw or Takeout), moderate accuracy, light sweeping.
    /// </summary>
    public class MediumAIStrategy : BaseAIStrategy
    {
        protected override float AccuracyBias => 0.40f;
        protected override float SweepQuality => 0.50f;

        private readonly AIDirector _director = new AIDirector();

        public override ThrowData CalculateThrow(SheetState sheetState, ThrowIntent _)
        {
            ThrowIntent intent = _director.SelectIntent(sheetState);
            // Medium only uses Draw or Takeout — cap complex intents
            if (intent == ThrowIntent.Peel || intent == ThrowIntent.Freeze || intent == ThrowIntent.Guard)
                intent = ThrowIntent.Draw;

            return base.CalculateThrow(sheetState, intent);
        }
    }
}
