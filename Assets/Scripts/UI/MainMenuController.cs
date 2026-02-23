using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using CurlingSimulator.Core;

namespace CurlingSimulator.UI
{
    /// <summary>
    /// Simple main menu that collects player prefs and starts a match.
    /// Assumes the game scene is named "MainGame" in Build Settings.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _difficultyDropdown; // Easy/Medium/Hard
        [SerializeField] private TMP_Dropdown _teamDropdown;        // Red/Yellow

        // Persist config across scene load
        public static MatchConfig PendingConfig { get; private set; }

        public void OnPlayClicked()
        {
            PendingConfig = new MatchConfig
            {
                TotalEnds   = 10,
                PlayerTeam  = _teamDropdown != null && _teamDropdown.value == 1
                                  ? TeamId.Yellow : TeamId.Red,
                FirstHammer = TeamId.Red, // conventional opening
                Difficulty  = _difficultyDropdown != null
                                  ? (AIDifficulty)_difficultyDropdown.value
                                  : AIDifficulty.Medium
            };

            SceneManager.LoadScene("MainGame");
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
