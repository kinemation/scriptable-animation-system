Shader "FPS/ScopeDemo HLSL"
{
    Properties
    {
        [NoScaleOffset] _RenderTexture ("RenderTexture", 2D) = "white" {}
        [NoScaleOffset] _Reticle ("Reticle", 2D) = "white" {}
        _ReticleColor ("ReticleColor", Color) = (0, 0.4528302, 0.05357282, 1)
        _ReticleSize ("ReticleSize", Vector) = (25, 25, 0, 0)
        _ReticleOffset ("ReticleOffset", Vector) = (0.5, 0.5, 0, 0)
        _ReticleEmission ("ReticleEmission", Float) = 10
        [Min(1)] _Magnification ("Magnification", Float) = 1
        [Toggle] _UseReticleZoom ("UseReticleZoom", Float) = 0
        _Distortion ("Distortion", Range(0, 0.3)) = 0.1
        _LensNormalOS ("Lens Normal OS", Vector) = (0, 0, 1, 0)
        [Toggle] _UseLensMask ("UseLensMask", Float) = 1

        _OcularDiameter ("Ocular Diameter (mm)", Range(20, 60)) = 35
        _EyeRelief ("Eye Relief (mm)", Range(10, 200)) = 80
        _EyeReliefMinFadeDistance ("Eye Relief Min Fade Distance (mm)", Range(1, 150)) = 35
        _EyeReliefMaxFadeDistance ("Eye Relief Max Fade Distance (mm)", Range(1, 300)) = 120
        _ExitPupilDiameter ("Exit Pupil Diameter (mm)", Range(1, 20)) = 8
        _VignetteSoftness ("Vignette Softness", Range(0.001, 0.5)) = 0.05
        [HideInInspector] _EyeReliefLensRightOS ("Eye Relief Lens Right OS", Vector) = (0, 0, 0, 0)
        [HideInInspector] _EyeReliefLensUpOS ("Eye Relief Lens Up OS", Vector) = (0, 0, 0, 0)
    }

    HLSLINCLUDE
    #pragma target 3.0
    Texture2D _RenderTexture;
    SamplerState sampler_RenderTexture;

    Texture2D _Reticle;
    SamplerState sampler_Reticle;

    cbuffer UnityPerMaterial
    {
        float4 _ReticleColor;
        float4 _ReticleSize;
        float4 _ReticleOffset;
        float _ReticleEmission;
        float _Magnification;
        float _UseReticleZoom;
        float _Distortion;
        float4 _LensNormalOS;
        float _UseLensMask;
        float _OcularDiameter;
        float _EyeRelief;
        float _EyeReliefMinFadeDistance;
        float _EyeReliefMaxFadeDistance;
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
        float useLensMask = step(0.5, _UseLensMask);
        clip(lerp(1.0, GetLensAlpha(uv) - 0.5, useLensMask));
    }

    float2 GetBaseScopeUv(float3 viewDirLS)
    {
        return (viewDirLS.xy * float2(-1.0, -1.0)) + float2(0.5, 0.5);
    }

    float2 GetDistortedScopeUv(float3 viewDirLS)
    {
        float distortion = (_Distortion * 10.0) + 1.0;
        return GetBaseScopeUv(viewDirLS) + (-viewDirLS.xy * distortion);
    }

    float2 ApplyMagnification(float2 uv)
    {
        float inverseMagnification = rcp(max(_Magnification, 1.0));
        return (uv * inverseMagnification) + ((1.0 - inverseMagnification) * 0.5);
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

    float3 GetObjectWorldAxis(float3 axisOS)
    {
        return NormalizeSafe(mul((float3x3) unity_ObjectToWorld, axisOS));
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

    float3 GetLensForward(float3 surfaceNormalWS)
    {
        if (dot(_LensNormalOS.xyz, _LensNormalOS.xyz) > 1e-6)
        {
            return NormalizeSafe(mul((float3x3) unity_ObjectToWorld, _LensNormalOS.xyz));
        }

        return NormalizeSafe(-surfaceNormalWS);
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
        lensForwardWS = GetLensForward(surfaceNormalWS);

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

    float3 ProjectOnLensPlane(float3 surfacePosWS, float3 planeOriginWS, float3 planeNormalWS)
    {
        return surfacePosWS - (planeNormalWS * dot(surfacePosWS - planeOriginWS, planeNormalWS));
    }

    float3 ProjectViewRayOnLensPlane(float3 surfacePosWS, float3 planeOriginWS, float3 planeNormalWS)
    {
        float3 rayDirWS = NormalizeSafe(surfacePosWS - _WorldSpaceCameraPos);
        float denom = dot(rayDirWS, planeNormalWS);
        float safeDenom = abs(denom) > 1e-5 ? denom : (denom < 0.0 ? -1e-5 : 1e-5);
        float planeT = dot(planeOriginWS - _WorldSpaceCameraPos, planeNormalWS) / safeDenom;
        float3 rayPlanePosWS = _WorldSpaceCameraPos + (rayDirWS * planeT);
        float3 normalPlanePosWS = ProjectOnLensPlane(surfacePosWS, planeOriginWS, planeNormalWS);
        float validRayProjection = step(1e-5, abs(denom)) * step(0.0, planeT);

        return lerp(normalPlanePosWS, rayPlanePosWS, validRayProjection);
    }

    float3 GetLensSpaceVector(float3 vectorWS, float3 lensRightWS, float3 lensUpWS, float3 lensForwardWS)
    {
        return float3(
            dot(vectorWS, lensRightWS),
            dot(vectorWS, lensUpWS),
            dot(vectorWS, lensForwardWS)
        );
    }

    float GetEyeReliefMask(float2 maskUv, float3 lensSpaceCamPos)
    {
        float ocularDiameter = max(_OcularDiameter, 1e-4);
        float ocularRadius = ocularDiameter * 0.5;
        float exitPupilRadius = max(_ExitPupilDiameter * 0.5, 1e-5);

        float2 uvCentered = maskUv - float2(0.5, 0.5);
        float uvDist = length(uvCentered);
        float authoredSoftness = max(_VignetteSoftness, 1e-4);
        float uvSoftness = max(authoredSoftness, fwidth(uvDist) * 1.5);
        float baseOcularMask = 1.0 - smoothstep(0.5 - uvSoftness, 0.5, uvDist);

        float axialDistance = max(lensSpaceCamPos.z, 1e-4);
        float targetEyeRelief = max(_EyeRelief, 1e-4);
        float2 projectedPointMm = uvCentered * ocularDiameter;
        float nearFadeDistance = max(_EyeReliefMinFadeDistance, 1e-4);
        float farFadeDistance = max(_EyeReliefMaxFadeDistance, 1e-4);
        float nearVisibleDistance = max(targetEyeRelief - nearFadeDistance, 0.0);
        float nearMask = smoothstep(nearVisibleDistance, targetEyeRelief, axialDistance);
        float farT = saturate((axialDistance - targetEyeRelief) / farFadeDistance);

        float exitPupilPlaneT = saturate(1.0 - (targetEyeRelief / axialDistance));
        float2 exitPupilPoint = lensSpaceCamPos.xy + ((projectedPointMm - lensSpaceCamPos.xy) * exitPupilPlaneT);
        float pupilDistance = length(exitPupilPoint);
        float fadeMm = max(authoredSoftness * max(ocularRadius, 1e-4), max(fwidth(pupilDistance) * 1.5, 1e-5));
        float farExitPupilRadius = lerp(exitPupilRadius, -fadeMm, farT);
        float eyeBoxMask = 1.0 - smoothstep(farExitPupilRadius, farExitPupilRadius + fadeMm, pupilDistance);

        return baseOcularMask * nearMask * eyeBoxMask;
    }

    float2 GetReticleUv(float2 distortedUv, float3 cameraToObjectDirLS)
    {
        float axial = max(abs(cameraToObjectDirLS.z), 1e-4);
        float reticleZoom = lerp(1.0, max(_Magnification, 1.0), step(0.5, _UseReticleZoom));
        float2 scale = rcp(max(axial * _ReticleSize.xy * reticleZoom, 1e-4));
        float2 offset = ((1.0 - scale) * 0.5) + _ReticleOffset.xy;
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
        float3 fallbackRightWS = GetObjectWorldAxis(float3(1.0, 0.0, 0.0));
        float3 fallbackUpWS = GetObjectWorldAxis(float3(0.0, 1.0, 0.0));
        float3 lensRightWS;
        float3 lensUpWS;
        float3 lensForwardWS;
        GetLensBasis(fallbackRightWS, fallbackUpWS, worldNormal, lensRightWS, lensUpWS, lensForwardWS);

        float3 lensOriginWS = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;
        float3 cameraToLensWS = _WorldSpaceCameraPos - lensOriginWS;
        if (dot(cameraToLensWS, lensForwardWS) < 0.0)
        {
            lensForwardWS = -lensForwardWS;
            lensRightWS = -lensRightWS;
        }

        float3 lensPlaneSamplePosWS = ProjectViewRayOnLensPlane(input.worldPos, lensOriginWS, lensForwardWS);
        float3 viewDirWS = NormalizeSafe(_WorldSpaceCameraPos - lensPlaneSamplePosWS);
        float3 viewDirLS = NormalizeSafe(GetLensSpaceVector(viewDirWS, lensRightWS, lensUpWS, lensForwardWS));

        float2 distortedUv = GetDistortedScopeUv(viewDirLS);
        float2 renderTextureUv = ApplyMagnification(distortedUv);

        float ocularDiameterMm = _OcularDiameter;

        float3 lensScalePosWS = ProjectOnLensPlane(input.worldPos, lensOriginWS, lensForwardWS);
        float3 dPosDx = ddx(lensScalePosWS);
        float3 dPosDy = ddy(lensScalePosWS);
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

        float2 reticleUv = GetReticleUv(distortedUv, cameraToObjectDirLS);
        float eyeReliefMask = GetEyeReliefMask(distortedUv, lensSpaceCamPos);

        float4 renderTextureSample = _RenderTexture.Sample(
            sampler_RenderTexture,
            renderTextureUv
        );
        float4 reticleSample = _Reticle.Sample(sampler_Reticle, reticleUv);

        float4 reticleColor = reticleSample * _ReticleColor;

        float4 baseColor = lerp(renderTextureSample, reticleColor, reticleSample.a);
        float4 emissionColor = lerp(renderTextureSample, reticleColor * _ReticleEmission, reticleSample.a);
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
