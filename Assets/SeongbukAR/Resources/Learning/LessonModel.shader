Shader "SeongbukAR/Lesson Model"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float shade : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            fixed4 _Color;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 normal = normalize(UnityObjectToWorldNormal(v.normal));
                o.shade = 0.55 + 0.45 * saturate(dot(normal, normalize(float3(-0.4, 0.8, -0.5))));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(_Color.rgb * i.shade, 1);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Color"
}
