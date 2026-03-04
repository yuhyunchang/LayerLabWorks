using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace LayerLabAsset
{
    public class CreateGridItemWindow : EditorWindow
    {
        [SerializeField] private Vector2 iconSize = new Vector2(100f, 100f);
        [SerializeField] private Vector2 spacing = new Vector2(10f, 10f);
        [SerializeField] private int columns = 4;
        [SerializeField] private int rows = 3;

        [SerializeField] private Object spriteFolder;
        [SerializeField] private Sprite[] sprites = new Sprite[0];

        [SerializeField] private bool useBackground;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color backgroundColor = Color.white;
        [SerializeField] private Vector2 iconOffset = Vector2.zero;

        private GameObject targetObject;
        private SerializedObject serializedWindow;
        private SerializedProperty spritesProperty;
        private Vector2 scrollPosition;

        [MenuItem("LayerLabAsset/Create Grid Item")]
        public static void ShowWindow()
        {
            var window = GetWindow<CreateGridItemWindow>("Create Grid Item");
            window.minSize = new Vector2(320f, 400f);
        }

        private void OnEnable()
        {
            serializedWindow = new SerializedObject(this);
            spritesProperty = serializedWindow.FindProperty("sprites");
        }

        private void OnGUI()
        {
            serializedWindow.Update();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // --- Target ---
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            targetObject = (GameObject)EditorGUILayout.ObjectField(
                "Parent Object", targetObject, typeof(GameObject), true);

            if (targetObject != null && targetObject.GetComponent<RectTransform>() == null)
                EditorGUILayout.HelpBox("UI 오브젝트(RectTransform)를 선택하세요.", MessageType.Warning);

            // --- 변경 감지 시작 ---
            EditorGUI.BeginChangeCheck();

            // --- Grid Settings ---
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            iconSize = EditorGUILayout.Vector2Field("Icon Size", iconSize);
            spacing = EditorGUILayout.Vector2Field("Spacing", spacing);
            columns = EditorGUILayout.IntField("Columns", columns);
            rows = EditorGUILayout.IntField("Rows", rows);

            // --- Sprites ---
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);

            Object prevFolder = spriteFolder;
            spriteFolder = EditorGUILayout.ObjectField("Sprite Folder", spriteFolder, typeof(Object), false);
            if (spriteFolder != prevFolder && spriteFolder != null)
                LoadSpritesFromFolder();

            EditorGUILayout.PropertyField(spritesProperty, true);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Load Sprites from Folder"))
            {
                if (spriteFolder != null)
                    LoadSpritesFromFolder();
                else
                    EditorUtility.DisplayDialog("Error", "Sprite Folder를 먼저 지정하세요.", "OK");
            }

            // --- Background ---
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Background", EditorStyles.boldLabel);
            useBackground = EditorGUILayout.Toggle("Use Background", useBackground);
            if (useBackground)
            {
                EditorGUI.indentLevel++;
                backgroundSprite = (Sprite)EditorGUILayout.ObjectField(
                    "Sprite (optional)", backgroundSprite, typeof(Sprite), false);
                backgroundColor = EditorGUILayout.ColorField("Color", backgroundColor);
                iconOffset = EditorGUILayout.Vector2Field("Icon Offset", iconOffset);
                EditorGUI.indentLevel--;
            }

            bool changed = EditorGUI.EndChangeCheck();

            // --- 변경 시 자동 리빌드 ---
            if (changed && targetObject != null && sprites != null && sprites.Length > 0)
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
            if (sprites == null || sprites.Length == 0 || columns <= 0 || rows <= 0)
                return;

            int itemsPerGroup = columns * rows;
            int totalGroups = Mathf.CeilToInt((float)sprites.Length / itemsPerGroup);

            for (int g = 0; g < totalGroups; g++)
            {
                int startIndex = g * itemsPerGroup;
                int endIndex = Mathf.Min(startIndex + itemsPerGroup, sprites.Length);

                GameObject groupObj = CreateGroup(parent, g);

                for (int i = startIndex; i < endIndex; i++)
                {
                    if (sprites[i] == null) continue;

                    if (useBackground)
                        CreateBackgroundItem(groupObj.transform, i);
                    else
                        CreateSimpleItem(groupObj.transform, i);
                }
            }
        }

        private GameObject CreateGroup(Transform parent, int groupIndex)
        {
            GameObject groupObj = new GameObject("Group_" + groupIndex);
            groupObj.transform.SetParent(parent, false);

            groupObj.AddComponent<RectTransform>().localScale = Vector3.one;

            GridLayoutGroup grid = groupObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = iconSize;
            grid.spacing = spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = groupObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return groupObj;
        }

        private void CreateSimpleItem(Transform parent, int index)
        {
            GameObject itemObj = new GameObject(sprites[index].name);
            itemObj.transform.SetParent(parent, false);
            itemObj.transform.localScale = Vector3.one;

            Image image = itemObj.AddComponent<Image>();
            image.sprite = sprites[index];
            image.preserveAspect = true;
        }

        private void CreateBackgroundItem(Transform parent, int index)
        {
            GameObject bgObj = new GameObject(sprites[index].name + "_BG");
            bgObj.transform.SetParent(parent, false);
            bgObj.transform.localScale = Vector3.one;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = backgroundSprite;
            bgImage.color = backgroundColor;

            GameObject iconObj = new GameObject(sprites[index].name);
            iconObj.transform.SetParent(bgObj.transform, false);
            iconObj.transform.localScale = Vector3.one;

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;
            iconRect.anchoredPosition = iconOffset;

            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = sprites[index];
            iconImage.preserveAspect = true;
        }

        private void ClearGrid(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private void LoadSpritesFromFolder()
        {
            string folderPath = AssetDatabase.GetAssetPath(spriteFolder);

            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog("Error", "유효하지 않은 폴더 경로입니다.", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Info", "해당 폴더에 스프라이트가 없습니다.", "OK");
                return;
            }

            Sprite[] loaded = new Sprite[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                loaded[i] = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            System.Array.Sort(loaded, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            sprites = loaded;
            serializedWindow.Update();
        }
    }
}
