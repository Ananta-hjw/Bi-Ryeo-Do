using System.Collections.Generic;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>
    /// Painted UI icons come from ReadableIconLibrary; other artwork and legacy fallbacks are dropped into <c>Assets/_Project/Resources/Art</c>. Files are matched by name, so nothing has to be wired up:
    /// <c>Backgrounds/battle_forest.png</c>, <c>Standing/tiger.png</c> (enemy art key), <c>Standing/player.png</c>, <c>Items/laundry_bat.png</c> (item id). Player, companions, enemies and story-only NPCs all live in <c>Standing</c> (non-combat art) and <c>Battle</c> (SD figures), found by id.
    /// Any missing file returns null and the UI falls back to its flat-colour placeholder.
    /// Sprites are cached at runtime. Readable sheets preserve their original dimensions and disable mipmaps.
    /// </summary>
    public static class ArtLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Background(int stageNumber) { return Environment(SceneryArt.StageKey(stageNumber)); }
        public static Sprite Environment(string key) { return Load("Backgrounds/" + key); }
        private static readonly HashSet<Sprite> CutoutIcons = new HashSet<Sprite>();
        private static Sprite Cutout(Sprite sprite) { if (sprite != null) CutoutIcons.Add(sprite); return sprite; }
        public static bool IsCutoutIcon(Sprite sprite) { return sprite != null && CutoutIcons.Contains(sprite); }
        /// <summary>Painted menu/command icon first; legacy fallback: <c>Icons/&lt;key&gt;.png</c> (daily, quest, fortune, star, tower, stage, promotion, party).</summary>
        public static Sprite Icon(string key) { return Cutout(ReadableIconLibrary.Find("Icon", key) ?? Load("Icons/" + key)); }
        /// <summary>Individually mapped item picture first; legacy fallback: <c>Items/&lt;item id&gt;.png</c>, else the kind's default <c>Items/_weapon|_armor|_pants|_boots|_accessory|_consumable|_material|_key.png</c>.</summary>
        public static Sprite Item(BiRyeoDo.Core.ItemData item)
        {
            if (item == null) return null;
            var individual = ReadableIconLibrary.Find("Item", item.Id);
            if (individual != null) return ReadableIconLibrary.HasTransparentCanvas("Item", item.Id) ? individual : Cutout(individual);
            return Cutout(Load("Items/" + item.Id)
                ?? ReadableIconLibrary.Find("DefaultItem", item.Kind.ToString()) ?? Load("Items/_" + item.Kind.ToString().ToLowerInvariant()));
        }
        public static Sprite ShopGood(BiRyeoDo.Core.ShopGood good)
        {
            return good == null ? null : ReadableIconLibrary.Find("Shop", good.Id);
        }
        /// <summary>Individually mapped martial-art picture first; legacy fallback: <c>Arts/&lt;art id&gt;.png</c> (also the skill id in battle); null when missing.</summary>
        public static Sprite Art(string artId) { return string.IsNullOrEmpty(artId) ? null : ReadableIconLibrary.Find("Art", artId) ?? Load("Arts/" + artId); }
        /// <summary>Progress actions reuse existing command icons; battle techniques retain their individual art.</summary>
        public static Sprite Command(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            var command = ReadableIconLibrary.Find("Command", skillId);
            return command != null ? Cutout(command) : Art(skillId);
        }
        /// <summary>Individual passive-method icon, shared with the martial-arts workspace.</summary>
        public static Sprite InnerArt(string id) { return ReadableIconLibrary.Find("Inner", id); }
        /// <summary>SD figure source: <c>Battle/&lt;id&gt;.png</c>, used on the field, combat summaries and compact character pickers.</summary>
        public static Sprite Chibi(string id) { return string.IsNullOrEmpty(id) ? null : Load("Battle/" + id); }
        /// <summary>Stand-in SD figure when a dedicated <c>Battle/</c> picture is missing: <c>Battle/default_assassin.png</c> for bandits and assassins, <c>default_woman.png</c> for the women listed in <c>Genders</c>, else <c>default_man.png</c> (all under <c>Battle/</c>). Null when that file is missing too.</summary>
        /// <summary>영수 art: <c>Beasts/&lt;id&gt;.png</c>, else the battle figure of its art key (청홍쌍각사, 백무후 = white_tiger), else null.</summary>
        public static Sprite Beast(BiRyeoDo.Core.BeastData beast)
        {
            if (beast == null) return null;
            return Load("Beasts/" + beast.Id) ?? Chibi(beast.ArtKey);
        }

        public static Sprite DefaultChibi(string artKey) { return Load("Battle/" + BiRyeoDo.Core.Genders.DefaultFigure(artKey)); }

        /// <summary>Stable filename for a character name or ID; enemy art keys may differ from their battle IDs.</summary>
        private static string CharacterKey(string name, bool allowShortTitle = true)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name == BiRyeoDo.Core.StoryLine.Protagonist) return "player";
            foreach (var companion in BiRyeoDo.Core.EarlyGameContent.Companions)
                if (companion.Name == name || companion.Id == name) return companion.Id;
            var cast = BiRyeoDo.Core.StoryCast.Find(name);
            if (cast != null) return cast.Id;
            foreach (var enemy in BiRyeoDo.Core.EarlyGameContent.Enemies)
                if (enemy.Name == name || enemy.Id == name) return enemy.ArtKey;
            // Dialogue often uses a short title (e.g. 염도) rather than the enemy's complete name.
            if (allowShortTitle)
                foreach (var enemy in BiRyeoDo.Core.EarlyGameContent.Enemies)
                    if (name.Length >= 2 && enemy.Name.Contains(name)) return enemy.ArtKey;
            return name;
        }

        // Exact SD identities only. Do not use title substrings or Standing-only aliases.
        private static readonly Dictionary<string, string> BattleKeys = new Dictionary<string, string>
        {
            { "채이수", "chae_isu" },
            { "문진 채이수", "chae_isu" },
            { "여홍아", "merchant_yeo" },
            { "금홍의복점주 여홍아", "merchant_yeo" },
            { "팽유경", "pae_yugyeong" },
            { "단옥도 팽유경", "pae_yugyeong" },
            { "혈창", "hyeol_chang" },
            { "혈창조 조장", "hyeol_chang" },
            { "등여운", "deung_yeoun" },
            { "쾌창 등여운", "deung_yeoun" },
            { "등여호", "deung_yeoho" },
            { "섬연창 등여호", "deung_yeoho" },
            { "막적", "mak_jeok" },
            { "적웅산도 막적", "mak_jeok" },
            { "조걸", "jo_geol" },
            { "쾌창 조걸", "jo_geol" },
            { "초월", "cho_wol" },
            { "검마", "cho_wol" },
            { "검마 초월", "cho_wol" },
            { "변장한 비류연", "biry_old_master" },
            { "비류연(노사부)", "biry_old_master" },
            { "가짜 노사부", "biry_old_master" },
            { "추항", "myeongbu_jang" },
            { "명염도", "myeongbu_jang" },
            { "명부오귀장", "myeongbu_jang" },
            { "초혼오귀검", "chohon_gwigeom" },
            { "백교", "baekgyo" },
            { "화룡", "hwaryong_hwayeom" },
            { "십이혈마대", "hyeolma_dae" },
            { "살룡대", "salryong_daewon" },
            { "살귀대", "salgwi_daewon" },
            { "매화가면", "maehwa_mask_trial" },
            { "멸성대", "mulsung_daewon" },
            { "하얀 손", "baek_son_gosu" },
            { "하얀 손 고수", "baek_son_gosu" },
            { "비류연", "player" },
            { "효룡", "hyo_ryong" },
            { "을진무쌍검", "hyo_ryong" },
            { "장홍", "jang_hong" },
            { "윤준호", "yun_junho" },
            { "나예린", "na_yerin" },
            { "빙백봉", "na_yerin" },
            { "빙백봉 나예린", "na_yerin" },
            { "임성진", "im_seongjin" },
            { "진성곤", "im_seongjin" },
            { "남궁상", "namgung_sang" },
            { "뇌전검룡", "namgung_sang" },
            { "모용휘", "mo_yonghwi" },
            { "칠절신검", "mo_yonghwi" },
            { "강장한", "gang_janghan" },
            { "비풍도", "gang_janghan" },
            { "중광", "paeho_jung_gwang" },
            { "패호", "paeho_jung_gwang" },
            { "우악", "paeryong_wooak" },
            { "패룡", "paeryong_wooak" },
            { "장우강", "jang_ugang" },
            { "염도", "yeomdo" },
            { "곽영희", "yeomdo" },
            { "염도 곽영희", "yeomdo" },
            { "호천상", "ho_cheonsang" },
            { "호아장주", "ho_cheonsang" },
            { "장우양", "jang_uyang" },
            { "십팔검", "jang_uyang" },
            { "관설지", "gwan_seolji_story" },
            { "청설옥검녀", "gwan_seolji_story" },
            { "청흔", "cheong_heun" },
            { "삼절검", "cheong_heun" },
            { "호천강", "ho_cheongang" },
            { "종리학", "jongri_hak" },
            { "추가연", "chu_gayeon" },
            { "구창", "gu_chang" },
            { "일도파쇄", "gu_chang" },
            { "소천군", "so_cheongun" },
            { "청풍검", "so_cheongun" },
            { "위지천", "wiji_cheon" },
            { "선풍검룡", "wiji_cheon" },
            { "십이선녀", "sunyeo" },
            { "독한녀", "sunyeo" },
            { "하세인", "ha_sein" },
            { "철검비룡", "ha_sein" },
            { "강현추", "gang_hyeonchu" },
            { "영취검", "gang_hyeonchu" },
            { "단대풍", "dan_daepung" },
            { "단평", "dan_pyeong" },
            { "쾌환검", "dan_pyeong" },
            { "언가영", "eonga_yeong" },
            { "파풍창", "eonga_yeong" },
            { "전옥기", "jeon_okgi" },
            { "팔비신검", "jeon_okgi" },
            { "당문천", "dang_munceon" },
            { "독랄수", "dang_munceon" },
            { "갈효봉", "gal_hyobong" },
            { "혈류도", "gal_hyobong" },
            { "천살", "cheonsal" },
            { "천살 초혼검", "cheonsal" },
            { "지살", "jisal" },
            { "지살 명왕도", "jisal" },
            { "구천학", "gu_cheonhak" },
            { "질풍묵흔", "gu_cheonhak" },
            { "질풍천뢰", "gu_cheonhak" },
            { "백탄저", "baek_tanjeo" },
            { "관살", "baek_tanjeo" },
            { "백인회주", "baek_tanjeo" },
            { "팽혁성", "pae_hyeokseong" },
            { "광풍맹호도", "pae_hyeokseong" },
            { "마하령", "ma_haryeong" },
            { "철옥잠", "ma_haryeong" },
            { "군웅회주", "ma_haryeong" },
            { "용천명", "yong_cheonmyeong" },
            { "창천룡", "yong_cheonmyeong" },
            { "빙검", "gwan_cheolsu" },
            { "관철수", "gwan_cheolsu" },
            { "빙검 관철수", "gwan_cheolsu" },
            { "임덕성", "im_deokseong" },
            { "녹림왕", "im_deokseong" },
            { "녹림 총표파자", "im_deokseong" },
            { "사부", "hwan_sabu" },
            { "사부님", "hwan_sabu" },
            { "노사부", "hwan_sabu" },
            { "노사부님", "hwan_sabu" },
            { "비류연의 사부", "hwan_sabu" },
            { "환영 속 사부", "hwan_sabu" },
            { "임개", "im_gae" },
            { "흑랑부", "im_gae" },
            { "혈궁", "hyeol_gung" },
            { "혈쇄", "hyeol_swae" },
            { "모사령", "mo_saryeong" },
            { "비사마군", "mo_saryeong" },
            { "동방학", "dongbang_hak" },
            { "섬뢰마검", "dongbang_hak" },
            { "공패", "gong_pae" },
            { "잔뢰마도", "gong_pae" },
            { "은명", "bi_daegongja" },
            { "대공자", "bi_daegongja" },
            { "대공자 비", "bi_daegongja" },
            { "무정", "mu_jeong" },
            { "잔무일점혈", "mu_jeong" },
            { "은형의 무정", "mu_jeong" },
            { "교옥", "gyo_ok" },
            { "혈심란", "gyo_ok" },
            { "호유광", "ho_yugwang" },
            { "화평장주", "ho_yugwang" },
            { "종리추", "jongri_chu" },
            { "칠련창", "jongri_chu" },
            { "추명", "chu_myeong" },
            { "마검익", "chu_myeong" },
            { "사마흔", "sama_heun" },
            { "천참마검", "sama_heun" },
            { "검후", "geomhu_ioksang" },
            { "이옥상", "geomhu_ioksang" },
            { "검후 이옥상", "geomhu_ioksang" },
            { "윤이정", "yun_ijeong" },
            { "풍마도", "yun_ijeong" },
            { "풍마도 윤이정", "yun_ijeong" },
            { "살룡", "yun_ijeong" },
            { "공손절휘", "gongson_jeolhwi" },
            { "이시건", "i_sigeon" },
            { "마천각 이공자", "i_sigeon" },
            { "마천각 이공자 이시건", "i_sigeon" },
            { "흑응비섬의 이시건", "i_sigeon" },
            { "자운 암풍 이시건", "i_sigeon" },
            { "진소령", "jin_soryeong" },
            { "아미신녀", "jin_soryeong" },
            { "혈표", "hyeol_pyo" },
            { "유란", "yu_ran" },
            { "유운비", "yu_unbi" },
            { "일혈", "il_hyeol" },
            { "검존", "geomjon_gongson" },
            { "공손일취", "geomjon_gongson" },
            { "검존 공손일취", "geomjon_gongson" },
            { "gongson_ilchwi", "geomjon_gongson" },
            { "옥유경", "ok_yugyeong" },
            { "혈옥선자", "ok_yugyeong" },
            { "백무후", "baekmu_ju" },
            { "하양이", "baekmu_ju" },
            { "흑룡왕", "heukryong_wang" },
            { "해응신", "heukryong_wang" },
            { "흑룡번신", "heukryong_wang" },
            { "흑룡왕 해응신", "heukryong_wang" },
            { "예청", "ye_cheong" },
            { "빙월선자", "ye_cheong" },
            { "빙월희 적예", "ye_cheong" },
            { "진령", "jin_ryeong" },
            { "독고령", "dokgo_ryeong" },
            { "독안봉", "dokgo_ryeong" },
            { "영령", "dokgo_ryeong" },
            { "몽영령", "dokgo_ryeong" },
            { "칠상흔", "chil_sangheun" },
            { "이벽한", "chil_sangheun" },
            { "혈염쌍도", "chil_sangheun" },
            { "락비오", "rakbio" },
            { "서해왕", "rakbio" },
            { "자군", "jagun" },
            { "동해왕", "jagun" },
            { "미폭공자", "jagun" },
            { "전혼", "jeonhon" },
            { "남해왕", "jeonhon" },
            { "창랑", "changrang" },
            { "마랑객", "changrang" },
            { "마랑객 창랑", "changrang" },
            { "창랑아", "changranga" },
            { "무명", "mumyeong" },
            { "나일천", "na_ilcheon" },
            { "서천", "na_ilcheon" },
            { "서천멸겁", "na_ilcheon" },
            { "무화", "mu_hwa" },
            { "구천현녀", "mu_hwa" },
            { "단혜", "dan_hye" },
            { "홍련선자", "dan_hye" },
            { "홍령선자", "dan_hye" },
            { "사란", "sa_ran" },
            { "빙련선자", "sa_ran" },
            { "빙령선자", "sa_ran" },
            { "갈효린", "gal_hyo_rin" },
            { "효린", "gal_hyo_rin" },
            { "갈효진", "gal_hyo_jin" },
            { "효진", "gal_hyo_jin" },
            { "갈효민", "gal_hyo_min" },
            { "효민", "gal_hyo_min" },
            { "천음선녀", "gal_hyo_min" },
            { "갈효효", "gal_hyo_hyo" },
            { "효효", "gal_hyo_hyo" },
            { "은창선녀", "gal_hyo_hyo" },
            { "갈효묘", "gal_hyo_myo" },
            { "효묘", "gal_hyo_myo" },
            { "표묘선녀", "gal_hyo_myo" },
            { "흑견", "heukgyeon" },
            { "효민·효효", "gal_hyo_duet" },
            { "갈효민·갈효효", "gal_hyo_duet" },
            { "단혜·사란", "mothers_fire_ice" },
            { "혁중", "hyeok_jung" },
            { "갈중혁", "hyeok_jung" },
            { "신마", "hyeok_jung" },
            { "무신마", "hyeok_jung" },
            { "패천도", "hyeok_jung" },
            { "무신마 갈중혁", "hyeok_jung" },
            { "패천도 갈중혁", "hyeok_jung" },
            { "혁월린", "hyeok_wolrin" },
            { "무신", "hyeok_wolrin" },
            { "태극신군", "hyeok_wolrin" },
            { "태극신군 무신", "hyeok_wolrin" },
            { "검성", "geom_seong" },
            { "모용정천", "geom_seong" },
            { "신검협", "geom_seong" },
            { "검성 모용정천", "geom_seong" },
            { "도성", "do_seong" },
            { "하후식", "do_seong" },
            { "도성 하후식", "do_seong" },
            { "갈중천", "gal_jungcheon" },
            { "흑천맹주", "gal_jungcheon" },
            { "신유성", "shin_yuseong" },
            { "취영검", "shin_yuseong" },
            { "은가면 공포", "mask_gongpo" },
            { "은가면 「공포」", "mask_gongpo" },
            { "은가면 절망", "mask_jeolmang" },
            { "은가면 「절망」", "mask_jeolmang" },
            { "은가면 비탄", "mask_bitan" },
            { "은가면 「비탄」", "mask_bitan" },
            { "윤미", "yunmi" },
            { "당철영", "dang_cheolyeong" },
            { "당삼", "dang_cheolyeong" },
            { "은설란", "eun_seolran" },
            { "사중화", "eun_seolran" },
            { "홍란", "hong_ran" },
            { "천음선자", "hong_ran" },
            { "석류화", "seok_ryuhwa" },
            { "진홍의 검희", "seok_ryuhwa" },
            { "노학", "no_hak" },
            { "마진가", "ma_jinga" },
            { "천무학관주", "ma_jinga" },
            { "나백천", "na_baekcheon" },
            { "무림맹주", "na_baekcheon" },
            { "손광", "son_gwang" },
            { "쾌영도", "son_gwang" },
            { "갈중하", "gal_jungha" },
            { "남궁산산", "namgung_sansan" },
            { "류은경", "ryu_eungyeong" },
            { "두노이", "du_noi" },
            { "구이십안", "du_noi" },
            { "유재룡", "yu_jaeryong" },
            { "유경영", "yu_gyeongyeong" },
            { "유은성", "yu_eunseong" },
            { "낙일검", "yu_eunseong" },
            { "현운", "hyeon_un" },
            { "비연태", "bi_yeontae" },
            { "백무영", "baek_muyeong" },
            { "지룡", "baek_muyeong" },
            { "형산일기", "baek_muyeong" },
            { "추일태", "chu_iltae" },
            { "일은무영", "chu_iltae" },
            { "문일기", "mun_ilgi" },
            { "화산비천응", "mun_ilgi" },
            { "당평", "dang_pyeong" },
            { "천수탈혼", "dang_pyeong" },
            { "도굴군", "do_gulgun" },
            { "천기수", "do_gulgun" },
            { "종쾌", "jong_kwae" },
            { "비공답운", "jong_kwae" },
            { "도광서", "do_gwangseo" },
            { "기환검쾌", "do_gwangseo" },
            { "단목우", "danmok_u" },
            { "천기룡", "danmok_u" },
            { "모경", "mo_gyeong" },
            { "폭랑귀도", "mo_gyeong" },
            { "도추운", "do_chuun" },
            { "사갈검편", "do_chuun" },
            { "맹연호", "maeng_yeonho" },
            { "청룡단주", "maeng_yeonho" },
            { "천수룡", "maeng_yeonho" },
            { "옥룡신검", "maeng_yeonho" },
            { "이송학", "lee_songhak" },
            { "이송", "lee_songhak" },
            { "군자소요검", "lee_songhak" },
            { "위무상", "wi_musang" },
            { "위천상", "wi_musang" },
            { "늑기한", "neuk_gihan" },
            { "천익검", "neuk_gihan" },
            { "담환", "dam_hwan" },
            { "담 총관", "dam_hwan" },
            { "용경의", "yong_gyeongui" },
            { "도제", "yong_gyeongui" },
            { "일도단애", "yong_gyeongui" },
            { "섭운명", "seop_unmyeong" },
            { "검치", "seop_unmyeong" },
            { "일지번천", "seop_unmyeong" },
            { "공손경운", "gongson_gyeongun" },
            { "돈왕", "donwang" },
            { "금적신", "donwang" },
            { "마천각주", "macheon_gakju" },
            { "북천멸겁", "macheon_gakju" },
            { "위천무", "wi_cheonmu" },
            { "천겁혈신", "wi_cheonmu" },
            { "갈효혜", "gal_hyo_hye" },
            { "효혜", "gal_hyo_hye" },
            { "십지선녀", "gal_hyo_hye" },
            { "여제갈", "gal_hyo_hye" },
            { "불락구척", "bullak_gucheok" },
            { "생사무허가", "bullak_gucheok" },
            { "고약한", "go_yakhan" },
            { "마검자", "go_yakhan" },
            { "몽환장주", "monghwan_jangju" },
            { "안명후", "an_myeonghu" },
            { "구척철안", "an_myeonghu" },
            { "구척철심안", "an_myeonghu" },
            { "매화검선", "maehwa_geomseon" },
            { "유환권", "maehwa_geomseon" },
            { "매화검선 유환권", "maehwa_geomseon" },
            { "옥현진인", "okhyeon_jinin" },
            { "허주운", "heo_juun" },
            { "천수신의", "heo_juun" },
            { "남궁진", "namgung_jin" },
            { "전광검", "namgung_jin" },
            { "전광검 남궁진", "namgung_jin" },
            { "백결", "baek_gyeol" },
            { "단목강", "danmok_gang" },
            { "철혈장창", "danmok_gang" },
            { "철혈장창 단목강", "danmok_gang" },
            { "노주생", "no_jusaeng" },
            { "곤륜일검 노주생", "no_jusaeng" },
            { "곤륜쌍검 노주생", "no_jusaeng" },
            { "채하군", "chae_hagun" },
            { "곤륜쌍검 채하군", "chae_hagun" },
        };

        /// <summary>All compact/combat character contexts stay in Battle/, including the fallback.</summary>
        public static Sprite SdFigure(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId)) return null;
            var exact = Chibi(nameOrId);
            if (exact != null) return exact;
            string key;
            if (!BattleKeys.TryGetValue(nameOrId, out key)) key = CharacterKey(nameOrId, allowShortTitle: false);
            return Chibi(key) ?? DefaultChibi(key);
        }

        // Standing-only display-name aliases. Keep SD identity and default selection independent.
        private static readonly Dictionary<string, string> StandingKeys = new Dictionary<string, string>
        {
            { "강씨", "gang_ssi" },
            { "어부 강씨", "gang_ssi" },
            { "호철", "ho_cheol" },
            { "화평장 무사 호철", "ho_cheol" },
            { "두칠", "du_chil" },
            { "구이관 두칠", "du_chil" },
            { "채이수", "chae_isu" },
            { "문진 채이수", "chae_isu" },
            { "여홍아", "merchant_yeo" },
            { "금홍의복점주 여홍아", "merchant_yeo" },
            { "팽유경", "pae_yugyeong" },
            { "단옥도 팽유경", "pae_yugyeong" },
            { "이무강", "i_mugang" },
            { "영뇌 이무강", "i_mugang" },
            { "영뇌", "i_mugang" },
            { "혈창", "hyeol_chang" },
            { "혈창조 조장", "hyeol_chang" },
            { "도남포", "do_nampo" },
            { "십자검 도남포", "do_nampo" },
            { "십자검", "do_nampo" },
            { "주월산", "ju_wolsan" },
            { "환월주루 주인", "ju_wolsan" },
            { "마하령", "ma_haryeong" },
            { "철옥잠", "ma_haryeong" },
            { "군웅회주", "ma_haryeong" },
            { "등여운", "deung_yeoun" },
            { "쾌창 등여운", "deung_yeoun" },
            { "등여호", "deung_yeoho" },
            { "섬연창 등여호", "deung_yeoho" },
            { "막적", "mak_jeok" },
            { "적웅산도 막적", "mak_jeok" },
            { "조걸", "jo_geol" },
            { "쾌창 조걸", "jo_geol" },
            { "초월", "cho_wol" },
            { "검마", "cho_wol" },
            { "검마 초월", "cho_wol" },
            { "남궁진", "namgung_jin" },
            { "전광검", "namgung_jin" },
            { "전광검 남궁진", "namgung_jin" },
            { "당철악", "dang_cheolak" },
            { "쾌속수 당철악", "dang_cheolak" },
            { "노주생", "no_jusaeng" },
            { "곤륜일검 노주생", "no_jusaeng" },
            { "곤륜쌍검 노주생", "no_jusaeng" },
            { "채하군", "chae_hagun" },
            { "곤륜쌍검 채하군", "chae_hagun" },
            { "하우수", "ha_usu" },
            { "의호 하우수", "ha_usu" },
            { "환무", "hwan_mu" },
            { "갈효린", "gal_hyo_rin" },
            { "효린", "gal_hyo_rin" },
            { "갈효진", "gal_hyo_jin" },
            { "효진", "gal_hyo_jin" },
            { "단목강", "danmok_gang" },
            { "철혈장창", "danmok_gang" },
            { "철혈장창 단목강", "danmok_gang" },
            { "백결", "baek_gyeol" },
            { "창랑", "changrang" },
            { "마랑객", "changrang" },
            { "마랑객 창랑", "changrang" },
            { "흑견", "heukgyeon" },
            { "갈효민", "gal_hyo_min" },
            { "효민", "gal_hyo_min" },
            { "천음선녀", "gal_hyo_min" },
            { "갈효효", "gal_hyo_hyo" },
            { "효효", "gal_hyo_hyo" },
            { "은창선녀", "gal_hyo_hyo" },
            { "갈효묘", "gal_hyo_myo" },
            { "효묘", "gal_hyo_myo" },
            { "표묘선녀", "gal_hyo_myo" },
            { "사란", "sa_ran" },
            { "빙련선자", "sa_ran" },
            { "빙령선자", "sa_ran" },
            { "락비오", "rakbio" },
            { "서해왕", "rakbio" },
            { "자군", "jagun" },
            { "동해왕", "jagun" },
            { "미폭공자", "jagun" },
            { "전혼", "jeonhon" },
            { "남해왕 전혼", "jeonhon" },
            { "흑룡왕", "heukryong_wang" },
            { "해응신", "heukryong_wang" },
            { "흑룡번신", "heukryong_wang" },
            { "흑룡왕 해응신", "heukryong_wang" },
            { "무명", "mumyeong" },
            { "나일천", "na_ilcheon" },
            { "서천", "na_ilcheon" },
            { "서천멸겁", "na_ilcheon" },
            { "칠상흔", "chil_sangheun" },
            { "이벽한", "chil_sangheun" },
            { "무화", "mu_hwa" },
            { "구천현녀", "mu_hwa" },
            { "단혜", "dan_hye" },
            { "홍련선자", "dan_hye" },
            { "변장한 비류연", "biry_old_master" },
            { "비류연(노사부)", "biry_old_master" },
            { "가짜 노사부", "biry_old_master" },
            { "도추운", "do_chuun" },
            { "맹연호", "maeng_yeonho" },
            { "이송학", "lee_songhak" },
            { "이송", "lee_songhak" },
            { "위무상", "wi_musang" },
            { "위천상", "wi_musang" },
            { "늑기한", "neuk_gihan" },
            { "담환", "dam_hwan" },
            { "담 총관", "dam_hwan" },
            { "용경의", "yong_gyeongui" },
            { "도제", "yong_gyeongui" },
            { "섭운명", "seop_unmyeong" },
            { "검치", "seop_unmyeong" },
            { "윤미", "yun_mi" },
            { "공손경운", "gongson_gyeongun" },
            { "몽환장주", "monghwan_jangju" },
            { "불락구척", "bullak_gucheok" },
            { "돈왕", "donwang" },
            { "당문혜", "dang_munhye" },
            { "마천각주", "macheon_gakju" },
            { "북천멸겁", "macheon_gakju" },
            { "천겁혈신", "wi_cheonmu" },
            { "위천무", "wi_cheonmu" },
            { "갈효혜", "gal_hyo_hye" },
            { "효혜", "gal_hyo_hye" },
            { "갈중혁", "hyeok_jung" },
            { "노사부", "hwan_sabu" },
            { "비류연의 사부", "hwan_sabu" },
            { "사감 강하윤", "gang_hayun" },
            { "은가면 공포", "mask_gongpo" },
            { "은가면 절망", "mask_jeolmang" },
            { "은명", "bi_daegongja" },
            { "은가면 비탄", "mask_bitan" },
            { "jongri_chu_night", "jongri_chu" },
            { "왕견", "wang_gyeon" },
            { "유선아", "yu_seona" },
            { "현의여인", "hyeonui_woman" },
            { "손님", "guest" },
            { "행인", "passerby" },
            { "치사한", "chi_sahan" },
            { "밀실의 무리", "secret_room_group" },
            { "호골채 도적", "hogol_robber" },
            { "무흔일호", "muheun_ilho" },
            { "흑의서생", "heukui_scholar" },
            { "수호대", "guard_unit" },
            { "강하윤", "gang_hayun" },
            { "고약한", "go_yakhan" },
            { "손문경", "son_mungyeong" },
            { "영령", "yeongryeong" },
            { "주작단원", "jujak_member" },
            { "지배자", "ruler" },
            { "율령자", "yullyeongja" },
            { "홍", "hong" },
            { "안내 선배", "senior_guide" },
            { "현수", "hyeon_su" },
            { "괴인", "mysterious_man" },
            { "장노", "jang_no" },
        };

        /// <summary>Non-combat art of a character: <c>Standing/&lt;id&gt;.png</c>; null when missing.</summary>
        public static Sprite StandingPortrait(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string key;
            if (!StandingKeys.TryGetValue(name, out key)) key = CharacterKey(name);
            return key == null ? null : Load("Standing/" + key);
        }

        /// <summary>Picture used outside SD contexts (profiles, codex, formation): the standing art.</summary>
        public static Sprite CharacterPortrait(string name) { return StandingPortrait(name); }

        public static Sprite LobbyPortrait(string name) { return CharacterPortrait(name); }

        /// <summary>The lobby and dialogue share the standing art.</summary>
        public static Sprite DialoguePortrait(string name) { return StandingPortrait(name); }

        /// <summary>Six painted lobby tiles, sliced at runtime without importing a sprite sheet.</summary>
        public static Sprite LobbyCard(string key)
        {
            string[] keys = { "companion", "equipment", "arts", "gacha", "bag", "record" };
            int index = System.Array.IndexOf(keys, key);
            if (index < 0) return Icon(key);
            string cacheKey = "LobbyCard/" + key;
            Sprite sprite;
            if (Cache.TryGetValue(cacheKey, out sprite)) return sprite;
            var atlas = Resources.Load<Texture2D>("Art/Lobby/menu_cards");
            if (atlas == null) return Icon(key);
            float width = atlas.width / 3f, height = atlas.height / 2f;
            // A small inset keeps neighbouring tile pixels outside bilinear sampling.
            sprite = Sprite.Create(atlas, new Rect(index % 3 * width + 3f, (1 - index / 3) * height + 3f,
                width - 6f, height - 6f), new Vector2(.5f, .5f), 100f);
            Cache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>Stable muted colour for a character without artwork.</summary>
        public static Color TintFor(string name)
        {
            int h = 17;
            foreach (char ch in name) h = h * 31 + ch;
            float hue = Mathf.Abs(h % 360) / 360f;
            return Color.HSVToRGB(hue, 0.35f, 0.45f);
        }

        private static Sprite Load(string path)
        {
            Sprite sprite;
            if (Cache.TryGetValue(path, out sprite)) return sprite;

            var tex = Resources.Load<Texture2D>("Art/" + path);
            sprite = tex == null ? null : Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[path] = sprite;
            return sprite;
        }
    }
}
