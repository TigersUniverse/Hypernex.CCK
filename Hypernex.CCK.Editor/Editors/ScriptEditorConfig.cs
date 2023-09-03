using System;
using System.IO;
using System.Linq;
using Hypernex.CCK.Editor.Editors.Tools;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Editor.Editors
{
    public class ScriptEditorConfig : EditorWindow
    {
        private const string DEFAULT_SCRIPT_EDITOR_LOCATION = "Hypernex.CCK.ScriptEditor";
        
        [MenuItem("Hypernex.CCK/ScriptEditor Config")]
        private static void ShowWindow()
        {
            ScriptEditorConfig Window = GetWindow<ScriptEditorConfig>();
            Window.titleContent = new GUIContent("ScriptEditor Config");
            Window.lastReadVersion = Window.GetVersion();
        }

        private static int ScriptEditorArtifact
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        return 0;
                    case RuntimePlatform.LinuxEditor:
                        return 1;
                    default:
                        throw new Exception("Unsupported Platform " + Application.platform);
                }
            }
        }

        private static bool isDownloading;

        private string lastReadVersion;

        private string GetVersion()
        {
            string file = Path.Combine(DEFAULT_SCRIPT_EDITOR_LOCATION, "version.txt");
            return !File.Exists(file) ? String.Empty : File.ReadAllText(file);
        }

        private static string[] LibraryExtensions = new[]
        {
            ".dll",
            ".so",
            ".dylib"
        };

        private string GetExecutable(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                if (Path.GetFileNameWithoutExtension(file) == "Hypernex.CCK.ScriptEditor" && !LibraryExtensions.Contains(Path.GetExtension(file)))
                    return file;
            }
            return Path.Combine(path, "Hypernex.CCK.ScriptEditor.exe");
        }

        private void DownloadAndInstallScriptEditor()
        {
            if (AuthManager.CurrentUser == null)
                throw new Exception("Cannot Download Script Editor until Logged In!");
            isDownloading = true;
            AuthManager.Instance.HypernexObject.GetVersions(result =>
            {
                if (!result.success)
                    return;
                string latest = result.result.Versions.First();
                if (latest == GetVersion())
                {
                    EditorTools.InvokeOnMainThread(new Action(() =>
                    {
                        isDownloading = false;
                        EditorUtility.DisplayDialog("Hypernex.CCK", "Version already Up to Date!", "OK");
                    }));
                    return;
                }
                if(Directory.Exists(DEFAULT_SCRIPT_EDITOR_LOCATION))
                    Directory.Delete(DEFAULT_SCRIPT_EDITOR_LOCATION, true);
                Directory.CreateDirectory(DEFAULT_SCRIPT_EDITOR_LOCATION);
                AuthManager.Instance.HypernexObject.GetBuild(buildStream =>
                    {
                        string zip = Path.Combine(DEFAULT_SCRIPT_EDITOR_LOCATION, "build.zip");
                        if(File.Exists(zip))
                            File.Delete(zip);
                        FileStream fs = new FileStream(zip, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                            FileShare.ReadWrite | FileShare.Delete);
                        buildStream.CopyTo(fs);
                        buildStream.Dispose();
                        fs.Dispose();
                        try
                        {
                            System.IO.Compression.ZipFile.ExtractToDirectory(zip, DEFAULT_SCRIPT_EDITOR_LOCATION);
                        }
                        catch (Exception e)
                        {
                            EditorTools.InvokeOnMainThread(new Action(() =>
                            {
                                isDownloading = false;
                                EditorUtility.DisplayDialog("Hypernex.CCK", e.ToString(), "OK");
                            }));
                        }
                        File.WriteAllText(Path.Combine(DEFAULT_SCRIPT_EDITOR_LOCATION, "version.txt"), latest);
                        EditorTools.InvokeOnMainThread(new Action(() =>
                        {
                            EditorConfig.LoadedConfig.ScriptEditorLocation = GetExecutable(DEFAULT_SCRIPT_EDITOR_LOCATION);
                            EditorConfig.SaveConfig(EditorConfig.GetEditorConfigLocation());
                            isDownloading = false;
                            EditorUtility.DisplayDialog("Hypernex.CCK",
                                "Downloaded and Installed Hypernex.CCK.ScriptEditor", "OK");
                        }));
                    }, "Hypernex.CCK.ScriptEditor", latest, ScriptEditorArtifact, AuthManager.CurrentUser,
                    AuthManager.CurrentToken);
            }, "Hypernex.CCK.ScriptEditor");
        }

        private void OnGUI()
        {
            EditorConfig c = EditorConfig.GetConfig();
            GUILayout.Label("Hypernex Script Editor", EditorStyles.miniBoldLabel);
            if (!ScriptEditorInstance.IsOpen)
            {
                EditorConfig.LoadedConfig.ScriptEditorLocation =
                    EditorGUILayout.TextField("Script Editor Location",
                        EditorConfig.LoadedConfig.ScriptEditorLocation);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select Script Editor"))
                    EditorConfig.LoadedConfig.ScriptEditorLocation =
                        EditorUtility.OpenFilePanel("Select Hypernex Script Editor", "", "");
                if (!isDownloading)
                {
                    if(GUILayout.Button("Download and Install Script Editor"))
                        DownloadAndInstallScriptEditor();
                }
                else
                    GUILayout.Label("Downloading...");
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Start Script Editor"))
                {
                    EditorConfig.SaveConfig(EditorConfig.GetEditorConfigLocation());
                    ScriptEditorInstance.StartApp(EditorConfig.LoadedConfig.ScriptEditorLocation);
                    Close();
                }
            }
            else
                GUILayout.Label("Already Connected!", EditorStyles.miniLabel);
            GUILayout.Label("Installed Version: " + lastReadVersion, EditorStyles.miniBoldLabel);
        }
    }
}