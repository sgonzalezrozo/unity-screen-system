// Separable Gaussian blur for the screen snapshot shown behind modals and alerts.
// Blitted a few times at low resolution when a modal opens, never per frame.
Shader "Hidden/ScreenSystem/SnapshotBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        ZTest Always
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float2 _Direction;
            float _Spread;

            // 9-tap Gaussian folded into 5 bilinear samples.
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 step = _Direction * _MainTex_TexelSize.xy * _Spread;
                fixed4 color = tex2D(_MainTex, i.uv) * 0.2270270270;
                color += tex2D(_MainTex, i.uv + step * 1.3846153846) * 0.3162162162;
                color += tex2D(_MainTex, i.uv - step * 1.3846153846) * 0.3162162162;
                color += tex2D(_MainTex, i.uv + step * 3.2307692308) * 0.0702702703;
                color += tex2D(_MainTex, i.uv - step * 3.2307692308) * 0.0702702703;
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
