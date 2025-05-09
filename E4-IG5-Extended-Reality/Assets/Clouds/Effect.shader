Shader "Unlit/Effect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _CameraDepthTexture;
            float3 _BoundsMin, _BoundsMax;
            float _NumSteps;
            float _CloudScale;
            float3 _Wind;
            float _containerEdgeFadeDst;
            float _cloudSmooth;
            half4 _color;
            float _DensityThreshold;
            float _DensityMultiplier;
            Texture2D<float> _ShapeNoise;
            SamplerState sampler_ShapeNoise;

            Texture3D<float> _DetailNoise;
            SamplerState sampler_DetailNoise;
            float _detailNoiseWeight;
            float _detailNoiseScale;
            float3 _detailNoiseWind;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                o.viewDir = mul(unity_CameraToWorld, float4(viewVector, 0));
                return o;
            }

            float2 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 invRayDir)
            {
                float3 t0 = (boundsMin - rayOrigin) * invRayDir;
                float3 t1 = (boundsMax - rayOrigin) * invRayDir;
                float3 tmin  = min(t0, t1);
                float3 tmax  = max(t0, t1);

                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(tmax.x, min(tmax.y, tmax.z));

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);

                return float2(dstToBox, dstInsideBox);
            }

            float sampleDensity(float3 pos)
            {
                float3 uvw = pos * _CloudScale * 0.001 + _Wind.xyz * 0.1 * _Time.y * _CloudScale;
                float3 size = _BoundsMax - _BoundsMin;
                float3 boundsCentre = (_BoundsMin+_BoundsMax) * 0.5f;

                float3 duvw = pos * _detailNoiseScale * 0.001 + _detailNoiseWind.xyz * 0.1 * _Time.y * _detailNoiseScale;

                float dstFromEdgeX = min(_containerEdgeFadeDst, min(pos.x - _BoundsMin.x, _BoundsMax.x - pos.x));
                float dstFromEdgeY = min(_cloudSmooth, min(pos.y - _BoundsMin.y, _BoundsMax.y - pos.y));
                float dstFromEdgeZ = min(_containerEdgeFadeDst, min(pos.z - _BoundsMin.z, _BoundsMax.z - pos.z));
                float edgeWeight = min(dstFromEdgeZ,dstFromEdgeX)/_containerEdgeFadeDst;

                float4 shape = _ShapeNoise.SampleLevel(sampler_ShapeNoise, uvw.xz, 0);
                float4 detail = _DetailNoise.SampleLevel(sampler_DetailNoise, duvw, 0);
                float density = max(0, lerp(shape.x, detail.x, _detailNoiseWeight) - _DensityThreshold) * _DensityMultiplier;
                return density * edgeWeight * (dstFromEdgeY/_cloudSmooth);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                float viewLength = length(i.viewDir);
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = i.viewDir / viewLength;

                //Depth
                float nonlin_depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float depth = LinearEyeDepth(nonlin_depth) * viewLength;

                float2 rayToContainerInfo = rayBoxDst(_BoundsMin, _BoundsMax, rayOrigin, 1/rayDir);
                float dstToBox = rayToContainerInfo.x;
                float dstInsideBox = rayToContainerInfo.y;

                float dstTravelled = 0;
                float stepSize = dstInsideBox / _NumSteps;
                float dstLimit = min(depth - dstToBox, dstInsideBox);
                
                float totalDensity = 0;
                while(dstTravelled < dstLimit)
                {
                    float3 rayPos = rayOrigin + rayDir * (dstToBox + dstTravelled);
                    totalDensity += sampleDensity(rayPos) * stepSize;
                    dstTravelled += stepSize;
                }
                float transmittance = exp(-totalDensity);
                return lerp(_color, col, transmittance);
            }
            ENDCG
        }
    }
}