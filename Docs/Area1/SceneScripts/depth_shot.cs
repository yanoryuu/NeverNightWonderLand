// 奥行き (§16) の点検用: プレイ中にプレイヤーを (x,y) に置いて止め、カメラを即座に寄せる → capture_game_view で撮る。
// 例: --entry DepthShot.At --args '[205,-114]' → (0.5 秒ほど待つ) → capture_game_view --save_path Screenshots/Depth/after_D2.png
using UnityEngine;

public static class DepthShot
{
    /// <summary>プレイ中に別のステージへ (既定のスポーン)。ロード完了まで 3〜5 秒待ってから At で撮る位置へ。</summary>
    public static string Go(string scene)
    {
        Application.runInBackground = true;
        var p = Object.FindAnyObjectByType<PlayerController>();
        if (p != null) p.GetComponent<Rigidbody2D>().simulated = true;
        return StageLoader.Instance.TransitionTo(scene) ? "go " + scene : "transition refused";
    }

    public static string At(float x, float y)
    {
        Application.runInBackground = true;
        var p = Object.FindAnyObjectByType<PlayerController>();
        if (p == null) return "no player";
        p.Teleport(new Vector2(x, y));
        p.GetComponent<Rigidbody2D>().simulated = false;
        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.SnapToTarget();
        var c = Camera.main.transform.position;
        return $"player=({x},{y}) cam=({c.x:F1},{c.y:F1}) stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")}";
    }
}
