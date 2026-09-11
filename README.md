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

# VeilSight (SPT 4.1 포팅)

**어둠에 숨으면 적이 나를 늦게 인지합니다.**

주변 밝기를 실시간으로 재서, 어두운 곳에 있으면 봇이 나를 "확실히 봤다"고 판정하는 데
걸리는 시간을 거리에 비례해서 늦춥니다. 밝은 데로 나가거나 손전등을 켜면 그 이점은
바로 사라집니다. 투명해지는 게 아니라 **인지 확정이 몇 초 늦어지는 것**뿐입니다.

> **원작자 · 원본**
> **Sherrifpowpow** — MIT License
>
> 이 저장소는 위 원작의 **포크**입니다. 기능·수치·밸런스는 하나도 안 건드렸고,
> **SPT 4.1에서 빌드·동작하도록 포팅**한 것이 전부입니다.

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

즉 **남이 빌드해준 DLL을 그냥 받아 쓸 수 없고**, 본인 SPT 설치본에 대고 빌드해야 합니다:

```
dotnet build VeilSight.csproj -c Release
```

`SptRoot` 기본값이 `E:\SPT 4.1` 이라 그대로 빌드하면 되고, 경로가 다르면:

```
dotnet build VeilSight.csproj -c Release -p:SptRoot="D:\내SPT경로"
```

빌드 결과물은 `bin\Release\VeilSight.dll` 입니다. 이걸

```
BepInEx\plugins\VeilSight\VeilSight.dll
```

에 넣으면 됩니다. `-p:DeployVeilSight=true` 를 붙이면 빌드가 알아서 저 위치로 복사합니다.

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

**Standard**는 같은 곡선을 20% 짧게 씁니다. SAIN 같은 걸로 봇을 빡세게 굴리고 있으면
이쪽이 맞습니다. VeilSight는 SAIN 설정을 읽거나 건드리지 않습니다 — 본인 AI 세팅에 맞는
프리셋을 직접 고르는 방식입니다.

### 손전등 / 레이저

**손전등을 켜면 즉시 완전 노출**로 칩니다. **레이저만 켠 건 노출로 안 칩니다.**
(`FlashlightOverride` 로 끌 수 있습니다.)

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

---

## 건드리는 범위

**적이 나를 "봤다"고 확정하는 단계 하나**만 바꿉니다. 청각, 수색, 조준, 사격, 기억,
그 외 봇 행동은 일절 안 건드립니다. BRIGHT면 아예 개입 안 하고, DIM/DARK만 각자
상한이 있는 곡선을 씁니다.

---

## 라이선스

원작 **VeilSight** 는 **Sherrifpowpow** 가 만들었고 **MIT License** 로 공개돼 있습니다.
이 포크도 동일하게 MIT입니다. 전문은 `LICENSE` 파일에 있습니다.

원작자 주석: That's Lit 에서 영감을 받았지만 **코드는 한 줄도 가져오지 않은** 독립 구현입니다.
