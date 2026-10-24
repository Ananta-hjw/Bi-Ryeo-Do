# 아트 제작 가이드 (AI 이미지 생성)

게임은 `Assets/_Project/Resources/Art` 아래에 **파일 이름이 맞는 이미지**를 넣으면 자동으로 적용한다. 파일이 없으면 지금처럼 색 블록과 큰 글자로 표시되므로 하나씩 채워 넣어도 된다. 코드는 건드릴 필요가 없다.

## 폴더와 파일 이름

| 위치 | 파일 이름 | 규격 |
|---|---|---|
| `Resources/Art/Backgrounds/` | `stage_01.png` … `stage_31.png` | 세로 1080×1920 (또는 9:16), 전투 화면 배경 |
| `Resources/Art/Characters/` | 아래 표의 id (예: `tiger.png`) | **투명 배경 PNG**, 512~1024px 정사각 |
| `Resources/Art/Characters/` | `player.png` | 투명 배경 PNG, 512px 정사각 (상반신 권장) |

- PNG/JPG 모두 된다. 배경은 JPG로 해도 된다(이름만 `.jpg`).
- 파일 크기가 클수록 앱 용량과 메모리가 늘어난다. 적은 1024px 이하, 배경은 1080×1920 정도가 적당하다.
- 이미지를 넣은 뒤 Unity에서 별도 설정은 필요 없다(런타임에 스프라이트로 변환한다). GitHub에 올리면 CI가 APK에 포함해 빌드한다.

## 화풍을 통일하는 법
모든 프롬프트 맨 앞에 같은 **공통 문구**를 붙이면 그림 스타일이 맞는다.

```
ink wash painting style, sumi-e, muted earth tones, soft brush strokes, wuxia, traditional Chinese, high quality
```

- 캐릭터는 `plain white background`를 같이 쓰면 배경 제거가 쉽다. 배경 제거는 remove.bg나 이미지 도구의 배경 제거 기능을 쓰면 된다.
- 화면 비율은 배경 `9:16`, 캐릭터 `1:1`로 지정한다.
- 마음에 드는 첫 결과가 나오면 같은 모델·같은 설정으로 이어서 뽑아야 통일감이 유지된다.
- 원작 인물의 얼굴은 생성할 때마다 달라진다. 외형 묘사는 원작에 나온 특징(머리색, 옷, 무기)만 적었고 얼굴은 정하지 않았다. 마음에 드는 결과를 고르면 된다.

## 주인공 (`player.png`)
원작에서 확인된 특징: 자칭 우아한 미소년, 천재, 자신만만. 사부에게서 받은 묵룡환(검은 쇠고리 족쇄)과 묵금(검은 칠현금)을 지님.

```
ink wash painting style, sumi-e, muted earth tones, soft brush strokes, wuxia, traditional Chinese, high quality, a handsome young martial artist around 19 years old, confident smile, simple dark traditional robe, black iron rings on his wrists, a black guqin on his back, upper body, plain white background
```

## 전투용 SD 캐릭터 (`Resources/Art/Battle/<id>.png`)
전투 화면에서만 쓰는 작은 SD(2등신) 그림이다. 파일이 있으면 전투가 이 그림을 쓰고, 없으면 아래의 기본 그림(`default_assassin`/`default_man`/`default_woman`)을, 그것도 없으면 글자 블록을 그린다. 대화·상태창·로비·도감은 계속 큰 그림을 쓴다.
- 이름: 주인공 `player.png`, 동료는 동료 id(`hyo_ryong.png` 등), 적은 `ArtKey`(`Characters/` 그림 이름과 같다).
- 형식: 배경이 투명한 PNG. 인물마다 캔버스 크기를 같게(권장 정사각 384x384 또는 가로 480x세로 384), 발끝을 하단 중앙에 둔다. 무기가 옆으로 나와도 캔버스 안에 들어가야 한다.
- 방향: **모두 오른쪽을 보게** 그린다. 적은 코드가 좌우로 뒤집어 왼쪽을 보게 한다.
- 크기: 비율을 유지한 채 전투 칸의 그림 영역(칸 폭의 96%, 칸 높이의 60%)에 맞춰 들어간다.
- 기본 그림: 이 폴더에 `default_assassin.png`(산적·자객·살수 계열: 호골채·용골채·흑랑채 산적, 백인회 자객, 쌍귀, 초혼오귀검, 천살·지살, 천지쌍살, 살귀대원, 무정, 복면인, 하얀 손의 고수 — `Core/Genders.cs`), `default_man.png`, `default_woman.png`를 넣으면, 전투 화면(칸 위의 인물과 전투 중 설명 창의 그림)에서 `Battle/<id>.png`이 없는 인물이 이 그림으로 나온다(적은 좌우 반전). `Characters/`의 초상화는 전투 화면에 쓰지 않는다(대화·로비·도감 같은 다른 화면은 그대로 `Characters/`를 쓴다). 여자는 이야기 분석에서 여자로 나오는 인물(나예린, 검후 이옥상, 독한녀 십이선녀, 철옥잠 마하령, 은가면 여인 셋)과 이름이 여자 같은 인물(추가연, 언가영, 사교검 백사영) — `Core/Genders.cs`이고, 암살자 계열이 아닌 나머지와 짐승·기관은 모두 `default_man`이다(암살자 계열은 성별과 상관없이 `default_assassin`이 먼저다). 기본 그림 파일이 없으면 예전처럼 이름 첫 글자 블록이 나온다.
- 애니메이션 프레임은 없고, 돌진·흔들림·쓰러짐은 코드가 정지 그림에 입힌다.

## 동료 (`Characters/<id>.png`)
동료 초상은 파티 패널 배경에 깔린다. 얼굴보다 상반신 구도를 권장한다. 원작에서 확인된 특징만 적었다.

| 파일 | 인물 | 프롬프트 (공통 문구 뒤에 붙이기) |
|---|---|---|
| `hyo_ryong.png` | 효룡 | a calm young swordsman holding twin swords, upper body, plain white background |
| `im_seongjin.png` | 임성진 | a sturdy young staff fighter with a thick build, holding a long wooden staff, wuxia character, upper body, plain white background, ink-wash style |
| `na_yerin.png` | 나예린 | a cold, elegant young swordswoman with long black hair and pale blue eyes, holding a slender sword, white-blue hanbok-style robe, wuxia heroine, upper body, plain white background, ink-wash style |
| `chohon_gwigeom.png` | 초혼귀검 산적 | wuxia character, upper body, plain white background, ink-wash style |
| `myeongbu_jang.png` | 명부장 산적 | wuxia character, upper body, plain white background, ink-wash style |
| `gal_hyobong.png` | 갈효봉(폭주) | wuxia character, upper body, plain white background, ink-wash style |
| `cheonsal.png` | 천살 | wuxia character, upper body, plain white background, ink-wash style |
| `jisal.png` | 지살 | wuxia character, upper body, plain white background, ink-wash style |
| `sadae_jilpung.png` | 사대질풍 철기병 | wuxia character, upper body, plain white background, ink-wash style |
| `cheolbi_gyeok.png` | 철비십이격 창기병 | wuxia character, upper body, plain white background, ink-wash style |
| `wi_musang.png` | 위무상 | wuxia character, upper body, plain white background, ink-wash style |
| `gu_cheonhak.png` | 구천학 | wuxia character, upper body, plain white background, ink-wash style |
| `baegin_jagaek.png` | 백인회 자객 | wuxia character, upper body, plain white background, ink-wash style |
| `ssanggwi.png` | 쌍귀 | wuxia character, upper body, plain white background, ink-wash style |
| `baek_tanjeo.png` | 백탄저 | wuxia character, upper body, plain white background, ink-wash style |
| `sinung_daewon.png` | 신응대원 | wuxia character, upper body, plain white background, ink-wash style |
| `pae_hyeokseong.png` | 패혁성 | wuxia character, upper body, plain white background, ink-wash style |
| `ma_haryeong.png` | 마하령 | wuxia character, upper body, plain white background, ink-wash style |
| `yong_cheonmyeong.png` | 용천명 | wuxia character, upper body, plain white background, ink-wash style |
| `gwan_cheolsu.png` | 관철수 | wuxia character, upper body, plain white background, ink-wash style |
| `hwan_ying.png` | 환마동 환영 | shadowy phantom figure, wuxia character, upper body, plain white background, ink-wash style |
| `hwan_sukbu.png` | 환영 속 광풍장 | sinister uncle phantom, wuxia character, upper body, plain white background, ink-wash style |
| `hwan_sabu.png` | 환영 속 사부 | white-haired old master phantom with silver beard, wuxia character, upper body, plain white background, ink-wash style |
| `cave_viper.png` | 동굴 독충 | venomous cave serpent, no human, wuxia character, upper body, plain white background, ink-wash style |
| `mukrin_hyeolmang.png` | 묵린혈망 | giant black-scaled ancient serpent, wuxia character, upper body, plain white background, ink-wash style |
| `heukrang_bandit.png` | 흑랑채 산적 | axe bandit, wuxia character, upper body, plain white background, ink-wash style |
| `lee_songhak.png` | 이송학 | gentleman swordsman, wuxia character, upper body, plain white background, ink-wash style |
| `im_gae.png` | 임개 | burly bandit chief with twin axes, wuxia character, upper body, plain white background, ink-wash style |
| `hyeolma_dae.png` | 십이혈마대 대원 | masked red-clad assassin, wuxia character, upper body, plain white background, ink-wash style |
| `hyeol_gung.png` | 혈궁 | masked red archer, wuxia character, upper body, plain white background, ink-wash style |
| `hyeol_swae.png` | 혈쇄 | masked chain assassin, wuxia character, upper body, plain white background, ink-wash style |
| `bisa_snake.png` | 비사진의 독사 떼 | swarm of snakes, wuxia character, upper body, plain white background, ink-wash style |
| `cheonghong_ssanggaksa.png` | 청홍쌍각사 | blue and red horned snakes, wuxia character, upper body, plain white background, ink-wash style |
| `mo_saryeong.png` | 모사령 | green-robed old snake master, wuxia character, upper body, plain white background, ink-wash style |
| `dongbang_hak.png` | 동방학 | blue-robed thin old swordsman, wuxia character, upper body, plain white background, ink-wash style |
| `gong_pae.png` | 공패 | grey-robed fat old saber master, wuxia character, upper body, plain white background, ink-wash style |
| `hwapyeong_soldier.png` | 화평장 훈련병 | black-clad trained guards with swords, wuxia character, upper body, plain white background, ink-wash style |
| `ssangsal_hapgyeok.png` | 천지쌍살 | thin one-armed swordsman and fat one-armed saber master standing back to back, wuxia character, upper body, plain white background, ink-wash style |
| `baek_son_gosu.png` | 하얀 손의 고수 | shadowy figure in the dark with pale glowing hands, wuxia character, upper body, plain white background, ink-wash style |
| `salgwi_daewon.png` | 살귀대원 | black-clad pursuit assassin, wuxia character, upper body, plain white background, ink-wash style |
| `ho_yugwang.png` | 호유광 | middle-aged manor lord in dark robes, wuxia character, upper body, plain white background, ink-wash style |
| `jongri_chu.png` | 종리추 | spearman with seven spears on his back, wuxia character, upper body, plain white background, ink-wash style |
| `do_chuun.png` | 도추운 | swordsman with a segmented whip-sword, wuxia character, upper body, plain white background, ink-wash style |
| `oh_munchu.png` | 오문추 | tall rough man with a chain sickle, wuxia character, upper body, plain white background, ink-wash style |
| `baek_sayeong.png` | 백사영 | lean swordsman with a snake-like curved sword, wuxia character, upper body, plain white background, ink-wash style |
| `chu_myeong.png` | 추명 | stern leader in black with wing-like blades, wuxia character, upper body, plain white background, ink-wash style |
| `mulsung_daewon.png` | 멸성대 대원 | faceless black-clad assassin, wuxia character, upper body, plain white background, ink-wash style |
| `i_gae_bujo.png` | 이개 | grim vice-captain in black, wuxia character, upper body, plain white background, ink-wash style |
| `sama_heun.png` | 사마흔 | masked swordmaster in black with a long sword, wuxia character, upper body, plain white background, ink-wash style |
| `suda_bokmyeon.png` | 수다쟁이 복면인 | masked man in dark clothes with a theatrical pose, wuxia character, upper body, plain white background, ink-wash style |
| `mu_jeong.png` | 무정 | shadow assassin emerging from darkness, wuxia character, upper body, plain white background, ink-wash style |
| `ma_chilgeol_jeoji.png` | 마천칠걸 저지조 | black-clad guards blocking a canyon exit, wuxia character, upper body, plain white background, ink-wash style |
| `hwaryong_hwayeom.png` | 화룡의 불꽃 | a dragon made of flames coiling through a valley, ink-wash style, upper body, plain white background, ink-wash style |
| `bi_daegongja.png` | 대공자 비 | cold young man in black robes with a long sword, silver glints, wuxia character, upper body, plain white background, ink-wash style |
| `geomhu_ioksang.png` | 검후 이옥상 | elegant ageless swordswoman in white with a silk sash, wuxia character, upper body, plain white background, ink-wash style |
| `heuksaek_amhyeok.png` | 검은 암벽 | a glossy black rock wall, upper body, plain white background, ink-wash style |
| `wiji_hyeolpok.png` | 혈폭환 위지천 | pale-haired young swordsman with bulging red veins, wuxia character, upper body, plain white background, ink-wash style |
| `mask_gongpo.png` | 은가면 공포 | white-haired old swordsman in a half silver mask, wuxia character, upper body, plain white background, ink-wash style |
| `mask_jeolmang.png` | 은가면 절망 | short stocky old man in a half silver mask, wuxia character, upper body, plain white background, ink-wash style |
| `mask_bitan.png` | 은가면 비탄 | graceful woman in red with a half silver mask holding a twig, wuxia character, upper body, plain white background, ink-wash style |
| `jongri_chu_night.png` | 종리추(야간) | spearman with seven spears on his back in the dark, wuxia character, upper body, plain white background, ink-wash style |
| `do_chuun_night.png` | 도추운(야간) | swordsman with a segmented whip-sword in the dark, wuxia character, upper body, plain white background, ink-wash style |
| `jang_hong.png` | 장홍 | a bearded man who looks older than his age, kind but nagging expression, sword at his waist, upper body, plain white background |
| `yun_junho.png` | 윤준호 | a timid young swordsman of a mountain sect with plum blossom emblem, white and purple robe, upper body, plain white background |

## 배경 (스테이지 9개)

| 파일 | 스테이지 | 프롬프트 (공통 문구 뒤에 붙이기) |
|---|---|---|
| `stage_01.png` | 아미산 숲 | misty pine forest on a steep mountain slope, a small wooden hut in the distance, morning fog, vertical composition, no characters, 9:16 |
| `stage_02.png` | 중양표국 | courtyard gate of a Chinese escort agency compound, wooden gate with a banner flag, stone paving, vertical composition, no characters, 9:16 |
| `stage_03.png` | 호골채 매복 | narrow mountain road between steep cliffs, ambush spot, dusk light, vertical composition, no characters, 9:16 |
| `stage_04.png` | 용골채 매복 | wide mountain pass with rocky cliffs and scattered boulders, tense atmosphere, overcast sky, vertical composition, no characters, 9:16 |
| `stage_05.png` | 주주 윤하루 | traditional Chinese inn exterior at night, red lanterns, wooden two-story building, quiet street, vertical composition, no characters, 9:16 |
| `stage_06.png` | 남창 호아장 | martial arts manor courtyard, weapon racks, stone training ground, large hall in the background, vertical composition, no characters, 9:16 |
| `stage_07.png` | 천무학관 검혼관 | dormitory approach path inside a Chinese martial academy, wooden corridor with hidden trap mechanisms, arrow slits and a row of swords in the ground, tense atmosphere, vertical composition, no characters, 9:16 |
| `stage_08.png` | 천무식당 | large dining hall of a Chinese martial academy, long wooden tables, warm lantern light, evening, vertical composition, no characters, 9:16 |
| `stage_09.png` | 천무학관 운향정 | elegant pavilion beside a calm artificial lake, stalagmite-like garden rocks, lush green trees, moonlit, vertical composition, no characters, 9:16 |
| `stage_10.png` | 백향관 침투 | dormitory tower of a Chinese martial academy for women at night, new moon, fragrant flower garden with hidden formation stones, lanterns off, vertical composition, no characters, 9:16 |
| `stage_11.png` | 승급시험 | octagonal stone duel stage with eight pillars inside a martial academy hall, judges' seats, daylight, vertical composition, no characters, 9:16 |
| `stage_12.png` | 삼성무제 본선 | huge outdoor martial arts tournament arena with banners of many sects, packed spectator stands, bright sky, vertical composition, no characters, 9:16 |
| `stage_13.png` | 삼성대전 결승 | final tournament arena with cracked stone floor, crowd in the stands, three elders' seats on a dais, dramatic sky, vertical composition, no characters, 9:16 |
| `stage_14.png` | 무당산 고립 | misty Wudang mountain trail at dusk, mist-covered stone steps, dark pines, ink-wash style, vertical composition, no characters |
| `stage_15.png` | 철각비마대 | windswept highland road with iron-clad cavalry banners, dust, ink-wash style, vertical composition, no characters |
| `stage_16.png` | 저잣거리 습격 | crowded old market street at night with paper lanterns, ink-wash style, vertical composition, no characters |
| `stage_17.png` | 마하령의 분노 | training hall courtyard with scattered shields and torn banners, ink-wash style, vertical composition, no characters |
| `stage_18.png` | 태극의 인재 | snowy cold training hall with frost on stone floor, taiji pattern on ground, ink-wash style, vertical composition, no characters |
| `stage_19.png` | 환마동 시험 | dim cave hall with eight stone doors and drifting incense smoke, ink-wash style, vertical composition, no characters |
| `stage_20.png` | 매몰 44일 | underground cave with glowing blue minerals like stars, ink-wash style, vertical composition, no characters |
| `stage_21.png` | 흑랑채 청혼 | mountain bandit fortress at dusk with black wolf banners, ink-wash style, vertical composition, no characters |
| `stage_22.png` | 낙뢰곡 철쇄봉혼진 | narrow canyon crisscrossed with iron chains and falling arrows, ink-wash style, vertical composition, no characters |
| `stage_23.png` | 비사진 | canyon floor swarming with snakes under a stormy sky, ink-wash style, vertical composition, no characters |
| `stage_24.png` | 비뢰쌍마 | scorched canyon battlefield with smoke and two elderly silhouettes, ink-wash style, vertical composition, no characters |
| `stage_25.png` | 화평장 | old manor at night with torchlight and dark guards, ink-wash style, vertical composition, no characters |
| `stage_26.png` | 살귀대 | tavern street at dusk with pursuers on rooftops, ink-wash style, vertical composition, no characters |
| `stage_27.png` | 마천칠걸 | red plum valley entrance with seven silhouettes, ink-wash style, vertical composition, no characters |
| `stage_28.png` | 멸성대 | small roadside inn at noon surrounded by dark silhouettes, ink-wash style, vertical composition, no characters |
| `stage_29.png` | 중토관 | dense wilderness mountain at dawn with mist, ink-wash style, vertical composition, no characters |
| `stage_30.png` | 화룡멸겁대진 | red plum valley engulfed by a ring of fire and smoke, ink-wash style, vertical composition, no characters |
| `stage_31.png` | 승천무제 시험관 | martial academy courtyard in early spring with a golden armband on a stone bench, flying swords in the mist, ink-wash style, vertical composition, no characters |

## 적 (36종)

| 파일 | 대상 | 프롬프트 (공통 문구 뒤에 붙이기) |
|---|---|---|
| `tiger.png` | 호랑이 | a fierce tiger in an aggressive stance, full body, plain white background |
| `bear.png` | 곰 | a huge black bear rearing up on its hind legs, full body, plain white background |
| `white_tiger.png` | 백호 (보스) | a large white tiger with black stripes, majestic and menacing, full body, plain white background |
| `biaosa.png` | 표사 | a Chinese escort guard in plain dark work clothes holding a straight sword, full body, plain white background |
| `gang_janghan.png` | 비풍도 강장한 (보스) | a burly Chinese escort chief wielding a broad saber, confident glare, full body, plain white background |
| `hogol_bandit.png` | 호골채 산적 | a mountain bandit in ragged clothes with a crude blade, full body, plain white background |
| `paeho_jung_gwang.png` | 패호 중광 (보스) | a bandit chieftain, large and muscular, heavy blade, fur-trimmed clothes, full body, plain white background |
| `yonggol_bandit.png` | 용골채 산적 | a mountain bandit in ragged clothes with a spear, full body, plain white background |
| `paeryong_wooak.png` | 패룡 우악 (보스) | a huge bandit chieftain with a massive weapon, scarred face, full body, plain white background |
| `jang_ugang.png` | 장우강 | a proud young swordsman from a Taoist sword sect (Qingcheng), blue-grey robe, arrogant look, full body, plain white background |
| `yeomdo.png` | 염도 곽영희 (보스) | a tall saber master with red hair, red eyebrows and red beard, crimson aura, fiery saber, full body, plain white background |
| `hoajang_disciple.png` | 호아장 제자 | a disciple of a martial arts manor in matching robes with a sword, full body, plain white background |
| `ho_cheonsang.png` | 호아장주 호천상 (보스) | a middle-aged master of a martial arts manor, tiger-themed robe, sword, stern, full body, plain white background |
| `trap_arrow.png` | 기관 화살대 | a wooden trap contraption with a crossbow-like arrow launcher, mechanical, full body, plain white background |
| `trap_needle.png` | 기관 비침대 | a trap contraption firing many thin needles, mechanical, full body, plain white background |
| `trap_log.png` | 기관 통나무 | a heavy swinging log trap with ropes, mechanical, full body, plain white background |
| `ho_cheongang.png` | 호아장 대사형 호천강 (보스) | an arrogant young swordsman of a martial manor, tiger-themed robe, thick heavy sword, aggressive stance, full body, plain white background |
| `jongri_hak.png` | 종리학 | a young swordsman of a mountain sect with plum blossom emblem, white and purple robe, sword, full body, plain white background |
| `chu_gayeon.png` | 추가연 (보스) | a young woman swordsman of a mountain sect with plum blossom emblem, white and purple robe, sword, cold expression, full body, plain white background |
| `gu_chang.png` | 일도파쇄 구창 | a rough young saber fighter in black clothes, thick dark eyebrows, strong jaw, crude heavy saber, overbearing aura, full body, plain white background |
| `so_cheongun.png` | 청풍검 소천군 | a calm young swordsman from a Taoist sword sect (Qingcheng), yellow martial robe, ornate sword with red tassel, cold clear eyes, full body, plain white background |
| `wiji_cheon.png` | 선풍검룡 위지천 (보스) | a handsome young swordsman in blue silk martial robe, ornate sword with white tassel, wind swirling around him, full body, plain white background |

원작에서 외형이 확인된 것은 염도(붉은 머리·눈썹·수염), 구창(흑삼, 굵고 짙은 눈썹, 강인한 턱, 투박한 도), 위지천(청색 비단 무복, 흰 술이 달린 보검, 칠검룡 문양)이다. 나머지(표사, 산적, 호아장 인물, 종리학 등)는 원작에 구체적인 묘사가 없어 일반적인 모습으로 적었다.

## 작업 순서 제안
1. 배경 1장과 적 1장(예: `stage_01.png`, `tiger.png`)을 먼저 만들어 넣고 폰에서 화면이 어떻게 보이는지 확인한다.
2. 마음에 들면 나머지를 같은 방식으로 채운다.
3. 앱 아이콘이나 타이틀 이미지는 나중에 따로 만든다.

## 홈 화면 버튼 아이콘

`Assets/_Project/Resources/Art/Icons/<이름>.png` 로 넣으면 강호 탭의 버튼에 글자 대신 그림이 나오고 글자는 아래에 작게 붙는다. 파일이 없으면 글자 버튼 그대로다. 투명 배경 PNG, 정사각형 256x256 권장.

| 파일 이름 | 버튼 |
|---|---|
| `daily.png` | 일일 임무 |
| `quest.png` | 의뢰 게시판 |
| `fortune.png` | 기연 |
| `star.png` | 별 보상 |
| `tower.png` | 무한 비무대 |
| `stage.png` | 스테이지 출전 |
| `promotion.png` | 승급 시험장 |
| `party.png` | 파티 편성 |

## 하단 탭 바 아이콘

같은 `Resources/Art/Icons/` 폴더에 아래 이름으로 넣으면 탭 버튼에 그림이 나오고 글자는 아래에 작게 붙는다. 없는 탭은 글자 그대로다. 투명 배경 PNG, 정사각형 256x256 권장.

| 파일 이름 | 탭 |
|---|---|
| `tab_world.png` | 강호 |
| `tab_arts.png` | 무공 |
| `tab_bag.png` | 행낭 |
| `tab_gacha.png` | 뽑기 |
| `tab_record.png` | 기록 |

## 아이템 아이콘 (무기·방어구·장신구·소모품·재료)

`Assets/_Project/Resources/Art/Items/<아이템 id>.png` 로 넣으면 행낭 목록·상세 창·뽑기 결과·상태창 장비 칸에 글자 대신 그림이 나온다. 투명 배경 PNG, 정사각형 256x256 권장(비율은 유지된다).
특정 아이템 파일이 없으면 종류별 기본 그림을 쓴다: `Items/_weapon.png`, `_armor.png`(상의), `_pants.png`(하의), `_boots.png`(신발), `_accessory.png`, `_consumable.png`, `_material.png`, `_key.png`. 그것도 없으면 종류 색 네모 + 이름 첫 글자다. (분해 재료 25종 `mat_<부위>_<등급 번호>.png`와 하의·신발 기본 그림 `_pants.png`·`_boots.png`와 새 장비 168종의 아이템별 임시 그림도 들어 있다. 종류별 기본 그림 8장은 이미 `Resources/Art/Items/`에 들어 있다. 먹물 번짐 스타일의 임시 그림이니 마음에 드는 그림으로 같은 이름으로 덮어쓰면 된다.) 무기·방어구·장신구 61종은 아이템별 임시 그림이 이미 들어 있고, 등급이 높을수록 후광·광선·보석·번개가 더해져 화려하다(일반 → 상급 약한 녹색 후광, 희귀 파랑 후광과 반짝임, 영웅 보라 후광·고리·보석, 전설 금빛 광선, 신화 붉은 광선과 입자, 초월 무지개 고리와 번개). 소모품 13종(웅담·인형설삼·설삼·약초차·영양 떡·옥루주·뇌주·녹림 화주·학관 정식·표국 건량·태극환·매화 영단·일급영단)은 아이템별 임시 그림도 이미 들어 있다.

AI 프롬프트 예시: "wuxia ink-wash style icon of <아이템 설명>, single object, centered, transparent background, dark outline, 256x256".

### 무기 (18개)

| 파일 이름 | 아이템 | 등급 |
|---|---|---|
| `blackiron_blade.png` | 흑철도 | Rare |
| `cheonggang_sword.png` | 청강검 | Fine |
| `crescent_glaive.png` | 월아극 | Epic |
| `crimson_saber.png` | 적염도 | Epic |
| `dragonfang_sword.png` | 용아검 | Legendary |
| `dumok_spear.png` | 단목창 | Fine |
| `frost_sword.png` | 한철검 | Epic |
| `heaven_cutter.png` | 참천도 | Mythic |
| `hunter_knife.png` | 사냥꾼 단도 | Normal |
| `iron_sword.png` | 낡은 철검 | Normal |
| `jin_biroedo.png` | 진비뢰도 | Transcendent |
| `laundry_bat.png` | 강철 빨래방망이 | Fine |
| `oak_club.png` | 박달나무 곤봉 | Normal |
| `starfall_sword.png` | 성락검 | Mythic |
| `storm_blade.png` | 폭풍도 | Legendary |
| `thunder_dagger.png` | 뇌문 비수 | Rare |
| `twin_daggers.png` | 쌍비수 | Fine |
| `windcut_sword.png` | 풍절검 | Rare |

### 방어구 (16개)

| 파일 이름 | 아이템 | 등급 |
|---|---|---|
| `blackturtle_cloak.png` | 현무 망토 | Epic |
| `chain_shirt.png` | 쇠사슬 속옷 | Fine |
| `cloth_robe.png` | 무명 도복 | Normal |
| `crimson_longcoat.png` | 적운 장포 | Epic |
| `dragonscale_armor.png` | 용린 전갑 | Legendary |
| `heavensilk_robe.png` | 천잠보의 | Mythic |
| `jade_scale_armor.png` | 옥린갑 | Epic |
| `leather_vest.png` | 가죽 조끼 | Normal |
| `mountain_armor.png` | 산문 철갑 | Rare |
| `padded_coat.png` | 솜누비 겉옷 | Normal |
| `phoenix_robe.png` | 봉황 우의 | Legendary |
| `scholar_robe.png` | 유삼 | Fine |
| `silk_battle_robe.png` | 비단 전포 | Rare |
| `snakescale_vest.png` | 사린 갑옷 | Rare |
| `tigerhide_coat.png` | 호피 외투 | Fine |
| `vajra_armor.png` | 금강불괴갑 | Mythic |

### 장신구 (27개)

| 파일 이름 | 아이템 | 등급 |
|---|---|---|
| `bloodjade_bracelet.png` | 혈옥 팔찌 | Epic |
| `bronze_gauntlet.png` | 청동 장갑 | Rare |
| `cinnabar_amulet.png` | 주사 부적 | Rare |
| `claw_necklace.png` | 발톱 목걸이 | Fine |
| `copper_bell.png` | 구리 방울 | Normal |
| `dragon_pearl.png` | 여의주 | Legendary |
| `emperor_seal.png` | 제왕금인 | Mythic |
| `heavenly_demon_core.png` | 천마심주 | Mythic |
| `iron_bracer.png` | 철완갑 | Fine |
| `jade_pendant.png` | 비취 패옥 | Fine |
| `jade_ring.png` | 옥가락지 | Normal |
| `moonlight_pendant.png` | 월광 노리개 | Rare |
| `mukrin_talisman.png` | 묵린혈망 독아 부적 | Rare |
| `mukryonghwan.png` | 묵룡환 | Rare |
| `ninetail_charm.png` | 구미 부적 | Epic |
| `phoenix_feather.png` | 봉황 깃털 | Legendary |
| `roegeum.png` | 뇌금 묵뢰 | Epic |
| `sapphire_ring.png` | 청옥 반지 | Epic |
| `silver_hairpin.png` | 은비녀 | Fine |
| `snowlotus_charm.png` | 설련 패 | Legendary |
| `starlight_necklace.png` | 성광 목걸이 | Epic |
| `straw_sandals.png` | 짚신 한 켤레 | Normal |
| `taegeuk_jade.png` | 태극 옥패 | Mythic |
| `thunder_beads.png` | 뇌정 염주 | Rare |
| `thunder_crown.png` | 뇌제관 | Legendary |
| `wind_boots.png` | 바람신 | Fine |
| `wooden_charm.png` | 목각 부적 | Normal |

### 소모품 (13개)

| 파일 이름 | 아이템 | 등급 |
|---|---|---|
| `art_scroll_5.png` | 무공 수련서 (소) | Rare |
| `art_scroll_10.png` | 무공 수련서 (중) | Epic |
| `art_scroll_20.png` | 무공 수련서 (대) | Legendary |
| `bear_gall.png` | 웅담 | Fine |
| `biaoguk_ration.png` | 표국 건량 | Normal |
| `ginseng.png` | 인형설삼 | Epic |
| `greenwood_liquor.png` | 녹림 화주 | Fine |
| `herbal_tea.png` | 약초차 | Normal |
| `ilgeup_yeongdan.png` | 일급영단 | Legendary |
| `meal_academy.png` | 학관 정식 | Normal |
| `okruju.png` | 옥루주 | Fine |
| `plum_elixir.png` | 매화 영단 | Rare |
| `rice_cake.png` | 영양 떡 | Normal |
| `snow_ginseng.png` | 설삼 | Rare |
| `taegeuk_pill.png` | 태극환 | Rare |
| `thunder_wine.png` | 뇌주 | Rare |

### 재료 (2개)

| 파일 이름 | 아이템 | 등급 |
|---|---|---|
| `insight_stone.png` | 심득석 | Epic |
| `leather.png` | 가죽 | Normal |

### 분해 재료 (25개)

분해 재료는 `Items/mat_<부위>_<등급 번호>.png` 규칙이다. 부위 = `weapon`(무기)·`armor`(상의)·`pants`(하의)·`boots`(신발)·`accessory`(장신구), 등급 번호 = 0 일반·1 고급·2 희귀·3 영웅·4 전설(신화·초월 장비는 분해되지 않아 재료가 없다).

| 파일 이름 | 재료 | 부위 | 등급 |
|---|---|---|---|
| `mat_weapon_0.png` | 철 부스러기 | 무기 | Normal |
| `mat_weapon_1.png` | 정련 철괴 | 무기 | Fine |
| `mat_weapon_2.png` | 흑철 정 | 무기 | Rare |
| `mat_weapon_3.png` | 현철 조각 | 무기 | Epic |
| `mat_weapon_4.png` | 용린 강철 | 무기 | Legendary |
| `mat_armor_0.png` | 삼베 조각 | 상의 | Normal |
| `mat_armor_1.png` | 질긴 가죽 | 상의 | Fine |
| `mat_armor_2.png` | 은사 실타래 | 상의 | Rare |
| `mat_armor_3.png` | 자운 비단 | 상의 | Epic |
| `mat_armor_4.png` | 천잠 명주 | 상의 | Legendary |
| `mat_pants_0.png` | 광목 자투리 | 하의 | Normal |
| `mat_pants_1.png` | 누빔 솜 | 하의 | Fine |
| `mat_pants_2.png` | 흑견 천 | 하의 | Rare |
| `mat_pants_3.png` | 청룡 직물 | 하의 | Epic |
| `mat_pants_4.png` | 금강사 천 | 하의 | Legendary |
| `mat_boots_0.png` | 가죽 밑창 | 신발 | Normal |
| `mat_boots_1.png` | 징 쇠조각 | 신발 | Fine |
| `mat_boots_2.png` | 경공 밑창 | 신발 | Rare |
| `mat_boots_3.png` | 운보 가죽 | 신발 | Epic |
| `mat_boots_4.png` | 천리 밑창 | 신발 | Legendary |
| `mat_accessory_0.png` | 구슬 부스러기 | 장신구 | Normal |
| `mat_accessory_1.png` | 옥 조각 | 장신구 | Fine |
| `mat_accessory_2.png` | 보석 가루 | 장신구 | Rare |
| `mat_accessory_3.png` | 영석 | 장신구 | Epic |
| `mat_accessory_4.png` | 정령석 | 장신구 | Legendary |

### 핵심 아이템 (2개)

| 파일 이름 | 아이템 | 등급 |
|---|---|---|
| `cheonyul_pae.png` | 천율패 | Epic |
| `seungryong_pae.png` | 승룡패 | Rare |

## 무공 아이콘 (`Resources/Art/Arts/<무공 id>.png`)
무공 탭 목록, 전투 명령 버튼(왼쪽), 수련서 무공 선택 창에 나온다. 256x256 투명 PNG, 둥근 먹 원판 안에 무공을 상징하는 그림을 둔 형태이고 비기는 금빛 햇살 테두리와 붉은 「祕」 도장이 붙은 별도 디자인이다. 파일이 없으면 이름 첫 글자 색 블록(무공 탭)이나 글자만 있는 버튼(전투)으로 나온다. 기본 공격은 아이콘이 없다.

| 파일 | 무공 |
|---|---|
| `bido_throw` | 비도 던지기 |
| `samboku_chobok` | 삼복구타권법 초복 |
| `bunroesu` | 분뢰수 |
| `samcheopyeong` | 삼첩영 |
| `bi_roe_do` | 비뢰도 |
| `yeobanjang` | 여반장 |
| `roegwang_ryuhagok` | 뢰광류하곡 |
| `seonpung_roejeol` | 선풍뇌절 (비기) |
| `cheongeun_roe` | 천근뢰 |
| `cheonji_ibun` | 천지이분 |
| `bongik_bisang` | 봉익비상 |
| `simroe` | 심뢰 (비기) |
| `saengsa_seomroe` | 섬뢰창 |
| `eunseompung` | 은섬풍 |
| `goengroe` | 굉뢰 |
| `seomroe` | 섬뢰 (비기) |
| `jikji_jilju` | 직지질주 |
| `pungsin` | 풍신 (비기) |
| `xr_twin` | 효룡 쌍검 연격 |
| `xr_barrier` | 효룡 태을천강벽 |
| `jh_slash` | 장홍 검 |
| `jh_stun` | 장홍 점혈 |
| `yj_slash` | 윤준호 화산의 검 |
| `yj_heal` | 윤준호 자하신공 |
| `yj_cleanse` | 윤준호 자하청심결 (상태 이상을 씻고 회복하는 무공이라 물방울과 초록 십자) |
| `isj_gon` | 임성진 진성십이곤 |
| `isj_seonggwang` | 임성진 성광일시 |
| `isj_iljeom` | 임성진 일점필중 |
| `ny_baekhon` | 나예린 백혼검뢰천검식 |
| `ny_hansang` | 나예린 한상옥령신검 |
| `ny_simahn` | 나예린 심안검 |

### 필살기 슬롯 아이콘 (`Resources/Art/Icons/ultimate_slot.png`)
전투 명령의 「필살기」 버튼 전용 그림이다. 비기를 배웠지만 장착하지 않았을 때 비어 있는 슬롯(비류연만)과, 장착한 비기에 그림이 없을 때 대신 나온다. 장착한 비기에 그림이 있으면 그 비기 그림이 나온다. 붉은 「必」 도장이 박힌 금빛 햇살 별 모양이다.

### 전투 명령·설명 창 아이콘 (`Resources/Art/Icons/`)
전투 명령 버튼 왼쪽과 길게 눌러 뜨는 설명 창 오른쪽 위에 나온다. 무공은 `Arts/<무공 id>.png`, 아이템은 `Items/<아이템 id>.png`, 인물(상태 설명)은 `Characters/`를 쓰고 아래 파일은 무공이 아닌 명령용이다. 파일이 없으면 그림 없이 글자만 나온다.

| 파일 | 명령 |
|---|---|
| `cmd_attack.png` | 공격 |
| `cmd_guard.png` | 방어 |
| `cmd_meditate.png` | 운기 |
| `cmd_item.png` | 아이템 |
| `cmd_joint.png` | 합공 |

