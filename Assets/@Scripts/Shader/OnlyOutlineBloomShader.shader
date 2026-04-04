Shader "Custom/OnlyOutlineBloomShader"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}

        // HDR 사용하면 PP 효과 적용
        [HDR][MainColor] _OutlineTint ("Outline Tint (HDR)", Color) = (1,1,1,1)

        // 외곽선 두께
        // 비용조절
        _OutlineWidth ("Outline Width (px)", Range(1, 32)) = 2

        // 외곽선 판정에 사용할 알파 기준값
        // 너무낮으면 희미한 부분도 경계로 잡힐 수 있고 너무 높으면 일부 경계가 사라질 수도
        _AlphaThreshold ("Alpha Threshold", Range(0,1)) = 0.08

        _EdgeSoftness ("Edge Softness", Range(0.5, 3)) = 1.2

        // HDR에 곱해서 Bloom 더 잘보이게
        _BloomIntensity ("Bloom Intensity", Range(1, 12)) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Pass
        {
            Name "OutlineOnly"
            Tags { "LightMode"="Universal2D" }

            // 일반 투명 블렌딩
            Blend SrcAlpha OneMinusSrcAlpha

            // 외곽선 패스라 깊이 기록 안 함
            ZWrite Off

            // 2D 스프라이트라 양면 렌더링
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION; // 오브젝트 공간 정점 위치
                float2 uv         : TEXCOORD0; // 텍스처 UV
                half4  color      : COLOR;     // SpriteRenderer 색상
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // 클립 공간 위치
                float2 uv          : TEXCOORD0;   // 프래그먼트로 전달할 UV
                half4  color       : COLOR;       // 프래그먼트로 전달할 색상
            };

            // 메인 텍스처와 샘플러
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float4 _MainTex_TexelSize; // x=1/width, y=1/height, z=width, w=height

            half4  _OutlineTint;
            float  _OutlineWidth;
            float  _AlphaThreshold;
            float  _EdgeSoftness;
            float  _BloomIntensity;

            Varyings vert(Attributes v)
            {
                Varyings o;

                // 정점을 클립 공간으로 변환
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);

                // 텍스처 ST 반영한 UV
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // SpriteRenderer tint/color 전달
                o.color = v.color;

                return o;
            }

            // UV 위치의 알파값만 읽는 함수
            half AlphaAt(float2 uv)
            {
                // UV 범위를 벗어나면 비어있는 픽셀(알파 0)로 취급
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return 0.0h;

                // 가장자리에서 샘플링이 애매해지는 걸 줄이기 위해
                // 반 픽셀 안쪽으로 clamp
                float2 inset = _MainTex_TexelSize.xy * 0.5;
                uv = clamp(uv, inset, 1.0 - inset);

                // 해당 위치의 알파값 반환
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 셰이더 상수 최대 반경
                const int MAX_RADIUS = 32;

                // 사용자가 지정한 두께를 정수 반경으로 보정
                int radius = (int)clamp(round(_OutlineWidth), 1.0, (float)MAX_RADIUS);

                // 현재 픽셀의 알파
                half aC = AlphaAt(i.uv);

                // 주변 픽셀들의 최대/최소 알파를 기록할 변수
                half aMax = 0.0h;
                half aMin = 1.0h;

                // r = 1, 2, 3 ... radius까지 확장하며 검사
                [unroll]
                for (int r = 1; r <= MAX_RADIUS; r++)
                {
                    if (r > radius) break;

                    // 현재 거리 r에 해당하는 UV 오프셋
                    float2 px = _MainTex_TexelSize.xy * r;

                    // 현재 픽셀 기준 상하좌우 4방향 샘플
                    // 즉 원형 검사가 아니라 십자(+) 방향 검사
                    half a0 = AlphaAt(i.uv + float2( px.x,  0.0)); // 오른쪽
                    half a1 = AlphaAt(i.uv + float2(-px.x,  0.0)); // 왼쪽
                    half a2 = AlphaAt(i.uv + float2( 0.0,  px.y)); // 위
                    half a3 = AlphaAt(i.uv + float2( 0.0, -px.y)); // 아래

                    // 이번 둘레(r)에서 가장 큰 알파 / 가장 작은 알파
                    half localMax = max(max(a0, a1), max(a2, a3));
                    half localMin = min(min(a0, a1), min(a2, a3));

                    // 지금까지 검사한 전체 둘레들 중 최대/최소 알파 갱신
                    aMax = max(aMax, localMax);
                    aMin = min(aMin, localMin);
                }

                // 바깥 경계 후보
                // 현재 픽셀은 비어 있는데 주변 어딘가에 채워진 픽셀이 있으면 값이 커짐
                half outerRaw = saturate(aMax - aC);

                // 안쪽 경계 후보
                // 현재 픽셀은 채워져 있는데 주변 어딘가에 빈 픽셀이 있으면 값이 커짐
                // step(_AlphaThreshold, aC)로 현재 픽셀이 충분히 채워진 경우만 인정
                half innerRaw = saturate(aC - aMin) * step(_AlphaThreshold, aC);

                // 바깥/안쪽 중 더 강한 쪽을 최종 경계 원본값으로 사용
                half raw = max(outerRaw, innerRaw);

                // fwidth 기반 안티앨리어싱용 부드러운 전이 폭 계산
                half w = max(fwidth(raw), 1e-4h) * _EdgeSoftness;

                // raw가 threshold 근처일 때 부드럽게 외곽선으로 변환
                half outline = smoothstep(_AlphaThreshold - w, _AlphaThreshold + w, raw);

                // 최종 기본 색
                half4 baseCol = _OutlineTint * i.color;

                // HDR RGB에 BloomIntensity를 곱해서 Bloom 잘 받게 함
                half3 hdrRgb = baseCol.rgb * (_BloomIntensity * outline);

                // 알파는 외곽선 마스크에 맞춰 출력
                half alpha = baseCol.a * outline;

                // 외곽선만 출력
                return half4(hdrRgb, alpha);
            }
            ENDHLSL
        }
    }
}