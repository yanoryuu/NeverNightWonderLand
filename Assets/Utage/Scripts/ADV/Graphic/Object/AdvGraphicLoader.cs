// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

namespace Utage
{

	/// <summary>
	/// ファイルのロードと参照管理
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicLoader")]
	public class AdvGraphicLoader : MonoBehaviour
	{
		public UnityEvent OnComplete = new UnityEvent();

		AdvGraphicInfo graphic;
		public bool IsLoading
		{
			get
			{
				if (graphic == null) return false;

				//新しいファイルへの参照
				return !graphic.File.IsLoadEnd;
			}
		}

		//新しいグラフィックをロードする（古いのはアンロード）
		public void LoadGraphic(AdvGraphicInfo graphic, Action onComplete )
		{
			Unload();
			this.graphic = graphic;
			//新しいファイルへの参照
			AssetFileManager.Load(graphic.File, this);
			StartCoroutine(CoLoadWait(onComplete));
		}

		//ロード待ち
		IEnumerator CoLoadWait(Action onComplete)
		{
			while(IsLoading)
			{
				yield return null;
			}
			OnComplete.Invoke();
			if (onComplete != null) onComplete();
		}

		//ファイルがあればそれをアンロード（正確にはロードへの参照を切る）
		public void Unload()
		{
			if (this.graphic == null) return;

			this.graphic.File.Unuse(this);
			this.graphic = null;
		}

		void OnDestroy()
		{
			Unload();
		}
	}
}