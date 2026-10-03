using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アイテム画面の「収集品」「資料」に並べる物の一覧 (データだけ)。
/// どちらも拾った時に立つ進行フラグ (KeyItemPickup の _flagId → GameProgress) で入手済みかを判定する。
/// - 収集品: ハートピン・糸巻き・秘伝書など KeyItem で拾う物 (日誌は含めない)。並び順 = 枠の順 (18 枠 × 3 ページ)
/// - 資料: 日誌。並び順 = 一覧の順
/// 新しい物を増やす時はこのアセットに 1 行足すだけでよい (ItemMenuView は表示するだけ)。
/// </summary>
[CreateAssetMenu(fileName = "CollectionCatalog", menuName = "NeverNight/Collection Catalog")]
public class CollectionCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("入手で立つ進行フラグ (KeyItemPickup の _flagId と同じ)")]
        public string Flag;

        [Tooltip("表示名")]
        public string Name;

        [Tooltip("説明 (収集品) / 本文 (資料)")]
        [TextArea(3, 12)]
        public string Description;

        [Tooltip("絵 (任意。無ければ名前の文字だけ出す)")]
        public Sprite Icon;

        public bool IsObtained => GameProgress.Has(Flag);
    }

    [Tooltip("収集品 (18 枠 × 3 ページに先頭から並ぶ)")]
    [SerializeField] private List<Entry> _collectibles = new();

    [Tooltip("資料 (日誌。一覧に先頭から並ぶ)")]
    [SerializeField] private List<Entry> _journals = new();

    public IReadOnlyList<Entry> Collectibles => _collectibles;

    public IReadOnlyList<Entry> Journals => _journals;

#if UNITY_EDITOR
    /// <summary>エディタのレシピ用: 一覧を置き換える。</summary>
    public void SetEntries(IEnumerable<Entry> collectibles, IEnumerable<Entry> journals)
    {
        _collectibles = new List<Entry>(collectibles);
        _journals = new List<Entry>(journals);
    }
#endif
}
