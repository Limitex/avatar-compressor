// Test-only shader declaring the lilToon properties consumed by LilToonTextureBaker's static
// bake decisions, so no-op detection and animation veto can be tested without lilToon installed.
Shader "Hidden/LAC/Tests/LilToonBake"
{
    Properties
    {
        _MainTex ("Main", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _MainTexHSVG ("HSVG", Vector) = (0,1,1,1)
        _MainGradationStrength ("Gradation Strength", Float) = 0
        _MainGradationTex ("Gradation Map", 2D) = "white" {}
        _MainColorAdjustMask ("Adjust Mask", 2D) = "white" {}
        _UseMain2ndTex ("Use 2nd", Float) = 0
        _Color2nd ("2nd Color", Color) = (1,1,1,1)
        _Main2ndTex ("2nd", 2D) = "white" {}
        _Main2ndBlendMask ("2nd Blend Mask", 2D) = "white" {}
        _Main2ndTexDecalAnimation ("2nd Decal Animation", Vector) = (1,1,1,30)
        _Main2ndTex_ScrollRotate ("2nd Scroll Rotate", Vector) = (0,0,0,0)
        _UseMain3rdTex ("Use 3rd", Float) = 0
        _Color3rd ("3rd Color", Color) = (1,1,1,1)
        _Main3rdTex ("3rd", 2D) = "white" {}
        _Main3rdBlendMask ("3rd Blend Mask", 2D) = "white" {}
        _Main3rdTexDecalAnimation ("3rd Decal Animation", Vector) = (1,1,1,30)
        _Main3rdTex_ScrollRotate ("3rd Scroll Rotate", Vector) = (0,0,0,0)
        _AlphaMaskMode ("Alpha Mask Mode", Float) = 0
        _AlphaMask ("Alpha Mask", 2D) = "white" {}
        _AlphaMaskScale ("Alpha Mask Scale", Float) = 1
        _AlphaMaskValue ("Alpha Mask Value", Float) = 0
        _OutlineTex ("Outline", 2D) = "white" {}
        _OutlineTexHSVG ("Outline HSVG", Vector) = (0,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
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
