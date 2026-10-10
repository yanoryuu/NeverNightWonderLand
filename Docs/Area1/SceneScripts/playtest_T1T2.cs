using UnityEngine;
using UnityEngine.SceneManagement;
public static class PlayT {
  static string State() {
    var sb = new System.Text.StringBuilder();
    sb.Append("scenes: ");
    for (int i=0;i<SceneManager.sceneCount;i++){ var s=SceneManager.GetSceneAt(i); sb.Append(s.name+(s.isLoaded?"":"(loading)")+(s==SceneManager.GetActiveScene()?"*":"")+" "); }
    var p = Object.FindAnyObjectByType<PlayerController>();
    if (p) { var rb = p.GetComponent<Rigidbody2D>(); sb.Append($"\nplayer pos={p.transform.position} vel={rb.linearVelocity} grounded={p.IsGrounded} ground={(p.GroundCollider? p.GroundCollider.name : "null")}"); }
    else sb.Append("\nno player");
    sb.Append($"\nstage={(StageLoader.Instance? StageLoader.Instance.CurrentStageName : "-")} time={Time.time:F1}");
    return sb.ToString();
  }
  public static string Status() { Application.runInBackground = true; return State(); }
  static string Tp(Vector2 v) { var p = Object.FindAnyObjectByType<PlayerController>(); p.Teleport(v); return "tp " + v + "\n" + State(); }
  public static string ToT2() => Tp(new Vector2(46.5f, -94.5f));
  public static string ToT1() => Tp(new Vector2(46f, -94.5f));
  public static string Pit() => Tp(new Vector2(73.5f, -100f));
  public static string Bridge() => Tp(new Vector2(68f, -83f));
  public static string Alcove() => Tp(new Vector2(60f, -83f));
  public static string Shelf() => Tp(new Vector2(21f, -91.5f));
}
