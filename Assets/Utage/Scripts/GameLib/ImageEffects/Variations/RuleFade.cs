using System;
using UnityEngine;

namespace Utage
{
    [ExecuteInEditMode]
    [AddComponentMenu("Utage/Lib/Image Effects/Color Adjustments/RuleFade")]
    public class RuleFade : ImageEffectSingelShaderBase, IImageEffectStrength
	{
		public Texture ruleTexture = null;
		public float vague = 0.2f;
		public float Strength { get { return strength; } set { strength = value; } }
		[Range(0, 1.0f)]
		public float strength = 1;

		public Color color = new Color(0, 0, 0, 0);

		//描画ロジック
		protected override void RenderImage(RenderTexture source, RenderTexture destination)
		{
			Material.SetFloat("_Vague", vague);
			Material.SetTexture("_RuleTex", ruleTexture);
			Material.SetFloat("_Strength", Strength);
			Material.color = color;
			Graphics.Blit(source, destination, Material);
		}

		//Json読み込みの時にUnityEngine.Objectも対象になってしまうので、それをいったん記録して戻す
		protected override void RestoreObjectsOnJsonRead()
		{
			base.RestoreObjectsOnJsonRead();
			//ルール画像はロードするとオブジェクトIDが不明になるのでNULLにする
			//ただしい画像をロードするのは現状無理（AdvEngineへの参照がない）だが、
			//実際はフェードアウトが完全に終わった状態。	つまり完全なブラックアウトなどの状態などを再現すればよいので、NULLでも可
			ruleTexture = null;
		}
	}
}
