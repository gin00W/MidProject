# 성북구 AR 분리배출 도우미: 맥북 → 아이폰 시작하기

Unity **6000.3.22f1**, 맥북 개발, 실제 아이폰 실행을 기준으로 준비한 시작 프로젝트입니다.
AR Foundation과 Apple ARKit XR Plug-in은 공식 레지스트리에서 존재와 Unity 요구 버전을 확인한 **6.3.5**로 맞췄습니다.

## 지금 준비된 것

- `Seongbuk AR > 1. Create Starter Scenes` 메뉴로 미리보기 장면과 AR 장면 생성
- 품목 검색, 별칭 검색, 상태별 질문, 결과·처리 순서·출처 표시
- 두 가지 인쇄 카드와 연결된 AR 안내 캐릭터
- 질문 흐름을 JSON으로 관리하는 구조
- 한글 표시용 나눔고딕과 OFL 라이선스

**현재는 기능 확인용 예시 6품목입니다.** 최종 품목 수를 6개로 제한한 설계가 아닙니다.
성북구 전체 데이터 수집·검증, 실물 쓰레기 자동인식, 세척·분리 동작 애니메이션은 아직 구현하지 않았습니다.
예시 데이터는 모두 `needs_local_review`로 표시되어 있습니다. 공식 자료와 성북구의 현재 안내를 대조한 뒤 검증 상태를 변경해야 합니다.
이 환경에는 Unity Editor와 Xcode가 없어 실제 프로젝트 임포트·컴파일·아이폰 빌드는 검증하지 못했습니다.

## 1. 맥북 준비

1. Unity Hub의 **Installs → 6000.3.22f1의 관리 메뉴 → Add modules**에서 **iOS Build Support**를 설치합니다. 이미 있으면 그대로 사용합니다.
2. 맥북에 Xcode를 설치하고 한 번 실행해 초기 설정을 마칩니다. 연결할 아이폰의 iOS를 지원하는 Xcode 버전을 사용합니다.
3. 이 프로젝트를 맥북으로 가져온 후, Unity Hub의 **Projects → Add/Open → Add project from disk**에서 `Assets`, `Packages`, `ProjectSettings`가 함께 있는 **MidProject 폴더**를 선택합니다.
4. 패키지 다운로드와 스크립트 컴파일이 끝날 때까지 기다립니다. 별도의 새 프로젝트나 AR 템플릿을 만들 필요는 없습니다.

## 2. 먼저 맥북에서 질문 흐름 확인

1. Unity 상단의 **Seongbuk AR → 1. Create Starter Scenes**를 누릅니다.
2. 생성된 `Assets/SeongbukAR/Scenes/GuidePreview.unity`가 열립니다.
3. Game 창의 화면 비율을 세로(예: 9:16)로 바꾸고 **Play**를 누릅니다.
4. `엽떡`, `마라탕 용기`, `치킨 뼈`, `옷` 등을 검색해 봅니다.
5. 배달 플라스틱 용기에서 다음 경로를 각각 확인합니다.
   - 이물질 제거 완료 → 플라스틱류 안내
   - 이물질 남음 → 세척 가능 → 세척 후 분리배출 안내
   - 이물질 남음 → 제거 어려움 → 공식 안내 재확인
6. **이전 질문으로**, **품목 목록으로**를 눌러 탐색을 확인합니다.

미리보기 장면은 데이터와 화면을 확인하는 용도입니다. 실제 카드 인식은 아이폰에서 확인합니다.
장면 생성 메뉴는 이미 만들어진 같은 이름의 장면을 덮어쓰지 않습니다.

## 3. iOS / ARKit 설정

1. **File → Build Profiles → iOS**를 선택해 해당 플랫폼으로 전환합니다. iOS 항목이 없으면 Hub에서 iOS Build Support 설치를 확인합니다.
2. **Edit → Project Settings → XR Plug-in Management → iOS 탭**에서 **Apple ARKit**을 활성화합니다.
3. **XR Plug-in Management → Project Validation**에서 iOS 관련 항목을 확인하고 필요한 수정 사항을 적용합니다.
4. **Player → iOS → Other Settings**에서 다음을 확인합니다.
   - Bundle Identifier: `com.본인고유이름.seongbukarguide` 형태의 고유한 영문 식별자
   - Camera Usage Description: `그림카드를 인식해 AR 분리배출 안내를 표시합니다.`
   - Scripting Backend: IL2CPP
   - Target minimum iOS Version: 현재 ARKit 패키지의 검증 조건과 실제 아이폰 버전에 맞춤
5. 새 Input System 활성화를 묻는 창이 나오면 활성화하고 에디터를 재시작합니다. 창이 나오지 않아도 **Player → Other Settings → Active Input Handling**에서 **Input System Package (New)** 활성화를 확인합니다. 이 프로젝트의 AR 카메라 포즈는 Input System을 사용합니다.
6. iOS 빌드의 Scene List에 **GuideAR.unity**가 포함되어 있는지 확인합니다. GuidePreview는 아이폰용 시작 장면으로 선택하지 않습니다.

계정·서명·개인 Team ID는 프로젝트 코드에 넣지 않았습니다. 다음 단계에서 본인 맥북의 Xcode 계정으로 설정합니다.

## 4. Xcode에서 실제 아이폰으로 실행

1. Unity의 iOS Build Profiles에서 **Build**를 누르고 `Builds/iOS` 같은 폴더를 지정합니다.
2. 생성된 `Unity-iPhone.xcodeproj`를 Xcode로 엽니다. 작업공간 파일이 별도로 생성된 경우 해당 `.xcworkspace`를 엽니다.
3. Xcode **Settings → Apple Accounts**에서 본인 Apple 계정으로 로그인합니다. 구버전 Xcode에서는 메뉴 이름이 Accounts일 수 있습니다.
4. **Unity-iPhone 타깃 → Signing & Capabilities**에서 **Automatically manage signing**을 켜고 본인 Team을 선택합니다. 개인 기기 테스트에는 Personal Team을 사용할 수 있습니다.
5. 아이폰을 연결하고 이 컴퓨터를 신뢰하도록 설정합니다. Xcode가 요구하면 아이폰의 **설정 → 개인정보 보호 및 보안 → 개발자 모드**를 켜고 기기를 재시작합니다.
6. Xcode 상단 실행 대상을 실제 아이폰으로 선택하고 **Run(▶)**을 누릅니다.
7. 아이폰에서 카메라 사용을 허용합니다.

## 5. 카드 인식 확인

1. 프로젝트 루트의 **AR_TEST_CARDS.pdf**를 A4, 실제 크기 100%로 인쇄합니다.
2. 카드 전체 그림의 가로 폭이 **12 cm**, 세로가 **9 cm**인지 확인합니다. 이 치수가 reference image library에 등록됩니다.
3. 아이폰으로 카드 한 장을 비춥니다. 충분한 조명에서 카드 전체가 보이게 시작합니다.
4. 인식되면 화면에 품목명이 표시되고 카드 위에 초록색 안내 캐릭터가 표시됩니다.
5. 하단 질문에 답하면 결과와 처리 순서가 표시됩니다.
6. 카드를 가리면 AR 캐릭터가 숨고 선택한 안내는 유지됩니다. 카드를 다시 비추면 캐릭터가 돌아옵니다.

카메라 영역의 가운데는 UI로 가리지 않도록 비워두었습니다. 인식 거리·각도·카드 이미지 품질은 실제 아이폰에서 조정해야 합니다.
PNG를 다른 화면에 표시해 시험할 수도 있지만 등록된 실물 크기와 표시 크기를 맞춰야 하므로 인쇄 카드로 먼저 확인하는 편이 좋습니다.

## 파일을 수정할 때

| 파일 | 역할 |
|---|---|
| `Assets/SeongbukAR/Scripts/WasteCatalog.cs` | 품목 구조, 별칭 검색, 질문 연결 검증 |
| `Assets/SeongbukAR/Scripts/GuideApp.cs` | 검색·질문·결과 화면 |
| `Assets/SeongbukAR/Scripts/CardGuideTracker.cs` | 카드 추적과 AR 캐릭터 표시 |
| `Assets/SeongbukAR/Editor/StarterSetup.cs` | 미리보기/AR 장면과 이미지 라이브러리 생성 |
| `Assets/SeongbukAR/Resources/Data/seongbuk-demo.json` | 예시 품목·질문·조건별 처리 안내 |
| `Assets/SeongbukAR/Art/*.png` | 인식용 그림카드 |

품목 추가는 JSON의 `items`에 같은 구조로 항목을 추가하면 됩니다. `id`는 중복 없이 만들고,
`startNodeId`는 실제 질문 또는 결과 노드를 가리켜야 합니다. 선택지의 `nextNodeId`도 존재해야 합니다.
질문에는 `choices`, 마지막 결과에는 `result`를 넣습니다.

검색 품목을 추가할 때마다 인쇄 카드를 만들 필요는 없습니다. 카드도 추가할 경우 Unity에서
`Generated/WasteCardLibrary`에 이미지를 추가하고, 이미지 이름을 JSON의 품목 `id`와 일치시킵니다.
이미지를 수정했다면 라이브러리에 등록된 물리적 크기도 확인합니다.

이후 데이터 확장 시에는 공식 품목사전을 근거로 품목·재질·상태를 정리하고,
성북구 안내를 우선 대조하며 출처, 적용 지역, 확인 날짜와 예외를 저장합니다.
공식 사이트의 공개 API·일괄 다운로드 제공 여부는 아직 확인하지 않았습니다.

## 다음 구현 순서

1. 맥북 미리보기에서 질문·검색·뒤로 가기 확인
2. 아이폰에서 카메라와 두 장의 카드 인식 확인
3. 공식 자료에 근거한 성북구 품목 데이터 확장 및 출처·확인일 관리
4. 라벨 분리·내용물 비우기 등 현실 물건과 연결되는 AR 시각 안내 보강
5. 실제 사용자에게 헷갈리는 상황을 제시해 안내 이해도 확인
6. 완료한 기능과 남은 기능을 구분해 중간보고서 작성

## 공식 참고 자료

- Unity 6.3 XR 프로젝트 생성: https://docs.unity.com/en-us/engine/6000.3/manual/xr/configuring-project-for/create-projects
- AR Foundation 이미지 추적: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/features/image-tracking.html
- Apple 기기 실행·서명: https://developer.apple.com/documentation/xcode/running-your-app-on-simulated-or-physical-devices
- Apple 개발자 모드: https://developer.apple.com/documentation/xcode/enabling-developer-mode-on-a-device
- 공식 품목사전: https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do
- 성북구 생활폐기물 안내: https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164
- 한글 폰트 원본·라이선스: https://github.com/google/fonts/tree/main/ofl/nanumgothic
