using UnityEngine;

/// <summary>
/// 鍛冶師 NPC。インタラクトで会話し、対応する色のハサミ強化を授ける。
/// 黄=カブトムシ、青=クモ、赤=トビムシ (企画書)。
/// 渡す条件: _requiredFlags が全て立ち (AND)、かつ _requiredAnyFlags が空でなければそのどれか 1 つが立っている (OR)。
/// 条件を満たす前は _lockedLines (あれば、NpcTalk と同じく 1 回のインタラクトで 1 行ずつ画面上部の案内枠に出す) か
/// _lockedMessage (トースト) を話す。強化を渡した後は有償の鍛冶強化 / 通常の会話。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Blacksmith : MonoBehaviour, IInteractable
{
    [Tooltip("授けるハサミ強化の色")]
    [SerializeField] private ScissorUpgrade _upgrade = ScissorUpgrade.Yellow;

    [Tooltip("false にするとハサミ強化を授けない (鍛冶強化と会話だけの鍛冶師)。既定 true = 従来どおり")]
    [SerializeField] private bool _grantsUpgrade = true;

    [Tooltip("鍛冶師の名前 (会話表示用)")]
    [SerializeField] private string _smithName = "カブトムシの鍛冶師";

    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("これらの進行フラグ (GameProgress) が全て立つまで強化を渡さない (AND)。空なら条件なし")]
    [SerializeField] private string[] _requiredFlags = new string[0];

    [Tooltip("これらの進行フラグのどれか 1 つが立つまで強化を渡さない (OR)。空なら条件なし。例: 中ボスどちらか")]
    [SerializeField] private string[] _requiredAnyFlags = new string[0];

    [Tooltip("解禁前の台詞 (トースト)。_lockedLines が空の時に使う")]
    [SerializeField] private string _lockedMessage = "「今は渡せるものがない」";

    [Tooltip("解禁前の台詞 (任意, 1要素 = 1回の表示)。設定すると話しかけるたびに1行ずつ案内枠に出す")]
    [SerializeField, TextArea] private string[] _lockedLines = new string[0];

    [Tooltip("強化を渡す時の台詞 (トースト)。続けて取得通知 (操作説明つき) が出る")]
    [SerializeField] private string _grantMessage = "「いいハサミだ。強化してやろう」";

    [Header("鍛冶強化 (能力とは別の有償強化)")]
    [Tooltip("この鍛冶師の鍛冶強化ID (1人1回まで)。空なら有償強化は行わない。例: CarouselHall")]
    [SerializeField] private string _forgeId = "";

    [Tooltip("鍛冶強化に必要な糸の数")]
    [SerializeField] private int _forgeCost = 30;

    [Tooltip("会話を閉じる距離 (_lockedLines の表示中)")]
    [SerializeField] private float _hideDistance = 3f;

    private TutorialMessageView _message;
    private string[] _talking; // 表示中のセリフ (null = 会話していない)
    private int _index;
    private Transform _listener;

    public string PromptText => _talking != null ? "次へ" : "話す";
    public bool CanInteract => true;
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    private string ForgeFlag => $"Forge_{_forgeId}";

    /// <summary>強化を渡す進行条件 (AND の _requiredFlags と OR の _requiredAnyFlags) を満たしているか。</summary>
    public bool IsUnlocked
    {
        get
        {
            if (_requiredFlags != null)
                foreach (var flag in _requiredFlags)
                    if (!string.IsNullOrEmpty(flag) && !GameProgress.Has(flag))
                        return false;

            if (_requiredAnyFlags == null)
                return true;
            var any = false;
            var hasCondition = false;
            foreach (var flag in _requiredAnyFlags)
            {
                if (string.IsNullOrEmpty(flag))
                    continue;
                hasCondition = true;
                if (GameProgress.Has(flag)) { any = true; break; }
            }
            return !hasCondition || any;
        }
    }

    public void Interact(GameObject interactor)
    {
        // 解禁前の案内 (複数行) を表示中なら次の行へ
        if (_talking != null)
        {
            _index++;
            if (_index >= _talking.Length) EndTalk();
            else Show(_talking[_index]);
            return;
        }

        var progression = interactor.GetComponent<PlayerProgression>();
        if (progression == null)
            return;

        // 鍛冶強化だけの鍛冶師: 能力の授与と進行条件を飛ばす
        if (!_grantsUpgrade)
        {
            if (TryForge(interactor, progression))
                return;
            Notifier.Notify($"{_smithName}「刃こぼれしたら、いつでも来な」");
            return;
        }

        // 進行条件を満たすまでは渡さない
        if (!progression.Has(_upgrade) && !IsUnlocked)
        {
            if (_lockedLines != null && _lockedLines.Length > 0)
                BeginTalk(interactor, _lockedLines);
            else
                Notifier.Notify($"{_smithName}{_lockedMessage}");
            return;
        }

        // 1回目の会話: 能力の強化 (ハサミの色)。取得通知 (操作説明つき) は Grant が出す
        if (!progression.Has(_upgrade))
        {
            Notifier.Notify($"{_smithName}{_grantMessage}");
            progression.Grant(_upgrade);
            return;
        }

        // 2回目以降: 鍛冶強化 (糸を払って攻撃力+コンボ数、1人1回)
        if (TryForge(interactor, progression))
            return;

        Notifier.Notify($"{_smithName}「{_upgrade.DisplayName()}の調子はどうだ?」");
    }

    private void Update()
    {
        if (_talking != null && _listener != null
            && Vector2.Distance(_listener.position, transform.position) > _hideDistance)
            EndTalk();
    }

    private void OnDisable()
    {
        if (_talking != null)
            EndTalk();
    }

    private void BeginTalk(GameObject interactor, string[] lines)
    {
        // 表示先は話しかけた時に探す (UI シーンはステージより後にロードされることがある)
        if (_message == null)
            _message = UIViewLookup.Find<TutorialMessageView>(this);
        if (_message == null)
        {
            foreach (var line in lines)
                Notifier.Notify(Format(line));
            return;
        }
        _talking = lines;
        _index = 0;
        _listener = interactor.transform;
        Show(lines[0]);
    }

    private void EndTalk()
    {
        _talking = null;
        _listener = null;
        if (_message != null)
            _message.Hide();
    }

    private void Show(string line)
    {
        if (_message != null)
            _message.Show(Format(line));
    }

    private string Format(string line) => $"{_smithName}「{line}」";

    /// <summary>
    /// 有償の鍛冶強化。糸 (_forgeCost) を消費して攻撃力と最大コンボ数を1段階伸ばす。
    /// この鍛冶師で強化済みなら false (通常会話へ)。
    /// </summary>
    private bool TryForge(GameObject interactor, PlayerProgression progression)
    {
        if (string.IsNullOrEmpty(_forgeId) || GameProgress.Has(ForgeFlag))
            return false;

        var inventory = interactor.GetComponent<PlayerItemInventory>();
        if (inventory == null)
            return false;

        if (!inventory.TrySpendThread(_forgeCost))
        {
            Notifier.Notify($"{_smithName}「刃を研ぎ直してやれるが、糸が{_forgeCost}いる」");
            return true;
        }

        GameProgress.Set(ForgeFlag);
        progression.AddForgeLevel();
        Notifier.Notify($"{_smithName}「研ぎ上げたぞ」攻撃力と最大コンボ数が上がった! (糸 -{_forgeCost})");
        return true;
    }
}
