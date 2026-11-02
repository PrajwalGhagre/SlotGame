using UnityEditor;

namespace SlotGame.EditorTools
{
    /// <summary>
    /// Makes sure every texture under Assets/Art and Assets/UI imports as a UI sprite with the
    /// right settings, even in a fresh project where Unity would default to
    /// "Default" textures. Saves a lot of clicking and avoids blurry or
    /// compressed pixel art.
    /// </summary>
    public sealed class SpriteImportSettings : AssetPostprocessor
    {
        private static readonly string[] SpriteFolders = { "Assets/Art/", "Assets/UI/" };

        private void OnPreprocessTexture()
        {
            if (!IsInSpriteFolder(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        private static bool IsInSpriteFolder(string path)
        {
            foreach (string folder in SpriteFolders)
            {
                if (path.StartsWith(folder))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
