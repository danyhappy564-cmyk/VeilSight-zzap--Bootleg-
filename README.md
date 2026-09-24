### ⚠️ IMPORTANT NOTICE / DISCLAIMER

**Original Author:** Sherrifpowpow
**Original Repository:** VeilSight
**Original Link:** https://github.com/Sherrifpowpow/VeilSight
**License:** MIT
**This Port By:** R_F (danyhappy564-cmyk) — unofficial, AI-assisted port. Not affiliated with or endorsed by the original author.

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

---

## 변경 이력

- 2026-09-25 04:41 (KST) — v1.2.2 실전 확인 (세관 19~20시): 손전등 → BRIGHT, 가시광 레이저만 → DIM, IR 라이트/IR 레이저 → 밝기 변화 없음,
  전부 끔 → 원래 밝기. 모드 7가지 조합이 전부 정확히 구분됨 (코드 변경 없음)
- 2026-09-25 04:24 (KST) — **v1.2.2: 손전등/레이저 판정 방식 변경** (두 번째 실전 로그, 세관·팩토리 야간)
  - 확인됨: 소음 사격 → DIM(어스름), 야시경 봇 우회(`bypass=night-vision`), 교전 우회(`bypass=combat`),
    지연이 끝나서 발견(`released`) 전부 작동, 팩토리 야간 예외 0건
  - 문제: 레이저만 켠 상태가 한 번도 "레이저"로 안 잡힘. 대신 "손전등만 켜짐"이 찍힘 → IR 라이트나 레이저 전용 모드에 들어 있는
    작은 조명 오브젝트를 손전등으로 오인했을 가능성
  - 수정: 부품 모드 안의 파츠 이름으로 구분(`light_0`=손전등, `vis_0`=가시광 레이저, `il_0`=IR 라이트, `ir_0`=IR 레이저, SAIN과 같은 규칙).
    이름 규칙이 없는 모드 부품만 예전 방식(조명 오브젝트 검색)으로 판정. 진단 로그에 `devices=`(켜진 모드 종류) 추가
- 2026-09-25 03:31 (KST) — **v1.2.1: 첫 실전 로그 반영 (Woods 야간)** — 기능 변화 없음, 진단 로그만 정리
  - 확인됨: SAIN 감지 + 지연 0.6배 적용(최대 4.00→2.40초), 사격 시 BRIGHT, 달리기/정지 노출 가감, 재발견 기억 작동, 예외 0건
  - 조명 검색은 400~490개에 0.2ms — 끊김 걱정은 근거 없음으로 확인
  - 자동 사격 때 `SHOT` 로그가 한 발마다 찍히던 것(한 판에 361줄)을 1초에 한 줄 요약으로 줄임
  - 판정 로그에 봇 이름(`bot=`)을 붙이고, 지연이 끝나서 봇이 나를 발견한 순간을 `VISIBILITY released`로 따로 남김
    (전에는 발견 직후가 `bypass=memory`로만 찍혀서 구분이 안 됐음)
- 2026-09-25 03:11 (KST) — 빌드 편의 개선: `VeilSight.sln`(Visual Studio 솔루션 파일)과 더블클릭용 `build.bat` 추가,
  Release 빌드 시 `BepInEx\plugins\VeilSight`로 자동 복사가 기본값이 됨. Visual Studio에서 csproj를 바로 빌드할 때
  NuGet 복원이 안 돼서 `NETSDK1004 project.assets.json 없음` 에러가 나던 문제 대응
- 2026-09-25 02:56 (KST) — **v1.2.0: 봇 인지 규칙 보강 + 기능 추가** (자세한 건 아래 "v1.2.0에서 추가/수정된 것")
  - 숨었다 나왔다를 반복하면 봇이 매번 처음부터 다시 늦게 알아채던 문제 수정 → 한 번 제대로 본 봇은 10초 동안 기억함
  - SAIN이 깔려 있으면 지연을 60%로 줄임 (SAIN도 어둠 은신 계산을 따로 해서 효과가 두 번 걸리던 문제)
  - 야시경(NVG)을 켠 봇은 어둠 지연을 안 받게 함
  - 나를 맞혔거나 나한테 맞은 봇은 8초 동안 지연 없음
  - 소음기 없이 쏘면 1.5초 동안 완전 노출, 소음기 달고 쏘면 어스름(DIM) 취급
  - 가시광 레이저를 켜면 최소 어스름(DIM) 취급 (IR 레이저는 제외)
  - 달리면 노출 증가, 가만히 서 있으면 노출 감소
  - 맵 조명 검색 주기를 설정으로 뺌 + 진단 로그에 검색 시간 표시 (조명 많은 맵 끊김 확인용)
- 2026-09-11 — SPT 4.1.5 포팅 (코드 변경 없이 버전 표기 + 빌드 안전장치만 추가)

---

# VeilSight (SPT 4.1 포팅)

**어둠에 숨으면 적이 나를 늦게 인지합니다.**

주변 밝기를 실시간으로 재서, 어두운 곳에 있으면 봇이 나를 "확실히 봤다"고 판정하는 데
걸리는 시간을 거리에 비례해서 늦춥니다. 밝은 데로 나가거나 손전등을 켜면 그 이점은
바로 사라집니다. 투명해지는 게 아니라 **인지 확정이 몇 초 늦어지는 것**뿐입니다.

> **원작자 · 원본**
> **Sherrifpowpow** — MIT License
>
> 이 저장소는 위 원작의 **포크**입니다. 1.1.0까지는 **SPT 4.1 포팅만** 했고,
> **1.2.0부터 이 포크만의 규칙 보강/기능 추가**가 들어갔습니다 (아래 절 참고).
> 새로 추가된 값은 전부 F12에서 0/끄기로 바꾸면 원작 동작으로 돌아갑니다.

---

## v1.2.0에서 추가/수정된 것

전부 **"봇이 나를 봤다고 확정하는 순간"**(`EnemyInfo.SetVisible`, 게임에서 봇의 "적이 보인다"
상태를 켜고 끄는 함수)을 늦출지 말지 판단하는 규칙에 들어갑니다. 새로 건드리는 게임 함수는
`Player.OnMakingShot`(플레이어가 총을 쏠 때 실행되는 함수) 하나뿐이고, 사격 시각을 기록만 합니다.

| 항목 | 무엇이 바뀌나 | 끄는 법 |
|---|---|---|
| **재발견 기억** | 봇이 나를 실제로 한 번 봤으면, 놓친 뒤 10초 안에 다시 보면 **바로** 봄. 예전엔 어둠 속에서 살짝 나왔다 숨기를 반복하면 매번 2~4초를 새로 벌 수 있었음 | `Awareness / ReacquireMemory` = 0 |
| **야시경 봇** | NVG를 **켜고 있는** 봇은 어둠 지연 없음 | `Awareness / NightVisionBypass` = false |
| **교전 중** | 내가 그 봇을 맞혔거나 그 봇이 나를 맞힌 뒤 8초 동안은 지연 없음 | `Awareness / CombatBypass` = 0 |
| **SAIN 보정** | SAIN이 설치돼 있으면 지연 시간 × 0.6. SAIN이 자체적으로 "어두우면 늦게 알아챔" 계산을 하기 때문에 그대로 두면 두 번 걸림 | `Compatibility / SainDelayMultiplier` = 1 |
| **총구 화염** | 소음기 없이 쏘면 1.5초 동안 BRIGHT(완전 노출). 소음기 있으면 같은 시간 동안 최소 DIM | `Exposure / MuzzleFlashDuration` = 0, `SuppressedShotDim` = false |
| **가시광 레이저** | 빨강/초록 레이저가 켜져 있으면 최소 DIM. IR 레이저·IR 라이트는 영향 없음 | `Exposure / VisibleLaserExposure` = false |
| **이동** | 달리면 노출 +10%, 완전히 멈춰 있으면 −5% | `Exposure / MovementWeight` = 0 |
| **조명 검색 주기** | 맵 조명 전체 검색 주기(기본 10초, 원작과 동일)를 설정으로 뺌. 진단 로그를 켜면 `LIGHT_REFRESH lights=… ms=…`로 검색에 걸린 시간이 찍힘 | 기본값이 원작 동작 |

SAIN 감지 여부는 레이드 첫 판정 때 BepInEx 로그에 한 줄 찍힙니다:
`[VeilSight] SAIN detected=True delayMultiplier=0.60`

---

## 4.1 포팅에서 바뀐 것 — 코드는 안 바뀌었습니다

솔직하게 적으면, **게임 코드 쪽은 고칠 게 없었습니다.**

이 모드가 EFT에서 건드리는 건 후킹 **딱 하나**입니다:

```csharp
AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.SetVisible), new[] { typeof(bool) });
```

실제 SPT 4.1.5 `Assembly-CSharp.dll`에 대고 확인했습니다:

| 쓰는 것 | 4.1.5 상태 |
|---|---|
| `EnemyInfo.SetVisible(bool value)` | 그대로 있음 — **파라미터 이름까지 `value`로 동일** |
| `EnemyInfo.Distance` / `.Person` / `.Owner` | 그대로 |
| `TacticalComboVisualController.LightMod` | 그대로 (`EFT.InventoryLogic.LightComponent`) |
| `PlayerBones.WeaponRoot` | 그대로 (`EFT.BifacialTransform`) |
| `LaserBeam`, `Player.FirearmController` | 그대로 |
| `EFT.EnvironmentEffect`, `EFT.Weather` | 그대로 |

파라미터 이름을 굳이 확인한 이유는 **Harmony가 패치 인자를 이름으로 바인딩**하기 때문입니다.
`SetVisible`의 인자가 4.1에서 `value`가 아닌 다른 이름으로 바뀌었으면, 컴파일은 되는데
게임에서 패치가 조용히 안 붙습니다. 4.1.5 어셈블리를 직접 디컴파일해서 `value` 맞는 것까지
봤습니다.

문자열로 타입/멤버를 찾는 리플렉션 호출은 이 모드에 **없습니다**. 위 `AccessTools.Method`
한 줄이 전부고, 그마저 `nameof` + 타입 지정이라 컴파일 단계에서 검증됩니다.

그래서 실제 diff는 버전 표기 쪽입니다:

| 파일 | 4.0 | 4.1 |
|---|---|---|
| `Plugin.cs` | `[BepInDependency("com.SPT.core", "4.0.13")]` | `"4.1.0"` |
| `VeilSight.csproj` | `VersionPrefix 1.0.1` | `1.1.0` |
| `tools/Package-Release.ps1` | `$SptVersion = "4.0.13"` | `"4.1.5"` |

여기에 빌드 안전장치 하나(`EnsureRealSptReflection`, 아래)와 `SptRoot` 기본값을 추가했습니다.

---

## 설치 — 직접 빌드하셔야 합니다

SPT 런처는 플러그인이 참조하는 **`spt-reflection` 어셈블리 버전**을 읽어서 "몇 버전용으로
빌드됐는지"를 판정하고, 안 맞으면 게임 실행 자체를 막습니다:

> These mods were built for a different version of SPT than the one you are running (4.1.5):
> VeilSight.dll (built for SPT 1.0.0)

즉 **남이 빌드해준 DLL을 그냥 받아 쓸 수 없고**, 본인 SPT 설치본에 대고 빌드해야 합니다.
방법은 셋 중 편한 걸 고르면 됩니다:

1. **`build.bat` 더블클릭** — 제일 간단합니다.
2. **Visual Studio로 `VeilSight.sln` 열기** → 상단 구성을 `Release`로 → `빌드 > 솔루션 빌드`.
   (csproj를 직접 열어 빌드하면 NuGet 복원이 안 돼서 `NETSDK1004` 에러가 날 수 있습니다 — sln으로 여세요.
   그래도 나면 `솔루션 탐색기`에서 솔루션 우클릭 → `NuGet 패키지 복원` 한 번 후 다시 빌드)
3. 명령줄: `dotnet build VeilSight.sln -c Release`

**Release 빌드는 자동으로 `BepInEx\plugins\VeilSight\VeilSight.dll`에 복사**되고,
`release\VeilSight-버전.zip`도 같이 만들어집니다. 복사를 원치 않으면 `-p:DeployVeilSight=false`.

`SptRoot` 기본값이 `E:\SPT 4.1` 이라 그대로 빌드하면 되고, 경로가 다르면:

```
dotnet build VeilSight.sln -c Release -p:SptRoot="D:\내SPT경로"
```

빌드 결과물은 `bin\Release\VeilSight.dll` 입니다. 이걸

```
BepInEx\plugins\VeilSight\VeilSight.dll
```

에 들어가야 하는데, 위에 적은 대로 Release 빌드면 알아서 복사됩니다.

**서버 모드는 없습니다.** 클라 플러그인 하나가 전부입니다.

껍데기 `spt-reflection.dll`(버전 1.0.0.0)에 대고 빌드하면 **컴파일은 되고 런처에서만 막히는**
DLL이 나옵니다. 그걸 막으려고 `EnsureRealSptReflection` 타겟을 넣어놨습니다 — 껍데기를
발견하면 빌드가 그 자리에서 실패하고 이유를 말해줍니다.

---

## 어떻게 동작하나

1. **0.35초마다 한 번씩** 플레이어 위치의 밝기를 잽니다 (햇빛/시간대/날씨, 실내 조명,
   근처 조명 기구, 자세, 손전등).
2. 그 결과를 **DARK / DIM / BRIGHT** 세 단계로 나눕니다.
3. 봇이 나를 봤다고 판정하려 하면(`SetVisible(true)`), 밝기 단계와 거리에 따라 그 판정을
   **잠깐 보류**합니다.

| 밝기 | Forgiving (기본) | Standard |
|---|---|---|
| DARK | 기본 2.20초 + 거리 × 0.022, 최대 4.00초 | 1.76초 + 거리 × 0.0176, 최대 3.20초 |
| DIM | 기본 1.50초 + 거리 × 0.018, 최대 3.00초 | 1.20초 + 거리 × 0.0144, 최대 2.40초 |
| BRIGHT | 지연 없음 | 지연 없음 |

**6m 이내는 두 프리셋 다 무조건 지연 없음**입니다. 코앞에서 안 보이는 건 말이 안 되니까요.

보류는 어디까지나 보류라서, 시간이 지나면 **결국 무조건 봅니다.** 영구 은신이 아닙니다.

**Standard**는 같은 곡선을 20% 짧게 씁니다. 원작자는 SAIN 사용자에게 이쪽을 권했습니다.
1.2.0부터는 SAIN이 깔려 있으면 `SainDelayMultiplier`(기본 0.6)가 **자동으로 추가 적용**되므로,
SAIN 사용자도 `Forgiving` 그대로 두고 배율만 조절하는 걸 권합니다 (Standard까지 겹치면 0.8 × 0.6 = 0.48배).
VeilSight는 SAIN 설정을 읽거나 건드리지 않고, 설치 여부만 확인합니다.

### 손전등 / 레이저

**손전등을 켜면 즉시 완전 노출**로 칩니다 (`FlashlightOverride` 로 끌 수 있습니다).
1.2.0부터 **가시광 레이저만 켠 것도 최소 DIM**으로 칩니다 (`VisibleLaserExposure`). IR 장비는 여전히 무시합니다.
1.2.2부터 손전등/레이저/IR 구분은 부품 모드의 파츠 이름(`light_0`/`vis_0`/`il_0`/`ir_0`)으로 하고,
이 규칙이 없는 모드 부품만 예전처럼 조명 오브젝트로 판정합니다.

### 노출 게이지

화면에 작은 주황색 막대가 뜹니다. 왼쪽이 DARK, 가운데가 DIM, 오른쪽이 BRIGHT입니다.
노출이 높아질수록 막대가 밝아집니다. **보기 편하라고 부드럽게 움직이는 것뿐이고,
실제 판정은 언제나 최신 샘플을 씁니다** — 게이지가 늦게 따라와도 판정은 이미 바뀌어 있습니다.

위치·크기·부드러움 전부 조절 가능하고, 아예 끌 수도 있습니다.

---

## F12 설정

레이드 중 **F12 → `VeilSight`**. 설정 파일을 직접 고쳐도 됩니다:

`BepInEx\config\com.sherrifpowpow.veilsight.cfg`

| 항목 | 기본값 | 설명 |
|---|---|---|
| `General / Enabled` | `true` | 모드 전체 on/off |
| `General / SampleInterval` | `0.35` | 밝기 측정 주기(초). 0.25~0.50 |
| `General / CloseRangeBypass` | `6` | 이 거리(m) 이하는 지연 없음 |
| `Balance / DifficultyPreset` | `Forgiving` | `Forgiving` / `Standard` |
| `Exposure / FlashlightOverride` | `true` | 손전등 켜면 완전 노출 취급 |
| `Exposure / PoseWeight` | `0.10` | 낮은 자세로 줄일 수 있는 노출 최대치 |
| `Meter / ShowMeter` | `true` | 게이지 표시 |
| `Meter / SmoothTime` | `0.09` | 게이지 부드러움(초). 표시에만 영향 |
| `Meter / Scale` | `1.0` | 게이지 크기 배율. 0.5~2.0 |
| `Meter / PositionX` | `0.50` | 가로 위치. 0=왼쪽, 1=오른쪽 |
| `Meter / PositionY` | `0.88` | 세로 위치. 0=위, 1=아래 |
| `Awareness / ReacquireMemory` | `10` | 한 번 본 봇이 다시 볼 때 지연 없는 시간(초). 0~30, 0=원작 |
| `Awareness / CombatBypass` | `8` | 서로 맞힌 뒤 지연 없는 시간(초). 0~30, 0=끄기 |
| `Awareness / NightVisionBypass` | `true` | NVG 켠 봇은 지연 없음 |
| `Compatibility / SainDelayMultiplier` | `0.6` | SAIN 설치 시에만 곱하는 지연 배율. 0~1, 1=원작 |
| `Exposure / MuzzleFlashDuration` | `1.5` | 비소음 사격 후 완전 노출 시간(초). 0~5, 0=끄기 |
| `Exposure / SuppressedShotDim` | `true` | 소음 사격 후 같은 시간 동안 최소 DIM |
| `Exposure / VisibleLaserExposure` | `true` | 가시광 레이저 켜면 최소 DIM |
| `Exposure / MovementWeight` | `0.10` | 달리기 +값, 정지 −값/2. 0~0.5, 0=원작 |
| `Performance / LightRefreshInterval` | `10` | 맵 조명 전체 검색 주기(초). 5~60 |
| `Diagnostics / DiagnosticsEnabled` | `false` | 로그에 판정 과정 전부 기록. 평소엔 끄세요 |

---

## 안전장치 (fail-open)

조명 데이터를 못 읽거나, 샘플이 너무 오래됐거나, 예외가 터지거나, 모드가 꺼져 있으면
**전부 "지연 없음"으로 빠집니다.** 버그 때문에 실수로 무적이 되는 일은 없습니다.

구체적으로: 샘플이 `SampleInterval × 3`(최소 1초)보다 오래됐으면 그 샘플은 버리고 그냥
원래대로 보이게 둡니다.

---

## 호환성 · 알려진 한계

- 같은 `EnemyInfo.SetVisible` 을 건드리는 다른 모드와는 충돌할 수 있습니다.
  대표적으로 **That's Lit** 계열 — 같이 쓰지 마세요.
- 원작자가 SAIN, Amands's Graphics, Better Night Skies, Dynamic Maps, Game Panel HUD,
  Fontaine's FOV Fix 와 같이 돌리면서 테스트했습니다. HUD가 겹치면 게이지 위치·크기로
  피하면 됩니다.
- **Factory**의 비스듬한 천장 빛줄기 중 일부는 실제 조명 오브젝트 없이 순수 시각 효과라,
  그 빛줄기를 가로질러도 게이지가 그 순간에 반응하지 않을 수 있습니다. 주변 밝기 자체는
  제대로 인식합니다. 순수 장식용 발광 표면도 마찬가지입니다. (맵 제작 쪽 한계입니다)
- **Labs, Factory 낮/밤, Labyrinth** 는 조명 구조가 특이해서 따로 처리가 들어가 있습니다.
- 멀티플레이(Fika) 동기화는 범위 밖입니다.
- **알려진 한계 (원작부터 있던 것):** 지연이 걸려 있는 동안에도 게임 쪽 시야 계산
  (`EnemyInfo.CheckLookEnemy`, 봇이 적을 살피는 함수)은 그대로 돌아서, 봇의 "마지막으로 본
  위치" 기록과 분대 내부 보고는 갱신될 수 있습니다. "봤다" 확정만 늦추는 구조라서 생기는 부분입니다.

---

## 건드리는 범위

**적이 나를 "봤다"고 확정하는 단계 하나**만 바꿉니다 (1.2.0의 사격 패치는 사격 시각 기록만 합니다). 청각, 수색, 조준, 사격, 기억,
그 외 봇 행동은 일절 안 건드립니다. BRIGHT면 아예 개입 안 하고, DIM/DARK만 각자
상한이 있는 곡선을 씁니다.

---

## 라이선스

원작 **VeilSight** 는 **Sherrifpowpow** 가 만들었고 **MIT License** 로 공개돼 있습니다.
이 포크도 동일하게 MIT입니다. 전문은 `LICENSE` 파일에 있습니다.

원작자 주석: That's Lit 에서 영감을 받았지만 **코드는 한 줄도 가져오지 않은** 독립 구현입니다.
