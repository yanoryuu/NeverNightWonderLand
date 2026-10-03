// プレイ中の状態確認/テレポート (A1_T3..C1 の遷移テストに使用)。例: --entry PlayA.Status / 引数付きは小さなラッパーから PlayA.Tp(x,y)。
// 遷移テストの座標: T2→T3 (91.5,-94.5) / T3→T4 (116,-52.5) / T4→B1 (163.8,-55.5) / B1→H (230,-52.5) / H→C1 (289.8,-52.5) / C1→H (289,-52.5) / H→B1 (229.5,-52.5) / B1→T4 (163.5,-55.5) / T4→T3 (115.5,-52.5) / T3→T2 (91.5,-94.5)
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PlayA {
  static string State() {
    var sb = new System.Text.StringBuilder();
    var p = Object.FindAnyObjectByType<PlayerController>();
    if (p) sb.Append($"pos={p.transform.position} grounded={p.IsGrounded} ground={(p.GroundCollider? p.GroundCollider.name : "null")}");
    else sb.Append("no player");
    sb.Append($" stage={(StageLoader.Instance? StageLoader.Instance.CurrentStageName : "-")} t={Time.time:F1}");
    return sb.ToString();
  }
  public static string Status() { Application.runInBackground = true; return State(); }
  public static string Tp(float x, float y) { Application.runInBackground = true; var p = Object.FindAnyObjectByType<PlayerController>(); p.Teleport(new Vector2(x,y)); return "tp\n" + State(); }
}
