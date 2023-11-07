using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hypernex.CCK.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Avatar = Hypernex.CCK.Unity.Avatar;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Editor.Editors.Tools
{
    public class EditorTools
    {
        public static void NewGUILine() => GUILayout.Label("", EditorStyles.largeLabel);

        public static void InvokeOnMainThread(Delegate d)
        {
            void Callback() => d.DynamicInvoke();
            EditorApplication.delayCall += Callback;
        }

        public static void DrawObjectList<T>(ref List<T> list, string listName, ref bool isOpen, Action requestSave,
            Action<T, int> CustomEvents = null, Action<int> OnAdd = null, Action<T, int> OnRemove = null, 
            bool allowSceneObjects = false) where T : Object
        {
            isOpen = EditorGUILayout.Foldout(isOpen, $"<b>{listName}</b>",
                new GUIStyle(EditorStyles.foldout) {richText = true});
            if (isOpen)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    T val = list.ElementAt(i);
                    T obj = (T) EditorGUILayout.ObjectField(val, typeof(T), allowSceneObjects);
                    if(obj != null && !obj.Equals(val) || val != null && obj == null)
                        requestSave.Invoke();
                    list[i] = obj;
                    CustomEvents?.Invoke(list[i], i);
                    if (GUILayout.Button("Remove"))
                    {
                        OnRemove?.Invoke(list[i], i);
                        list.RemoveAt(i);
                        requestSave.Invoke();
                    }
                }
                NewGUILine();
                if (GUILayout.Button("Add New " + listName))
                {
                    list.Add(null);
                    OnAdd?.Invoke(list.Count - 1);
                    requestSave.Invoke();
                }
            }
        }
        
        public static void ADrawObjectList<T>(List<T> list, string listName, ref bool isOpen, Action requestSave,
            Action<T, int> CustomEvents = null, Action<int> OnAdd = null, Action<T, int> OnRemove = null)
        {
            isOpen = EditorGUILayout.Foldout(isOpen, $"<b>{listName}</b>",
                new GUIStyle(EditorStyles.foldout) {richText = true});
            if (isOpen)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (typeof(T) == typeof(bool))
                    {
                        bool val = Convert.ToBoolean(list[i]);
                        bool v = EditorGUILayout.Toggle($"Element {i}", val);
                        if(val != v)
                            requestSave.Invoke();
                        list[i] = (T) Convert.ChangeType(v, typeof(T));
                    }
                    else if (typeof(T) == typeof(short) || typeof(T) == typeof(int) ||
                             typeof(T) == typeof(ushort) || typeof(T) == typeof(uint))
                    {
                        int val = Convert.ToInt32(list[i]);
                        int v = EditorGUILayout.IntField($"Element {i}", val);
                        if(val != v)
                            requestSave.Invoke();
                        list[i] = (T) Convert.ChangeType(v, typeof(T));
                    }
                    else if (typeof(T) == typeof(decimal) || typeof(T) == typeof(double))
                    {
                        double val = Convert.ToDouble(list[i]);
                        double v = EditorGUILayout.DoubleField($"Element {i}", val);
                        if(val != v)
                            requestSave.Invoke();
                        list[i] = (T) Convert.ChangeType(v, typeof(T));
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        float val = (float) Convert.ToDouble(list[i]);
                        float v = EditorGUILayout.FloatField($"Element {i}", val);
                        if(val != v)
                            requestSave.Invoke();
                        list[i] = (T) Convert.ChangeType(v, typeof(T));
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        long val = Convert.ToInt64(list[i]);
                        long v = EditorGUILayout.LongField($"Element {i}", val);
                        if(val != v)
                            requestSave.Invoke();
                        list[i] = (T) Convert.ChangeType(v, typeof(T));
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        string val = Convert.ToString(list[i]);
                        string v = EditorGUILayout.TextField($"Element {i}", val);
                        if(val != v)
                            requestSave.Invoke();
                        list[i] = (T) Convert.ChangeType(v, typeof(T));
                    }
                    CustomEvents?.Invoke(list[i], i);
                    if (GUILayout.Button("Remove"))
                    {
                        OnRemove?.Invoke(list[i], i);
                        list.RemoveAt(i);
                        requestSave.Invoke();
                    }
                }
                NewGUILine();
                if (GUILayout.Button("Add New " + listName))
                {
                    list.Add(default);
                    OnAdd?.Invoke(list.Count - 1);
                    requestSave.Invoke();
                }
            }
        }

        public static void DrawSimpleList<T>(ref List<T> list, string listName, ref bool isOpen, Action requestSave,
            Func<T, string> GetIterationName, Func<T> CreateNewObject, Action<T, int> CustomEvents = null, Action<T, int> OnAdd = null,
            Action<T, int> OnRemove = null)
        {
            isOpen = EditorGUILayout.Foldout(isOpen, $"<b>{listName}</b>",
                new GUIStyle(EditorStyles.foldout) {richText = true});
            if (isOpen)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    GUILayout.Label(GetIterationName.Invoke(list[i]), EditorStyles.boldLabel);
                    foreach (FieldInfo fieldInfo in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
                    {
                        bool isPrimitive = fieldInfo.FieldType.IsPrimitive || fieldInfo.FieldType.IsValueType ||
                                           fieldInfo.FieldType == typeof(string);
                        if (!isPrimitive)
                        {
                            if (fieldInfo.FieldType == typeof(AnimationCurve))
                            {
                                if(fieldInfo.GetValue(list[i]) == null)
                                    fieldInfo.SetValue(list[i], CreateNewObject.Invoke());
                                AnimationCurve val = (AnimationCurve) fieldInfo.GetValue(list[i]);
                                AnimationCurve obj = EditorGUILayout.CurveField(fieldInfo.Name, val);
                                if(obj != null && !obj.Equals(val) || val != null && obj == null)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(Gradient))
                            {
                                if(fieldInfo.GetValue(list[i]) == null)
                                    fieldInfo.SetValue(list[i], CreateNewObject.Invoke());
                                Gradient val = (Gradient) fieldInfo.GetValue(list[i]);
                                Gradient obj = EditorGUILayout.GradientField(fieldInfo.Name, val);
                                if(obj != null && !obj.Equals(val) || val != null && obj == null)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else
                            {
                                Object val = (Object) fieldInfo.GetValue(list[i]);
                                Object obj =
                                    EditorGUILayout.ObjectField(fieldInfo.Name, val, fieldInfo.FieldType, false);
                                if(obj != null && !obj.Equals(val) || val != null && obj == null)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                        }
                        else
                        {
                            if (fieldInfo.FieldType == typeof(bool))
                            {
                                bool val = Convert.ToBoolean(fieldInfo.GetValue(list[i]));
                                bool v = EditorGUILayout.Toggle(fieldInfo.Name, val);
                                if(val != v)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], v);
                            }
                            else if (fieldInfo.FieldType == typeof(short) || fieldInfo.FieldType == typeof(int) ||
                                     fieldInfo.FieldType == typeof(ushort) || fieldInfo.FieldType == typeof(uint))
                            {
                                int val = Convert.ToInt32(fieldInfo.GetValue(list[i]));
                                int v = EditorGUILayout.IntField(fieldInfo.Name, val);
                                if(val != v)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], v);
                            }
                            else if (fieldInfo.FieldType == typeof(decimal) || fieldInfo.FieldType == typeof(double))
                            {
                                double val = Convert.ToDouble(fieldInfo.GetValue(list[i]));
                                double v = EditorGUILayout.DoubleField(fieldInfo.Name, val);
                                if(val != v)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], v);
                            }
                            else if (fieldInfo.FieldType == typeof(float))
                            {
                                float val = (float) Convert.ToDouble(fieldInfo.GetValue(list[i]));
                                float v = EditorGUILayout.FloatField(fieldInfo.Name, val);
                                if(val != v)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], v);
                            }
                            else if (fieldInfo.FieldType == typeof(long))
                            {
                                long val = Convert.ToInt64(fieldInfo.GetValue(list[i]));
                                long v = EditorGUILayout.LongField(fieldInfo.Name, val);
                                if(val != v)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], v);
                            }
                            else if (fieldInfo.FieldType == typeof(string))
                            {
                                string val = Convert.ToString(fieldInfo.GetValue(list[i]));
                                string v = EditorGUILayout.TextField(fieldInfo.Name, val);
                                if(val != v)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], v);
                            }
                            else if (fieldInfo.FieldType == typeof(Bounds))
                            {
                                Bounds val = (Bounds) fieldInfo.GetValue(list[i]);
                                Bounds obj = EditorGUILayout.BoundsField(fieldInfo.Name, val);
                                if(val != obj)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(BoundsInt))
                            {
                                BoundsInt val = (BoundsInt) fieldInfo.GetValue(list[i]);
                                BoundsInt obj = EditorGUILayout.BoundsIntField(fieldInfo.Name, val);
                                if(val != obj)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(Color))
                            {
                                Color val = (Color) fieldInfo.GetValue(list[i]);
                                Color obj = EditorGUILayout.ColorField(fieldInfo.Name, val);
                                if(val != obj)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(Rect))
                            {
                                Rect val = (Rect) fieldInfo.GetValue(list[i]);
                                Rect obj = EditorGUILayout.RectField(fieldInfo.Name, val);
                                if(val != obj)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(RectInt))
                            {
                                RectInt val = (RectInt) fieldInfo.GetValue(list[i]);
                                RectInt obj = EditorGUILayout.RectIntField(fieldInfo.Name, val);
                                if(!val.Equals(obj))
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(Vector2))
                            {
                                Vector2 val = (Vector2) fieldInfo.GetValue(list[i]);
                                Vector2 obj = EditorGUILayout.Vector2Field(fieldInfo.Name, val);
                                if(val != obj)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(Vector2Int))
                            {
                                Vector2Int val = (Vector2Int) fieldInfo.GetValue(list[i]);
                                Vector2Int obj = EditorGUILayout.Vector2IntField(fieldInfo.Name, val);
                                if(val != obj)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(Vector3))
                            {
                                Vector3 val = (Vector3) fieldInfo.GetValue(list[i]);
                                Vector3 obj = EditorGUILayout.Vector3Field(fieldInfo.Name, val);
                                if(val != obj)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(Vector3Int))
                            {
                                Vector3Int val = (Vector3Int) fieldInfo.GetValue(list[i]);
                                Vector3Int obj = EditorGUILayout.Vector3IntField(fieldInfo.Name, val);
                                if(val != obj)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType == typeof(Vector4))
                            {
                                Vector4 val = (Vector4) fieldInfo.GetValue(list[i]);
                                Vector4 obj = EditorGUILayout.Vector4Field(fieldInfo.Name, val);
                                if(val != obj)
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], obj);
                            }
                            else if (fieldInfo.FieldType.IsEnum)
                            {
                                Enum val = (Enum) fieldInfo.GetValue(list[i]);
                                Enum v = EditorGUILayout.EnumPopup(fieldInfo.Name, val);
                                if(val != null && !Equals(val, v))
                                    requestSave.Invoke();
                                fieldInfo.SetValue(list[i], v);
                            }
                        }
                    }
                    CustomEvents?.Invoke(list[i], i);
                    if (GUILayout.Button("Remove"))
                    {
                        OnRemove?.Invoke(list[i], i);
                        list.RemoveAt(i);
                        requestSave.Invoke();
                    }
                }
                NewGUILine();
                if (GUILayout.Button("Add New " + listName))
                {
                    T o = CreateNewObject.Invoke();
                    list.Add(o);
                    OnAdd?.Invoke(o, list.Count - 1);
                    requestSave.Invoke();
                }
            }
        }
        
        public static void DrawScriptEditorOnCustomEvent(LocalScript LocalScript, ref NexboxScript script)
        {
            EditorGUILayout.BeginHorizontal();
            if (ScriptEditorInstance.IsOpen)
            {
                if (GUILayout.Button("Open Script in Hypernex Script Editor"))
                {
                    ScriptEditorInstance scriptEditorInstance =
                        ScriptEditorInstance.GetInstanceFromScript(script);
                    if (scriptEditorInstance == null)
                        scriptEditorInstance = new ScriptEditorInstance(script,
                            s =>
                            {
                                // TODO fix Set Dirty (not threading issue)
                                InvokeOnMainThread((Action)(() =>
                                {
                                    LocalScript.NexboxScript.Script = s;
                                    EditorUtility.SetDirty(LocalScript.gameObject);
                                }));
                            });
                    scriptEditorInstance.CreateScript();
                }
            }
            else
                GUILayout.Label("Not Connected");

            if (GUILayout.Button("Open Script in Text Editor"))
                SimpleScriptEditor.ShowWindow(script, () => EditorUtility.SetDirty(LocalScript.gameObject));
            EditorGUILayout.EndHorizontal();
            NewGUILine();
        }

        public static void DrawScriptEditorOnCustomEvent(Avatar Avatar, ref NexboxScript script, int index)
        {
            EditorGUILayout.BeginHorizontal();
            if (ScriptEditorInstance.IsOpen)
            {
                if (GUILayout.Button("Open Script in Hypernex Script Editor"))
                {
                    ScriptEditorInstance scriptEditorInstance =
                        ScriptEditorInstance.GetInstanceFromScript(script);
                    if (scriptEditorInstance == null)
                        scriptEditorInstance = new ScriptEditorInstance(script,
                            s =>
                            {
                                // TODO fix Set Dirty (not threading issue)
                                InvokeOnMainThread((Action)(() =>
                                {
                                    Avatar.LocalAvatarScripts[index].Script = s;
                                    EditorUtility.SetDirty(Avatar.gameObject);
                                }));
                            });
                    scriptEditorInstance.CreateScript();
                }
            }
            else
                GUILayout.Label("Not Connected");

            if (GUILayout.Button("Open Script in Text Editor"))
                SimpleScriptEditor.ShowWindow(script, () => EditorUtility.SetDirty(Avatar.gameObject));
            EditorGUILayout.EndHorizontal();
            NewGUILine();
        }
        
        public static void DrawScriptEditorOnCustomEvent(World World, ref NexboxScript script, int index)
        {
            EditorGUILayout.BeginHorizontal();
            if (ScriptEditorInstance.IsOpen)
            {
                if (GUILayout.Button("Open Script in Hypernex Script Editor"))
                {
                    ScriptEditorInstance scriptEditorInstance =
                        ScriptEditorInstance.GetInstanceFromScript(script);
                    if (scriptEditorInstance == null)
                        scriptEditorInstance = new ScriptEditorInstance(script,
                            s =>
                            {
                                // TODO fix Set Dirty (not threading issue)
                                InvokeOnMainThread((Action)(() =>
                                {
                                    World.ServerScripts[index].Script = s;
                                    EditorUtility.SetDirty(World.gameObject);
                                }));
                            });
                    scriptEditorInstance.CreateScript();
                }
            }
            else
                GUILayout.Label("Not Connected");

            if (GUILayout.Button("Open Script in Text Editor"))
                SimpleScriptEditor.ShowWindow(script, () => EditorUtility.SetDirty(World.gameObject));
            EditorGUILayout.EndHorizontal();
            NewGUILine();
        }

        public static void MakeSave(Scene? s = null)
        {
            if (!s.HasValue)
                s = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(s.Value);
            EditorSceneManager.SaveScene(s.Value);
            AssetDatabase.SaveAssets();
        }

        public static void MakeSave(Object o, Scene? s = null)
        {
            if (!s.HasValue)
                s = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(s.Value);
            if(o != null)
                EditorUtility.SetDirty(o);
            EditorSceneManager.SaveScene(s.Value);
            AssetDatabase.SaveAssets();
        }

        public static string BuildAssetBundle(Avatar avatar, TempDir tempDir)
        {
            bool didSavePrefab;
            string uploadTypeString = "avatar_";
            string uploadFileType = "hna";
            AssetIdentifier assetIdentifier = avatar.gameObject.GetComponent<AssetIdentifier>();
            string id = assetIdentifier.GetId();
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
            EditorUtility.DisplayDialog("Hypernex.CCK",
                "Failed to build! Please see console for more information.", "OK");
            return String.Empty;
        }

        private static List<NexboxScript> CloneServerScripts(List<NexboxScript> s)
        {
            List<NexboxScript> nexboxScripts = new List<NexboxScript>();
            s.ForEach(x => nexboxScripts.Add(new NexboxScript(x.Language, x.Script){Name = x.Name}));
            return nexboxScripts;
        }
        
        private static List<NexboxScript> oldServerScripts;
        
        public static (string, List<NexboxScript>) BuildAssetBundle(World w, TempDir tempDir)
        {
            oldServerScripts = CloneServerScripts(w.ServerScripts);
            string uploadTypeString = "world_";
            string uploadFileType = "hnw";
            AssetIdentifier assetIdentifier = w.gameObject.GetComponent<AssetIdentifier>();
            string id = assetIdentifier.GetId();
            if (string.IsNullOrEmpty(id))
                id = uploadTypeString + "temp_" + Guid.NewGuid();
            tempDir.CreateChildDirectory("assetbundle");
            string abp = Path.Combine(tempDir.GetPath(), "assetbundle");
            Scene currentScene = SceneManager.GetActiveScene();
            w.ServerScripts.Clear();
            /*EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());*/
            if(w != null)
                MakeSave(w);
            string[] assets = { currentScene.path };
            AssetBundleBuild[] builds = new AssetBundleBuild[1];
            builds[0].assetBundleName = id;
            builds[0].assetNames = assets;
            builds[0].assetBundleVariant = uploadFileType;
            BuildPipeline.BuildAssetBundles(abp, builds, BuildAssetBundleOptions.ChunkBasedCompression,
                EditorUserBuildSettings.activeBuildTarget);
            EditorSceneManager.OpenScene(currentScene.path);
            if(w != null)
                MakeSave(w);
            /*EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());*/
            foreach (string assetBundle in Directory.GetFiles(abp))
            {
                string assetBundleName = Path.GetFileName(assetBundle);
                if (assetBundleName == $"{id}.{uploadFileType}")
                {
                    return (assetBundle, oldServerScripts);
                }
            }
            Logger.CurrentLogger.Error("Target AssetBundle did not exist!");
            // Failed to Copy File
            EditorUtility.DisplayDialog("Hypernex.CCK",
                "Failed to build! Please see console for more information.", "OK");
            return (String.Empty, new List<NexboxScript>());
        }
    }
}