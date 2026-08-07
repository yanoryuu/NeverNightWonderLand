using System;

namespace Utage
{
	//staticなFieldにつけるattribute
	//メモリリークがある場合などに、staticな部分を見つけるために使う
	//テストでマークのついてないものにもエラーをだす
	[AttributeUsage(AttributeTargets.Field)]
	public class StaticField : Attribute
	{
	}

	//RuntimeInitialize時に、初期化が必要なstaticな変数やコールバックに設定する目印となるattribute
	//Domain Reloadingがオフになる場合の対応が、RuntimeInitialize時にできているかをチェックするのに使う
	//staticでも初期化を行わない以下のものは、通常のStaticFieldを設定すること
	//	・シングルトンなMonoBehaviourのインスタンス（null判定で判別できるので）
	//	・高速化用のワーキングメモリ用の変数
	//	・コンパイル以外で変更されないような処理
	//	・既に未使用なものなど初期化しない
	[AttributeUsage(AttributeTargets.Field)]
	public class RuntimeInitializeStaticField : StaticField
	{
	}

}
