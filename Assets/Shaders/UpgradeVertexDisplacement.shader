Shader "TopDeck/UpgradeVertexDisplacement"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _DisplacementAmount ("Displacement Amount", Range(0, 1)) = 0.0
        _DisplacementFrequency ("Displacement Frequency", Range(0, 10)) = 2.0
        _DisplacementSpeed ("Displacement Speed", Range(0, 5)) = 1.0
        _UpgradeLevel ("Upgrade Level", Range(0, 1)) = 0.0
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
                float _DisplacementAmount;
                float _DisplacementFrequency;
                float _DisplacementSpeed;
                float _UpgradeLevel;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionOS = input.positionOS.xyz;
                
                // Apply vertex displacement based on upgrade level
                float wave = sin(_Time.y * _DisplacementSpeed + positionOS.y * _DisplacementFrequency);
                float displacement = wave * _DisplacementAmount * _UpgradeLevel;
                positionOS += input.normalOS * displacement;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
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
                half4 color = texColor * _BaseColor;

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
