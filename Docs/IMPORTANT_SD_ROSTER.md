# 중요 인물 전투 SD 완료 목록

2026-11-01: 처음 사용자에게 중요로 분류한 23명과 복합 그림 2종의 제작·리소스 연결을 완료했다. 애매로 분류한 인물 전체나 대화에만 등장하는 추가 인물을 완료했다는 뜻은 아니다. 실제 Unity 렌더링은 확인하지 못했다.

| 인물 | Battle 키 | 상태 |
|---|---|---|
| 나일천 / 서천 | `na_ilcheon` | 전용 SD |
| 무명 | `mumyeong` | 전용 SD |
| 칠상흔 / 이벽한 | `chil_sangheun` | 전용 SD |
| 독고령 / 영령 | `dokgo_ryeong` | 전용 SD |
| 공손절휘 | `gongson_jeolhwi` | 전용 SD |
| 진령 | `jin_ryeong` | 전용 SD |
| 임덕성 | `im_deokseong` | 전용 SD |
| 검성 모용정천 | `geom_seong` | 전용 SD |
| 흑룡왕 해응신 | `heukryong_wang` | 전용 SD |
| 락비오 | `rakbio` | 전용 SD |
| 자군 | `jagun` | 전용 SD |
| 전혼 | `jeonhon` | 전용 SD |
| 창랑 / 마랑객 | `changrang` | 전용 SD |
| 혈심란 교옥 | `gyo_ok` | 전용 SD |
| 구천현녀 무화 | `mu_hwa` | 전용 SD |
| 단혜 | `dan_hye` | 전용 SD |
| 사란 | `sa_ran` | 전용 SD |
| 갈효린 | `gal_hyo_rin` | 전용 SD |
| 갈효진 | `gal_hyo_jin` | 전용 SD |
| 갈효민 | `gal_hyo_min` | 전용 SD |
| 갈효효 | `gal_hyo_hyo` | 전용 SD |
| 갈효묘 | `gal_hyo_myo` | 전용 SD |
| 흑견 | `heukgyeon` | 전용 SD |

| 복합 그림 | Battle 키 | 상태 |
|---|---|---|
| 단혜·사란 불꽃과 얼음 | `mothers_fire_ice` | 두 인물의 합격 SD |
| 갈효민·갈효효 무음탄주 | `gal_hyo_duet` | 흑소·은금 합주 SD |

무명은 사용자 정정에 따라 백발·주름 없는 40대 초중반 외형과 녹슨 검으로, 임덕성은 사용자 선택대로 낭아도로 표현한다. 검성은 현재 전투에서 환영으로 나온다. 갈효민은 29권의 흑옥 흑소로 개인·합주 그림을 맞췄다. 디자인 근거와 미술 해석은 [ART_LOOKS.md](ART_LOOKS.md)의 각 제작 차수에 기록한다.

검증: 현재 코드의 25개 키에 연결된 적 57개 변형 모두에서 전용 SD를 조회했다. 실제 ArtLibrary 소스와 파일 기반 Resources/도형 대역으로 검사했으며, 기본 남성·여성·암살자 대체 규칙도 유지된다. Core 빌드와 StoryCast 식별 검사를 통과했다. Unity 에디터의 최종 렌더링은 미검증이다.

### 추가 완료: 전투 등장 인물 7종 — 2026-11-02

모용휘(`mo_yonghwi`), 남궁상(`namgung_sang`), 맹연호(`maeng_yeonho`), 이송학(`lee_songhak`), 위무상(`wi_musang`), 도추운(`do_chuun`), 윤미(`yunmi`)를 추가했다. 앞의 23명 명단과 별도로 보완한 인물들이다. 도추운 야간 변형은 같은 SD를 공유하고, 윤미는 윤준호의 변장 장면에만 적용한다.

검증: 신규 7종 1254×1254 RGBA 투명 PNG·Unity 메타파일을 확인했으며 기존 파일과 GUID/이미지 중복이 없다. 실제 ArtLibrary와 파일 기반 Resources 대역에서 Battle 151종·별칭 319개·적 300종의 키/ID/이름·동료 7명·윤미 변장·기존 노사부 구분을 검사했다. 관련 NUnit 70건 통과, 전체 Core 1,458건 통과·기존과 같은 밸런스 12건 실패다. Unity 실제 렌더링은 미검증이다.
