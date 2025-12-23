# LayerLab Asset

Unity 유틸리티 패키지 - Singleton, FSM, Popup Manager, UI 컴포넌트 제공

## 요구사항

- Unity 6000.0+ (Unity 6)
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

### 2. TextMeshPro 설치 (필수)

**Window > TextMeshPro > Import TMP Essential Resources**

## 패키지 업데이트

메뉴에서 **LayerLabAsset > Update Package** 선택

## 주요 기능

- **Singleton** - 제네릭 싱글톤 패턴
- **FSM** - 유한 상태 기계 (Enum 기반 / Character 기반)
- **Popup Manager** - 팝업 생명주기 관리
- **UI Components** - UIButton (스케일 애니메이션), UICanvasView 등

## Editor Tools

- **LayerLabAsset > Favorites Panel** - 자주 사용하는 에셋 즐겨찾기
- **LayerLabAsset > Update Package** - 패키지 업데이트
- **LayerLabAsset > Disable Raycast Target** - UI Raycast Target 일괄 비활성화
