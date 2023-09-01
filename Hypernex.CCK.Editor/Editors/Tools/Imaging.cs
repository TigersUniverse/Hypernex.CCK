using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Editor.Editors.Tools
{
    public class Imaging
    {
        private static Texture2D cachedHeaderImage;

        private static Texture2D HeaderImage
        {
            get
            {
                if (cachedHeaderImage != null)
                    return cachedHeaderImage;
                Texture2D t = new Texture2D(1, 1);
                Stream stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Hypernex.CCK.Editor.Resources.banner.png");
                if (stream != null)
                {
                    MemoryStream ms = new MemoryStream();
                    stream.CopyTo(ms);
                    t = new Texture2D(50, 50);
                    t.LoadImage(ms.ToArray());
                    t.Apply();
                    ms.Dispose();
                    stream.Dispose();
                }
                else
                    Debug.Log("Could not load header!");
                cachedHeaderImage = t;
                return t;
            }
        }

        public static (FileStream, Texture2D)? GetBitmapFromAsset(Object asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return null;
            FileStream fileStream = new FileStream(assetPath, FileMode.Open,
                FileAccess.ReadWrite, FileShare.Delete | FileShare.ReadWrite);
            MemoryStream ms = new MemoryStream();
            fileStream.CopyTo(ms);
            Texture2D t = new Texture2D(1, 1);
            t.LoadImage(ms.ToArray());
            t.Apply();
            ms.Dispose();
            return (fileStream, t);
        }
        
        public static void DrawHeader(Vector2 windowSize)
        {
            // Image Scaling
            float maxXsize = 400;
            float maxYsize = 200;
            float imagesizeX = windowSize.x - 20;
            float imagesizeY = windowSize.x / 2 - 20;
            if (imagesizeX > maxXsize)
                imagesizeX = maxXsize;
            if (imagesizeY > maxYsize)
                imagesizeY = maxYsize;
            Rect imageLayout = new Rect((windowSize.x - imagesizeX) / 2, 10, imagesizeX, imagesizeY);
            Rect area = new Rect(0, imagesizeY + 20, windowSize.x, windowSize.y - imagesizeY - 20);
            // Draw the Image
            EditorGUI.DrawPreviewTexture(imageLayout, HeaderImage);
            // Set an area
            GUILayout.BeginArea(area);
        }
    }
}