# 중요 인물 전투 SD 완료 목록

## 위천무·매화검선·모경·옥현진인·허주운 — 2026-11-08

기존 Standing을 직접 참조한 `wi_cheonmu`, `maehwa_geomseon`, `mo_gyeong`, `okhyeon_jinin`, `heo_juun`의 전투 SD와 Unity 메타파일을 추가했다. 모두 오른쪽 방향 1254×1254 RGBA 투명 PNG다. 매화검선은 비류연의 변장 `maehwa_mask_trial`과 다른 키·그림을 사용하고 모경은 한 자루 도와 빈 칼집으로 정리했다.

매화검선·유환권·옥현진인·허주운·천수신의의 정확한 SD 이름 연결을 보완했다. 원화·이전 SD는 보존한다. 파일 크기·알파·인물/소품 경계·이미지/GUID 중복·이름 연결을 정적으로 검사했다. Unity 실제 화면·좌우 반전·전투 칸 크기는 미검증이다.

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

### 추가 완료: 중요 전투 인물 5종 — 2026-11-07

남궁산산(`namgung_sansan`), 현운(`hyeon_un`), 당철영/당삼(`dang_cheolyeong`), 백무영/지룡/형산일기(`baek_muyeong`), 류은경(`ryu_eungyeong`)의 전용 Battle SD를 추가했다. 기존 Standing을 외형 기준으로 삼고 오른쪽 적을 바라보는 전투 자세로 바꿨다. 기존 Roster·ArtLibrary 키를 그대로 사용한다.

검증: 5종 모두 1254×1254 RGBA이며 인물·무기·발끝이 캔버스 안에 있다. 신규 PNG 중복과 Unity GUID 중복이 없다. 실제 ArtLibrary 소스를 파일 기반 Resources 대역과 함께 실행해 5개 키·8개 이름/별칭·16개 출전 슬롯이 새 그림을 조회하고 기존 기본 그림 3종도 유지됨을 확인했다. Unity 실제 화면과 좌우 반전 합성은 미검증이다.

### 추가 완료: 이진설·유은성·유재룡·몽환쌍무 — 2026-11-07

이진설(`i_jinseol`), 유은성/낙일검(`yu_eunseong`), 유재룡(`yu_jaeryong`), 몽무(`mong_mu`), 환무(`hwan_mu`)의 전용 Battle SD를 추가했다. 앞의 네 명은 같은 키 Standing을 참고했고, Standing이 없는 환무는 과묵한 성격과 몽환쌍무의 피리를 바탕으로 몽무와 구분되는 외형을 새로 해석했다. 모두 오른쪽을 바라보는 1254×1254 RGBA다.

실제 ArtLibrary와 파일 기반 Resources 대역에서 신규 5개 키·6개 이름/별칭·7개 출전 슬롯·몽무/환무 적 2종의 전용 그림 연결, 앞선 신규 5명 및 기본 SD 3종 유지를 확인했다. 인물/무기/피리/발끝은 캔버스 안에 있고, 모서리 알파는 모두 0이다. 신규 PNG·Unity GUID는 중복이 없다. Unity 실제 합성·좌우 반전·전투 칸 크기는 미검증이다.

## 돈왕·몽환장주·갈중하·안명후·마천각주 — 2026-11-08

현재 브랜치에 `donwang`, `monghwan_jangju`, `gal_jungha`, `an_myeonghu`, `macheon_gakju`의 Battle SD 5종과 Unity 메타파일을 추가했다. 로컬 `work`(49b4ccc)의 Standing을 참조한 오른쪽 방향 1254×1254 RGBA다. 돈왕의 장부·반지, 몽환장주의 장포, 갈중하의 쌍도·흑사자패, 안명후의 철봉·문서통, 마천각주의 북(北) 청동가면을 구분한다.

현재 `ArtLibrary`의 SD 전용 이름·별칭을 추가했다. 이전 5명과 원화는 보존한다. 크기·알파·인물/소품 경계·이미지/GUID 중복·이름 연결을 정적으로 검사했다. 실제 Unity 렌더링·좌우 반전·전투 칸 크기는 미검증이다.

## 고약한·용경의·섭운명·공손경운·불락구척 — 2026-11-08

현재 `ccr-358d1bdb-dic5c1`에 Battle SD 5종과 Unity 메타파일을 추가했다. 참조 원화는 로컬 `work` 브랜치(49b4ccc)의 Standing이며 이 브랜치의 기존 그림은 보존한다.

| 인물 | Battle 키 | 특징 |
|---|---|---|
| 고약한 | `go_yakhan` | 어두운 무복·검술 준비 자세 |
| 용경의 | `yong_gyeongui` | 오른팔 없음·왼손에 칼집에 든 도 |
| 섭운명 | `seop_unmyeong` | 오른팔 없음·얼굴 검흔·검 없는 검결 |
| 공손경운 | `gongson_gyeongun` | 가주 정복·수비 자세 |
| 불락구척 | `bullak_gucheok` | 근육 거구·봉합 흔적·의료 도구 |

모두 오른쪽을 바라보는 1254×1254 RGBA 투명 PNG다. 현재 브랜치의 `ArtLibrary.SdFigure`에 전용 이름·별칭을 추가했다. 직접 파일 키도 기존 Chibi 경로로 조회한다. Standing 조회에는 적용하지 않는다.

크기·알파·눈에 보이는 인물/소품 경계·이미지 및 GUID 중복·소스 연결을 정적으로 검사했다. Unity 실제 렌더링·좌우 반전·전투 칸 크기는 미검증이다. 상세 기준은 `ART_LOOKS.md`와 `Story/GAME_MAPPING.md` 참조.
