using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TheTen.Editor
{
    public static class LocalApkBuild
    {
        public static void Configure()
        {
            PlayerSettings.productName = "THE TEN!";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.theten.local");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/TheTen.unity", true) };
            var balance = AssetDatabase.LoadAssetAtPath<GameBalance>("Assets/TheTen/GameBalance.asset");
            balance.EnableMockCommerce = true;
            EditorUtility.SetDirty(balance);
            AssetDatabase.SaveAssets();
        }
    }
}
