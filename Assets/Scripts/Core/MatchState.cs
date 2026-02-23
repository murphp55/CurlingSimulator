using System;
using System.Collections.Generic;
using CurlingSimulator.Core;

namespace CurlingSimulator.Core
{
    [Serializable]
    public class MatchState
    {
        // 1-based end number (1–10)
        public int CurrentEnd = 1;

        // 0-based throw index within the current end (0–15, 16 throws per end)
        public int CurrentThrowIndex = 0;

        // Team that throws last in the end (last-stone advantage)
        public TeamId HammerTeam = TeamId.Red;

        // Team whose turn it is to throw right now
        public TeamId ThrowingTeam = TeamId.Yellow; // non-hammer throws first

        // [0] = Red total, [1] = Yellow total; index by (int)TeamId - 1
        public int[] TotalScore = new int[2];

        // Score per end: [end-1] = points scored (positive = Red, negative = Yellow)
        // Or store separately per team:
        public int[] RedScoreByEnd    = new int[10];
        public int[] YellowScoreByEnd = new int[10];

        public GamePhase Phase = GamePhase.MatchSetup;

        public List<StoneState> StonesInPlay = new List<StoneState>();

        // Convenience: which team is NOT the hammer team
        public TeamId NonHammerTeam => HammerTeam == TeamId.Red ? TeamId.Yellow : TeamId.Red;

        // True when all 16 throws in this end are done
        public bool IsEndComplete => CurrentThrowIndex >= 16;

        // True when all 10 ends are done
        public bool IsMatchComplete => CurrentEnd > 10 && IsEndComplete;
    }
}
