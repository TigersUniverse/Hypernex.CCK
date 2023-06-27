using System;
using System.Collections.Generic;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using HypernexSharp.APIObjects;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Editor.Editors
{
    [CustomEditor(typeof(MaterialDescriptor))]
    public class MaterialDescriptorEditor : UnityEditor.Editor
    {
        private MaterialDescriptor MaterialDescriptor;

        private void OnEnable()
        {
            MaterialDescriptor = target as MaterialDescriptor;
        }

        private BuildPlatform GetPlatformFromTarget(BuildTargetGroup buildTargetGroup)
        {
            if (buildTargetGroup == BuildTargetGroup.Android)
                return BuildPlatform.Android;
            return BuildPlatform.Windows;
        }

        private static List<(string, string)> cache = new List<(string, string)>();

        public override void OnInspectorGUI()
        {
            try
            {
                MaterialDescriptor.Refresh();
                BuildTargetGroup bg = EditorGUILayout.BeginBuildTargetSelectionGrouping();
                if (bg != BuildTargetGroup.Standalone && bg != BuildTargetGroup.Android)
                    GUILayout.Label("Unsupported Build Target!", EditorStyles.centeredGreyMiniLabel);
                else
                {
                    if (MaterialDescriptor.IsSet)
                    {
                        EditorGUILayout.LabelField("Currently Previewing Materials", EditorStyles.centeredGreyMiniLabel);
                        if (GUILayout.Button("Revert"))
                        {
                            List<(Material, Material)> mats = new List<(Material, Material)>();
                            foreach ((string, string) valueTuple in cache)
                            {
                                Material o = AssetDatabase.LoadAssetAtPath<Material>(valueTuple.Item1);
                                Material n = AssetDatabase.LoadAssetAtPath<Material>(valueTuple.Item2);
                                if(o != null && n != null)
                                    mats.Add((o, n));
                            }
                            if (mats.Count != MaterialDescriptor.TargetRenderer.sharedMaterials.Length)
                            {
                                Logger.CurrentLogger.Warn("Failed to find all Materials! Defaulting to Memory.");
                                MaterialDescriptor.Revert();
                                return;
                            }
                            MaterialDescriptor.Revert(mats);
                        }
                    }
                    else
                    {
                        BuildPlatform buildPlatform = GetPlatformFromTarget(bg);
                        foreach (KeyValuePair<Material, Material> keyValuePair in new SerializedDictionaries.MaterialsDict(
                                     MaterialDescriptor.Materials[buildPlatform]))
                        {
                            Material old = keyValuePair.Value;
                            MaterialDescriptor.Materials[buildPlatform][keyValuePair.Key] =
                                (Material) EditorGUILayout.ObjectField(keyValuePair.Key.name,
                                    MaterialDescriptor.Materials[buildPlatform][keyValuePair.Key], typeof(Material), false);
                            if(old != MaterialDescriptor.Materials[buildPlatform][keyValuePair.Key])
                                EditorUtility.SetDirty(MaterialDescriptor.gameObject);
                        }
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Preview"))
                        {
                            cache = new List<(string, string)>();
                            MaterialDescriptor.SetMaterials(buildPlatform, true, (o, n) =>
                            {
                                string pathOld = AssetDatabase.GetAssetPath(o);
                                string pathNew = AssetDatabase.GetAssetPath(n);
                                if(!string.IsNullOrEmpty(pathOld) && !string.IsNullOrEmpty(pathNew))
                                    cache.Add((pathOld, pathNew));
                            });
                        }
                        if (GUILayout.Button("Auto-Fill for same Platform"))
                        {
                            foreach (KeyValuePair<Material, Material> keyValuePair in new
                                         SerializedDictionaries.MaterialsDict(MaterialDescriptor.Materials[buildPlatform]))
                                MaterialDescriptor.Materials[buildPlatform][keyValuePair.Key] = keyValuePair.Key;
                            EditorUtility.SetDirty(MaterialDescriptor.gameObject);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndBuildTargetSelectionGrouping();
            }
            catch(ArgumentException){}
        }
    }
}