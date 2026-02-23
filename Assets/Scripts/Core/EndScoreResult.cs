using System;
using System.Collections.Generic;

namespace CurlingSimulator.Core
{
    [Serializable]
    public class EndScoreResult
    {
        public int    EndNumber;
        public TeamId ScoringTeam;   // TeamId.None if blank end
        public int    PointsScored;
        public List<StoneState> FinalStonePositions = new List<StoneState>();

        // The stone closest to the button for the scoring team (null if blank)
        public StoneState? ClosestScoringStone;
    }
}
