Shader "Hidden/ColonyFlow/QueueScreenOutline"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "VisibleMask"
            ZWrite Off ZTest Always Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = SampleSceneDepth(uv);
                // Device depth works for both perspective and orthographic cameras.
                #if UNITY_REVERSED_Z
                    clip(input.positionCS.z - sceneDepth + 0.00001);
                #else
                    clip(sceneDepth - input.positionCS.z + 0.00001);
                #endif
                return 1;
            }
            ENDHLSL
        }
        Pass
        {
            Name "OutsideStroke"
            ZWrite Off ZTest Always Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float _WidthPixels;
            half4 _OutlineColor;
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float center = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).r;
                float expanded = center;
                // Sample concentric rings so thin silhouettes remain connected.
                [unroll] for (int ring = 1; ring <= 3; ring++)
                {
                    [unroll] for (int i = 0; i < 16; i++)
                    {
                        float angle = i * 0.3926990817;
                        float2 offset = float2(cos(angle), sin(angle)) *
                            (_WidthPixels * ring / 3.0) * _BlitTexture_TexelSize.xy;
                        expanded = max(expanded, SAMPLE_TEXTURE2D_X(
                            _BlitTexture, sampler_LinearClamp, uv + offset).r);
                    }
                }
                return half4(_OutlineColor.rgb, saturate(expanded - center) * _OutlineColor.a);
            }
            ENDHLSL
        }
        Pass
        {
            Name "FocusDim"
            ZWrite Off ZTest Always Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            half4 _FocusDimColor;
            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 position = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.positionCS = float4(position * 2.0 - 1.0, 0.0, 1.0);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target { return _FocusDimColor; }
            ENDHLSL
        }
    }
}
