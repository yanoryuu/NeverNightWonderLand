using UnityEngine;

/// <summary>
/// 拾うとハサミ強化 (黄/青/赤) を得られるピックアップ。エリアボス撃破後のご褒美 (赤 = 二段ジャンプ) など。
/// 取得の通知 (「赤色のハサミを手に入れた!」) は PlayerProgression.Grant が出し、
/// 続けて _hintMessage (操作説明など, 任意) を通知する。
/// 強化そのものは PlayerProgression 経由でセーブされ、ピックアップの消滅は _pickupId で永続化される。
/// </summary>
public class UpgradePickup : ItemPickup
{
    [Tooltip("得られるハサミ強化")]
    [SerializeField] private ScissorUpgrade _upgrade = ScissorUpgrade.Red;

    [Tooltip("取得後に続けて出す通知 (操作説明など, 任意)")]
    [SerializeField] private string _hintMessage = "空中でもう一度ジャンプできるようになった";

    protected override bool OnPickedUp(GameObject interactor)
    {
        var progression = interactor.GetComponent<PlayerProgression>();
        if (progression == null)
            return false;

        progression.Grant(_upgrade); // 取得通知は Grant が出す (取得済みなら何もしない)
        Notifier.Notify(_hintMessage);
        return true;
    }
}
