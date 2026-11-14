// 데모 씬 빌더들이 함께 쓰는 도구: 셰이더 컴파일 확인, 머티리얼 생성, 스크린샷.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DemoUtil
{
    public static bool CheckShader(Shader shader, string path)
    {
        if (shader == null)
        {
            Debug.LogError("[Demo] Shader not found or failed to import: " + path);
            return false;
        }
        bool hasError = ShaderUtil.ShaderHasError(shader);
        foreach (var m in ShaderUtil.GetShaderMessages(shader))
            Debug.Log($"[Demo] {shader.name} {m.severity}: {m.message} (line {m.line}) {m.messageDetails}");
        Debug.Log($"[Demo] Shader '{shader.name}' has error: {hasError}, properties: {shader.GetPropertyCount()}");
        return !hasError;
    }

    public static Material CreateOrLoadMaterial(string path, Shader shader)
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

    public static Transform FindDeep(Transform parent, string name)
    {
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// 씬의 파티클을 simulateSeconds만큼 진행시킨 뒤 카메라 화면을 PNG로 저장한다.
    public static void CaptureScreenshot(Camera cam, string path, float simulateSeconds)
    {
        if (simulateSeconds > 0f)
        {
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (ps.transform.parent != null && ps.transform.parent.GetComponent<ParticleSystem>() != null) continue;
                ps.Simulate(simulateSeconds, true, true);
                ps.Pause();
            }
        }

        const int w = 1920, h = 1080;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
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

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Debug.Log("[Demo] Screenshot saved: " + path);
    }
}
