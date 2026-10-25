using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Characters who only appear in story scenes (neither a companion nor an enemy). Each gets a standing picture Standing/&lt;Id&gt;.png.</summary>
    public static class StoryCast
    {
        public class Entry
        {
            public readonly string Name, Id, Role;
            public Entry(string name, string id, string role) { Name = name; Id = id; Role = role; }
        }

        public static readonly IReadOnlyList<Entry> All = new List<Entry>
        {
            new Entry("모용휘", "mo_yonghwi", "칠절신검, 하남 모용세가"),
            new Entry("금영호", "geum_yeongho", "금호상회 외동아들"),
            new Entry("노학", "no_hak", "개방 8결"),
            new Entry("남궁상", "namgung_sang", "남궁세가, 주작단 단주"),
            new Entry("이진설", "i_jinseol", "전주 이가, 쌍검"),
            new Entry("청흔", "cheong_heun", "구정회, 무당 삼절검"),
            new Entry("당철영", "dang_cheolyeong", "사천당문 막내아들"),
            new Entry("마진가", "ma_jinga", "천무학관주"),
            new Entry("장우양", "jang_uyang", "중양표국 국주"),
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
            new Entry("독고령", "dokgo_ryeong", "독안봉"),
            new Entry("혁월린", "hyeok_wolrin", "초대 학관주, 염도·빙검의 사부"),
            new Entry("공손일취", "gongson_ilchwi", "검존, 원로원 원주"),
            new Entry("단목우", "danmok_u", "천기룡, 천무구룡"),
            new Entry("모경", "mo_gyeong", "폭랑귀도, 폭랑삼십육도 대장")
        };

        public static Entry Find(string name)
        {
            foreach (var e in All) if (e.Name == name) return e;
            return null;
        }
    }
}
