# 성능 점검과 최적화 (2026-11)

기기 프로파일링 없이 코드 분석과 PC .NET 측정(일반 49 진행 세이브)으로 정리했다. 안드로이드(IL2CPP)는 대략 3~10배 느리다고 본다.

## 진단 요약

| 단계 | 문제 | 상태 |
|---|---|---|
| 매 프레임 | 전투 중에도 강호 로비(전체 화면 그림)가 켜져 그려지고 0.5초 갱신이 돌았다 | A 완료 |
| 매 프레임 | 로비 배지 갱신 0.5초마다 PC 약 2.5ms(업적 0.74ms·기록 알림 1.6ms) | A 완료 |
| 매 프레임 | `WorldScreen.RefreshStatusChips`가 매 프레임 문자열을 만들었다 | A 완료 |
| 그리기 | `UiFactory.Label`이 크기 28 이하에도 best fit(효과 없음)을 켰다 | A 완료 |
| 그리기 | 모든 UI가 캔버스 하나라 움직이는 곳 하나가 전체를 다시 묶는다 | B 완료 |
| 이미지 | 842장 중 291장이 4의 배수가 아님, 안드로이드 압축 형식 미지정, 그림 캐시 해제 없음 | C 완료 |
| 계산 | 추천 장비 PC 260ms, 자동 배치 첫 계산 PC 64ms(버튼·준비 창에서 멈춤) | D 예정 |

## A단계 (적용)

- `MainScreen.ShowBattleOverlay/HideBattleOverlay`: 강호 탭에서 전투(스테이지·의뢰·학관·탑·승급)를 여는 동안 강호 패널을 끈다. 끝나면 다시 켜며 `LandscapeHome.OnEnable`이 갱신한다. 기록 탭의 이야기 재생은 그 탭의 열린 창을 지키려고 그대로 둔다.
- `LandscapeHome`: 0.5초마다 `Core/SaveFingerprint`(은자·경험치·진행·수령·본 것 목록 길이·일일 진행)만 비교하고, 바뀌었을 때와 6초마다 한 번만 전체 배지를 다시 계산한다.
- `WorldScreen.RefreshStatusChips`: 은자·레벨·경험치·검룡위가 그대로면 아무것도 하지 않는다.
- `UiFactory.Label`: best fit은 크기 29 이상일 때만 켠다(28 이하는 최소=최대라 화면이 같다). 최소 크기를 낮춰 줄이기를 원하는 곳(전투 명령 설명, 무공 장착 칸)은 직접 켠다.

## B단계 (적용)

`UiFactory.OwnCanvas(go, clickable)`로 자주 움직이는 부분에 하위 캔버스를 붙였다. 정렬 덮어쓰기 없이 계층 순서대로 그려지므로 겹치는 순서는 그대로이고, 누를 수 있는 곳은 `GraphicRaycaster`를 함께 단다.

- 전투 화면 전체(`BattleOverlay`): 전투 연출이 강호·도움말 등 나머지 화면을 다시 묶지 않는다.
- 전투 안: 적 카드(`Cards`)·아군 패널(`Party`)·효과층(`Fx`, 피해 숫자·이펙트, 누르지 않음)을 각각 분리했다. 카드가 돌진·흔들림·게이지 변화를 해도 HUD·배경은 다시 묶지 않는다.
- 학관 마당 시야(`CampusViewport`): 걷기·카메라 이동은 마당만 다시 묶는다. 조이스틱·버튼은 바깥 캔버스에 남는다.
- 로비 입화(`CharacterViewport`): 인물 교체 페이드·이동이 로비 전체를 다시 묶지 않는다.

확인 필요(기기): 하위 캔버스 안의 버튼(적·아군 선택, 학관 안내원) 터치와 RectMask2D 잘림.

## C단계 (적용)

- 압축 형식: `ProjectSetup`이 안드로이드 텍스처 압축을 ASTC로 지정한다(`EditorUserBuildSettings.androidBuildSubtarget`). ETC2는 가로·세로가 4의 배수가 아닌 그림(842장 중 291장)을 압축하지 못해 비압축으로 넣을 수 있다. 설정 메뉴(BiRyeoDo ▸ Setup Project)나 APK 빌드 메뉴를 한 번 실행하면 적용되고, 처음에는 텍스처를 다시 가져오느라 시간이 걸린다.
- 전투 SD 크기: `Editor/ArtImportRules`가 `Resources/Art/Battle/`(원본 약 1024~1280px)을 최대 512px로 가져온다. 화면에는 수백 픽셀로만 그려지며 메모리는 약 1/4이다. 측정 좌표를 쓰는 아틀라스(`Exploration`, `Readable`)와 입화·배경은 건드리지 않는다.
- 큰 그림 캐시: `ArtLibrary`가 배경·입화·전투 SD를 최근 사용 40장까지만 캐시한다. 밀려난 것은 파괴하지 않고, 전투가 끝날 때와 화면을 바꿀 때 `ArtLibrary.ReleaseUnused`가 `Resources.UnloadUnusedAssets`로 아무도 쓰지 않는 것만 내린다(내릴 것이 없으면 아무것도 하지 않는다).

확인 필요(기기): 전투 SD가 정보 창·도감에서 흐려 보이지 않는지(흐리면 `ArtImportRules.BattleMaxSize`를 1024로), 전투 종료·탭 이동 때 짧은 멈춤이 거슬리지 않는지.
