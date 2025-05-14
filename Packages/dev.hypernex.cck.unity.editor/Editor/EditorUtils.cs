using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Hypernex.CCK.Unity.Auth;
using Hypernex.CCK.Unity.Internals;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Unity.Editor
{
    [InitializeOnLoad]
    public static class EditorUtils
    {
        private static readonly ReadOnlyDictionary<BuildTarget, GraphicsDeviceType[]> GraphicsAPIs =
            new ReadOnlyDictionary<BuildTarget, GraphicsDeviceType[]>(new Dictionary<BuildTarget, GraphicsDeviceType[]>
            {
                [BuildTarget.StandaloneWindows64] = new[]
                {
                    GraphicsDeviceType.Direct3D12,
                    GraphicsDeviceType.Direct3D11,
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLCore
                },
                [BuildTarget.StandaloneWindows] = new[]
                {
                    GraphicsDeviceType.Direct3D12,
                    GraphicsDeviceType.Direct3D11,
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLCore
                },
                [BuildTarget.StandaloneOSX] = new[]
                {
                    GraphicsDeviceType.Metal
                },
                [BuildTarget.EmbeddedLinux] = new[]
                {
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLCore
                },
                [BuildTarget.StandaloneLinux64] = new[]
                {
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLCore
                },
                [BuildTarget.Android] = new []
                {
                    GraphicsDeviceType.Vulkan
                },
                [BuildTarget.iOS] = new[]
                {
                    GraphicsDeviceType.Metal
                }
            });
        
        private static Texture2D hoverBackground;

        private static Texture2D HoverBackground
        {
            get
            {
                if (hoverBackground != null) return hoverBackground;
                hoverBackground = MakeTex(2, 2, new Color(0, 0, 0, 0.5f));
                return hoverBackground;
            }
        }
        
        private static Texture2D activeBackground;

        private static Texture2D ActiveBackground
        {
            get
            {
                if (activeBackground != null) return activeBackground;
                activeBackground = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));
                return activeBackground;
            }
        }
        
        private static Texture2D transparentBackground;

        private static Texture2D TransparentBackground
        {
            get
            {
                if (transparentBackground != null) return transparentBackground;
                transparentBackground = MakeTex(2, 2, new Color(0, 0, 0, 0f));
                return transparentBackground;
            }
        }
        
        public static GUIStyle TextureStyle(bool isHovered) => new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            normal = new GUIStyleState()
            {
                textColor = new Color(1, 1, 1, isHovered ? 1f : 0f),
                background = TransparentBackground
            }, // Show text only on hover
            hover = new GUIStyleState() { textColor = Color.white, background = HoverBackground }, // Semi-transparent on hover
            active = new GUIStyleState() { textColor = Color.gray, background = ActiveBackground } // Darker when clicked
        };

        private static Dictionary<string, Object> objcache = new Dictionary<string, Object>();

        public static T GetResource<T>(string name) where T : Object
        {
            if (objcache.TryGetValue(name, out Object o))
                return (T) o;
            T obj = Resources.Load<T>(name);
            if (obj == null) return null;
            objcache.Add(name, obj);
            return obj;
        }

        public static T PullFromCache<T>(string name, Func<T> create) where T : Object
        {
            if (objcache.TryGetValue(name, out Object c))
                return (T) c;
            T o = create.Invoke();
            if (o == null) return null;
            objcache.Add(name, o);
            return o;
        }
        
        private static UnityLogger logger;
        private static UserAuth auth;
        
        static EditorUtils()
        {
            logger = new UnityLogger();
            logger.SetLogger();
            AuthConfig authConfig = AuthConfig.GetConfig();
            if (authConfig.SavedAuth)
                Auth(authConfig);
            foreach (KeyValuePair<BuildTarget,GraphicsDeviceType[]> graphicsAPI in GraphicsAPIs)
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(graphicsAPI.Key),
                        graphicsAPI.Key)) continue;
                bool compareA = Compare(PlayerSettings.GetGraphicsAPIs(graphicsAPI.Key), graphicsAPI.Value);
                bool compareB = PlayerSettings.GetUseDefaultGraphicsAPIs(graphicsAPI.Key);
                PlayerSettings.SetUseDefaultGraphicsAPIs(graphicsAPI.Key, false);
                PlayerSettings.SetGraphicsAPIs(graphicsAPI.Key, graphicsAPI.Value);
                if(!compareA || compareB)
                {
                    EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                    new Thread(() =>
                    {
                        // TODO: Make this so it knows when to exit?
                        Thread.Sleep(7000);
                        InvokeOnMainThread(new Action(() => EditorApplication.Exit(0)));
                    }).Start();
                }
            }
        }
        
        private static void InvokeOnMainThread(Delegate d)
        {
            void Callback() => d.DynamicInvoke();
            EditorApplication.delayCall += Callback;
        }
        
        private static bool Compare(GraphicsDeviceType[] a, GraphicsDeviceType[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                GraphicsDeviceType ag = a[i];
                GraphicsDeviceType bg = b[i];
                if (ag != bg)
                    return false;
            }
            return true;
        }

        internal static UserAuth Auth(AuthConfig authConfig)
        {
            auth = new UserAuth(authConfig);
            return auth;
        }

        public static void SimpleDialog(string content) =>
            EditorUtility.DisplayDialog("Hypernex.CCK.Unity", content, "OK");
        
        public static void DrawTitle(string content)
        {
            GUILayout.Label(content, EditorStyles.boldLabel);
            Rect r = EditorGUILayout.GetControlRect(false, 2f);
            EditorGUI.DrawRect(r, Color.gray);
        }
        
        public static void DrawSpecialHelpBox(MessageType messageType, string t, float h = 24f)
        {
            Rect r = EditorGUILayout.GetControlRect(false, h);
            EditorGUI.HelpBox(r, t, messageType);
        }
        
        public static void Line()
        {
            Rect r = EditorGUILayout.GetControlRect(false, 2f);
            EditorGUI.DrawRect(r, Color.gray);
        }

        public static bool PropertyField(SerializedProperty property, string label) =>
            EditorGUILayout.PropertyField(property, new GUIContent(label));

        public static void DrawReorderableListHeader(Rect rect, string text, float left = 25)
        {
            EditorGUI.PrefixLabel(new Rect(left, rect.y, rect.width, rect.height), new GUIContent(text));
            // TODO: I refuse to believe there isn't a better way to do this
            EditorGUI.Toggle(new Rect(0, 0, 0, 0), false);
        }

        private static readonly Color darkRed = new Color(0.7f, 0, 0, 1f);
        private static readonly Color lightPlaceholderColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color darkPlaceholderColor = new Color(0.5f, 0.5f, 0.5f);
        private static GUIStyle placeholderStyle => new GUIStyle(GUI.skin.label)
        {
            normal = {textColor = EditorGUIUtility.isProSkin ? darkPlaceholderColor : lightPlaceholderColor},
            hover = {textColor = EditorGUIUtility.isProSkin ? darkPlaceholderColor : lightPlaceholderColor},
            //fontStyle = FontStyle.Italic
        };
        
        public static string DrawExtraTextField(string label, string text, string placeholder = "", bool required = false, float spacing = 151.8f)
        {
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textField)
            {
                focused = new GUIStyleState{textColor = EditorStyles.textField.focused.textColor}
            };
            float minHeight = EditorGUIUtility.singleLineHeight;
            float calculatedHeight = textAreaStyle.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - spacing);
            float height = Mathf.Max(minHeight, calculatedHeight);
            Rect r = EditorGUILayout.GetControlRect(false, height);
            r.height = EditorGUIUtility.singleLineHeight;
            r.x -= 1;
            bool isEmpty = string.IsNullOrEmpty(text);
            if (required)
            {
                Color labelColor = isEmpty ? darkRed : GUI.skin.label.normal.textColor;
                GUI.Label(r, label, new GUIStyle(GUI.skin.label)
                {
                    normal = {textColor = labelColor},
                    hover = {textColor = labelColor},
                    fontStyle = isEmpty ? FontStyle.Bold : FontStyle.Normal
                });
            }
            else
                GUI.Label(r, label);
            r.x += 1;
            r.height = height;
            r.x += spacing;
            r.width -= spacing;
            if(isEmpty && required)
                GUI.color = EditorGUIUtility.isProSkin ? Color.red : darkRed;
            string t = EditorGUI.TextField(r, text, textAreaStyle);
            if(isEmpty && required)
                GUI.color = Color.white;
            if (isEmpty)
            {
                r.x += 1;
                GUI.Label(r, placeholder, placeholderStyle);
                r.x -= 1;
            }
            return t;
        }

        public static string DrawProperTextArea(string label, string text, string placeholder = "", bool required = false, float spacing = 151.8f)
        {
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textField);
            float minHeight = EditorGUIUtility.singleLineHeight;
            float calculatedHeight = textAreaStyle.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - spacing);
            float height = Mathf.Max(minHeight, calculatedHeight);
            Rect r = EditorGUILayout.GetControlRect(false, height);
            r.height = EditorGUIUtility.singleLineHeight;
            r.x -= 1;
            bool isEmpty = string.IsNullOrEmpty(text);
            if (required)
            {
                Color labelColor = isEmpty ? darkRed : GUI.skin.label.normal.textColor;
                GUI.Label(r, label, new GUIStyle(GUI.skin.label)
                {
                    normal = {textColor = labelColor},
                    hover = {textColor = labelColor},
                    fontStyle = isEmpty ? FontStyle.Bold : FontStyle.Normal
                });
            }
            else
                GUI.Label(r, label);
            r.x += 1;
            r.height = height;
            r.x += spacing;
            r.width -= spacing;
            if(isEmpty && required)
                GUI.color = EditorGUIUtility.isProSkin ? Color.red : darkRed;
            string t = EditorGUI.TextArea(r, text, textAreaStyle);
            if(isEmpty && required)
                GUI.color = Color.white;
            if (isEmpty)
            {
                r.x += 1;
                GUI.Label(r, placeholder, placeholderStyle);
                r.x -= 1;
            }
            return t;
        }

        public static void SimpleDrawList<T>(ref List<T> list, ref ReorderableList reorderableList, string header, Action<Rect, int, bool, bool> draw, Func<int, float> height)
        {
            reorderableList = new ReorderableList(list, typeof(T), true, true, true, true);
            reorderableList.drawHeaderCallback += rect => DrawReorderableListHeader(rect, header, 5f);
            reorderableList.drawElementCallback += draw.Invoke;
            reorderableList.elementHeightCallback += height.Invoke;
        }

        public static bool RightButton(string text, ButtonIcon buttonIcon, float height = 24)
        {
            Rect r = EditorGUILayout.GetControlRect(false, height);
            bool b = GUI.Button(r, text, new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            });
            r.x = r.width - height;
            r.width = height;
            GUI.DrawTexture(r, GetResource<Texture2D>(buttonIcon.ButtonIconToString()));
            return b;
        }

        public static bool LeftButton(string text, ButtonIcon buttonIcon, float height = 24)
        {
            Rect r = EditorGUILayout.GetControlRect(false, height);
            bool b = GUI.Button(r, text);
            r.x += 1;
            r.width = height;
            GUI.DrawTexture(r, GetResource<Texture2D>(buttonIcon.ButtonIconToString()));
            return b;
        }

        public static int IconPopout(string text, ButtonIcon buttonIcon, int index, string[] options, float height = 24)
        {
            Rect r = EditorGUILayout.GetControlRect(false, height);
            float fullWidth = r.width;
            r.width = height;
            GUI.DrawTexture(r, GetResource<Texture2D>(buttonIcon.ButtonIconToString()));
            r.width = fullWidth;
            r.x += height + 10;
            r.y += 2;
            r.width -= height + 10;
            r = EditorGUI.PrefixLabel(r, new GUIContent(text), new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft
            });
            r.y -= 1;
            return EditorGUI.Popup(r, index, options, new GUIStyle(EditorStyles.popup)
            {
                fixedHeight = height
            });
        }
        
        public static bool LeftToggleButton(GUIContent guiContent, ButtonIcon buttonIcon, float height = 24)
        {
            Rect r = EditorGUILayout.GetControlRect(false, height);
            bool b = GUI.Button(r, guiContent);
            r.x +=  1;
            r.width = height;
            r.y -= 0.5175f;
            GUI.DrawTexture(r, GetResource<Texture2D>(buttonIcon.ButtonIconToString()));
            return b;
        }

        public static bool LargeToggle(string text, bool v)
        {
            GUIStyle labelWordWrap = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };
            float height = labelWordWrap.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth + EditorGUIUtility.singleLineHeight);
            Rect r = EditorGUILayout.GetControlRect(false, height);
            float lastY = r.y;
            //r.y = height / 2 + lastY;
            bool v2 = EditorGUI.Toggle(r, v);
            r.y = lastY;
            r.x += EditorGUIUtility.singleLineHeight;
            r.width -= EditorGUIUtility.singleLineHeight;
            r.height = height;
            GUI.Label(r, text, labelWordWrap);
            return v2;
        }

        public static void SimpleThumbnail(ref Texture2D texture, ref Camera camera, ref bool replaceTexture, string at)
        {
            Rect r = EditorGUILayout.GetControlRect(false, 100);
            if(texture == null)
                texture = GetResource<Texture2D>("Hypernex_bg");
            r.width = 100 * ((float) texture.width / texture.height);
            EditorGUI.DrawPreviewTexture(r, texture);
            r.y -= 1;
            r.height -= 1;
            bool isHovered = r.Contains(Event.current.mousePosition);
            if (GUI.Button(r, "Select Image\n(16:9)", TextureStyle(isHovered)))
            {
                Texture2D selected = SelectImage();
                if (selected != null)
                {
                    texture = selected;
                    replaceTexture = true;
                }
                else
                    replaceTexture = false;
            }
            float imgw = r.width;
            r.width = EditorGUIUtility.currentViewWidth - imgw;
            r.height = EditorGUIUtility.singleLineHeight;
            r.x = imgw + 5;
            r.width -= 20;
            GUI.Label(r, "Thumbnail", EditorStyles.boldLabel);
            r.y += 16;
            r.height = 2;
            EditorGUI.DrawRect(r, Color.gray);
            r.height = EditorGUIUtility.singleLineHeight * 2;
            r.y += 5;
            GUI.Label(r, $"Select the Thumbnail you will use with your {at}.", new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            });
            r.height = EditorGUIUtility.singleLineHeight;
            r.y += EditorGUIUtility.singleLineHeight * 2 + 4;
            camera = (Camera) EditorGUI.ObjectField(r, camera, typeof(Camera), true);
            r.y += EditorGUIUtility.singleLineHeight + 5;
            if(camera == null)
                GUI.Label(r, "Please Select a Camera", EditorStyles.centeredGreyMiniLabel);
            else if (GUI.Button(r, "Capture from Selected Camera"))
                texture = RTImage(camera);
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        private static Texture2D RTImage(Camera mCamera, int mWidth = 600, int mHeight = 300)
        {
            RenderTexture renderTexture = new RenderTexture(mWidth, mHeight, 24);
            renderTexture.Create();
            Texture2D screenShot = new Texture2D(mWidth, mHeight, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture previousCamera = mCamera.targetTexture;
            try
            {
                mCamera.targetTexture = renderTexture;
                RenderPipeline.SubmitRenderRequest(mCamera, new RenderPipeline.StandardRequest
                {
                    destination = renderTexture,
                });
                RenderTexture.active = renderTexture;
                screenShot.ReadPixels(new Rect(0, 0, mWidth, mHeight), 0, 0);
                screenShot.Apply();
            }
            finally
            {
                mCamera.targetTexture = previousCamera;
                RenderTexture.active = previous;
                Object.DestroyImmediate(renderTexture);
            }
            return screenShot;
        }

        public static Texture2D SelectImage()
        {
            string path = EditorUtility.OpenFilePanel("Please select an Image", String.Empty, "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path)) return null;
            byte[] data = File.ReadAllBytes(path);
            Texture2D t = new Texture2D(0, 0);
            t.name = Path.GetFileName(path);
            t.LoadImage(data, false);
            return t;
        }
        
        public static Texture2D Resize(Texture2D texture2D,int targetX,int targetY)
        {
            RenderTexture rt=new RenderTexture(targetX, targetY,24);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(texture2D,rt);
            Texture2D result=new Texture2D(targetX,targetY);
            result.ReadPixels(new Rect(0,0,targetX,targetY),0,0);
            result.Apply();
            RenderTexture.active = previous;
            return result;
        }
    }
}