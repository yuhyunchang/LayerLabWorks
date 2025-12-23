using UnityEditor;
using UnityEngine;
using System.IO;

namespace CatWars.Editor
{
    /// <summary>
    /// WebGL 초기 로딩 최적화를 위한 텍스처 Import Settings 자동 설정
    /// - Spine: ASTC 6x6, Max 2048, Mipmap Off
    /// - UI: ASTC 8x8, Max 1024, Mipmap Off
    /// - Dynamic: ASTC 8x8, Max 1024, Mipmap Off
    /// </summary>
    public class TextureImportSettingsOptimizer : AssetPostprocessor
    {
        private const string ART_PATH = "Assets/_Project/Art/";

        void OnPreprocessTexture()
        {
            // 프로젝트 아트 폴더 내의 파일만 처리
            if (!assetPath.StartsWith(ART_PATH))
                return;

            TextureImporter textureImporter = (TextureImporter)assetImporter;
            string pathLower = assetPath.ToLower();

            // Spine 애니메이션 텍스처
            if (pathLower.Contains("/spineanimations/"))
            {
                ConfigureSpineTexture(textureImporter);
            }
            // UI 스프라이트
            else if (pathLower.Contains("/sprite/ui/") ||
                     pathLower.Contains("/sprite/dynamic_ui/"))
            {
                ConfigureUITexture(textureImporter);
            }
            // Dynamic InGame 스프라이트
            else if (pathLower.Contains("/sprite/dynamic_ingame/"))
            {
                ConfigureDynamicTexture(textureImporter);
            }
            // Play 스프라이트
            else if (pathLower.Contains("/sprite/play/"))
            {
                ConfigurePlayTexture(textureImporter);
            }
        }

        private void ConfigureSpineTexture(TextureImporter importer)
        {
            // WebGL 설정 - Spine Atlas는 품질 유지 필요
            TextureImporterPlatformSettings webglSettings = new TextureImporterPlatformSettings
            {
                name = "WebGL",
                overridden = true,
                maxTextureSize = 2048,
                format = TextureImporterFormat.ASTC_6x6,
                compressionQuality = 50,
                textureCompression = TextureImporterCompression.Compressed
            };
            importer.SetPlatformTextureSettings(webglSettings);

            // 공통 설정
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.streamingMipmaps = false;
        }

        private void ConfigureUITexture(TextureImporter importer)
        {
            // WebGL 설정 - UI는 크기 절약 우선
            TextureImporterPlatformSettings webglSettings = new TextureImporterPlatformSettings
            {
                name = "WebGL",
                overridden = true,
                maxTextureSize = 1024,
                format = TextureImporterFormat.ASTC_8x8,
                compressionQuality = 50,
                textureCompression = TextureImporterCompression.Compressed
            };
            importer.SetPlatformTextureSettings(webglSettings);

            // 공통 설정
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.streamingMipmaps = false;
        }

        private void ConfigureDynamicTexture(TextureImporter importer)
        {
            // WebGL 설정 - Dynamic 텍스처
            TextureImporterPlatformSettings webglSettings = new TextureImporterPlatformSettings
            {
                name = "WebGL",
                overridden = true,
                maxTextureSize = 1024,
                format = TextureImporterFormat.ASTC_8x8,
                compressionQuality = 50,
                textureCompression = TextureImporterCompression.Compressed
            };
            importer.SetPlatformTextureSettings(webglSettings);

            // 공통 설정
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.streamingMipmaps = false;
        }

        private void ConfigurePlayTexture(TextureImporter importer)
        {
            // WebGL 설정 - Play 화면 텍스처
            TextureImporterPlatformSettings webglSettings = new TextureImporterPlatformSettings
            {
                name = "WebGL",
                overridden = true,
                maxTextureSize = 1024,
                format = TextureImporterFormat.ASTC_6x6,
                compressionQuality = 50,
                textureCompression = TextureImporterCompression.Compressed
            };
            importer.SetPlatformTextureSettings(webglSettings);

            // 공통 설정
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.streamingMipmaps = false;
        }
    }

    /// <summary>
    /// 텍스처 에셋 일괄 Reimport를 위한 에디터 메뉴
    /// </summary>
    public static class TextureOptimizationMenu
    {
        private const string SPRITE_PATH = "Assets/_Project/Art/Sprite";
        private const string SPINE_PATH = "Assets/_Project/Art/SpineAnimations";

        [MenuItem("Tools/WebGL Optimization/Reimport All Textures (WebGL Optimized)")]
        public static void ReimportAllTextures()
        {
            if (!EditorUtility.DisplayDialog(
                "Reimport Texture Assets",
                "모든 텍스처 에셋을 WebGL 최적화 설정으로 다시 임포트합니다.\n\n" +
                "Spine: ASTC 6x6, Max 2048\n" +
                "UI: ASTC 8x8, Max 1024\n" +
                "Dynamic: ASTC 8x8, Max 1024\n" +
                "Play: ASTC 6x6, Max 1024\n\n" +
                "이 작업은 시간이 걸릴 수 있습니다. 계속하시겠습니까?",
                "Reimport",
                "Cancel"))
            {
                return;
            }

            var paths = new[] { SPRITE_PATH, SPINE_PATH };
            var allGuids = new System.Collections.Generic.List<string>();

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    allGuids.AddRange(AssetDatabase.FindAssets("t:Texture2D", new[] { path }));
                }
            }

            int total = allGuids.Count;
            int current = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string guid in allGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    current++;

                    EditorUtility.DisplayProgressBar(
                        "Reimporting Textures",
                        $"Processing: {Path.GetFileName(path)} ({current}/{total})",
                        (float)current / total);

                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[TextureOptimization] {total}개의 텍스처 파일이 WebGL 최적화 설정으로 다시 임포트되었습니다.");
        }

        [MenuItem("Tools/WebGL Optimization/Reimport Spine Textures Only")]
        public static void ReimportSpineTexturesOnly()
        {
            if (!Directory.Exists(SPINE_PATH))
            {
                Debug.LogError($"[TextureOptimization] 경로를 찾을 수 없습니다: {SPINE_PATH}");
                return;
            }

            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { SPINE_PATH });
            int total = textureGuids.Length;
            int current = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string guid in textureGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    current++;

                    EditorUtility.DisplayProgressBar(
                        "Reimporting Spine Textures",
                        $"Processing: {Path.GetFileName(path)} ({current}/{total})",
                        (float)current / total);

                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[TextureOptimization] {total}개의 Spine 텍스처가 WebGL 최적화 설정으로 다시 임포트되었습니다.");
        }

        [MenuItem("Tools/WebGL Optimization/Show Texture Statistics")]
        public static void ShowTextureStatistics()
        {
            string[] folders = {
                "Assets/_Project/Art/Sprite/UI",
                "Assets/_Project/Art/Sprite/Play",
                "Assets/_Project/Art/Sprite/Dynamic_UI",
                "Assets/_Project/Art/Sprite/Dynamic_InGame",
                "Assets/_Project/Art/SpineAnimations"
            };

            Debug.Log("=== Texture Statistics ===");

            long totalSize = 0;
            int totalCount = 0;

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                    continue;

                string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                long folderSize = 0;

                foreach (string guid in textureGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    FileInfo fileInfo = new FileInfo(path);
                    if (fileInfo.Exists)
                    {
                        folderSize += fileInfo.Length;
                    }
                }

                totalSize += folderSize;
                totalCount += textureGuids.Length;

                string folderName = Path.GetFileName(folder);
                Debug.Log($"{folderName}: {textureGuids.Length}개, {FormatBytes(folderSize)}");
            }

            Debug.Log($"Total: {totalCount}개, {FormatBytes(totalSize)}");
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
