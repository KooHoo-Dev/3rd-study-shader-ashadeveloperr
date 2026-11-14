// Blizzard 데모 씬과 머티리얼을 만드는 에디터 스크립트.
// 메뉴 MyShaders > Build Blizzard Demo 로 다시 만들 수 있다 (기존 씬/머티리얼을 덮어씀).
// 배치 모드: Unity.exe -batchmode -projectPath <프로젝트> -executeMethod BlizzardDemoBuilder.BuildFromCommandLine -quit
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BlizzardDemoBuilder
{
    const string Folder      = "Assets/02. MyShaders/02_Blizzard";
    const string ShaderPath  = Folder + "/Blizzard.shader";
    const string FlakeMat    = Folder + "/Blizzard.mat";
    const string GustMat     = Folder + "/Blizzard_Gust.mat";
    const string DemoFolder  = Folder + "/Demo";
    const string ScenePath   = "Assets/00. Scenes/MyShader_Blizzard.unity";
    const string Screenshot  = "Screenshots/02_Blizzard.png";

    static readonly Color FogColor = new Color(0.60f, 0.65f, 0.72f);
    // 바람: +X 방향(화면 오른쪽)으로 불고, 눈은 내려오면서 살짝 카메라 쪽으로 온다
    static readonly Vector3 WindMin = new Vector3(5f, -3.0f, -1.5f);
    static readonly Vector3 WindMax = new Vector3(8f, -1.5f, -0.3f);

    [MenuItem("MyShaders/Build Blizzard Demo")]
    public static void BuildMenu()
    {
        Build(captureScreenshot: false);
    }

    public static void BuildFromCommandLine()
    {
        bool ok = Build(captureScreenshot: true);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Build(bool captureScreenshot)
    {
        AssetDatabase.Refresh();
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            Debug.LogError("[Blizzard] Shader not found: " + ShaderPath);
            return false;
        }

        // ---- 셰이더 컴파일 확인 ----
        bool hasError = ShaderUtil.ShaderHasError(shader);
        foreach (var m in ShaderUtil.GetShaderMessages(shader))
            Debug.Log($"[Blizzard] Shader {m.severity}: {m.message} (line {m.line}) {m.messageDetails}");
        Debug.Log("[Blizzard] Shader has error: " + hasError);
        if (hasError) return false;

        // ---- 머티리얼 ----
        var flake = CreateOrLoadMaterial(FlakeMat, shader);
        SetFlakeDefaults(flake);
        var gust = CreateOrLoadMaterial(GustMat, shader);
        SetGustDefaults(gust);

        if (!AssetDatabase.IsValidFolder(DemoFolder))
            AssetDatabase.CreateFolder(Folder, "Demo");
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var groundMat = CreateOrLoadMaterial(DemoFolder + "/Demo_SnowGround.mat", lit);
        groundMat.SetColor("_BaseColor", new Color(0.86f, 0.89f, 0.93f));
        groundMat.SetFloat("_Smoothness", 0.25f);
        var propMat = CreateOrLoadMaterial(DemoFolder + "/Demo_DarkProp.mat", lit);
        propMat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f));
        propMat.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(groundMat);
        EditorUtility.SetDirty(propMat);
        AssetDatabase.SaveAssets();

        // ---- 씬 ----
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.045f;
        RenderSettings.fogColor = FogColor;
        RenderSettings.ambientMode = AmbientMode.Flat;
        var ambient = new Color(0.50f, 0.55f, 0.63f);
        RenderSettings.ambientLight = ambient;
        var sh = new SphericalHarmonicsL2();
        sh.AddAmbientLight(ambient);
        RenderSettings.ambientProbe = sh;

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = FogColor;
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 300f;
        camGo.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.Euler(4f, 0f, 0f));
        camGo.AddComponent<AudioListener>();

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.78f, 0.85f, 1.0f);
        light.intensity = 0.7f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(22f, 160f, 0f); // 해가 카메라 앞쪽 위에 있어 역광 산란이 보이게

        BuildEnvironment(groundMat, propMat);

        var root = new GameObject("Blizzard").transform;
        // 멀리: 촘촘하고 작은 눈 (대부분 안개 속으로 사라짐)
        CreateSnowLayer(root, "Snow_Far",  flake, new Vector3(-12f, 6f, 32f), new Vector3(110f, 14f, 44f),
                        rate: 7000, size: new Vector2(0.05f, 0.09f), lifetime: new Vector2(4f, 6f), maxParticles: 50000);
        // 중간: 주된 눈 (초점이 맞는 거리)
        CreateSnowLayer(root, "Snow_Mid",  flake, new Vector3(-8f, 4f, 11f),  new Vector3(40f, 9f, 18f),
                        rate: 6000, size: new Vector2(0.05f, 0.11f), lifetime: new Vector2(4f, 6f), maxParticles: 40000);
        // 가까이: 가끔 카메라를 스치는 큰 흐린 눈
        CreateSnowLayer(root, "Snow_Near", flake, new Vector3(-3f, 1.7f, 2.5f), new Vector3(10f, 4f, 5f),
                        rate: 120, size: new Vector2(0.05f, 0.1f), lifetime: new Vector2(3f, 4f), maxParticles: 1000);
        // 바람에 휩쓸리는 눈가루 덩어리
        var gusts = CreateSnowLayer(root, "Snow_Gusts", gust, new Vector3(-10f, 2.5f, 16f), new Vector3(60f, 5f, 30f),
                        rate: 8, size: new Vector2(6f, 12f), lifetime: new Vector2(6f, 8f), maxParticles: 200);
        var gm = gusts.main;
        gm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.2f), new Color(1f, 1f, 1f, 0.4f));
        var gv = gusts.velocityOverLifetime;
        gv.x = new ParticleSystem.MinMaxCurve(8f, 12f);
        gv.y = new ParticleSystem.MinMaxCurve(-1.0f, 0.3f);
        gv.z = new ParticleSystem.MinMaxCurve(-1.0f, 0.5f);
        var gn = gusts.noise;
        gn.strength = 0.8f;
        gn.frequency = 0.08f;

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[Blizzard] Scene saved: " + ScenePath);

        if (captureScreenshot)
            CaptureScreenshot(cam, root);

        AssetDatabase.Refresh();
        return true;
    }

    static Material CreateOrLoadMaterial(string path, Shader shader)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }
        return mat;
    }

    static void SetFlakeDefaults(Material m)
    {
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Opacity", 0.9f);
        m.SetFloat("_AmbientStrength", 1.0f);
        m.SetFloat("_LightStrength", 0.6f);
        m.SetFloat("_ForwardScatter", 1.5f);
        m.SetFloat("_EdgeSoftness", 0.7f);
        m.SetFloat("_Irregularity", 0.18f);
        m.SetFloat("_Twinkle", 0.25f);
        m.SetFloat("_Wispiness", 0f);
        m.SetFloat("_NoiseScale", 2f);
        m.SetFloat("_NoiseScroll", 0.4f);
        m.SetFloat("_StretchAmount", 0.018f);
        m.SetFloat("_MaxStretch", 3.5f);
        m.SetFloat("_FocusDistance", 7f);
        m.SetFloat("_FocusRange", 3f);
        m.SetFloat("_BlurDistance", 25f);
        m.SetFloat("_BokehGrow", 2.5f);
        SetSoftParticles(m, true);
        m.SetFloat("_SoftParticleDistance", 0.3f);
        m.SetFloat("_NearFadeStart", 0.25f);
        m.SetFloat("_NearFadeRange", 0.6f);
        EditorUtility.SetDirty(m);
    }

    static void SetGustDefaults(Material m)
    {
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Opacity", 0.85f);
        m.SetFloat("_AmbientStrength", 1.1f);
        m.SetFloat("_LightStrength", 0.4f);
        m.SetFloat("_ForwardScatter", 1.0f);
        m.SetFloat("_EdgeSoftness", 1f);
        m.SetFloat("_Irregularity", 0.3f);
        m.SetFloat("_Twinkle", 0f);
        m.SetFloat("_Wispiness", 0.85f);
        m.SetFloat("_NoiseScale", 1.3f);
        m.SetFloat("_NoiseScroll", 0.3f);
        m.SetFloat("_StretchAmount", 0.12f);
        m.SetFloat("_MaxStretch", 2.5f);
        m.SetFloat("_FocusDistance", 7f);
        m.SetFloat("_FocusRange", 3f);
        m.SetFloat("_BlurDistance", 25f);
        m.SetFloat("_BokehGrow", 0f);
        SetSoftParticles(m, true);
        m.SetFloat("_SoftParticleDistance", 2f);
        m.SetFloat("_NearFadeStart", 0.5f);
        m.SetFloat("_NearFadeRange", 3f);
        EditorUtility.SetDirty(m);
    }

    static void SetSoftParticles(Material m, bool on)
    {
        m.SetFloat("_SoftParticles", on ? 1f : 0f);
        if (on) m.EnableKeyword("_SOFTPARTICLES_ON");
        else m.DisableKeyword("_SOFTPARTICLES_ON");
    }

    static void BuildEnvironment(Material groundMat, Material propMat)
    {
        var env = new GameObject("Environment").transform;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(env);
        ground.transform.localScale = new Vector3(30f, 1f, 30f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // 거리별로 놓인 기둥/오두막: 안개와 Soft Particle 효과를 보여 주는 기준점
        var props = new (Vector3 pos, Vector3 scale)[]
        {
            (new Vector3(-2.5f, 1.5f, 5f),  new Vector3(0.3f, 3f, 0.3f)),
            (new Vector3( 3.0f, 2.0f, 9f),  new Vector3(0.35f, 4f, 0.35f)),
            (new Vector3(-6.0f, 2.5f, 16f), new Vector3(0.4f, 5f, 0.4f)),
            (new Vector3( 6.0f, 1.5f, 22f), new Vector3(5f, 3f, 4f)),
            (new Vector3(-12f, 3.0f, 32f),  new Vector3(0.5f, 6f, 0.5f)),
            (new Vector3( 14f, 3.0f, 45f),  new Vector3(0.5f, 6f, 0.5f)),
        };
        for (int i = 0; i < props.Length; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Prop_" + (i + 1);
            go.transform.SetParent(env);
            go.transform.position = props[i].pos;
            go.transform.localScale = props[i].scale;
            go.GetComponent<Renderer>().sharedMaterial = propMat;
        }
    }

    static ParticleSystem CreateSnowLayer(Transform parent, string name, Material mat, Vector3 center, Vector3 boxSize,
                                          float rate, Vector2 size, Vector2 lifetime, int maxParticles)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = center;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 10f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.55f), Color.white);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;   // 상자 부피 안 아무 곳에서나 생성
        shape.scale = boxSize;

        // 바람: 파티클마다 두 상수 사이의 다른 속도
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(WindMin.x, WindMax.x);
        vel.y = new ParticleSystem.MinMaxCurve(WindMin.y, WindMax.y);
        vel.z = new ParticleSystem.MinMaxCurve(WindMin.z, WindMax.z);

        // 돌풍/소용돌이: 공간을 흐르는 노이즈 필드
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1.5f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.6f;
        noise.octaveCount = 2;
        noise.damping = true;

        // 생겨날 때와 사라질 때 서서히 (상자 안에서 갑자기 나타나지 않게)
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = mat;
        renderer.sortMode = ParticleSystemSortMode.None;
        renderer.minParticleSize = 0f;
        renderer.maxParticleSize = 10f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        // 셰이더의 Attributes와 순서가 맞아야 한다
        renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
            ParticleSystemVertexStream.StableRandomXY,
            ParticleSystemVertexStream.Center,
            ParticleSystemVertexStream.SizeX,
            ParticleSystemVertexStream.Velocity,
        });

        return ps;
    }

    static void CaptureScreenshot(Camera cam, Transform blizzardRoot)
    {
        foreach (var ps in blizzardRoot.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Simulate(8f, false, true);
            ps.Pause();
        }

        const int w = 1920, h = 1080;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        cam.targetTexture = rt;
        cam.Render();
        cam.Render(); // 첫 프레임은 깊이 텍스처 등이 준비되지 않을 수 있어 한 번 더

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;

        Directory.CreateDirectory(Path.GetDirectoryName(Screenshot));
        File.WriteAllBytes(Screenshot, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Debug.Log("[Blizzard] Screenshot saved: " + Screenshot);
    }
}
