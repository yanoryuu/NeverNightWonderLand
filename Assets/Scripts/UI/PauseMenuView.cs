using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer;

/// <summary>
/// ポーズメニュー。Esc / Menu ボタンで開閉し、ゲームを一時停止する。
/// 再開 / アイテム切替 / ハサミ強化状況 / 操作方法 / タイトルへ。
/// メニュー UI を Additive シーンに分離できるよう、プレイヤーは実行時に解決する
/// (PlayerRuntime 注入 → シリアライズ参照 → シーン検索 の順)。
/// </summary>
public class PauseMenuView : MonoBehaviour
{
    private enum Page { Main, Items, SlotSelect, ItemSelect, Upgrades, Controls }

    [Tooltip("メニューに使う日本語フォント (無ければ TMP デフォルト)")]
    [SerializeField] private TMP_FontAsset _font;

    [Tooltip("参照するプレイヤー (任意。未設定なら実行時に解決する)")]
    [SerializeField] private PlayerController _player;

    [Tooltip("タイトルシーン名")]
    [SerializeField] private string _titleSceneName = "TitleScene";

    [Tooltip("メニューパネル (プレハブ上で事前配置)")]
    [SerializeField] private MenuPanelView _menu;

    private Page _page;
    private ItemSlot _editingSlot; // アイテムセットで編集中のスロット

    private PlayerRuntime _playerRuntime;

    [Inject]
    public void Construct(PlayerRuntime playerRuntime)
    {
        _playerRuntime = playerRuntime;
    }

    /// <summary>現在のプレイヤー。Additive シーン運用でもシーンをまたいで解決できる。</summary>
    private PlayerController Player
    {
        get
        {
            if (_playerRuntime != null && _playerRuntime.Current.CurrentValue != null)
                return _playerRuntime.Current.CurrentValue;

            if (_player == null)
                _player = FindAnyObjectByType<PlayerController>();
            return _player;
        }
    }

    private void Awake()
    {
        if (_menu == null)
        {
            Debug.LogError($"[{nameof(PauseMenuView)}] MenuPanelView が設定されていません。", this);
            enabled = false;
            return;
        }

        _menu.Initialize(_font);
        _menu.OnCancelled += OnCancel;
    }

    private void Update()
    {
        if (_menu.IsOpen)
            return;

        // 他のメニュー (拠点・リザルト等) が開いている間は反応しない
        if (MenuPanelView.AnyOpen)
            return;

        var player = Player;
        if (player != null && player.IsDead)
            return;

        var pressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                      || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        if (pressed)
            OpenMenu();
    }

    private void OpenMenu()
    {
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
        switch (_page)
        {
            case Page.Main:
                CloseMenu();
                break;
            case Page.ItemSelect:
                ShowSlotSelect();
                break;
            case Page.SlotSelect:
                ShowItems();
                break;
            default:
                ShowMain();
                break;
        }
    }

    #region Pages

    private void ShowMain()
    {
        _page = Page.Main;
        _menu.SetTitle("ポーズ");
        _menu.SetBody("");
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("再開", CloseMenu),
            new("アイテム", ShowItems),
            new("ハサミ強化状況", ShowUpgrades),
            new("操作方法", ShowControls),
            new("タイトルへ", GoTitle),
        });
    }

    private void ShowItems()
    {
        _page = Page.Items;
        _menu.SetTitle("アイテム");

        var player = Player;
        var inventory = player != null ? player.Inventory : null;
        var body = "";
        if (inventory != null)
        {
            foreach (var item in inventory.Catalog)
            {
                if (item != null)
                    body += $"{item.DisplayName} x{inventory.GetCount(item)} — {item.Description}\n";
            }
        }

        _menu.SetBody(body);
        _menu.SetEntries(new List<MenuPanelView.Entry>
        {
            new("スロットにセットする", ShowSlotSelect, inventory != null),
            new("戻る", ShowMain),
        });
    }

    private void ShowSlotSelect()
    {
        _page = Page.SlotSelect;
        _menu.SetTitle("アイテムセット");
        _menu.SetBody("セットするスロットを選ぶ (アイテムボタンを押しながら 下/左/右 で使用)");

        var inventory = Player.Inventory;
        var entries = new List<MenuPanelView.Entry>();
        for (var i = 0; i < ItemSlotExtensions.SlotCount; i++)
        {
            var slot = (ItemSlot)i;
            var item = inventory.GetSlotItem(slot);
            var current = item == null ? "---" : $"{item.DisplayName} x{inventory.GetCount(item)}";
            entries.Add(new MenuPanelView.Entry(
                $"{slot.Glyph()} {slot.DisplayName()}: {current}",
                () =>
                {
                    _editingSlot = slot;
                    ShowItemSelect();
                }));
        }

        entries.Add(new MenuPanelView.Entry("戻る", ShowItems));
        _menu.SetEntries(entries);
    }

    private void ShowItemSelect()
    {
        _page = Page.ItemSelect;
        _menu.SetTitle($"{_editingSlot.Glyph()} {_editingSlot.DisplayName()} にセット");
        _menu.SetBody("");

        var inventory = Player.Inventory;
        var entries = new List<MenuPanelView.Entry>();
        foreach (var item in inventory.Catalog)
        {
            if (item == null)
                continue;

            var captured = item;
            entries.Add(new MenuPanelView.Entry(
                $"{item.DisplayName}  x{inventory.GetCount(item)}",
                () =>
                {
                    inventory.SetSlot(_editingSlot, captured);
                    ShowSlotSelect();
                }));
        }

        entries.Add(new MenuPanelView.Entry("はずす", () =>
        {
            inventory.SetSlot(_editingSlot, null);
            ShowSlotSelect();
        }));
        entries.Add(new MenuPanelView.Entry("戻る", ShowSlotSelect));
        _menu.SetEntries(entries);
    }

    private void ShowUpgrades()
    {
        _page = Page.Upgrades;
        _menu.SetTitle("ハサミ強化状況");

        var player = Player;
        var progression = player != null ? player.Progression : null;
        string Line(ScissorUpgrade u, string effect) =>
            $"{(progression != null && progression.Has(u) ? "○" : "×")} {u.DisplayName()} — {effect}";

        _menu.SetBody(
            Line(ScissorUpgrade.Yellow, "壁に張り付き、壁ジャンプできる") + "\n" +
            Line(ScissorUpgrade.Blue, "糸でハサミを飛ばして移動 [F]") + "\n" +
            Line(ScissorUpgrade.Red, "二段ジャンプ・滑空"));
        _menu.SetEntries(new List<MenuPanelView.Entry> { new("戻る", ShowMain) });
    }

    private void ShowControls()
    {
        _page = Page.Controls;
        _menu.SetTitle("操作方法");
        _menu.SetBody(
            "移動: A/D  ジャンプ: Space  ダッシュ: Shift (敵をすり抜ける)\n" +
            "近接攻撃: J  特殊攻撃: K  裁断: L\n" +
            "回復: S  糸移動: F  しらべる: E  ポーズ: Esc\n" +
            "アイテム: I を押しながら 下/左/右\n" +
            "パッド: ジャンプ× 近接□ 特殊△ 回復○ ダッシュR2\n" +
            "裁断R1 アイテム L1押しながら十字 糸移動L2 しらべる↑ ポーズMenu");
        _menu.SetEntries(new List<MenuPanelView.Entry> { new("戻る", ShowMain) });
    }

    private void GoTitle()
    {
        GamePause.Reset();
        SceneManager.LoadScene(_titleSceneName);
    }

    #endregion
}
