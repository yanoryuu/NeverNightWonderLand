using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アイテム画面「装備品」タブ (案A, 完成形 item/soubi/装備_soubi.png)。
/// - 左の大枠: 選んでいる物の絵 (素材が無い間は非表示) と説明 / 左下の帯: その名前
/// - 中央の縦長の枠: ハサミ強化 (黄・青・赤) の所持状況 (旧ポーズの「ハサミ強化状況」)
/// - 右上の帯: 糸の所持数
/// - 右の小枠 4 つ: □ 攻撃 / △ 攻撃 / アイテム下スロット / アイテム左右スロット
/// 選択: 上下左右で中央の 3 行と右の 4 枠を移動。
/// アイテムスロット (↓ / ← →) の枠で決定 → 中央の縦長の枠がアイテムの選び直し (所持アイテム + はずす) に替わる
/// (旧ポーズの「スロットにセットする」の流用。UIAssignment.md「3回目の決定」4)。左右スロットの枠では ← → で対象のスロットを切り替える。
/// 選び直し中は左の大枠・帯にカーソルの物の説明・名前を出す。決定でセット、戻るで取り消し。
/// 一覧の行 = 板 (仮素材 picker_row / _selected / _disabled) + アイコン + 「名前 x所持数」。所持数 0 の物も出し、
/// 灰色の板・灰色のアイコン・薄い文字にする。別スロットに入っている物を選ぶと 2 つのスロットの中身を入れ替える
/// (入れ替えは PlayerItemInventoryModel.SetSlot。UIAssignment.md「4回目の決定」2・3・7)。
/// </summary>
public class ItemEquipTab : ItemMenuTab
{
    // 選択位置: 0..2 = ハサミ強化 (黄・青・赤), 3 = □, 4 = △, 5 = 下スロット, 6 = 左右スロット
    private const int UpgradeCount = 3;
    private const int SlotMelee = 3, SlotSpecial = 4, SlotDown = 5, SlotSides = 6;

    private static readonly ScissorUpgrade[] Upgrades = { ScissorUpgrade.Yellow, ScissorUpgrade.Blue, ScissorUpgrade.Red };

    // 旧ポーズ「ハサミ強化状況」の文言
    private static readonly string[] UpgradeEffects =
    {
        "壁に張り付き、壁ジャンプできる",
        "糸でハサミを飛ばして移動 [F]",
        "二段ジャンプ・滑空",
    };

    [Header("左: 説明")]
    [Tooltip("選んでいる物の絵 (左の大枠の上部。素材が無い間は非表示)")]
    [SerializeField] private Image _detailIcon;

    [Tooltip("説明の文字 (左の大枠)")]
    [SerializeField] private TMP_Text _detailText;

    [Tooltip("絵がある時の説明の位置・大きさ (左の大枠内)")]
    [SerializeField] private Vector2 _textPosWithIcon = new(0f, -150f);
    [SerializeField] private Vector2 _textSizeWithIcon = new(560f, 160f);

    [Tooltip("絵が無い時の説明の位置・大きさ (枠いっぱい)")]
    [SerializeField] private Vector2 _textPosNoIcon = new(0f, 0f);
    [SerializeField] private Vector2 _textSizeNoIcon = new(560f, 420f);

    [Tooltip("名前の文字 (左下の帯)")]
    [SerializeField] private TMP_Text _nameText;

    [Header("右上: 糸")]
    [SerializeField] private TMP_Text _threadText;

    [Header("中央: ハサミ強化 (黄・青・赤)")]
    [SerializeField] private Cell[] _upgradeRows = new Cell[UpgradeCount];

    [Tooltip("持っていない強化の文字のアルファ (無効の見た目と同じ 0.45)")]
    [Range(0f, 1f)]
    [SerializeField] private float _notOwnedAlpha = 0.45f;

    [Header("右: 小枠 (□ / △ / 下スロット / 左右スロット)")]
    [SerializeField] private Cell[] _slotCells = new Cell[4];

    [Header("中央: アイテムの選び直し (スロットの枠で決定した時だけ表示)")]
    [Tooltip("ハサミ強化の見出し・3 行をまとめたもの (選び直しの間は隠す)")]
    [SerializeField] private GameObject _upgradeGroup;

    [Tooltip("選び直しの見出し・行をまとめたもの")]
    [SerializeField] private GameObject _pickerGroup;

    [Tooltip("選び直しの見出し (対象のスロット)")]
    [SerializeField] private TMP_Text _pickerHeading;

    [Tooltip("選び直しの行 (上から。入りきらない時は送る)。Root = 行の板 (Image)、Icon = アイテムのアイコン、Label = 名前 x所持数")]
    [SerializeField] private Cell[] _pickerRows = new Cell[7];

    [Tooltip("行の板 (通常 / 選択中 / 所持数 0)。仮素材 Assets/LocalData/UI/_temp/picker_row*.png")]
    [SerializeField] private Sprite _pickerRowSprite;
    [SerializeField] private Sprite _pickerRowSelectedSprite;
    [SerializeField] private Sprite _pickerRowEmptySprite;

    [Header("文言")]
    [SerializeField] private string _noneLabel = "なし";
    [SerializeField] private string _ownedLabel = "入手済み";
    [SerializeField] private string _notOwnedLabel = "未入手";
    [SerializeField] private string _unequipLabel = "はずす";
    [SerializeField] private string _unequipDescription = "スロットを空にする";

    private int _index = SlotMelee;
    private Color[] _rowColors;

    // アイテムの選び直し
    private bool _picking;
    private ItemSlot _pickSlot;
    private readonly List<ItemDefinition> _pickItems = new(); // null = はずす
    private int _pickIndex;
    private int _pickScroll;
    private Color[] _pickerRowColors;

    protected override void OnShow()
    {
        if (_rowColors == null)
        {
            _rowColors = new Color[_upgradeRows.Length];
            for (var i = 0; i < _upgradeRows.Length; i++)
                _rowColors[i] = _upgradeRows[i]?.Label != null ? _upgradeRows[i].Label.color : Color.white;
        }

        ClosePicker();
        Refresh();
    }

    protected override void OnHide()
    {
        ClosePicker();
    }

    public override bool HandleInput(MenuInput.Frame input)
    {
        if (_picking)
            return HandlePickerInput(input);

        if (input.Submit && (_index == SlotDown || _index == SlotSides))
        {
            OpenPicker(_index == SlotDown ? ItemSlot.Down : ItemSlot.Left);
            return false;
        }

        var next = Navigate(_index, input.Move, input.MoveX);
        if (next != _index)
        {
            _index = next;
            RenderSelection();
            RenderDetail();
        }

        return false; // 戻るは画面を閉じる
    }

    /// <summary>上下左右の移動先。中央の 3 行 | 右の 2×2 枠。</summary>
    private static int Navigate(int index, int moveY, int moveX)
    {
        if (index < UpgradeCount)
        {
            if (moveY != 0)
                return Mathf.Clamp(index + moveY, 0, UpgradeCount - 1);
            if (moveX > 0)
                return index < 2 ? SlotMelee : SlotDown; // 上 2 行 → 上の枠、下の行 → 下の枠
            return index;
        }

        var col = (index - SlotMelee) % 2; // 0 = 左列, 1 = 右列
        var row = (index - SlotMelee) / 2; // 0 = 上段, 1 = 下段
        if (moveY != 0)
            row = Mathf.Clamp(row + moveY, 0, 1);
        if (moveX < 0)
        {
            if (col == 0)
                return row == 0 ? 0 : UpgradeCount - 1;
            col = 0;
        }
        else if (moveX > 0)
        {
            col = 1;
        }

        return SlotMelee + row * 2 + col;
    }

    #region Picker (旧 PauseMenuView の ShowItemSelect の流用)

    private PlayerItemInventory Inventory => Player != null ? Player.Inventory : null;

    private void OpenPicker(ItemSlot slot)
    {
        var inventory = Inventory;
        if (inventory == null)
            return;

        _picking = true;
        _pickSlot = slot;
        _pickScroll = 0;
        BuildPickItems(inventory);
        // 今セットされている物にカーソルを合わせる (空きスロットなら先頭)
        var current = inventory.GetSlotItem(slot);
        _pickIndex = current != null ? Mathf.Max(0, _pickItems.IndexOf(current)) : 0;
        if (_upgradeGroup != null) _upgradeGroup.SetActive(false);
        if (_pickerGroup != null) _pickerGroup.SetActive(true);
        RenderPicker();
    }

    private void ClosePicker()
    {
        _picking = false;
        if (_upgradeGroup != null) _upgradeGroup.SetActive(true);
        if (_pickerGroup != null) _pickerGroup.SetActive(false);
    }

    /// <summary>全アイテム (所持数 0 の物も灰色で出す。UIAssignment.md「4回目の決定」3) + はずす。</summary>
    private void BuildPickItems(PlayerItemInventory inventory)
    {
        _pickItems.Clear();
        foreach (var item in inventory.Catalog)
        {
            if (item != null)
                _pickItems.Add(item);
        }

        _pickItems.Add(null); // はずす
    }

    private bool HandlePickerInput(MenuInput.Frame input)
    {
        if (input.Cancel)
        {
            ClosePicker();
            Refresh();
            return true;
        }

        var inventory = Inventory;
        if (inventory == null)
        {
            ClosePicker();
            return false;
        }

        // 左右スロットの枠: ← → で対象のスロットを切り替える
        if (input.MoveX != 0 && _pickSlot != ItemSlot.Down)
        {
            var slot = input.MoveX < 0 ? ItemSlot.Left : ItemSlot.Right;
            if (slot != _pickSlot)
            {
                var keep = _pickIndex < _pickItems.Count ? _pickItems[_pickIndex] : null;
                _pickSlot = slot;
                BuildPickItems(inventory);
                _pickIndex = Mathf.Max(0, _pickItems.IndexOf(keep));
                RenderPicker();
            }
        }

        if (input.Move != 0)
        {
            var next = Mathf.Clamp(_pickIndex + input.Move, 0, _pickItems.Count - 1);
            if (next != _pickIndex)
            {
                _pickIndex = next;
                RenderPicker();
            }
        }

        if (input.Submit)
        {
            // null = はずす。別スロットに入っている物なら Model が 2 つのスロットの中身を入れ替える
            inventory.SetSlot(_pickSlot, _pickItems[_pickIndex]);
            ClosePicker();
            Refresh();
        }

        return false;
    }

    private void RenderPicker()
    {
        var inventory = Inventory;
        SetText(_pickerHeading, $"{_pickSlot.Glyph()} {_pickSlot.DisplayName()}");

        // 見えている行に収まるように送る
        var visible = Mathf.Max(1, _pickerRows.Length);
        if (_pickIndex < _pickScroll) _pickScroll = _pickIndex;
        if (_pickIndex > _pickScroll + visible - 1) _pickScroll = _pickIndex - visible + 1;
        _pickScroll = Mathf.Clamp(_pickScroll, 0, Mathf.Max(0, _pickItems.Count - visible));

        if (_pickerRowColors == null)
        {
            _pickerRowColors = new Color[_pickerRows.Length];
            for (var i = 0; i < _pickerRows.Length; i++)
                _pickerRowColors[i] = _pickerRows[i]?.Label != null ? _pickerRows[i].Label.color : Color.white;
        }

        var current = inventory != null ? inventory.GetSlotItem(_pickSlot) : null;
        for (var i = 0; i < _pickerRows.Length; i++)
        {
            var row = _pickerRows[i];
            if (row?.Root == null)
                continue;

            var n = _pickScroll + i;
            var has = n < _pickItems.Count;
            row.Root.gameObject.SetActive(has);
            if (!has)
                continue;

            var item = _pickItems[n];
            var count = item != null && inventory != null ? inventory.GetCount(item) : 0;
            var empty = item != null && count <= 0; // 使い切った (0 個) = 灰色
            var selected = n == _pickIndex;
            var label = item == null ? _unequipLabel : $"{item.DisplayName} x{count}";
            if (item != null && item == current)
                label = "○ " + label; // 今セットされている物
            SetText(row.Label, label);
            if (row.Label != null)
            {
                var color = _pickerRowColors[i];
                if (empty)
                    color.a *= _notOwnedAlpha;
                row.Label.color = color;
            }

            SetIcon(row.Icon, item == null ? null : empty ? item.IconEmpty : item.Icon);
            var plate = row.Root.GetComponent<Image>();
            if (plate != null)
            {
                var sprite = selected ? _pickerRowSelectedSprite : empty ? _pickerRowEmptySprite : _pickerRowSprite;
                if (sprite != null)
                    plate.sprite = sprite;
            }

            SetSelected(row, selected);
        }

        // 左の大枠・帯: カーソルの物
        var cursor = _pickIndex < _pickItems.Count ? _pickItems[_pickIndex] : null;
        SetText(_nameText, cursor != null ? cursor.DisplayName : _unequipLabel);
        SetText(_detailText, cursor != null && inventory != null
            ? ItemDescription(inventory, _pickSlot, cursor)
            : $"{_pickSlot.Glyph()} {_unequipDescription}");
        SetIcon(_detailIcon, null);
    }

    #endregion

    #region Render

    private void Refresh()
    {
        var player = Player;
        var inventory = player != null ? player.Inventory : null;
        var progression = player != null ? player.Progression : null;
        var loadout = player != null ? player.AttackLoadout : null;

        SetText(_threadText, $"糸  {(inventory != null ? inventory.Thread.CurrentValue : 0)}");

        for (var i = 0; i < _upgradeRows.Length && i < Upgrades.Length; i++)
        {
            var row = _upgradeRows[i];
            if (row?.Label == null)
                continue;

            var owned = progression != null && progression.Has(Upgrades[i]);
            row.Label.text = $"{(owned ? "○" : "×")} {Upgrades[i].DisplayName()}";
            var color = _rowColors[i];
            if (!owned)
                color.a *= _notOwnedAlpha;
            row.Label.color = color;
        }

        SetSlotLabel(SlotMelee, loadout != null && loadout.CurrentMelee != null ? loadout.CurrentMelee.DisplayName : _noneLabel);
        SetSlotLabel(SlotSpecial, loadout != null && loadout.CurrentSpecial != null ? loadout.CurrentSpecial.DisplayName : _noneLabel);
        SetSlotLabel(SlotDown, ItemName(inventory, ItemSlot.Down));
        SetSlotLabel(SlotSides, $"{ItemName(inventory, ItemSlot.Left)}\n{ItemName(inventory, ItemSlot.Right)}");

        RenderSelection();
        RenderDetail();
    }

    private void SetSlotLabel(int index, string label)
    {
        var cell = CellAt(index);
        if (cell != null)
            SetText(cell.Label, label);
    }

    private Cell CellAt(int index) =>
        index >= SlotMelee && index - SlotMelee < _slotCells.Length ? _slotCells[index - SlotMelee] : null;

    private string ItemName(PlayerItemInventory inventory, ItemSlot slot)
    {
        var item = inventory != null ? inventory.GetSlotItem(slot) : null;
        return item != null ? item.DisplayName : _noneLabel;
    }

    private void RenderSelection()
    {
        for (var i = 0; i < _upgradeRows.Length; i++)
            SetSelected(_upgradeRows[i], i == _index);
        for (var i = 0; i < _slotCells.Length; i++)
            SetSelected(_slotCells[i], SlotMelee + i == _index);
    }

    private void RenderDetail()
    {
        var player = Player;
        var inventory = player != null ? player.Inventory : null;
        var progression = player != null ? player.Progression : null;
        var loadout = player != null ? player.AttackLoadout : null;

        string name, text;
        Sprite icon = null;
        switch (_index)
        {
            case < UpgradeCount:
            {
                var upgrade = Upgrades[_index];
                var owned = progression != null && progression.Has(upgrade);
                name = upgrade.DisplayName();
                text = $"{UpgradeEffects[_index]}\n\n{(owned ? _ownedLabel : _notOwnedLabel)}";
                break;
            }
            case SlotMelee:
            case SlotSpecial:
            {
                AttackDefinition attack = null;
                if (loadout != null)
                    attack = _index == SlotMelee ? loadout.CurrentMelee : loadout.CurrentSpecial;
                var glyph = _index == SlotMelee ? "□" : "△";
                name = attack != null ? attack.DisplayName : _noneLabel;
                text = attack != null ? $"{glyph} {attack.Description}" : "";
                break;
            }
            case SlotDown:
            {
                var item = inventory != null ? inventory.GetSlotItem(ItemSlot.Down) : null;
                name = item != null ? item.DisplayName : _noneLabel;
                text = item != null ? ItemDescription(inventory, ItemSlot.Down, item) : "";
                break;
            }
            default:
            {
                var left = inventory != null ? inventory.GetSlotItem(ItemSlot.Left) : null;
                var right = inventory != null ? inventory.GetSlotItem(ItemSlot.Right) : null;
                name = $"{ItemName(inventory, ItemSlot.Left)} / {ItemName(inventory, ItemSlot.Right)}";
                text = (left != null ? ItemDescription(inventory, ItemSlot.Left, left) : $"{ItemSlot.Left.Glyph()} {_noneLabel}")
                       + "\n\n"
                       + (right != null ? ItemDescription(inventory, ItemSlot.Right, right) : $"{ItemSlot.Right.Glyph()} {_noneLabel}");
                break;
            }
        }

        SetText(_nameText, name);
        SetText(_detailText, text);
        SetIcon(_detailIcon, icon);
        if (_detailText != null)
        {
            var rt = _detailText.rectTransform;
            rt.anchoredPosition = icon != null ? _textPosWithIcon : _textPosNoIcon;
            rt.sizeDelta = icon != null ? _textSizeWithIcon : _textSizeNoIcon;
        }
    }

    private static string ItemDescription(PlayerItemInventory inventory, ItemSlot slot, ItemDefinition item) =>
        $"{slot.Glyph()} {item.DisplayName}  {inventory.GetCount(item)} / {item.MaxCount}\n{item.Description}";

    #endregion
}
