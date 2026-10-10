using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーン名 → 場所の表示名・章の表示名の対応表 (セーブスロット画面用)。
/// 新しいセーブは記録時に場所・章を持つので、これは旧形式のセーブ (save.json から移したスロットなど、
/// 場所・章が空のデータ) を表示する時の補完にだけ使う。
/// Resources/StageNameCatalog.asset に置き、Docs/UI/Build/build_ui.cs (BuildUI.StageNames) が
/// Build Settings のシーンの部屋名 (StageInfo / 旧 AreaTitleView の _areaName) から作り直す。
/// </summary>
[CreateAssetMenu(fileName = "StageNameCatalog", menuName = "NeverNight/Stage Name Catalog")]
public class StageNameCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string SceneName;
        public string PlaceName;
        public string ChapterName;
    }

    [SerializeField] private List<Entry> _entries = new();

    private const string ResourcePath = "StageNameCatalog";
    private static StageNameCatalog _instance;
    private static bool _loaded;

    public IReadOnlyList<Entry> Entries => _entries;

    /// <summary>Resources から読む (無ければ null)。</summary>
    public static StageNameCatalog Instance
    {
        get
        {
            if (!_loaded)
            {
                _loaded = true;
                _instance = Resources.Load<StageNameCatalog>(ResourcePath);
            }

            return _instance;
        }
    }

    /// <summary>シーン名から対応を探す (無ければ null)。</summary>
    public static Entry Find(string sceneName)
    {
        var catalog = Instance;
        if (catalog == null || string.IsNullOrEmpty(sceneName))
            return null;

        foreach (var e in catalog._entries)
        {
            if (e != null && e.SceneName == sceneName)
                return e;
        }

        return null;
    }

#if UNITY_EDITOR
    /// <summary>エディタのレシピ用: 表を置き換える。</summary>
    public void SetEntries(IEnumerable<Entry> entries)
    {
        _entries = new List<Entry>(entries);
    }
#endif
}
