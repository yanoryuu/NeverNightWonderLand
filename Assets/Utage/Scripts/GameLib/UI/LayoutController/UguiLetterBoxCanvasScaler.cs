using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// レターボックスつきのカメラ制御から、解像度を参照してキャンバスに設定する
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Canvas))]
	[AddComponentMenu("Utage/Lib/UI/UguiLetterBoxCanvasScaler")]
	public class UguiLetterBoxCanvasScaler : UguiLayoutControllerBase, ILayoutSelfController
	{
		public Canvas Canvas {
			get
			{
				if(canvas==null)
				{
					canvas = GetComponent<Canvas>();
				}
				return canvas;
			}
		}
		Canvas canvas;

		public LetterBoxCamera LetterBoxCamera
		{
			get
			{
				if (letterBoxCamera == null)
				{
					if (Canvas.worldCamera == null)
					{
						if (!IsPrefabAsset()) Debug.LogError("Canvas worldCamera is null");
					}
					else
					{
						letterBoxCamera = Canvas.worldCamera.GetComponent<LetterBoxCamera>();
					}
				}
				return letterBoxCamera;
			}
		}
		LetterBoxCamera letterBoxCamera;

		protected override void Update()
		{
			//ゲーム解像度設定
			Vector2 size = LetterBoxCamera.CurrentSize;
			if (!Mathf.Approximately(size.x, CachedRectTransform.sizeDelta.x) || !Mathf.Approximately(size.y, CachedRectTransform.sizeDelta.y))
			{
				SetDirty();
				return;
			}

			//ゲームスケール値設定
			float scale = 1.0f / LetterBoxCamera.PixelsToUnits;
			if (!Mathf.Approximately(scale, CachedRectTransform.localScale.x)
				|| !Mathf.Approximately(scale, CachedRectTransform.localScale.y)
				|| !Mathf.Approximately(scale, CachedRectTransform.localScale.z)
				)
			{
				SetDirty();
				return;
			}
		}

		public void SetLayoutHorizontal()
		{
			tracker.Clear();

			if (Canvas.renderMode != RenderMode.WorldSpace)
			{
				if (!IsPrefabAsset()) Debug.LogError("LetterBoxCanvas is not RenderMode.World");
				return;
			}
			if (LetterBoxCamera == null)
			{
				if( !IsPrefabAsset()) Debug.LogError("LetterBoxCamera is null");
				return;
			}

			tracker.Add(this, CachedRectTransform,
				DrivenTransformProperties.Anchors |
				DrivenTransformProperties.Scale |
				DrivenTransformProperties.SizeDelta);
			//アンカー設定
			CachedRectTransform.anchorMin = CachedRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			//ゲーム解像度設定
			CachedRectTransform.sizeDelta = LetterBoxCamera.CurrentSize;
			//ゲームスケール値設定
			float scale = 1.0f / LetterBoxCamera.PixelsToUnits;
			CachedRectTransform.localScale = Vector3.one * scale;
		}

		public void SetLayoutVertical()
		{
		}

		bool IsPrefabAsset()
		{
#if UNITY_EDITOR
			if (UnityEditor.AssetDatabase.Contains(this.transform.root.gameObject))
			{
				return true;
			}
#endif
			return false;
		}

	}
}