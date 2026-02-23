using UnityEngine;

namespace CurlingSimulator.Simulation
{
    /// <summary>
    /// All physics tuning constants for stone motion. Exposed as a ScriptableObject
    /// so values can be tweaked in the Inspector without recompiling.
    ///
    /// Create via: Assets > Create > CurlingSimulator > StoneSimConfig
    /// </summary>
    [CreateAssetMenu(menuName = "CurlingSimulator/StoneSimConfig", fileName = "StoneSimConfig")]
    public class StoneSimConfig : ScriptableObject
    {
        [Header("Deceleration")]
        [Tooltip("Speed lost per second at base (no sweeping). Tune so max-power stone travels ~28 m.")]
        public float BaseDecelerationRate = 0.018f;

        [Header("Curl")]
        [Tooltip("Lateral drift rate per unit of speed per second. Tune for ~0.3–0.5 m total curl.")]
        public float BaseCurlRate = 0.004f;

        [Header("Sweep Modifiers")]
        [Tooltip("Fraction by which full sweeping reduces deceleration. 0.35 = 35% less friction.")]
        [Range(0f, 1f)]
        public float SweepFrictionReduction = 0.35f;

        [Tooltip("Fraction by which full sweeping reduces curl. 0.60 = 60% less curl.")]
        [Range(0f, 1f)]
        public float SweepCurlReduction = 0.60f;

        [Header("Launch Speed")]
        [Tooltip("Stone speed (m/s) at power = 1.")]
        public float MaxLaunchSpeed = 4.5f;

        [Tooltip("Stone speed (m/s) at power = 0. Prevents dead-weight rolls.")]
        public float MinLaunchSpeed = 1.8f;

        [Header("Geometry")]
        [Tooltip("Stone radius in metres (real-world spec is 0.145 m).")]
        public float StoneRadius = 0.145f;

        [Header("Collision")]
        [Tooltip("Coefficient of restitution for stone-on-stone impacts (1 = perfectly elastic).")]
        [Range(0f, 1f)]
        public float CollisionRestitution = 0.85f;

        [Header("Stopping")]
        [Tooltip("Speed below this threshold (m/s) is treated as zero — stone comes to rest.")]
        public float MinSpeedThreshold = 0.05f;

        [Header("Sheet Boundaries (sheet-local Z, metres from button)")]
        [Tooltip("Hog line distance from the button. Stone must clear this to be in play.")]
        public float HogLineDistance = 21.94f;

        [Tooltip("Back line distance from the button. Stone past this is out of play.")]
        public float BackLineDistance = -1.83f;

        [Tooltip("Half-width of the sheet. Stone past this side-boundary is out of play.")]
        public float SheetHalfWidth = 2.375f;

        [Header("House")]
        [Tooltip("Outer radius of the house (12-foot ring). Used for scoring boundary.")]
        public float HouseRadius = 1.829f; // 6 feet
    }
}
