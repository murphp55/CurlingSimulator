// Editor-only: lives in an Assets/Editor folder so Unity strips it from builds.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using CurlingSimulator.Core;
using CurlingSimulator.Input;
using CurlingSimulator.Simulation;
using CurlingSimulator.AI;
using CurlingSimulator.Audio;
using CurlingSimulator.Visuals;
using CurlingSimulator.UI;

#if UNITY_CINEMACHINE
using Unity.Cinemachine;
#endif

namespace CurlingSimulator.Editor
{
    /// <summary>
    /// One-click scene builder. Run via:  CurlingSimulator > Build Game Scene
    /// Or in batch mode: -executeMethod CurlingSimulator.Editor.SceneBuilder.BuildScene
    ///
    /// Creates:
    ///   Assets/Settings/StoneSimConfig.asset
    ///   Assets/Settings/PostProcessProfile.asset  (Bloom + Color Adjustments)
    ///   Assets/Prefabs/RedStone.prefab
    ///   Assets/Prefabs/YellowStone.prefab
    ///   Assets/Prefabs/ImpactBurst.prefab
    ///   Assets/Scenes/MainGame.unity   (fully wired, press Play immediately)
    ///   Assets/Scenes/MainMenu.unity   (basic menu scene)
    ///   Adds both scenes to Build Settings.
    /// </summary>
    public static class SceneBuilder
    {
        private const string PrefabsFolder   = "Assets/Prefabs";
        private const string ScenesFolder    = "Assets/Scenes";
        private const string SettingsFolder  = "Assets/Settings";
        private const string MaterialsFolder = "Assets/Materials";

        // ── Entry point ───────────────────────────────────────────────────────────

        [MenuItem("CurlingSimulator/Build Game Scene")]
        public static void BuildScene()
        {
            try
            {
                Log("=== SceneBuilder starting ===");

                EnsureFolders();

                // Materials first (ArtSetupWizard creates them)
                ArtSetupWizard.SetupMaterials();

                var config        = CreateStoneSimConfig();
                var stoneMats     = LoadStoneMaterials();
                var sheetMats     = LoadSheetMaterials();
                var impactPrefab  = CreateImpactBurstPrefab();
                var redPrefab     = CreateStonePrefab("RedStone",    config, stoneMats.red,    stoneMats.yellow, impactPrefab);
                var yellowPrefab  = CreateStonePrefab("YellowStone", config, stoneMats.yellow, stoneMats.red,    impactPrefab);

                BuildMainGameScene(config, redPrefab, yellowPrefab, impactPrefab, sheetMats, stoneMats);
                BuildMainMenuScene();
                SetupBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Log("=== SceneBuilder complete! Open Assets/Scenes/MainGame.unity and press Play. ===");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneBuilder] Failed: {ex}");
            }
        }

        // ── Folders ───────────────────────────────────────────────────────────────

        static void EnsureFolders()
        {
            EnsureFolder(SettingsFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(ScenesFolder);
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string child  = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        // ── StoneSimConfig ────────────────────────────────────────────────────────

        static StoneSimConfig CreateStoneSimConfig()
        {
            const string path = SettingsFolder + "/StoneSimConfig.asset";
            var existing = AssetDatabase.LoadAssetAtPath<StoneSimConfig>(path);
            if (existing != null) { Log("StoneSimConfig already exists — reusing."); return existing; }

            var cfg = ScriptableObject.CreateInstance<StoneSimConfig>();
            // Values are the defaults from StoneSimConfig.cs; explicit here for clarity.
            cfg.BaseDecelerationRate    = 0.018f;
            cfg.BaseCurlRate            = 0.004f;
            cfg.SweepFrictionReduction  = 0.35f;
            cfg.SweepCurlReduction      = 0.60f;
            cfg.MaxLaunchSpeed          = 4.5f;
            cfg.MinLaunchSpeed          = 1.8f;
            cfg.StoneRadius             = 0.145f;
            cfg.CollisionRestitution    = 0.85f;
            cfg.MinSpeedThreshold       = 0.05f;
            cfg.HogLineDistance         = 21.94f;
            cfg.BackLineDistance        = -1.83f;
            cfg.SheetHalfWidth          = 2.375f;
            cfg.HouseRadius             = 1.829f;

            AssetDatabase.CreateAsset(cfg, path);
            Log($"Created {path}");
            return cfg;
        }

        // ── Material loaders ──────────────────────────────────────────────────────

        static (Material red, Material yellow) LoadStoneMaterials()
        {
            var red    = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Stone_Red_Mat.mat");
            var yellow = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Stone_Yellow_Mat.mat");
            if (red    == null) LogWarn("Stone_Red_Mat not found — run Setup Art Materials first.");
            if (yellow == null) LogWarn("Stone_Yellow_Mat not found — run Setup Art Materials first.");
            return (red, yellow);
        }

        static (Material ice, Material ring12, Material ring8, Material ring4,
                Material button, Material line) LoadSheetMaterials()
        {
            Material Load(string name) =>
                AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{name}.mat");
            return (Load("Ice_Mat"), Load("Ring12_Mat"), Load("Ring8_Mat"),
                    Load("Ring4_Mat"), Load("Button_Mat"), Load("Line_Mat"));
        }

        // ── ImpactBurst prefab ────────────────────────────────────────────────────

        static GameObject CreateImpactBurstPrefab()
        {
            const string path = PrefabsFolder + "/ImpactBurst.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { Log("ImpactBurst prefab already exists — reusing."); return existing; }

            var go = new GameObject("ImpactBurst");
            var ps = go.AddComponent<ParticleSystem>();

            var main                  = ps.main;
            main.duration             = 0.15f;
            main.loop                 = false;
            main.startLifetime        = 0.5f;
            main.startSpeed           = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize            = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor           = new Color(0.85f, 0.95f, 1f, 1f);
            main.gravityModifier      = 0.3f;
            main.stopAction           = ParticleSystemStopAction.Destroy;
            main.maxParticles         = 40;

            var emission              = ps.emission;
            emission.rateOverTime     = 0f;
            var burst = new ParticleSystem.Burst(0f, 30);
            emission.SetBursts(new[] { burst });

            var shape             = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Sphere;
            shape.radius          = 0.05f;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Particle_IceBurst_Mat.mat");
            if (mat != null)
            {
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = mat;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Log($"Created {path}");
            return prefab;
        }

        // ── Stone prefabs ─────────────────────────────────────────────────────────

        static GameObject CreateStonePrefab(string stoneName, StoneSimConfig config,
                                            Material primaryMat, Material secondaryMat,
                                            GameObject impactPrefab)
        {
            string path = $"{PrefabsFolder}/{stoneName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { Log($"{stoneName} prefab already exists — reusing."); return existing; }

            // ── Root ──────────────────────────────────────────────────────────────
            var root = new GameObject(stoneName);

            // Rigidbody (required by StoneController)
            var rb               = root.AddComponent<Rigidbody>();
            rb.isKinematic       = true;
            rb.interpolation     = RigidbodyInterpolation.Interpolate;
            rb.useGravity        = false;

            root.AddComponent<StoneController>();

            var stoneVisuals = root.AddComponent<StoneVisuals>();
            var sweepFX      = root.AddComponent<SweepFX>();

            // ── Mesh child (visual cylinder) ──────────────────────────────────────
            var meshGo     = new GameObject("Mesh");
            meshGo.transform.SetParent(root.transform, false);
            // Grab the built-in cylinder mesh via a temp primitive
            var tempCyl    = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var cylMesh    = tempCyl.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempCyl);

            var mf         = meshGo.AddComponent<MeshFilter>();
            mf.sharedMesh  = cylMesh;
            var mr         = meshGo.AddComponent<MeshRenderer>();
            // Scale: diameter 0.29 m, height 0.15 m (cylinder default = 2 units tall, 1 unit wide)
            meshGo.transform.localScale = new Vector3(0.29f, 0.075f, 0.29f);

            if (primaryMat != null) mr.sharedMaterial = primaryMat;

            // ── Trail child ───────────────────────────────────────────────────────
            var trailGo   = new GameObject("Trail");
            trailGo.transform.SetParent(root.transform, false);
            var trail     = trailGo.AddComponent<TrailRenderer>();
            trail.time    = 0.8f;
            trail.startWidth = 0.14f;
            trail.endWidth   = 0.0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
            if (primaryMat != null)
            {
                // Use an unlit mat for the trail if available, else the stone mat
                var trailMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Line_Mat.mat");
                trail.sharedMaterial = trailMat != null ? trailMat : primaryMat;
            }

            // ── Sweep particles child ─────────────────────────────────────────────
            var particleGo = new GameObject("SweepParticles");
            particleGo.transform.SetParent(root.transform, false);
            particleGo.transform.localPosition = Vector3.zero;
            var ps         = particleGo.AddComponent<ParticleSystem>();

            var mainMod              = ps.main;
            mainMod.loop             = true;
            mainMod.startLifetime    = 0.4f;
            mainMod.startSpeed       = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            mainMod.startSize        = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            mainMod.startColor       = new Color(0.75f, 0.90f, 1f, 0.9f);
            mainMod.maxParticles     = 200;
            mainMod.simulationSpace  = ParticleSystemSimulationSpace.World;

            var emissionMod          = ps.emission;
            emissionMod.rateOverTime = 0f;   // controlled by SweepFX

            var shapeMod     = ps.shape;
            shapeMod.enabled = true;
            shapeMod.shapeType = ParticleSystemShapeType.Box;
            shapeMod.scale   = new Vector3(0.25f, 0.02f, 0.25f);

            var sweepChipMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Particle_SweepChip_Mat.mat");
            if (sweepChipMat != null)
                particleGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = sweepChipMat;

            // ── Wire StoneVisuals refs ────────────────────────────────────────────
            SetRef(stoneVisuals, "_redMaterial",    primaryMat);
            SetRef(stoneVisuals, "_yellowMaterial", secondaryMat);
            SetRef(stoneVisuals, "_stoneRenderer",  mr);
            SetRef(stoneVisuals, "_spinRoot",       meshGo.transform);
            SetRef(stoneVisuals, "_trail",          trail);

            // ── Wire SweepFX ref ──────────────────────────────────────────────────
            SetRef(sweepFX, "_particles", ps);

            // ── Save prefab ───────────────────────────────────────────────────────
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Log($"Created {path}");
            return prefab;
        }

        // ── Main game scene ───────────────────────────────────────────────────────

        static void BuildMainGameScene(
            StoneSimConfig config,
            GameObject redPrefab, GameObject yellowPrefab,
            GameObject impactPrefab,
            (Material ice, Material ring12, Material ring8, Material ring4,
             Material button, Material line) sheetMats,
            (Material red, Material yellow) stoneMats)
        {
            const string scenePath = ScenesFolder + "/MainGame.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Lighting ──────────────────────────────────────────────────────────
            var sunGo    = new GameObject("Directional Light");
            var sunLight = sunGo.AddComponent<Light>();
            sunLight.type      = LightType.Directional;
            sunLight.intensity = 1.2f;
            sunLight.shadows   = LightShadows.Soft;
            sunLight.color     = new Color(1f, 0.96f, 0.88f);
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.188f, 0.220f, 0.282f);
            RenderSettings.fog          = false;

            // ── Main Camera ───────────────────────────────────────────────────────
            var camGo  = new GameObject("Main Camera");
            var cam    = camGo.AddComponent<UnityEngine.Camera>();
            cam.tag    = "MainCamera";
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            cam.nearClipPlane   = 0.1f;
            cam.farClipPlane    = 200f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 18f, -5f);
            camGo.transform.rotation = Quaternion.Euler(75f, 0f, 0f);

#if UNITY_CINEMACHINE
            camGo.AddComponent<CinemachineBrain>();
#endif

            // ── Post-processing Global Volume ─────────────────────────────────────
            SetupGlobalVolume();

            // ── Sheet ─────────────────────────────────────────────────────────────
            var sheetGo = new GameObject("Sheet");
            var sheetRend = sheetGo.AddComponent<SheetRenderer>();
            SetRef(sheetRend, "_config",       config);
            SetRef(sheetRend, "_iceMaterial",  sheetMats.ice);
            SetRef(sheetRend, "_ring12Material",sheetMats.ring12);
            SetRef(sheetRend, "_ring8Material", sheetMats.ring8);
            SetRef(sheetRend, "_ring4Material", sheetMats.ring4);
            SetRef(sheetRend, "_buttonMaterial",sheetMats.button);
            SetRef(sheetRend, "_lineMaterial",  sheetMats.line);

            // ── Hacks ─────────────────────────────────────────────────────────────
            // Both teams throw in +Z direction from behind Z=0.
            // Red hack slightly left of centre, yellow slightly right (real curling convention).
            var redHack    = new GameObject("RedHack");
            var yellowHack = new GameObject("YellowHack");
            redHack.transform.position    = new Vector3(-0.23f, 0f, -24.5f);
            yellowHack.transform.position = new Vector3( 0.23f, 0f, -24.5f);

            // ── StoneSimulator ────────────────────────────────────────────────────
            var simGo  = new GameObject("StoneSimulator");
            var sim    = simGo.AddComponent<StoneSimulator>();
            SetRef(sim, "_config",           config);
            SetRef(sim, "_redStonePrefab",   redPrefab);
            SetRef(sim, "_yellowStonePrefab",yellowPrefab);
            SetRef(sim, "_redHack",          redHack.transform);
            SetRef(sim, "_yellowHack",       yellowHack.transform);

            // ── GameManager ───────────────────────────────────────────────────────
            var gmGo = new GameObject("GameManager");
            var gm   = gmGo.AddComponent<GameManager>();
            SetRef(gm, "_stoneSimulator", sim);

            // ── Input providers ───────────────────────────────────────────────────
            var pipGo = new GameObject("PlayerInputProvider");
            var pip   = pipGo.AddComponent<PlayerInputProvider>();

            var aiGo  = new GameObject("AIInputProvider");
            var ai    = aiGo.AddComponent<AIInputProvider>();
            SetRef(ai, "_aiTeam",        TeamId.Yellow);
            SetRef(ai, "_difficulty",    AIDifficulty.Medium);
            SetRef(ai, "_stoneSimulator",sim);
            SetRef(ai, "_config",        config);

            // ── InputRouter ───────────────────────────────────────────────────────
            var routerGo = new GameObject("InputRouter");
            var router   = routerGo.AddComponent<InputRouter>();
            SetRef(router, "_playerInput", pip);
            SetRef(router, "_aiInput",     ai);

            // ── CameraDirector + virtual cams ─────────────────────────────────────
            var cdGo = new GameObject("CameraDirector");
            cdGo.AddComponent<CurlingSimulator.Camera.CameraDirector>();

            var hackCamGo     = CreateVirtualCam("HackCam",
                new Vector3(0f, 2.5f, -29f), Quaternion.Euler(12f, 0f, 0f));
            var sweeperCamGo  = CreateVirtualCam("SweeperCam",
                new Vector3(3.5f, 1.8f, -12f), Quaternion.Euler(15f, -15f, 0f));
            var overheadCamGo = CreateVirtualCam("OverheadCam",
                new Vector3(0f, 20f, 0f), Quaternion.Euler(90f, 0f, 0f));

            var cd = cdGo.GetComponent<CurlingSimulator.Camera.CameraDirector>();
            SetRef(cd, "_hackCam",     hackCamGo);
            SetRef(cd, "_sweeperCam",  sweeperCamGo);
            SetRef(cd, "_overheadCam", overheadCamGo);

            // ── ImpactFX ──────────────────────────────────────────────────────────
            var impactGo = new GameObject("ImpactFX");
            var impact   = impactGo.AddComponent<ImpactFX>();
            SetRef(impact, "_simulator",    sim);
            SetRef(impact, "_impactPrefab", impactPrefab);

            // ── AimIndicator ──────────────────────────────────────────────────────
            var aimGo = new GameObject("AimIndicator");

            var aimLineGo = new GameObject("AimLine");
            aimLineGo.transform.SetParent(aimGo.transform, false);
            var aimLR         = aimLineGo.AddComponent<LineRenderer>();
            aimLR.positionCount = 25;
            aimLR.startWidth  = 0.04f;
            aimLR.endWidth    = 0.04f;
            aimLR.useWorldSpace = true;
            aimLR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var aimMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/AimLine_Red_Mat.mat");
            if (aimMat != null) aimLR.sharedMaterial = aimMat;

            var landingGo = new GameObject("LandingMarker");
            landingGo.transform.SetParent(aimGo.transform, false);
            // Simple disc: use a flat cylinder as placeholder
            var tempDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var discMesh = tempDisc.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempDisc);
            var landingMF = landingGo.AddComponent<MeshFilter>();
            landingMF.sharedMesh = discMesh;
            var landingMR = landingGo.AddComponent<MeshRenderer>();
            landingGo.transform.localScale = new Vector3(0.3f, 0.005f, 0.3f);
            if (aimMat != null) landingMR.sharedMaterial = aimMat;

            var aimInd = aimGo.AddComponent<AimIndicator>();
            SetRef(aimInd, "_redHack",       redHack.transform);
            SetRef(aimInd, "_yellowHack",    yellowHack.transform);
            SetRef(aimInd, "_playerInput",   pip);
            SetRef(aimInd, "_config",        config);
            SetRef(aimInd, "_aimLine",       aimLR);
            SetRef(aimInd, "_landingMarker", landingGo);

            // ── Audio system ──────────────────────────────────────────────────────
            var audioGo = new GameObject("AudioSystem");
            var audioMgr = audioGo.AddComponent<AudioManager>();

            var sfxSrc   = audioGo.AddComponent<AudioSource>();
            sfxSrc.playOnAwake  = false;
            sfxSrc.spatialBlend = 0f;

            var sweepSrc = audioGo.AddComponent<AudioSource>();
            sweepSrc.playOnAwake  = false;
            sweepSrc.loop         = true;
            sweepSrc.spatialBlend = 0f;

            var musicSrc = audioGo.AddComponent<AudioSource>();
            musicSrc.playOnAwake  = false;
            musicSrc.loop         = true;
            musicSrc.volume       = 0.4f;
            musicSrc.spatialBlend = 0f;

            SetRef(audioMgr, "_sfxSource",   sfxSrc);
            SetRef(audioMgr, "_sweepSource", sweepSrc);
            SetRef(audioMgr, "_musicSource", musicSrc);

            var bridge = audioGo.AddComponent<AudioBridge>();
            SetRef(bridge, "_simulator",   sim);
            SetRef(bridge, "_playerInput", pip);

            // ── HUD Canvas ────────────────────────────────────────────────────────
            var hudCanvas = BuildHUDCanvas(pip);

            // ── Save scene ────────────────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"Saved {scenePath}");
        }

        static void SetupGlobalVolume()
        {
            var volGo = new GameObject("Global Volume");

            try
            {
                var vol      = volGo.AddComponent<Volume>();
                vol.isGlobal = true;

                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                const string profilePath = SettingsFolder + "/PostProcessProfile.asset";
                AssetDatabase.CreateAsset(profile, profilePath);

#if UNITY_PIPELINE_URP
                var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
                bloom.intensity.Override(1.2f);
                bloom.scatter.Override(0.7f);
                bloom.threshold.Override(0.85f);

                var colorAdj = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
                colorAdj.saturation.Override(15f);
                colorAdj.postExposure.Override(0.1f);
#endif

                vol.sharedProfile = profile;
                EditorUtility.SetDirty(profile);
            }
            catch (System.Exception ex)
            {
                LogWarn($"Global Volume setup failed (URP might not be imported yet): {ex.Message}");
            }
        }

        static GameObject CreateVirtualCam(string name, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.rotation = rotation;

#if UNITY_CINEMACHINE
            var vcam = go.AddComponent<CinemachineCamera>();
            vcam.Priority = 0;
#endif
            return go;
        }

        static GameObject BuildHUDCanvas(PlayerInputProvider pip)
        {
            var canvasGo = new GameObject("HUD Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<CurlingSimulator.UI.HUDController>();

            // ── Throw phase root ──────────────────────────────────────────────────
            var throwRoot = CreateUIPanel(canvasGo, "ThrowPhaseRoot",
                new Vector2(200, 120), new Vector2(110, -70));

            var powerSlider = CreateSlider(throwRoot, "PowerSlider");
            var curlLabel   = CreateTMPLabel(throwRoot, "CurlLabel",   "IN-TURN",
                              new Vector2(0, -50), 18);
            var throwCounter= CreateTMPLabel(throwRoot, "ThrowCounter","End 1 | Throw 1/16",
                              new Vector2(0, -75), 16);
            var teamLabel   = CreateTMPLabel(throwRoot, "ThrowingTeam","RED",
                              new Vector2(0, -100), 22, Color.red);

            // Direction indicator (a simple RectTransform, parent it to throwRoot)
            var dirIndicator = new GameObject("DirectionIndicator").AddComponent<RectTransform>();
            dirIndicator.transform.SetParent(throwRoot.transform, false);
            dirIndicator.sizeDelta        = new Vector2(4, 50);
            dirIndicator.anchoredPosition = new Vector2(0, -25);

            SetRef(hud, "_throwPhaseRoot",       throwRoot);
            SetRef(hud, "_powerSlider",          powerSlider);
            SetRef(hud, "_directionIndicator",   dirIndicator);
            SetRef(hud, "_curlLabel",            curlLabel);
            SetRef(hud, "_throwCounterLabel",    throwCounter);
            SetRef(hud, "_throwingTeamLabel",    teamLabel);

            // ── Sweep phase root ──────────────────────────────────────────────────
            var sweepRoot = CreateUIPanel(canvasGo, "SweepPhaseRoot",
                new Vector2(200, 60), new Vector2(110, -160));

            var sweepSlider = CreateSlider(sweepRoot, "SweepIntensitySlider");
            SetRef(hud, "_sweepPhaseRoot",        sweepRoot);
            SetRef(hud, "_sweepIntensitySlider",  sweepSlider);

            // ── Scoreboard root ───────────────────────────────────────────────────
            var scoreRoot = CreateUIPanel(canvasGo, "ScoreboardRoot",
                new Vector2(200, 50), new Vector2(110, 35));

            var redTotal    = CreateTMPLabel(scoreRoot, "RedTotal",    "0", new Vector2(-40, 0), 24, Color.red);
            var yellowTotal = CreateTMPLabel(scoreRoot, "YellowTotal", "0", new Vector2( 40, 0), 24, Color.yellow);

            SetRef(hud, "_scoreboardRoot",    scoreRoot);
            SetRef(hud, "_redTotalLabel",     redTotal);
            SetRef(hud, "_yellowTotalLabel",  yellowTotal);

            // ── End scoring overlay ───────────────────────────────────────────────
            var endScoringRoot = CreateUIPanel(canvasGo, "EndScoringRoot",
                new Vector2(400, 80), new Vector2(0, 0));
            var endLabel = CreateTMPLabel(endScoringRoot, "EndScoringLabel",
                           "END SCORE", new Vector2(0, 0), 32);

            SetRef(hud, "_endScoringRoot",  endScoringRoot);
            SetRef(hud, "_endScoringLabel", endLabel);
            endScoringRoot.SetActive(false);

            // ── Game over overlay ─────────────────────────────────────────────────
            var gameOverRoot = CreateUIPanel(canvasGo, "GameOverRoot",
                new Vector2(600, 150), new Vector2(0, 0));
            var winnerLabel = CreateTMPLabel(gameOverRoot, "WinnerLabel",
                              "WINNER!", new Vector2(0, 0), 48);

            SetRef(hud, "_gameOverRoot",  gameOverRoot);
            SetRef(hud, "_winnerLabel",   winnerLabel);
            gameOverRoot.SetActive(false);

            return canvasGo;
        }

        // ── Main menu scene ───────────────────────────────────────────────────────

        static void BuildMainMenuScene()
        {
            const string scenePath = ScenesFolder + "/MainMenu.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam   = camGo.AddComponent<UnityEngine.Camera>();
            cam.tag   = "MainCamera";
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.10f);
            camGo.AddComponent<AudioListener>();

            var canvasGo = new GameObject("Menu Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var menu = canvasGo.AddComponent<CurlingSimulator.UI.MainMenuController>();

            var titleLabel = CreateTMPLabel(canvasGo, "TitleLabel", "CURLING SIMULATOR",
                             new Vector2(0, 120), 48);

            var diffDropdown = CreateDropdown(canvasGo, "DifficultyDropdown",
                               new Vector2(0, 30), new [] {"Easy", "Medium", "Hard"});
            var teamDropdown = CreateDropdown(canvasGo, "TeamDropdown",
                               new Vector2(0, -30), new [] {"Red", "Yellow"});

            SetRef(menu, "_difficultyDropdown", diffDropdown);
            SetRef(menu, "_teamDropdown",       teamDropdown);

            // Play button
            var playBtn  = CreateButton(canvasGo, "Play", new Vector2(0, -100));
            var playEvt  = new UnityEngine.Events.UnityEvent();
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                playBtn.onClick, menu.OnPlayClicked);

            // Quit button
            var quitBtn = CreateButton(canvasGo, "Quit", new Vector2(0, -160));
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                quitBtn.onClick, menu.OnQuitClicked);

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"Saved {scenePath}");
        }

        // ── Build settings ────────────────────────────────────────────────────────

        static void SetupBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenesFolder + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(ScenesFolder + "/MainGame.unity",  true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            Log("Build settings updated: MainMenu (0), MainGame (1).");
        }

        // ── UI helpers ────────────────────────────────────────────────────────────

        static GameObject CreateUIPanel(GameObject parent, string name,
                                        Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt              = go.AddComponent<RectTransform>();
            rt.sizeDelta        = size;
            rt.anchoredPosition = anchoredPos;
            var img             = go.AddComponent<Image>();
            img.color           = new Color(0f, 0f, 0f, 0.5f);
            return go;
        }

        static TMP_Text CreateTMPLabel(GameObject parent, string name, string text,
                                       Vector2 anchoredPos, float fontSize,
                                       Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt              = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(300, 40);
            rt.anchoredPosition = anchoredPos;
            var tmp             = go.AddComponent<TextMeshProUGUI>();
            tmp.text            = text;
            tmp.fontSize        = fontSize;
            tmp.color           = color ?? Color.white;
            tmp.alignment       = TextAlignmentOptions.Center;
            return tmp;
        }

        static Slider CreateSlider(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt              = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(160, 20);
            rt.anchoredPosition = Vector2.zero;
            return go.AddComponent<Slider>();
        }

        static TMP_Dropdown CreateDropdown(GameObject parent, string name,
                                           Vector2 pos, string[] options)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt              = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(200, 40);
            rt.anchoredPosition = pos;
            var dd              = go.AddComponent<TMP_Dropdown>();
            dd.ClearOptions();
            var optList = new List<TMP_Dropdown.OptionData>();
            foreach (var o in options)
                optList.Add(new TMP_Dropdown.OptionData(o));
            dd.AddOptions(optList);
            return dd;
        }

        static Button CreateButton(GameObject parent, string label, Vector2 pos)
        {
            var go = new GameObject($"Button_{label}");
            go.transform.SetParent(parent.transform, false);
            var rt              = go.AddComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(200, 50);
            rt.anchoredPosition = pos;
            var img             = go.AddComponent<Image>();
            img.color           = new Color(0.2f, 0.5f, 0.9f, 1f);
            var btn             = go.AddComponent<Button>();
            CreateTMPLabel(go, "Text", label, Vector2.zero, 22);
            return btn;
        }

        // ── SerializedObject helpers ──────────────────────────────────────────────

        static void SetRef(Object target, string propName, Object value)
        {
            if (target == null) return;
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop == null) { LogWarn($"Property '{propName}' not found on {target.GetType().Name}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        static void SetRef(Object target, string propName, TeamId value)
        {
            if (target == null) return;
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop != null) { prop.enumValueIndex = (int)value; so.ApplyModifiedProperties(); }
        }

        static void SetRef(Object target, string propName, AIDifficulty value)
        {
            if (target == null) return;
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop != null) { prop.enumValueIndex = (int)value; so.ApplyModifiedProperties(); }
        }

        // ── Logging ───────────────────────────────────────────────────────────────

        static void Log(string msg)     => Debug.Log($"[SceneBuilder] {msg}");
        static void LogWarn(string msg) => Debug.LogWarning($"[SceneBuilder] {msg}");
    }
}
