# MidProject
AR Mid Term Project

성북구 사용자를 위한 실물 스캔·분리배출 안내 Unity 프로젝트입니다.

- Unity 6000.3.22f1 / 맥북 개발 / 실제 아이폰에서 ARKit 실행
- 실행 순서: [START_HERE.md](START_HERE.md)
- 실물 스캔 → 인식 후보 확인 → 재질·오염 질문 → 준비 순서와 배출 방법
- SUIT Regular/SemiBold, 흰색·회색·검정의 uGUI 화면, 검색·분류 59품목
- iPhone에서 Apple Vision으로 기기 내 사진 분석. 서버 전송/API 키 없이 동작
- 배출 안내를 인식된 책상·바닥 위에 놓는 공간 AR 화면
- 처음 장면 생성: `Seongbuk AR > 1. Create Starter Scenes`
- 기존 사용자: 같은 브랜치에서 Pull한 뒤 기존 `GuidePreview`를 Play
- 데이터 근거와 범위: [Docs/catalog-sources.md](Docs/catalog-sources.md)
- 발표용 작동 구조와 설명: [Docs/PROJECT_STRUCTURE.md](Docs/PROJECT_STRUCTURE.md)

59품목은 검색·상태별 안내 범위이며 59품목 전체의 사진 인식 보장을 뜻하지 않습니다.
사진만으로 기름 제거 가능 여부·소재·지역 배출 규칙을 확정하지 않고 사용자가 확인합니다.
Unity/uGUI 렌더링, Xcode 네이티브 빌드, 실제 iPhone 인식 성능은 아직 검증하지 못했습니다.
