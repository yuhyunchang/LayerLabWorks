using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LayerLabAsset
{
    [InitializeOnLoad]
    public static class ForceStartScene
    {
        private const string PreviousSceneKey = "PreviousScenePath";
        private const string EnabledKey = "ForceStartScene_Enabled";

        static ForceStartScene()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Force Start Scene/Enable")]
        private static void Enable()
        {
            EditorPrefs.SetBool(EnabledKey, true);
            UnityEngine.Debug.Log("Force Start Scene: Enabled");
        }

        [MenuItem("Tools/Force Start Scene/Disable")]
        private static void Disable()
        {
            EditorPrefs.SetBool(EnabledKey, false);
            UnityEngine.Debug.Log("Force Start Scene: Disabled");
        }

        [MenuItem("Tools/Force Start Scene/Enable", true)]
        private static bool EnableValidate()
        {
            return !EditorPrefs.GetBool(EnabledKey, false);
        }

        [MenuItem("Tools/Force Start Scene/Disable", true)]
        private static bool DisableValidate()
        {
            return EditorPrefs.GetBool(EnabledKey, false);
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