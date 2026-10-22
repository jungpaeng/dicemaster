using UnityEngine;

namespace DiceMaster.Information
{
    internal static class InformationSpinner
    {
        private static Texture2D image;
        private static bool attempted;
        internal static void Release()
        { if (image != null) UnityEngine.Object.Destroy(image); image = null; attempted = false; }
        internal static void Draw(Rect rect, float now)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            if (!attempted)
            {
                attempted = true;
                try
                {
                    image = new Texture2D(InformationLoadingState.ImageSize, InformationLoadingState.ImageSize * InformationLoadingState.Frames, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                    image.LoadRawTextureData(InformationLoadingState.Pixels()); image.Apply(false, true);
                }
                catch { Release(); attempted = true; throw; }
            }
            if (image == null) return;
            int frame = (int)((now % 1f) * InformationLoadingState.Frames);
            Color previous = GUI.color;
            try
            {
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(rect, image, new Rect(0, (float)frame / InformationLoadingState.Frames, 1, 1f / InformationLoadingState.Frames));
            }
            finally { GUI.color = previous; }
        }
    }
}
