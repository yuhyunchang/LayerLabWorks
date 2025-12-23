# LayerLab Asset

Unity 유틸리티 패키지 - Singleton, FSM, Popup Manager, UI 컴포넌트 제공

## 요구사항

- Unity 6000.0+ (Unity 6)
- UniRx
- DOTween
- TextMeshPro

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

### 2. 의존성 설치 (필수)

1. **UniRx** - Asset Store 또는 OpenUPM에서 설치
2. **DOTween** - Asset Store에서 설치
3. **TextMeshPro** - Window > TextMeshPro > Import TMP Essential Resources

### 3. DOTween ASMDEF 생성 (필수)

DOTween 설치 후 반드시 ASMDEF를 생성해야 합니다:

1. **Tools > Demigiant > DOTween Utility Panel** 열기
2. **"Create ASMDEF"** 버튼 클릭

> ⚠️ 이 단계를 건너뛰면 DOTween 관련 컴파일 에러가 발생합니다.

## 패키지 업데이트

메뉴에서 **LayerLabAsset > Update Package** 선택

## 주요 기능

- **Singleton** - 제네릭 싱글톤 패턴
- **FSM** - 유한 상태 기계 (Enum 기반 / Character 기반)
- **Popup Manager** - 팝업 생명주기 관리
- **UI Components** - UIButton, UICanvasView 등

## Editor Tools

- **LayerLabAsset > Favorites Panel** - 자주 사용하는 에셋 즐겨찾기
- **LayerLabAsset > Update Package** - 패키지 업데이트
- **LayerLabAsset > Disable Raycast Target** - UI Raycast Target 일괄 비활성화
