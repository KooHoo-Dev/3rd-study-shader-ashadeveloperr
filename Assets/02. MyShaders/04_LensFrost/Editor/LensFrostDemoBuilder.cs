// LensFrost 데모 씬, 머티리얼, 전용 렌더러를 만드는 에디터 스크립트.
// 메뉴 MyShaders > Build Lens Frost Demo (기존 데모 씬/머티리얼을 덮어씀).
//
// 화면 효과는 URP 렌더러의 Full Screen Pass 기능으로 그린다. 기본 렌더러(PC_Renderer)에 넣으면
// 모든 씬에 서리가 끼므로, 서리 전용 렌더러(LensFrost_Renderer)를 따로 만들고
// 데모 씬의 카메라만 그 렌더러를 쓰게 한다.
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class LensFrostDemoBuilder
{
    const string Folder       = "Assets/02. MyShaders/04_LensFrost";
    const string GraphPath    = Folder + "/LensFrost.shadergraph";
    const string MatPath      = Folder + "/LensFrost.mat";
    const string RendererPath = Folder + "/LensFrost_Renderer.asset";
    const string BaseRenderer = "Assets/Settings/PC_Renderer.asset";
    static readonly string[] PipelineAssets = { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" };
    const string SourceScene  = "Assets/00. Scenes/MyShader_Blizzard.unity";
    const string ScenePath    = "Assets/00. Scenes/MyShader_LensFrost.unity";
    const string Screenshot   = "Screenshots/04_LensFrost.png";

    [MenuItem("MyShaders/Build Lens Frost Demo")]
    public static void BuildMenu() => Build(false);

    public static void BuildFromCommandLine() => EditorApplication.Exit(Build(true) ? 0 : 1);

    static bool Build(bool captureScreenshot)
    {
        AssetDatabase.Refresh();
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
        if (!DemoUtil.CheckShader(shader, GraphPath)) return false;

        var mat = DemoUtil.CreateOrLoadMaterial(MatPath, shader);
        EditorUtility.SetDirty(mat);

        // ---- 1. 서리 전용 렌더러: 기본 렌더러를 복사하고 Full Screen Pass를 추가 ----
        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (rendererData == null)
        {
            if (!AssetDatabase.CopyAsset(BaseRenderer, RendererPath)) { Debug.LogError("[Demo] Copy renderer failed"); return false; }
            rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        }
        var feature = rendererData.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault();
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = "LensFrost";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var so = new SerializedObject(rendererData);
            var list = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        feature.passMaterial = mat;
        feature.passIndex = 0;
        feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
        feature.fetchColorBuffer = true;
        feature.requirements = ScriptableRenderPassInput.Color;
        feature.SetActive(true);
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);

        // ---- 2. 렌더러 목록에 추가만 한다 (기본 렌더러 index 0은 그대로) ----
        int rendererIndex = -1;
        foreach (var path in PipelineAssets)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset == null) continue;
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            int idx = -1;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == rendererData) idx = i;
            if (idx < 0)
            {
                list.arraySize++;
                idx = list.arraySize - 1;
                list.GetArrayElementAtIndex(idx).objectReferenceValue = rendererData;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            if (rendererIndex >= 0 && rendererIndex != idx)
                Debug.LogWarning($"[Demo] LensFrost renderer index differs in {path}: {idx} vs {rendererIndex}");
            rendererIndex = idx;
            Debug.Log($"[Demo] {path}: LensFrost_Renderer at index {idx}");
        }
        AssetDatabase.SaveAssets();

        // ---- 3. 데모 씬: 눈보라 씬을 복사하고 카메라만 서리 렌더러로 ----
        AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SourceScene, ScenePath)) { Debug.LogError("[Demo] Copy scene failed"); return false; }
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var cam = Camera.main;
        var camData = cam.GetUniversalAdditionalCameraData();
        camData.SetRenderer(rendererIndex);
        EditorUtility.SetDirty(camData);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Demo] Scene saved: " + ScenePath);

        if (captureScreenshot)
            DemoUtil.CaptureScreenshot(cam, Screenshot, 8f);
        AssetDatabase.Refresh();
        return true;
    }
}
