using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace LayerLabAsset
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PatternGridScroller : MonoBehaviour
    {
        [SerializeField] private Vector2 scrollVelocity = new Vector2(1f, 0f);
        [SerializeField] private Vector2 gridSize = Vector2.zero;
        [SerializeField] private bool playInEditMode = false;

        private Transform[] _children;
        private Vector3[] _basePositions;
        private Vector2 _accumOffset;

#if UNITY_EDITOR
        private double _lastEditorTime;
        private bool _restoredForSave;
#endif

        public void Configure(int columns, int rows, float pitchX, float pitchY, Vector2 velocity, bool playInEditMode)
        {
            gridSize = new Vector2(columns * pitchX, rows * pitchY);
            scrollVelocity = velocity;
            this.playInEditMode = playInEditMode;
            _accumOffset = Vector2.zero;
            InvalidateCache();
        }

        private void OnEnable()
        {
            InvalidateCache();
#if UNITY_EDITOR
            _lastEditorTime = EditorApplication.timeSinceStartup;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
#endif
        }

        private void OnDisable()
        {
            RestoreBasePositions();
#if UNITY_EDITOR
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
#endif
        }

        private void OnValidate()
        {
            InvalidateCache();
        }

        private void OnTransformChildrenChanged()
        {
            InvalidateCache();
        }

        private void Update()
        {
            if (gridSize.x <= 0f || gridSize.y <= 0f) return;
            if (_children == null) RebuildCache();
            if (_children.Length == 0) return;

            float dt;
            if (Application.isPlaying)
            {
                dt = Time.deltaTime;
            }
            else
            {
#if UNITY_EDITOR
                if (!playInEditMode)
                {
                    _lastEditorTime = EditorApplication.timeSinceStartup;
                    return;
                }

                double now = EditorApplication.timeSinceStartup;
                dt = (float)(now - _lastEditorTime);
                _lastEditorTime = now;
#else
                dt = 0f;
#endif
            }

            _accumOffset += scrollVelocity * dt;

            float halfW = gridSize.x * 0.5f;
            float halfH = gridSize.y * 0.5f;

            for (int i = 0; i < _children.Length; i++)
            {
                Transform t = _children[i];
                if (t == null) continue;

                Vector3 baseP = _basePositions[i];
                float x = Mathf.Repeat(baseP.x + _accumOffset.x + halfW, gridSize.x) - halfW;
                float y = Mathf.Repeat(baseP.y + _accumOffset.y + halfH, gridSize.y) - halfH;
                t.localPosition = new Vector3(x, y, baseP.z);
            }
        }

        private void RebuildCache()
        {
            int count = transform.childCount;
            _children = new Transform[count];
            _basePositions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                _children[i] = transform.GetChild(i);
                _basePositions[i] = _children[i].localPosition;
            }
        }

        private void InvalidateCache()
        {
            _children = null;
            _basePositions = null;
        }

        private void RestoreBasePositions()
        {
            if (_children == null || _basePositions == null) return;
            for (int i = 0; i < _children.Length; i++)
            {
                if (_children[i] == null) continue;
                _children[i].localPosition = _basePositions[i];
            }
        }

#if UNITY_EDITOR
        private void OnSceneSaving(Scene scene, string path)
        {
            if (this == null || gameObject == null) return;
            if (gameObject.scene != scene) return;
            RestoreBasePositions();
            _restoredForSave = true;
        }

        private void OnSceneSaved(Scene scene)
        {
            if (!_restoredForSave) return;
            _restoredForSave = false;
        }
#endif
    }
}
