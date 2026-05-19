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
        _ReticleMaskDepth ("Reticle Mask Depth", Range(-1, 1)) = 0
        [Toggle(_USE_TEXTURE_COLOR)] _USE_TEXTURE_COLOR ("Use Texture Color", Float) = 1
        _d ("d", Range(0, 0.3)) = 0.1

        _OcularDiameter ("Ocular Diameter (mm)", Range(20, 60)) = 35
        _EyeRelief ("Eye Relief (mm)", Range(10, 200)) = 80
        _EyeReliefTolerance ("Eye Relief Tolerance (mm)", Range(1, 100)) = 30
        _ExitPupilDiameter ("Exit Pupil Diameter (mm)", Range(1, 20)) = 8
        _VignetteSoftness ("Vignette Softness", Range(0.001, 0.5)) = 0.05
        [HideInInspector] _EyeReliefLensRightOS ("Eye Relief Lens Right OS", Vector) = (0, 0, 0, 0)
        [HideInInspector] _EyeReliefLensUpOS ("Eye Relief Lens Up OS", Vector) = (0, 0, 0, 0)
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
        float _ReticleMaskDepth;
        float _d;
        float _OcularDiameter;
        float _EyeRelief;
        float _EyeReliefTolerance;
        float _ExitPupilDiameter;
        float _VignetteSoftness;
        float4 _EyeReliefLensRightOS;
        float4 _EyeReliefLensUpOS;
    }

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

    float GetLensAlpha(float2 uv)
    {
        return step(0.5, 1.0 - distance(uv, float2(0.5, 0.5)));
    }

    void ClipLens(float2 uv)
    {
        clip(GetLensAlpha(uv) - 0.5);
    }

    float2 GetBaseScopeUv(float3 viewDirLS)
    {
        return (viewDirLS.xy * float2(-1.0, -1.0)) + float2(0.5, 0.5);
    }

    float2 GetDistortedScopeUv(float3 viewDirLS)
    {
        float distortion = (_d * 10.0) + 1.0;
        return GetBaseScopeUv(viewDirLS) + (-viewDirLS.xy * distortion);
    }

    float2 ApplyShaderZoom(float2 uv)
    {
        float inverseZoom = rcp(max(_ShaderZoom, 1e-4));
        return (uv * inverseZoom) + ((1.0 - inverseZoom) * 0.5);
    }

    float2 GetReticleMaskUv(float2 distortedUv)
    {
        float depthScale = max(1.0 + _ReticleMaskDepth, 1e-4);
        return ((distortedUv - float2(0.5, 0.5)) * depthScale) + float2(0.5, 0.5);
    }

    float GetScopeFovMask(float2 reticleMaskUv)
    {
    #if defined(_USESCOPEFOV)
        float objectiveDistance = distance(reticleMaskUv, float2(0.5, 0.5)) * 2.0;
        float fadeDistance = max(_FOVFadeDistance, 1e-5);
        float fade = saturate(saturate(_FOVFadeDistance - (objectiveDistance - _FOV_Size)) / fadeDistance);
        return 0.5 - (0.5 * cos(fade * SCOPE_PI));
    #else
        return 1.0;
    #endif
    }

    float3 GetConfiguredWorldAxis(float4 objectAxis, float3 fallbackAxis)
    {
        float objectAxisLengthSq = dot(objectAxis.xyz, objectAxis.xyz);
        if (objectAxisLengthSq > 1e-6)
        {
            return NormalizeSafe(mul((float3x3) unity_ObjectToWorld, objectAxis.xyz));
        }

        return NormalizeSafe(fallbackAxis);
    }

    float3 ProjectAxisOnLensPlane(float3 axisWS, float3 lensForwardWS, float3 fallbackAxisWS)
    {
        float3 projectedAxis = axisWS - (lensForwardWS * dot(axisWS, lensForwardWS));
        float projectedLengthSq = dot(projectedAxis, projectedAxis);

        if (projectedLengthSq > 1e-6)
        {
            return projectedAxis * rsqrt(projectedLengthSq);
        }

        projectedAxis = fallbackAxisWS - (lensForwardWS * dot(fallbackAxisWS, lensForwardWS));
        projectedLengthSq = dot(projectedAxis, projectedAxis);

        if (projectedLengthSq > 1e-6)
        {
            return projectedAxis * rsqrt(projectedLengthSq);
        }

        return NormalizeSafe(cross(lensForwardWS, abs(lensForwardWS.y) < 0.999 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0)));
    }

    void GetLensBasis(
        float3 fallbackRightWS,
        float3 fallbackUpWS,
        float3 surfaceNormalWS,
        out float3 lensRightWS,
        out float3 lensUpWS,
        out float3 lensForwardWS
    )
    {
        lensForwardWS = NormalizeSafe(-surfaceNormalWS);

        float3 configuredRightWS = GetConfiguredWorldAxis(_EyeReliefLensRightOS, fallbackRightWS);
        float3 configuredUpWS = GetConfiguredWorldAxis(_EyeReliefLensUpOS, fallbackUpWS);
        lensRightWS = ProjectAxisOnLensPlane(configuredRightWS, lensForwardWS, fallbackRightWS);
        lensUpWS = NormalizeSafe(cross(lensForwardWS, lensRightWS));

        if (dot(lensUpWS, ProjectAxisOnLensPlane(configuredUpWS, lensForwardWS, fallbackUpWS)) < 0.0)
        {
            lensRightWS = -lensRightWS;
            lensUpWS = -lensUpWS;
        }
    }

    float3 GetLensSpaceVector(float3 vectorWS, float3 lensRightWS, float3 lensUpWS, float3 lensForwardWS)
    {
        return float3(
            dot(vectorWS, lensRightWS),
            dot(vectorWS, lensUpWS),
            dot(vectorWS, lensForwardWS)
        );
    }

    float GetEyeReliefDistanceMask(float axialDistance)
    {
        float targetEyeRelief = max(_EyeRelief, 1e-4);
        float tolerance = max(_EyeReliefTolerance, 1e-5);
        float distanceError = abs(axialDistance - targetEyeRelief);

        return 1.0 - smoothstep(0.0, tolerance, distanceError);
    }

    float GetEyeReliefMask(float2 projectedUv, float2 projectedPointMm, float3 lensSpaceCamPos)
    {
        float ocularRadius = _OcularDiameter * 0.5;
        float exitPupilRadius = max(_ExitPupilDiameter * 0.5, 1e-5);

        float2 uvCentered = projectedUv - float2(0.5, 0.5);
        float uvDist = length(uvCentered);
        float baseOcularMask = 1.0 - smoothstep(0.5 - _VignetteSoftness, 0.5, uvDist);

        float axialDistance = max(abs(lensSpaceCamPos.z), 1e-4);
        float axialMask = GetEyeReliefDistanceMask(axialDistance);
        float2 exitPupilPoint = lensSpaceCamPos.xy + ((1.0 - (_EyeRelief / axialDistance)) * (projectedPointMm - lensSpaceCamPos.xy));
        float pupilDistance = length(exitPupilPoint);
        float fadeMm = max(_VignetteSoftness * max(ocularRadius, 1e-4), 1e-5);
        float eyeBoxMask = 1.0 - smoothstep(exitPupilRadius, exitPupilRadius + fadeMm, pupilDistance);

        return baseOcularMask * eyeBoxMask * axialMask;
    }

    float2 GetCrosshairUv(float2 distortedUv, float3 cameraToObjectDirLS)
    {
        float axial = max(abs(cameraToObjectDirLS.z), 1e-4);
        float2 scale = rcp(max(axial * _CrosshairSize.xy, 1e-4));
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
        float3 lensRightWS;
        float3 lensUpWS;
        float3 lensForwardWS;
        GetLensBasis(worldTangent, worldBitangent, worldNormal, lensRightWS, lensUpWS, lensForwardWS);

        float3 viewDirWS = NormalizeSafe(_WorldSpaceCameraPos - input.worldPos);
        float3 viewDirLS = NormalizeSafe(GetLensSpaceVector(viewDirWS, lensRightWS, lensUpWS, lensForwardWS));

        float2 distortedUv = GetDistortedScopeUv(viewDirLS);
        float2 reticleMaskUv = GetReticleMaskUv(distortedUv);
        float2 pipUv = ApplyShaderZoom(distortedUv);
        float scopeFovMask = GetScopeFovMask(reticleMaskUv);

        float2 projectedUvCentered = reticleMaskUv - float2(0.5, 0.5);
        float ocularDiameterMm = _OcularDiameter;
        float2 projectedPointMm = projectedUvCentered * ocularDiameterMm;
        float3 lensOriginWS = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;
        float3 cameraToLensWS = _WorldSpaceCameraPos - lensOriginWS;

        float3 dPosDx = ddx(input.worldPos);
        float3 dPosDy = ddy(input.worldPos);
        float2 dUvDx = ddx(input.uv0);
        float2 dUvDy = ddy(input.uv0);
        float uvDet = (dUvDx.x * dUvDy.y) - (dUvDx.y * dUvDy.x);
        float safeUvDet = abs(uvDet) > 1e-8 ? uvDet : (uvDet < 0.0 ? -1e-8 : 1e-8);
        float3 worldPerUvX = ((dPosDx * dUvDy.y) - (dPosDy * dUvDx.y)) / safeUvDet;
        float3 worldPerUvY = ((dPosDy * dUvDx.x) - (dPosDx * dUvDy.x)) / safeUvDet;
        float2 physicalPerWorld = ocularDiameterMm / max(float2(length(worldPerUvX), length(worldPerUvY)), 1e-5);
        float axialPhysicalPerWorld = (physicalPerWorld.x + physicalPerWorld.y) * 0.5;

        float3 lensSpaceCamPosWS = GetLensSpaceVector(cameraToLensWS, lensRightWS, lensUpWS, lensForwardWS);
        float3 lensSpaceCamPos = float3(
            lensSpaceCamPosWS.xy * physicalPerWorld,
            lensSpaceCamPosWS.z * axialPhysicalPerWorld
        );
        float3 cameraToObjectDirLS = NormalizeSafe(lensSpaceCamPos);

        float2 crosshairUv = GetCrosshairUv(distortedUv, cameraToObjectDirLS);
        float eyeReliefMask = GetEyeReliefMask(reticleMaskUv, projectedPointMm, lensSpaceCamPos);

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
