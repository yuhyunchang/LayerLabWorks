using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace LayerLabAsset
{
    /// <summary>
    /// 타임스케일 컨트롤 설정 및 공유 데이터
    /// </summary>
    public static class TimeScaleSettings
    {
        private const string SceneViewKey = "LayerLabAsset_TimeScaleControl_SceneView";
        private const string MainToolbarKey = "LayerLabAsset_TimeScaleControl_MainToolbar";

        public static bool ShowInSceneView
        {
            get => EditorPrefs.GetBool(SceneViewKey, true);
            set => EditorPrefs.SetBool(SceneViewKey, value);
        }

        public static bool ShowInMainToolbar
        {
            get => EditorPrefs.GetBool(MainToolbarKey, true);
            set => EditorPrefs.SetBool(MainToolbarKey, value);
        }

        public static float CurrentTimeScale = 1f;
        public static event Action OnTimeScaleChanged;

        public static void SetTimeScale(float scale)
        {
            CurrentTimeScale = Mathf.Clamp(scale, 0f, 10f);
            if (EditorApplication.isPlaying)
            {
                Time.timeScale = CurrentTimeScale;
            }
            OnTimeScaleChanged?.Invoke();
        }

        public static void SyncFromGame()
        {
            if (EditorApplication.isPlaying && Mathf.Abs(Time.timeScale - CurrentTimeScale) > 0.01f)
            {
                CurrentTimeScale = Time.timeScale;
                OnTimeScaleChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// 메뉴를 통한 설정 변경
    /// </summary>
    public static class TimeScaleMenu
    {
        private const string SceneViewMenuPath = "LayerLabAsset/TimeScale Control/Show in Scene View";
        private const string MainToolbarMenuPath = "LayerLabAsset/TimeScale Control/Show in Main Toolbar";

        [MenuItem(SceneViewMenuPath, false, 200)]
        private static void ToggleSceneView()
        {
            TimeScaleSettings.ShowInSceneView = !TimeScaleSettings.ShowInSceneView;
        }

        [MenuItem(SceneViewMenuPath, true)]
        private static bool ToggleSceneViewValidate()
        {
            Menu.SetChecked(SceneViewMenuPath, TimeScaleSettings.ShowInSceneView);
            return true;
        }

        [MenuItem(MainToolbarMenuPath, false, 201)]
        private static void ToggleMainToolbar()
        {
            TimeScaleSettings.ShowInMainToolbar = !TimeScaleSettings.ShowInMainToolbar;
            MainToolbarTimeScale.RefreshToolbar();
        }

        [MenuItem(MainToolbarMenuPath, true)]
        private static bool ToggleMainToolbarValidate()
        {
            Menu.SetChecked(MainToolbarMenuPath, TimeScaleSettings.ShowInMainToolbar);
            return true;
        }
    }

    #region Scene View Overlay

    /// <summary>
    /// Scene View에 타임스케일 조절 오버레이
    /// </summary>
    [Overlay(typeof(SceneView), "Time Scale Control", true)]
    public class TimeScaleOverlay : ToolbarOverlay
    {
        public TimeScaleOverlay() : base(
            TimeScaleSlider.Id,
            TimeScaleValue.Id,
            TimeScale1xButton.Id,
            TimeScale2xButton.Id,
            TimeScale3xButton.Id,
            TimeScale5xButton.Id,
            TimeScale10xButton.Id,
            TimeScaleCloseButton.Id
        )
        { }

        public override void OnCreated()
        {
            base.OnCreated();
            displayed = TimeScaleSettings.ShowInSceneView;
            TimeScaleSettings.OnTimeScaleChanged += UpdateDisplayed;
        }

        public override void OnWillBeDestroyed()
        {
            TimeScaleSettings.OnTimeScaleChanged -= UpdateDisplayed;
            base.OnWillBeDestroyed();
        }

        private void UpdateDisplayed()
        {
            // 설정 변경 시 표시 여부 업데이트
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class TimeScaleLabel : VisualElement
    {
        public const string Id = "LayerLabAsset/TimeScaleControl/Label";

        public TimeScaleLabel()
        {
            var label = new Label("TimeScale:")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginRight = 5,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            Add(label);
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class TimeScaleSlider : VisualElement
    {
        public const string Id = "LayerLabAsset/TimeScaleControl/Slider";
        private Slider _slider;

        public TimeScaleSlider()
        {
            _slider = new Slider(0f, 10f)
            {
                value = TimeScaleSettings.CurrentTimeScale,
                style = { width = 100 }
            };
            _slider.RegisterValueChangedCallback(evt =>
            {
                TimeScaleSettings.SetTimeScale(evt.newValue);
            });

            TimeScaleSettings.OnTimeScaleChanged += () =>
            {
                _slider.SetValueWithoutNotify(TimeScaleSettings.CurrentTimeScale);
            };

            Add(_slider);

            EditorApplication.update += () => TimeScaleSettings.SyncFromGame();
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class TimeScaleValue : VisualElement
    {
        public const string Id = "LayerLabAsset/TimeScaleControl/Value";
        private Label _label;

        public TimeScaleValue()
        {
            _label = new Label($"{TimeScaleSettings.CurrentTimeScale:F1}x")
            {
                style =
                {
                    width = 40,
                    marginLeft = 5,
                    marginRight = 10,
                    color = new Color(1f, 0.9f, 0.4f),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };

            TimeScaleSettings.OnTimeScaleChanged += UpdateLabel;
            EditorApplication.update += () =>
            {
                TimeScaleSettings.SyncFromGame();
                UpdateLabel();
            };

            Add(_label);
        }

        private void UpdateLabel()
        {
            if (_label != null)
                _label.text = $"{TimeScaleSettings.CurrentTimeScale:F1}x";
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class TimeScale1xButton : EditorToolbarButton
    {
        public const string Id = "LayerLabAsset/TimeScaleControl/1x";
        public TimeScale1xButton()
        {
            text = "1x";
            clicked += () => TimeScaleSettings.SetTimeScale(1f);
            style.width = 24;
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class TimeScale2xButton : EditorToolbarButton
    {
        public const string Id = "LayerLabAsset/TimeScaleControl/2x";
        public TimeScale2xButton()
        {
            text = "2x";
            clicked += () => TimeScaleSettings.SetTimeScale(2f);
            style.width = 24;
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class TimeScale3xButton : EditorToolbarButton
    {
        public const string Id = "LayerLabAsset/TimeScaleControl/3x";
        public TimeScale3xButton()
        {
            text = "3x";
            clicked += () => TimeScaleSettings.SetTimeScale(3f);
            style.width = 24;
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class TimeScale5xButton : EditorToolbarButton
    {
        public const string Id = "LayerLabAsset/TimeScaleControl/5x";
        public TimeScale5xButton()
        {
            text = "5x";
            clicked += () => TimeScaleSettings.SetTimeScale(5f);
            style.width = 24;
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class TimeScale10xButton : EditorToolbarButton
    {
        public const string Id = "LayerLabAsset/TimeScaleControl/10x";
        public TimeScale10xButton()
        {
            text = "10x";
            clicked += () => TimeScaleSettings.SetTimeScale(10f);
            style.width = 28;
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class TimeScaleCloseButton : VisualElement
    {
        public const string Id = "LayerLabAsset/TimeScaleControl/Close";

        public TimeScaleCloseButton()
        {
            var button = new Label("×")
            {
                style =
                {
                    width = 16,
                    height = 16,
                    marginLeft = 8,
                    fontSize = 14,
                    color = new Color(0.6f, 0.6f, 0.6f),
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                button.style.color = new Color(1f, 0.4f, 0.4f);
            });
            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.style.color = new Color(0.6f, 0.6f, 0.6f);
            });
            button.RegisterCallback<MouseDownEvent>(evt =>
            {
                TimeScaleSettings.ShowInSceneView = false;
                foreach (var sceneView in SceneView.sceneViews)
                {
                    if (sceneView is SceneView sv)
                    {
                        sv.TryGetOverlay("Time Scale Control", out var overlay);
                        if (overlay != null)
                            overlay.displayed = false;
                    }
                }
            });

            Add(button);
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
        }
    }

    #endregion

    #region Main Toolbar

    /// <summary>
    /// 메인 툴바에 타임스케일 컨트롤 추가 (재생 버튼 우측)
    /// </summary>
    [InitializeOnLoad]
    public static class MainToolbarTimeScale
    {
        private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject _currentToolbar;
        private static VisualElement _toolbarRoot;
        private static VisualElement _timeScaleContainer;
        private static bool _initialized;

        private static Slider _slider;
        private static Label _valueLabel;

        static MainToolbarTimeScale()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            TimeScaleSettings.OnTimeScaleChanged += UpdateUI;
        }

        public static void RefreshToolbar()
        {
            if (_timeScaleContainer != null)
            {
                _timeScaleContainer.style.display = TimeScaleSettings.ShowInMainToolbar
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private static void OnUpdate()
        {
            if (_currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
                _currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

                if (_currentToolbar != null)
                {
                    var rootField = _currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (rootField != null)
                    {
                        _toolbarRoot = rootField.GetValue(_currentToolbar) as VisualElement;
                    }
                }
            }

            if (_toolbarRoot != null && !_initialized)
            {
                CreateTimeScaleUI();
            }

            TimeScaleSettings.SyncFromGame();
        }

        private static void CreateTimeScaleUI()
        {
            var toolbarZone = _toolbarRoot.Q("ToolbarZonePlayMode");
            if (toolbarZone == null)
            {
                toolbarZone = _toolbarRoot.Q("unity-editor-toolbar-container");
            }
            if (toolbarZone == null) return;

            _timeScaleContainer = new VisualElement
            {
                name = "LayerLabAsset_TimeScaleContainer",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginLeft = 15,
                    marginRight = 5,
                    height = 22,
                    display = TimeScaleSettings.ShowInMainToolbar ? DisplayStyle.Flex : DisplayStyle.None
                }
            };

            var separator = new VisualElement
            {
                style =
                {
                    width = 1,
                    height = 16,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f),
                    marginRight = 10
                }
            };
            _timeScaleContainer.Add(separator);


            _slider = new Slider(0f, 10f)
            {
                value = TimeScaleSettings.CurrentTimeScale,
                style =
                {
                    width = 80,
                    marginRight = 3
                }
            };
            _slider.RegisterValueChangedCallback(evt =>
            {
                TimeScaleSettings.SetTimeScale(evt.newValue);
            });
            _timeScaleContainer.Add(_slider);

            _valueLabel = new Label($"{TimeScaleSettings.CurrentTimeScale:F1}x")
            {
                style =
                {
                    width = 35,
                    marginRight = 8,
                    fontSize = 11,
                    color = new Color(1f, 0.85f, 0.3f),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            _timeScaleContainer.Add(_valueLabel);

            AddSpeedButton("1x", 1f);
            AddSpeedButton("2x", 2f);
            AddSpeedButton("3x", 3f);
            AddSpeedButton("5x", 5f);
            AddSpeedButton("10x", 10f);

            toolbarZone.Add(_timeScaleContainer);
            _initialized = true;
        }

        private static void AddSpeedButton(string text, float timeScale)
        {
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    height = 18,
                    minWidth = 28,
                    marginLeft = 2,
                    marginRight = 2,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 2,
                    paddingBottom = 2
                }
            };

            var normalBgColor = new Color(0.35f, 0.35f, 0.35f);
            var hoverBgColor = new Color(0.45f, 0.45f, 0.45f);
            var activeBgColor = new Color(0.25f, 0.25f, 0.25f);

            buttonContainer.style.backgroundColor = normalBgColor;
            buttonContainer.style.borderTopWidth = 1;
            buttonContainer.style.borderBottomWidth = 1;
            buttonContainer.style.borderLeftWidth = 1;
            buttonContainer.style.borderRightWidth = 1;
            buttonContainer.style.borderTopColor = new Color(0.2f, 0.2f, 0.2f);
            buttonContainer.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f);
            buttonContainer.style.borderLeftColor = new Color(0.2f, 0.2f, 0.2f);
            buttonContainer.style.borderRightColor = new Color(0.2f, 0.2f, 0.2f);
            buttonContainer.style.borderTopLeftRadius = 3;
            buttonContainer.style.borderTopRightRadius = 3;
            buttonContainer.style.borderBottomLeftRadius = 3;
            buttonContainer.style.borderBottomRightRadius = 3;

            var buttonLabel = new Label(text)
            {
                style =
                {
                    fontSize = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.85f, 0.85f, 0.85f),
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginTop = 0,
                    marginBottom = 0,
                    paddingTop = 0,
                    paddingBottom = 0
                }
            };
            buttonContainer.Add(buttonLabel);

            buttonContainer.RegisterCallback<MouseEnterEvent>(evt =>
            {
                buttonContainer.style.backgroundColor = hoverBgColor;
            });
            buttonContainer.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                buttonContainer.style.backgroundColor = normalBgColor;
            });
            buttonContainer.RegisterCallback<MouseDownEvent>(evt =>
            {
                buttonContainer.style.backgroundColor = activeBgColor;
            });
            buttonContainer.RegisterCallback<MouseUpEvent>(evt =>
            {
                buttonContainer.style.backgroundColor = hoverBgColor;
                TimeScaleSettings.SetTimeScale(timeScale);
            });

            _timeScaleContainer.Add(buttonContainer);
        }

        private static void UpdateUI()
        {
            if (_slider != null)
                _slider.SetValueWithoutNotify(TimeScaleSettings.CurrentTimeScale);
            if (_valueLabel != null)
                _valueLabel.text = $"{TimeScaleSettings.CurrentTimeScale:F1}x";
        }
    }

    #endregion
}