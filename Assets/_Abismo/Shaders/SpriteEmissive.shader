// Partes que BRILLAN de un sprite (ojos, brasas, runas): se dibujan encima del sprite iluminado, sin luz 2D,
// multiplicadas por una intensidad HDR (> 1) para que el bloom de URP las haga resplandecer.
// En URP 2D se dibuja en la pasada "SRPDefaultUnlit".
Shader "Abismo/SpriteEmissive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Intensity ("Intensidad (HDR)", Float) = 1.6
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            half _Intensity;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(v2f IN) : SV_Target
            {
                half4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                c.rgb *= _Intensity;
                return c;
            }
            ENDCG
        }
    }
}
