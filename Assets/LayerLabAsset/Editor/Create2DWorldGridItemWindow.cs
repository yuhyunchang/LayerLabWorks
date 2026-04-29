using UnityEngine;
using UnityEditor;

namespace LayerLabAsset
{
    public class Create2DWorldGridItemWindow : EditorWindow
    {
        [SerializeField] private Vector2 cellSize = new Vector2(1f, 1f);
        [SerializeField] private Vector2 spacing = new Vector2(0.1f, 0.1f);
        [SerializeField] private int columns = 4;
        [SerializeField] private int rows = 3;
        [SerializeField] private Vector2 groupSpacing = new Vector2(1f, 1f);

        [SerializeField] private Object prefabFolder;
        [SerializeField] private GameObject[] prefabs = new GameObject[0];

        private GameObject targetObject;
        private SerializedObject serializedWindow;
        private SerializedProperty prefabsProperty;
        private Vector2 scrollPosition;

        [MenuItem("LayerLabAsset/Create 2D World Grid Item", false, 103)]
        public static void ShowWindow()
        {
            var window = GetWindow<Create2DWorldGridItemWindow>("Create 2D World Grid Item");
            window.minSize = new Vector2(320f, 400f);
        }

        private void OnEnable()
        {
            serializedWindow = new SerializedObject(this);
            prefabsProperty = serializedWindow.FindProperty("prefabs");
        }

        private void OnGUI()
        {
            serializedWindow.Update();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // --- Target ---
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            targetObject = (GameObject)EditorGUILayout.ObjectField(
                "Parent Object", targetObject, typeof(GameObject), true);

            if (targetObject != null && targetObject.GetComponent<RectTransform>() != null)
                EditorGUILayout.HelpBox("UI 오브젝트(RectTransform)는 사용할 수 없습니다. 일반 Transform을 사용하세요.", MessageType.Warning);

            // --- 변경 감지 시작 ---
            EditorGUI.BeginChangeCheck();

            // --- Grid Settings ---
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            cellSize = EditorGUILayout.Vector2Field("Cell Size", cellSize);
            spacing = EditorGUILayout.Vector2Field("Spacing", spacing);
            columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", columns));
            rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", rows));
            groupSpacing = EditorGUILayout.Vector2Field("Group Spacing", groupSpacing);

            // --- Prefabs ---
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

            Object prevFolder = prefabFolder;
            prefabFolder = EditorGUILayout.ObjectField("Prefab Folder", prefabFolder, typeof(Object), false);
            if (prefabFolder != prevFolder && prefabFolder != null)
                LoadPrefabsFromFolder();

            EditorGUILayout.PropertyField(prefabsProperty, true);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Load Prefabs from Folder"))
            {
                if (prefabFolder != null)
                    LoadPrefabsFromFolder();
                else
                    EditorUtility.DisplayDialog("Error", "Prefab Folder를 먼저 지정하세요.", "OK");
            }

            bool changed = EditorGUI.EndChangeCheck();

            // --- 변경 시 자동 리빌드 ---
            if (changed && targetObject != null && prefabs != null && prefabs.Length > 0)
            {
                Undo.RegisterFullObjectHierarchyUndo(targetObject, "Auto Rebuild Grid");
                ClearGrid(targetObject.transform);
                BuildGrid(targetObject.transform);
            }

            // --- Buttons ---
            EditorGUILayout.Space(15);

            GUI.enabled = targetObject != null;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild Grid", GUILayout.Height(30)))
            {
                Undo.RegisterFullObjectHierarchyUndo(targetObject, "Rebuild Grid");
                ClearGrid(targetObject.transform);
                BuildGrid(targetObject.transform);
            }

            if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
            {
                Undo.RegisterFullObjectHierarchyUndo(targetObject, "Clear Grid");
                ClearGrid(targetObject.transform);
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            if (targetObject == null)
                EditorGUILayout.HelpBox("Parent Object를 지정하면 버튼이 활성화됩니다.", MessageType.Info);

            EditorGUILayout.EndScrollView();
            serializedWindow.ApplyModifiedProperties();
        }

        private void BuildGrid(Transform parent)
        {
            if (prefabs == null || prefabs.Length == 0 || columns <= 0 || rows <= 0)
                return;

            int itemsPerGroup = columns * rows;
            int totalGroups = Mathf.CeilToInt((float)prefabs.Length / itemsPerGroup);

            float groupWidth = columns * cellSize.x + (columns - 1) * spacing.x;
            float groupHeight = rows * cellSize.y + (rows - 1) * spacing.y;

            for (int g = 0; g < totalGroups; g++)
            {
                int startIndex = g * itemsPerGroup;
                int endIndex = Mathf.Min(startIndex + itemsPerGroup, prefabs.Length);

                Vector3 groupLocalPos = new Vector3(
                    g * (groupWidth + groupSpacing.x),
                    0f,
                    0f);

                GameObject groupObj = CreateGroup(parent, g, groupLocalPos);

                for (int i = startIndex; i < endIndex; i++)
                {
                    if (prefabs[i] == null) continue;

                    int localIndex = i - startIndex;
                    int col = localIndex % columns;
                    int row = localIndex / columns;

                    Vector3 itemLocalPos = new Vector3(
                        col * (cellSize.x + spacing.x),
                        -row * (cellSize.y + spacing.y),
                        0f);

                    CreateItem(groupObj.transform, prefabs[i], itemLocalPos);
                }
            }
        }

        private GameObject CreateGroup(Transform parent, int groupIndex, Vector3 localPos)
        {
            GameObject groupObj = new GameObject("Group_" + groupIndex);
            groupObj.transform.SetParent(parent, false);
            groupObj.transform.localPosition = localPos;
            groupObj.transform.localRotation = Quaternion.identity;
            groupObj.transform.localScale = Vector3.one;
            return groupObj;
        }

        private void CreateItem(Transform parent, GameObject prefab, Vector3 localPos)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null)
            {
                instance = Instantiate(prefab, parent);
                instance.name = prefab.name;
            }

            instance.transform.localPosition = localPos;
            instance.transform.localRotation = Quaternion.identity;

            Undo.RegisterCreatedObjectUndo(instance, "Create Grid Item");
        }

        private void ClearGrid(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }

        private void LoadPrefabsFromFolder()
        {
            string folderPath = AssetDatabase.GetAssetPath(prefabFolder);

            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog("Error", "유효하지 않은 폴더 경로입니다.", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Info", "해당 폴더에 프리팹이 없습니다.", "OK");
                return;
            }

            GameObject[] loaded = new GameObject[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                loaded[i] = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }

            System.Array.Sort(loaded, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            prefabs = loaded;
            serializedWindow.Update();
        }
    }
}