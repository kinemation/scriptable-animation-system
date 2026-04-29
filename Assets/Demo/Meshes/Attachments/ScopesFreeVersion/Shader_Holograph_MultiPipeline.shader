Shader "KINEMATION/TacticalShooterPack/Holograph Multi Pipeline"
{
    Properties
    {
        [NoScaleOffset] _Reticle("Reticle", 2D) = "white" {}
        [HDR] _EmissiveColor("Emissive Color", Color) = (0, 0, 0, 0)
        _ReticleDepth("Reticle Depth", Float) = -0.5
        _ReticleSize("Reticle Size", Vector) = (0.45, 0.45, 0, 0)
        _MaskSize("Mask Size", Range(0, 0.5)) = 0.45
        _MaskSoftness("Mask Softness", Range(0, 1)) = 0.05
        _AlphaCutoff("Alpha Clip Threshold", Range(0, 1)) = 0.5

        [HideInInspector] _Cutoff("Alpha Clip Threshold", Range(0, 1)) = 0.5
        [HideInInspector] _MainTex("Main Texture", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1, 1, 1, 1)
    }

    HLSLINCLUDE
    #include "UnityCG.cginc"

    sampler2D _Reticle;
    float4 _EmissiveColor;
    float4 _ReticleSize;
    float _MaskSize;
    float _ReticleDepth;
    float _MaskSoftness;
    float _AlphaCutoff;

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float4 tangentOS : TANGENT;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 positionWS : TEXCOORD1;
        float3 normalWS : TEXCOORD2;
        float3 tangentWS : TEXCOORD3;
        float3 bitangentWS : TEXCOORD4;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings HolographVert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_OUTPUT(Varyings, output);
        UNITY_TRANSFER_INSTANCE_ID(input, output);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        output.positionCS = UnityObjectToClipPos(input.positionOS);
        output.positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;
        output.uv = input.uv;

        output.normalWS = normalize(UnityObjectToWorldNormal(input.normalOS));
        output.tangentWS = normalize(UnityObjectToWorldDir(input.tangentOS.xyz));
        output.bitangentWS = normalize(cross(output.normalWS, output.tangentWS) * input.tangentOS.w * unity_WorldTransformParams.w);

        return output;
    }

    // Keep the reticle square on rectangular meshes or non-uniformly scaled planes.
    float2 HolographReticleAspectScale(Varyings input)
    {
        float3 positionDx = ddx(input.positionWS);
        float3 positionDy = ddy(input.positionWS);
        float2 uvDx = ddx(input.uv);
        float2 uvDy = ddy(input.uv);

        float determinant = uvDx.x * uvDy.y - uvDx.y * uvDy.x;
        float inverseDeterminant = sign(determinant) * rcp(max(abs(determinant), 1.0e-8));

        float3 positionPerU = (positionDx * uvDy.y - positionDy * uvDx.y) * inverseDeterminant;
        float3 positionPerV = (positionDy * uvDx.x - positionDx * uvDy.x) * inverseDeterminant;
        float2 uvWorldSize = max(float2(length(positionPerU), length(positionPerV)), float2(1.0e-5, 1.0e-5));
        float referenceSize = max(min(uvWorldSize.x, uvWorldSize.y), 1.0e-5);

        return uvWorldSize / referenceSize;
    }

    float2 HolographViewOffsetUV(Varyings input)
    {
        float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
        float3 tangentSpaceViewDirection = float3(
            dot(viewDirectionWS, normalize(input.tangentWS)),
            dot(viewDirectionWS, normalize(input.bitangentWS)),
            dot(viewDirectionWS, normalize(input.normalWS)));

        float2 reticleSize = max(abs(_ReticleSize.xy), float2(1.0e-5, 1.0e-5));
        float2 aspectScale = HolographReticleAspectScale(input);
        float2 centeredUV = (input.uv - float2(0.5, 0.5)) * aspectScale;
        float2 viewOffsetUV = centeredUV + tangentSpaceViewDirection.xy * _ReticleDepth;

        return viewOffsetUV / reticleSize + float2(0.5, 0.5);
    }

    float HolographSoftMask(float2 uv)
    {
        float softness = max(_MaskSoftness, 1.0e-5);
        float maskSize = saturate(_MaskSize);
        float2 edge0 = float2(maskSize - softness, maskSize - softness);
        float2 edge1 = float2(maskSize, maskSize);
        float2 centeredUV = abs(uv - float2(0.5, 0.5));
        float2 fadedEdges = smoothstep(edge0, edge1, centeredUV);

        return saturate((1.0 - fadedEdges.x) * (1.0 - fadedEdges.y));
    }

    float4 HolographFrag(Varyings input) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float4 reticle = tex2D(_Reticle, HolographViewOffsetUV(input));
        float alpha = saturate(reticle.a * HolographSoftMask(input.uv));

        clip(alpha - _AlphaCutoff);

        float3 color = reticle.rgb + _EmissiveColor.rgb;
        return float4(color * alpha, alpha);
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex HolographVert
            #pragma fragment HolographFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex HolographVert
            #pragma fragment HolographFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "Always" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex HolographVert
            #pragma fragment HolographFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    Fallback Off
}
