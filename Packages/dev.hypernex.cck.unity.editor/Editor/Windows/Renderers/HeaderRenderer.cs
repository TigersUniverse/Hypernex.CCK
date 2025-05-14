using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Windows.Renderers
{
    public class HeaderRenderer : IRenderer
    {
        public RendererResult OnGUI()
        {
            float maxXsize = 1920f / 4f;
            float maxYsize = 800f / 4f;
            float windowWidth = EditorGUIUtility.currentViewWidth;
            float drawWidth = Mathf.Min(maxXsize, windowWidth - 20);
            float scaleFactor = drawWidth / maxXsize;
            float drawHeight = maxYsize * scaleFactor;
            Rect r = EditorGUILayout.GetControlRect(false, drawHeight);
            r.width = drawWidth;
            r.height = drawHeight;
            r.x = (windowWidth - drawWidth) / 2;
            EditorGUI.DrawPreviewTexture(r, EditorUtils.GetResource<Texture2D>("Hypernex_CCK"));
            r.y += drawHeight;
            EditorGUILayout.Separator();
            return RendererResult.RenderedOnTop;
        }
    }
}