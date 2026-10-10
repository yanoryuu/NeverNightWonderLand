// エリア1 の見た目用スプライトシェーダー (polish B)。SpriteRenderer で使う (頂点カラー = SpriteRenderer.color)。
// - _Scroll: UV のスクロール (UV/秒)。テクスチャの wrapMode = Repeat の継ぎ目なし素材 (水の筋・蒸気の柱・水面) を流す。
//   Tiled 描画でも 1 タイル = UV 0..1 なので、ずらしても継ぎ目は出ない。+y = 絵の下へ流れて見える (絵の上の方の模様が下りてくる)。
// - _SrcBlend/_DstBlend: 通常 (SrcAlpha, OneMinusSrcAlpha) / 加算 (SrcAlpha, One)。光暈・噴水の水しぶきなどの柔らかい光に加算を使う。
// マテリアルは Area1AssetPrep.PreparePolish() が Area1Gen/polish/mat/ に作る。
Shader "NeverNight/A1SpriteScroll"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Scroll ("Scroll (UV/s)", Vector) = (0, 0, 0, 0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Lighting Off
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Scroll;

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv + _Scroll.xy * _Time.y;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                return c;
            }
            ENDHLSL
        }
    }
}
