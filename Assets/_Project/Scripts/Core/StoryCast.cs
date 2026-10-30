using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Additional story characters and their standing pictures Standing/&lt;Id&gt;.png. A combat portrayal of the same person may share the art key.</summary>
    public static class StoryCast
    {
        public class Entry
        {
            public readonly string Name, Id, Role;
            public readonly string[] Aliases;
            public Entry(string name, string id, string role, params string[] aliases) { Name = name; Id = id; Role = role; Aliases = aliases; }
            public bool MatchesCombatName(string name)
            {
                if (name.Contains(Name)) return true;
                foreach (var alias in Aliases) if (name.Contains(alias)) return true;
                return false;
            }
        }

        public static readonly IReadOnlyList<Entry> All = new List<Entry>
        {
            new Entry("무화", "mu_hwa", "구천현녀, 신마가 대부인"),
            new Entry("단혜", "dan_hye", "홍련선자"),
            new Entry("사란", "sa_ran", "빙련선자"),
            new Entry("갈효린", "gal_hyo_rin", "신마팔선자 넷째"),
            new Entry("갈효진", "gal_hyo_jin", "신마팔선자 다섯째"),
            new Entry("갈효민", "gal_hyo_min", "천음선녀"),
            new Entry("갈효효", "gal_hyo_hyo", "은창선녀"),
            new Entry("갈효묘", "gal_hyo_myo", "표묘선녀"),
            new Entry("흑견", "heukgyeon", "흑천십비의 노졸"),
            new Entry("손광", "son_gwang", "순찰단주 쾌영도"),
            new Entry("갈중하", "gal_jungha", "쌍도를 멘 둘째"),
            new Entry("남궁산산", "namgung_sansan", "남궁세가, 남궁상의 쌍둥이"),
            new Entry("예청", "ye_cheong", "빙월선자, 나예린의 어머니"),
            new Entry("류은경", "ryu_eungyeong", "세류보 장녀, 이약빙의 후계자"),
            new Entry("몽무", "mong_mu", "몽환쌍무"),
            new Entry("환무", "hwan_mu", "몽환쌍무"),
            new Entry("두노이", "du_noi", "구이십안, 은퇴한 정보상"),
            new Entry("연비", "yeonbi", "현의와 우산, 십 년을 잇는 이름"),
            new Entry("린", "young_na_yerin", "십 년 전 사천의 소녀"),
            new Entry("어린 연비", "young_yeonbi", "사천 천향루의 어린 무희"),
            new Entry("소한산", "so_hansan", "사천 천향루 주인"),
            new Entry("나일천", "na_ilcheon", "십 년 전 사천 부총령, 새로운 서천", "서천"),
            new Entry("옥유경", "ok_yugyeong", "혈옥선자, 마천각 무교관"),
            new Entry("모용휘", "mo_yonghwi", "칠절신검, 하남 모용세가"),
            new Entry("금영호", "geum_yeongho", "금호상회 외동아들"),
            new Entry("노학", "no_hak", "개방 8결"),
            new Entry("남궁상", "namgung_sang", "남궁세가, 주작단 단주"),
            new Entry("이진설", "i_jinseol", "전주 이가, 쌍검"),
            new Entry("청흔", "cheong_heun", "구정회, 무당 삼절검"),
            new Entry("당철영", "dang_cheolyeong", "사천당문 막내아들", "당삼"),
            new Entry("마진가", "ma_jinga", "천무학관주"),
            new Entry("장우양", "jang_uyang", "중양표국 국주"),
            new Entry("장우경", "jang_ugyeong", "중양표국 남창지국주"),
            new Entry("유재룡", "yu_jaeryong", "청룡은장 장주"),
            new Entry("유경영", "yu_gyeongyeong", "청룡은장의 남매, 장주의 유언을 지킴"),
            new Entry("공손절휘", "gongson_jeolhwi", "공손세가 후계자, 지존검법"),
            new Entry("유은성", "yu_eunseong", "점창 낙일검"),
            new Entry("은설란", "eun_seolran", "흑천맹 조사관, 사중화"),
            new Entry("홍란", "hong_ran", "천음선자, 음공 노사"),
            new Entry("현운", "hyeon_un", "무당 2대 제자, 구룡"),
            new Entry("비연태", "bi_yeontae", "애소저회 부장"),
            new Entry("진령", "jin_ryeong", "아미파 칠봉"),
            new Entry("혁중", "hyeok_jung", "화산규약지회 총책임"),
            new Entry("교옥", "gyo_ok", "마천칠걸 칠걸"),
            new Entry("백무영", "baek_muyeong", "구정회 부회주"),
            new Entry("추일태", "chu_iltae", "구정회, 검혼관"),
            new Entry("문일기", "mun_ilgi", "화산비천응, 검술 노사"),
            new Entry("나백천", "na_baekcheon", "무림맹주, 나예린의 아버지"),
            new Entry("조천우", "jo_cheonu", "스토리 인물"),
            new Entry("당평", "dang_pyeong", "사천당문, 암기 노사"),
            new Entry("무진자", "mu_jinja", "무당 전대 장로"),
            new Entry("현검자", "hyeon_geomja", "무당 전대 장문인, 청흔의 사부"),
            new Entry("도굴군", "do_gulgun", "천기수, 환마동 구조 지휘"),
            new Entry("임덕성", "im_deokseong", "녹림 총표파자, 임성진의 아버지"),
            new Entry("남궁호", "namgung_ho", "팔대세가 직계 낙제생"),
            new Entry("허주운", "heo_juun", "의약전 수석 의원"),
            new Entry("나중해", "na_junghae", "순풍당 주인, 정보상"),
            new Entry("옥현진인", "okhyeon_jinin", "무당 장로"),
            new Entry("적혈", "jeok_hyeol", "십이혈마대 총대주"),
            new Entry("매화검선", "maehwa_geomseon", "화산 최고 배분"),
            new Entry("안명후", "an_myeonghu", "정천맹 특급 감찰조사관"),
            new Entry("종쾌", "jong_kwae", "백 년 전 천하제일경공"),
            new Entry("천소해", "cheon_sohae", "천무쌍귀영"),
            new Entry("당철기", "dang_cheolgi", "천무쌍귀영"),
            new Entry("도광서", "do_gwangseo", "청룡단 기환검쾌"),
            new Entry("석류화", "seok_ryuhwa", "마천각 검희"),
            new Entry("도성", "do_seong", "천무삼성"),
            new Entry("검성", "geom_seong", "천무삼성, 모용휘의 조부"),
            new Entry("독고령", "dokgo_ryeong", "독안봉", "영령"),
            new Entry("혁월린", "hyeok_wolrin", "초대 학관주, 염도·빙검의 사부"),
            new Entry("공손일취", "geomjon_gongson", "검존, 원로원 원주", "gongson_ilchwi"),
            new Entry("단목우", "danmok_u", "천기룡, 천무구룡"),
            new Entry("모경", "mo_gyeong", "폭랑귀도, 폭랑삼십육도 대장")
        };

        public static Entry Find(string name)
        {
            foreach (var e in All)
            {
                if (e.Name == name) return e;
                foreach (var alias in e.Aliases) if (alias == name) return e;
            }
            return null;
        }
    }
}
