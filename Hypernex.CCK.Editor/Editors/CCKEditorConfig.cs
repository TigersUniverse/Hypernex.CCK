using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hypernex.CCK.Editor.Editors.Tools;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Editor.Editors
{
    public class CCKEditorConfig : EditorWindow
    {
        private const string DEFAULT_SCRIPT_EDITOR_LOCATION = "Hypernex.CCK.ScriptEditor";
        
        [MenuItem("Hypernex.CCK/Config")]
        private static void ShowWindow()
        {
            CCKEditorConfig Window = GetWindow<CCKEditorConfig>();
            Window.titleContent = new GUIContent("CCK Config");
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

        private void StartAppWithWait(string path)
        {
            if (!File.Exists(path))
                return;
            Type t = typeof(ScriptEditorInstance);
            int p = (int) t.GetMethod("GetHTTPPort", BindingFlags.Static | BindingFlags.Public)?
                .Invoke(null, Array.Empty<object>())!;
            FieldInfo appField = t.GetField("app", BindingFlags.Static | BindingFlags.NonPublic);
            Process app = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = Path.GetDirectoryName(path),
                    Arguments = "-port " + p,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            appField?.SetValue(null, app);
            bool b = app.Start();
            if (b)
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(2000);
                    WebClient webClient = new WebClient();
                    int wp = Convert.ToInt32(webClient.DownloadString("http://localhost:" + p + "/getWSPort"));
                    object[] par = new object[2]
                    {
                        wp,
                        null
                    };
                    t.GetMethod("InitSocket", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(null, par);
                });
            }
        }

        private void RenderScriptEditorConfig()
        {
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
                    if(Application.platform == RuntimePlatform.WindowsEditor)
                        ScriptEditorInstance.StartApp(EditorConfig.LoadedConfig.ScriptEditorLocation);
                    else
                        StartAppWithWait(EditorConfig.LoadedConfig.ScriptEditorLocation);
                    Close();
                }
            }
            else
                GUILayout.Label("Already Connected!", EditorStyles.miniLabel);
            GUILayout.Label("Installed Version: " + lastReadVersion, EditorStyles.miniBoldLabel);
        }

        private void RenderAdvancedConfig()
        {
            GUILayout.Label("Hypernex Advanced Config", EditorStyles.miniBoldLabel);
            EditorConfig.LoadedConfig.UseHTTP =
                GUILayout.Toggle(EditorConfig.LoadedConfig.UseHTTP, "Allow HTTP Servers");
            if (EditorConfig.LoadedConfig.OverrideSecurity)
                EditorGUILayout.HelpBox(
                    "Component Security prevents you from uploading any invalid components. Any invalid components will be removed in-game.",
                    MessageType.Warning);
            EditorConfig.LoadedConfig.OverrideSecurity = GUILayout.Toggle(EditorConfig.LoadedConfig.OverrideSecurity,
                "Override Component Security");
        }

        private void OnGUI()
        {
            EditorConfig.GetConfig();
            RenderScriptEditorConfig();
            GUILayout.Space(5);
            RenderAdvancedConfig();
        }
    }
}