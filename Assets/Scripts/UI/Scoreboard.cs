using UnityEngine;
using TMPro;
using CurlingSimulator.Core;

namespace CurlingSimulator.UI
{
    /// <summary>
    /// Renders the 10-end scoreboard. Assign the 20 per-end text cells (10 Red, 10 Yellow)
    /// in the Inspector in order (end 1 first).
    /// Updated after each end via GameManager.OnEndScored.
    /// </summary>
    public class Scoreboard : MonoBehaviour
    {
        [Header("Per-end score cells (10 each, end 1 first)")]
        [SerializeField] private TMP_Text[] _redEndCells    = new TMP_Text[10];
        [SerializeField] private TMP_Text[] _yellowEndCells = new TMP_Text[10];

        [Header("Running totals")]
        [SerializeField] private TMP_Text _redTotalCell;
        [SerializeField] private TMP_Text _yellowTotalCell;

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnEndScored += OnEndScored;

            ClearAll();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnEndScored -= OnEndScored;
        }

        private void OnEndScored(EndScoreResult result)
        {
            int endIdx = result.EndNumber - 1;
            if (endIdx < 0 || endIdx >= 10) return;

            var match = GameManager.Instance.CurrentMatch;
            if (match == null) return;

            // Red end cell
            if (endIdx < _redEndCells.Length && _redEndCells[endIdx] != null)
                _redEndCells[endIdx].text = match.RedScoreByEnd[endIdx] > 0
                    ? match.RedScoreByEnd[endIdx].ToString() : "";

            // Yellow end cell
            if (endIdx < _yellowEndCells.Length && _yellowEndCells[endIdx] != null)
                _yellowEndCells[endIdx].text = match.YellowScoreByEnd[endIdx] > 0
                    ? match.YellowScoreByEnd[endIdx].ToString() : "";

            // Running totals
            if (_redTotalCell != null)    _redTotalCell.text    = match.TotalScore[0].ToString();
            if (_yellowTotalCell != null) _yellowTotalCell.text = match.TotalScore[1].ToString();
        }

        private void ClearAll()
        {
            foreach (var cell in _redEndCells)    if (cell != null) cell.text = "";
            foreach (var cell in _yellowEndCells) if (cell != null) cell.text = "";
            if (_redTotalCell != null)    _redTotalCell.text    = "0";
            if (_yellowTotalCell != null) _yellowTotalCell.text = "0";
        }
    }
}
