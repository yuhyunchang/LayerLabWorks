using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

namespace LayerLabAsset
{
    /// <summary>
    /// Favorites Panel - Editor window for quick access to frequently used assets
    /// </summary>
    public class FavoritesPanel : EditorWindow
    {
        private const string PREFS_KEY = "FavoritesPanel_Data_V2";

        private List<FavoriteItem> favoriteItems = new List<FavoriteItem>();
        private List<FavoriteGroup> groups = new List<FavoriteGroup>();
        private Vector2 scrollPosition;
        private string newGroupName = "";
        private bool showAddGroup = false;

        private ReorderableList ungroupedList;
        private Dictionary<string, ReorderableList> groupLists = new Dictionary<string, ReorderableList>();

        [System.Serializable]
        private class FavoriteItem
        {
            public string guid;
            public string path;
            public string name;
            public string assetType;
            public string groupId = "";
            public int order;

            public FavoriteItem(string guid, string path, string groupId = "")
            {
                this.guid = guid;
                this.path = path;
                this.name = System.IO.Path.GetFileName(path);
                this.groupId = groupId;

                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null)
                {
                    this.assetType = asset.GetType().Name;
                }
            }
        }

        [System.Serializable]
        public class FavoriteGroup
        {
            public string id;
            public string name;
            public bool isExpanded = true;
            public int order;

            public FavoriteGroup(string name)
            {
                this.id = System.Guid.NewGuid().ToString();
                this.name = name;
                this.isExpanded = true;
            }
        }

        [System.Serializable]
        private class FavoritesData
        {
            public List<FavoriteItem> items = new List<FavoriteItem>();
            public List<FavoriteGroup> groups = new List<FavoriteGroup>();
        }

        [MenuItem("Tools/Favorites Panel %#f")]
        public static void ShowWindow()
        {
            var window = GetWindow<FavoritesPanel>("Favorites");
            window.minSize = new Vector2(350, 200);
            window.Show();
        }

        private void OnEnable()
        {
            LoadFavorites();
            RebuildLists();
        }

        private void OnDisable()
        {
            SaveFavorites();
        }

        private void RebuildLists()
        {
            // Ungrouped list
            var ungroupedItems = favoriteItems.Where(x => string.IsNullOrEmpty(x.groupId)).ToList();
            ungroupedList = CreateReorderableList(ungroupedItems, "");

            // Group lists
            groupLists.Clear();
            foreach (var group in groups)
            {
                var groupItems = favoriteItems.Where(x => x.groupId == group.id).ToList();
                groupLists[group.id] = CreateReorderableList(groupItems, group.id);
            }
        }

        private ReorderableList CreateReorderableList(List<FavoriteItem> items, string groupId)
        {
            var list = new ReorderableList(items, typeof(FavoriteItem), true, false, false, false);

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= items.Count) return;
                DrawListItem(rect, items[index]);
            };

            list.elementHeight = 24;

            list.onReorderCallback = (ReorderableList l) =>
            {
                // Update order in main list
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].order = i;
                }

                SaveFavorites();
            };

            return list;
        }

        private void DrawListItem(Rect rect, FavoriteItem item)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(item.path);

            float x = rect.x;
            float xButtonWidth = 22;
            float selectButtonWidth = 45;
            float iconWidth = 20;
            float spacing = 2;

            // X (Delete) button - leftmost
            if (GUI.Button(new Rect(x, rect.y + 2, xButtonWidth, rect.height - 4), "X"))
            {
                EditorApplication.delayCall += () =>
                {
                    int idx = favoriteItems.IndexOf(item);
                    if (idx >= 0)
                    {
                        favoriteItems.RemoveAt(idx);
                        SaveFavorites();
                        RebuildLists();
                    }
                };
                return;
            }

            x += xButtonWidth + spacing;

            // Select button
            if (GUI.Button(new Rect(x, rect.y + 2, selectButtonWidth, rect.height - 4), "Select"))
            {
                if (asset != null)
                    PingAssetInProject(asset);
            }

            x += selectButtonWidth + spacing;

            if (asset == null)
            {
                EditorGUI.LabelField(new Rect(x, rect.y, rect.width - x + rect.x, rect.height),
                    "[Missing] " + item.name);
                return;
            }

            // Icon
            var icon = AssetDatabase.GetCachedIcon(item.path);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(x, rect.y + 2, iconWidth, iconWidth), icon, ScaleMode.ScaleToFit);
            }

            x += iconWidth + spacing;

            // Path + Name (clickable)
            string folderPath = System.IO.Path.GetDirectoryName(item.path);
            if (!string.IsNullOrEmpty(folderPath))
            {
                if (folderPath.StartsWith("Assets/"))
                    folderPath = folderPath.Substring(7);
                else if (folderPath.StartsWith("Assets"))
                    folderPath = folderPath.Substring(6);
            }
            else
            {
                folderPath = "";
            }

            float remainingWidth = rect.width - (x - rect.x);
            Rect labelRect = new Rect(x, rect.y, remainingWidth, rect.height);

            // Click detection
            if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    OpenOrSelectAsset(asset, item.path);
                    Event.current.Use();
                }
                else if (Event.current.button == 1)
                {
                    ShowItemContextMenu(item);
                    Event.current.Use();
                }
            }

            // Draw path (gray) + name (bold)
            var pathStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            var nameStyle = new GUIStyle(EditorStyles.boldLabel);

            string pathText = string.IsNullOrEmpty(folderPath) ? "" : folderPath + "/";
            float pathWidth = string.IsNullOrEmpty(pathText) ? 0 : pathStyle.CalcSize(new GUIContent(pathText)).x;

            if (pathWidth > 0)
                EditorGUI.LabelField(new Rect(x, rect.y, pathWidth, rect.height), pathText, pathStyle);
            EditorGUI.LabelField(new Rect(x + pathWidth, rect.y, remainingWidth - pathWidth, rect.height), item.name, nameStyle);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawContent();
            HandleDragAndDrop();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Favorites", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ Group", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                showAddGroup = !showAddGroup;
                newGroupName = "";
            }

            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Clear Favorites", "Delete all favorites and groups?", "Yes", "No"))
                {
                    favoriteItems.Clear();
                    groups.Clear();
                    SaveFavorites();
                    RebuildLists();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (showAddGroup)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                newGroupName = EditorGUILayout.TextField("Group Name", newGroupName);

                GUI.enabled = !string.IsNullOrEmpty(newGroupName);
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    AddGroup(newGroupName);
                    showAddGroup = false;
                    newGroupName = "";
                }

                GUI.enabled = true;

                if (GUILayout.Button("Cancel", GUILayout.Width(50)))
                {
                    showAddGroup = false;
                    newGroupName = "";
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Ungrouped section header (for dropping items out of groups)
            Rect ungroupedHeader = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Ungrouped", EditorStyles.boldLabel);
            int ungroupedCount = favoriteItems.Count(x => string.IsNullOrEmpty(x.groupId));
            GUILayout.Label($"({ungroupedCount})", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Drop target for ungrouped
            HandleDropToUngrouped(ungroupedHeader);

            // Draw ungrouped items
            if (ungroupedList != null && ungroupedList.count > 0)
            {
                ungroupedList.DoLayoutList();
            }
            else if (ungroupedCount == 0)
            {
                EditorGUILayout.LabelField("(Drag files here to add)", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.Space(10);

            // Groups
            foreach (var group in groups.OrderBy(g => g.order).ToList())
            {
                DrawGroup(group);
            }

            EditorGUILayout.EndScrollView();
        }

        private void HandleDropToUngrouped(Rect dropArea)
        {
            Event evt = Event.current;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    // Check if dragging from another group
                    if (DragAndDrop.GetGenericData("FavoriteItem") is FavoriteItem draggedItem)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            draggedItem.groupId = "";
                            SaveFavorites();
                            RebuildLists();
                        }

                        evt.Use();
                    }
                }
            }
        }

        private void DrawGroup(FavoriteGroup group)
        {
            // Group header
            Rect headerRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Fold/Unfold
            string foldoutIcon = group.isExpanded ? "▼" : "▶";
            if (GUILayout.Button(foldoutIcon, EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                group.isExpanded = !group.isExpanded;
                SaveFavorites();
            }

            // Group name
            GUILayout.Label(group.name, EditorStyles.boldLabel);

            // Item count
            int itemCount = favoriteItems.Count(x => x.groupId == group.id);
            GUILayout.Label($"({itemCount})", EditorStyles.miniLabel, GUILayout.Width(30));

            GUILayout.FlexibleSpace();

            // Group menu
            if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                ShowGroupContextMenu(group);
            }

            EditorGUILayout.EndHorizontal();

            // Handle drop to group
            HandleDropToGroup(headerRect, group);

            // Group content
            if (group.isExpanded)
            {
                EditorGUI.indentLevel++;

                if (groupLists.TryGetValue(group.id, out var list) && list.count > 0)
                {
                    list.DoLayoutList();
                }
                else if (itemCount == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField("(Drag items here)", EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }
        }

        private void HandleDropToGroup(Rect dropArea, FavoriteGroup group)
        {
            Event evt = Event.current;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    // Check if dragging a FavoriteItem
                    if (DragAndDrop.GetGenericData("FavoriteItem") is FavoriteItem draggedItem)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            draggedItem.groupId = group.id;
                            SaveFavorites();
                            RebuildLists();
                        }

                        evt.Use();
                    }
                    // Check if dragging from Project window
                    else if (DragAndDrop.objectReferences.Length > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (var obj in DragAndDrop.objectReferences)
                            {
                                AddToFavorites(obj, group.id);
                            }
                        }

                        evt.Use();
                    }
                }
            }
        }

        private void ShowGroupContextMenu(FavoriteGroup group)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Rename"), false, () =>
            {
                RenameGroupPopup.Show(group, (newName) =>
                {
                    group.name = newName;
                    SaveFavorites();
                });
            });

            menu.AddItem(new GUIContent("Delete Group (Keep Items)"), false, () =>
            {
                foreach (var item in favoriteItems.Where(x => x.groupId == group.id))
                {
                    item.groupId = "";
                }

                groups.Remove(group);
                SaveFavorites();
                RebuildLists();
            });

            menu.AddItem(new GUIContent("Delete Group (Include Items)"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Delete Group", $"Delete '{group.name}' group and all items?", "Yes", "No"))
                {
                    favoriteItems.RemoveAll(x => x.groupId == group.id);
                    groups.Remove(group);
                    SaveFavorites();
                    RebuildLists();
                }
            });

            menu.ShowAsContext();
        }

        private void ShowItemContextMenu(FavoriteItem item)
        {
            GenericMenu menu = new GenericMenu();

            // Remove from group
            if (!string.IsNullOrEmpty(item.groupId))
            {
                menu.AddItem(new GUIContent("Remove from Group"), false, () =>
                {
                    item.groupId = "";
                    SaveFavorites();
                    RebuildLists();
                });
            }

            // Move to other groups
            if (groups.Count > 0)
            {
                menu.AddSeparator("");
                foreach (var group in groups)
                {
                    if (group.id != item.groupId)
                    {
                        string groupName = group.name;
                        string groupId = group.id;
                        menu.AddItem(new GUIContent($"Move to '{groupName}'"), false, () =>
                        {
                            item.groupId = groupId;
                            SaveFavorites();
                            RebuildLists();
                        });
                    }
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                int idx = favoriteItems.IndexOf(item);
                if (idx >= 0)
                {
                    favoriteItems.RemoveAt(idx);
                    SaveFavorites();
                    RebuildLists();
                }
            });

            menu.ShowAsContext();
        }

        private void HandleDragAndDrop()
        {
            Event evt = Event.current;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                // Only handle drops from Project window (not internal drags)
                if (DragAndDrop.GetGenericData("FavoriteItem") == null && DragAndDrop.objectReferences.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            AddToFavorites(obj);
                        }
                    }

                    evt.Use();
                }
            }
        }

        private void AddGroup(string name)
        {
            var group = new FavoriteGroup(name);
            group.order = groups.Count;
            groups.Add(group);
            SaveFavorites();
            RebuildLists();
        }

        private void AddToFavorites(Object asset, string groupId = "")
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return;

            string guid = AssetDatabase.AssetPathToGUID(path);

            if (favoriteItems.Any(x => x.guid == guid))
            {
                Debug.Log($"'{asset.name}' is already in favorites.");
                return;
            }

            var item = new FavoriteItem(guid, path, groupId);
            item.order = favoriteItems.Count(x => x.groupId == groupId);
            favoriteItems.Add(item);
            SaveFavorites();
            RebuildLists();
            Debug.Log($"'{asset.name}' added to favorites.");
        }

        private void OpenOrSelectAsset(Object asset, string path)
        {
            if (path.EndsWith(".unity"))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                }

                return;
            }

            if (asset is GameObject && path.EndsWith(".prefab"))
            {
                AssetDatabase.OpenAsset(asset);
                return;
            }

            if (asset is MonoScript || asset is TextAsset ||
                path.EndsWith(".cs") || path.EndsWith(".txt") ||
                path.EndsWith(".json") || path.EndsWith(".xml"))
            {
                AssetDatabase.OpenAsset(asset);
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void PingAssetInProject(Object asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            EditorUtility.FocusProjectWindow();
        }

        private void SaveFavorites()
        {
            var data = new FavoritesData { items = favoriteItems, groups = groups };
            string json = JsonUtility.ToJson(data);
            EditorPrefs.SetString(PREFS_KEY, json);
        }

        private void LoadFavorites()
        {
            favoriteItems.Clear();
            groups.Clear();

            string json = EditorPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<FavoritesData>(json);
                    if (data != null)
                    {
                        if (data.groups != null)
                            groups = data.groups;

                        if (data.items != null)
                        {
                            foreach (var item in data.items)
                            {
                                string currentPath = AssetDatabase.GUIDToAssetPath(item.guid);
                                if (!string.IsNullOrEmpty(currentPath))
                                {
                                    item.path = currentPath;
                                    item.name = System.IO.Path.GetFileName(currentPath);
                                }

                                favoriteItems.Add(item);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load favorites: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Rename Group Popup
    /// </summary>
    public class RenameGroupPopup : EditorWindow
    {
        private string groupName;
        private System.Action<string> onRename;

        public static void Show(FavoritesPanel.FavoriteGroup group, System.Action<string> callback)
        {
            var window = CreateInstance<RenameGroupPopup>();
            window.groupName = group.name;
            window.onRename = callback;
            window.titleContent = new GUIContent("Rename Group");
            window.ShowUtility();
            window.minSize = new Vector2(250, 70);
            window.maxSize = new Vector2(250, 70);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            groupName = EditorGUILayout.TextField("Name", groupName);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = !string.IsNullOrEmpty(groupName);
            if (GUILayout.Button("OK", GUILayout.Width(60)))
            {
                onRename?.Invoke(groupName);
                Close();
            }

            GUI.enabled = true;

            if (GUILayout.Button("Cancel", GUILayout.Width(60)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}