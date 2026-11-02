using UnityEditor;
using UnityEngine;

namespace SlotGame.EditorTools
{
    /// <summary>
    /// Runs once when the project opens. Unity's default WebGL compression is
    /// Brotli, which GitHub Pages can not serve correctly (it does not send the
    /// Content-Encoding header), so the page would fail to load. This switches the
    /// project to gzip with decompression fallback, which works everywhere, even
    /// if the build is made from File > Build Settings instead of the Tools menu.
    /// </summary>
    [InitializeOnLoad]
    public static class WebGlSettingsGuard
    {
        private const string SessionKey = "SlotGame.WebGlSettingsChecked";

        static WebGlSettingsGuard()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += ApplyIfNeeded;
        }

        private static void ApplyIfNeeded()
        {
            if (PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Gzip &&
                PlayerSettings.WebGL.decompressionFallback)
            {
                return;
            }

            SlotGameEditorMenu.ApplyWebGlPlayerSettings();
            Debug.Log("Slot Game: WebGL settings set to gzip + decompression fallback (works on GitHub Pages).");
        }
    }
}
