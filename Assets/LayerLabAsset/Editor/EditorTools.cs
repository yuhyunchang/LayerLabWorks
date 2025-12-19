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

        [MenuItem("LayerLabAsset/Update Package")]
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

        [MenuItem("LayerLabAsset/Reset PlayerPrefs")]
        static public void TestCode()
        {
            PlayerPrefs.DeleteAll();
        }

        [MenuItem("LayerLabAsset/Disable Raycast Target")]
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
    }
}