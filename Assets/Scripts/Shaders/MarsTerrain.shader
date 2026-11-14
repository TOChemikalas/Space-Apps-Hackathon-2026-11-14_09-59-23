
Shader "Arcadia/PixelTerrain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _NormalMap ("Optional Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _NormalMap;
            fixed4 _Color, _MarsTint;
            float _NormalStrength, _MarsAmbient, _MarsDirect;
            float4 _MarsSunDirection;

            struct Input { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };

            Output vert(Input i)
            {
                Output o;
                o.pos = UnityObjectToClipPos(i.vertex);
                o.color = i.color * _Color;
                o.uv = i.uv;
                return o;
            }

            fixed4 frag(Output i):SV_Target
            {
                fixed4 pixel = tex2D(_MainTex, i.uv) * i.color;
                float3 normal = normalize(lerp(float3(0,0,1), UnpackNormal(tex2D(_NormalMap, i.uv)), _NormalStrength));
                float directional = saturate(dot(normal, normalize(_MarsSunDirection.xyz)));
                float intensity = saturate(_MarsAmbient + _MarsDirect * directional * _NormalStrength);

                pixel.rgb *= _MarsTint.rgb * intensity;
                pixel.rgb *= pixel.a;
                return pixel;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
