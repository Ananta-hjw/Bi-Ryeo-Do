# 전투 SD 이름·별칭 연결

2026-11-02. ArtLibrary의 SD 전용 정확 일치 목록이다. Standing 이름 연결과 분리한다.

명시한 이름·별호·호칭 319개, 대상 키 151종. 기존 StoryCast와 전체 적 이름으로 이미 연결되던 항목도 함께 명시했다. 모두 새로 누락된 이름이었다는 뜻은 아니다.

## 선택 순서

1. 입력과 이름이 같은 Battle 파일이 있으면 해당 파일을 우선 사용한다.
2. 아래의 정확한 이름 목록에서 SD 키를 찾는다.
3. 없으면 기존 주인공·동료·StoryCast·적의 전체 이름/ID로 찾는다. SD에는 부분 문자열 추측을 사용하지 않는다.
4. 해당 Battle 파일이 없으면 기존 암살자 → 여성 → 남성 기본 SD 규칙을 따른다. Standing 이미지를 전투 SD 대신 쓰지 않는다.

## 인물 구분

- 갈중혁/혁중/신마/무신마는 `hyeok_jung`, 갈중천/흑천맹주는 `gal_jungcheon`, 혁월린/무신은 `hyeok_wolrin`이다.
- 창랑/마랑객은 `changrang`. 창랑대 철갑늑대의 `armored_wolf`와 분리한다.
- 효민·효효 개인 그림과 합주 그림, 단혜·사란 개인 그림과 합격 그림을 구분한다. 천음선자는 홍란, 천음선녀는 갈효민이다.
- 노사부·사부 호칭만으로 조회할 때는 기존 `hwan_sabu`가 기본이다. 실제 스토리 재생은 장면에 명시한 `HeroArtKey`를 우선해 비류연의 노사부 변장(`biry_old_master`)과 실제 사부(`hwan_sabu`)를 구분한다. 단계 번호만으로 모든 대화를 한 인물에 묶지 않는다.
- 윤미는 변장 전용 전투 키 `yunmi`로 연결한다. 전용 `Battle/yunmi.png`를 사용한다. `yun_junho`의 평상시 SD와 Standing의 `yun_mi`를 혼동하지 않는다.
- 사부 관계 일반화·미확정 정체 연결은 하지 않는다. 무명과 위천무를 합치지 않고, 이름만으로 특정할 수 없는 복면인·환영·주군·아버지·노인은 특정 개인 SD에 묶지 않는다.
- 전용 파일이 없는 인물의 기본 SD 분류와 외팔 천살·지살의 그림은 이번 별칭 연결 작업에서 변경하지 않는다.

## 전체 명시 목록

| 이름·별호·호칭 | SD 키 | 파일 또는 현재 대체 |
|---|---|---|
| 비류연 | `player` | `player.png` |
| 효룡 / 을진무쌍검 | `hyo_ryong` | `hyo_ryong.png` |
| 장홍 | `jang_hong` | `jang_hong.png` |
| 윤준호 | `yun_junho` | `yun_junho.png` |
| 나예린 / 빙백봉 / 빙백봉 나예린 | `na_yerin` | `na_yerin.png` |
| 임성진 / 진성곤 | `im_seongjin` | `im_seongjin.png` |
| 남궁상 / 뇌전검룡 | `namgung_sang` | `namgung_sang.png` |
| 모용휘 / 칠절신검 | `mo_yonghwi` | `mo_yonghwi.png` |
| 강장한 / 비풍도 | `gang_janghan` | `gang_janghan.png` |
| 중광 / 패호 | `paeho_jung_gwang` | `paeho_jung_gwang.png` |
| 우악 / 패룡 | `paeryong_wooak` | `paeryong_wooak.png` |
| 장우강 | `jang_ugang` | `jang_ugang.png` |
| 염도 / 곽영희 / 염도 곽영희 | `yeomdo` | `yeomdo.png` |
| 호천상 / 호아장주 | `ho_cheonsang` | `ho_cheonsang.png` |
| 장우양 / 십팔검 | `jang_uyang` | `jang_uyang.png` |
| 관설지 / 청설옥검녀 | `gwan_seolji_story` | `gwan_seolji_story.png` |
| 청흔 / 삼절검 | `cheong_heun` | `cheong_heun.png` |
| 호천강 | `ho_cheongang` | `ho_cheongang.png` |
| 종리학 | `jongri_hak` | `jongri_hak.png` |
| 추가연 | `chu_gayeon` | `chu_gayeon.png` |
| 구창 / 일도파쇄 | `gu_chang` | `gu_chang.png` |
| 소천군 / 청풍검 | `so_cheongun` | `so_cheongun.png` |
| 위지천 / 선풍검룡 | `wiji_cheon` | `wiji_cheon.png` |
| 십이선녀 / 독한녀 | `sunyeo` | `sunyeo.png` |
| 하세인 / 철검비룡 | `ha_sein` | `ha_sein.png` |
| 강현추 / 영취검 | `gang_hyeonchu` | `gang_hyeonchu.png` |
| 단대풍 | `dan_daepung` | `dan_daepung.png` |
| 단평 / 쾌환검 | `dan_pyeong` | `dan_pyeong.png` |
| 언가영 / 파풍창 | `eonga_yeong` | `eonga_yeong.png` |
| 전옥기 / 팔비신검 | `jeon_okgi` | `jeon_okgi.png` |
| 당문천 / 독랄수 | `dang_munceon` | `dang_munceon.png` |
| 갈효봉 / 혈류도 | `gal_hyobong` | `gal_hyobong.png` |
| 천살 / 천살 초혼검 | `cheonsal` | `cheonsal.png` |
| 지살 / 지살 명왕도 | `jisal` | `jisal.png` |
| 구천학 / 질풍묵흔 / 질풍천뢰 | `gu_cheonhak` | `gu_cheonhak.png` |
| 백탄저 / 관살 / 백인회주 | `baek_tanjeo` | `baek_tanjeo.png` |
| 팽혁성 / 광풍맹호도 | `pae_hyeokseong` | `pae_hyeokseong.png` |
| 마하령 / 철옥잠 / 군웅회주 | `ma_haryeong` | `ma_haryeong.png` |
| 용천명 / 창천룡 | `yong_cheonmyeong` | `yong_cheonmyeong.png` |
| 빙검 / 관철수 / 빙검 관철수 | `gwan_cheolsu` | `gwan_cheolsu.png` |
| 임덕성 / 녹림왕 / 녹림 총표파자 | `im_deokseong` | `im_deokseong.png` |
| 사부 / 사부님 / 노사부 / 노사부님 / 비류연의 사부 / 환영 속 사부 | `hwan_sabu` | `hwan_sabu.png` |
| 임개 / 흑랑부 | `im_gae` | `im_gae.png` |
| 혈궁 | `hyeol_gung` | `hyeol_gung.png` |
| 혈쇄 | `hyeol_swae` | `hyeol_swae.png` |
| 모사령 / 비사마군 | `mo_saryeong` | `mo_saryeong.png` |
| 동방학 / 섬뢰마검 | `dongbang_hak` | `dongbang_hak.png` |
| 공패 / 잔뢰마도 | `gong_pae` | `gong_pae.png` |
| 은명 / 대공자 / 대공자 비 | `bi_daegongja` | `bi_daegongja.png` |
| 무정 / 잔무일점혈 / 은형의 무정 | `mu_jeong` | `mu_jeong.png` |
| 교옥 / 혈심란 | `gyo_ok` | `gyo_ok.png` |
| 호유광 / 화평장주 | `ho_yugwang` | `ho_yugwang.png` |
| 종리추 / 칠련창 | `jongri_chu` | `jongri_chu.png` |
| 추명 / 마검익 | `chu_myeong` | `chu_myeong.png` |
| 사마흔 / 천참마검 | `sama_heun` | `sama_heun.png` |
| 검후 / 이옥상 / 검후 이옥상 | `geomhu_ioksang` | `geomhu_ioksang.png` |
| 윤이정 / 풍마도 / 풍마도 윤이정 / 살룡 | `yun_ijeong` | `yun_ijeong.png` |
| 공손절휘 | `gongson_jeolhwi` | `gongson_jeolhwi.png` |
| 이시건 / 마천각 이공자 / 마천각 이공자 이시건 / 흑응비섬의 이시건 / 자운 암풍 이시건 | `i_sigeon` | `i_sigeon.png` |
| 진소령 / 아미신녀 | `jin_soryeong` | `jin_soryeong.png` |
| 혈표 | `hyeol_pyo` | `hyeol_pyo.png` |
| 유란 | `yu_ran` | `yu_ran.png` |
| 유운비 | `yu_unbi` | `yu_unbi.png` |
| 일혈 | `il_hyeol` | `il_hyeol.png` |
| 검존 / 공손일취 / 검존 공손일취 / gongson_ilchwi | `geomjon_gongson` | `geomjon_gongson.png` |
| 옥유경 / 혈옥선자 | `ok_yugyeong` | `ok_yugyeong.png` |
| 백무후 / 하양이 | `baekmu_ju` | `baekmu_ju.png` |
| 흑룡왕 / 해응신 / 흑룡번신 / 흑룡왕 해응신 | `heukryong_wang` | `heukryong_wang.png` |
| 예청 / 빙월선자 / 빙월희 적예 | `ye_cheong` | `ye_cheong.png` |
| 진령 | `jin_ryeong` | `jin_ryeong.png` |
| 독고령 / 독안봉 / 영령 / 몽영령 | `dokgo_ryeong` | `dokgo_ryeong.png` |
| 칠상흔 / 이벽한 / 혈염쌍도 | `chil_sangheun` | `chil_sangheun.png` |
| 락비오 / 서해왕 | `rakbio` | `rakbio.png` |
| 자군 / 동해왕 / 미폭공자 | `jagun` | `jagun.png` |
| 전혼 / 남해왕 | `jeonhon` | `jeonhon.png` |
| 창랑 / 마랑객 / 마랑객 창랑 | `changrang` | `changrang.png` |
| 창랑아 | `changranga` | `changranga.png` |
| 무명 | `mumyeong` | `mumyeong.png` |
| 나일천 / 서천 / 서천멸겁 | `na_ilcheon` | `na_ilcheon.png` |
| 무화 / 구천현녀 | `mu_hwa` | `mu_hwa.png` |
| 단혜 / 홍련선자 / 홍령선자 | `dan_hye` | `dan_hye.png` |
| 사란 / 빙련선자 / 빙령선자 | `sa_ran` | `sa_ran.png` |
| 갈효린 / 효린 | `gal_hyo_rin` | `gal_hyo_rin.png` |
| 갈효진 / 효진 | `gal_hyo_jin` | `gal_hyo_jin.png` |
| 갈효민 / 효민 / 천음선녀 | `gal_hyo_min` | `gal_hyo_min.png` |
| 갈효효 / 효효 / 은창선녀 | `gal_hyo_hyo` | `gal_hyo_hyo.png` |
| 갈효묘 / 효묘 / 표묘선녀 | `gal_hyo_myo` | `gal_hyo_myo.png` |
| 흑견 | `heukgyeon` | `heukgyeon.png` |
| 효민·효효 / 갈효민·갈효효 | `gal_hyo_duet` | `gal_hyo_duet.png` |
| 단혜·사란 | `mothers_fire_ice` | `mothers_fire_ice.png` |
| 혁중 / 갈중혁 / 신마 / 무신마 / 패천도 / 무신마 갈중혁 / 패천도 갈중혁 | `hyeok_jung` | `hyeok_jung.png` |
| 혁월린 / 무신 / 태극신군 / 태극신군 무신 | `hyeok_wolrin` | `hyeok_wolrin.png` |
| 검성 / 모용정천 / 신검협 / 검성 모용정천 | `geom_seong` | `geom_seong.png` |
| 도성 / 하후식 / 도성 하후식 | `do_seong` | `do_seong.png` |
| 갈중천 / 흑천맹주 | `gal_jungcheon` | `gal_jungcheon.png` |
| 신유성 / 취영검 | `shin_yuseong` | `shin_yuseong.png` |
| 은가면 공포 / 은가면 「공포」 | `mask_gongpo` | `mask_gongpo.png` |
| 은가면 절망 / 은가면 「절망」 | `mask_jeolmang` | `mask_jeolmang.png` |
| 은가면 비탄 / 은가면 「비탄」 | `mask_bitan` | `mask_bitan.png` |
| 윤미 | `yunmi` | `yunmi.png` |
| 당철영 / 당삼 | `dang_cheolyeong` | `default_man.png` (전용 없음) |
| 은설란 / 사중화 | `eun_seolran` | `default_man.png` (전용 없음) |
| 홍란 / 천음선자 | `hong_ran` | `default_man.png` (전용 없음) |
| 석류화 / 진홍의 검희 | `seok_ryuhwa` | `default_man.png` (전용 없음) |
| 노학 | `no_hak` | `default_man.png` (전용 없음) |
| 마진가 / 천무학관주 | `ma_jinga` | `default_man.png` (전용 없음) |
| 나백천 / 무림맹주 | `na_baekcheon` | `default_man.png` (전용 없음) |
| 손광 / 쾌영도 | `son_gwang` | `default_man.png` (전용 없음) |
| 갈중하 | `gal_jungha` | `default_man.png` (전용 없음) |
| 남궁산산 | `namgung_sansan` | `default_woman.png` (전용 없음) |
| 류은경 | `ryu_eungyeong` | `default_woman.png` (전용 없음) |
| 두노이 / 구이십안 | `du_noi` | `default_man.png` (전용 없음) |
| 유재룡 | `yu_jaeryong` | `default_man.png` (전용 없음) |
| 유경영 | `yu_gyeongyeong` | `default_man.png` (전용 없음) |
| 유은성 / 낙일검 | `yu_eunseong` | `default_man.png` (전용 없음) |
| 현운 | `hyeon_un` | `default_man.png` (전용 없음) |
| 비연태 | `bi_yeontae` | `default_man.png` (전용 없음) |
| 백무영 / 지룡 / 형산일기 | `baek_muyeong` | `default_man.png` (전용 없음) |
| 추일태 / 일은무영 | `chu_iltae` | `default_man.png` (전용 없음) |
| 문일기 / 화산비천응 | `mun_ilgi` | `default_man.png` (전용 없음) |
| 당평 / 천수탈혼 | `dang_pyeong` | `default_man.png` (전용 없음) |
| 도굴군 / 천기수 | `do_gulgun` | `default_man.png` (전용 없음) |
| 종쾌 / 비공답운 | `jong_kwae` | `default_man.png` (전용 없음) |
| 도광서 / 기환검쾌 | `do_gwangseo` | `default_man.png` (전용 없음) |
| 단목우 / 천기룡 | `danmok_u` | `default_man.png` (전용 없음) |
| 모경 / 폭랑귀도 | `mo_gyeong` | `default_man.png` (전용 없음) |
| 도추운 / 사갈검편 | `do_chuun` | `do_chuun.png` |
| 맹연호 / 청룡단주 / 천수룡 / 옥룡신검 | `maeng_yeonho` | `maeng_yeonho.png` |
| 이송학 / 이송 / 군자소요검 | `lee_songhak` | `lee_songhak.png` |
| 위무상 / 위천상 | `wi_musang` | `wi_musang.png` |
| 늑기한 / 천익검 | `neuk_gihan` | `default_man.png` (전용 없음) |
| 담환 / 담 총관 | `dam_hwan` | `default_man.png` (전용 없음) |
| 용경의 / 도제 / 일도단애 | `yong_gyeongui` | `default_man.png` (전용 없음) |
| 섭운명 / 검치 / 일지번천 | `seop_unmyeong` | `default_man.png` (전용 없음) |
| 공손경운 | `gongson_gyeongun` | `default_man.png` (전용 없음) |
| 돈왕 / 금적신 | `donwang` | `default_man.png` (전용 없음) |
| 마천각주 / 북천멸겁 | `macheon_gakju` | `default_man.png` (전용 없음) |
| 위천무 / 천겁혈신 | `wi_cheonmu` | `default_man.png` (전용 없음) |
| 갈효혜 / 효혜 / 십지선녀 / 여제갈 | `gal_hyo_hye` | `default_man.png` (전용 없음) |
| 불락구척 / 생사무허가 | `bullak_gucheok` | `default_man.png` (전용 없음) |
| 매화가면 | `maehwa_mask_trial` | `maehwa_mask_trial.png` |
| 멸성대 | `mulsung_daewon` | `mulsung_daewon.png` |
| 하얀 손 / 하얀 손 고수 | `baek_son_gosu` | `baek_son_gosu.png` |
| 추항 / 명염도 / 명부오귀장 | `myeongbu_jang` | `myeongbu_jang.png` |
| 초혼오귀검 | `chohon_gwigeom` | `chohon_gwigeom.png` |
| 백교 | `baekgyo` | `baekgyo.png` |
| 화룡 | `hwaryong_hwayeom` | `hwaryong_hwayeom.png` |
| 십이혈마대 | `hyeolma_dae` | `hyeolma_dae.png` |
| 살룡대 | `salryong_daewon` | `salryong_daewon.png` |
| 살귀대 | `salgwi_daewon` | `salgwi_daewon.png` |

| 변장한 비류연 / 비류연(노사부) / 가짜 노사부 | `biry_old_master` | `biry_old_master.png` |

## 근거

기존 EnemyData.Name/ArtKey·StoryCast 및 Story/CHARACTERS.md의 인물·별호, 13~16권의 검성/도성·혁중·은명, 21~27권의 사천왕·칠상흔·창랑·무명, 28~29권의 신마가 인물 분석을 기준으로 한다. 사천왕은 동해왕 자군·서해왕 락비오·남해왕 전혼을 구분하며 이름 미확인 북해왕을 자군으로 매핑하지 않는다. 흑룡왕 본명의 해어광/해중신 혼용은 추가 별칭으로 확정하지 않는다.


## 검증

- 실제 ArtLibrary 소스와 파일 기반 Unity Resources 대역으로 319개 명시 이름을 확인했다. 전용 파일이 있으면 그 파일, 없으면 기존 분류의 기본 SD를 선택했다.
- 변경 전 실행 결과와 대조해 적 300개의 ArtKey·ID·전체 이름 조회 및 동료 7명의 이름·ID 조회가 동일했다. Battle 파일 144개의 파일명 직접 조회도 유지됐다.
- 신마/무신·창랑/철갑늑대·개인/합주·윤준호/윤미·사천왕 및 미확정 공통 호칭을 별도 사례로 검사했다. 실제 대화 화자도 대조해 기존 SD가 있는 명시 인물 호칭의 누락을 확인했다. 공통 명칭 복면인·환영은 제외했다.
- 기존 Standing 조회 29개 사례가 동일하고, Game 61개 파일 C# 9 구문 검사 오류 0건, git diff 공백 검사 오류 0건이다. Unity 실제 렌더링은 확인하지 못했다.


## 같은 호칭을 쓰는 노사부 두 인물

- Standing/Battle에 `biry_old_master.png`를 각각 추가했다. 실제 사부의 `hwan_sabu` 자산·GUID는 유지한다.
- `StoryScene.HeroArtKey`, `StageData.IntroHeroArtKey/OutroHeroArtKey`에 변장 장면을 명시한다. 합숙 소개·백호 뒤의 대화·표국 습격/지원금/이별·초혼오귀검 저지에서는 변장한 비류연을 보여준다. 이름표와 원래 대사는 보존하며 그 장면에서만 사부 호칭의 발화자를 주인공 쪽으로 처리한다.
- 실제 사부의 초반 모옥 회상과 18권 이후의 등장·백무후·비뢰도 회수 장면에는 변장 외형을 지정하지 않는다. 원작의 두 인물 정체를 합치지 않는다.
- 전투 SD는 1단계 백호전·2단계 표국전에서만 변장 외형을 사용한다. 그 전의 산짐승전은 평상시 비류연, 이후 표행은 제자들의 전투다. 의뢰·무한 비무대에는 이 변장 규칙을 적용하지 않는다. 변장 상태를 벗어나면 임시 외형 키를 지운다.

검증: 추가한 노사부 구분·장면 메타데이터·외형 전환 회귀 6건이 통과했다. 전체 Core NUnit은 1,457 통과·12 실패였고, 변경 전 HEAD `1e0eb1e`에서도 같은 12개 밸런스 검사가 동일한 값으로 실패했다. 기존 적 300개의 키/ID/전체 이름과 동료 7명의 SD 조회는 동일하며, Battle 144개 파일·319개 이름 연결·Standing 179개 파일 검사와 Game C# 9 구문 검사도 통과했다. 실제 Unity 렌더링은 확인하지 못했다.

### 전투 등장 인물 7종 보완 — 2026-11-02

모용휘·남궁상·맹연호·이송학·위무상·도추운·윤미에 전용 SD를 추가했다. 도추운의 야간 적 `do_chuun_night`도 `ArtId = "do_chuun"`으로 같은 인물의 그림을 사용한다. 윤미는 전투 키 `yunmi`와 입화 키 `yun_mi`를 유지하며, 44장 변장 전투 외의 윤준호는 기존 `yun_junho` 그림을 사용한다.
