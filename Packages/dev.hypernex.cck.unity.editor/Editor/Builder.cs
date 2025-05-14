using System;
using System.IO;
using Hypernex.CCK.Unity.Assets;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Hypernex.CCK.Unity.Editor
{
    public static class Builder
    {
        public static string BuildAssetBundle(Avatar avatar, AssetIdentifier assetIdentifier, TempDir tempDir)
        {
            bool didSavePrefab;
            string uploadTypeString = "avatar_";
            string uploadFileType = "hna";
            string id = assetIdentifier.Id;
            if (string.IsNullOrEmpty(id))
                id = uploadTypeString + "temp_" + Guid.NewGuid();
            string assetToSave = Path.Combine(tempDir.GetPath(), id + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(avatar.gameObject, assetToSave, out didSavePrefab);
            if (didSavePrefab && File.Exists(assetToSave))
            {
                // Build AssetBundle
                string[] assets = { assetToSave };
                AssetBundleBuild[] builds = new AssetBundleBuild[1];
                builds[0].assetBundleName = id;
                builds[0].assetNames = assets;
                builds[0].assetBundleVariant = uploadFileType;
                tempDir.CreateChildDirectory("assetbundle");
                string abp = Path.Combine(tempDir.GetPath(), "assetbundle");
                BuildPipeline.BuildAssetBundles(abp, builds, BuildAssetBundleOptions.ChunkBasedCompression,
                    EditorUserBuildSettings.activeBuildTarget);
                foreach (string assetBundle in Directory.GetFiles(abp))
                {
                    string assetBundleName = Path.GetFileName(assetBundle);
                    if (assetBundleName == $"{id}.{uploadFileType}")
                    {
                        return assetBundle;
                    }
                }
                Logger.CurrentLogger.Error("Target AssetBundle did not exist!");
            }
            else
                Logger.CurrentLogger.Error("Prefab failed to save or prefab does not exist!");
            // Failed to Copy File
            EditorUtility.DisplayDialog("Hypernex.CCK.Unity",
                "Failed to build! Please see console for more information.", "OK");
            return String.Empty;
        }
        
        public static string BuildAssetBundle(World w, AssetIdentifier assetIdentifier, TempDir tempDir)
        {
            string uploadTypeString = "world_";
            string uploadFileType = "hnw";
            string id = assetIdentifier.Id;
            if (string.IsNullOrEmpty(id))
                id = uploadTypeString + "temp_" + Guid.NewGuid();
            tempDir.CreateChildDirectory("assetbundle");
            string abp = Path.Combine(tempDir.GetPath(), "assetbundle");
            Scene currentScene = SceneManager.GetActiveScene();
            string[] assets = { currentScene.path };
            AssetBundleBuild[] builds = new AssetBundleBuild[1];
            builds[0].assetBundleName = id;
            builds[0].assetNames = assets;
            builds[0].assetBundleVariant = uploadFileType;
            BuildPipeline.BuildAssetBundles(abp, builds, BuildAssetBundleOptions.ChunkBasedCompression,
                EditorUserBuildSettings.activeBuildTarget);
            foreach (string assetBundle in Directory.GetFiles(abp))
            {
                string assetBundleName = Path.GetFileName(assetBundle);
                if (assetBundleName == $"{id}.{uploadFileType}")
                {
                    return assetBundle;
                }
            }
            Logger.CurrentLogger.Error("Target AssetBundle did not exist!");
            // Failed to Copy File
            EditorUtility.DisplayDialog("Hypernex.CCK",
                "Failed to build! Please see console for more information.", "OK");
            return String.Empty;
        }
    }
}