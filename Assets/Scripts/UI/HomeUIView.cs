using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ホーム画面 (拠点で開く)。SavePoint のインタラクトから開かれ、開いた時点で HP は自動で全回復する。
/// アイテム補充 (糸を消費して再生成) / 攻撃方法の入れ替え / セーブ (確認あり) を行う。
/// HomeUI シーン (Additive) に置かれ、UI はプレハブ上で事前配置した MenuPanelView を参照する。
/// </summary>
public class HomeUIView : MonoBehaviour
{
    private enum Page { Main, Attacks, SaveConfirm }

    [Tooltip("メニューに使う日本語フォント")]
    [SerializeField] private TMP_FontAsset _font;

    [Tooltip("メニューパネル (プレハブ上で事前配置)")]
    [SerializeField] private MenuPanelView _menu;

    private Page _page;

    // 開いた時のコンテキスト
    private PlayerController _player;
    private PlayerSaveBridge _saveBridge;
    private SavePoint _savePoint;

    private void Awake()
    {
        if (_menu == null)
        {
            Debug.LogError($"[{nameof(HomeUIView)}] MenuPanelView が設定されていません。", this);
            return;
        }

        _menu.Initialize(_font);
        _menu.OnCancelled += OnCancel;
    }

    public void Open(GameObject playerGo, SavePoint savePoint)
    {
        if (_menu == null || _menu.IsOpen)
            return;

        _player = playerGo.GetComponent<PlayerController>();
        _saveBridge = playerGo.GetComponent<PlayerSaveBridge>();
        _savePoint = savePoint;

        // 拠点に入ったら自動で全回復する
        HealFull();

        GamePause.Push();
        ShowMain();
        _menu.Open();
    }

    private void CloseMenu()
    {
        _menu.Close();
        GamePause.Pop();
    }

    private void OnCancel()
    {
        if (_page == Page.Main)
            CloseMenu();
        else
            ShowMain();
    }

    #region Pages

    private void ShowMain()
    {
        _page = Page.Main;
        _menu.SetTitle("拠点");

        var inventory = _player != null ? _player.Inventory : null;
        var refillCost = _player != null ? _player.Consts.RefillThreadCost : 0;
        var thread = inventory != null ? inventory.Thread.CurrentValue : 0;
        _menu.SetBody($"糸: {thread}");

        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new($"アイテムを補充する (糸 {refillCost})", Refill, inventory != null),
            new("攻撃方法を入れ替える", ShowAttacks, _player != null && _player.AttackLoadout != null),
            new("セーブ", ShowSaveConfirm, _saveBridge != null),
            new("閉じる", CloseMenu),
        });
    }

    /// <summary>HP を全回復する (拠点画面を開いた時に自動で呼ばれる)。満タンなら何もしない。</summary>
    private void HealFull()
    {
        if (_player == null || _player.Health == null)
            return;

        if (_player.Health.Hp.CurrentValue >= _player.Health.MaxHp)
            return;

        _player.Health.Heal(_player.Consts.MaxHp);
        Notifier.Notify("体力が全回復した");
    }

    private void Refill()
    {
        _player?.Inventory?.TryRefillAll();
        ShowMain(); // 糸の表示を更新
    }

    /// <summary>
    /// 攻撃方法の入れ替えページ。解放済みの攻撃方法を一覧し、選択で装備する。
    /// □=近接 / △=特殊 は定義の型で自動的に対応する枠へ入る。
    /// </summary>
    private void ShowAttacks()
    {
        _page = Page.Attacks;
        _menu.SetTitle("攻撃方法の入れ替え");

        var loadout = _player.AttackLoadout;
        var melee = loadout.CurrentMelee;
        var special = loadout.CurrentSpecial;
        _menu.SetBody($"装備中  □ {(melee != null ? melee.DisplayName : "---")} / △ {(special != null ? special.DisplayName : "---")}");

        var entries = new List<MenuPanelView.Entry>();
        foreach (var attack in loadout.Catalog)
        {
            if (attack == null || !loadout.IsUnlocked(attack))
                continue;

            var glyph = attack is MeleeAttackDefinition ? "□" : "△";
            var equipped = attack == (AttackDefinition)melee || attack == (AttackDefinition)special;
            var label = $"{glyph} {attack.DisplayName}{(equipped ? " (装備中)" : "")}";

            var captured = attack;
            entries.Add(new MenuPanelView.Entry(label, () =>
            {
                loadout.TryEquip(captured);
                ShowAttacks(); // 装備中マークを更新
            }, !equipped));
        }

        entries.Add(new MenuPanelView.Entry("戻る", ShowMain));
        _menu.SetEntries(entries);
    }

    private void ShowSaveConfirm()
    {
        _page = Page.SaveConfirm;
        _menu.SetTitle("セーブ");
        _menu.SetBody("ここまでの進行をセーブしますか?");
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("はい", DoSave),
            new("いいえ", ShowMain),
        });
    }

    private void DoSave()
    {
        var data = _saveBridge.Collect();

        // 復帰位置は拠点の位置にする
        if (_savePoint != null)
        {
            data.posX = _savePoint.transform.position.x;
            data.posY = _savePoint.transform.position.y + 0.5f;
        }

        SaveSystem.Save(data);
        Notifier.Notify("セーブしました");
        ShowMain();
    }

    #endregion
}
