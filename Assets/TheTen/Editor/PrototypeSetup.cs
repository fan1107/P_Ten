using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheTen.Editor
{
    public static class PrototypeSetup
    {
        [MenuItem("THE TEN/Create or open prototype")]
        public static void Create()
        {
            const string configPath = "Assets/TheTen/GameBalance.asset";
            const string scenePath = "Assets/Scenes/TheTen.unity";
            var balance = AssetDatabase.LoadAssetAtPath<GameBalance>(configPath);
            if (balance == null) { balance = ScriptableObject.CreateInstance<GameBalance>(); AssetDatabase.CreateAsset(balance, configPath); }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null) EditorSceneManager.OpenScene(scenePath);
            else
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera";
                var c = camera.GetComponent<Camera>(); c.orthographic = true; c.backgroundColor = new Color32(26, 51, 54, 255); c.clearFlags = CameraClearFlags.SolidColor;
                camera.transform.position = new Vector3(0, 0, -10);
                var light = new GameObject("Directional Light", typeof(Light)); light.GetComponent<Light>().type = LightType.Directional;
                var app = new GameObject("THE TEN • Application", typeof(TheTenApp)); app.GetComponent<TheTenApp>().Balance = balance;
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 720; PlayerSettings.defaultScreenHeight = 1280;
            PlayerSettings.productName = "THE TEN!";
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
