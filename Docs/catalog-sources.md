# 분리배출 데이터 근거와 적용 범위

확인일: 2026-10-28. 대상 지역 설정은 서울특별시 성북구입니다.

## 데이터의 의미

이 목록은 공식 자료를 확인해 직접 구조화한 생활 품목 안내입니다. 공식 사이트의 API를 연동하거나 전체 품목을 일괄 수집한 데이터가 아닙니다.
카메라 인식은 물건의 후보를 찾는 용도이며 재질·오염·세척 가능성까지 검증하지 않습니다. 사용자가 후보와 필요한 상태 질문을 확인한 뒤 안내를 읽도록 구성합니다.

- 모든 품목의 `reviewStatus`는 `needs_local_review`입니다. 전국 공통 지침과 서울시 안내는 확인했지만, 성북구의 모든 품목·동·주거형태에 대한 현재 적용을 검증했다고 표시하지 않습니다.
- `sourceScope`는 근거의 범위입니다. `national`은 국가 지침·공식 품목사전, `seoul`은 서울시 안내, `seoul_other_district_reference`는 다른 서울 자치구의 참고 사례입니다.
- `sourceCheckedOn`은 자료를 확인한 날짜이며 제도 시행일이나 성북구 확인일을 뜻하지 않습니다.
- `recognitionGroup`, `recognitionKeywords`는 분류 모델 결과를 후보 품목에 연결하기 위한 단서입니다. 실제 인식 정확도나 지원 품목 보장이 아닙니다.
- 모든 결과에 공식 출처 링크를 남겼습니다. 수거요일·정확한 배출장소·특수봉투 종류는 성북구와 해당 건물의 안내를 우선합니다.

## 주요 판단 원칙

1. 배달 용기와 음식 내용물을 구분합니다. `엽떡`, `마라탕` 관련 별칭은 플라스틱 용기 문맥에 연결했습니다. 남은 음식·국물을 플라스틱으로 안내하지 않습니다.
2. 빨간 변색 자체를 재활용 불가의 증거로 삼지 않습니다. 실제 음식물·기름이 남았는지, 제거할 수 있는지 묻습니다.
3. 생수·음료 무색 투명 페트병과 기타 플라스틱 용기를 구분합니다. 투명페트병은 라벨 제거·압착 후 뚜껑을 닫아 배출하는 공식 안내를 사용합니다.
4. 과일 씨는 종류를 확인합니다. 복숭아 씨 안내를 모든 씨에 일반화하지 않습니다.
5. 작은 단추는 사진만으로 재질을 확정하지 않습니다. 수지·복합재질, 금속, 재사용 가능한 의류에 붙은 상태를 구분하고 성북구 확인이 필요한 부분을 표시합니다.
6. 전지·가스·조명은 손상과 잔여 내용물 여부를 먼저 확인합니다. 임의 천공·분해 등 위험한 처리 동작을 안내하지 않습니다.

## 우선 참고한 공식 자료

| 근거 | 범위와 반영 내용 |
|---|---|
| [재활용가능자원의 분리수거 등에 관한 지침](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) | 2026-01-01 시행 지침. 종이·용기·금속·의류·전지·조명·전기전자제품 등 기본 배출 요령과 지자체별 조정 가능성 |
| [공식 품목사전](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) | 품목별 재질·예외 확인. 개별 품목 URL은 아래 목록과 JSON에 기록 |
| [성북구 생활폐기물 안내](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) | 지역 수거 방식 재확인 경로. 이번 확인에서는 페이지 일부가 시간 초과되어 전체 본문을 안정적으로 대조하지 못했음 |
| [서울시 장본 후 분리배출 안내](https://news.seoul.go.kr/env/archives/558269) | 아이스팩·과일망·종이팩·폐식용유·부드러운 과일껍질. 사실 관계를 참고해 자체 문구로 작성했으며 해당 페이지의 이미지·도안은 사용하지 않음 |
| [서울시 음식물쓰레기 표준안 소개](https://mediahub.seoul.go.kr/archives/2004122) | 치킨 뼈·복숭아 씨·차 찌꺼기 등 일반쓰레기 구분 |
| [한국소비자원 분리배출 안내](https://www.kca.go.kr/webzine/board/view?div=kca_2202&linkId=393&menuId=MENU00322) | 음식물에서 제외되는 뼈·알껍데기·티백·커피 찌꺼기 등을 보조 대조 |
| [동대문구 재활용품 안내](https://www.ddm.go.kr/www/contents.do?key=860) | 열에 잘 녹지 않는 플라스틱 단추가 비해당 품목으로 예시된 지역 참고 자료. 모든 단추나 성북구의 현행 규칙으로 일반화하지 않음 |

## 품목별 출처 연결

| ID | 품목 | 근거 범위 | 결과에 연결된 공식 출처 |
|---|---|---|---|
| `pet_bottle` | 투명 생수·음료 페트병 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=122) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) · [자료 4](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `plastic_container` | 배달 플라스틱 용기 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `chicken_bone` | 치킨 뼈 | `seoul` | [자료 1](https://mediahub.seoul.go.kr/archives/2004122) · [자료 2](https://www.kca.go.kr/webzine/board/view?div=kca_2202&linkId=393&menuId=MENU00322) |
| `peach_pit` | 복숭아 씨 | `seoul` | [자료 1](https://mediahub.seoul.go.kr/archives/2004122) · [자료 2](https://www.kca.go.kr/webzine/board/view?div=kca_2202&linkId=393&menuId=MENU00322) |
| `clothing` | 옷 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `small_appliance` | 소형 폐가전 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `beverage_can` | 음료·맥주 캔 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=617) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `food_can` | 통조림·참치·햄 캔 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=85) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `aerosol_can` | 부탄가스·스프레이 캔 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `metal_cap` | 금속 병뚜껑 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `glass_bottle` | 음료·식품 유리병 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `deposit_glass_bottle` | 보증금 표시가 있는 소주·맥주병 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `broken_glass` | 깨진 유리·유리조각 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `ceramic` | 도자기·사기 그릇 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `cardboard_box` | 택배 골판지 상자 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/typeItemHtml.do) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `newspaper` | 신문지 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/typeItemHtml.do) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `book` | 책·책자 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/typeItemHtml.do) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `notebook` | 공책·스프링 노트 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/typeItemHtml.do) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `office_paper` | 복사지·일반 종이 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/typeItemHtml.do) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `milk_carton` | 우유팩 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=529) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `aseptic_carton` | 두유·주스 멸균팩 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=529) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `paper_cup` | 종이컵 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/typeItemHtml.do) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) · [자료 4](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `receipt` | 감열지 영수증 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=730) |
| `pizza_box` | 기름 묻은 피자·치킨 종이상자 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.wasteguide.or.kr/front/dischargeMethod/typeItemHtml.do) |
| `coated_paper` | 양면 코팅지·사진용지 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.wasteguide.or.kr/front/dischargeMethod/typeItemHtml.do) |
| `tissue` | 사용한 휴지·키친타월 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `plastic_cup` | 카페 일회용 플라스틱 컵 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=112) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 4](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `yogurt_container` | 요거트 플라스틱 용기 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=74) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `instant_rice_container` | 즉석밥 플라스틱 용기 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=98) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 4](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `shampoo_bottle` | 샴푸·바디워시 플라스틱 용기 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `detergent_bottle` | 세제 플라스틱 용기 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `colored_pet_bottle` | 유색·기타 용도 페트병 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `plastic_toothpaste_tube` | 치약 용기 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=593) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) · [자료 4](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `pet_cap` | 페트병 뚜껑 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=122) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `vinyl_bag` | 비닐봉지·위생팩 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=532) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 4](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `snack_wrapper` | 과자·라면 비닐 포장지 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=622) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 4](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `bubble_wrap` | 에어캡·뽁뽁이 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=624) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 4](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `shipping_bag` | 택배 비닐봉투 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=624) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 4](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `foam_packaging` | 스티로폼 상자·완충재 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 3](https://news.seoul.go.kr/env/archives/558269) |
| `fruit_foam_net` | 과일 포장망 | `seoul` | [자료 1](https://news.seoul.go.kr/env/archives/558269) |
| `water_ice_pack` | 물 아이스팩 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=779) · [자료 2](https://news.seoul.go.kr/env/archives/558269) · [자료 3](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) · [자료 4](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `gel_ice_pack` | 젤 아이스팩 | `seoul` | [자료 1](https://news.seoul.go.kr/env/archives/558269) |
| `battery` | 건전지·분리한 전지 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=537) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `power_bank` | 보조배터리 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=537) |
| `fluorescent_lamp` | 형광등 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=538) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `led_lamp` | LED 조명 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=137) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `button` | 옷 단추 | `seoul_other_district_reference` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.ddm.go.kr/www/contents.do?key=860) · [자료 3](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) · [자료 4](https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do) |
| `toothbrush` | 일반 칫솔 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `pen` | 볼펜·혼합재질 필기구 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=394) |
| `pencil` | 연필 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=316) |
| `rubber_glove` | 고무·라텍스 장갑 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=366) |
| `wet_wipe` | 사용한 물티슈 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=375) |
| `coffee_grounds` | 가정에서 나온 커피찌꺼기 | `seoul` | [자료 1](https://www.kca.go.kr/webzine/board/view?div=kca_2202&linkId=393&menuId=MENU00322) · [자료 2](https://mediahub.seoul.go.kr/archives/2004122) |
| `tea_bag` | 티백 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=419) |
| `eggshell` | 달걀·메추리알 껍데기 | `seoul` | [자료 1](https://mediahub.seoul.go.kr/archives/2004122) · [자료 2](https://www.kca.go.kr/webzine/board/view?div=kca_2202&linkId=393&menuId=MENU00322) |
| `shellfish_shell` | 조개·굴 껍데기 | `national` | [자료 1](https://www.wasteguide.or.kr/front/dischargeMethod/dictionaryView.do?niIdx=425) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |
| `banana_peel` | 바나나·귤 등 부드러운 과일 껍질 | `seoul` | [자료 1](https://news.seoul.go.kr/env/archives/558269) |
| `cooking_oil` | 폐식용유 | `seoul` | [자료 1](https://news.seoul.go.kr/env/archives/558269) · [자료 2](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) |
| `aluminum_foil` | 알루미늄 호일 | `national` | [자료 1](https://www.wasteguide.or.kr/front/bbsList.do?bbsId=BBS_0003) · [자료 2](https://www.sb.go.kr/www/selectBbsNttView.do?bbsNo=91&key=5927&nttNo=9502164) |

## 남은 확인 사항

- 성북구의 특수규격봉투·불연성 폐기물, 작은 금속 부품·단추, 손상된 전지, 조명 형태별 수거 방법을 담당 부서 또는 최신 홍보물로 추가 대조해야 합니다.
- 앱에는 아직 실제 수거함 좌표·실시간 운영 상태·배출요일 데이터가 없습니다. 지도 링크를 수거 가능 보장으로 표현하지 않습니다.
- 공식 사이트의 품목 수와 이 앱의 품목 수는 서로 다릅니다. 인식되지 않거나 등록되지 않은 물건은 검색·재질 확인·공식 안내 연결로 처리합니다.
- 검증은 JSON 구조, 중복 ID, 질문 연결, 순환·고아 노드, 출처 URL, 그룹 메타데이터를 확인한 것입니다. 지자체의 검수·승인을 의미하지 않습니다.
