using UnityEngine;

/// <summary>
/// 進行フラグ (GameProgress) に合わせて GameObject の表示/非表示を切り替える。
/// フラグ成立時に _showWhenSet を有効・_hideWhenSet を無効にし、不成立時はその逆にする。
/// 同シーン内でフラグが立った場合もその場で切り替わる (GameProgress.Changed を購読)。
/// 用途: 大扉のランプ点灯 / 闘技場クリア後のご褒美ピックアップの出現 / 起動したリフトの見た目など。
/// _setFlagsWhenSatisfied で「成立したら別のフラグも立てる」(フラグの別名) こともできる。
/// 切り替える対象とは別の、常に有効な GameObject に付けること (自分自身を無効にすると購読が止まる)。
/// </summary>
public class FlagActivator : MonoBehaviour
{
    [Tooltip("判定する進行フラグ")]
    [SerializeField] private string[] _flags = new string[0];

    [Tooltip("true: 全てのフラグが立っていれば成立 / false: どれか1つでも立っていれば成立")]
    [SerializeField] private bool _requireAll = true;

    [Tooltip("成立時に有効にするオブジェクト (不成立時は無効)")]
    [SerializeField] private GameObject[] _showWhenSet = new GameObject[0];

    [Tooltip("成立時に無効にするオブジェクト (不成立時は有効)")]
    [SerializeField] private GameObject[] _hideWhenSet = new GameObject[0];

    [Tooltip("成立時に追加で立てる進行フラグ (任意)。1つの出来事で別名のフラグも立てたい時に使う。例: A1_Speaker2 成立で A1_ZiplineReady")]
    [SerializeField] private string[] _setFlagsWhenSatisfied = new string[0];

    /// <summary>現在フラグ条件が成立しているか。</summary>
    public bool IsSatisfied
    {
        get
        {
            if (_flags == null || _flags.Length == 0)
                return false;

            var any = false;
            foreach (var flag in _flags)
            {
                if (string.IsNullOrEmpty(flag))
                    continue;

                var has = GameProgress.Has(flag);
                if (_requireAll && !has)
                    return false;
                any |= has;
            }

            return any;
        }
    }

    private void OnEnable()
    {
        GameProgress.Changed += OnFlagChanged;
        Apply(); // シーンロード時の初期状態 + 無効だった間に立ったフラグの反映
    }

    private void OnDisable()
    {
        GameProgress.Changed -= OnFlagChanged;
    }

    private void OnFlagChanged(string flag)
    {
        if (System.Array.IndexOf(_flags, flag) >= 0)
            Apply();
    }

    private void Apply()
    {
        var satisfied = IsSatisfied;
        SetAll(_showWhenSet, satisfied);
        SetAll(_hideWhenSet, !satisfied);

        if (satisfied && _setFlagsWhenSatisfied != null)
        {
            foreach (var flag in _setFlagsWhenSatisfied)
                GameProgress.Set(flag); // 立て済み・空文字は GameProgress 側で無視される
        }
    }

    private static void SetAll(GameObject[] targets, bool active)
    {
        if (targets == null)
            return;

        foreach (var target in targets)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
