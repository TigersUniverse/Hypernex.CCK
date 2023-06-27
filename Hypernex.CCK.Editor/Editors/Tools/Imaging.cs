using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bitmap = System.Drawing.Bitmap;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Editor.Editors.Tools
{
    public class Imaging
    {
        private static Dictionary<string, Texture2D> CachedImages = new Dictionary<string, Texture2D>();
        
        public static Bitmap DownloadImage(string url)
        {
            WebClient webClient = new WebClient();
            byte[] data = webClient.DownloadData(url);
            return new Bitmap(new MemoryStream(data));
        }

        [Obsolete("Unreliable when it comes to reading sprite data. Use GetBitmapFromAsset instead.")]
        public static Bitmap SpriteToBitmap(Sprite sprite)
        {
            Bitmap b = new Bitmap(sprite.texture.width, sprite.texture.height);
            for (int x = 0; x < sprite.texture.width; x++)
            {
                for (int y = 0; y < sprite.texture.height; y++)
                {
                    Color pixel = sprite.texture.GetPixel(x, sprite.texture.height - y);
                    System.Drawing.Color newPixel = System.Drawing.Color.FromArgb((int) (pixel.a * 255.0f),
                        (int) (pixel.r * 255.0f), (int) (pixel.g * 255.0f), (int) (pixel.b * 255.0f));
                    b.SetPixel(x, y, newPixel);
                }
            }
            return b;
        }

        public static (FileStream, Bitmap)? GetBitmapFromAsset(Object asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return null;
            FileStream fileStream = new FileStream(assetPath, FileMode.Open,
                FileAccess.ReadWrite, FileShare.Delete | FileShare.ReadWrite);
            Bitmap bitmap = new Bitmap(fileStream);
            return (fileStream, bitmap);
        }
        
        public static Texture2D TextureFromBitmap(Bitmap image)
        {
            Texture2D texture = new Texture2D(image.Width, image.Height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Trilinear
            };
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    System.Drawing.Color pixelColor = image.GetPixel(x, y);
                    UnityEngine.Color unity_pixelColor =
                        new UnityEngine.Color(pixelColor.R / 255.0f, pixelColor.G / 255.0f, 
                            pixelColor.B / 255.0f, pixelColor.A / 255.0f);
                    texture.SetPixel(x, image.Height - y, unity_pixelColor);
                }
            }
            texture.Apply();
            return texture;
        }
        
        public static void DrawHeader(string name, Vector2 windowPosition, Vector2 windowSize)
        {
            Bitmap image = null;
            Texture2D texture = null;
            if (CachedImages.ContainsKey(name))
                texture = CachedImages[name];
            // Get image and draw texture
            if (texture == null)
            {
                using (Stream stream = Assembly.GetExecutingAssembly()
                           .GetManifestResourceStream(name))
                {
                    if (stream != null)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            image = new Bitmap(ms);
                        }
                    }
                }
            }
            // Check if we need to convert the bytes to a texture
            if (texture == null && image != null)
            {
                texture = new Texture2D(image.Width, image.Height, TextureFormat.ARGB32, false)
                {
                    filterMode = FilterMode.Trilinear
                };
                for (int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        System.Drawing.Color pixelColor = image.GetPixel(x, y);
                        UnityEngine.Color unity_pixelColor =
                            new UnityEngine.Color(pixelColor.R / 255.0f, pixelColor.G / 255.0f, 
                                pixelColor.B / 255.0f, pixelColor.A / 255.0f);
                        texture.SetPixel(x, image.Height - y, unity_pixelColor);
                    }
                }
                texture.Apply();
            }
            if (!CachedImages.ContainsKey(name))
                CachedImages.Add(name, texture);
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
            EditorGUI.DrawPreviewTexture(imageLayout, texture);
            // Set an area
            GUILayout.BeginArea(area);
        }
    }
}