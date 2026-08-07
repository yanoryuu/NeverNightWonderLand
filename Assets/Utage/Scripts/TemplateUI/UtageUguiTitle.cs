// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// タイトル表示のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiTitle")]
	public class UtageUguiTitle : UguiView
	{
		/// <summary>スターター</summary>
		public AdvEngineStarter Starter
		{
			get { return this.GetComponentCacheFindIfMissing(ref starter); }
		}

		[SerializeField] protected AdvEngineStarter starter;

		/// <summary>メインゲーム画面</summary>
		public UtageUguiMainGame mainGame;

		/// <summary>コンフィグ画面</summary>
		public UtageUguiConfig config;

		/// <summary>セーブデターのロード画面</summary>
		public UtageUguiSaveLoad load;

		///<summary>ギャラリー画面</summary>
		public UtageUguiGallery gallery;
		public bool resetTabIndexOnOpenGallery = false;

		///<summary>ダウンロード画面</summary>
		public UtageUguiLoadWait download;

		///<summary>ダウンロードボタン</summary>
		public GameObject downloadButton;

		protected virtual void OnOpen()
		{
			//		if (Starter != null && Starter.enabled != AdvEngineStarter.ScenarioLoadType.Server)
			{
				if (downloadButton != null)
				{
					downloadButton.SetActive(false);
				}
			}
		}

		///「はじめから」ボタンが押された
		public virtual void OnTapStart()
		{
			Close();
			mainGame.OpenStartGame();
		}

		///「つづきから」ボタンが押された
		public virtual void OnTapLoad()
		{
			Close();
			load.OpenLoad(this);
		}

		///「コンフィグ」ボタンが押された
		public virtual void OnTapConfig()
		{
			Close();
			config.Open(this);
		}

		//「ギャラリー」ボタンが押された
		public virtual void OnTapGallery()
		{
			Close();
			if (resetTabIndexOnOpenGallery)
			{
				gallery.OpenAndResetAllTabIndex(this);
			}
			else
			{
				gallery.Open(this);
			}
		}

		//「ダウンロード」ボタンが押された
		public virtual void OnTapDownLoad()
		{
			Close();
			download.Open(this);
		}

		///「指定のラベルからスタート」ボタンが押された
		public virtual void OnTapStartLabel(string label)
		{
			Close();
			mainGame.OpenStartLabel(label);
		}


		protected virtual void OnCloseLoadChapter(string startLabel)
		{
			download.onClose.RemoveAllListeners();
			Close();
			mainGame.OpenStartLabel(startLabel);
		}
	}
}
