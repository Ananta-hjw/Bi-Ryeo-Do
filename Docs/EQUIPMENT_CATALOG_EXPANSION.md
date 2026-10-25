# Equipment Catalog Expansion

각 부위의 뽑기 장비 수량을 일반 20종·고급 17종·희귀 15종·영웅 12종·전설 8종·신화 6종으로 맞췄다. 신규 장비 162종을 추가했으며 기존 229종(초월 진비뢰도 포함)의 ID와 세트 구성을 유지한다. 내공 기본 효과는 모든 장비에 동일한 50% 감소 규칙을 적용한다. 이름과 설명은 게임용 창작이며 원작의 신규 설정으로 취급하지 않는다.

| 등급 | 무기 | 상의 | 하의 | 신발 | 장신구 | 등급 합계 | 신규 추가 |
|---|---:|---:|---:|---:|---:|---:|---:|
| 일반 | 20 | 20 | 20 | 20 | 20 | 100 | 40 |
| 고급 | 17 | 17 | 17 | 17 | 17 | 85 | 33 |
| 희귀 | 15 | 15 | 15 | 15 | 15 | 75 | 28 |
| 영웅 | 12 | 12 | 12 | 12 | 12 | 60 | 30 |
| 전설 | 8 | 8 | 8 | 8 | 8 | 40 | 19 |
| 신화 | 6 | 6 | 6 | 6 | 6 | 30 | 12 |

총 일반~신화 장비는 390종, 초월 진비뢰도를 포함하면 391종이다. 초월은 뽑기에서 제외한다. 소모품·재료는 목표 장비 수량에 포함하지 않는다.

## 능력치와 성장

기존 등급별 기본 효과 수준 안에서 공격·생존·내공·속도·치명 계열의 조합을 나눴다. 신규 항목도 기존 전체 장비와 기본 효과 조합 및 표시 효과 문자열이 중복되지 않도록 검사한다. 신화 비뢰 부위는 기존 최상위 비교 기준을 유지한다. 신규 장비에 세트 보너스는 추가하지 않는다. 영웅 이상은 기존 공통 1·3·5성 추가 옵션을, 그 아래는 기존 돌파 기본 보너스만 사용한다. 강화·분해·합성·동료 장착·추천 장비는 공통 규칙을 따른다.

뽑기의 등급 확률과 가격은 유지한다. 같은 등급의 풀은 더 커지므로 특정 기존 장비 하나의 개별 획득 확률은 낮아진다.

## 아이콘

`Art/Readable/gear_expansion_<grade>.png` 6종의 투명 배경 시트에 신규 아이콘 162개를 제작했다. 각 ID에는 별도 그림 영역을 지정했으며, 실제 행·열 사이의 투명 여백으로 경계를 측정했다. 기존 그림의 복제나 기본 아이콘 대체로 수량을 채우지 않는다. 무기·상의·하의·신발·장신구 순으로 행을 구분하고, 없는 칸은 투명 여백으로 남긴다. 비뢰 이외의 신규 장비에는 보라색 이펙트를 사용하지 않는다.

`catalog.json`, `ICON_SHEET_LAYOUTS.json`, `ITEM_ICON_AUDIT.csv`에 매핑·경계·검수 정보를 함께 기록한다. `python3 Tools/audit_icon_regions.py`로 전체 그림 영역의 유효성·겹침·검수 목록 누락을 확인한다. UI 테스트는 모든 아이템의 전용 그림 매핑을 확인한다. Unity 렌더링과 실제 터치 검증은 에디터/기기에서 별도로 필요하다.

## 신규 장비 목록

### 일반

| 부위 | 이름 | 기본 효과 | ID |
|---|---|---|---|
| 무기 | 수련용 단검 | 체력 +1% 공격 +4% 치명타 +1% | `catalog_normal_weapon_01` |
| 무기 | 떡갈나무 봉 | 공격 +3% 방어 +2% 속도 +1 | `catalog_normal_weapon_02` |
| 무기 | 약초꾼 낫 | 체력 +1% 공격 +4% | `catalog_normal_weapon_03` |
| 무기 | 황죽창 | 내공 +1% 공격 +3% | `catalog_normal_weapon_04` |
| 무기 | 무딘 환도 | 체력 +2% 공격 +3% 치명타 +1% | `catalog_normal_weapon_05` |
| 무기 | 산행 도끼 | 체력 +3% 공격 +3% 치명타 +1% | `catalog_normal_weapon_06` |
| 무기 | 철제 단척 | 공격 +4% 속도 +1 | `catalog_normal_weapon_07` |
| 상의 | 행상 적삼 | 방어 +3% 치명타 +1% | `catalog_normal_armor_01` |
| 상의 | 광목 도복 | 체력 +1% 방어 +4% | `catalog_normal_armor_02` |
| 상의 | 누더기 배자 | 내공 +1.5% 방어 +4% | `catalog_normal_armor_03` |
| 상의 | 산행 두루마기 | 내공 +1% 방어 +4% | `catalog_normal_armor_04` |
| 상의 | 목면 저고리 | 내공 +1.5% 방어 +3% 속도 +1 | `catalog_normal_armor_05` |
| 상의 | 대련용 조끼 | 내공 +1% 방어 +3% 속도 +1 | `catalog_normal_armor_06` |
| 상의 | 덧댄 장삼 | 공격 +1% 방어 +4% | `catalog_normal_armor_07` |
| 하의 | 목면 수련바지 | 체력 +5% 방어 +2% 치명타 +1% | `catalog_normal_pants_01` |
| 하의 | 산행 각반 | 체력 +6% 치명타 +1% | `catalog_normal_pants_02` |
| 하의 | 광목 행전 | 체력 +4% 내공 +0.5% 방어 +3% | `catalog_normal_pants_03` |
| 하의 | 염색 베바지 | 체력 +5% 방어 +1% 속도 +1 | `catalog_normal_pants_04` |
| 하의 | 마부 바지 | 체력 +4% 내공 +2% 공격 +1% | `catalog_normal_pants_05` |
| 하의 | 품 넓은 잠방이 | 체력 +5% 방어 +1% | `catalog_normal_pants_06` |
| 하의 | 덧댄 무명고 | 체력 +4% 방어 +1% 속도 +1 | `catalog_normal_pants_07` |
| 하의 | 대련용 각반 | 체력 +3% 공격 +2% 치명타 +1% | `catalog_normal_pants_08` |
| 하의 | 짚끈 바지 | 체력 +5% 내공 +0.5% 속도 +1 | `catalog_normal_pants_09` |
| 하의 | 행상 통바지 | 체력 +6% 내공 +1% | `catalog_normal_pants_10` |
| 신발 | 볏짚 미투리 | 공격 +2% 속도 +1 치명타 +1% | `catalog_normal_boots_01` |
| 신발 | 목면 버선신 | 내공 +1.5% 공격 +1% 속도 +1 | `catalog_normal_boots_02` |
| 신발 | 여행용 마혜 | 내공 +1% 공격 +2% 속도 +1 | `catalog_normal_boots_03` |
| 신발 | 굽 낮은 가죽화 | 공격 +3% 속도 +1 치명타 +1% | `catalog_normal_boots_04` |
| 신발 | 등짐꾼 짚신 | 체력 +4% 속도 +1 치명타 +1% | `catalog_normal_boots_05` |
| 신발 | 덧댄 포혜 | 방어 +3% 속도 +1 치명타 +1% | `catalog_normal_boots_06` |
| 신발 | 삼끈 행전신 | 방어 +4% 속도 +1 치명타 +1% | `catalog_normal_boots_07` |
| 신발 | 산행 목화 | 방어 +4% 속도 +1 | `catalog_normal_boots_08` |
| 신발 | 두툼한 헝겊화 | 체력 +3% 속도 +1 치명타 +1% | `catalog_normal_boots_09` |
| 장신구 | 솔나무 염주 | 내공 +3% 방어 +2% | `catalog_normal_accessory_01` |
| 장신구 | 삼끈 호패 | 체력 +5% 내공 +2% 속도 +1 | `catalog_normal_accessory_02` |
| 장신구 | 황동 손목고리 | 체력 +2% 공격 +5% | `catalog_normal_accessory_03` |
| 장신구 | 백석 목걸이 | 내공 +2% 방어 +4% | `catalog_normal_accessory_04` |
| 장신구 | 목각 매듭패 | 체력 +2% 내공 +1.5% 속도 +1 | `catalog_normal_accessory_05` |
| 장신구 | 물소뿔 가락지 | 체력 +5% 내공 +1.5% 치명타 +1% | `catalog_normal_accessory_06` |
| 장신구 | 붉은실 노리개 | 체력 +2% 공격 +3% 속도 +1 | `catalog_normal_accessory_07` |

### 고급

| 부위 | 이름 | 기본 효과 | ID |
|---|---|---|---|
| 무기 | 연철 단검 | 공격 +5% 방어 +6% | `catalog_fine_weapon_01` |
| 무기 | 적동 유엽도 | 공격 +7% 속도 +1 | `catalog_fine_weapon_02` |
| 무기 | 호위 장창 | 내공 +3% 공격 +5% | `catalog_fine_weapon_03` |
| 무기 | 청목 장봉 | 공격 +7% 치명타 +2% | `catalog_fine_weapon_04` |
| 무기 | 철엽 부채 | 공격 +6% 속도 +1 | `catalog_fine_weapon_05` |
| 상의 | 호위 경갑 | 공격 +2% 방어 +6% 치명타 +2% | `catalog_fine_armor_01` |
| 상의 | 청사 무복 | 체력 +3% 방어 +5% 속도 +2 | `catalog_fine_armor_02` |
| 상의 | 녹피 조끼 | 체력 +1% 내공 +0.5% 방어 +7% | `catalog_fine_armor_03` |
| 상의 | 연철 흉갑 | 공격 +1% 방어 +6% 속도 +1 | `catalog_fine_armor_04` |
| 상의 | 표행 겉옷 | 방어 +5% 치명타 +2% | `catalog_fine_armor_05` |
| 상의 | 황면 전포 | 내공 +1.5% 방어 +5% 치명타 +2% | `catalog_fine_armor_06` |
| 하의 | 호위 경장고 | 체력 +6% 내공 +2% 속도 +2 | `catalog_fine_pants_01` |
| 하의 | 청사 수련고 | 체력 +7% 방어 +1% 치명타 +1% | `catalog_fine_pants_02` |
| 하의 | 연철 정강이갑 | 체력 +6% 공격 +4% 치명타 +2% | `catalog_fine_pants_03` |
| 하의 | 녹피 각반 | 체력 +7% 치명타 +2% | `catalog_fine_pants_04` |
| 하의 | 표행 통바지 | 체력 +7% 공격 +1% 방어 +4% | `catalog_fine_pants_05` |
| 하의 | 질긴 면포고 | 체력 +8% 방어 +5% | `catalog_fine_pants_06` |
| 하의 | 황면 누비고 | 체력 +8% 내공 +3% 치명타 +1% | `catalog_fine_pants_07` |
| 하의 | 적동 무릎갑 | 체력 +6% 방어 +3% 속도 +1 | `catalog_fine_pants_08` |
| 하의 | 은사 행전 | 체력 +6% 방어 +6% 치명타 +1% | `catalog_fine_pants_09` |
| 신발 | 표행 가죽화 | 내공 +2% 속도 +1 치명타 +2% | `catalog_fine_boots_01` |
| 신발 | 청사 경공화 | 공격 +5% 속도 +1 | `catalog_fine_boots_02` |
| 신발 | 연철 군화 | 내공 +2.5% 공격 +4% 속도 +2 | `catalog_fine_boots_03` |
| 신발 | 산사슴 비화 | 체력 +4% 공격 +5% 속도 +2 | `catalog_fine_boots_04` |
| 신발 | 녹피 장화 | 내공 +3% 방어 +4% 속도 +2 | `catalog_fine_boots_05` |
| 신발 | 황면 덧신 | 공격 +4% 방어 +2% 속도 +2 | `catalog_fine_boots_06` |
| 신발 | 무관 단화 | 내공 +2.5% 공격 +3% 속도 +2 | `catalog_fine_boots_07` |
| 신발 | 적동 징화 | 공격 +6% 속도 +2 치명타 +1% | `catalog_fine_boots_08` |
| 장신구 | 청사 향낭 | 체력 +2% 내공 +4% 속도 +2 | `catalog_fine_accessory_01` |
| 장신구 | 연철 호심패 | 체력 +8% 공격 +4% | `catalog_fine_accessory_02` |
| 장신구 | 산호 귀고리 | 공격 +9% 방어 +1% | `catalog_fine_accessory_03` |
| 장신구 | 백동 팔찌 | 방어 +8% 치명타 +2% | `catalog_fine_accessory_04` |
| 장신구 | 금실 매듭장식 | 내공 +4% 치명타 +2% | `catalog_fine_accessory_05` |

### 희귀

| 부위 | 이름 | 기본 효과 | ID |
|---|---|---|---|
| 무기 | 월백검 | 체력 +2% 공격 +11% | `catalog_rare_weapon_01` |
| 무기 | 적수도 | 공격 +9% 치명타 +1% | `catalog_rare_weapon_02` |
| 무기 | 옥죽곤 | 공격 +11% 치명타 +1% | `catalog_rare_weapon_03` |
| 무기 | 청상창 | 체력 +9% 공격 +8% | `catalog_rare_weapon_04` |
| 무기 | 현사편 | 공격 +11% 방어 +5% | `catalog_rare_weapon_05` |
| 상의 | 청상 전포 | 방어 +7% 속도 +3 치명타 +1% | `catalog_rare_armor_01` |
| 상의 | 옥사 호신복 | 공격 +3% 방어 +9% 치명타 +1% | `catalog_rare_armor_02` |
| 상의 | 현린 경갑 | 체력 +2% 내공 +2.5% 방어 +10% | `catalog_rare_armor_03` |
| 상의 | 적수 무복 | 방어 +9% 속도 +2 치명타 +3% | `catalog_rare_armor_04` |
| 상의 | 월백 장삼 | 내공 +3% 방어 +10% | `catalog_rare_armor_05` |
| 하의 | 월백 비단고 | 체력 +11% 내공 +0.5% 공격 +8% | `catalog_rare_pants_01` |
| 하의 | 청상 경장고 | 체력 +12% 내공 +4.5% | `catalog_rare_pants_02` |
| 하의 | 옥사 행전 | 체력 +10% 내공 +2% 방어 +8% | `catalog_rare_pants_03` |
| 하의 | 현린 각반 | 체력 +13% 내공 +2% 치명타 +3% | `catalog_rare_pants_04` |
| 하의 | 적수 전투고 | 체력 +11% 내공 +3.5% | `catalog_rare_pants_05` |
| 하의 | 백사 누비고 | 체력 +11% 내공 +2.5% 속도 +3 | `catalog_rare_pants_06` |
| 하의 | 운학 장고 | 체력 +12% 공격 +7% 속도 +2 | `catalog_rare_pants_07` |
| 하의 | 벽옥 무릎갑 | 체력 +10% 방어 +7% 치명타 +1% | `catalog_rare_pants_08` |
| 신발 | 월백 경공화 | 체력 +8% 내공 +3.5% 속도 +4 | `catalog_rare_boots_01` |
| 신발 | 청상 비화 | 체력 +8% 공격 +3% 속도 +4 | `catalog_rare_boots_02` |
| 신발 | 옥사 장화 | 내공 +4.5% 방어 +7% 속도 +3 | `catalog_rare_boots_03` |
| 신발 | 현린 전투화 | 방어 +8% 속도 +4 | `catalog_rare_boots_04` |
| 신발 | 적수 단화 | 체력 +7% 속도 +4 치명타 +1% | `catalog_rare_boots_05` |
| 신발 | 운학 포혜 | 내공 +4% 속도 +4 치명타 +1% | `catalog_rare_boots_06` |
| 신발 | 벽옥 철화 | 공격 +8% 속도 +2 | `catalog_rare_boots_07` |
| 신발 | 백사 보운리 | 공격 +7% 속도 +3 치명타 +1% | `catalog_rare_boots_08` |
| 장신구 | 벽옥 호심패 | 체력 +9% 내공 +4.5% 방어 +7% | `catalog_rare_accessory_01` |
| 장신구 | 운학 깃노리개 | 체력 +14% 방어 +7% 속도 +2 | `catalog_rare_accessory_02` |

### 영웅

| 부위 | 이름 | 기본 효과 | ID |
|---|---|---|---|
| 무기 | 홍련검 | 공격 +13% 속도 +4 치명타 +2% | `catalog_epic_weapon_01` |
| 무기 | 창해도 | 내공 +1.5% 공격 +15% 방어 +2% | `catalog_epic_weapon_02` |
| 무기 | 금사 장창 | 공격 +16% 속도 +1 치명타 +3% | `catalog_epic_weapon_03` |
| 무기 | 설풍 쌍도 | 공격 +12% 방어 +3% 치명타 +3% 치명타 피해 +8% | `catalog_epic_weapon_04` |
| 무기 | 백매 철선 | 내공 +5.5% 공격 +13% 치명타 피해 +3% | `catalog_epic_weapon_05` |
| 상의 | 홍련 호신포 | 공격 +10% 방어 +14% 치명타 +1% | `catalog_epic_armor_01` |
| 상의 | 창해 운의 | 체력 +6% 내공 +6% 방어 +12% | `catalog_epic_armor_02` |
| 상의 | 금사 쇄자갑 | 공격 +2% 방어 +13% 치명타 피해 +6% | `catalog_epic_armor_03` |
| 상의 | 설풍 전포 | 공격 +5% 방어 +11% 속도 +1 치명타 피해 +4% | `catalog_epic_armor_04` |
| 상의 | 백매 수련복 | 체력 +3% 방어 +14% 속도 +4 | `catalog_epic_armor_05` |
| 하의 | 홍련 전투고 | 체력 +16% 공격 +10% 속도 +4 | `catalog_epic_pants_01` |
| 하의 | 창해 운고 | 체력 +16% 속도 +5 치명타 +1% 치명타 피해 +6% | `catalog_epic_pants_02` |
| 하의 | 금사 정강이갑 | 체력 +18% 방어 +8% 치명타 +3% 치명타 피해 +2% | `catalog_epic_pants_03` |
| 하의 | 설풍 비단고 | 체력 +16% 내공 +6% 치명타 +1% 치명타 피해 +5% | `catalog_epic_pants_04` |
| 하의 | 백매 장고 | 체력 +11% 내공 +3.5% 속도 +3 치명타 피해 +4% | `catalog_epic_pants_05` |
| 하의 | 흑송 경장고 | 체력 +12% 속도 +5 치명타 +3% | `catalog_epic_pants_06` |
| 하의 | 비취 호슬갑 | 체력 +15% 방어 +8% 치명타 피해 +4% | `catalog_epic_pants_07` |
| 하의 | 적린 각반 | 체력 +10% 내공 +3% 공격 +6% 치명타 피해 +2% | `catalog_epic_pants_08` |
| 신발 | 홍련 경공화 | 체력 +13% 공격 +9% 방어 +2% 속도 +5 | `catalog_epic_boots_01` |
| 신발 | 창해 보운리 | 방어 +12% 속도 +4 치명타 +4% | `catalog_epic_boots_02` |
| 신발 | 금사 전투화 | 체력 +9% 공격 +4% 방어 +6% 속도 +3 | `catalog_epic_boots_03` |
| 신발 | 설풍 무영화 | 체력 +8% 내공 +2% 방어 +13% 속도 +5 | `catalog_epic_boots_04` |
| 신발 | 백매 단화 | 방어 +5% 속도 +6 치명타 +4% 치명타 피해 +5% | `catalog_epic_boots_05` |
| 신발 | 흑송 장화 | 체력 +10% 공격 +7% 속도 +4 | `catalog_epic_boots_06` |
| 신발 | 비취 풍행화 | 공격 +11% 속도 +3 치명타 +2% | `catalog_epic_boots_07` |
| 신발 | 적린 철화 | 체력 +2% 내공 +5% 방어 +11% 속도 +3 | `catalog_epic_boots_08` |
| 장신구 | 백매 옥비녀 | 내공 +9.5% 공격 +6% 속도 +3 | `catalog_epic_accessory_01` |
| 장신구 | 창해 명주 | 체력 +13% 방어 +3% 치명타 +3% 치명타 피해 +2% | `catalog_epic_accessory_02` |
| 장신구 | 흑송 호심경 | 체력 +5% 공격 +13% 치명타 +4% | `catalog_epic_accessory_03` |
| 장신구 | 금사 결속환 | 체력 +10% 방어 +11% 치명타 +3% | `catalog_epic_accessory_04` |

### 전설

| 부위 | 이름 | 기본 효과 | ID |
|---|---|---|---|
| 무기 | 일월 쌍검 | 체력 +4% 내공 +7.5% 공격 +21% 속도 +3 | `catalog_legendary_weapon_01` |
| 무기 | 벽해 장창 | 체력 +5% 공격 +21% 치명타 +2% | `catalog_legendary_weapon_02` |
| 무기 | 백린 파천극 | 공격 +21% 방어 +8% 치명타 +2% | `catalog_legendary_weapon_03` |
| 상의 | 일월 보의 | 방어 +18% 치명타 +5% 치명타 피해 +7% | `catalog_legendary_armor_01` |
| 상의 | 벽해 비늘갑 | 내공 +2.5% 공격 +5% 방어 +22% 치명타 +4% | `catalog_legendary_armor_02` |
| 상의 | 백린 호신포 | 내공 +8% 방어 +21% 치명타 +3% | `catalog_legendary_armor_03` |
| 하의 | 일월 천잠고 | 체력 +23% 공격 +12% 방어 +6% 치명타 피해 +8% | `catalog_legendary_pants_01` |
| 하의 | 벽해 용린고 | 체력 +25% 내공 +0.5% 공격 +7% 속도 +2 | `catalog_legendary_pants_02` |
| 하의 | 백린 금강고 | 체력 +15% 내공 +6% 방어 +14% 속도 +7 | `catalog_legendary_pants_03` |
| 하의 | 홍운 비단고 | 체력 +21% 속도 +7 치명타 피해 +10% | `catalog_legendary_pants_04` |
| 하의 | 태산 철각반 | 체력 +19% 내공 +6% 공격 +10% 치명타 +2% | `catalog_legendary_pants_05` |
| 신발 | 일월 유영화 | 내공 +6% 속도 +9 치명타 +1% 치명타 피해 +12% | `catalog_legendary_boots_01` |
| 신발 | 벽해 답파화 | 내공 +3% 공격 +12% 속도 +9 | `catalog_legendary_boots_02` |
| 신발 | 백린 무영리 | 체력 +16% 방어 +18% 속도 +5 치명타 피해 +3% | `catalog_legendary_boots_03` |
| 신발 | 홍운 비천화 | 공격 +8% 방어 +15% 속도 +8 치명타 피해 +7% | `catalog_legendary_boots_04` |
| 신발 | 태산 금강화 | 내공 +6% 방어 +18% 속도 +6 | `catalog_legendary_boots_05` |
| 신발 | 은월 보행리 | 공격 +12% 속도 +6 치명타 +5% 치명타 피해 +6% | `catalog_legendary_boots_06` |
| 장신구 | 일월 보주 | 내공 +11.5% 방어 +9% 속도 +6 | `catalog_legendary_accessory_01` |
| 장신구 | 벽해 호신경 | 체력 +24% 방어 +16% 속도 +2 | `catalog_legendary_accessory_02` |

### 신화

| 부위 | 이름 | 기본 효과 | ID |
|---|---|---|---|
| 무기 | 태양신창 | 체력 +15% 공격 +30% 치명타 피해 +13% | `catalog_mythic_weapon_01` |
| 무기 | 창해월륜 | 체력 +17% 공격 +28% 속도 +10 | `catalog_mythic_weapon_02` |
| 상의 | 태양 천린갑 | 내공 +9.5% 방어 +24% 치명타 +3% 치명타 피해 +9% | `catalog_mythic_armor_01` |
| 상의 | 창해 월백포 | 체력 +17% 방어 +25% 속도 +4 치명타 피해 +12% | `catalog_mythic_armor_02` |
| 하의 | 태양 천린고 | 체력 +21% 공격 +10% 방어 +9% 치명타 피해 +12% | `catalog_mythic_pants_01` |
| 하의 | 창해 월백고 | 체력 +20% 공격 +17% 치명타 피해 +14% | `catalog_mythic_pants_02` |
| 하의 | 금오 천잠고 | 체력 +21% 공격 +18% 치명타 피해 +14% | `catalog_mythic_pants_03` |
| 하의 | 현천 금강고 | 체력 +20% 방어 +20% 치명타 피해 +16% | `catalog_mythic_pants_04` |
| 신발 | 태양 축일화 | 체력 +20% 공격 +21% 방어 +5% 속도 +10 | `catalog_mythic_boots_01` |
| 신발 | 창해 월행리 | 공격 +3% 방어 +18% 속도 +11 치명타 피해 +18% | `catalog_mythic_boots_02` |
| 신발 | 금오 비천화 | 체력 +22% 공격 +8% 방어 +13% 속도 +8 | `catalog_mythic_boots_03` |
| 신발 | 현천 답운화 | 체력 +7% 공격 +18% 속도 +11 치명타 +2% | `catalog_mythic_boots_04` |

## 검증 결과

관련 Core 테스트 139개 통과. 전체 Core 검사는 813개 중 809개 통과·기존 밸런스 검사 4개 실패이며 새 실패는 없다. 아이콘 감사 도구는 547개 매핑·475개 측정 영역·443개 아이템의 경계와 중복을 검사해 통과했다. Core/Game 및 UI 테스트 소스 참조 컴파일 통과. 실제 Unity UI 실행·Android 터치 검증은 수행하지 못했다.

원격 내공 조정 병합: 신규 162종의 기본 내공 효과와 설명에도 기존 장비와 동일한 50% 감소를 반영했다. 돌파 옵션·세트 내공은 병합된 공통 규칙을 따른다.
