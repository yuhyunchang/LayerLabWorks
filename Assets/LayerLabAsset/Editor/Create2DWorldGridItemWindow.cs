using System.Collections.Generic;
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

        [SerializeField] private Object itemFolder;
        [SerializeField] private Object[] items = new Object[0];

        private GameObject targetObject;
        private SerializedObject serializedWindow;
        private SerializedProperty itemsProperty;
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
            itemsProperty = serializedWindow.FindProperty("items");
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

            // --- Items ---
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);

            Object prevFolder = itemFolder;
            itemFolder = EditorGUILayout.ObjectField("Asset Folder", itemFolder, typeof(Object), false);
            if (itemFolder != prevFolder && itemFolder != null)
                LoadItemsFromFolder();

            EditorGUILayout.PropertyField(itemsProperty, true);
            serializedWindow.ApplyModifiedProperties();
            NormalizeItems();
            serializedWindow.Update();

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Load Prefabs and Sprites from Folder"))
            {
                if (itemFolder != null)
                    LoadItemsFromFolder();
                else
                    EditorUtility.DisplayDialog("Error", "Asset Folder를 먼저 지정하세요.", "OK");
            }

            bool changed = EditorGUI.EndChangeCheck();

            // --- 변경 시 자동 리빌드 ---
            if (changed && targetObject != null && items != null && items.Length > 0)
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
            if (items == null || items.Length == 0 || columns <= 0 || rows <= 0)
                return;

            int itemsPerGroup = columns * rows;
            int totalGroups = Mathf.CeilToInt((float)items.Length / itemsPerGroup);

            for (int g = 0; g < totalGroups; g++)
            {
                int startIndex = g * itemsPerGroup;
                int endIndex = Mathf.Min(startIndex + itemsPerGroup, items.Length);

                GameObject groupObj = CreateGroup(parent, g, Vector3.zero);

                for (int i = startIndex; i < endIndex; i++)
                {
                    Object item = items[i];
                    if (!IsSupportedItem(item)) continue;

                    int localIndex = i - startIndex;
                    int col = localIndex % columns;
                    int row = localIndex / columns;

                    Vector3 itemLocalPos = new Vector3(
                        (col - (columns - 1) * 0.5f) * (cellSize.x + spacing.x),
                        -(row - (rows - 1) * 0.5f) * (cellSize.y + spacing.y),
                        0f);

                    CreateItem(groupObj.transform, item, itemLocalPos);
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

        private void CreateItem(Transform parent, Object item, Vector3 localPos)
        {
            GameObject instance = null;

            if (item is GameObject prefab)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                if (instance == null)
                {
                    instance = Instantiate(prefab, parent);
                    instance.name = prefab.name;
                }
            }
            else if (item is Sprite sprite)
            {
                instance = new GameObject(sprite.name);
                instance.transform.SetParent(parent, false);

                SpriteRenderer spriteRenderer = instance.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
            }

            if (instance == null)
                return;

            instance.transform.localPosition = localPos;
            instance.transform.localRotation = Quaternion.identity;

            Undo.RegisterCreatedObjectUndo(instance, "Create Grid Item");
        }

        private void ClearGrid(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }

        private void NormalizeItems()
        {
            if (items == null || items.Length == 0)
                return;

            List<Object> validItems = new List<Object>();
            bool changed = false;

            foreach (Object item in items)
            {
                if (item == null || IsSupportedItem(item))
                {
                    validItems.Add(item);
                }
                else
                {
                    changed = true;
                }
            }

            if (!changed)
                return;

            items = validItems.ToArray();
            serializedWindow.Update();
        }

        private bool IsSupportedItem(Object item)
        {
            return item is GameObject || item is Sprite;
        }

        private void LoadItemsFromFolder()
        {
            string folderPath = AssetDatabase.GetAssetPath(itemFolder);

            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog("Error", "유효하지 않은 폴더 경로입니다.", "OK");
                return;
            }

            HashSet<string> guidSet = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folderPath }))
                guidSet.Add(guid);

            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folderPath }))
                guidSet.Add(guid);

            if (guidSet.Count == 0)
            {
                EditorUtility.DisplayDialog("Info", "해당 폴더에 프리팹 또는 스프라이트가 없습니다.", "OK");
                return;
            }

            List<Object> loaded = new List<Object>();
            foreach (string guid in guidSet)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Object item = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (item == null)
                    item = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                if (item != null)
                    loaded.Add(item);
            }

            if (loaded.Count == 0)
            {
                EditorUtility.DisplayDialog("Info", "로드할 수 있는 프리팹 또는 스프라이트가 없습니다.", "OK");
                return;
            }

            loaded.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            items = loaded.ToArray();
            serializedWindow.Update();
        }
    }
}