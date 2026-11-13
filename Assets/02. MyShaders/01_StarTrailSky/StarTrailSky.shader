// =============================================================================
//  StarTrailSky — 별의 일주 운동과 긴 잔상(Star Trail)을 그리는 URP 스카이박스
// -----------------------------------------------------------------------------
//  [원리 요약]
//  1) 천구 좌표계: 화면의 각 픽셀이 바라보는 방향(dir)을 "천구 북극(pole)" 기준의
//     극좌표 (theta = 북극에서 떨어진 각도, phi = 북극을 도는 방위각)로 바꾼다.
//     별의 일주 운동은 북극 축을 중심으로 한 회전이므로, 이 좌표에서는
//     "phi에 시간에 비례하는 값을 더하는 것"만으로 표현된다.
//  2) 절차적 별 배치: theta를 일정 간격의 고리(band)로 나누고, 각 고리를 phi 방향
//     칸(cell)으로 나눈다. 칸마다 해시(의사 난수)로 별의 존재 여부, 위치, 밝기,
//     색을 정한다. 고리 둘레(sin(theta))에 비례해 칸 수를 정해서 하늘 전체에
//     별이 고르게 퍼지도록 한다. → 텍스처 없이 수천 개의 별.
//  3) 잔상(궤적): 별은 고리를 따라서만 움직이므로, 지나온 길은 같은 theta 위의
//     호(arc)다. 픽셀 앞쪽(진행 방향)으로 TrailLength 범위 안에 있는 별들을
//     찾아, "별이 이 픽셀을 지나간 뒤 얼마나 흘렀는지(age)"에 따라 밝기를 줄인다.
// =============================================================================
Shader "MyShaders/StarTrailSky"
{
    Properties
    {
        [Header(Sky Gradient)]
        _ZenithColor     ("Zenith Color", Color) = (0.004, 0.008, 0.025, 1)
        _HorizonColor    ("Horizon Color", Color) = (0.035, 0.05, 0.10, 1)
        _GroundColor     ("Ground Color", Color) = (0.006, 0.006, 0.008, 1)
        _HorizonFalloff  ("Horizon Falloff", Range(1, 20)) = 5

        [Header(Celestial Pole)]
        _PoleLatitude    ("Pole Elevation (= Latitude, deg)", Range(0, 90)) = 37
        _PoleAzimuth     ("Pole Azimuth (deg, 0 = +Z)", Range(0, 360)) = 0
        _RotationSpeed   ("Rotation Speed (deg/sec, sign = direction)", Range(-10, 10)) = 1.2

        [Header(Stars)]
        _BandCount       ("Ring Count", Range(30, 600)) = 260
        _CellsAtEquator  ("Cells per Ring at Equator", Range(8, 200)) = 70
        _StarProbability ("Star Probability", Range(0, 1)) = 0.4
        _StarSize        ("Star Size (deg)", Range(0.01, 0.5)) = 0.06
        _StarBrightness  ("Star Brightness", Range(0, 20)) = 5
        _Twinkle         ("Twinkle", Range(0, 1)) = 0.35
        _ColorVariation  ("Color Variation", Range(0, 1)) = 0.7

        [Header(Trails)]
        _TrailLength     ("Trail Length (deg)", Range(0, 300)) = 140
        _TrailBrightness ("Trail Brightness", Range(0, 5)) = 0.3
        _TrailFalloff    ("Trail Falloff (higher = shorter tail)", Range(0.1, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "StarTrailSky"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define TWO_PI_F   6.28318530718
            #define DEG2RAD_F  0.01745329252
            #define MAX_STEPS  128      // 한 고리에서 잔상을 찾을 때 확인하는 최대 칸 수

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                float _HorizonFalloff;
                float _PoleLatitude;
                float _PoleAzimuth;
                float _RotationSpeed;
                float _BandCount;
                float _CellsAtEquator;
                float _StarProbability;
                float _StarSize;
                float _StarBrightness;
                float _Twinkle;
                float _ColorVariation;
                float _TrailLength;
                float _TrailBrightness;
                float _TrailFalloff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS      : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                // 스카이박스 메시는 원점을 감싸는 구/큐브이므로 정점 위치 = 바라보는 방향
                o.dirWS = v.positionOS.xyz;
                return o;
            }

            // ---- 해시: 정수 (고리 번호, 칸 번호, 용도) → 0~1 난수 -------------------
            uint HashU(uint x)
            {
                x ^= x >> 16; x *= 0x7feb352du;
                x ^= x >> 15; x *= 0x846ca68bu;
                x ^= x >> 16;
                return x;
            }

            float Hash01(uint band, uint cell, uint salt)
            {
                uint h = HashU(band * 0x9E3779B1u ^ HashU(cell + salt * 0x85EBCA77u));
                return (float)(h & 0x00FFFFFFu) / 16777216.0;
            }

            // 0 = 푸른 별, 0.5 = 흰 별, 1 = 주황 별 (색온도 흉내)
            float3 StarTint(float t)
            {
                float3 blue   = float3(0.62, 0.74, 1.00);
                float3 white  = float3(1.00, 0.97, 0.92);
                float3 orange = float3(1.00, 0.68, 0.42);
                return t < 0.5 ? lerp(blue, white, t * 2.0) : lerp(white, orange, t * 2.0 - 1.0);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 dir = normalize(i.dirWS);

                // ---- 1. 하늘 그라데이션 -------------------------------------------
                float up = dir.y;
                float skyT = 1.0 - pow(1.0 - saturate(up), _HorizonFalloff);
                float3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, skyT);
                color = lerp(color, _GroundColor.rgb, saturate(-up * 25.0));

                // ---- 2. 천구 북극 기준 극좌표로 변환 -------------------------------
                float lat = _PoleLatitude * DEG2RAD_F;   // 북극의 고도 = 관측 위도
                float az  = _PoleAzimuth  * DEG2RAD_F;
                float3 pole  = float3(sin(az) * cos(lat), sin(lat), cos(az) * cos(lat));
                float3 refUp = abs(pole.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 axisU = normalize(cross(refUp, pole));
                float3 axisW = cross(pole, axisU);

                float theta = acos(clamp(dot(dir, pole), -1.0, 1.0));      // 0 ~ PI
                float phi   = atan2(dot(dir, axisW), dot(dir, axisU));     // -PI ~ PI
                float sinT  = max(sin(theta), 1e-4);

                // 하늘 전체의 회전량 (큰 시간 값에서 정밀도가 떨어지지 않게 0~2PI로 감싼다)
                float rot = frac(_Time.y * _RotationSpeed / 360.0) * TWO_PI_F;
                float dirSign = _RotationSpeed >= 0.0 ? 1.0 : -1.0;

                // ---- 3. 별과 잔상 --------------------------------------------------
                float bandCount = floor(_BandCount);
                float bandH   = PI / bandCount;
                int   band0   = (int)floor(theta / bandH);
                float starW   = _StarSize * DEG2RAD_F;
                float pixelW  = max(fwidth(theta), 1e-5);
                float effW    = max(starW, pixelW * 0.75);   // 픽셀보다 가늘면 깜빡이므로 최소 폭 보장
                float energy  = starW / effW;                // 넓힌 만큼 밝기를 낮춰 총 에너지 보존
                float trailL  = min(_TrailLength * DEG2RAD_F, TWO_PI_F * 0.98);

                float3 stars = 0;

                [unroll]
                for (int db = -1; db <= 1; db++)
                {
                    int b = band0 + db;
                    if (b < 0 || b >= (int)bandCount) continue;

                    // 이 고리의 별들이 놓일 수 있는 theta 범위에서 멀면 고리 전체를 건너뛴다
                    float lo = (b + 0.2) * bandH;
                    float hi = (b + 0.8) * bandH;
                    float bandDist = max(max(lo - theta, theta - hi), 0.0);
                    if (bandDist > effW * 6.0) continue;

                    // 고리 둘레에 비례하는 칸 수 → 별 밀도가 하늘 전체에서 균일
                    float thetaC = (b + 0.5) * bandH;
                    int   cells  = (int)max(1.0, floor(_CellsAtEquator * sin(thetaC) + 0.5));
                    float cellW  = TWO_PI_F / cells;

                    // 회전하는 하늘 기준에서 이 픽셀의 위치(칸 단위)
                    float rel = dirSign * (phi - rot) / cellW;
                    int   j0  = (int)floor(rel);
                    int   steps = (int)ceil(trailL / cellW) + 1;

                    // 별의 진행 방향 앞쪽 칸들을 차례로 확인: 그 칸의 별이 이 픽셀을
                    // 지나간 지 trailL 이내라면 이 픽셀은 그 별의 잔상 위에 있다.
                    [loop]
                    for (int k = -1; k < MAX_STEPS; k++)
                    {
                        if (k > steps) break;

                        int  j  = j0 + k;
                        // j는 음수일 수 있으므로 양수로 옮긴 뒤 uint 나머지 연산 (int 나머지는 느림)
                        uint cw = (uint)(j + cells * 4) % (uint)cells;
                        uint ub = (uint)b;

                        if (Hash01(ub, cw, 0u) > _StarProbability) continue;

                        float rPhi   = Hash01(ub, cw, 1u);
                        float rTheta = Hash01(ub, cw, 2u);
                        float rMag   = Hash01(ub, cw, 3u);
                        float rCol   = Hash01(ub, cw, 4u);

                        float starTheta = (b + 0.2 + 0.6 * rTheta) * bandH;
                        float dTheta    = theta - starTheta;

                        // age: 별이 이 픽셀 위치를 지나간 뒤 이동한 각도 (음수면 아직 도착 전)
                        float age   = ((j + rPhi) - rel) * cellW;
                        float along = age * sinT;                 // 고리를 따라 잰 실제 각거리

                        // 소수의 밝은 별 + 다수의 어두운 별
                        float magP   = pow(rMag, 5.0);
                        float bright = lerp(0.06, 1.0, magP);
                        float w      = effW * (0.7 + 0.8 * magP);
                        float invW2  = 1.0 / (w * w);

                        float across = exp(-dTheta * dTheta * invW2);
                        if (across < 1e-3) continue;

                        // 별 머리(현재 위치) + 반짝임
                        float twinkle = 1.0 - _Twinkle * (0.5 + 0.5 * sin(_Time.y * (1.5 + rCol * 4.0) + rPhi * 40.0));
                        float head = exp(-(dTheta * dTheta + along * along) * invW2) * _StarBrightness * twinkle;

                        // 잔상: 지나간 지 오래될수록 어두워진다
                        float trail = 0.0;
                        if (age > 0.0 && age < trailL)
                        {
                            float fade = pow(saturate(1.0 - age / trailL), _TrailFalloff);
                            float startSoft = saturate(age * sinT / w);   // 머리 바로 뒤는 머리와 자연스럽게 이어지게
                            trail = across * fade * startSoft * _TrailBrightness;
                        }

                        float3 tint = lerp(float3(1.0, 0.97, 0.92), StarTint(rCol), _ColorVariation);
                        stars += tint * bright * energy * (head + trail);
                    }
                }

                // 지평선 아래는 가리고, 지평선 근처는 대기 때문에 흐려진다
                float horizonMask = smoothstep(-0.005, 0.12, up);
                color += stars * horizonMask;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
