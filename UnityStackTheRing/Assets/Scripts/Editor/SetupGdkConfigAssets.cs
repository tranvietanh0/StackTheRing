namespace HyperCasualGame.Scripts.Editor
{
    using BlueprintFlow.BlueprintControlFlow;
    using GameFoundationCore.Scripts.Models;
    using UnityEditor;
    using UnityEngine;

    public static class SetupGdkConfigAssets
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string ConfigsFolderPath = "Assets/Resources/Configs";
        private const string GdkConfigAssetPath = ConfigsFolderPath + "/GDKConfig.asset";
        private const string BlueprintConfigAssetPath = ConfigsFolderPath + "/BlueprintConfig.asset";

        [MenuItem("StackTheRing/Setup/Create GDK Config Assets")]
        public static void CreateAssets()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder(ResourcesFolderPath, "Configs");

            var gdkConfig = AssetDatabase.LoadAssetAtPath<GDKConfig>(GdkConfigAssetPath);
            if (gdkConfig == null)
            {
                gdkConfig = ScriptableObject.CreateInstance<GDKConfig>();
                AssetDatabase.CreateAsset(gdkConfig, GdkConfigAssetPath);
            }

            var blueprintConfig = AssetDatabase.LoadAssetAtPath<BlueprintConfig>(BlueprintConfigAssetPath);
            if (blueprintConfig == null)
            {
                blueprintConfig = ScriptableObject.CreateInstance<BlueprintConfig>();
                AssetDatabase.CreateAsset(blueprintConfig, BlueprintConfigAssetPath);
            }

            if (!gdkConfig.HasGameConfig<BlueprintConfig>())
            {
                gdkConfig.AddGameConfig(blueprintConfig);
            }

            EditorUtility.SetDirty(gdkConfig);
            EditorUtility.SetDirty(blueprintConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = gdkConfig;
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            var folderPath = $"{parentPath}/{folderName}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }
    }
}
