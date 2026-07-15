using UnityEngine;

/// <summary>
/// 布カッター: 前方に突進し、触れた敵にダメージ+ノックバック。被弾でキャンセルされる。
/// 突進パラメータは DashItemDefinition が持ち、ItemDashState が参照する。
/// </summary>
[CreateAssetMenu(fileName = "ClothCutter", menuName = "NeverNight/Items/布カッター")]
public class ClothCutterItem : DashItemDefinition
{
}
