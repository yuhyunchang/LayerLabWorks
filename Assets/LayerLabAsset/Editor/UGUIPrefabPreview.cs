using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LayerLabAsset
{
    /// <summary>
    /// 프로젝트 창에서 UGUI 프리팹의 기본 아이콘을 실제 프리뷰 이미지로 교체
    /// </summary>
    [InitializeOnLoad]
    public static class UGUIPrefabPreview
    {
        private const string MENU_PATH = "LayerLabAsset/UGUI Prefab Preview";
        private const string PREFS_KEY = "LayerLabAsset.UGUIPrefabPreview.Enabled";
        private const int PreviewSize = 128;

        private static readonly Dictionary<string, bool> _isUGUICache = new Dictionary<string, bool>();
        private static readonly Dictionary<string, Texture2D> _previewCache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> _renderQueue = new HashSet<string>();
        private static bool _enabled;

        static UGUIPrefabPreview()
        {
            _enabled = EditorPrefs.GetBool(PREFS_KEY, false);
            ApplyState();
        }

        [MenuItem(MENU_PATH, false, 102)]
        private static void Toggle()
        {
            _enabled = !_enabled;
            EditorPrefs.SetBool(PREFS_KEY, _enabled);
            ApplyState();
            EditorApplication.RepaintProjectWindow();
            Debug.Log($"[LayerLabAsset] UGUI Prefab Preview: {(_enabled ? "ON" : "OFF")}");
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MENU_PATH, _enabled);
            return true;
        }

        [MenuItem("LayerLabAsset/UGUI Prefab Preview - Refresh Cache", false, 103)]
        private static void RefreshCache()
        {
            ClearPreviewCache();
            _isUGUICache.Clear();
            _renderQueue.Clear();
            EditorApplication.RepaintProjectWindow();
            Debug.Log("[LayerLabAsset] UGUI Prefab Preview cache cleared");
        }

        private static void ApplyState()
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.update -= ProcessRenderQueue;

            if (_enabled)
            {
                EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
                EditorApplication.update += ProcessRenderQueue;
            }
            else
            {
                ClearPreviewCache();
                _isUGUICache.Clear();
                _renderQueue.Clear();
            }
        }

        private static void ClearPreviewCache()
        {
            foreach (var tex in _previewCache.Values)
            {
                if (tex != null) Object.DestroyImmediate(tex);
            }
            _previewCache.Clear();
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (string.IsNullOrEmpty(guid)) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;
            if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) return;

            var asset = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
            if (asset == null) return;
            if (!IsUGUIPrefab(guid, asset)) return;

            Rect iconRect = GetIconRect(selectionRect);
            EditorGUI.DrawRect(iconRect, GetBackgroundColor());

            if (_previewCache.TryGetValue(guid, out var cached) && cached != null)
            {
                GUI.DrawTexture(iconRect, cached, ScaleMode.ScaleToFit);
                return;
            }

            _renderQueue.Add(guid);
        }

        private static void ProcessRenderQueue()
        {
            if (_renderQueue.Count == 0) return;

            int processed = 0;
            var toProcess = new List<string>(_renderQueue);
            foreach (var guid in toProcess)
            {
                _renderQueue.Remove(guid);

                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var prefab = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
                if (prefab == null) continue;

                Texture2D tex = RenderPrefabPreview(prefab);
                if (tex != null) _previewCache[guid] = tex;

                if (++processed >= 1) break;
            }

            EditorApplication.RepaintProjectWindow();
        }

        private static Texture2D RenderPrefabPreview(GameObject prefab)
        {
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject instance = null;
            RenderTexture rt = null;
            RenderTexture prevActive = null;
            bool hadPrev = false;

            try
            {
                // 활성 씬에 숨김 루트 생성 (메인 카메라 시야 밖으로 이동)
                root = EditorUtility.CreateGameObjectWithHideFlags(
                    "__UGUIPreviewRoot__",
                    HideFlags.HideAndDontSave);
                root.transform.position = new Vector3(10000, 10000, 10000);

                // 프리팹 인스턴스화 (root의 자식으로)
                instance = Object.Instantiate(prefab, root.transform, false);
                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                // RectTransform 사이즈 확보
                var rectTransform = instance.GetComponent<RectTransform>();
                Vector2 rectSize = rectTransform.rect.size;
                if (rectSize.x <= 0 || rectSize.y <= 0)
                {
                    rectSize = new Vector2(800, 600);
                    rectTransform.sizeDelta = rectSize;
                }

                // 카메라 (root의 자식, 캔버스 정중앙을 바라보게 위치)
                cameraGo = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
                cameraGo.transform.SetParent(root.transform, false);
                Vector2 pivot = rectTransform.pivot;
                cameraGo.transform.localPosition = new Vector3(
                    (0.5f - pivot.x) * rectSize.x,
                    (0.5f - pivot.y) * rectSize.y,
                    -10f);
                cameraGo.transform.localRotation = Quaternion.identity;

                var camera = cameraGo.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = GetBackgroundColor();
                camera.orthographic = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.cullingMask = ~0;
                camera.cameraType = CameraType.Game;
                camera.useOcclusionCulling = false;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.enabled = false;
                camera.aspect = rectSize.x / rectSize.y;
                camera.orthographicSize = rectSize.y * 0.5f;

                // Canvas 설정 - WorldSpace 모드로 강제 (에디터 경로에서 가장 안정적)
                Canvas canvas = instance.GetComponent<Canvas>();
                if (canvas == null) canvas = instance.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                canvas.sortingOrder = 32767;

                if (instance.GetComponent<GraphicRaycaster>() == null)
                    instance.AddComponent<GraphicRaycaster>();

                // 자식의 nested Canvas는 sorting override 끄기
                foreach (var nested in instance.GetComponentsInChildren<Canvas>(true))
                {
                    if (nested == canvas) continue;
                    nested.overrideSorting = false;
                }

                // 머티리얼/캔버스 강제 갱신
                instance.SetActive(false);
                instance.SetActive(true);
                foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
                {
                    graphic.SetAllDirty();
                }
                Canvas.ForceUpdateCanvases();

                // 렌더 타겟 (캔버스 비율에 맞춰 RT 사이즈 설정해서 왜곡 방지)
                int rtW = PreviewSize;
                int rtH = PreviewSize;
                if (rectSize.x > rectSize.y)
                {
                    rtH = Mathf.Max(8, Mathf.RoundToInt(PreviewSize * (rectSize.y / rectSize.x)));
                }
                else if (rectSize.y > rectSize.x)
                {
                    rtW = Mathf.Max(8, Mathf.RoundToInt(PreviewSize * (rectSize.x / rectSize.y)));
                }

                rt = RenderTexture.GetTemporary(rtW, rtH, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 1;
                camera.targetTexture = rt;
                camera.aspect = (float)rtW / rtH;

                Canvas.ForceUpdateCanvases();
                camera.Render();

                hadPrev = true;
                prevActive = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D result = new Texture2D(rtW, rtH, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, rtW, rtH), 0, 0);
                result.Apply();
                result.hideFlags = HideFlags.HideAndDontSave;

                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LayerLabAsset] UGUI Prefab Preview render failed for {prefab.name}: {ex.Message}");
                return null;
            }
            finally
            {
                if (hadPrev) RenderTexture.active = prevActive;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (root != null) Object.DestroyImmediate(root);
            }
        }

        private static Rect GetIconRect(Rect selectionRect)
        {
            Rect iconRect = selectionRect;
            if (iconRect.width > iconRect.height)
            {
                iconRect.width = iconRect.height;
            }
            else
            {
                iconRect.height = iconRect.width;
            }
            return iconRect;
        }

        private static Color GetBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.76f, 0.76f, 0.76f);
        }

        private static bool IsUGUIPrefab(string guid, GameObject prefab)
        {
            if (_isUGUICache.TryGetValue(guid, out bool cached)) return cached;

            bool isUGUI = prefab.GetComponent<RectTransform>() != null;
            _isUGUICache[guid] = isUGUI;
            return isUGUI;
        }
    }
}
