using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine.UI;

namespace LayerLabAsset
{
    public class EditorTools : MonoBehaviour
    {
        private const string PACKAGE_NAME = "com.layerlab.asset";
        private static ListRequest _listRequest;
        private static AddRequest _addRequest;

        [MenuItem("LayerLabAsset/Update Package", false, 200)]
        static public void UpdatePackage()
        {
            Debug.Log("LayerLabAsset: Checking for updates...");
            _listRequest = Client.List(true);
            EditorApplication.update += OnListRequestComplete;
        }

        private static void OnListRequestComplete()
        {
            if (!_listRequest.IsCompleted)
                return;

            EditorApplication.update -= OnListRequestComplete;

            if (_listRequest.Status == StatusCode.Failure)
            {
                Debug.LogError($"LayerLabAsset: Failed to get package list - {_listRequest.Error.message}");
                return;
            }

            string packageUrl = null;
            foreach (var package in _listRequest.Result)
            {
                if (package.name == PACKAGE_NAME)
                {
                    if (package.source == PackageSource.Git)
                    {
                        // packageId contains the git URL for git packages
                        // Format: "com.layerlab.asset@https://github.com/..."
                        string packageId = package.packageId;
                        int atIndex = packageId.IndexOf('@');
                        if (atIndex >= 0 && atIndex < packageId.Length - 1)
                        {
                            packageUrl = packageId.Substring(atIndex + 1);
                        }
                    }
                    break;
                }
            }

            if (string.IsNullOrEmpty(packageUrl))
            {
                Debug.LogWarning("LayerLabAsset: Package is not installed from Git URL. Cannot auto-update.");
                return;
            }

            Debug.Log($"LayerLabAsset: Updating from {packageUrl}...");
            _addRequest = Client.Add(packageUrl);
            EditorApplication.update += OnAddRequestComplete;
        }

        private static void OnAddRequestComplete()
        {
            if (!_addRequest.IsCompleted)
                return;

            EditorApplication.update -= OnAddRequestComplete;

            if (_addRequest.Status == StatusCode.Failure)
            {
                Debug.LogError($"LayerLabAsset: Update failed - {_addRequest.Error.message}");
            }
            else
            {
                Debug.Log($"LayerLabAsset: Successfully updated to version {_addRequest.Result.version}");
            }
        }

        [MenuItem("LayerLabAsset/Reset PlayerPrefs", false, 100)]
        static public void TestCode()
        {
            PlayerPrefs.DeleteAll();
        }

        [MenuItem("LayerLabAsset/Disable Raycast Target", false, 101)]
        static public void DisableRaycastTarget()
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("오브젝트를 선택해주세요.");
                return;
            }

            int imageCount = 0;
            int tmpCount = 0;

            foreach (GameObject obj in selectedObjects)
            {
                DisableRaycastTargetRecursive(obj, ref imageCount, ref tmpCount);
            }

            Debug.Log($"Raycast Target 비활성화 완료 - Image: {imageCount}개, TextMeshPro: {tmpCount}개");
        }

        static void DisableRaycastTargetRecursive(GameObject obj, ref int imageCount, ref int tmpCount)
        {
            // Button 컴포넌트가 있는지 확인
            bool hasButton = obj.GetComponent<Button>() != null;

            // Image 컴포넌트 처리
            Image[] images = obj.GetComponents<Image>();
            foreach (Image img in images)
            {
                bool targetValue = hasButton; // Button이 있으면 true, 없으면 false
                if (img.raycastTarget != targetValue)
                {
                    Undo.RecordObject(img, "Set Raycast Target");
                    img.raycastTarget = targetValue;
                    imageCount++;
                }
            }

            // TextMeshProUGUI 컴포넌트 처리
            TextMeshProUGUI[] tmps = obj.GetComponents<TextMeshProUGUI>();
            foreach (TextMeshProUGUI tmp in tmps)
            {
                bool targetValue = hasButton; // Button이 있으면 true, 없으면 false
                if (tmp.raycastTarget != targetValue)
                {
                    Undo.RecordObject(tmp, "Set Raycast Target");
                    tmp.raycastTarget = targetValue;
                    tmpCount++;
                }
            }

            // 자식들 순회
            foreach (Transform child in obj.transform)
            {
                DisableRaycastTargetRecursive(child.gameObject, ref imageCount, ref tmpCount);
            }
        }

        private const string RegenerateGuidsMenuPath = "LayerLabAsset/Regenerate Selected Asset GUIDs";
        private static readonly Regex MetaGuidRegex = new Regex("^guid: (?<guid>[0-9a-fA-F]{32})$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly HashSet<string> GuidReferenceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".anim", ".asmdef", ".asmref", ".asset", ".compute", ".controller", ".cs", ".cginc",
            ".hlsl", ".inputactions", ".json", ".mat", ".mask", ".meta", ".overridecontroller",
            ".playable", ".prefab", ".shader", ".shadergraph", ".shadersubgraph", ".txt", ".unity",
            ".uss", ".uxml", ".vfx"
        };

        [MenuItem(RegenerateGuidsMenuPath, false, 150)]
        static public void RegenerateSelectedAssetGuids()
        {
            List<string> assetPaths = GetSelectedAssetPathsForGuidRegeneration();

            if (assetPaths.Count == 0)
            {
                Debug.LogWarning("Assets 폴더 아래의 파일 또는 폴더를 선택해주세요.");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "GUID 갱신 확인",
                $"선택한 대상에서 {assetPaths.Count}개 파일 asset의 GUID를 새로 생성합니다.\n\nAssets와 ProjectSettings 아래 텍스트 파일의 기존 GUID 참조도 새 GUID로 함께 치환합니다. 계속할까요?",
                "GUID 갱신",
                "취소");

            if (!confirmed)
            {
                return;
            }

            int updatedCount = 0;
            int skippedCount = 0;
            Dictionary<string, string> guidReplacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (string assetPath in assetPaths)
                {
                    if (RegenerateAssetGuid(assetPath, out string oldGuid, out string newGuid))
                    {
                        guidReplacements[oldGuid] = newGuid;
                        updatedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            int referenceFileCount = 0;
            int referenceReplaceCount = 0;

            try
            {
                if (guidReplacements.Count > 0)
                {
                    UpdateGuidReferences(guidReplacements, out referenceFileCount, out referenceReplaceCount);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            Debug.Log($"GUID 갱신 완료 - 갱신: {updatedCount}개, 건너뜀: {skippedCount}개, 참조 수정 파일: {referenceFileCount}개, 참조 치환: {referenceReplaceCount}개");
        }

        [MenuItem(RegenerateGuidsMenuPath, true)]
        static public bool CanRegenerateSelectedAssetGuids()
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (IsEditableAssetPath(path))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> GetSelectedAssetPathsForGuidRegeneration()
        {
            HashSet<string> paths = new HashSet<string>();

            foreach (string guid in Selection.assetGUIDs)
            {
                string selectedPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!IsEditableAssetPath(selectedPath))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(selectedPath))
                {
                    foreach (string childGuid in AssetDatabase.FindAssets(string.Empty, new[] { selectedPath }))
                    {
                        string childPath = AssetDatabase.GUIDToAssetPath(childGuid);

                        if (IsEditableFileAssetPath(childPath))
                        {
                            paths.Add(childPath);
                        }
                    }
                }
                else if (IsEditableFileAssetPath(selectedPath))
                {
                    paths.Add(selectedPath);
                }
            }

            List<string> sortedPaths = new List<string>(paths);
            sortedPaths.Sort(StringComparer.Ordinal);
            return sortedPaths;
        }

        private static bool RegenerateAssetGuid(string assetPath, out string oldGuid, out string newGuid)
        {
            oldGuid = null;
            newGuid = null;
            string metaPath = assetPath + ".meta";

            if (!File.Exists(metaPath))
            {
                Debug.LogWarning($"메타 파일을 찾을 수 없어 GUID 갱신을 건너뜁니다: {assetPath}");
                return false;
            }

            string metaText = File.ReadAllText(metaPath, Encoding.UTF8);
            Match guidMatch = MetaGuidRegex.Match(metaText);

            if (!guidMatch.Success)
            {
                Debug.LogWarning($"메타 파일에서 GUID를 찾을 수 없어 갱신을 건너뜁니다: {assetPath}");
                return false;
            }

            oldGuid = guidMatch.Groups["guid"].Value;
            newGuid = GUID.Generate().ToString();
            string updatedMetaText = MetaGuidRegex.Replace(metaText, "guid: " + newGuid, 1);
            File.WriteAllText(metaPath, updatedMetaText, new UTF8Encoding(false));
            return true;
        }

        private static void UpdateGuidReferences(Dictionary<string, string> guidReplacements, out int modifiedFileCount, out int replacementCount)
        {
            modifiedFileCount = 0;
            replacementCount = 0;

            List<string> referenceFiles = GetGuidReferenceFiles();

            for (int i = 0; i < referenceFiles.Count; i++)
            {
                string path = referenceFiles[i];
                EditorUtility.DisplayProgressBar("GUID 참조 갱신", path, referenceFiles.Count == 0 ? 1f : (float)i / referenceFiles.Count);

                string text;
                try
                {
                    text = File.ReadAllText(path, Encoding.UTF8);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"GUID 참조 파일을 읽을 수 없어 건너뜁니다: {path}\n{exception.Message}");
                    continue;
                }

                int fileReplacementCount = 0;
                string updatedText = ReplaceGuidReferences(text, guidReplacements, ref fileReplacementCount);

                if (fileReplacementCount == 0)
                    continue;

                try
                {
                    File.WriteAllText(path, updatedText, new UTF8Encoding(false));
                    modifiedFileCount++;
                    replacementCount += fileReplacementCount;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"GUID 참조 파일을 쓸 수 없어 건너뜁니다: {path}\n{exception.Message}");
                }
            }
        }

        private static List<string> GetGuidReferenceFiles()
        {
            List<string> files = new List<string>();
            AddGuidReferenceFiles(files, "Assets");
            AddGuidReferenceFiles(files, "ProjectSettings");
            files.Sort(StringComparer.Ordinal);
            return files;
        }

        private static void AddGuidReferenceFiles(List<string> files, string rootPath)
        {
            if (!Directory.Exists(rootPath))
                return;

            foreach (string filePath in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
            {
                string normalizedPath = filePath.Replace('\\', '/');

                if (ShouldScanGuidReferenceFile(normalizedPath))
                    files.Add(normalizedPath);
            }
        }

        private static bool ShouldScanGuidReferenceFile(string path)
        {
            string extension = Path.GetExtension(path);
            return !string.IsNullOrEmpty(extension) && GuidReferenceExtensions.Contains(extension);
        }

        private static string ReplaceGuidReferences(string text, Dictionary<string, string> guidReplacements, ref int replacementCount)
        {
            string updatedText = text;
            int totalReplacementCount = replacementCount;

            foreach (KeyValuePair<string, string> replacement in guidReplacements)
            {
                string oldGuidPattern = Regex.Escape(replacement.Key);
                updatedText = Regex.Replace(
                    updatedText,
                    oldGuidPattern,
                    match =>
                    {
                        totalReplacementCount++;
                        return replacement.Value;
                    },
                    RegexOptions.IgnoreCase);
            }

            replacementCount = totalReplacementCount;
            return updatedText;
        }

        private static bool IsEditableAssetPath(string path)
        {
            return path == "Assets" || (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.Ordinal));
        }

        private static bool IsEditableFileAssetPath(string path)
        {
            return IsEditableAssetPath(path) && !AssetDatabase.IsValidFolder(path);
        }

        [MenuItem("LayerLabAsset/GitHub", false, 1000)]
        static public void OpenGitHub()
        {
            Application.OpenURL("https://github.com/yuhyunchang/LayerLabWorks");
        }
    }
}