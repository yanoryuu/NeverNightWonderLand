#if UTAGE_URP_EDITOR
using UnityEditor;

namespace Utage.RenderPipeline.Urp
{
	public class UrpExtensionPackageImporter
	{
		//エディタ起動時やスクリプトリロード時に、追加パッケージの情報を登録
		[InitializeOnLoadMethod]
		static void Initialize()
		{
			//デフォルトアセット用のパッケージ
#if UNITY_6000_6_OR_NEWER
			//6000.6以降は署名付きパッケージを使用
			ExtensionPackageManager.Instance.AddPackage(new ExtensionPackage("772711b75b34d2c4ba739ef28966e71e",1));
#else
			ExtensionPackageManager.Instance.AddPackage(new ExtensionPackage("d823085cc08011a418659a5eb6fabaec",1));
#endif
#if URP_17_OR_NEWER
			//Urp17以降用のパッケージ
			//シェーダーグラフのアセットなど
#if UNITY_6000_6_OR_NEWER
			//6000.6以降は署名付きパッケージを使用
			ExtensionPackageManager.Instance.AddPackage(new ExtensionPackage("5c3b5e869f5f5f34b993d2a1f55cb777", 1));
#else
			ExtensionPackageManager.Instance.AddPackage(new ExtensionPackage("ba83337a383980049a8d1796c51d7c7c", 1));
#endif
#endif
		}
	}
}
#endif
