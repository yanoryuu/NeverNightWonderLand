// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UtageExtensions;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Utage
{

	/// <summary>
	/// ノベル用のイメージ表示
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/AvatarImage")]
    [ExecuteInEditMode,RequireComponent(typeof(RectTransform))]
	public class AvatarImage : MonoBehaviour
	{
		public AvatarData AvatarData
		{
			get { return avatarData; }
			set
			{
				avatarData = value;
				avatarPattern.Rebuild(AvatarData);
				HasChanged = true;
			}
		}
		[SerializeField]
		AvatarData avatarData;

		public AvatarPattern AvatarPattern
		{
			get { return avatarPattern; }
			set
			{
				avatarPattern = value;
				HasChanged = true;
			}
		}
		[SerializeField,NovelAvatarPattern("AvatarData")]
		AvatarPattern avatarPattern = new AvatarPattern();

        public RectTransform CachedRectTransform
        {
            get
            {
                if (cachedRectTransform == null)
                {
                    cachedRectTransform = GetComponent<RectTransform>();
                }
                return cachedRectTransform;
            }
        }
        RectTransform cachedRectTransform;

		public Material Material
		{
			get { return material; }
			set
			{
				material = value;
				HasChanged = true;
			}
		}
		[SerializeField]
		Material material;

		public UnityEvent OnPostRefresh = new UnityEvent();


		//上下左右の反転
		public bool FlipX { get { return flipX; } set { flipX = value; HasChanged = true; } }
		[SerializeField]
		bool flipX;

		public bool FlipY { get { return flipY; } set { flipY = value; HasChanged = true; } }
		[SerializeField]
		bool flipY;

		//上下左右の反転
		public void Flip(bool flipX, bool flipY)
		{
			this.FlipX = flipX;
			this.FlipY = flipY;
		}

		RectTransform RootChildren
        {
            get
            {
                if (rootChildren == null)
                {
                    rootChildren = this.transform.AddChildGameObjectComponent<RectTransform>("childRoot");
                    rootChildren.gameObject.hideFlags = HideFlags.DontSave;
                }
                return rootChildren;
            }
        }

		RectTransform rootChildren;

        bool HasChanged { get; set; }

		void OnEnable()
        {
            HasChanged = true;
        }

#if UNITY_EDITOR
		void OnValidate()
        {
			HasChanged = true;
        }
#endif

        void LateUpdate()
        {
            if (HasChanged)
            {
                Refresh();
                HasChanged = false;
            }
        }

        void Refresh()
        {
			RootChildren.DestroyChildrenInEditorOrPlayer();
			avatarPattern.Rebuild(AvatarData);
			MakeImageFromAvartorData(AvatarData);
			OnPostRefresh.Invoke();
		}

		//イメージデータから作成
		void MakeImageFromAvartorData(AvatarData data)
		{
			if (AvatarData == null) return;
			data.CheckPatternError(avatarPattern);
			List<Sprite> parts = data.MakeSortedSprites(avatarPattern);
			foreach (var part in parts)
			{
				if (part == null) continue;
				RectTransform child = RootChildren.AddChildGameObjectComponent<RectTransform>(part.name);
				child.gameObject.hideFlags = HideFlags.DontSave;
				Image image = child.gameObject.AddComponent<Image>();
				image.material = this.Material;
				image.sprite = part;
				image.SetNativeSize();

				UguiFlip flip = image.gameObject.AddComponent<UguiFlip>();
				flip.FlipX = flipX;
				flip.FlipY = FlipY;
			}
		}

		public void ChangePattern(string tag, string patternName)
		{
			this.avatarPattern.SetPatternName(tag, patternName);
			HasChanged = true;
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(AvatarImage))]
		public class UguiNovelImageAvatarInspector : Editor
		{
			AvatarData Obj { get { return this.target as AvatarData; } }

			//プレビュー表示する場合true
			public override bool HasPreviewGUI()
			{
				return true;
			}

			public override void OnPreviewGUI(Rect r, GUIStyle background)
			{
				AvatarImage obj = this.target as AvatarImage;
				if (obj == null) return;
				if (obj.AvatarData == null) return;
				obj.AvatarData.OnPreviewGUI(r,background,obj.avatarPattern);
			}
		}
#endif
	}
}

