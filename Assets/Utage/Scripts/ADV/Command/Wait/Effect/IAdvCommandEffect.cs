// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;

namespace Utage
{

	// 再生中のエフェクトを、エフェクト終了時点までスキップして止める
	public interface IAdvCommandEffect
	{
		//エフェクトの終了処理。キャッシュした参照などをクリアする
		void OnEffectFinalize();
		//エフェクトをスキップする
		void OnEffectSkip();
	}

	// 無限ループエフェクトが存在するコマンドか
	public interface IAdvCommandEffectLoop : IAdvCommandEffect
	{
		//無限ループエフェクトか
		bool IsLoopEffect();
	}

	//コールバックでは実行されず、終わったかの終了チェックが必要なものをここで呼ぶ
	//対象のコマンドがWait待機中にしか呼ばれないので、毎フレームの時間加算などには使えない点に注意
	public interface IAdvCommandUpdateWait
	{
		bool UpdateCheckWait();
	}
}
