using UnityEditor;
using UnityEngine;

namespace PirateGame.UI.Theme.Editor
{
    // Import settings for the generated Drowned Sun UI textures (tools/theme/generate_theme.py)
    // and the UI preview's stand-in vista:
    // full quality, no mipmaps, clamped except the tiling hatch pattern.
    public sealed class ThemeTextureImport : AssetPostprocessor
    {
        private const string Folder = "Assets/_Game/UI/Theme/", Preview = "Assets/_Game/UI/Preview/";

        private void OnPreprocessTexture()
        {
            if (!(assetPath.StartsWith(Folder) || assetPath.StartsWith(Preview)) || !assetPath.EndsWith(".png")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = assetPath.EndsWith("hatch.png") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
        }
    }
}
