using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Utage;
using UtageExtensions;

namespace Utage
{
	//拡張packageの自動インポーター
	public class ExtensionPackageAutoImporter
	{
		static ExtensionPackageAutoImporter Instance { get; } = new ();

		[InitializeOnLoadMethod]
		static void InitializeOnLoad()
		{
			Instance.Initialize();
		}
		
		bool DebugLog { get; } = ExtensionPackageManager.Instance.DebugLog;

		//インポート処理開始までのカウント
		int UpdateCount { get; set; }
		//インポート処理の遅延カウント
		const int DelayCount = 1;
		
		//エディタ起動時やスクリプトリロード時に自動インポート処理を開始
		void Initialize()
		{
			if (DebugLog) Debug.Log("Initialize");
			if(!EnableEditorUpdate()) return;

			//他のパッケージ情報もInitializeOnLoadMethodで登録するので指定フレーム待ってから処理を開始するため
			//EditorApplication.updateを使って数フレーム待機してから登録を行う
			UpdateCount = 0;
			EditorApplication.update -= EditorUpdate;
			EditorApplication.update += EditorUpdate;
			
			return;
			
			//インポート処理を開始するかのチェック
			bool EnableEditorUpdate()
			{
				//ゲームプレイ中やビルド中は無効
				if (Application.isPlaying) return false;
				if (BuildPipeline.isBuildingPlayer) return false;
				if (EditorApplication.isPlaying) return false;
				return true;
			}
		}

		//エディタのアップデート処理
		void EditorUpdate()
		{
			if(DebugLog) Debug.Log($"EditorUpdate {UpdateCount} ");

			if (!DelayUpdate())
			{
				//インポート処理が終了、キャンセルしたら終了処理を行う
				OnFinalize();
				return;
			}
		}
		
		//アップデートの待機処理の実行
		//待機中はtrueを返す
		bool DelayUpdate()
		{
			//指定カウント待機
			if (UpdateCount < DelayCount)
			{
				UpdateCount++;
				return true;
			}

			//必要な場合のみパッケージのインポート処理を行う
			if (ExtensionPackageManager.Instance.IsNeedImportPackages())
			{
				if (DebugLog) Debug.Log($"ImportPackages");
				ExtensionPackageManager.Instance.ImportPackages();
			}
			return false;
		}

		void OnFinalize()
		{
			if (DebugLog) Debug.Log($"OnFinalize");
			EditorApplication.update -= EditorUpdate;
		}
	}
}
