using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// セーブデータとプレイヤーの橋渡し。
/// - シーン開始時: GameSession.PendingLoad が現在シーンのものなら適用する (位置・強化・アイテム・糸)
/// - セーブ時: SavePoint から Collect() でデータを組み立てる
/// - 能力 (ハサミ強化・スキル) の取得時: このプレイのセーブがあれば、その場でセーブの能力にも書き足す
///   (能力は一度取ったら死亡・終了でも失わない。PendingChanges 38)
/// - 死亡リトライ: <see cref="CollectForRetry"/> で引き継ぐデータを作る
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSaveBridge : MonoBehaviour
{
    private PlayerProgression _progression;
    private PlayerItemInventory _inventory;
    private PlayerAttackLoadout _attackLoadout;

    private void Awake()
    {
        _progression = GetComponent<PlayerProgression>();
        _inventory = GetComponent<PlayerItemInventory>();
        _attackLoadout = GetComponent<PlayerAttackLoadout>();
    }

    private void OnEnable()
    {
        if (_progression == null)
            return;

        _progression.OnUpgradeGranted += OnUpgradeGranted;
        _progression.OnSkillGranted += OnSkillGranted;
    }

    private void OnDisable()
    {
        if (_progression == null)
            return;

        _progression.OnUpgradeGranted -= OnUpgradeGranted;
        _progression.OnSkillGranted -= OnSkillGranted;
    }

    private void OnUpgradeGranted(ScissorUpgrade _) => PersistAbilities();

    private void OnSkillGranted(PlayerSkill _) => PersistAbilities();

    /// <summary>
    /// 取得済みの能力をこのプレイのセーブに書き足す (位置・アイテム・フラグ等はセーブしたときのまま)。
    /// 拠点でセーブする前に取った能力も、死亡リトライ・ゲームの終了で失わないようにする。
    /// </summary>
    private void PersistAbilities()
    {
        if (!GameSession.SaveBelongsToRun || _progression == null)
            return;

        var save = SaveSystem.TryLoad();
        if (save == null)
            return;

        MergeAbilities(save);
        SaveSystem.Save(save);
    }

    /// <summary>セーブデータの能力に、今の取得状況を足す (セーブ側にしか無い物も消さない)。</summary>
    private void MergeAbilities(SaveData data)
    {
        if (_progression == null)
            return;

        data.upgrades = (data.upgrades ?? new int[0]).Union(_progression.CollectUpgrades()).ToArray();
        data.skills = (data.skills ?? new int[0]).Union(_progression.CollectSkills()).ToArray();
    }

    /// <summary>
    /// 死亡リトライ (PlayerScene ごと読み直す) で引き継ぐデータ。能力 (ハサミ強化・スキル) は死亡で失わない。
    /// - このプレイのセーブが今のステージにあれば拠点から再開する (セーブの状態に戻す)。能力はセーブ後に取った物も残す
    /// - 無ければ今の状態 (能力・アイテム・糸・コイン・装備・進行フラグ) をそのまま持って、ステージの入り口から再開する
    ///   (以前はプレイヤーを作り直すだけで、能力・アイテム・糸などが初期状態に戻っていた)
    /// </summary>
    public SaveData CollectForRetry(string stageName)
    {
        var save = GameSession.SaveBelongsToRun ? SaveSystem.TryLoad() : null;
        if (save != null && save.sceneName == stageName)
        {
            MergeAbilities(save);
            return save;
        }

        var data = Collect();
        data.sceneName = stageName;
        data.useStageSpawn = true;
        return data;
    }

    private void Start()
    {
        // PlayerScene 方式ではステージのロード完了後に StageLoader が適用する
        if (StageLoader.Instance != null)
            return;

        TryApplyPendingLoad();
    }

    /// <summary>
    /// PendingLoad が現在のステージのものなら適用する (位置・強化・アイテム・装備)。
    /// 復帰位置へ置いたら true (StageLoader がステージロード後に呼ぶ)。
    /// 死亡リトライの引き継ぎ (useStageSpawn) は位置を変えずに false を返し、StageLoader が入り口へ置く。
    /// </summary>
    public bool TryApplyPendingLoad()
    {
        var data = GameSession.PendingLoad;
        if (data == null)
            return false;

        var stageName = StageLoader.Instance != null
            ? StageLoader.Instance.CurrentStageName
            : SceneManager.GetActiveScene().name;
        if (data.sceneName != stageName)
            return false;

        GameSession.PendingLoad = null;

        if (!data.useStageSpawn)
            transform.position = new Vector3(data.posX, data.posY, transform.position.z);
        _progression?.LoadFrom(data);
        _inventory?.LoadFrom(data);
        _attackLoadout?.LoadFrom(data);
        GameProgress.LoadFrom(data);
        return !data.useStageSpawn;
    }

    /// <summary>拠点でセーブする時のデータ (復帰位置 = 拠点の位置)。</summary>
    public SaveData CollectAt(SavePoint savePoint)
    {
        var data = Collect();
        if (savePoint != null)
        {
            data.posX = savePoint.transform.position.x;
            data.posY = savePoint.transform.position.y + 0.5f;
        }

        return data;
    }

    /// <summary>場所の表示名: StageInfo の部屋名 → 旧ステージの AreaTitleView の名前 → シーン名。</summary>
    private static string CurrentPlaceName()
    {
        if (StageInfo.Current != null && !string.IsNullOrEmpty(StageInfo.Current.AreaName))
            return StageInfo.Current.AreaName;

        foreach (var title in FindObjectsByType<AreaTitleView>(FindObjectsSortMode.None))
        {
            if (!title.gameObject.scene.name.EndsWith("UI") && !string.IsNullOrEmpty(title.AreaName))
                return title.AreaName;
        }

        return StageLoader.Instance != null ? StageLoader.Instance.CurrentStageName : SceneManager.GetActiveScene().name;
    }

    /// <summary>現在の状態からセーブデータを組み立てる。復帰位置は呼び出し側 (拠点) が上書きする。</summary>
    public SaveData Collect()
    {
        var data = new SaveData
        {
            sceneName = StageLoader.Instance != null
                ? StageLoader.Instance.CurrentStageName
                : SceneManager.GetActiveScene().name,
            posX = transform.position.x,
            posY = transform.position.y,
            upgrades = _progression != null ? _progression.CollectUpgrades() : new int[0],
            skills = _progression != null ? _progression.CollectSkills() : new int[0],
            forgeLevel = _progression != null ? _progression.ForgeLevel : 0,
            thread = _inventory != null ? _inventory.Thread.CurrentValue : 0,
            coins = _inventory != null ? _inventory.Coins.CurrentValue : 0,
            progressFlags = GameProgress.Collect(),
            savedAt = System.DateTime.Now.ToString("o"),
            playTimeSeconds = GameSession.PlayTimeSeconds,
            chapterName = StageInfo.Current != null ? StageInfo.Current.ChapterName : "",
            placeName = CurrentPlaceName(),
        };

        if (_inventory != null)
        {
            _inventory.CollectCounts(out var ids, out var counts);
            data.itemIds = ids;
            data.itemCounts = counts;
            data.slotItemIds = _inventory.CollectSlots();
        }

        _attackLoadout?.CollectTo(data);

        return data;
    }
}
