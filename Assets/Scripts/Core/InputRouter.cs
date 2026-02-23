using UnityEngine;
using CurlingSimulator.Core;
using CurlingSimulator.Input;
using CurlingSimulator.AI;

namespace CurlingSimulator.Core
{
    /// <summary>
    /// Wires IInputProvider instances to GameManager at scene start.
    /// Reads MatchConfig from MainMenuController.PendingConfig (or falls back to a default).
    ///
    /// Assign in Inspector:
    ///   - PlayerInput: the PlayerInputProvider in the scene
    ///   - AIInput: the AIInputProvider in the scene
    ///   - GameManager reference (or it will find the singleton)
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        [SerializeField] private PlayerInputProvider _playerInput;
        [SerializeField] private AIInputProvider     _aiInput;

        private void Start()
        {
            var config = UI.MainMenuController.PendingConfig ?? new MatchConfig();
            var gm     = GameManager.Instance;

            if (gm == null)
            {
                Debug.LogError("[InputRouter] GameManager not found in scene.");
                return;
            }

            // Assign input providers: player controls their team, AI controls the other
            IInputProvider redInput    = config.PlayerTeam == TeamId.Red
                ? (IInputProvider)_playerInput : _aiInput;
            IInputProvider yellowInput = config.PlayerTeam == TeamId.Yellow
                ? (IInputProvider)_playerInput : _aiInput;

            gm.StartMatch(config, redInput, yellowInput);
        }
    }
}
