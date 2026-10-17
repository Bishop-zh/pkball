using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Only the built WebGL scenes change; authored desktop scenes remain untouched.</summary>
public sealed class PortfolioWebGLFonts : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || report.summary.platform != BuildTarget.WebGL) return;
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Portfolio/Fonts/NotoSansSC-Regular.otf");
        if (font == null) throw new BuildFailedException("Missing bundled browser Chinese font");
        foreach (var root in scene.GetRootGameObjects())
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Font)
                    property.objectReferenceValue = font;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
