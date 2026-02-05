# LayerLab Asset 개발 가이드

Unity 유틸리티 패키지 - Singleton, FSM, Popup Manager, UI 컴포넌트를 제공하는 재사용 가능한 에셋 라이브러리입니다.

---

## 목차

1. [요구사항](#요구사항)
2. [프로젝트 구조](#프로젝트-구조)
3. [핵심 컴포넌트](#핵심-컴포넌트)
   - [Singleton 패턴](#singleton-패턴)
   - [FSM (유한 상태 기계)](#fsm-유한-상태-기계)
   - [Popup Manager](#popup-manager)
   - [UI Components](#ui-components)
4. [Editor Tools](#editor-tools)
5. [에셋 제작 지침](#에셋-제작-지침)

---

## 요구사항

- **Unity 6000.0+** (Unity 6)
- **TextMeshPro** (필수)

---

## 프로젝트 구조

```
Assets/LayerLabAsset/
├── Editor/                          # 에디터 전용 도구
│   ├── EditorTools.cs               # 패키지 업데이트, Raycast Target 관리
│   ├── FavoritesPanel.cs            # 즐겨찾기 패널
│   ├── PlayModeStartScene.cs        # 플레이 모드 시작 씬 설정
│   ├── TimeScaleControl.cs          # 타임스케일 조절 도구
│   ├── AudioImportSettingsOptimizer.cs
│   └── TextureImportSettingsOptimizer.cs
├── Runtime/
│   ├── Pattern/
│   │   ├── Singleton.cs             # 싱글톤 베이스 클래스
│   │   └── Fsm/
│   │       ├── FsmClass.cs          # Enum 기반 FSM
│   │       ├── FsmState.cs          # FSM 상태 베이스
│   │       ├── CharacterFSM.cs      # 캐릭터용 FSM
│   │       └── CharacterStateBase.cs
│   ├── Manager/
│   │   └── Popup/
│   │       ├── PopupManager.cs      # 팝업 생명주기 관리
│   │       ├── PopupBase.cs         # 팝업 베이스 클래스
│   │       └── PopupAnimation.cs    # 팝업 애니메이션
│   └── UI/
│       ├── UIButton.cs              # 스케일 애니메이션 버튼
│       └── UICanvasView.cs          # 캔버스 뷰 베이스
├── package.json                     # UPM 패키지 설정
└── README.md
```

---

## 핵심 컴포넌트

### Singleton 패턴

제네릭 싱글톤 패턴을 제공하여 MonoBehaviour 기반 매니저 클래스를 쉽게 구현할 수 있습니다.

#### 특징
- Thread-safe 구현 (lock 사용)
- Lazy initialization
- 자동 GameObject 생성 및 DontDestroyOnLoad 적용
- 중복 인스턴스 방지 (`[DisallowMultipleComponent]`)

#### 사용법

```csharp
using LayerLabAsset;

public class GameManager : Singleton<GameManager>
{
    public int Score { get; private set; }

    public void AddScore(int points)
    {
        Score += points;
    }
}

// 사용
GameManager.Instance.AddScore(100);
```

#### 주의사항
- `Application.isPlaying`이 false일 때 Instance 접근 시 null 반환
- 생성자를 통한 직접 생성 방지를 위해 `protected T() {}` 추가 권장

---

### FSM (유한 상태 기계)

두 가지 FSM 구현을 제공합니다.

#### 1. FsmClass<T> - Enum 기반 FSM

게임 상태, UI 상태 등을 관리할 때 적합합니다.

```csharp
// 상태 Enum 정의
public enum GameState
{
    Idle,
    Playing,
    Paused,
    GameOver
}

// 상태 클래스 구현
public class IdleState : FsmState<GameState>
{
    public IdleState() : base(GameState.Idle) { }

    public override void Enter()
    {
        Debug.Log("Idle 상태 진입");
    }

    public override void Update()
    {
        // 프레임별 로직
    }

    public override void Exit()
    {
        Debug.Log("Idle 상태 종료");
    }
}

// FSM 사용
var fsm = new FsmClass<GameState>();
fsm.AddState(new IdleState());
fsm.AddState(new PlayingState());
fsm.SetState(GameState.Idle);

// Update에서 호출
fsm.Update();
```

#### 2. CharacterFSM<T> - 캐릭터 기반 FSM

캐릭터 컨트롤러와 함께 사용하기 적합합니다.

```csharp
public class PlayerController : MonoBehaviour
{
    private CharacterFSM<PlayerController> _fsm;

    void Start()
    {
        _fsm = new CharacterFSM<PlayerController>();
        _fsm.AddState(new PlayerIdleState(this, this));
        _fsm.AddState(new PlayerRunState(this, this));
        _fsm.ChangeState(this);
    }

    void Update()
    {
        _fsm.Update();
    }
}

public class PlayerIdleState : CharacterStateBase<PlayerController>
{
    public PlayerIdleState(PlayerController owner, PlayerController stateType)
        : base(owner, stateType) { }

    public override void Enter()
    {
        Owner.PlayAnimation("Idle");
    }
}
```

#### FSM 안전장치
- 상태 변경 중 중복 변경 방지 (`_isStateChanging` 플래그)
- 동일 상태로의 변경 무시
- 존재하지 않는 상태 접근 시 에러 로그

---

### Popup Manager

팝업 UI의 생명주기를 관리하는 시스템입니다.

#### 팝업 카테고리

| 카테고리 | 용도 |
|---------|------|
| `popupSystem` | 시스템 알림, 에러 메시지 등 최상위 팝업 |
| `popupUI` | 일반 UI 팝업 |
| `popupIgnore` | 다른 팝업과 독립적인 팝업 |
| `popupQueue` | 순차적으로 표시되는 팝업 큐 |

#### 설정

1. `PopupType` enum에 팝업 타입 추가:
```csharp
public enum PopupType
{
    Settings,
    Inventory,
    Shop,
    Confirm
}
```

2. 팝업 프리팹을 `Resources/_UI/Popup/{PopupType}` 경로에 배치

3. 팝업 클래스 구현:
```csharp
public class SettingsPopup : PopupBase
{
    protected override void Init()
    {
        base.Init();
        // 초기화 로직
    }

    public override void Show()
    {
        base.Show();
        // 표시 로직
    }

    public override void Close()
    {
        // 정리 로직
        base.Close();
    }
}
```

#### 사용법

```csharp
// 초기화 (한 번만 호출)
PopupManager.Instance.Init();

// 팝업 생성
PopupBase popup = PopupManager.Instance.Create(PopupType.Settings);

// 동일 타입 팝업이 있으면 닫고 새로 생성
PopupManager.Instance.CreateOnlyLastPopup(PopupType.Confirm);

// 큐 팝업 생성 (이전 팝업이 닫히면 자동 표시)
PopupManager.Instance.Create(PopupType.Tutorial, isShow: true, isQueue: true);

// 특정 타입 팝업 닫기
PopupManager.Instance.CloseByPopupType(PopupType.Settings);

// 마지막 팝업 닫기
PopupManager.Instance.CloseLastPopup(out bool success);

// 모든 팝업 닫기
PopupManager.Instance.CloseAll();

// 팝업 개수 변경 이벤트 구독
PopupManager.Instance.OnPopupCountChanged += (count) => {
    Debug.Log($"현재 팝업 수: {count}");
};
```

#### PopupAnimation

팝업에 애니메이션을 추가하려면 `PopupAnimation` 컴포넌트를 사용합니다.

**지원 애니메이션:**
- **Scale**: 0.8 → 1.0 (OutBack 이징)
- **Fade**: 0 → 1 (알파)
- **Move**: 방향별 슬라이드
  - `LeftToRight`, `RightToLeft`
  - `TopToBot`, `BotToTop`

---

### UI Components

#### UIButton

기본 Button을 확장하여 스케일 애니메이션과 다양한 이벤트 콜백을 제공합니다.

**특징:**
- 누르면 1.03x 스케일 확대 (OutCubic)
- 놓으면 탄성 애니메이션으로 복귀 (OutElastic)
- `Time.unscaledDeltaTime` 사용으로 Time.timeScale 영향 없음

**이벤트:**
- `onClick`: 클릭 시
- `onDown`: 누를 때
- `onUp`: 놓을 때
- `onEnter`: 마우스/터치 진입 시
- `onExit`: 마우스/터치 이탈 시

```csharp
UIButton button = GetComponent<UIButton>();
button.IsScaleAnim = true;  // 스케일 애니메이션 활성화
button.onClick.AddListener(() => Debug.Log("Clicked!"));
button.onDown.AddListener(() => Debug.Log("Pressed!"));
```

#### UICanvasView

Canvas 기반 뷰의 베이스 클래스입니다.

```csharp
public class MyView : UICanvasView
{
    public void ShowView()
    {
        SetView(true);  // Canvas와 GraphicRaycaster 활성화
    }

    public void HideView()
    {
        SetView(false);  // Canvas와 GraphicRaycaster 비활성화
    }
}
```

---

## Editor Tools

### 패키지 업데이트
**Menu**: `LayerLabAsset > Update Package`

Git URL로 설치된 패키지를 최신 버전으로 업데이트합니다.

### Raycast Target 비활성화
**Menu**: `LayerLabAsset > Disable Raycast Target`

선택한 오브젝트와 하위 오브젝트의 Image, TextMeshProUGUI 컴포넌트에서 불필요한 Raycast Target을 비활성화합니다. Button 컴포넌트가 있는 오브젝트는 활성화 상태를 유지합니다.

### Favorites Panel
**Menu**: `LayerLabAsset > Favorites Panel` 또는 `Ctrl+Shift+F`

자주 사용하는 에셋을 즐겨찾기로 관리합니다.
- 드래그 앤 드롭으로 추가
- 그룹 생성 및 관리
- 싱글 클릭: 프로젝트 창에서 선택
- 더블 클릭: 에셋 열기

### PlayerPrefs 초기화
**Menu**: `LayerLabAsset > Reset PlayerPrefs`

모든 PlayerPrefs 데이터를 삭제합니다.

### TimeScale Control
**Menu**: `LayerLabAsset > TimeScale Control`

플레이 모드에서 게임 속도를 실시간으로 조절할 수 있는 도구입니다.

**표시 위치:**
- **Scene View Overlay**: Scene View 상단에 오버레이로 표시
- **Main Toolbar**: Unity 에디터 메인 툴바 (재생 버튼 우측)에 표시

**기능:**
- 슬라이더로 0x ~ 10x 범위 조절
- 프리셋 버튼: 1x, 2x, 3x, 5x, 10x
- 게임 내 Time.timeScale 변경 자동 동기화

**설정:**
- `LayerLabAsset > TimeScale Control > Show in Scene View` - Scene View 표시 토글
- `LayerLabAsset > TimeScale Control > Show in Main Toolbar` - Main Toolbar 표시 토글

**API 사용:**
```csharp
using LayerLabAsset;

// 타임스케일 설정
TimeScaleSettings.SetTimeScale(2f);

// 현재 타임스케일 확인
float current = TimeScaleSettings.CurrentTimeScale;

// 변경 이벤트 구독
TimeScaleSettings.OnTimeScaleChanged += () => {
    Debug.Log($"TimeScale changed to {TimeScaleSettings.CurrentTimeScale}");
};
```

---

## 에셋 제작 지침

Unity 에셋 제작 시 품질과 재사용성을 높이기 위한 가이드라인입니다.

### 1. 네이밍 규칙

```csharp
// Private 필드: 언더스코어 프리픽스
private int _count;
private List<Item> _items;

// Public 프로퍼티/메서드: PascalCase
public int Count { get; private set; }
public void AddItem(Item item) { }

// Enum 값: PascalCase
public enum GameState { Idle, Playing, Paused }
```

### 2. 네임스페이스 사용

모든 클래스는 고유 네임스페이스에 포함시켜 충돌을 방지합니다.

```csharp
namespace LayerLabAsset
{
    public class MyComponent : MonoBehaviour { }
}
```

### 3. 제네릭과 인터페이스 활용

재사용 가능한 코드를 위해 제네릭과 인터페이스를 적극 활용합니다.

```csharp
// 제네릭 싱글톤
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour

// 제네릭 FSM
public class FsmClass<T> where T : Enum
```

### 4. 의존성 최소화

**필수 의존성만 사용:**
- Unity 내장 기능 우선 사용
- 외부 라이브러리 의존 최소화
- DOTween, UniRx 등은 선택적 의존성으로 처리

**조건부 컴파일 활용:**
```csharp
#if DOTWEEN_EXISTS
    DOTween.Sequence()...
#else
    StartCoroutine(AnimationCoroutine());
#endif
```

### 5. 에디터와 런타임 분리

```
Editor/                    # UnityEditor 네임스페이스 사용
├── LayerLabAsset.Editor.asmdef

Runtime/                   # 게임 런타임 코드
├── LayerLabAsset.Runtime.asmdef
```

### 6. 리소스 경로 규칙

```csharp
// Resources 폴더 기반 경로 규칙
private const string Path = "_UI/Popup/";  // Resources/_UI/Popup/

// 프리팹 로드
Resources.Load<GameObject>($"{Path}{popupType}");
```

### 7. Null 안전성

```csharp
// Null 체크
if (_instance == null)
{
    Debug.LogError("Instance is null");
    return;
}

// Null 조건 연산자
_currentState?.Update();
OnCloseEvent?.Invoke();
```

### 8. 이벤트 시스템

```csharp
// UnityEvent (Inspector에서 설정 가능)
public UnityEvent onClick = new();

// C# 이벤트 (코드에서만 구독)
public event Action<int> OnPopupCountChanged;
```

### 9. 직렬화 고려

```csharp
// 자동 프로퍼티 직렬화
[field: SerializeField] public bool IsScaleAnim { get; set; } = true;

// 직렬화 클래스
[System.Serializable]
private class FavoriteItem
{
    public string guid;
    public string path;
}
```

### 10. 성능 최적화

```csharp
// Dictionary로 상태 캐싱
private readonly Dictionary<T, FsmState<T>> _stateList = new();

// 코루틴 참조 관리
private Coroutine _scaleCoroutine;

private void StopScaleCoroutine()
{
    if (_scaleCoroutine != null)
    {
        StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = null;
    }
}

// Time.unscaledDeltaTime 사용 (TimeScale 독립)
elapsed += Time.unscaledDeltaTime;
```

### 11. 에디터 UX

- `[MenuItem]` 속성으로 메뉴 추가
- `Undo.RecordObject`로 실행 취소 지원
- `EditorUtility.DisplayDialog`로 확인 다이얼로그
- `EditorUtility.DisplayProgressBar`로 진행 상황 표시

### 12. 문서화

```csharp
/// <summary>
/// 팝업 생성
/// 팝업을 생성하기 위한 가장 기본적인 함수
/// </summary>
/// <param name="popupType">생성할 팝업 타입</param>
/// <param name="isShow">즉시 표시 여부</param>
/// <param name="isQueue">큐에 추가 여부</param>
/// <returns>생성된 PopupBase 인스턴스</returns>
public PopupBase Create(PopupType popupType, bool isShow = true, bool isQueue = false)
```

### 13. 에러 처리

```csharp
// 명확한 에러 메시지
Debug.LogError("FsmClass::AddFsm()[ null == FsmState<T>");
Debug.LogError($"FsmClass::SetState()[ no have state : {stateType}");

// 경고 메시지
Debug.LogWarning("FsmClass::SetState()[ same state : " + stateType);
```

### 14. 패키지 구조 (package.json)

```json
{
    "name": "com.layerlab.asset",
    "displayName": "LayerLab Asset",
    "version": "1.0.0",
    "unity": "6000.0",
    "description": "Unity utility package",
    "dependencies": {}
}
```

---

## 버전 히스토리

| 버전 | 날짜 | 변경사항 |
|-----|------|---------|
| 1.0.0 | - | 초기 릴리즈 |

---

## 라이선스

이 프로젝트는 LayerLab에서 관리합니다.