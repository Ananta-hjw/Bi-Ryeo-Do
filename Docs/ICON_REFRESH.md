# 객잔·신규 무공 아이콘 정비

잠긴 상품은 원본 RGB를 같은 배율로 어둡게 하던 방식을 고쳤다. 원본이 밝은 무기와 어두운 설삼의 차이, 자식 그림의 원색 유지가 겹쳤다.
`IconCutout`의 잠금용 머티리얼은 RGB를 같은 회녹색 잉크 값으로 바꾸고 투명도·윤곽·스크롤 클립을 유지한다. 목록·상세, 잠김·매진에 같은 처리를 쓴다. 은자 부족만으로 열린 상품 그림을 숨기지는 않는다.
이미지뿐 아니라 원색을 유지하는 WuxiaGlyph와 자식 벡터/글자도 처리하고, 원래 투명한 부모 이미지를 불투명 사각형으로 바꾸지 않는다. 검은 배경판을 추가하지 않는다.

동일한 인형설삼·설삼·영단을 여러 세력이 파는 경우는 같은 아이템이다. 그림은 같은 것을 쓰며 목록 모서리의 학/표/무/림/화 표식으로 선반을 구분한다.
무당 수련10/25/60은 고리1/2/3개와 연꽃으로, 경험치20/50은 얇은 녹색 일지/금빛 펼친 일지로 구분했다. 무공 수련서 소/중/대도 파랑 단권·보라 묶음·금빛 인장 묶음으로 나눴다.
수련·경험치5개 서비스는 catalog의 `Shop` 범주, 수련서3개는 개별 `Item` 매핑이다. 거래·평판·가격·효과는 변경하지 않는다.

기존 제작분을 다시 정리해 진행·조사용 전용 도안20종을 배포 이미지에서 제거했다. 남은 그림은 실제 전투에 쓰이는72종과 객잔8종, 총80종이다.
진행 명령25개는 기존 공통 아이콘을 재사용하는 `Command` 범주로 옮겼다. 기본 공격과 보고 기록처럼 기존 그림을 재사용하던 명령도 포함한다.
실제 전투용 아이콘에는 현재 조작 가능한 이야기 전투의 기술도 포함된다. 일반 습득·장착 무공과 일회성 이야기 명령을 같은 목록으로 안내하지 않는다.
전투 규칙·스킬 이름/효과·목표·스토리·가격은 변경하지 않았다. 명령 버튼과 길게 누르기에는 `ArtLibrary.Command`를 사용해 공통 명령 그림과 무공 그림을 구별한다.
새 그림을 추가 제작하지 않고 기존 PNG에서 유지할 영역만 원본 픽셀 그대로 추출·재배치했다. 폐기한 진행용 그림은 최종 시트에 포함하지 않는다.
최종 파일은 `active_icons_01`~`active_icons_05`(4×4)와 `story_perception`(2×2)다. 남는 칸은 완전 투명이며 도안이 없다.
최초 이미지 원본은 작업 공간의 `generated_images`에 보관하며 배포용 Resources에서는 제거했다.
투명 PNG의 `transparent` 표시는 기존 청록 캔버스 제거를 건너뛰게 한다. 잠금·매진의 공통 잉크 처리와 객잔 상품 구분은 유지한다.

## 제작 범위

앞으로의 무공 아이콘 제작은 현재 플레이 가능한 습득·장착·전투 명령에서 사용을 확인한 무공에 한한다. 분석 후보나 향후 계획만 있는 무공은 제외한다.
아래 기존 연결표에는 조사·승선·안전칸 선택 같은 이야기 진행 명령도 포함되어 있다. 이 항목들은 일반 무공과 구분한다.

## 현재 전투용 연결표

현재 쓰이는 이야기 전투 기술을 포함한다. cell은0부터 시작하는 행 우선 순서다.

| ID | 이름 | 게임 효과 | 시트 / cell |
|---|---|---|---|
|ny_biseolbo|비설보|회피·대응|active_icons_01 / 11|
|story_strike|청룡검 연격|타격 · 2연격|active_icons_04 / 9|
|story_sweep|청룡검 횡격|타격 · 전체|active_icons_04 / 9|
|story_footwork|틈새 보법|회피·대응|active_icons_01 / 12|
|story_hide|몸을 숨긴다|회피·대응|story_perception / 1|
|scene_palhwa|팔화난무|타격 · 4연격|active_icons_02 / 10|
|scene_ssangseom|충각 쌍섬일격뢰|타격|active_icons_02 / 11|
|scene_taesanbong|붕결 일도광 태산봉|타격|active_icons_02 / 12|
|scene_pyoru|표류무상도법|타격|active_icons_02 / 13|
|scene_umbrella|현천은린 · 타격|타격 · 2연격|active_icons_04 / 13|
|scene_sanmak|산막|동료 보호|active_icons_04 / 14|
|scene_sambok|삼복구타산|타격 · 4연격|active_icons_04 / 13|
|scene_silver_snow|새 검기|타격 · 전체|skills / 29|
|scene_staff|삼복구타봉법|타격 · 2연격|active_icons_02 / 14|
|scene_shell_deflect|벽력포 흘리기|회피·대응|active_icons_05 / 2|
|scene_shell_cut|벽력탄 가르기|타격|active_icons_05 / 3|
|scene_muzzle_stop|포구 막기|타격|active_icons_05 / 4|
|scene_left_umbrella|현천은린 회천|타격 · 2연격|active_icons_04 / 15|
|scene_left_evade|왼손 우산막|회피·대응|active_icons_05 / 0|
|scene_redirect|기류를 땅으로 돌린다|회피·대응|active_icons_05 / 5|
|scene_umbrella_spear|천지무견 자다봉창|타격|active_icons_05 / 1|
|scene_suyak|수약답운|회피·대응|active_icons_05 / 7|
|scene_seonpung|선풍검계|타격|active_icons_05 / 6|
|scene_wind_sword|풍령검|타격 · 2연격|active_icons_05 / 6|
|scene_geumgang|금강복마지|타격|active_icons_05 / 8|
|scene_budong|부동명왕보|회피·대응|active_icons_05 / 9|
|scene_tanji|탄지신통|타격|active_icons_05 / 10|
|scene_chilmae|칠매검|타격 · 2연격|active_icons_05 / 11|
|scene_yunmi_step|매화의 보법|회피·대응|active_icons_05 / 11|
|scene_roegwang|뇌광단천|타격 · 2연격|active_icons_03 / 13|
|scene_roegwang_step|뇌광산란|회피·대응|active_icons_03 / 14|
|scene_soan|소안검|타격|active_icons_03 / 15|
|scene_ny_step|비설보|회피·대응|active_icons_01 / 11|
|scene_hanbing|한빙섬옥|타격 · 2연격|active_icons_04 / 0|
|scene_haesang|해상비조천참절|타격|active_icons_04 / 1|
|scene_mukroesal|무명검 · 묵뢰살|타격|active_icons_04 / 2|
|scene_search_fist|삼복구타권법|타격 · 2연격|active_icons_02 / 15|
|scene_search_step|퇴로 살피기|회피·대응|active_icons_01 / 12|
|scene_loaned_darts|빌린 비뢰도|타격 · 5연격|active_icons_03 / 6|
|scene_jungbok|삼복구타권법 중복|타격 · 2연격|active_icons_02 / 15|
|scene_half_step|봉황무의 틈|회피·대응|active_icons_01 / 12|
|scene_slow_fist|느린 접촉|타격|active_icons_03 / 0|
|scene_second_fist|반응을 확인하는 두 번째 타격|타격|active_icons_03 / 0|
|scene_probe_step|운균 앞의 호흡|회피·대응|skills / 53|
|scene_rescue_fist|삼복구타권법 중복|타격 · 2연격|active_icons_02 / 15|
|scene_rescue_step|봉황무|회피·대응|active_icons_01 / 12|
|scene_air_push|격공장|타격|active_icons_03 / 1|
|scene_needle_flash|뇌섬일시|타격|active_icons_03 / 2|
|scene_rain_step|우중거 불점의|회피·대응|active_icons_01 / 13|
|scene_mind_wave|심뢰열파|회피·대응|active_icons_03 / 4|
|scene_eight_shadows|팔섬 뇌광굉타|타격 · 8연격|active_icons_03 / 3|
|scene_star_sword|은하일섬|타격|active_icons_04 / 3|
|scene_star_rain|은하유성만천|타격 · 2연격|active_icons_04 / 4|
|scene_star_step|유성보|회피·대응|active_icons_01 / 14|
|scene_sense_heat|눈을 감고 열기를 읽는다|회피·대응|story_perception / 2|
|scene_ice_union|염천빙지 일원합|타격|active_icons_04 / 6|
|scene_thunder_scar|뇌전참흔|타격 · 2연격|active_icons_03 / 13|
|scene_thunder_shadow|뇌광참영|타격|skills / 15|
|scene_sansan_step|전뢰보|회피·대응|active_icons_03 / 14|
|scene_taicheong|태청강기사 청천우|타격 · 2연격|active_icons_04 / 8|
|scene_tao_step|제운종|회피·대응|active_icons_01 / 15|
|scene_coin_parry|칠연비성격 쳐내기|회피·대응|active_icons_02 / 0|
|scene_bend_wind|풍우만곡|회피·대응|active_icons_02 / 0|
|scene_blood_blade|혈풍란|타격 · 2연격|active_icons_04 / 7|
|scene_ok_step|추격의 틈|회피·대응|active_icons_01 / 12|
|scene_flying_twin|비조쌍월인|타격 · 2연격|skills / 18|
|scene_empty_nest|공저물사 · 포룡귀원|회피·대응|active_icons_03 / 10|
|scene_golden_thread|두 손가락과 뇌령사|타격|active_icons_03 / 11|
|scene_gongson_sword|지존검|타격 · 2연격|active_icons_04 / 9|
|scene_gongson_step|지존군림보|회피·대응|active_icons_02 / 3|
|scene_wind_wall|풍우만곡 · 풍인벽|동료 보호|active_icons_02 / 1|
|scene_gongson_support|지존군림보 · 지존편재|동료 보호|active_icons_02 / 2|
|scene_stars_end|은하멸멸 · 섬|타격|active_icons_04 / 5|
|scene_wolf_stop|창랑아 저지|타격|skills / 1|
|scene_escape_sword|해상비조천참절|타격|active_icons_04 / 1|
|scene_return_yerin|순신 · 뇌령사|회피·대응|active_icons_01 / 10|
|scene_fist_clash|첫 수 · 주먹으로 맞선다|타격|active_icons_02 / 15|
|scene_roegwang_response|셋째 수 · 뇌광류하곡|타격|skills / 6|
|scene_injured_sword|뇌광단천|타격 · 2연격|active_icons_03 / 13|
|scene_namgung_step|퇴로를 살핀다|회피·대응|active_icons_01 / 12|
|scene_dang_darts|당문 암기|타격 · 3연격|skills / 4|
|scene_baek_sword|형산의 검|타격 · 2연격|active_icons_04 / 9|
|scene_baek_step|바람 읽기|회피·대응|story_perception / 3|
|scene_five_sweep|다섯 비뢰도 · 제압|타격 · 전체|active_icons_03 / 6|
|scene_silver_whirl|질풍 용권인|타격|active_icons_03 / 7|
|scene_soul_scar|심뢰 극 천형인|타격|active_icons_03 / 5|
|scene_umbrella_boat|봉익일우|회피·대응|active_icons_03 / 8|
|scene_water_funeral|편수 풍신 · 수룡의 번 승천|타격 · 5연격|active_icons_03 / 9|
|scene_escape_yerin_sword|한상옥령신검|타격 · 2연격|skills / 29|
|scene_escape_yerin_step|비설보|회피·대응|active_icons_01 / 11|
|scene_escape_fist|삼복구타권법|타격 · 2연격|active_icons_02 / 15|
|scene_escape_sweep|비뢰도 · 제압|타격 · 전체|active_icons_03 / 6|
|scene_escape_step|봉황무|회피·대응|active_icons_01 / 12|
|scene_escape_gray|잿빛 검강|타격 · 2연격|active_icons_04 / 9|
|scene_escape_shadow|신은술|회피·대응|story_perception / 1|
|scene_escape_ice|빙령수류검|타격 · 2연격|active_icons_04 / 0|
|scene_escape_snow_step|설풍보|회피·대응|skills / 3|
|scene_escape_galaxy|은하일섬|타격 · 2연격|active_icons_04 / 3|
|scene_escape_star_step|유성보|회피·대응|active_icons_01 / 14|
|scene_escape_thunder|뇌전검|타격 · 2연격|active_icons_03 / 13|
|scene_escape_namgung_step|뇌광산란|회피·대응|active_icons_03 / 14|
|scene_escape_flame|진홍십칠염|타격 · 2연격|active_icons_04 / 11|
|scene_escape_fire_step|홍련지벽|회피·대응|active_icons_02 / 4|
|scene_escape_fire_wall|홍련지벽 · 맡은 공세 대응|회피·대응|active_icons_02 / 4|
|scene_escape_ice_guard|빙령지순 · 맡은 공세 대응|회피·대응|active_icons_02 / 5|
|scene_escape_save_fire|설풍보 · 염도 구원|타격|active_icons_01 / 11|
|scene_escape_save_ice|비도 화륜참 · 빙검 구원|타격|active_icons_04 / 7|
|scene_escape_cut_sound|뇌절단음|타격|active_icons_02 / 6|
|scene_escape_silver_net|천라은망지세 뇌망옥|회피·대응|active_icons_02 / 7|
|scene_escape_returned_fist|돌아온 말복 삼년구타|타격|active_icons_02 / 15|
|scene_escape_gate_thunder|뇌궁섬뢰|타격|active_icons_03 / 13|
|scene_escape_gate_gray|잿빛 검강 · 연환|타격|active_icons_04 / 9|
|scene_escape_gate_galaxy|은하만상|타격|active_icons_04 / 4|
|scene_escape_shadow_bind|차음막 · 영사삭 묵영포박|회피·대응|active_icons_02 / 7|
|scene_escape_dog_capture|변장 뒤의 몽둥이|타격|active_icons_01 / 9|
|scene_escape_baek_sword|형산의 검|타격 · 2연격|active_icons_04 / 9|
|scene_escape_baek_step|흐름 읽기|회피·대응|story_perception / 3|
|scene_escape_flute_feint|옥소로 시선을 끈다|회피·대응|active_icons_01 / 8|
|scene_escape_cheong_sword|삼절검|타격 · 2연격|active_icons_04 / 9|
|scene_escape_cheong_step|태극의 보법|회피·대응|active_icons_01 / 15|
|scene_escape_yong_finger|금강복마지|타격 · 2연격|active_icons_05 / 8|
|scene_escape_yong_step|부동명왕보|회피·대응|active_icons_05 / 9|
|scene_escape_ma_blade|표류무상도법|타격 · 2연격|active_icons_02 / 13|
|scene_escape_ma_step|유가신공|회피·대응|active_icons_01 / 12|
|scene_escape_three_swords|삼검교연 삼검귀일|타격|active_icons_04 / 10|
|scene_escape_triple_roar|삼존불영삼재 · 삼중 사자후|타격|story_perception / 0|
|scene_escape_earth_wave|지진각 · 대지의 파도|타격|active_icons_02 / 8|
|scene_escape_spear_read|용안 · 비설보|회피·대응|active_icons_01 / 11|
|scene_escape_storm_wings|폭풍비상 비홍관운|타격|active_icons_02 / 9|
|scene_escape_yeong_sword|몸이 기억하는 검로|타격 · 2연격|active_icons_04 / 9|
|scene_escape_yeong_step|틈을 잡는 보법|회피·대응|active_icons_01 / 12|
|scene_escape_distrust|우민군무 불신지옥|회피·대응|active_icons_03 / 12|
|scene_escape_clear_elements|풍신 질풍|타격|active_icons_03 / 7|
|scene_escape_free_father|뇌령사 · 나백천 구출|타격|active_icons_01 / 10|
|scene_escape_fire_dragon|화룡난류|타격|active_icons_04 / 11|
|scene_escape_twin_dragons|뇌화열풍인 쌍룡귀천|타격|active_icons_04 / 12|

## 공통 아이콘으로 전환한 진행 명령

이 항목을 위한 새로운 전용 그림은 제작하지 않는다.

| ID | 이름 | 게임 효과 | 공통 시트 / cell |
|---|---|---|---|
|basic|기본 공격|타격|skills / 51|
|scene_young_evade|비늘의 틈 읽기|회피·대응|menus / 2|
|scene_release_flow|힘의 흐름을 잇는다|회피·대응|skills / 53|
|scene_confirm_charm|남겨진 뿔 장식 확인|회피·대응|items_4 / 33|
|scene_trace|흔적분으로 경로 살피기|회피·대응|items_4 / 33|
|scene_safe_27|27번 · 왼쪽 안전칸|회피·대응|skills / 52|
|scene_fall_37|37번 · 가운데 낙하칸|회피·대응|skills / 52|
|scene_fall_47|47번 · 오른쪽 낙하칸|회피·대응|skills / 52|
|scene_read_needles|용안 · 기맥 관조|회피·대응|menus / 2|
|scene_release_needles|서른여섯 금침을 밀어낸다|회피·대응|items_4 / 54|
|scene_open_door|관찰 · 유인 · 검 회수|회피·대응|items_4 / 37|
|scene_dang_smoke|안락휴 준비|회피·대응|skills / 54|
|scene_sleep_fan|안락휴를 부채질한다|회피·대응|skills / 54|
|scene_escape_seat|배에 오른다|회피·대응|items_4 / 37|
|scene_escape_badge|연화검패를 내민다|회피·대응|menus / 1|
|scene_escape_cover|표사의 예로 답한다|회피·대응|skills / 58|
|scene_escape_contacts|거점의 흔적을 읽는다|회피·대응|items_4 / 33|
|scene_escape_report|상현의 보고를 기록한다|회피·대응|items_4 / 46|
|scene_escape_warning|조용히 일행을 깨운다|회피·대응|skills / 58|
|scene_escape_laughter|웃음으로 긴장을 감춘다|회피·대응|skills / 58|
|scene_escape_hold_sword|검을 놓지 않는 의지|회피·대응|skills / 52|
|scene_escape_sister_smoke|향신료 연막 · 예린과 이탈|회피·대응|skills / 54|
|scene_escape_sect_threads|뇌령사를 세 문파에 건다|회피·대응|items_4 / 33|
|scene_escape_red_flower|암시 봉인 · 붉은 남천|회피·대응|items_4 / 33|
|scene_escape_current|급류 아래로 선행 진입|회피·대응|items_4 / 37|

## 검증

- 유지한 도안80종 모두 실제 catalog 참조가 있으며, 폐기한 진행용20종과 시트 참조가 남지 않았는지 검사했다.
- 실제 스킬/명령192개는 Art 또는 Command로 모두 연결되며 명령25개를 일반 무공과 분리했다.
- 새 시트6장에 유효한 알파와 Unity 메타가 있고, 각 사용 영역은 비어 있지 않다. 남는 칸은 완전 투명이다.
- 객잔 거래·잠김/매진 처리·수련서/서비스별 그림은 유지한다. Unity 에디터가 없어 실제 셰이더 컴파일·렌더링·터치는 검증하지 못했다.
