Shader "FPS/ScopeDemo HLSL"
{
    Properties
    {
        [NoScaleOffset] Texture2D_331d4af5170a491393c16295823bf2d6 ("PIPInput", 2D) = "white" {}
        [NoScaleOffset] _Crosshair ("Crosshair", 2D) = "white" {}
        _CrosshairColor ("CrosshairColor", Color) = (0, 0.4528302, 0.05357282, 1)
        _CrosshairSize ("CrosshairSize", Vector) = (25, 25, 0, 0)
        _CrosshairOffset ("CrosshairOffset", Vector) = (0.5, 0.5, 0, 0)
        _CrosshairEmission ("CrosshairEmission", Float) = 10
        _ShaderZoom ("ShaderZoom", Range(1, 10)) = 1
        [Toggle(_USESCOPEFOV)] _USESCOPEFOV ("UseScopeFOV", Float) = 1
        _FOV_Size ("FOV Size", Range(0, 1)) = 0.75
        _FOVFadeDistance ("FOVFadeDistance", Range(0, 0.05)) = 0.01
        [Toggle(_USE_TEXTURE_COLOR)] _USE_TEXTURE_COLOR ("Use Texture Color", Float) = 1
        _d ("d", Range(0, 0.3)) = 0.1
        _EyeReliefStrength ("Eye Relief Strength", Range(0, 1)) = 1
        _EyeReliefMinDistance ("Eye Relief Min Distance", Range(0, 1)) = 0
        _EyeReliefMaxDistance ("Eye Relief Max Distance", Range(0, 1)) = 0.18
        _EyeReliefDistanceFade ("Eye Relief Distance Fade", Range(0.001, 0.5)) = 0.02
        _EyeReliefEyeBoxRadius ("Eye Relief Eye Box Radius", Range(0.001, 0.2)) = 0.045
        _EyeReliefEyeBoxFade ("Eye Relief Eye Box Fade", Range(0.001, 0.1)) = 0.015
        _EyeReliefRadius ("Eye Relief Aperture Radius", Range(0.01, 1)) = 0.5
        _EyeReliefFadeDistance ("Eye Relief Aperture Fade", Range(0.001, 0.25)) = 0.06
        _EyeReliefOffsetStrength ("Eye Relief Aperture Offset", Range(-4, 4)) = 1
    }

    HLSLINCLUDE
    #pragma target 3.0
    #pragma shader_feature_local_fragment _USESCOPEFOV
    #pragma shader_feature_local_fragment _USE_TEXTURE_COLOR

    static const float SCOPE_PI = 3.14159265359;

    Texture2D Texture2D_331d4af5170a491393c16295823bf2d6;
    SamplerState sampler_Texture2D_331d4af5170a491393c16295823bf2d6;

    Texture2D _Crosshair;
    SamplerState sampler_Crosshair;

    cbuffer UnityPerMaterial
    {
        float4 _CrosshairColor;
        float4 _CrosshairSize;
        float4 _CrosshairOffset;
        float _CrosshairEmission;
        float _ShaderZoom;
        float _FOV_Size;
        float _FOVFadeDistance;
        float _d;
        float _EyeReliefStrength;
        float _EyeReliefMinDistance;
        float _EyeReliefMaxDistance;
        float _EyeReliefDistanceFade;
        float _EyeReliefEyeBoxRadius;
        float _EyeReliefEyeBoxFade;
        float _EyeReliefRadius;
        float _EyeReliefFadeDistance;
        float _EyeReliefOffsetStrength;
    }

    // Declare the Unity-built transform/camera uniforms explicitly because this
    // shader avoids pipeline-specific includes.
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4x4 unity_MatrixVP;
    float4 unity_WorldTransformParams;
    float3 _WorldSpaceCameraPos;

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float4 tangentOS : TANGENT;
        float2 uv0 : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv0 : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
        float3 worldNormal : TEXCOORD2;
        float3 worldTangent : TEXCOORD3;
        float tangentSign : TEXCOORD4;
    };

    float3 NormalizeSafe(float3 value)
    {
        float lengthSq = max(dot(value, value), 1e-8);
        return value * rsqrt(lengthSq);
    }

    float3 TransformWorldToTangentExact(float3 vectorWS, float3x3 tangentToWorld, bool doNormalize)
    {
        float3 row0 = tangentToWorld[0];
        float3 row1 = tangentToWorld[1];
        float3 row2 = tangentToWorld[2];

        float3 col0 = cross(row1, row2);
        float3 col1 = cross(row2, row0);
        float3 col2 = cross(row0, row1);

        float determinant = dot(row0, col0);
        float3x3 inverseTransposeTbn = float3x3(col0, col1, col2);
        float3 result = mul(inverseTransposeTbn, vectorWS);

        if (doNormalize)
        {
            float signDeterminant = determinant < 0.0 ? -1.0 : 1.0;
            return NormalizeSafe(signDeterminant * result);
        }

        return result / max(abs(determinant), 1e-8);
    }

    float3 TransformWorldToTangentDirExact(float3 directionWS, float3x3 tangentToWorld, bool doNormalize)
    {
        float3 result = mul(tangentToWorld, directionWS);
        return doNormalize ? NormalizeSafe(result) : result;
    }

    float GetLensAlpha(float2 uv)
    {
        return step(0.5, 1.0 - distance(uv, float2(0.5, 0.5)));
    }

    void ClipLens(float2 uv)
    {
        clip(GetLensAlpha(uv) - 0.5);
    }

    float2 GetBaseScopeUv(float3 viewDirTS)
    {
        return (viewDirTS.xy * float2(-1.0, -1.0)) + float2(0.5, 0.5);
    }

    float2 GetDistortedScopeUv(float3 viewDirTS)
    {
        float2 baseUv = GetBaseScopeUv(viewDirTS);

        // Keep the distortion uniform here; the previous anisotropic variant
        // regressed the reticle shape.
        float distortion = (_d * 10.0) + 1.0;
        return baseUv + (-viewDirTS.xy * distortion);
    }

    float2 ApplyShaderZoom(float2 uv)
    {
        float inverseZoom = rcp(max(_ShaderZoom, 1e-4));
        return (uv * inverseZoom) + ((1.0 - inverseZoom) * 0.5);
    }

    float GetScopeFovMask(float2 distortedUv)
    {
    #if defined(_USESCOPEFOV)
        float objectiveDistance = distance(distortedUv, float2(0.5, 0.5)) * 2.0;
        float fadeDistance = max(_FOVFadeDistance, 1e-5);
        float fade = saturate(saturate(_FOVFadeDistance - (objectiveDistance - _FOV_Size)) / fadeDistance);
        return 0.5 - (0.5 * cos(fade * SCOPE_PI));
    #else
        return 1.0;
    #endif
    }

    float GetReliefDistanceBand(float axialDistance)
    {
        float minDistance = min(_EyeReliefMinDistance, _EyeReliefMaxDistance);
        float maxDistance = max(_EyeReliefMinDistance, _EyeReliefMaxDistance);
        float halfBand = max((maxDistance - minDistance) * 0.5, 1e-5);
        float fadeDistance = min(max(_EyeReliefDistanceFade, 1e-5), halfBand);
        float nearMask = smoothstep(minDistance, minDistance + fadeDistance, axialDistance);
        float farMask = 1.0 - smoothstep(maxDistance - fadeDistance, maxDistance, axialDistance);

        return saturate(nearMask * farMask);
    }

    float GetEyeBoxMask(float2 lateralPosition)
    {
        float radius = max(_EyeReliefEyeBoxRadius, 1e-5);
        float fadeDistance = min(max(_EyeReliefEyeBoxFade, 1e-5), radius);
        float innerRadius = max(radius - fadeDistance, 0.0);

        return 1.0 - smoothstep(innerRadius, radius, length(lateralPosition));
    }

    float GetEyeReliefMask(float2 lensUv, float3 cameraToObjectTS)
    {
        float strength = saturate(_EyeReliefStrength);
        float axialDistance = max(abs(cameraToObjectTS.z), 1e-5);
        float distanceMask = GetReliefDistanceBand(axialDistance);
        float eyeBoxMask = GetEyeBoxMask(cameraToObjectTS.xy);
        float apertureRadius = max(_EyeReliefRadius, 1e-5);
        float apertureFade = min(max(_EyeReliefFadeDistance, 1e-5), apertureRadius);
        float innerApertureRadius = max(apertureRadius - apertureFade, 0.0);
        float2 reliefOffset = (cameraToObjectTS.xy / axialDistance) * _EyeReliefOffsetStrength;
        float2 reliefCenter = float2(0.5, 0.5) - reliefOffset;
        float apertureDistance = distance(lensUv, reliefCenter);
        float apertureMask = 1.0 - smoothstep(innerApertureRadius, apertureRadius, apertureDistance);
        float reliefMask = apertureMask * distanceMask * eyeBoxMask;

        return lerp(1.0, saturate(reliefMask), strength);
    }

    float2 GetCrosshairUv(float2 distortedUv, float3 cameraToObjectDirTS)
    {
        float2 scale = rcp(max(cameraToObjectDirTS.z * _CrosshairSize.xy, 1e-4));
        float2 offset = ((1.0 - scale) * 0.5) + _CrosshairOffset.xy;
        return (distortedUv * scale) + offset;
    }

    Varyings ScopeVert(Attributes input)
    {
        Varyings output;

        float4 worldPos = mul(unity_ObjectToWorld, input.positionOS);
        output.positionCS = mul(unity_MatrixVP, worldPos);
        output.uv0 = input.uv0;
        output.worldPos = worldPos.xyz;
        output.worldNormal = NormalizeSafe(mul(input.normalOS, (float3x3) unity_WorldToObject));
        output.worldTangent = NormalizeSafe(mul((float3x3) unity_ObjectToWorld, input.tangentOS.xyz));
        output.tangentSign = input.tangentOS.w * unity_WorldTransformParams.w;

        return output;
    }

    float4 ScopeFrag(Varyings input) : SV_Target
    {
        ClipLens(input.uv0);

        float3 unnormalizedNormalWS = input.worldNormal;
        float renormFactor = rsqrt(max(dot(unnormalizedNormalWS, unnormalizedNormalWS), 1e-8));
        float crossSign = input.tangentSign > 0.0 ? 1.0 : -1.0;
        float3 bitangentWS = crossSign * cross(input.worldNormal, input.worldTangent);

        float3 worldNormal = renormFactor * input.worldNormal;
        float3 worldTangent = renormFactor * input.worldTangent;
        float3 worldBitangent = renormFactor * bitangentWS;
        float3x3 tangentToWorld = float3x3(worldTangent, worldBitangent, worldNormal);

        float3 viewDirWS = NormalizeSafe(_WorldSpaceCameraPos - input.worldPos);
        float3 viewDirTS = TransformWorldToTangentExact(viewDirWS, tangentToWorld, true);

        float2 distortedUv = GetDistortedScopeUv(viewDirTS);
        float2 pipUv = ApplyShaderZoom(distortedUv);
        float scopeFovMask = GetScopeFovMask(distortedUv);

        float3 objectOriginWS = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;
        float3 cameraToObjectWS = _WorldSpaceCameraPos - objectOriginWS;
        float3 cameraToObjectTS = TransformWorldToTangentDirExact(cameraToObjectWS, tangentToWorld, false);
        float3 cameraToObjectDirTS = TransformWorldToTangentDirExact(cameraToObjectWS, tangentToWorld, true);
        float eyeReliefMask = GetEyeReliefMask(input.uv0, cameraToObjectTS);

        float2 crosshairUv = GetCrosshairUv(distortedUv, cameraToObjectDirTS);

        float4 pipSample = Texture2D_331d4af5170a491393c16295823bf2d6.Sample(
            sampler_Texture2D_331d4af5170a491393c16295823bf2d6,
            pipUv
        );
        float4 crosshairSample = _Crosshair.Sample(sampler_Crosshair, crosshairUv);

        float4 crosshairColor;
    #if defined(_USE_TEXTURE_COLOR)
        crosshairColor = crosshairSample * _CrosshairColor;
    #else
        crosshairColor = crosshairSample.a * _CrosshairColor;
    #endif

        float4 baseColor = lerp(pipSample * scopeFovMask, crosshairColor, crosshairSample.a);
        float4 emissionColor = lerp(pipSample * scopeFovMask, crosshairColor * _CrosshairEmission, crosshairSample.a);
        float3 finalColor = max(baseColor.rgb, emissionColor.rgb) * eyeReliefMask;

        return float4(finalColor, 1.0);
    }

    struct DepthVaryings
    {
        float4 positionCS : SV_POSITION;
        float2 uv0 : TEXCOORD0;
        float3 worldNormal : TEXCOORD1;
    };

    DepthVaryings ScopeDepthVert(Attributes input)
    {
        DepthVaryings output;

        float4 worldPos = mul(unity_ObjectToWorld, input.positionOS);
        output.positionCS = mul(unity_MatrixVP, worldPos);
        output.uv0 = input.uv0;
        output.worldNormal = NormalizeSafe(mul(input.normalOS, (float3x3) unity_WorldToObject));

        return output;
    }

    float ScopeDepthOnlyFrag(DepthVaryings input) : SV_Target
    {
        ClipLens(input.uv0);
        return input.positionCS.z;
    }

    float2 PackNormalOctQuadEncode(float3 n)
    {
        n *= rcp(max(dot(abs(n), 1.0), 1e-6));
        float t = saturate(-n.z);
        return n.xy + float2(n.x >= 0.0 ? t : -t, n.y >= 0.0 ? t : -t);
    }

    uint3 PackFloat2To888UInt(float2 f)
    {
        uint2 i = (uint2) (saturate(f) * 4095.5);
        uint2 hi = i >> 8;
        uint2 lo = i & 255;
        return uint3(lo, hi.x | (hi.y << 4));
    }

    float3 PackFloat2To888(float2 f)
    {
        return PackFloat2To888UInt(f) / 255.0;
    }

    half4 ScopeDepthNormalsFrag(DepthVaryings input) : SV_Target
    {
        ClipLens(input.uv0);

        float3 normalWS = NormalizeSafe(input.worldNormal);

    #if defined(_GBUFFER_NORMALS_OCT)
        float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
        float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
        return half4(PackFloat2To888(remappedOctNormalWS), 0.0);
    #else
        return half4(normalWS, 0.0);
    #endif
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
        }

        Cull Back
        ZWrite On
        ZTest LEqual
        Blend Off

        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "UniversalForwardOnly"
            }

            HLSLPROGRAM
            #pragma vertex ScopeVert
            #pragma fragment ScopeFrag
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma vertex ScopeDepthVert
            #pragma fragment ScopeDepthOnlyFrag
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex ScopeDepthVert
            #pragma fragment ScopeDepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }

    Fallback Off
}
