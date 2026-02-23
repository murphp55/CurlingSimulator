using UnityEngine;
using CurlingSimulator.Simulation;

namespace CurlingSimulator.Visuals
{
    /// <summary>
    /// Procedurally builds all ice-sheet geometry and markings at runtime from StoneSimConfig values.
    ///
    /// Creates (as children of this GameObject):
    ///   - Ice plane (scaled Unity Plane primitive).
    ///   - House rings: 12-ft (red), 8-ft (white), 4-ft (blue), button (white).
    ///   - Centre line, tee line, back line, hog line.
    ///
    /// Sheet coordinate convention (matches StoneController / StoneSimConfig):
    ///   - Button (house centre) at world (0, 0, 0).
    ///   - Stones travel in the +Z direction (hack at negative Z).
    ///   - Back line at Z = BackLineDistance (negative value, behind the house).
    ///   - Hog line at Z = HogLineDistance (positive value, near the hack end).
    ///
    /// Assign materials in the Inspector; the ArtSetupWizard editor tool creates them for you.
    /// </summary>
    public class SheetRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Config (required)")]
        [SerializeField] private StoneSimConfig _config;

        [Header("Materials (assign from Assets/Materials/)")]
        [SerializeField] private Material _iceMaterial;
        [SerializeField] private Material _ring12Material;   // 12-ft outer ring — red
        [SerializeField] private Material _ring8Material;    // 8-ft ring          — white
        [SerializeField] private Material _ring4Material;    // 4-ft ring          — blue
        [SerializeField] private Material _buttonMaterial;   // centre button      — white
        [SerializeField] private Material _lineMaterial;     // all line markings  — white

        [Header("Geometry")]
        [Tooltip("Extra sheet length beyond the hog line toward the hack area.")]
        [SerializeField] private float _hackPadding = 3f;

        [Tooltip("Extra sheet length behind the back line.")]
        [SerializeField] private float _backPadding = 1f;

        // ── Constants ─────────────────────────────────────────────────────────────
        private const float MarkingY       = 0.005f;  // hover slightly above ice to avoid Z-fight
        private const float LineWidth      = 0.06f;
        private const float HogLineWidth   = 0.10f;   // hog line is thicker in real curling
        private const int   CircleSegments = 72;

        // House ring radii (metres): 12-ft, 8-ft, 4-ft, button (approx 1-ft)
        private static readonly float[] RingRadii = { 1.829f, 1.219f, 0.610f, 0.152f };

        // ─────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (_config == null)
            {
                Debug.LogWarning("[SheetRenderer] No StoneSimConfig assigned — sheet not built.", this);
                return;
            }

            BuildIcePlane();
            BuildHouseRings();
            BuildLines();
        }

        // ── Builders ──────────────────────────────────────────────────────────────

        private void BuildIcePlane()
        {
            float hw         = _config.SheetHalfWidth;
            float zMin       = _config.BackLineDistance - _backPadding;
            float zMax       = _config.HogLineDistance  + _hackPadding;
            float planeLen   = zMax - zMin;
            float planeCentZ = (zMin + zMax) * 0.5f;

            // Unity's default Plane is 10×10 units in XZ; scale to match sheet dimensions.
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "IcePlane";
            plane.transform.SetParent(transform, false);
            plane.transform.localPosition = new Vector3(0f, 0f, planeCentZ);
            plane.transform.localScale    = new Vector3(hw * 2f / 10f, 1f, planeLen / 10f);

            // Remove the default collider — physics is handled kinematically
            Destroy(plane.GetComponent<Collider>());

            if (_iceMaterial != null)
                plane.GetComponent<Renderer>().sharedMaterial = _iceMaterial;
        }

        private void BuildHouseRings()
        {
            Material[] mats = { _ring12Material, _ring8Material, _ring4Material, _buttonMaterial };

            // Draw outer rings first (back-to-front by radius, large to small)
            for (int i = 0; i < RingRadii.Length; i++)
                BuildCircle($"Ring_{i}", RingRadii[i], mats[i]);
        }

        private void BuildLines()
        {
            float hw  = _config.SheetHalfWidth;
            float bk  = _config.BackLineDistance;
            float hog = _config.HogLineDistance;

            // Centre line: full sheet length
            float lineZMin = bk - _backPadding;
            float lineZMax = hog + _hackPadding;
            BuildLine("CentreLine", new Vector3(0f, MarkingY, lineZMin),
                                    new Vector3(0f, MarkingY, lineZMax), LineWidth);

            // Tee line: horizontal line through the button (Z = 0)
            BuildLine("TeeLine",    new Vector3(-hw, MarkingY, 0f),
                                    new Vector3( hw, MarkingY, 0f),      LineWidth);

            // Back line: behind the house
            BuildLine("BackLine",   new Vector3(-hw, MarkingY, bk),
                                    new Vector3( hw, MarkingY, bk),      LineWidth);

            // Hog line: near the hack end — thicker in real curling
            BuildLine("HogLine",    new Vector3(-hw, MarkingY, hog),
                                    new Vector3( hw, MarkingY, hog),     HogLineWidth);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void BuildCircle(string goName, float radius, Material mat)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, MarkingY, 0f);

            var lr            = go.AddComponent<LineRenderer>();
            lr.useWorldSpace  = false;
            lr.loop           = true;
            lr.positionCount  = CircleSegments;
            lr.startWidth     = LineWidth * 2f;
            lr.endWidth       = LineWidth * 2f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (mat != null)
                lr.sharedMaterial = mat;

            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i / (float)CircleSegments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f,
                                              Mathf.Sin(angle) * radius));
            }
        }

        private void BuildLine(string goName, Vector3 start, Vector3 end, float width)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);

            var lr           = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth    = width;
            lr.endWidth      = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            if (_lineMaterial != null)
                lr.sharedMaterial = _lineMaterial;
        }
    }
}
