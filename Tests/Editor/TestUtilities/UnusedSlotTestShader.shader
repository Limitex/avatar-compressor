// Test-only shader exposing a few texture slots so UnusedSlotPruner tests can build materials with
// real bindings and exercise the actual Material.GetTexture / SetTexture path. The pruner uses an
// injected optimizer for the unused decision, so no shader-specific toggles are needed here.
Shader "Hidden/LAC/Tests/UnusedSlot"
{
    Properties
    {
        _MainTex ("Main", 2D) = "white" {}
        _EmissionMap ("Emission", 2D) = "black" {}
        _BumpMap ("Bump", 2D) = "bump" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 1);
            }
            ENDCG
        }
    }
}
