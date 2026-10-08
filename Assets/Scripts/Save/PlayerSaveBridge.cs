using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// セーブデータとプレイヤーの橋渡し。
/// - シーン開始時: GameSession.PendingLoad が現在シーンのものなら適用する (位置・強化・アイテム・糸)
/// - セーブ時: SavePoint から CollectAt() でデータを組み立てる
/// - 永続する進行 (能力・鍛冶強化・キーアイテム・進行フラグ・コイン・拾った物・糸・装備) が変わったら、
///   このプレイのセーブへその場で書き込む (位置・場所の表示・日時・プレイ時間はセーブした拠点のまま。PendingChanges 39)
/// - 死亡リトライ: <see cref="CollectForRetry"/> で今の状態のまま最後に休んだ拠点から再開するデータを作る
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSaveBridge : MonoBehaviour
{
    /// <summary>進行の変化からセーブへ書き込むまでの待ち (秒, 実時間)。同じ時に起きた変化 (フラグ → 報酬) をまとめて 1 回で書く。</summary>
    private const float AutosaveDelay = 0.5f;

    private PlayerProgression _progression;
    private PlayerItemInventory _inventory;
    private PlayerAttackLoadout _attackLoadout;

    private readonly CompositeDisposable _subscriptions = new();
    private bool _subscribed;

    // このプレイヤーが生まれた時、すでにこのプレイのセーブがあったか (あれば、このプレイの続きのデータを適用するか
    // 拠点でセーブするまでは書き込まない = _runLinked)
    private bool _spawnedWithRunSave;
    private bool _runLinked;

    private bool _autosaveDirty;
    private float _autosaveDirtyAt;

    private void Awake()
    {
        _progression = GetComponent<PlayerProgression>();
        _inventory = GetComponent<PlayerItemInventory>();
        _attackLoadout = GetComponent<PlayerAttackLoadout>();
        _spawnedWithRunSave = GameSession.SaveBelongsToRun;
    }

    /// <summary>
    /// 今の状態をこのプレイのセーブへ書き込んでよいか。
    /// はじめから (拠点でセーブするまで)・エディタの直接再生では false (前のプレイのセーブを書き換えない)。
    /// セーブがある状態で生まれたプレイヤーは、つづきから・死亡リトライのデータを適用するか拠点でセーブするまで false
    /// (初期状態のままセーブを上書きしない。ゲームクリア後の「もう一度」・テストプレイの記録の再生もここで弾く)。
    /// </summary>
    private bool LinkedToRunSave => GameSession.SaveBelongsToRun && (!_spawnedWithRunSave || _runLinked);

    /// <summary>進行の変化を購読する (各コンポーネントの Awake で Model の初期化が済んだ後)。</summary>
    private void Subscribe()
    {
        if (_subscribed)
            return;

        _subscribed = true;
        GameProgress.Changed += OnProgressChanged;
        if (_progression != null)
        {
            _progression.OnUpgradeGranted += OnUpgradeGranted;
            _progression.OnSkillGranted += OnSkillGranted;
        }

        if (_inventory != null)
        {
            _inventory.Changed.Subscribe(_ => MarkAutosave()).AddTo(_subscriptions);
            _inventory.Thread.Skip(1).Subscribe(_ => MarkAutosave()).AddTo(_subscriptions);
            _inventory.Coins.Skip(1).Subscribe(_ => MarkAutosave()).AddTo(_subscriptions);
        }

        _attackLoadout?.Changed.Subscribe(_ => MarkAutosave()).AddTo(_subscriptions);
    }

    private void OnDestroy()
    {
        // 待ちの間に終了・タイトルへ戻った時も書き込んでおく
        FlushAutosave();

        if (!_subscribed)
            return;

        GameProgress.Changed -= OnProgressChanged;
        if (_progression != null)
        {
            _progression.OnUpgradeGranted -= OnUpgradeGranted;
            _progression.OnSkillGranted -= OnSkillGranted;
        }

        _subscriptions.Dispose();
    }

    // 鍛冶強化 (ForgeLevel) は鍛冶師が必ずフラグと同時に上げるので、フラグの変化で拾う
    private void OnProgressChanged(string _) => MarkAutosave();

    private void OnUpgradeGranted(ScissorUpgrade _) => MarkAutosave();

    private void OnSkillGranted(PlayerSkill _) => MarkAutosave();

    private void MarkAutosave()
    {
        if (_autosaveDirty)
            return;

        _autosaveDirty = true;
        _autosaveDirtyAt = Time.unscaledTime;
    }

    private void Update()
    {
        if (_autosaveDirty && Time.unscaledTime - _autosaveDirtyAt >= AutosaveDelay)
            FlushAutosave();
    }

    /// <summary>
    /// 待っている変化があれば、今の進行・持ち物をこのプレイのセーブへ書き込む (Hollow Knight の自動セーブと同じ考え方)。
    /// 復帰位置・ステージ・場所/章の表示・セーブ日時・プレイ時間はセーブした拠点のまま。HP と壊した木箱は書かない。
    /// </summary>
    public void FlushAutosave()
    {
        if (!_autosaveDirty)
            return;

        _autosaveDirty = false;
        if (!LinkedToRunSave)
            return;

        var save = SaveSystem.TryLoad();
        if (save == null)
            return;

        WriteProgress(save);
        SaveSystem.Save(save);
    }

    /// <summary>
    /// 死亡リトライ (PlayerScene ごと読み直す) で引き継ぐデータ。状態は戻さない: 今の能力・鍛冶強化・キーアイテム・アイテム・
    /// 装備・糸・コイン・進行フラグ (ボス撃破など) をそのまま持って、最後に休んだ拠点 (別の部屋でもそのシーン) から再開する。
    /// HP・敵・木箱は読み直しで元に戻る。まだどの拠点でも休んでいなければ、今のステージの入り口から再開する。
    /// </summary>
    public SaveData CollectForRetry(string stageName)
    {
        FlushAutosave();

        var data = Collect();
        data.continuesRun = true;
        if (GameSession.HasRespawnBench)
        {
            data.sceneName = GameSession.RespawnScene;
            data.posX = GameSession.RespawnPosition.x;
            data.posY = GameSession.RespawnPosition.y;
        }
        else
        {
            data.sceneName = stageName;
            data.useStageSpawn = true;
        }

        return data;
    }

    private void Start()
    {
        Subscribe();

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

        if (data.sceneName != CurrentStageName())
            return false;

        GameSession.PendingLoad = null;

        if (!data.useStageSpawn)
            transform.position = new Vector3(data.posX, data.posY, transform.position.z);
        _progression?.LoadFrom(data);
        _inventory?.LoadFrom(data);
        _attackLoadout?.LoadFrom(data);
        GameProgress.LoadFrom(data);

        // 適用で起きた変化はセーブと同じ内容なので書き込まない。このプレイの続きなら、以後の変化はセーブへ書き込む
        _autosaveDirty = false;
        if (data.continuesRun)
            _runLinked = true;
        return !data.useStageSpawn;
    }

    /// <summary>拠点でセーブする時のデータ (復帰位置 = 拠点の位置)。</summary>
    public SaveData CollectAt(SavePoint savePoint)
    {
        _runLinked = true; // このプレイヤーの状態がそのままセーブになる → 以後の変化も書き込んでよい
        var data = Collect();
        if (savePoint != null)
        {
            var pos = BenchPosition(savePoint);
            data.posX = pos.x;
            data.posY = pos.y;
        }

        return data;
    }

    /// <summary>
    /// 拠点で休んだ時の自動セーブ (PendingChanges 47): このプレイのセーブを拠点の位置で丸ごと更新する
    /// (ポーズ・拠点メニューの「セーブ」と同じ内容 = 位置・シーン・場所/章の表示・日時・プレイ時間も)。
    /// はじめから (まだセーブしていない) なら、タイトルで選んだスロットへ書いてこのプレイのセーブにする
    /// (データのあるスロットはタイトルのスロット画面で上書きを確認済み)。
    /// 書かないのは: スロットを選んでいない (エディタの直接再生)・セーブがある状態で生まれて続きのデータを適用していない
    /// (ゲームクリア後の「もう一度」・テストプレイの記録の再生) 時。書いたら true。
    /// </summary>
    public bool SaveOnRest(SavePoint savePoint)
    {
        if (GameSession.SaveBelongsToRun ? !LinkedToRunSave : !GameSession.RunSlotChosen)
            return false;

        _autosaveDirty = false;
        SaveSystem.Save(SaveSystem.ActiveSlot, CollectAt(savePoint));
        GameSession.SaveBelongsToRun = true;
        return true;
    }

    /// <summary>拠点での復帰位置 (拠点の少し上)。</summary>
    private static Vector2 BenchPosition(SavePoint savePoint) =>
        (Vector2)savePoint.transform.position + new Vector2(0f, 0.5f);

    /// <summary>拠点で休んだ (セーブした) ことを覚える = 死亡リトライの復帰先にする (PendingChanges 39)。</summary>
    public static void RememberBench(SavePoint savePoint)
    {
        if (savePoint == null)
            return;

        GameSession.SetRespawnBench(CurrentStageName(), BenchPosition(savePoint));
    }

    private static string CurrentStageName() =>
        StageLoader.Instance != null ? StageLoader.Instance.CurrentStageName : SceneManager.GetActiveScene().name;

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

        return CurrentStageName();
    }

    /// <summary>現在の状態からセーブデータを組み立てる。復帰位置は呼び出し側 (拠点) が上書きする。</summary>
    public SaveData Collect()
    {
        var data = new SaveData
        {
            sceneName = CurrentStageName(),
            posX = transform.position.x,
            posY = transform.position.y,
            savedAt = System.DateTime.Now.ToString("o"),
            playTimeSeconds = GameSession.PlayTimeSeconds,
            chapterName = StageInfo.Current != null ? StageInfo.Current.ChapterName : "",
            placeName = CurrentPlaceName(),
        };

        WriteProgress(data);
        return data;
    }

    /// <summary>進行・持ち物 (位置・場所・日時・プレイ時間以外の全部) をセーブデータへ書く。</summary>
    private void WriteProgress(SaveData data)
    {
        data.upgrades = _progression != null ? _progression.CollectUpgrades() : new int[0];
        data.skills = _progression != null ? _progression.CollectSkills() : new int[0];
        data.forgeLevel = _progression != null ? _progression.ForgeLevel : 0;
        data.thread = _inventory != null ? _inventory.Thread.CurrentValue : 0;
        data.coins = _inventory != null ? _inventory.Coins.CurrentValue : 0;
        data.progressFlags = GameProgress.Collect();

        if (_inventory != null)
        {
            _inventory.CollectCounts(out var ids, out var counts);
            data.itemIds = ids;
            data.itemCounts = counts;
            data.slotItemIds = _inventory.CollectSlots();
        }

        _attackLoadout?.CollectTo(data);
    }
}
