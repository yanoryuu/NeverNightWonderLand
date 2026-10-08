using DG.Tweening;
using UnityEngine;

/// <summary>
/// 開き戸の見た目 (Door / FlagDoor 共用)。閉 → 開 の順のコマを SpriteRenderer に流す。
/// 絵が設定されている扉は上へスライドせず、その場で開いて開いた扉の絵を残す。
/// </summary>
public static class DoorSwing
{
    /// <summary>開き戸の絵が設定されているか。</summary>
    public static bool Has(SpriteRenderer renderer, Sprite[] frames) =>
        renderer != null && frames != null && frames.Length > 0;

    /// <summary>閉じた絵 (最初のコマ) にする。</summary>
    public static void ShowClosed(SpriteRenderer renderer, Sprite[] frames)
    {
        if (Has(renderer, frames) && frames[0] != null)
            renderer.sprite = frames[0];
    }

    /// <summary>開いた絵 (最後のコマ) にする (シーンロード時の復元用)。</summary>
    public static void ShowOpen(SpriteRenderer renderer, Sprite[] frames)
    {
        if (Has(renderer, frames) && frames[frames.Length - 1] != null)
            renderer.sprite = frames[frames.Length - 1];
    }

    /// <summary>コマ送りで開く。duration 秒で最後のコマに着く。</summary>
    public static Tween Play(SpriteRenderer renderer, Sprite[] frames, float duration)
    {
        var last = frames.Length - 1;
        return DOVirtual.Float(0f, last + 0.999f, Mathf.Max(0.01f, duration), v =>
            {
                var i = Mathf.Clamp(Mathf.FloorToInt(v), 0, last);
                if (frames[i] != null)
                    renderer.sprite = frames[i];
            })
            .SetEase(Ease.Linear)
            .SetLink(renderer.gameObject);
    }
}
