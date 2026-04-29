using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LayerLabAsset
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class CameraGradientBackground : MonoBehaviour
    {
        public enum GradientDirection { Vertical, Horizontal, Angle }

        [SerializeField] private Gradient gradient = CreateDefaultGradient();
        [SerializeField] private GradientDirection direction = GradientDirection.Vertical;
        [SerializeField] private float angleDegrees = 0f;
        [SerializeField, Range(8, 1024)] private int textureResolution = 256;

        private const string HostName = "_GradientBackground";
        private const int CanvasSortingOrder = -1000;

        private Canvas _canvas;
        private RawImage _image;
        private Texture2D _texture;

        private void OnEnable()
        {
            EnsureHierarchy();
            Rebake();
        }

        private void OnDisable()
        {
            DisposeTexture();
        }

        private void OnDestroy()
        {
            DisposeTexture();
            if (this == null || transform == null) return;
            Transform host = transform.Find(HostName);
            if (host == null) return;
            if (Application.isPlaying) Destroy(host.gameObject);
            else DestroyImmediate(host.gameObject);
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                EnsureHierarchy();
                Rebake();
            };
#else
            EnsureHierarchy();
            Rebake();
#endif
        }

        private void LateUpdate()
        {
            if (_canvas == null) return;
            Camera cam = GetComponent<Camera>();
            if (cam == null) return;
            float d = cam.farClipPlane * 0.99f;
            if (!Mathf.Approximately(_canvas.planeDistance, d))
                _canvas.planeDistance = d;
        }

        private static Gradient CreateDefaultGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.20f, 0.40f, 0.80f), 0f),
                    new GradientColorKey(new Color(1.00f, 0.78f, 0.55f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return g;
        }

        private void EnsureHierarchy()
        {
            if (this == null || transform == null) return;

            Transform host = transform.Find(HostName);
            GameObject hostGo = host != null ? host.gameObject : null;
            if (hostGo == null)
            {
                hostGo = new GameObject(HostName);
                hostGo.transform.SetParent(transform, false);
                hostGo.hideFlags = HideFlags.DontSave;
            }

            _canvas = hostGo.GetComponent<Canvas>();
            if (_canvas == null) _canvas = hostGo.AddComponent<Canvas>();

            Camera cam = GetComponent<Camera>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = cam;
            _canvas.planeDistance = cam.farClipPlane * 0.99f;
            _canvas.sortingOrder = CanvasSortingOrder;

            if (hostGo.GetComponent<GraphicRaycaster>() == null)
            {
                // No raycaster — keep input pass-through for game objects.
            }

            Transform imgT = hostGo.transform.childCount > 0 ? hostGo.transform.GetChild(0) : null;
            GameObject imgGo = imgT != null ? imgT.gameObject : null;
            if (imgGo == null)
            {
                imgGo = new GameObject("Image");
                imgGo.transform.SetParent(hostGo.transform, false);
                imgGo.hideFlags = HideFlags.DontSave;
            }

            _image = imgGo.GetComponent<RawImage>();
            if (_image == null) _image = imgGo.AddComponent<RawImage>();
            _image.raycastTarget = false;

            RectTransform rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private void Rebake()
        {
            if (gradient == null || _image == null) return;

            int w, h;
            int res = Mathf.Max(8, textureResolution);
            if (direction == GradientDirection.Horizontal) { w = res; h = 1; }
            else if (direction == GradientDirection.Vertical) { w = 1; h = res; }
            else { w = res; h = res; }

            if (_texture == null || _texture.width != w || _texture.height != h)
            {
                DisposeTexture();
                _texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave
                };
            }

            Color32[] pixels = new Color32[w * h];

            switch (direction)
            {
                case GradientDirection.Horizontal:
                    for (int x = 0; x < w; x++)
                        pixels[x] = gradient.Evaluate((float)x / (w - 1));
                    break;

                case GradientDirection.Vertical:
                    for (int y = 0; y < h; y++)
                        pixels[y] = gradient.Evaluate((float)y / (h - 1));
                    break;

                default: // Angle
                {
                    float rad = angleDegrees * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                    float maxProj = Mathf.Max(1e-6f, 0.5f * (Mathf.Abs(dir.x) + Mathf.Abs(dir.y)));
                    int idx = 0;
                    for (int y = 0; y < h; y++)
                    {
                        float v = (float)y / (h - 1) - 0.5f;
                        for (int x = 0; x < w; x++)
                        {
                            float u = (float)x / (w - 1) - 0.5f;
                            float t = (u * dir.x + v * dir.y + maxProj) / (2f * maxProj);
                            pixels[idx++] = gradient.Evaluate(t);
                        }
                    }
                    break;
                }
            }

            _texture.SetPixels32(pixels);
            _texture.Apply(false, false);
            _image.texture = _texture;
        }

        private void DisposeTexture()
        {
            if (_texture == null) return;
            if (Application.isPlaying) Destroy(_texture);
            else DestroyImmediate(_texture);
            _texture = null;
        }
    }
}
