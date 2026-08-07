// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Utage
{

	/// <summary>
	/// ファイルタイプ
	/// </summary>
	public enum AssetFileType
	{
		/// <summary>テキスト</summary>
		Text,
		/// <summary>テクスチャ</summary>
		Texture,
		/// <summary>サウンド</summary>
		Sound,
		/// <summary>その他のオブジェクト</summary>
		UnityObject,
	};

	/// <summary>
	/// ファイルのおき場所のタイプ
	/// </summary>
	public enum AssetFileStrageType
	{
		/// <summary>サーバー</summary>
		Server,
		/// <summary>ストリーミングアセット</summary>
		StreamingAssets,
		/// <summary>リソース</summary>
		Resources,
	};

	/// <summary>
	/// ロードする際のフラグ
	/// </summary>
	[System.Flags]
	public enum AssetFileLoadFlags
	{
		/// <summary>なにもなし</summary>
		None = 0x00,
		/// <summary>ストリーミングでロードする</summary>
		Streaming = 0x01,
		/// <summary>3Dサウンドとしてロードする</summary>
		Audio3D = 0x02,
		/// <summary>テクスチャにミップマップを使う</summary>
		TextureMipmap = 0x04,
		/// <summary>CSVをロードする際にTSV形式でロードする</summary>
		Tsv = 0x08,
	};

	/// <summary>
	/// ロードの優先順
	/// </summary>
	public enum AssetFileLoadPriority
	{
		Default,				//通常
		Preload,				//先読み
		BackGround,				//バックグラウンドでのロード
		DownloadOnly,			//ダウンロードのみ
	};

	/// <summary>
	/// ファイル設定のインターフェース
	/// </summary>
	public interface IAssetFileSettingData
	{
		StringGridRow RowData { get; }
	}

	/// <summary>
	/// サウンドファイル設定のインターフェース
	/// </summary>
	public interface IAssetFileSoundSettingData : IAssetFileSettingData
	{
		/// <summary>
		/// イントロループ用のループポイント
		/// </summary>
		float IntroTime { get; }

		/// <summary>
		/// ボリューム
		/// </summary>
		float Volume { get; }
	}

	/// サウンドファイル設定のキー取得のためのインターフェース
	public interface IAssetFileSoundSettingDataKey : IAssetFileSettingData
	{
		string Key { get; }
	}

	/// <summary>
	/// ファイルのインターフェース
	/// </summary>
	public interface AssetFile
	{
		/// <summary>ファイル名</summary>
		string FileName { get; }

		/// <summary>エクセルなどで設定したデータ（nullの場合もあるので注意）</summary>
		IAssetFileSettingData SettingData { get; }

		AssetFileType FileType { get; }

		/// <summary>関連ファイルも含めてすべてロード終了したか</summary>
		bool IsLoadEnd { get; }

		/// <summary>ロードエラーしたか</summary>
		bool IsLoadError { get; }

		/// <summary>ロードエラーメッセージ</summary>
		string LoadErrorMsg { get; }

		/// <summary>ロードしたTextAsset</summary>
		TextAsset Text { get; }

		/// <summary>ロードしたテクスチャ</summary>
		Texture2D Texture { get; }

		/// <summary>ロードしたサウンド</summary>
		AudioClip Sound { get; }

		/// <summary>ロードしたUnityオブジェクト</summary>
		UnityEngine.Object UnityObject { get; }

		/// <summary>
		/// オブジェクトがファイルを使用することを宣言（参照を設定する）
		/// </summary>
		/// <param name="obj">使用するオブジェクト</param>
		void Use(System.Object obj);

		/// <summary>
		/// オブジェクトがファイルを使用することをやめる（参照を解除する）
		/// </summary>
		/// <param name="obj">使用をやめるオブジェクト</param>
		void Unuse(System.Object obj);

		/// <summary>
		/// Gameオブジェクトに、このファイルの参照コンポーネントを追加
		/// これを使った後は、GameオブジェクトがDestoryされると自動的に、参照が解除される
		/// </summary>
		/// <param name="go">参照をするGameObject</param>
		void AddReferenceComponent(GameObject go);
	};

	public delegate void AssetFileLoadComplete(AssetFile file);
	public delegate void AssetFileEvent(AssetFile file);
}