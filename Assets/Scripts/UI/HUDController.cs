using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CurlingSimulator.Core;
using CurlingSimulator.Input;

namespace CurlingSimulator.UI
{
    /// <summary>
    /// Central HUD controller. Subscribes to GameManager and PlayerInputProvider events
    /// to keep all on-screen indicators in sync.
    ///
    /// Requires TextMeshPro (com.unity.textmeshpro) to be installed.
    /// If you prefer UnityEngine.UI.Text, swap TMP_Text references for Text.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Throw phase")]
        [SerializeField] private GameObject   _throwPhaseRoot;
        [SerializeField] private Slider       _powerSlider;
        [SerializeField] private RectTransform _directionIndicator; // rotates to show aim angle
        [SerializeField] private TMP_Text     _curlLabel;           // "IN-TURN" / "OUT-TURN"
        [SerializeField] private TMP_Text     _throwCounterLabel;   // "End 3 | Throw 5/16"
        [SerializeField] private TMP_Text     _throwingTeamLabel;   // "RED" / "YELLOW"

        [Header("Sweep phase")]
        [SerializeField] private GameObject _sweepPhaseRoot;
        [SerializeField] private Slider     _sweepIntensitySlider;

        [Header("Scoreboard")]
        [SerializeField] private GameObject _scoreboardRoot;
        [SerializeField] private TMP_Text   _redTotalLabel;
        [SerializeField] private TMP_Text   _yellowTotalLabel;

        [Header("End scoring overlay")]
        [SerializeField] private GameObject _endScoringRoot;
        [SerializeField] private TMP_Text   _endScoringLabel;

        [Header("Game over screen")]
        [SerializeField] private GameObject _gameOverRoot;
        [SerializeField] private TMP_Text   _winnerLabel;

        // ── Private refs ──────────────────────────────────────────────────────────
        private PlayerInputProvider _playerInput;

        // ─────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
                GameManager.Instance.OnEndScored    += OnEndScored;
                GameManager.Instance.OnMatchOver    += OnMatchOver;
            }

            // Find the human player's input provider and subscribe to aim updates
            _playerInput = FindAnyObjectByType<PlayerInputProvider>();
            if (_playerInput != null)
                _playerInput.OnAimUpdated += OnAimUpdated;

            ShowThrowPhase(false);
            ShowSweepPhase(false);
            _endScoringRoot?.SetActive(false);
            _gameOverRoot?.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                GameManager.Instance.OnEndScored    -= OnEndScored;
                GameManager.Instance.OnMatchOver    -= OnMatchOver;
            }
            if (_playerInput != null)
                _playerInput.OnAimUpdated -= OnAimUpdated;
        }

        // ── Phase responses ───────────────────────────────────────────────────────

        private void OnPhaseChanged(GamePhase phase)
        {
            ShowThrowPhase(phase == GamePhase.ThrowAim);
            ShowSweepPhase(phase == GamePhase.StoneInMotion);

            if (phase == GamePhase.ThrowAim)
            {
                var match = GameManager.Instance.CurrentMatch;
                if (match == null) return;

                UpdateThrowCounter(match.CurrentEnd, match.CurrentThrowIndex + 1);
                SetThrowingTeamLabel(match.ThrowingTeam);
            }

            if (phase == GamePhase.EndScoring || phase == GamePhase.EndTransition)
                _endScoringRoot?.SetActive(true);
            else
                _endScoringRoot?.SetActive(false);
        }

        private void OnEndScored(EndScoreResult result)
        {
            // Update scoreboard totals
            var match = GameManager.Instance.CurrentMatch;
            if (match == null) return;

            if (_redTotalLabel != null)    _redTotalLabel.text    = match.TotalScore[0].ToString();
            if (_yellowTotalLabel != null) _yellowTotalLabel.text = match.TotalScore[1].ToString();

            // End scoring overlay
            if (_endScoringLabel != null)
            {
                string team   = result.ScoringTeam == TeamId.None ? "Blank End" :
                                result.ScoringTeam == TeamId.Red  ? "RED" : "YELLOW";
                string points = result.ScoringTeam == TeamId.None ? "" :
                                $" scores {result.PointsScored}";
                _endScoringLabel.text = $"{team}{points}";
            }
        }

        private void OnMatchOver(MatchState match)
        {
            _gameOverRoot?.SetActive(true);
            if (_winnerLabel == null) return;

            bool redWins    = match.TotalScore[0] > match.TotalScore[1];
            bool yellowWins = match.TotalScore[1] > match.TotalScore[0];
            _winnerLabel.text = redWins    ? "RED WINS!"
                              : yellowWins ? "YELLOW WINS!"
                              : "TIE!";
        }

        // ── Aim feedback ──────────────────────────────────────────────────────────

        private void OnAimUpdated(float power, float angle, CurlDirection curl)
        {
            if (_powerSlider != null)        _powerSlider.value        = power;
            if (_directionIndicator != null) _directionIndicator.localEulerAngles = new Vector3(0, 0, -angle);
            if (_curlLabel != null)          _curlLabel.text           = curl == CurlDirection.InTurn
                                                                            ? "IN-TURN" : "OUT-TURN";
        }

        private void OnSweepUpdated(SweepData data)
        {
            if (_sweepIntensitySlider != null)
                _sweepIntensitySlider.value = data.Intensity;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ShowThrowPhase(bool show)  => _throwPhaseRoot?.SetActive(show);
        private void ShowSweepPhase(bool show)  => _sweepPhaseRoot?.SetActive(show);

        private void UpdateThrowCounter(int end, int throwNumber)
        {
            if (_throwCounterLabel != null)
                _throwCounterLabel.text = $"End {end} | Throw {throwNumber} / 16";
        }

        private void SetThrowingTeamLabel(TeamId team)
        {
            if (_throwingTeamLabel == null) return;
            _throwingTeamLabel.text  = team == TeamId.Red ? "RED" : "YELLOW";
            _throwingTeamLabel.color = team == TeamId.Red
                ? new Color(1f, 0.3f, 0.3f)
                : new Color(1f, 0.9f, 0.2f);
        }
    }
}
