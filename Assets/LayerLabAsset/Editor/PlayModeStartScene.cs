using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LayerLabAsset
{
    [InitializeOnLoad]
    public static class ForceStartScene
    {
        private const string MenuPath = "LayerLabAsset/Force Start Scene";
        private const string PreviousSceneKey = "PreviousScenePath";
        private const string EnabledKey = "ForceStartScene_Enabled";

        static ForceStartScene()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuPath, false, 104)]
        private static void Toggle()
        {
            bool next = !EditorPrefs.GetBool(EnabledKey, false);
            EditorPrefs.SetBool(EnabledKey, next);
            UnityEngine.Debug.Log($"Force Start Scene: {(next ? "Enabled" : "Disabled")}");
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(EnabledKey, false));
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(EnabledKey, false)) return;

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    var currentScenePath = SceneManager.GetActiveScene().path;
                    var firstScenePath = EditorBuildSettings.scenes[0].path;

                    // 항상 현재 씬 저장
                    EditorPrefs.SetString(PreviousSceneKey, currentScenePath);

                    if (currentScenePath != firstScenePath)
                    {
                        if (SceneManager.GetActiveScene().isDirty)
                        {
                            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                        }

                        EditorSceneManager.OpenScene(firstScenePath);
                    }
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    string previousScenePath = EditorPrefs.GetString(PreviousSceneKey);

                    if (!string.IsNullOrEmpty(previousScenePath) &&
                        previousScenePath != SceneManager.GetActiveScene().path)
                    {
                        EditorSceneManager.OpenScene(previousScenePath);
                    }
                    break;
            }
        }
    }
}
