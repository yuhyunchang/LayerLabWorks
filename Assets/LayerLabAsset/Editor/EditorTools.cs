using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace LayerLabAsset
{
    public class EditorTools : MonoBehaviour
    {
        [MenuItem("PlayPrefabs/Reset")]
        static public void TestCode()
        {
            PlayerPrefs.DeleteAll();
        }

        [MenuItem("Tools/Disable Raycast Target")]
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