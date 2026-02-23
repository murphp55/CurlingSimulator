namespace CurlingSimulator.AI
{
    public enum ThrowIntent
    {
        Draw,       // place stone in the house (or behind a guard)
        Guard,      // place stone in front of the house to protect a draw
        Takeout,    // hit and remove an opponent stone
        Peel,       // aggressive hit-and-roll to remove a guard
        Freeze      // come to rest touching the team's lead stone in the house
    }
}
