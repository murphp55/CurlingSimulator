using System.Collections.Generic;
using UnityEngine;
using CurlingSimulator.Core;

namespace CurlingSimulator.Simulation
{
    /// <summary>
    /// Pure static scoring logic. No MonoBehaviour, no Unity lifecycle dependency.
    /// Safe to unit-test outside of play mode.
    ///
    /// Rules:
    ///   - Only one team scores per end.
    ///   - The scoring team is the one whose stone is closest to the button.
    ///   - That team scores 1 point for each of its stones that is closer to the button
    ///     than the opponent's nearest stone.
    ///   - A stone must be within (houseRadius + stoneRadius) of the button to count.
    ///   - A blank end (no stones in the house) scores 0 for both teams.
    /// </summary>
    public static class ScoringSystem
    {
        /// <summary>
        /// Calculates the end result given the final stone positions.
        /// buttonCenter is in sheet-local XZ coordinates (typically Vector2.zero).
        /// </summary>
        public static EndScoreResult CalculateEndScore(
            IEnumerable<StoneState> finalStones,
            Vector2                 buttonCenter,
            float                   houseRadius,
            float                   stoneRadius)
        {
            float scoringRadius = houseRadius + stoneRadius;

            // Collect distances for each team's stones that are in the house
            var redDistances    = new List<(float dist, StoneState stone)>();
            var yellowDistances = new List<(float dist, StoneState stone)>();

            foreach (var stone in finalStones)
            {
                if (!stone.IsInPlay) continue;

                float dist = Vector2.Distance(stone.Position, buttonCenter);
                if (dist > scoringRadius) continue;

                if (stone.Owner == TeamId.Red)
                    redDistances.Add((dist, stone));
                else if (stone.Owner == TeamId.Yellow)
                    yellowDistances.Add((dist, stone));
            }

            // Sort ascending by distance
            redDistances.Sort((a, b) => a.dist.CompareTo(b.dist));
            yellowDistances.Sort((a, b) => a.dist.CompareTo(b.dist));

            var result = new EndScoreResult();

            // Blank end
            if (redDistances.Count == 0 && yellowDistances.Count == 0)
            {
                result.ScoringTeam  = TeamId.None;
                result.PointsScored = 0;
                return result;
            }

            // Red scores (or Yellow has no stones in house)
            if (yellowDistances.Count == 0 ||
                (redDistances.Count > 0 && redDistances[0].dist < yellowDistances[0].dist))
            {
                result.ScoringTeam         = TeamId.Red;
                result.ClosestScoringStone = redDistances[0].stone;

                float opponentNearest = yellowDistances.Count > 0
                    ? yellowDistances[0].dist
                    : float.MaxValue;

                int points = 0;
                foreach (var (d, _) in redDistances)
                {
                    if (d < opponentNearest) points++;
                    else break;
                }
                result.PointsScored = points;
            }
            // Yellow scores
            else
            {
                result.ScoringTeam         = TeamId.Yellow;
                result.ClosestScoringStone = yellowDistances[0].stone;

                float opponentNearest = redDistances.Count > 0
                    ? redDistances[0].dist
                    : float.MaxValue;

                int points = 0;
                foreach (var (d, _) in yellowDistances)
                {
                    if (d < opponentNearest) points++;
                    else break;
                }
                result.PointsScored = points;
            }

            return result;
        }

        /// <summary>Distance from a stone's centre to the button.</summary>
        public static float DistanceToButton(Vector2 stonePosition, Vector2 buttonCenter)
            => Vector2.Distance(stonePosition, buttonCenter);

        /// <summary>
        /// True if any part of the stone overlaps the outer edge of the house.
        /// A stone only needs to touch the outer ring to be counted (real rules).
        /// </summary>
        public static bool IsStoneInHouse(Vector2 stonePosition, Vector2 buttonCenter,
                                           float houseRadius, float stoneRadius)
            => Vector2.Distance(stonePosition, buttonCenter) <= houseRadius + stoneRadius;
    }
}
