// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;

namespace Utage
{
	// フェード処理用のインターフェース
	public interface IAdvFade
	{
		//フェードイン
		void FadeIn(float time, Action onComplete);

		//フェードアウト
		void FadeOut(float time, Action onComplete);

		//ルール画像つきのフェードイン
		void RuleFadeIn(AdvEngine engine, AdvTransitionArgs data, Action onComplete);

		//ルール画像つきのフェードアウト
		void RuleFadeOut(AdvEngine engine, AdvTransitionArgs data, Action onComplete);
	}
	
	
	// スキップ可能なフェード処理用のインターフェース
	public interface IAdvFadeSkippable : IAdvFade
	{
		//ルール画像付きのフェードをスキップする
		void SkipRuleFade();
	}
	
	// アニメーション制御する際の
	public interface IAdvFadeAnimation : IAdvFadeSkippable
	{
		//ルール画像制御用のコンポーネントを初期化して返す
		IAnimationRuleFade BeginRuleFade(AdvEngine engine, AdvTransitionArgs data);
		
		//フェードアウトのときにオブジェクトの消去を行う
		void Clear();
	}
}
