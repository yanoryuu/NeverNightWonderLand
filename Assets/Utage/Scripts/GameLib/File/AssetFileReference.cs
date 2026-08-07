// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// ファイルの参照管理
	/// </summary>
	[AddComponentMenu("Utage/Lib/File/AssetFileReference")]
	public class AssetFileReference : MonoBehaviour
	{
		/// <summary>
		/// 参照しているファイル
		/// </summary>
		public AssetFile File { get { return file; } }
		AssetFile file;

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="file">参照するファイル</param>
		public void Init(AssetFile file)
		{
			this.file = file;
			this.file.Use(this);
		}

		void OnDestroy()
		{
			this.file.Unuse(this);
		}
	}
}