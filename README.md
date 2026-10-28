# MidProject
AR Mid Term Project

성북구 사용자를 위한 실물 스캔·분리배출 안내 Unity 프로젝트입니다.

- Unity 6000.3.22f1 / 맥북 개발 / 실제 아이폰·아이패드에서 ARKit 실행
- 실행 순서: [START_HERE.md](START_HERE.md)
- 실물 스캔 → 인식 후보 확인 → 재질·오염 질문 → 준비 순서와 배출 방법
- SUIT Regular/SemiBold, 흰색·회색·검정의 uGUI 화면, 검색·분류 59품목
- iPhone·iPad에서 Apple Vision으로 촬영 영역과 중앙 확대본을 기기 내 분석. 서버 전송/API 키 없이 동작
- 배출 안내를 인식된 책상·바닥 위에 놓는 공간 AR 화면
- 인식 실패·목록에 없는 물건: 재촬영, 이름 검색, 공식 자료 확인, 기기에 저장하는 개인 메모
- 처음 장면 생성: `Seongbuk AR > 1. Create Starter Scenes`
- 기존 사용자: 같은 브랜치에서 Pull한 뒤 기존 `GuidePreview`를 Play
- 데이터 근거와 범위: [Docs/catalog-sources.md](Docs/catalog-sources.md)
- 발표용 작동 구조와 설명: [Docs/PROJECT_STRUCTURE.md](Docs/PROJECT_STRUCTURE.md)

59품목은 검색·상태별 안내 범위이며 59품목 전체의 사진 인식 보장을 뜻하지 않습니다.
사진만으로 기름 제거 가능 여부·소재·지역 배출 규칙을 확정하지 않고 사용자가 확인합니다.
개인 메모는 기본 59품목 안내와 구분하며, 공식 검수·공유 서버·사진 인식 학습에 자동 반영하지 않습니다.
사용자 iPad Air 11형(M3)에서 기존 버전의 설치·실행과 촬영을 확인했습니다.
이번 수정은 음료 캔·골판지 후보 연결과 촬영 테두리/캡처 정렬을 보완합니다.
수정본의 Unity/Xcode 빌드와 실제 사진 인식률은 기기에서 다시 확인해야 합니다.
