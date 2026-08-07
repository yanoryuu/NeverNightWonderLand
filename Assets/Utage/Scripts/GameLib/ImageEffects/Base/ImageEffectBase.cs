using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    [RequireComponent(typeof (Camera))]
    public abstract class ImageEffectBase : MonoBehaviour, IPostEffect
    {
		//作成したマテリアル
		private List<Material> createdMaterials = new List<Material>();

		void Start()
		{
			CheckResources();
		}

		protected virtual void OnDestroy()
		{
			//作成したマテリアルを消去
			ClearCreatedMaterials();
		}

		//リソースのチェック（サポートされているハードか、シェーダーがある・サポートされているかなど）
		protected virtual bool CheckResources()
		{
			if (!CheckSupport() || !CheckShaderAndCreateMaterial())
			{
				enabled = false;
				Debug.LogWarning("The image effect " + ToString() + " has been disabled as it's not supported on the current platform.");
				return false;
			}
			return true;
		}

		protected bool CheckSupport()
		{
			return CheckSupport(NeedRenderTexture, NeedDepth, NeedHdr);
		}

		//必要なタイプによってここを override　して書き換え
		protected virtual bool NeedRenderTexture { get { return false; } }
		protected virtual bool NeedDepth { get { return false; } }
		protected virtual bool NeedHdr { get { return false; } }


		//スクリプトからAddComponentした時のシェーダーの設定
		//各派生クラスで override
		public abstract void SetShaders(params Shader[] shadres);

		//シェーダーをチェックしてマテリアルを作る処理
		//各派生クラスで override
		protected abstract bool CheckShaderAndCreateMaterial();

		//ハードのサポートチェック
		protected bool CheckSupport(bool needRenderTexture, bool needDepth, bool needHdr)
		{
			if (!CheckSupportSub(needRenderTexture, needDepth, needHdr))
			{
				return false;
			}
			if (needDepth)
			{
				GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
			}
			return true;
		}

		//ハードのサポートチェック
		protected bool CheckSupportSub(bool needRenderTexture, bool needDepth, bool needHdr)
		{
			if (!ImageEffectUtil.SupportsImageEffects)
			{
				return false;
			}

			if (needRenderTexture && !ImageEffectUtil.SupportsRenderTextures)
			{
				return false;
			}

			if (needDepth && !ImageEffectUtil.SupportsDepth)
			{
				return false;
			}

			if (needHdr && !ImageEffectUtil.SupportsHDRTextures)
			{
				return false;
			}
			return true;
		}

		//シェーダーをチェックして作成
		protected bool TryCheckShaderAndCreateMaterialSub(Shader s, Material m2Create, out Material mat)
		{
			mat = null;
			if (!s)
			{
				//シェーダーがない
				Debug.Log("Missing shader in " + ToString());
				return false;
			}

			if (!s.isSupported)
			{
				//シェーダーがサポートされていない
				Debug.Log("The shader " + s.ToString() + " on effect " + ToString() + " is not supported on this platform!");
				return false;
			}

			if (m2Create && m2Create.shader == s)
			{
				//マテリアルのシェーダーが指定のシェーダーと同じ
				mat = m2Create;
				return true;
			}
			else
			{
				//マテリアルを作成し、シェーダーを設定
				m2Create = new Material(s);
				createdMaterials.Add(m2Create);
				m2Create.hideFlags = HideFlags.DontSave;
				mat = m2Create;
				return true;
			}
		}

		//マテリアルをすべて消去
		void ClearCreatedMaterials()
		{
			foreach(Material mat in this.createdMaterials)
			{
#if UNITY_EDITOR
				DestroyImmediate(mat);
#else
                Destroy(mat);
#endif
			}
			createdMaterials.Clear();
		}

		//描画処理
		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (CheckResources() == false)
			{
				//チェック失敗なので通常描画
				Graphics.Blit(source, destination);
				return;
			}
			else
			{
				RenderImage(source, destination);
			}
		}

		//ここをoverrideして描画ロジックを書く
		protected abstract void RenderImage(RenderTexture source, RenderTexture destination);

		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(this.enabled);
			writer.WriteJson(this);
		}

		public virtual void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			StoreObjectsOnJsonRead();
			this.enabled = reader.ReadBoolean();
			reader.ReadJson(this);
			RestoreObjectsOnJsonRead();
		}

		//Json読み込みの時にUnityEngine.Objectも対象になってしまうので、それをいったん記録して戻す
		protected abstract void StoreObjectsOnJsonRead();
		protected abstract void RestoreObjectsOnJsonRead();
	}
}
