// This file is Editor-only. Unity ignores it in builds automatically because it
// lives in an "Editor" folder. No #if UNITY_EDITOR guard is needed.

using UnityEngine;
using UnityEditor;

namespace CurlingSimulator.Editor
{
    /// <summary>
    /// One-click tool that creates all URP materials needed for Stage 8 and prints
    /// a scene-setup checklist to the Console.
    ///
    /// Run via:  CurlingSimulator > Setup Art Materials
    /// </summary>
    public static class ArtSetupWizard
    {
        private const string Folder      = "Assets/Materials";
        private const string URPLit      = "Universal Render Pipeline/Lit";
        private const string URPUnlit    = "Universal Render Pipeline/Unlit";

        // ─────────────────────────────────────────────────────────────────────────

        [MenuItem("CurlingSimulator/Setup Art Materials")]
        public static void SetupMaterials()
        {
            EnsureFolder(Folder);

            // ── Ice ───────────────────────────────────────────────────────────────
            MakeLit("Ice_Mat", m =>
            {
                m.SetColor("_BaseColor",  new Color(0.84f, 0.92f, 1.00f));
                m.SetFloat("_Smoothness", 0.95f);
                m.SetFloat("_Metallic",   0.00f);
            });

            // ── Stones ────────────────────────────────────────────────────────────
            MakeLit("Stone_Red_Mat", m =>
            {
                m.SetColor("_BaseColor",     new Color(0.90f, 0.10f, 0.10f));
                m.SetFloat("_Smoothness",    0.85f);
                m.SetFloat("_Metallic",      0.10f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0.60f, 0.04f, 0.04f));
            });

            MakeLit("Stone_Yellow_Mat", m =>
            {
                m.SetColor("_BaseColor",     new Color(1.00f, 0.84f, 0.05f));
                m.SetFloat("_Smoothness",    0.85f);
                m.SetFloat("_Metallic",      0.10f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0.70f, 0.55f, 0.00f));
            });

            // ── House rings (Unlit so colours read strongly on ice) ───────────────
            MakeUnlit("Ring12_Mat", new Color(0.85f, 0.15f, 0.15f));  // red
            MakeUnlit("Ring8_Mat",  Color.white);                       // white
            MakeUnlit("Ring4_Mat",  new Color(0.20f, 0.40f, 0.90f));  // blue
            MakeUnlit("Button_Mat", Color.white);                       // white centre

            // ── Sheet lines ───────────────────────────────────────────────────────
            MakeUnlit("Line_Mat", new Color(0.95f, 0.95f, 0.95f));

            // ── Aim indicator lines (semi-transparent — enable Transparent mode) ──
            MakeUnlitTransparent("AimLine_Red_Mat",    new Color(1.00f, 0.30f, 0.30f, 0.75f));
            MakeUnlitTransparent("AimLine_Yellow_Mat", new Color(1.00f, 0.88f, 0.15f, 0.75f));

            // ── Impact / Sweep particles ──────────────────────────────────────────
            // Particle materials: Additive Unlit white so they glow on any background
            MakeUnlitAdditive("Particle_IceBurst_Mat",  new Color(0.85f, 0.95f, 1.00f));
            MakeUnlitAdditive("Particle_SweepChip_Mat", new Color(0.75f, 0.90f, 1.00f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PrintChecklist();
        }

        // ── Material factories ────────────────────────────────────────────────────

        private static void MakeLit(string name, System.Action<Material> configure)
            => CreateMat(name, URPLit, configure);

        private static void MakeUnlit(string name, Color color)
            => CreateMat(name, URPUnlit, m => m.SetColor("_BaseColor", color));

        private static void MakeUnlitTransparent(string name, Color color)
        {
            CreateMat(name, URPUnlit, m =>
            {
                m.SetColor("_BaseColor", color);
                // URP Unlit transparent surface
                m.SetFloat("_Surface", 1f);   // 0 = Opaque, 1 = Transparent
                m.SetFloat("_Blend",   0f);   // 0 = Alpha, 1 = Premultiply
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            });
        }

        private static void MakeUnlitAdditive(string name, Color color)
        {
            CreateMat(name, URPUnlit, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend",   3f);   // 3 = Additive in URP Unlit
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            });
        }

        private static void CreateMat(string name, string shaderName, System.Action<Material> configure)
        {
            string path = $"{Folder}/{name}.mat";

            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                Debug.Log($"[ArtSetupWizard] {name} already exists — skipped.");
                return;
            }

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[ArtSetupWizard] Shader not found: '{shaderName}'. " +
                                 "Make sure URP is installed via Package Manager.");
                return;
            }

            var mat = new Material(shader) { name = name };
            configure(mat);
            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"[ArtSetupWizard] Created {path}");
        }

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
                string child  = System.IO.Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, child);
                Debug.Log($"[ArtSetupWizard] Created folder {folder}");
            }
        }

        // ── Setup checklist ───────────────────────────────────────────────────────

        private static void PrintChecklist()
        {
            Debug.Log(
                "╔══════════════════════════════════════════════════════════════╗\n" +
                "║          CurlingSimulator — Art Setup Checklist              ║\n" +
                "╚══════════════════════════════════════════════════════════════╝\n\n" +

                "Materials created in Assets/Materials/.\n\n" +

                "── SHEET ───────────────────────────────────────────────────────\n" +
                "1. Create empty GameObject 'Sheet', add SheetRenderer component.\n" +
                "   Assign StoneSimConfig (ScriptableObject) and all ring/line mats:\n" +
                "     Ice_Mat, Ring12_Mat, Ring8_Mat, Ring4_Mat, Button_Mat, Line_Mat.\n\n" +

                "── STONE PREFABS ────────────────────────────────────────────────\n" +
                "2. Create 'RedStone' prefab:\n" +
                "   - Root: empty GO with StoneController, StoneVisuals, SweepFX, Rigidbody.\n" +
                "   - Child 'Mesh': Cylinder (scale 0.29, 0.15, 0.29) with Stone_Red_Mat.\n" +
                "     Assign this Renderer to StoneVisuals._stoneRenderer.\n" +
                "     Assign Root transform (or Mesh) to StoneVisuals._spinRoot.\n" +
                "   - Child 'Trail': TrailRenderer (width 0.12, time 0.6).\n" +
                "     Assign to StoneVisuals._trail. Use Stone_Red_Mat or additive mat.\n" +
                "   - Child 'SweepParticles': ParticleSystem.\n" +
                "     Shape: Box (0.3×0.05×0.3), Start Speed 0.3, Lifetime 0.4,\n" +
                "     Rate=0 (driven by SweepFX). Mat = Particle_SweepChip_Mat.\n" +
                "     Assign to SweepFX._particles.\n" +
                "   - Assign Stone_Red_Mat and Stone_Yellow_Mat to StoneVisuals.\n" +
                "3. Duplicate for 'YellowStone'; swap primary material to Stone_Yellow_Mat.\n" +
                "4. Assign both prefabs to StoneSimulator (_redStonePrefab / _yellowStonePrefab).\n\n" +

                "── HACKS ───────────────────────────────────────────────────────\n" +
                "5. Create two empty GameObjects:\n" +
                "     RedHack    at position (0, 0, -26)\n" +
                "     YellowHack at position (0, 0, -26)   [same side; alternate throws]\n" +
                "   Assign to StoneSimulator (_redHack / _yellowHack).\n\n" +

                "── CAMERAS ─────────────────────────────────────────────────────\n" +
                "6. Add CinemachineBrain to Main Camera.\n" +
                "7. Create 3 CinemachineCamera GameObjects:\n" +
                "     HackCam    — behind hack, e.g. pos (0, 2, -30), look at (0, 0, 0).\n" +
                "     SweeperCam — side follow, e.g. pos (4, 1.5, -10), look at active stone.\n" +
                "                  Set Follow and LookAt to a stone target (swap at launch).\n" +
                "     OverheadCam — top-down, e.g. pos (0, 18, 0), rot (90, 0, 0).\n" +
                "8. Assign all three to CameraDirector.\n\n" +

                "── EFFECTS ─────────────────────────────────────────────────────\n" +
                "9. Create empty GameObject 'ImpactFX', add ImpactFX component.\n" +
                "   Create a burst ParticleSystem prefab (Assets/Prefabs/ImpactBurst):\n" +
                "     Duration 0.1, burst count 25, start speed 1-3, size 0.05-0.1,\n" +
                "     Mat = Particle_IceBurst_Mat, Stop Action = Destroy.\n" +
                "   Assign prefab to ImpactFX._impactPrefab and StoneSimulator to _simulator.\n\n" +
                "10. Create empty GameObject 'AimIndicator', add AimIndicator component.\n" +
                "    - Child 'AimLine': LineRenderer, Mat = AimLine_Red_Mat (swap per team).\n" +
                "    - Child 'LandingMarker': flat disc/ring mesh, scale 0.3.\n" +
                "    - Assign _redHack, _yellowHack, PlayerInputProvider, StoneSimConfig.\n\n" +

                "── AUDIO ───────────────────────────────────────────────────────\n" +
                "11. Add AudioBridge component to AudioManager GameObject.\n" +
                "    Assign StoneSimulator and PlayerInputProvider.\n" +
                "    Import audio clips and assign in AudioManager:\n" +
                "      _throwReleaseClip, _sweepingLoopClip, _stoneCollisionClip,\n" +
                "      _stoneRestClip, _endScoreClips[]. Free sources: freesound.org\n\n" +

                "── POST PROCESSING ─────────────────────────────────────────────\n" +
                "12. Create a Global Volume:\n" +
                "    - Add Volume component, set Is Global = true.\n" +
                "    - New Profile: add Bloom (Intensity 1.2, Scatter 0.7, Threshold 0.9).\n" +
                "    - Add Color Adjustments (Saturation +15, Post Exposure +0.1).\n" +
                "13. In URP Asset (Project Settings > Graphics):\n" +
                "    - Enable HDR, Anti-aliasing (MSAA 4x or FXAA).\n" +
                "14. On Main Camera: enable Post Processing = true.\n\n" +

                "── LIGHTING ────────────────────────────────────────────────────\n" +
                "15. Directional Light: rotation (50, -30, 0), intensity 1.2, soft shadows.\n" +
                "16. Window > Rendering > Lighting > Environment:\n" +
                "    Ambient Source = Color, Ambient Color = #303848 (dark blue-grey).\n\n" +

                "All code is ready. Good luck building the scene!"
            );
        }
    }
}
