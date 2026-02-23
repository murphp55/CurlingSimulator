using System;
using System.Collections.Generic;
using CurlingSimulator.Core;

namespace CurlingSimulator.AI
{
    /// <summary>
    /// Read-only snapshot of the sheet passed to the AI each turn.
    /// Contains only the information the AI needs; GameManager owns the authoritative state.
    /// </summary>
    [Serializable]
    public struct SheetState
    {
        public List<StoneState> Stones;
        public int              StonesRemainingThisEnd; // total throws left in the end
        public TeamId           AITeam;
        public TeamId           OpponentTeam;
        public int              CurrentEnd;
        public int[]            CurrentScore;   // [0]=Red, [1]=Yellow
        public bool             AIHasHammer;
        public Vector2          ButtonCenter;
        public float            HouseRadius;
        public float            StoneRadius;
    }
}
