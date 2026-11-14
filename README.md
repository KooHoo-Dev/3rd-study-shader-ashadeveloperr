# 3rd-unity-6-templete

> 과제 안내: [작업지시서 — 나만의 셰이더 제작](ASSIGNMENT.md)
> 셰이더 원리 설명: [SUBMISSION.md](SUBMISSION.md)

## 내 셰이더 목록

| # | 셰이더 | 분야 | 데모 씬 | 매뉴얼 |
| --- | --- | --- | --- | --- |
| 01 | StarTrailSky (별의 일주 운동) | 스카이박스 | `Assets/00. Scenes/MyShader_StarTrailSky.unity` | [바로가기](#01-startrailsky-매뉴얼) |
| 02 | Blizzard (눈보라) | 파티클 | `Assets/00. Scenes/MyShader_Blizzard.unity` | [바로가기](#02-blizzard-매뉴얼) |

---

## 01. StarTrailSky 매뉴얼

밤하늘의 별이 천구 북극을 중심으로 돌며 장노출 사진처럼 긴 궤적을 남기는 스카이박스입니다.

### 파일 위치
| 경로 | 설명 |
| --- | --- |
| `Assets/02. MyShaders/01_StarTrailSky/StarTrailSky.shader` | 셰이더 (`MyShaders/StarTrailSky`) |
| `Assets/02. MyShaders/01_StarTrailSky/StarTrailSky.mat` | 머티리얼 — **값은 여기서 조정합니다** |
| `Assets/00. Scenes/MyShader_StarTrailSky.unity` | 데모 씬 |

### 보는 방법
- 데모 씬을 열고 Game 뷰를 봅니다. 별은 **Play 모드가 아니어도** 시간에 따라 돕니다. Scene 뷰에서 움직임이 멈춰 있다면 Scene 뷰 상단의 효과 메뉴에서 `Always Refresh`를 켭니다.
- 다른 씬에 쓰려면 `Window > Rendering > Lighting > Environment > Skybox Material`에 `StarTrailSky.mat`을 넣습니다.

### 조정하는 곳: `StarTrailSky.mat` 인스펙터
| 그룹 | 프로퍼티 | 무엇이 바뀌나 | 팁 |
| --- | --- | --- | --- |
| Sky Gradient | Zenith / Horizon / Ground Color | 머리 위 하늘, 지평선 근처, 지평선 아래 색 | 지평선을 살짝 밝게 하면 도시 불빛 느낌 |
| | Horizon Falloff | 지평선 색이 위로 얼마나 퍼지는지 | 클수록 지평선 띠가 얇아짐 |
| Celestial Pole | Pole Elevation | 북극(회전 중심)의 높이 = 관측 위도 | 90이면 머리 위에서 동심원, 0이면 지평선에서 회전 |
| | Pole Azimuth | 북극이 있는 방향 (0 = +Z) | 카메라가 바라보는 쪽으로 맞추면 동심원이 화면에 보임 |
| | Rotation Speed | 초당 회전 각도, 부호 = 방향 | 실제 하늘은 매우 느리므로 1~3 정도가 보기 좋음 |
| Stars | Ring Count | 하늘을 나누는 고리 수 | 많을수록 별이 촘촘하고 작아짐 (무거워짐) |
| | Cells per Ring at Equator | 고리 하나를 나누는 칸 수 | **성능에 가장 크게 영향**. 버벅이면 먼저 줄이기 |
| | Star Probability | 칸에 별이 있을 확률 | 별 개수 조절 |
| | Star Size / Brightness | 별 크기(도), 밝기 | |
| | Twinkle | 반짝임 세기 | 0이면 반짝이지 않음 |
| | Color Variation | 푸른 별~주황 별 색 차이 | 0이면 모두 흰 별 |
| Trails | Trail Length | 잔상 길이(도) | 0이면 잔상 없이 별만, 크면 긴 원호. **길수록 무거워짐** |
| | Trail Brightness | 잔상 밝기 | |
| | Trail Falloff | 잔상이 사라지는 빠르기 | 클수록 꼬리가 짧고 급하게 사라짐 |

---

## 02. Blizzard 매뉴얼

바람에 휘날리는 눈보라 파티클입니다. 텍스처 없이 셰이더가 눈송이 모양을 계산하며, 가까운 눈은 크게 번지고 먼 눈은 안개 속으로 사라집니다.

### 파일 위치
| 경로 | 설명 |
| --- | --- |
| `Assets/02. MyShaders/02_Blizzard/Blizzard.shader` | 셰이더 (`MyShaders/Blizzard`) |
| `Assets/02. MyShaders/02_Blizzard/Blizzard.mat` | 눈송이 머티리얼 (`Snow_Far`, `Snow_Mid`, `Snow_Near`가 사용) |
| `Assets/02. MyShaders/02_Blizzard/Blizzard_Gust.mat` | 눈가루 덩어리 머티리얼 (`Snow_Gusts`가 사용) |
| `Assets/02. MyShaders/02_Blizzard/Demo/` | 데모 씬의 바닥·기둥용 머티리얼 |
| `Assets/02. MyShaders/02_Blizzard/Editor/BlizzardDemoBuilder.cs` | 데모 씬과 머티리얼을 처음부터 다시 만드는 스크립트 |
| `Assets/00. Scenes/MyShader_Blizzard.unity` | 데모 씬 |

### 보는 방법
데모 씬을 열고 **Play**를 누릅니다. (편집 모드에서는 Hierarchy에서 `Blizzard`의 파티클 시스템을 선택해야 Scene 뷰에서 재생됩니다.)

### 어디서 무엇을 조정하나
눈보라는 세 곳에서 조정합니다.

| 바꾸고 싶은 것 | 조정하는 곳 |
| --- | --- |
| 눈송이 **모양·흐림·꼬리·밝기** | `Blizzard.mat` / `Blizzard_Gust.mat` 인스펙터 |
| 눈의 **양·크기·바람 방향과 세기** | Hierarchy `Blizzard > Snow_*` 파티클 시스템 |
| **시야(얼마나 뿌연지)·하늘색·빛** | Lighting 창의 안개, `Main Camera`, `Directional Light` |

#### ① 머티리얼 (`Blizzard.mat`, `Blizzard_Gust.mat`)
| 그룹 | 프로퍼티 | 무엇이 바뀌나 | 팁 |
| --- | --- | --- | --- |
| Color | Base Color / Opacity | 눈 색, 전체 불투명도 | |
| Lighting | Ambient Strength | 환경광을 얼마나 받는지 | 어두운 씬에서 눈이 너무 밝으면 낮추기 |
| | Main Light Strength | 해(Directional Light)를 얼마나 받는지 | |
| | Forward Scatter | 해를 마주 볼 때 눈이 밝아지는 정도 | 역광 장면에서 효과가 큼 |
| Shape | Edge Softness | 초점이 맞은 눈송이의 가장자리 부드러움 | **눈이 너무 선명해 보이면 올리기** |
| | Edge Irregularity | 가장자리를 울퉁불퉁하게 | 0이면 완전한 원 |
| | Tumble Twinkle | 눈송이가 구르며 깜빡이는 정도 | |
| | Wispiness | 노이즈로 구멍을 뚫는 정도 | 눈가루 덩어리용. 눈송이에는 0 |
| | Noise Scale / Scroll Speed | 노이즈 무늬 크기, 흐르는 속도 | Wispiness가 0보다 클 때만 의미 있음 |
| Motion Stretch | Stretch (exposure sec) | 속도 방향으로 늘어나는 정도 (카메라 노출 시간) | **꼬리가 길어 막대처럼 보이면 낮추기** |
| | Max Stretch | 최대 늘어남 (크기의 몇 배) | 빠른 눈이 지나치게 길어지는 것을 막음 |
| Depth Of Field | Focus Distance | 가장 또렷하게 보이는 거리(m) | |
| | In-Focus Range | 초점 거리 앞뒤로 또렷한 범위(m) | 넓히면 전체가 또렷, 좁히면 몽환적 |
| | Far Blur Distance | 먼 쪽이 완전히 흐려지기까지 거리(m) | |
| | Blur Size Grow | 흐린 눈송이가 커지는 정도 | 0이면 흐려도 크기 그대로 |
| Fading | Soft Particles | 바닥·물체와 닿는 곳을 부드럽게 | URP 에셋의 **Depth Texture**가 꺼져 있으면 끄기 |
| | Soft Particle Distance | 부드럽게 사라지는 깊이 범위 | |
| | Near Fade Start / Range | 카메라 바로 앞 눈이 사라지기 시작하는 거리와 범위 | 카메라에 눈이 툭 붙어 보이면 올리기 |

#### ② 파티클 시스템 (Hierarchy `Blizzard` 아래)
| 오브젝트 | 역할 |
| --- | --- |
| `Snow_Far` | 멀리 촘촘하게 날리는 작은 눈 (파티클 수가 가장 많음) |
| `Snow_Mid` | 초점이 맞는 거리의 주된 눈 |
| `Snow_Near` | 가끔 카메라를 스치는 큰 흐린 눈 |
| `Snow_Gusts` | 바람에 휩쓸리는 눈가루 덩어리 |

| 바꾸고 싶은 것 | 모듈 > 항목 |
| --- | --- |
| 눈의 양 | `Emission > Rate over Time` (필요하면 `Main > Max Particles`도 같이 올리기) |
| 눈송이 크기 | `Main > Start Size` |
| 바람 방향·세기 | `Velocity over Lifetime > Linear` (X = 옆바람, Y = 낙하, Z = 앞뒤). 세 레이어를 같이 바꿔야 자연스러움 |
| 휘몰아치는 정도 | `Noise > Strength`(세기), `Frequency`(소용돌이 크기), `Scroll Speed`(변하는 속도) |
| 눈이 내리는 영역 | `Shape > Scale` (상자 크기), 오브젝트 위치 |

> **주의:** `Renderer > Custom Vertex Streams`의 목록과 순서(Position, Color, UV, StableRandom.xy, Center, Size.x, Velocity)는 바꾸면 안 됩니다. 셰이더가 이 순서대로 데이터를 읽습니다. 다른 파티클 시스템에 `Blizzard.mat`을 쓸 때도 이 순서로 맞추고 `Render Mode`는 `Billboard`로 둡니다.

#### ③ 씬 환경
| 바꾸고 싶은 것 | 조정하는 곳 |
| --- | --- |
| 시야가 얼마나 뿌연지 | `Window > Rendering > Lighting > Environment > Other Settings > Fog`의 `Density` |
| 안개·하늘 색 | 같은 곳의 Fog `Color`와 `Main Camera > Background` (둘을 같은 색으로 맞추기) |
| 해의 방향·밝기 | `Directional Light`의 회전과 `Intensity` |

### 성능
파티클이 최대 9만 개 정도 쓰입니다. 버벅이면 `Snow_Far`의 `Rate over Time`부터 줄이세요. 안개가 짙어서 먼 눈은 거의 보이지 않으므로 줄여도 티가 잘 나지 않습니다.

### 데모 씬 다시 만들기
메뉴 `MyShaders > Build Blizzard Demo`는 데모 씬과 두 머티리얼을 **기본값으로 덮어써서** 다시 만듭니다. 에디터에서 값을 다듬은 뒤에는 실행하지 마세요.

## 셰이더볼 사용법

셰이더 테스트용 씬과 셰이더볼이 준비되어 있습니다.

| 경로 | 설명 |
| --- | --- |
| `Assets/00. Scenes/Study Shader.unity` | 셰이더볼 3개가 배치된 테스트 씬 |
| `Assets/01. ShaderBall/ShaderBall.prefab` | 셰이더볼 프리팹 |
| `Assets/01. ShaderBall/Materials/ShaderBall_Surface.mat` | 볼 표면에 쓰이는 기본 머티리얼 (흰색 URP Lit) |
| `Assets/01. ShaderBall/Materials/ShaderBall_Base.mat` | 받침대 · 링 · 로고용 머티리얼 (수정하지 않아도 됨) |

### 내 셰이더 적용하기

1. `Study Shader` 씬을 엽니다.
2. 셰이더를 만들고, 그 셰이더로 머티리얼을 만듭니다.
3. Hierarchy에서 `ShaderBalls > ShaderBall_1`(또는 `_2`, `_3`)을 펼칩니다.
4. 표면 파트 4개에 만든 머티리얼을 드래그해서 넣습니다.
   - `Ball/Ball_Part` (구 본체)
   - `Ball/Front_Part`, `Ball/Back_Part_1`, `Ball/Back_Part_2` (앞뒤 패널)
5. 볼 3개에 서로 다른 머티리얼을 넣으면 셰이더끼리 나란히 비교할 수 있습니다.

> 씬에 있는 볼만 바꾸면 프리팹 원본은 그대로 남습니다. 프리팹이나 `ShaderBall_Surface.mat`을 직접 수정하면 모든 볼이 함께 바뀌니 주의하세요.
