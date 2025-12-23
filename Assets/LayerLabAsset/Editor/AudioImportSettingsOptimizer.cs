using UnityEditor;
using UnityEngine;
using System.IO;

namespace CatWars.Editor
{
    /// <summary>
    /// WebGL 초기 로딩 최적화를 위한 오디오 Import Settings 자동 설정
    /// - BGM: Streaming, Quality 0.5, 44100Hz, Mono
    /// - Ambient: Streaming, Quality 0.4, 22050Hz, Mono
    /// - SFX: CompressedInMemory, Quality 0.5, 22050Hz, Mono
    /// </summary>
    public class AudioImportSettingsOptimizer : AssetPostprocessor
    {
        private const string AUDIO_PATH = "Assets/_Project/Art/Audio/";

        void OnPreprocessAudio()
        {
            // 프로젝트 오디오 폴더 내의 파일만 처리
            if (!assetPath.StartsWith(AUDIO_PATH))
                return;

            AudioImporter audioImporter = (AudioImporter)assetImporter;
            string pathLower = assetPath.ToLower();

            // WebGL 플랫폼 설정
            AudioImporterSampleSettings webglSettings = new AudioImporterSampleSettings();
            webglSettings.compressionFormat = AudioCompressionFormat.AAC;

            // 공통 설정 (플랫폼별 설정에 포함)
            webglSettings.preloadAudioData = false; // 초기 로딩 부담 제거

            if (pathLower.Contains("/bgm/"))
            {
                // BGM: Streaming (큰 파일), 높은 샘플레이트 유지
                webglSettings.loadType = AudioClipLoadType.Streaming;
                webglSettings.quality = 0.5f;
                webglSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                webglSettings.sampleRateOverride = 44100;
            }
            else if (pathLower.Contains("/ambient/"))
            {
                // Ambient: Streaming, 낮은 품질 (환경음은 품질 저하 체감 적음)
                webglSettings.loadType = AudioClipLoadType.Streaming;
                webglSettings.quality = 0.4f;
                webglSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                webglSettings.sampleRateOverride = 22050;
            }
            else
            {
                // SFX (Weapon, UI, InGame): CompressedInMemory
                webglSettings.loadType = AudioClipLoadType.CompressedInMemory;
                webglSettings.quality = 0.5f;
                webglSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                webglSettings.sampleRateOverride = 22050;
            }

            // 공통 설정
            audioImporter.forceToMono = true;
            audioImporter.loadInBackground = true;

            audioImporter.SetOverrideSampleSettings("WebGL", webglSettings);
        }
    }

    /// <summary>
    /// 오디오 에셋 일괄 Reimport를 위한 에디터 메뉴
    /// </summary>
    public static class AudioOptimizationMenu
    {
        private const string AUDIO_PATH = "Assets/_Project/Art/Audio";

        [MenuItem("Tools/WebGL Optimization/Reimport All Audio (WebGL Optimized)")]
        public static void ReimportAllAudio()
        {
            if (!EditorUtility.DisplayDialog(
                "Reimport Audio Assets",
                "모든 오디오 에셋을 WebGL 최적화 설정으로 다시 임포트합니다.\n\n" +
                "BGM: Streaming, Quality 0.5, 44100Hz, Mono\n" +
                "Ambient: Streaming, Quality 0.4, 22050Hz, Mono\n" +
                "SFX: CompressedInMemory, Quality 0.5, 22050Hz, Mono\n\n" +
                "이 작업은 시간이 걸릴 수 있습니다. 계속하시겠습니까?",
                "Reimport",
                "Cancel"))
            {
                return;
            }

            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { AUDIO_PATH });
            int total = audioGuids.Length;
            int current = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string guid in audioGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    current++;

                    EditorUtility.DisplayProgressBar(
                        "Reimporting Audio",
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
            Debug.Log($"[AudioOptimization] {total}개의 오디오 파일이 WebGL 최적화 설정으로 다시 임포트되었습니다.");
        }

        [MenuItem("Tools/WebGL Optimization/Show Audio Statistics")]
        public static void ShowAudioStatistics()
        {
            string[] subfolders = { "BGM", "Ambient", "Weapon", "UI", "InGame" };

            Debug.Log("=== Audio Statistics ===");

            long totalSize = 0;
            int totalCount = 0;

            foreach (string folder in subfolders)
            {
                string folderPath = $"{AUDIO_PATH}/{folder}";
                if (!Directory.Exists(folderPath))
                    continue;

                string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
                long folderSize = 0;

                foreach (string guid in audioGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    FileInfo fileInfo = new FileInfo(path);
                    if (fileInfo.Exists)
                    {
                        folderSize += fileInfo.Length;
                    }
                }

                totalSize += folderSize;
                totalCount += audioGuids.Length;

                Debug.Log($"{folder}: {audioGuids.Length}개, {FormatBytes(folderSize)}");
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
