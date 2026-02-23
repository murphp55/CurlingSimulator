using System;
using CurlingSimulator.Core;

namespace CurlingSimulator.Core
{
    [Serializable]
    public class MatchConfig
    {
        public int      TotalEnds       = 10;
        public TeamId   PlayerTeam      = TeamId.Red;
        public TeamId   FirstHammer     = TeamId.Red;   // who has hammer in end 1
        public AIDifficulty Difficulty  = AIDifficulty.Medium;
    }

    public enum AIDifficulty
    {
        Easy,
        Medium,
        Hard
    }
}
