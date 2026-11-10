using UnityEditor;

namespace BiRyeoDo.Editor
{
    /// <summary>
    /// Import rules for runtime art. Battle SD figures are drawn a few hundred pixels tall (cells, info cards, codex),
    /// but their sources are about 1024-1280px; capping them at 512 keeps them sharp on screen with a quarter of the memory.
    /// Atlases read with measured source-pixel rectangles (Exploration, Readable) are not touched.
    /// Bump the version to reimport after changing a rule.
    /// </summary>
    public sealed class ArtImportRules : AssetPostprocessor
    {
        private const string BattleFolder = "Assets/_Project/Resources/Art/Battle/";
        public const int BattleMaxSize = 512;

        public override uint GetVersion() { return 1; }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(BattleFolder)) return;
            var importer = (TextureImporter)assetImporter;
            if (importer.maxTextureSize > BattleMaxSize) importer.maxTextureSize = BattleMaxSize;
            importer.mipmapEnabled = false;
        }
    }
}
