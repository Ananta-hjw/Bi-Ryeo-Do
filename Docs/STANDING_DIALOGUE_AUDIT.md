# 대화 화자 Standing 누락 조사

조사일: 2026-11-01. 초기 조사 기준 커밋: `2c35fbd`. 아래 집계는 중요 인물·모습 입화 12종과 대화 별칭을 추가한 뒤 다시 확인한 결과다. 진행 중인 여정 지도 변경은 조사 결과에 영향 없음.

66개 스테이지의 일반·어려움·극악 본문, 실제 사용하는 구간 장면, 선택지의 모든 결과, 도입·막간·후일담, 나예린 추억, 해당 전투의 보스 단계 대사와 화룡멸겁대진 연출 대사를 조사했다. 단순히 지문에 이름이 언급된 인물이나 더 이상 사용하지 않는 옛 구간 대사는 누락 인물로 세지 않았다.

`StoryLine.Parse`로 실제 대화 화자를 추출하고 현재 `ArtLibrary.DialoguePortrait` 전체 소스의 별칭·StoryCast·동료·적 이름 해석을 적용했다. 외부 .NET 감사 도구에서 Unity의 텍스처 로딩 부분만 실제 `Resources` 파일 존재 확인으로 대체했다. Unity 렌더링·그림의 외형 일치 여부를 검사한 것은 아니다.

| 구분 | 화자 표기 수 |
|---|---:|
| 조사한 서로 다른 대화 화자 표기 | 158 |
| Standing 파일을 정상적으로 찾는 화자 | 113 |
| Standing 조회가 실패하는 화자 | 45 |
| 위 45종 중 기존 그림을 연결할 수 있는 명확한 별칭 | 5 |
| 전용 파일이 없는 이름·고유호칭 | 16 |
| 전용 파일이 없는 공용·집단·익명 호칭 | 24 |

이 수치는 실제 등장인물 수나 새로 그려야 할 그림 수와 다르다. 같은 인물의 별칭·어린 모습·변장과 여러 사람을 가리키는 공용 호칭이 섞여 있다. 공용 화자는 적합한 기존 대표 그림을 재사용할 수 있지만 현재 코드에는 해당 연결이 없다. 파일이 없을 때 대화 화면은 색상 블록과 이름 첫 글자를 표시한다.

## 전용 파일이 없는 이름·고유호칭 16종

대화에서 사용하는 표기를 기준으로 기재했다. `현재 조회 키`가 한글인 경우는 인물 등록·별칭에서 찾지 못해 대화 이름 자체를 파일명으로 조회한 경우다. 이 키는 새 그림 파일명 권장안이 아니다.

| 대화 화자 | 대화 등장 스테이지 | 현재 조회 키 |
|---|---|---|
| 등여운 | 2 | `deung_yeoun` |
| 등여호 | 2 | `deung_yeoho` |
| 노주생 | 11 | `no_jusaeng` |
| 채하군 | 11 | `chae_hagun` |
| 막적 | 14 | `mak_jeok` |
| 맹연호 | 14 | `maeng_yeonho` |
| 위무상 | 15 | `wi_musang` |
| 조걸 | 15 | `조걸` |
| 초월 | 16 | `초월` |
| 단옥도 팽유경 | 17 | `pae_yugyeong` |
| 이무강 | 17 | `이무강` |
| 늑기한 | 18 | `늑기한` |
| 강씨 | 20 | `강씨` |
| 이송학 | 21, 22 | `lee_songhak` |
| 당철악 | 16 | `당철악` |
| 혈창 | 23 | `hyeol_chang` |

## 1차에서 해결한 중요 인물·모습 6종

초기 미연결 화자 57종에서 아래 6종을 해결해 51종이 남았다. 새 그림은 RGBA 투명 입화이며 실제 대사 이름으로 조회 성공을 확인했다.

| 대화 화자 | 추가 파일 |
|---|---|
| 예청 | [ye_cheong.png](../Assets/_Project/Resources/Art/Standing/ye_cheong.png) |
| 옥유경 | [ok_yugyeong.png](../Assets/_Project/Resources/Art/Standing/ok_yugyeong.png) |
| 연비 | [yeonbi.png](../Assets/_Project/Resources/Art/Standing/yeonbi.png) |
| 린 | [young_na_yerin.png](../Assets/_Project/Resources/Art/Standing/young_na_yerin.png) |
| 혁월린 | [hyeok_wolrin.png](../Assets/_Project/Resources/Art/Standing/hyeok_wolrin.png) |
| 흑천맹주 | [gal_jungcheon.png](../Assets/_Project/Resources/Art/Standing/gal_jungcheon.png) |

`린`은 어린 나예린, `연비`는 비류연의 여장이므로 성인 나예린·비류연 입화와 구분했다. `갈중천`과 이 구간의 대사 호칭 `흑천맹주`는 같은 `gal_jungcheon` 그림으로 연결한다.

## 2차에서 해결한 중요 인물·모습 6종

미연결 화자 51종에서 아래 6종을 해결해 45종이 남았다. 실제 대사 이름으로 전용 입화를 찾는 것을 확인했다.

| 대화 화자 | 추가 파일 |
|---|---|
| 관설지 | `gwan_seolji_story.png` |
| 동방학 | `dongbang_hak.png` |
| 매화가면 | `maehwa_mask_trial.png` |
| 은가면 비탄 | `mask_bitan.png` |
| 종리추 | `jongri_chu.png` |
| 신유성 | `shin_yuseong.png` |

신유성은 StoryCast에 등록하고 은가면 비탄은 Standing 전용 별칭으로 연결했다. 종리추의 야간 ID `jongri_chu_night`도 같은 입화를 사용한다. 6종은 1024×1536 RGBA이며 외부 빈 영역은 투명하다. 동방학은 후속 재디자인 후 네 모서리 알파 0을 확인했다.

## 기존 그림이 있으나 대화 별칭이 연결되지 않은 5종

| 대화 화자 | 스테이지 | 연결 가능한 기존 그림 |
|---|---|---|
| 사감 강하윤 | 7 | 강하윤 — [gang_hayun.png](../Assets/_Project/Resources/Art/Standing/gang_hayun.png) |
| 소녀 | 9 | 나예린 — [na_yerin.png](../Assets/_Project/Resources/Art/Standing/na_yerin.png) |
| 노인 | 30 | 사부 — [hwan_sabu.png](../Assets/_Project/Resources/Art/Standing/hwan_sabu.png) |
| 은가면 공포 | 30 | 은가면 공포 — [mask_gongpo.png](../Assets/_Project/Resources/Art/Standing/mask_gongpo.png) |
| 은가면 절망 | 30 | 은가면 절망 — [mask_jeolmang.png](../Assets/_Project/Resources/Art/Standing/mask_jeolmang.png) |

위 항목은 새 그림 제작보다 대화용 별칭 연결을 보완하는 편이 적절하다. 이 5종의 연결 코드는 이번 작업에서 수정하지 않았다.

## 공용·집단·익명 호칭 24종

각 호칭을 새로운 한 명의 캐릭터로 계산하면 안 된다. 특히 관도·관중·표두·부채주 등은 장면마다 같은 인물을 가리킨다고 확정할 수 없다.

| 대화 화자 | 대화 등장 스테이지 | 현재 조회 키 |
|---|---|---|
| 의원 | 2 | `의원` |
| 표두 | 5, 14 | `표두` |
| 응시생 | 7 | `admit_jeomchang` |
| 관도 | 9, 12, 13, 14, 15, 19, 20, 22 | `관도` |
| 구정팔검 | 9 | `구정팔검` |
| 천응방 주인 | 9 | `천응방 주인` |
| 서고 관리인 | 11 | `서고 관리인` |
| 관중 | 12, 46 | `관중` |
| 내기꾼 | 13 | `내기꾼` |
| 심사 노사 | 13 | `심사 노사` |
| 심판 | 13 | `심판` |
| 부채주 | 14, 21 | `부채주` |
| 장로 | 14 | `장로` |
| 사대질풍 | 15 | `sadae_jilpung` |
| 자객 | 16 | `baegin_jagaek` |
| 구경꾼 | 17 | `구경꾼` |
| 신응대원 | 17 | `신응대원` |
| 구정회원 | 18 | `구정회원` |
| 미행자 | 18 | `미행자` |
| 점원 | 18 | `점원` |
| 조 총관 | 18 | `조 총관` |
| 숙부 | 19 | `숙부` |
| 혈마대원 | 16 | `혈마대원` |
| 누군가 | 30 | `누군가` |

## 확인 위치

- [대화 파서](../Assets/_Project/Scripts/Core/StoryLine.cs)
- [Standing 초상화 및 별칭 해석](../Assets/_Project/Scripts/Game/Systems/ArtLibrary.cs)
- [이야기 본문 수집](../Assets/_Project/Scripts/Core/StoryEpisodes.cs)
- [실제 초상화 대체 표시](../Assets/_Project/Scripts/Game/UI/BattleScreen.cs)
- [Standing 리소스 폴더](../Assets/_Project/Resources/Art/Standing)

이번 갱신은 입화 12종과 대화 별칭 연결 후 재조사한 결과를 반영한다. 대사·전투·진행 데이터는 바꾸지 않았다.
