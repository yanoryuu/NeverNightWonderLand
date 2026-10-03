using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アイテム画面のタブ 1 枚分 (装備品 / 収集品 / 資料) の基底。ItemMenuView が表示切替と入力の受け渡しをする。
/// </summary>
public abstract class ItemMenuTab : MonoBehaviour
{
    /// <summary>選べる枠 1 つ (選択中は拡大する)。</summary>
    [Serializable]
    public class Cell
    {
        [Tooltip("枠のルート (選択中は拡大される)")]
        public RectTransform Root;

        [Tooltip("絵 (任意。素材が無い間は非表示)")]
        public Image Icon;

        [Tooltip("名前の文字")]
        public TMP_Text Label;
    }

    [Tooltip("タブの中身のルート (表示切替される)")]
    [SerializeField] protected GameObject _root;

    [Tooltip("選択中の枠の拡大率 (選択用の絵が来るまでの仮の強調。セーブスロットと同じ 1.04)")]
    [SerializeField] protected float _selectedScale = 1.04f;

    protected ItemMenuView Owner { get; private set; }

    protected PlayerController Player => Owner != null ? Owner.Player : null;

    protected virtual void Awake()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    /// <summary>タブを表示して中身を最新にする。</summary>
    public void Show(ItemMenuView owner)
    {
        Owner = owner;
        if (_root != null)
            _root.SetActive(true);
        OnShow();
    }

    /// <summary>タブを隠す (詳細も閉じる)。</summary>
    public void Hide()
    {
        OnHide();
        if (_root != null)
            _root.SetActive(false);
    }

    protected abstract void OnShow();

    protected virtual void OnHide() { }

    /// <summary>
    /// 入力を処理する (タブ切替と開閉は ItemMenuView が先に処理済み)。
    /// 戻る (Cancel) をタブ内で使った (詳細を閉じた等) なら true。false なら ItemMenuView が画面を閉じる。
    /// </summary>
    public abstract bool HandleInput(MenuInput.Frame input);

    #region helpers

    protected void SetSelected(Cell cell, bool selected)
    {
        if (cell?.Root != null)
            cell.Root.localScale = Vector3.one * (selected ? _selectedScale : 1f);
    }

    protected static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? "";
    }

    /// <summary>絵を設定する (null なら非表示)。</summary>
    protected static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.gameObject.SetActive(sprite != null);
    }

    #endregion
}
