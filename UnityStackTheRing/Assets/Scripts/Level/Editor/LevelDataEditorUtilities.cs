#if UNITY_EDITOR
namespace HyperCasualGame.Scripts.Level.Editor
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public static class LevelDataEditorUtilities
    {
        [MenuItem("Tools/StackTheRing/Level Data/Migrate All BucketColumns To BucketGrid")]
        public static void MigrateAllBucketColumnsToBucketGrid()
        {
            var guids = AssetDatabase.FindAssets("t:LevelData", new[]
            {
                "Assets/Data/Levels",
                "Assets/Resources/Levels"
            });

            var assets = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<global::HyperCasualGame.Scripts.Level.LevelData>(path))
                .Where(asset => asset != null)
                .ToArray();

            var migratedCount = 0;
            foreach (var levelData in assets)
            {
                if (levelData.HasBucketGrid)
                {
                    continue;
                }

                Undo.RecordObject(levelData, "Migrate BucketColumns To BucketGrid");
                levelData.MigrateBucketColumnsToGrid();
                EditorUtility.SetDirty(levelData);
                migratedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LevelDataEditorUtilities] Migrated {migratedCount} LevelData assets to BucketGrid.");
        }

        [MenuItem("Tools/StackTheRing/Level Data/Repair BucketGrid From Legacy BucketColumns")]
        public static void RepairBucketGridFromLegacyBucketColumns()
        {
            var guids = AssetDatabase.FindAssets("t:LevelData", new[]
            {
                "Assets/Data/Levels",
                "Assets/Resources/Levels"
            });

            var repairedCount = 0;
            var skippedCount = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var levelData = AssetDatabase.LoadAssetAtPath<global::HyperCasualGame.Scripts.Level.LevelData>(path);
                if (levelData == null || !levelData.HasBucketGrid || levelData.DoesBucketGridMatchLegacyColumns())
                {
                    continue;
                }

                try
                {
                    levelData.ValidateBucketGridForLegacySync();
                }
                catch
                {
                    skippedCount++;
                    continue;
                }

                Undo.RecordObject(levelData, "Repair BucketGrid From Legacy BucketColumns");
                levelData.MigrateBucketColumnsToGrid();
                EditorUtility.SetDirty(levelData);
                repairedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LevelDataEditorUtilities] Repaired {repairedCount} LevelData assets from legacy BucketColumns. skipped={skippedCount}");
        }

        [MenuItem("Tools/StackTheRing/Level Data/Sync Legacy BucketColumns From BucketGrid")]
        public static void SyncLegacyBucketColumnsFromBucketGrid()
        {
            var guids = AssetDatabase.FindAssets("t:LevelData", new[]
            {
                "Assets/Data/Levels",
                "Assets/Resources/Levels"
            });

            var assets = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<global::HyperCasualGame.Scripts.Level.LevelData>(path))
                .Where(asset => asset != null)
                .ToArray();

            var invalidAssets = assets
                .Where(asset =>
                {
                    try
                    {
                        asset.ValidateBucketGridForLegacySync();
                        return false;
                    }
                    catch
                    {
                        return true;
                    }
                })
                .ToArray();

            if (invalidAssets.Length > 0)
            {
                Debug.LogWarning($"[LevelDataEditorUtilities] Skipping {invalidAssets.Length} LevelData assets that cannot be represented by legacy BucketColumns.");
                foreach (var invalidAsset in invalidAssets)
                {
                    Debug.LogWarning($" - {AssetDatabase.GetAssetPath(invalidAsset)}");
                }
            }

            var syncedCount = 0;
            foreach (var levelData in assets)
            {
                if (!levelData.HasBucketGrid)
                {
                    continue;
                }

                try
                {
                    levelData.ValidateBucketGridForLegacySync();
                }
                catch
                {
                    continue;
                }

                Undo.RecordObject(levelData, "Sync Legacy BucketColumns From BucketGrid");
                levelData.SyncLegacyColumnsFromGrid();
                EditorUtility.SetDirty(levelData);
                syncedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LevelDataEditorUtilities] Synced legacy BucketColumns for {syncedCount} LevelData assets.");
        }

        [MenuItem("Tools/StackTheRing/Level Data/Validate All BucketGrid Assets")]
        public static void ValidateAllBucketGridAssets()
        {
            var guids = AssetDatabase.FindAssets("t:LevelData", new[]
            {
                "Assets/Data/Levels",
                "Assets/Resources/Levels"
            });

            var invalidCount = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var levelData = AssetDatabase.LoadAssetAtPath<global::HyperCasualGame.Scripts.Level.LevelData>(path);
                if (levelData == null || !levelData.HasBucketGrid)
                {
                    continue;
                }

                try
                {
                    levelData.ValidateBucketGridForRuntime();

                    if (!levelData.DoesBucketGridMatchLegacyColumns())
                    {
                        invalidCount++;
                        Debug.LogWarning($"[LevelDataEditorUtilities] BucketGrid differs from legacy BucketColumns at {path}. This is expected when the grid uses gaps that legacy columns cannot represent.", levelData);
                    }
                }
                catch (System.Exception exception)
                {
                    invalidCount++;
                    Debug.LogError($"[LevelDataEditorUtilities] Invalid BucketGrid at {path}: {exception.Message}", levelData);
                }
            }

            Debug.Log($"[LevelDataEditorUtilities] BucketGrid validation complete. invalid={invalidCount}");
        }
    }
}
#endif
