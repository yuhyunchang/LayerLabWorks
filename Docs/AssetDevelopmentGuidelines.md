# Unity 에셋 제작 가이드라인

재사용 가능하고 유지보수하기 쉬운 Unity 에셋을 제작하기 위한 상세 지침입니다.

---

## 목차

1. [아키텍처 설계 원칙](#아키텍처-설계-원칙)
2. [코드 품질 가이드](#코드-품질-가이드)
3. [의존성 관리](#의존성-관리)
4. [패키지 구조](#패키지-구조)
5. [에디터 확장](#에디터-확장)
6. [성능 고려사항](#성능-고려사항)
7. [테스트 및 검증](#테스트-및-검증)
8. [배포 준비](#배포-준비)

---

## 아키텍처 설계 원칙

### 1. 단일 책임 원칙 (SRP)

각 클래스는 하나의 책임만 가져야 합니다.

```csharp
// Bad: 여러 책임을 가진 클래스
public class GameManager : MonoBehaviour
{
    public void SaveGame() { }
    public void LoadGame() { }
    public void PlaySound() { }
    public void ShowUI() { }
}

// Good: 분리된 책임
public class SaveManager : Singleton<SaveManager> { }
public class AudioManager : Singleton<AudioManager> { }
public class UIManager : Singleton<UIManager> { }
```

### 2. 개방-폐쇄 원칙 (OCP)

확장에는 열려있고, 수정에는 닫혀있어야 합니다.

```csharp
// PopupBase를 상속하여 새 팝업 타입 추가
public class InventoryPopup : PopupBase
{
    public override void Show()
    {
        base.Show();
        LoadInventoryData();
    }
}
```

### 3. 의존성 역전 원칙 (DIP)

구체적인 구현이 아닌 추상화에 의존합니다.

```csharp
// 인터페이스 정의
public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

// FsmState가 인터페이스 구현
public class FsmState<T> : IState where T : Enum
{
    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
```

### 4. 컴포지션 우선

상속보다 컴포지션을 선호합니다.

```csharp
// PopupBase + PopupAnimation 조합
public class PopupBase : MonoBehaviour
{
    private PopupAnimation PopupAnimation { get; set; }

    protected virtual void Init()
    {
        TryGetComponent(out PopupAnimation panelAnimation);
        if (panelAnimation != null)
        {
            PopupAnimation = panelAnimation;
            PopupAnimation.Init();
        }
    }
}
```

---

## 코드 품질 가이드

### 네이밍 컨벤션

| 요소 | 규칙 | 예시 |
|-----|------|------|
| Private 필드 | _camelCase | `_instance`, `_stateList` |
| Public 필드 | PascalCase | `PopupType`, `IsScaleAnim` |
| 프로퍼티 | PascalCase | `Instance`, `CurrentState` |
| 메서드 | PascalCase | `AddState()`, `SetView()` |
| 매개변수 | camelCase | `popupType`, `isShow` |
| 상수 | UPPER_SNAKE | `PACKAGE_NAME`, `PREFS_KEY` |
| Enum | PascalCase | `PopupSameType.UI` |

### SerializeField 사용

```csharp
// 자동 프로퍼티 직렬화 (Unity 2019.3+)
[field: SerializeField] public bool IsScaleAnim { get; set; } = true;

// private 필드 직렬화
[SerializeField] private RectTransform rect;
[SerializeField] private bool isScale;
```

### Null 안전 패턴

```csharp
// 1. Early return 패턴
public virtual void AddState(FsmState<T> state)
{
    if (state == null)
    {
        Debug.LogError("FsmClass::AddFsm()[ null == FsmState<T>");
        return;
    }
    // 이후 로직
}

// 2. Null 조건 연산자
_currentState?.Update();
OnCloseEvent?.Invoke();

// 3. Null 병합 연산자
var component = GetComponent<MyComponent>() ?? gameObject.AddComponent<MyComponent>();

// 4. TryGetComponent 패턴
if (TryGetComponent(out PopupAnimation animation))
{
    animation.Init();
}
```

### 컬렉션 초기화

```csharp
// 필드 레벨에서 초기화
private readonly Dictionary<T, FsmState<T>> _stateList = new();
public List<PopupBase> popupUI { get; set; } = new();

// TryAdd로 중복 방지
if (!_stateList.TryAdd(state.StateType, state))
{
    Debug.LogError("State already exists: " + state.StateType);
}
```

---

## 의존성 관리

### 선택적 의존성 처리

```csharp
// asmdef에서 선택적 참조 설정
// LayerLabAsset.Runtime.asmdef
{
    "references": [],
    "optionalUnityReferences": [],
    "defineConstraints": []
}

// 코드에서 조건부 컴파일
#if UNITASK_EXISTS
    await UniTask.Delay(1000);
#else
    yield return new WaitForSeconds(1f);
#endif
```

### 내장 대안 제공

DOTween 없이도 동작하는 코루틴 기반 애니메이션:

```csharp
private IEnumerator ScaleTo(float targetScale, float duration, EaseType easeType)
{
    Vector3 startScale = transform.localScale;
    Vector3 endScale = Vector3.one * targetScale;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float easedT = easeType == EaseType.OutElastic ? EaseOutElastic(t) : EaseOutCubic(t);
        transform.localScale = Vector3.LerpUnclamped(startScale, endScale, easedT);
        yield return null;
    }

    transform.localScale = endScale;
}

// Easing 함수 구현
private static float EaseOutCubic(float t)
{
    return 1f - Mathf.Pow(1f - t, 3f);
}

private static float EaseOutElastic(float t)
{
    if (t == 0f) return 0f;
    if (t == 1f) return 1f;
    float p = 0.3f;
    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
}
```

---

## 패키지 구조

### Assembly Definition 구성

```
Assets/LayerLabAsset/
├── Editor/
│   └── LayerLabAsset.Editor.asmdef
│       {
│         "name": "LayerLabAsset.Editor",
│         "references": ["LayerLabAsset.Runtime"],
│         "includePlatforms": ["Editor"],
│         "excludePlatforms": []
│       }
└── Runtime/
    └── LayerLabAsset.Runtime.asmdef
        {
          "name": "LayerLabAsset.Runtime",
          "references": [],
          "includePlatforms": [],
          "excludePlatforms": []
        }
```

### package.json 구성

```json
{
    "name": "com.layerlab.asset",
    "displayName": "LayerLab Asset",
    "version": "1.0.0",
    "unity": "6000.0",
    "description": "Unity utility package - Singleton, FSM, Popup Manager, UI components",
    "keywords": ["singleton", "fsm", "popup", "ui"],
    "author": {
        "name": "LayerLab",
        "url": "https://github.com/yuhyunchang"
    },
    "dependencies": {
        "com.unity.textmeshpro": "3.0.0"
    }
}
```

### 리소스 경로 규칙

```
Assets/
└── Resources/
    └── _UI/
        └── Popup/
            ├── Settings.prefab    # PopupType.Settings
            ├── Inventory.prefab   # PopupType.Inventory
            └── Confirm.prefab     # PopupType.Confirm
```

---

## 에디터 확장

### MenuItem 구성

```csharp
// 메뉴 그룹화
[MenuItem("LayerLabAsset/Update Package")]
[MenuItem("LayerLabAsset/Favorites Panel %#f")]  // Ctrl+Shift+F
[MenuItem("LayerLabAsset/Disable Raycast Target")]
[MenuItem("LayerLabAsset/Reset PlayerPrefs")]

// 서브메뉴
[MenuItem("Tools/WebGL Optimization/Reimport All Textures")]
[MenuItem("Tools/WebGL Optimization/Show Texture Statistics")]
```

### Undo 지원

```csharp
// 변경 전 Undo 기록
Undo.RecordObject(img, "Set Raycast Target");
img.raycastTarget = targetValue;
```

### 진행 상황 표시

```csharp
try
{
    AssetDatabase.StartAssetEditing();

    for (int i = 0; i < total; i++)
    {
        EditorUtility.DisplayProgressBar(
            "Processing",
            $"File {i}/{total}",
            (float)i / total);

        // 작업 수행
    }
}
finally
{
    AssetDatabase.StopAssetEditing();
    EditorUtility.ClearProgressBar();
}
```

### 확인 다이얼로그

```csharp
if (EditorUtility.DisplayDialog(
    "Clear Favorites",
    "Delete all favorites and groups?",
    "Yes", "No"))
{
    // 실행
}
```

### EditorWindow 구현

```csharp
public class FavoritesPanel : EditorWindow
{
    [MenuItem("LayerLabAsset/Favorites Panel %#f")]
    public static void ShowWindow()
    {
        var window = GetWindow<FavoritesPanel>("Favorites");
        window.minSize = new Vector2(350, 200);
        window.Show();
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void OnDisable()
    {
        SaveData();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawContent();
        HandleDragAndDrop();
    }
}
```

---

## 성능 고려사항

### 1. 캐싱

```csharp
// 컴포넌트 캐싱
private Canvas _canvas;

private void Awake()
{
    _canvas = GetComponent<Canvas>();
}

// Dictionary로 상태 캐싱
private readonly Dictionary<T, FsmState<T>> _stateList = new();
```

### 2. 메모리 관리

```csharp
// 이벤트 해제
protected override void OnDestroy()
{
    base.OnDestroy();
    onClick.RemoveAllListeners();
}

// IDisposable 정리
public virtual void Close()
{
    Disposable?.Dispose();
}

// 코루틴 정리
private void StopScaleCoroutine()
{
    if (_scaleCoroutine != null)
    {
        StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = null;
    }
}
```

### 3. GC 최적화

```csharp
// 문자열 보간 대신 StringBuilder (반복 호출 시)
private StringBuilder _sb = new StringBuilder();

// foreach 대신 for (리스트)
for (int i = popupUI.Count - 1; i >= 0; i--)
{
    RemoveList(PopupSameType.UI, popupUI[i]);
}
```

### 4. Time.unscaledDeltaTime

TimeScale에 영향받지 않는 애니메이션:

```csharp
while (elapsed < duration)
{
    elapsed += Time.unscaledDeltaTime;  // TimeScale=0에서도 동작
    // ...
    yield return null;
}
```

---

## 테스트 및 검증

### 체크리스트

**코드 품질:**
- [ ] 모든 public API에 XML 문서 주석
- [ ] 의미 있는 에러 메시지
- [ ] null 체크 및 경계 조건 처리

**기능:**
- [ ] 새 씬에서 단독 동작 확인
- [ ] 다른 에셋과 충돌 없음
- [ ] Editor/Runtime 빌드 모두 성공

**성능:**
- [ ] 프로파일러로 GC Alloc 확인
- [ ] 메모리 누수 확인 (이벤트 해제)

**호환성:**
- [ ] 지원 Unity 버전에서 테스트
- [ ] 다양한 플랫폼 빌드 테스트

---

## 배포 준비

### 버전 관리

Semantic Versioning 사용:
- **MAJOR**: 호환되지 않는 API 변경
- **MINOR**: 하위 호환 기능 추가
- **PATCH**: 하위 호환 버그 수정

### CHANGELOG.md 작성

```markdown
# Changelog

## [1.1.0] - 2024-XX-XX

### Added
- PopupAnimation에 새로운 이징 함수 추가

### Changed
- UIButton 기본 스케일 값 1.05에서 1.03으로 변경

### Fixed
- FSM 상태 변경 중 중복 호출 방지
```

### README.md 필수 항목

1. **설치 방법** (UPM, Git URL)
2. **요구사항** (Unity 버전, 의존성)
3. **빠른 시작** 예제
4. **주요 기능** 목록
5. **라이선스**

### Git 설정 (.gitignore)

```
# Unity
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
*.csproj
*.sln

# OS
.DS_Store
Thumbs.db

# IDE
.idea/
.vscode/
*.suo
```

---

## 참고 자료

- [Unity Package Manager](https://docs.unity3d.com/Manual/Packages.html)
- [Assembly Definitions](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html)
- [Semantic Versioning](https://semver.org/)
- [Unity Style Guide](https://github.com/raywenderlich/c-sharp-style-guide)