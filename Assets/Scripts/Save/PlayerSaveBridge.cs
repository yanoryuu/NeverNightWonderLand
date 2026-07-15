using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// セーブデータとプレイヤーの橋渡し。
/// - シーン開始時: GameSession.PendingLoad が現在シーンのものなら適用する (位置・強化・アイテム・糸)
/// - セーブ時: SavePoint から Collect() でデータを組み立てる
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSaveBridge : MonoBehaviour
{
    private PlayerProgression _progression;
    private PlayerItemInventory _inventory;

    private void Awake()
    {
        _progression = GetComponent<PlayerProgression>();
        _inventory = GetComponent<PlayerItemInventory>();
    }

    private void Start()
    {
        var data = GameSession.PendingLoad;
        if (data == null)
            return;

        GameSession.PendingLoad = null;

        if (data.sceneName != SceneManager.GetActiveScene().name)
            return;

        transform.position = new Vector3(data.posX, data.posY, transform.position.z);
        _progression?.LoadFrom(data);
        _inventory?.LoadFrom(data);
    }

    /// <summary>現在の状態からセーブデータを組み立てる。復帰位置は呼び出し側 (拠点) が上書きする。</summary>
    public SaveData Collect()
    {
        var data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            posX = transform.position.x,
            posY = transform.position.y,
            upgrades = _progression != null ? _progression.CollectUpgrades() : new int[0],
            thread = _inventory != null ? _inventory.Thread.CurrentValue : 0,
        };

        if (_inventory != null)
        {
            _inventory.CollectCounts(out var ids, out var counts);
            data.itemIds = ids;
            data.itemCounts = counts;
            data.slotItemIds = _inventory.CollectSlots();
        }

        return data;
    }
}
