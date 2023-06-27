using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity.Internals;
using HypernexSharp.APIObjects;
using UnityEngine;

namespace Hypernex.CCK.Unity
{
    [RequireComponent(typeof(Renderer))]
    public class MaterialDescriptor : MonoBehaviour
    {
        public Renderer TargetRenderer;

        [SerializeField] public SerializedDictionaries.BuildMaterialsDict Materials = new SerializedDictionaries.BuildMaterialsDict(
            new Dictionary<BuildPlatform, SerializedDictionaries.MaterialsDict>
            {
                [BuildPlatform.Windows] = new SerializedDictionaries.MaterialsDict(new Dictionary<Material, Material>()),
                [BuildPlatform.Android] = new SerializedDictionaries.MaterialsDict(new Dictionary<Material, Material>())
            });

        public bool IsSet => (Backup?.Count ?? 0) > 0;

        private BuildPlatform TargetBackup;
        private List<(Material, Material)> Backup;
        private bool BackupUseShared;

        public void Refresh()
        {
            if (TargetRenderer == null)
            {
                TargetRenderer = GetComponent<Renderer>();
                if (TargetRenderer == null)
                    return;
            }
            if (IsSet)
                return;
            try
            {
                foreach (Material targetMeshSharedMaterial in TargetRenderer.sharedMaterials)
                {
                    if(!Materials[BuildPlatform.Windows].ContainsKey(targetMeshSharedMaterial))
                        Materials[BuildPlatform.Windows].Add(targetMeshSharedMaterial, null);
                    if(!Materials[BuildPlatform.Android].ContainsKey(targetMeshSharedMaterial))
                        Materials[BuildPlatform.Android].Add(targetMeshSharedMaterial, null);
                }
                foreach (Material material in Materials[BuildPlatform.Windows].Keys)
                {
                    if (!TargetRenderer.sharedMaterials.Contains(material))
                        Materials[BuildPlatform.Windows].Remove(material);
                }
                foreach (Material material in Materials[BuildPlatform.Android].Keys)
                {
                    if (!TargetRenderer.sharedMaterials.Contains(material))
                        Materials[BuildPlatform.Android].Remove(material);
                }
            }
            catch(InvalidOperationException){}
        }

        public void SetMaterials(BuildPlatform buildPlatform, bool useShared = false, Action<Material, Material> onOldMaterial = null)
        {
            if (Backup != null && Backup.Count > 0)
            {
                Logger.CurrentLogger.Warn("Cannot SetMaterials when already set!");
                return;
            }
            TargetBackup = buildPlatform;
            Backup = new List<(Material, Material)>();
            BackupUseShared = useShared;
            List<Material> newSharedMaterials = new List<Material>();
            foreach (Material targetRendererSharedMaterial in TargetRenderer.sharedMaterials)
            {
                Material newMaterial =
                    new Material(
                        Materials[buildPlatform][
                            Materials[buildPlatform].Keys.First(x => x == targetRendererSharedMaterial)]);
                onOldMaterial?.Invoke(targetRendererSharedMaterial,
                    Materials[buildPlatform][
                        Materials[buildPlatform].Keys.First(x => x == targetRendererSharedMaterial)]);
                Backup.Add((new Material(targetRendererSharedMaterial), newMaterial));
                newSharedMaterials.Add(newMaterial);
            }
            if (useShared)
                TargetRenderer.sharedMaterials = newSharedMaterials.ToArray();
            else
                TargetRenderer.materials = newSharedMaterials.ToArray();
        }

        public void Revert()
        {
            if (!IsSet)
                return;
            List<Material> newSharedMaterials = new List<Material>();
            foreach ((Material, Material) valueTuple in Backup)
                newSharedMaterials.Add(valueTuple.Item1);
            if (BackupUseShared)
                TargetRenderer.sharedMaterials = newSharedMaterials.ToArray();
            else
                TargetRenderer.materials = newSharedMaterials.ToArray();
            Backup.Clear();
        }

        public void Revert(List<(Material, Material)> materialsFromAssetDatabase)
        {
            if (!IsSet)
                return;
            List<Material> newSharedMaterials = new List<Material>();
            foreach ((Material, Material) valueTuple in materialsFromAssetDatabase)
                newSharedMaterials.Add(valueTuple.Item1);
            if (BackupUseShared)
                TargetRenderer.sharedMaterials = newSharedMaterials.ToArray();
            else
                TargetRenderer.materials = newSharedMaterials.ToArray();
            Backup.Clear();
        }
    }
}