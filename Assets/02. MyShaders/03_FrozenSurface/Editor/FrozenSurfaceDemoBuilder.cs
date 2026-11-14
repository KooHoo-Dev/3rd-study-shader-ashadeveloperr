// FrozenSurface 데모 씬과 머티리얼을 만드는 에디터 스크립트.
// 메뉴 MyShaders > Build Frozen Surface Demo (기존 데모 씬/머티리얼을 덮어씀).
// Study Shader 씬을 복사해서 셰이더볼 3개에 서로 다른 머티리얼을 넣는다. 원본 씬은 건드리지 않는다.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FrozenSurfaceDemoBuilder
{
    const string Folder      = "Assets/02. MyShaders/03_FrozenSurface";
    const string GraphPath   = Folder + "/FrozenSurface.shadergraph";
    const string SourceScene = "Assets/00. Scenes/Study Shader.unity";
    const string ScenePath   = "Assets/00. Scenes/MyShader_FrozenSurface.unity";
    const string Screenshot  = "Screenshots/03_FrozenSurface.png";

    static readonly string[] SurfaceParts = { "Ball_Part", "Front_Part", "Back_Part_1", "Back_Part_2" };

    [MenuItem("MyShaders/Build Frozen Surface Demo")]
    public static void BuildMenu() => Build(false);

    public static void BuildFromCommandLine() => EditorApplication.Exit(Build(true) ? 0 : 1);

    static bool Build(bool captureScreenshot)
    {
        AssetDatabase.Refresh();
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
        if (!DemoUtil.CheckShader(shader, GraphPath)) return false;

        AssetDatabase.DeleteAsset(ScenePath);   // 이전에 만든 데모 씬만 지운다 (원본 Study Shader는 그대로)
        if (!AssetDatabase.CopyAsset(SourceScene, ScenePath))
        {
            Debug.LogError("[Demo] Could not copy " + SourceScene);
            return false;
        }
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var root = GameObject.Find("ShaderBalls");
        if (root == null) { Debug.LogError("[Demo] ShaderBalls not found"); return false; }

        // 얼음이 차오르는 높이 범위를 셰이더볼 표면의 실제 월드 높이에 맞춘다
        var bounds = new Bounds();
        bool hasBounds = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (System.Array.IndexOf(SurfaceParts, r.name) < 0) continue;
            if (!hasBounds) { bounds = r.bounds; hasBounds = true; } else bounds.Encapsulate(r.bounds);
        }
        float bottom = hasBounds ? bounds.min.y : 0f;
        float top = hasBounds ? bounds.max.y : 2f;
        Debug.Log($"[Demo] Shader ball surface height {bottom:F2} ~ {top:F2}");

        // 볼 1: 절반쯤 언 붉은 표면 / 볼 2: 얼었다 녹았다 반복 / 볼 3: 거의 다 언 금속 표면
        var half = DemoUtil.CreateOrLoadMaterial(Folder + "/FrozenSurface_Half.mat", shader);
        SetCommon(half, bottom, top);
        half.SetFloat("_FreezeAmount", 0.5f);

        var anim = DemoUtil.CreateOrLoadMaterial(Folder + "/FrozenSurface.mat", shader);
        SetCommon(anim, bottom, top);
        anim.SetFloat("_FreezeAmount", 0.6f);
        anim.SetFloat("_AnimateFreeze", 1f);
        anim.SetColor("_BaseColor", new Color(0.12f, 0.30f, 0.16f));

        var full = DemoUtil.CreateOrLoadMaterial(Folder + "/FrozenSurface_Metal.mat", shader);
        SetCommon(full, bottom, top);
        full.SetFloat("_FreezeAmount", 0.85f);
        full.SetColor("_BaseColor", new Color(0.85f, 0.62f, 0.25f));
        full.SetFloat("_BaseMetallic", 1f);
        full.SetFloat("_BaseSmoothness", 0.7f);
        foreach (var m in new[] { half, anim, full }) EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();

        Assign(root.transform, "ShaderBall_1", half);
        Assign(root.transform, "ShaderBall_2", anim);
        Assign(root.transform, "ShaderBall_3", full);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Demo] Scene saved: " + ScenePath);

        if (captureScreenshot)
        {
            // 볼 2는 시간에 따라 얼었다 녹으므로, 스크린샷에서는 막 얼기 시작한 순간으로 고정했다가 되돌린다
            anim.SetFloat("_AnimateFreeze", 0f);
            anim.SetFloat("_FreezeAmount", 0.25f);
            DemoUtil.CaptureScreenshot(Camera.main, Screenshot, 0f);
            anim.SetFloat("_AnimateFreeze", 1f);
            anim.SetFloat("_FreezeAmount", 0.6f);
            EditorUtility.SetDirty(anim);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();
        return true;
    }

    static void SetCommon(Material m, float bottom, float top)
    {
        m.SetFloat("_FreezeBottom", bottom);
        m.SetFloat("_FreezeTop", top);
    }

    static void Assign(Transform root, string ballName, Material mat)
    {
        var ball = DemoUtil.FindDeep(root, ballName);
        if (ball == null) { Debug.LogWarning("[Demo] Not found: " + ballName); return; }
        foreach (var part in SurfaceParts)
        {
            var t = DemoUtil.FindDeep(ball, part);
            if (t == null) { Debug.LogWarning($"[Demo] {ballName}/{part} not found"); continue; }
            var r = t.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            EditorUtility.SetDirty(r);
        }
    }
}
