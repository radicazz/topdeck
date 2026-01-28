Shader "TopDeck/UpgradeColorModulation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _UpgradeColor ("Upgrade Color", Color) = (0,1,1,1)
        _UpgradeLevel ("Upgrade Level", Range(0, 1)) = 0.0
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.3
        _EmissionStrength ("Emission Strength", Range(0, 2)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _UpgradeColor;
                float _UpgradeLevel;
                float _PulseSpeed;
                float _PulseIntensity;
                float _EmissionStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Lerp between base and upgrade color based on level
                half4 baseColor = _BaseColor;
                half4 targetColor = lerp(baseColor, _UpgradeColor, _UpgradeLevel);
                
                // Add pulsing effect
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(1.0, pulse, _PulseIntensity * _UpgradeLevel);
                
                half4 color = texColor * targetColor * pulse;

                // Add emission based on upgrade level
                float emission = _UpgradeLevel * _EmissionStrength;
                color.rgb += _UpgradeColor.rgb * emission;

                // Simple lighting
                Light mainLight = GetMainLight();
                float3 lighting = mainLight.color * saturate(dot(input.normalWS, mainLight.direction));
                color.rgb *= lighting + 0.3; // ambient

                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogFactor);

                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
