using UnityEngine;
using CurlingSimulator.Core;

#if UNITY_CINEMACHINE
using Unity.Cinemachine;
#endif

namespace CurlingSimulator.Camera
{
    /// <summary>
    /// Reacts to GamePhase changes and activates the appropriate Cinemachine virtual camera.
    ///
    /// Setup:
    ///   - Attach to a GameObject in the scene along with a CinemachineBrain on Main Camera.
    ///   - Assign the three virtual cameras in the Inspector.
    ///   - Subscribe to GameManager.OnPhaseChanged.
    ///
    /// Priority scheme: active cam = 20, inactive cams = 0.
    /// Cinemachine blends automatically using the CinemachineBrain blend profile.
    /// </summary>
    public class CameraDirector : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Virtual Cameras")]
        [Tooltip("Behind-the-hack camera used during the throw aim phase.")]
        [SerializeField] private GameObject _hackCam;

        [Tooltip("Follows the stone from the side/front during motion.")]
        [SerializeField] private GameObject _sweeperCam;

        [Tooltip("Overhead / broadcast view for scoring and transitions.")]
        [SerializeField] private GameObject _overheadCam;

        [Header("Sweeper Cam Target")]
        [Tooltip("The transform the sweeper cam tracks. Reassign to the active stone at launch.")]
        [SerializeField] private Transform _sweeperLookAt;

        private const int ActivePriority   = 20;
        private const int InactivePriority = 0;

        // ─────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            // Subscribe to game manager once it exists
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }

        public void SetSweeperTarget(Transform target)
        {
            _sweeperLookAt = target;
            // If using Cinemachine, reassign LookAt/Follow here
        }

        // ── Phase handler ─────────────────────────────────────────────────────────

        private void OnPhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.ThrowAim:
                case GamePhase.ThrowRelease:
                    ActivateCam(_hackCam);
                    break;

                case GamePhase.StoneInMotion:
                    ActivateCam(_sweeperCam);
                    break;

                case GamePhase.EndScoring:
                case GamePhase.EndTransition:
                case GamePhase.MatchOver:
                    ActivateCam(_overheadCam);
                    break;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Raises the priority of the target cam and lowers all others.
        /// Cinemachine's brain will blend to the highest-priority active cam.
        /// </summary>
        private void ActivateCam(GameObject targetCam)
        {
            SetPriority(_hackCam,     targetCam == _hackCam     ? ActivePriority : InactivePriority);
            SetPriority(_sweeperCam,  targetCam == _sweeperCam  ? ActivePriority : InactivePriority);
            SetPriority(_overheadCam, targetCam == _overheadCam ? ActivePriority : InactivePriority);
        }

        private static void SetPriority(GameObject camGo, int priority)
        {
            if (camGo == null) return;

#if UNITY_CINEMACHINE
            var vcam = camGo.GetComponent<CinemachineCamera>();
            if (vcam != null) vcam.Priority = priority;
#else
            // Without Cinemachine: simple enable/disable
            camGo.SetActive(priority == ActivePriority);
#endif
        }
    }
}
