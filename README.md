# LayerLab Asset

Unity 유틸리티 패키지 - Singleton, FSM, Popup Manager, UI 컴포넌트 제공

## 요구사항

- Unity 6000.0+ (Unity 6)
- TextMeshPro (필수)
- DOTween (선택) - UI 애니메이션 기능
- UniRx (선택) - Popup Manager 리액티브 기능

## 설치 방법

### 1. 패키지 설치

Unity Package Manager에서 **Add package from git URL**을 선택하고 **아래 전체 주소를 복사**해서 입력하세요:

> ⚠️ GitHub "Code" 버튼의 URL은 사용할 수 없습니다. 반드시 아래 주소를 복사하세요.

**HTTPS (권장):**
```
https://github.com/yuhyunchang/LayerLabWorks.git?path=/Assets/LayerLabAsset
```

**SSH:**
```
git@github.com:yuhyunchang/LayerLabWorks.git?path=/Assets/LayerLabAsset
```

### 2. TextMeshPro 설치 (필수)

**Window > TextMeshPro > Import TMP Essential Resources**

### 3. 선택적 의존성 설치

#### DOTween (UIButton 애니메이션, PopupAnimation)

1. Asset Store에서 DOTween 설치
2. **Tools > Demigiant > DOTween Utility Panel** 열기
3. **"Create ASMDEF"** 버튼 클릭
4. `Packages/com.layerlab.asset/Runtime/LayerLabAsset.Runtime.asmdef` 파일 선택
5. References에 `DOTween.Modules` 추가
6. **Project Settings > Player > Scripting Define Symbols**에 `DOTWEEN_EXISTS` 추가

#### UniRx (Popup Manager 리액티브 기능)

**OpenUPM 설치 (자동 감지):**
```
openupm add com.neuecc.unirx
```
OpenUPM으로 설치하면 자동으로 기능이 활성화됩니다.

**Asset Store 설치 (수동 설정):**
1. Asset Store에서 UniRx 설치
2. `Packages/com.layerlab.asset/Runtime/LayerLabAsset.Runtime.asmdef` 파일 선택
3. References에 `UniRx` 추가
4. **Project Settings > Player > Scripting Define Symbols**에 `UNIRX_EXISTS` 추가

## 패키지 업데이트

메뉴에서 **LayerLabAsset > Update Package** 선택

## 주요 기능

- **Singleton** - 제네릭 싱글톤 패턴
- **FSM** - 유한 상태 기계 (Enum 기반 / Character 기반)
- **Popup Manager** - 팝업 생명주기 관리 (UniRx 필요)
- **UI Components** - UIButton (DOTween으로 애니메이션), UICanvasView 등

## Editor Tools

- **LayerLabAsset > Favorites Panel** - 자주 사용하는 에셋 즐겨찾기
- **LayerLabAsset > Update Package** - 패키지 업데이트
- **LayerLabAsset > Disable Raycast Target** - UI Raycast Target 일괄 비활성화

## 기능별 의존성

| 기능 | DOTween | UniRx |
|------|---------|-------|
| Singleton | - | - |
| FSM | - | - |
| UIButton 애니메이션 | 필요 | - |
| PopupAnimation | 필요 | - |
| PopupManager 리액티브 | - | 필요 |
