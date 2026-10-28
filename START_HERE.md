# 어디다 버려? — 맥북에서 열고 아이폰에서 스캔하기

Unity **6000.3.22f1**, 맥북 개발, 실제 아이폰 실행을 기준으로 합니다.
현재 작업 브랜치: `codex/seongbuk-ar-starter-20261022`.

## 이번 버전에서 바뀐 것

- 그림카드 중심에서 **실물 스캔 → 후보 확인 → 상태 질문 → 배출 안내**로 변경했습니다.
- 본문용 고딕 글꼴 **SUIT Regular·SemiBold**를 적용하고, uGUI 화면의 색상·장식·문구를 정리했습니다.
- 흰 배경·회색 보조 글씨·검정 버튼을 사용하고, 굵기·여백·구분선으로 정보를 구분합니다. 큰 둥근 카드 대신 목록과 간단한 선 아이콘을 사용합니다.
- 품목 목록을 **6개에서 59개**로 늘렸습니다. 캔, 단추, 페트병 뚜껑, 영수증, 의류, 전지, 조명, 배달 용기 등을 검색할 수 있습니다.
- 실제 아이폰 카메라 사진을 Apple Vision이 기기 안에서 분류합니다. 사진을 서버로 전송하거나 API 키를 입력하지 않습니다.
- 인식 후보를 확인한 뒤 재질·남은 내용물·오염 제거 가능 여부를 질문합니다.
- 결과 화면에서 **공간에 안내 놓기**를 선택하면 인식된 책상·바닥 위에 안내 패널을 놓습니다.
- 인식 실패·미등록 품목에 대해 재촬영·검색·공식 자료 확인과 **내 메모** 저장 경로를 제공합니다.

기존 그림카드와 PDF는 이전 버전의 자료로 남아 있습니다. 새 실물 스캔 흐름에는 인쇄 카드가 필요하지 않습니다.

## 먼저 알아둘 범위

- **59품목은 검색·안내 범위**입니다. 모든 품목의 카메라 인식을 보장하는 숫자는 아닙니다.
- Apple Vision은 일반 사진 분류기입니다. 성북구 쓰레기만을 학습한 전용 모델이 아닙니다.
- 단추의 정확한 소재, 세척 가능한 기름, 빨간 변색과 잔여 음식물의 차이는 사진만으로 확정하지 않습니다.
- 일치하는 후보가 없으면 재촬영·이름 검색·공식 자료 확인으로 이어집니다. 아직 찾지 못한 물건은 이름만 개인 메모로 저장하고 나중에 내용을 추가할 수 있습니다.
- 개인 메모는 기기에 저장한 검증되지 않은 기록이며, 기본 안내 59품목과 구분합니다. 메모 저장이 공식 자료 검수·공유 서버 등록·사진 인식 학습을 뜻하지 않습니다.
- 출처는 국가 공통/서울시/성북구/다른 자치구 참고 자료를 구분합니다. 성북구 전체 품목·수거 방식을 검수받았다는 뜻은 아닙니다.
- 이 환경에는 Unity Editor와 Xcode가 없어 **Unity 화면 렌더링·Xcode 컴파일·실제 아이폰 인식 품질은 아직 검증하지 못했습니다.**

## 1. 기존 프로젝트 업데이트

1. Unity에서 **Play를 중지**합니다.
2. GitHub Desktop에서 위 작업 브랜치를 선택하고 **Fetch origin → Pull origin**을 누릅니다.
3. Unity로 돌아와 새 글꼴·스크립트 임포트가 끝날 때까지 기다립니다.
4. 기존 `GuidePreview`에서 Play를 누릅니다. 새 화면은 런타임에 생성되므로 장면을 다시 만들 필요는 없습니다.
5. Game 창의 **Scale은 1x**로 두고 세로 화면 비율(예: 9:16 또는 아이폰 비율)에서 확인합니다. Free Aspect의 짧은 화면에서도 홈 내용을 스크롤할 수 있습니다.

처음 여는 프로젝트라면 Unity Hub **Add → Add project from disk**에서 `MidProject` 폴더를 선택한 뒤,
Unity 상단 **Seongbuk AR → 1. Create Starter Scenes**를 눌러 두 장면을 생성합니다.

## 2. 맥북에서 확인할 흐름

`GuidePreview`에는 실제 아이폰 카메라가 없습니다. 미리보기에서 스캔 버튼을 누르면 기기 안내가 나오고,
품목 검색과 상태 질문은 계속 사용할 수 있습니다. 가짜 인식 결과는 표시하지 않습니다.

1. **이름으로 직접 찾기**를 누릅니다.
2. `엽떡`, `마라탕 용기`, `캔`, `단추`, `페트병 뚜껑`, `영수증`을 검색합니다.
3. 배달 용기의 경우 **이물질 남음 → 세척 가능 / 세척 어려움**을 각각 선택합니다.
4. 붉은 자국 자체와 실제로 남은 음식물·기름을 구분하는 안내를 확인합니다.
5. 단추에서는 재사용 가능한 옷에 달린 상태인지, 소재를 확인할 수 있는지 선택합니다.
6. 결과에서 준비 순서, 설명, 공식 출처, 적용 범위를 확인합니다.

Unity 상단 **Seongbuk AR → 2. Check Catalog**는 실제 Unity JsonUtility로 목록을 읽어 구조를 점검합니다.

### 목록에 없는 물건과 내 메모 확인

1. 이름 검색에서 `접이식우산2026x`처럼 기본 목록에 없는 이름을 입력합니다.
2. **목록에 없는 물건 메모하기**를 누릅니다. 이름은 필수이며, 재질·상태, 확인할 내용·배출 방법 메모, `https://` 참고 링크는 선택입니다. 방법을 모르면 이름만 저장해도 됩니다.
3. **이 기기에 저장**을 누른 뒤 홈의 **내 메모**에서 저장한 물건을 확인합니다. 일반 이름 검색에서도 개인 메모 결과가 기본 안내와 구분되어 표시됩니다.
4. Play를 중지하고 다시 실행한 뒤 같은 이름으로 메모를 찾습니다. 내용을 수정해 저장하고, **메모 삭제 → 삭제**로 삭제 동작도 확인합니다.
5. 검색 분류 때문에 기존 품목이 보이지 않는 경우에는 **전체 분류에서 보기**로 먼저 확인합니다.

인식 실패 화면과 잘못된 후보 화면에서도 재촬영·이름 검색·메모 경로로 이어집니다. 공식 품목사전과 성북구 수거 안내 링크를 통해 자료를 확인한 뒤 메모를 수정할 수 있습니다. 참고 링크를 적었다고 해당 메모가 공식 검수된 정보로 바뀌지는 않습니다.

기록은 `Application.persistentDataPath/personal-waste-notes.json`에 저장합니다. 앱 재실행 후에도 유지되지만 다른 기기로 자동 동기화되지 않으며 앱 삭제 시 사라질 수 있습니다. 사진은 개인 메모에 저장하거나 서버로 업로드하지 않습니다. 실제 아이폰의 키보드·저장 동작은 기기 실행 후 별도로 확인해야 합니다.

## 3. iPhone 빌드 준비

VS Code는 C# 코드 편집용으로 계속 사용할 수 있습니다. 맥북에서 직접 아이폰 앱을 빌드·서명·설치할 때에는 Xcode의 도구가 필요합니다. 이 프로젝트는 Unity가 Xcode 프로젝트를 만든 뒤 Xcode에서 개인 아이폰으로 실행하는 흐름을 사용합니다.

1. Unity Hub의 Installs에서 **6000.3.22f1 → Add modules → iOS Build Support**를 설치합니다.
2. 맥북에 연결할 아이폰의 iOS를 지원하는 Xcode를 설치하고 한 번 실행해 초기 설정을 마칩니다.
3. Unity **File → Build Profiles → iOS**로 플랫폼을 전환합니다.
4. **Project Settings → XR Plug-in Management → iOS**에서 **Apple ARKit**을 활성화합니다.
5. **XR Plug-in Management → Project Validation**에서 필요한 설정을 적용합니다.
6. **Player → Other Settings → Active Input Handling**을 **Input System Package (New)** 또는 **Both**로 설정하고 Unity를 재시작합니다. 카메라 포즈 추적에 필요합니다.
7. **Player → iOS → Other Settings**를 확인합니다.
   - Bundle Identifier: 본인의 고유한 영문 ID. 예: `com.본인영문이름.seongbukarguide`
   - Camera Usage Description: `물건을 스캔해 분리배출 방법과 공간 안내를 표시합니다.`
   - Scripting Backend: IL2CPP
   - Target minimum iOS Version: 현재 ARKit 패키지의 검증 조건과 실제 기기 버전에 맞춤. 사진 분류는 iOS 13 이상에서 지원합니다.
8. **Seongbuk AR → 3. Open iPhone Scan Scene**을 눌러 `GuideAR`를 엽니다.
9. iOS Build Profiles의 Scene List에서 `GuideAR.unity`가 첫 번째로 활성화되어 있는지 확인합니다. 첫 기기 테스트에서는 GuideAR만 체크해도 됩니다. 아이폰 시작 장면은 `GuidePreview`가 아닌 `GuideAR`입니다.

새 스캐너와 공간 안내는 기존 AR 장면에서도 자동으로 연결됩니다. 이전 그림카드 추적과 캐릭터는 기본 흐름에서 비활성화됩니다.
iOS 내보내기 후 처리기가 필요한 Apple 프레임워크와 해당 `.mm` 파일의 ARC 컴파일 옵션을 연결합니다.

## 4. Xcode에서 아이폰에 실행

1. Unity iOS Build Profiles에서 **Build**를 누르고 `Builds/iOS` 같은 폴더를 지정합니다.
2. 생성된 `Unity-iPhone.xcodeproj`를 Xcode로 엽니다. `.xcworkspace`가 별도로 생성됐다면 그 작업공간을 엽니다.
3. Xcode Settings의 Apple Accounts/Accounts에서 본인 Apple 계정으로 로그인합니다.
4. **Unity-iPhone 타깃 → Signing & Capabilities**에서 **Automatically manage signing**과 본인 Team을 선택합니다. 개인 기기 시험에는 Personal Team을 사용할 수 있습니다.
5. 아이폰을 연결하고 컴퓨터 신뢰·개발자 모드 등 Xcode의 안내를 완료합니다.
6. 실행 대상을 실제 아이폰으로 선택하고 **Run**을 누릅니다.
7. 아이폰에서 카메라 사용을 허용합니다.

## 5. 실제 쓰레기를 스캔하는 흐름

1. 밝은 곳에서 물건 **하나**를 카메라 가운데 가까이 놓습니다.
2. **쓰레기 스캔하기**를 누릅니다. 화면 중앙을 자른 사진을 기기 안에서 분석합니다.
3. 촬영된 사진과 함께 **어떤 물건인가요?** 후보가 표시됩니다.
4. 실제 물건과 일치하는 항목을 선택합니다. 잘못된 후보라면 **맞는 항목 없음 · 이름으로 찾기**를 누릅니다.
5. 캔이면 내용물을 비우고 헹구는 등의 캔류 안내로, 배달 용기면 재질·음식물·기름 제거 가능 여부 질문으로 이어집니다.
6. 같은 물건을 다시 스캔하거나 처음부터 확인하면 이전 답변을 새로 받습니다.

작은 단추·복합재질·빛 반사·여러 물건이 섞인 장면은 특히 확인이 필요합니다.
기기 모델과 iOS 버전에 따른 실제 인식 정확도는 측정되지 않았습니다.

## 6. 공간 AR 안내

1. 배출 방법 결과에서 **공간에 안내 놓기**를 누릅니다.
2. 투명한 카메라 화면에서 책상이나 바닥을 천천히 비춥니다.
3. 화면 가운데를 원하는 위치에 맞추고 **이 위치에 안내 놓기**를 누릅니다.
4. 인식된 표면 위에 품목·배출 종류·최대 3개 준비 단계가 표시됩니다. 전체 단계는 결과 화면에서 확인합니다.

표면을 찾지 못하면 위치를 추측해서 놓지 않고 주변을 비추라는 안내를 표시합니다.
이 패널은 현재 AR 세션의 표면 위치에 놓는 안내이며 쓰레기 자체의 부품 추적·객체 추적이나 재실행 후 유지되는 앵커가 아닙니다.

## 주요 파일

| 파일 | 역할 |
|---|---|
| `Assets/SeongbukAR/Scripts/GuideApp.cs` | 스캔·후보 확인·검색·질문·결과 화면 전환 |
| `Assets/SeongbukAR/Scripts/GuideUi.cs` | SUIT·uGUI 레이아웃과 안전 영역 |
| `Assets/SeongbukAR/Scripts/WasteCatalog.cs` | 품목 구조와 질문 검증 |
| `Assets/SeongbukAR/Scripts/WasteRecognition.cs` | 실제 분류 라벨을 확인 후보에 보수적으로 연결 |
| `Assets/SeongbukAR/Scripts/Scanning/` | 카메라 캡처, 비동기 요청, 방향 변환, 네이티브 응답 |
| `Assets/SeongbukAR/Scripts/Notes/` | 개인 메모의 검색·수정·삭제와 기기 내 파일 저장 |
| `Assets/SeongbukAR/Plugins/iOS/SeongbukVision.mm` | Apple Vision 기기 내 사진 분류 |
| `Assets/SeongbukAR/Scripts/SpatialGuide.cs` | 표면 감지와 공간 안내 패널 |
| `Assets/SeongbukAR/Resources/Data/seongbuk-demo.json` | 59품목·조건별 안내·출처 |
| `Docs/catalog-sources.md` | 근거 범위와 추가 확인 사항 |

현재 화면의 글꼴은 [SUIT](https://github.com/sun-typeface/SUIT)의 공식 v2.0.5 배포 중 정적 OTF Regular·SemiBold를 사용합니다.
앱과 함께 배포할 수 있는 SIL Open Font License 1.1의 전체 문구를 `Assets/SeongbukAR/Resources/Fonts/SUIT-LICENSE.txt`에 포함했습니다.
글꼴 출처와 파일 정보는 같은 폴더의 `SUIT-SOURCE.md`에서 확인할 수 있습니다.

## 검증

- C# 카탈로그/인식 후보 회귀 검사: `Tests/CatalogRegression`
- 네이티브 응답/요청 상태/사진 방향 변환 검사: `Tests/ScanRegression`
- 개인 메모 데이터·파일 저장 검사: `Tests/NotesRegression`
- .NET 8을 사용하는 순수 C# 검사입니다. 실제 Unity UI·네이티브 컴파일·기기 인식 시험을 대체하지 않습니다.

공식 참고: [Unity Hub](https://docs.unity.com/en-us/hub/project-manage),
[AR Foundation 카메라](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/features/camera/image-capture.html),
[Apple Vision](https://developer.apple.com/documentation/vision/vnclassifyimagerequest),
[Apple 기기 실행](https://developer.apple.com/documentation/xcode/running-your-app-on-simulated-or-physical-devices).
