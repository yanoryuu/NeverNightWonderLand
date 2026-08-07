using System;
using UnityEngine;

namespace Utage
{
	//シェーダー1つで動作するイメージエフェクト
    [RequireComponent(typeof (Camera))]
    public abstract class ImageEffectSingelShaderBase : ImageEffectBase
	{
		public Shader Shader { get { return shader; } set { shader = value; } }
		[SerializeField]
		Shader shader;

		protected Material Material { get; set; }

		//スクリプトからAddComponentした時のシェーダーの設定
		public override void SetShaders(params Shader[] shadres)
		{
			if (shadres.Length <= 0) return;
			Shader = shadres[0];
		}

		//シェーダーをチェックしてマテリアルを作る処理
		protected override bool CheckShaderAndCreateMaterial()
		{
			bool ret = TryCheckShaderAndCreateMaterialSub(Shader, Material, out Material mat);
			Material = mat;
			return ret;
		}

		Shader tmpShader;
		//Json読み込みの時にUnityEngine.Objectも対象になってしまうので、それをいったん記録して戻す
		protected override void StoreObjectsOnJsonRead()
		{
			tmpShader = shader;
		}
		protected override void RestoreObjectsOnJsonRead()
		{
			shader = tmpShader;
		}
	}
}
