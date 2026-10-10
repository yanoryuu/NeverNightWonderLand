using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
// プレイ中の計測 (A1_D 区画)。結果は PlayerPrefs "dtest_log" 経由 (run_script は呼ぶたびに別アセンブリになるので static に残せない)
// 蒸気: --entry SteamTest.Start --args '["SteamC1",206.5,-131]' → 7 秒後 --entry SteamTest.Log (その場に留めて ON/OFF と HP の変化を記録)
// 水:   --entry WaterTest.Start → 9 秒後 --entry WaterTest.Log (乾いた床/水中の歩き速度とジャンプの上昇量。Keyboard に状態イベントを積んで A/Space を押す)
// 静止画: --entry Freeze.AtWarning --args '["SteamC2",210,-131]' (予兆が出た瞬間に timeScale=0) → capture_game_view → --entry Freeze.Resume
// 蒸気の真下に立たせて、HazardZone の ON/OFF とダメージの瞬間を記録する (コルーチンで 6 秒)
public static class SteamTest {
  static string _log { get => PlayerPrefs.GetString("dtest_log", ""); set => PlayerPrefs.SetString("dtest_log", value); }
  public static string Start(string name, float x, float y) {
    Application.runInBackground = true;
    _log = "";
    var p = Object.FindAnyObjectByType<PlayerController>();
    var hz = GameObject.Find(name).GetComponent<HazardZone>();
    var hp = p.GetComponent<PlayerHealth>();
    var runner = new GameObject("SteamTestRunner").AddComponent<Runner>();
    runner.StartCoroutine(Run(p, hz, hp, x, y));
    return "started";
  }
  public static string Log() => _log;
  class Runner : MonoBehaviour {}
  static System.Collections.IEnumerator Run(PlayerController p, HazardZone hz, PlayerHealth hp, float x, float y) {
    int last = hp.Hp.CurrentValue; bool lastOn = hz.IsHazardActive;
    _log += $"t={Time.time:F2} start on={lastOn} hp={last}\n";
    float t0 = Time.time;
    while (Time.time - t0 < 6f) {
      p.Teleport(new Vector2(x, y)); // その場に留める (ノックバックで外へ出ないように毎フレーム戻す)
      if (hz.IsHazardActive != lastOn) { lastOn = hz.IsHazardActive; _log += $"t={Time.time:F2} on={lastOn}\n"; }
      if (hp.Hp.CurrentValue != last) { _log += $"t={Time.time:F2} HP {last}->{hp.Hp.CurrentValue} (on={hz.IsHazardActive})\n"; last = hp.Hp.CurrentValue; }
      yield return new WaitForFixedUpdate();
    }
    _log += "done\n";
  }
}
public static class WaterTest {
  static string _log { get => PlayerPrefs.GetString("dtest_log", ""); set => PlayerPrefs.SetString("dtest_log", value); }
  public static string Log() => _log;
  class Runner : MonoBehaviour {}
  public static string Start() {
    Application.runInBackground = true;
    _log = "";
    new GameObject("WaterTestRunner").AddComponent<Runner>().StartCoroutine(Run());
    return "started";
  }
  static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys)); }
  static System.Collections.IEnumerator Measure(PlayerController p, Vector2 at, string label) {
    p.Teleport(at);
    Keys();
    yield return new WaitForSeconds(0.6f);
    var x0 = p.transform.position.x; var t0 = Time.time;
    Keys(Key.A);
    yield return new WaitForSeconds(0.25f);
    x0 = p.transform.position.x; t0 = Time.time;
    yield return new WaitForSeconds(0.5f);
    var v = (p.transform.position.x - x0) / (Time.time - t0);
    Keys();
    _log += $"{label}: vx={v:F2} pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) grounded={p.IsGrounded}\n";
  }
  static System.Collections.IEnumerator Run() {
    var p = Object.FindAnyObjectByType<PlayerController>();
    yield return Measure(p, new Vector2(200f, -131f), "dry(D5 chamber floor)");
    yield return Measure(p, new Vector2(163f, -152f), "water(D6 floor)");
    // ジャンプ高さ (水中): Space を 0.4 秒押す
    p.Teleport(new Vector2(160f, -152f));
    yield return new WaitForSeconds(0.6f);
    var y0 = p.transform.position.y; var maxY = y0;
    Keys(Key.Space);
    for (var i = 0; i < 90; i++) { yield return new WaitForFixedUpdate(); maxY = Mathf.Max(maxY, p.transform.position.y); if (i == 20) Keys(); }
    _log += $"water jump: rise={maxY - y0:F2}\n";
    p.Teleport(new Vector2(199.5f, -131f));
    yield return new WaitForSeconds(0.6f);
    y0 = p.transform.position.y; maxY = y0;
    Keys(Key.Space);
    for (var i = 0; i < 90; i++) { yield return new WaitForFixedUpdate(); maxY = Mathf.Max(maxY, p.transform.position.y); if (i == 20) Keys(); }
    _log += $"dry jump: rise={maxY - y0:F2}\n";
    Keys();
    _log += "done\n";
  }
}
public static class Freeze {
  class Runner : MonoBehaviour {}
  public static string AtWarning(string name, float px, float py) {
    Application.runInBackground = true;
    var p = Object.FindAnyObjectByType<PlayerController>();
    p.Teleport(new Vector2(px, py));
    var hiss = GameObject.Find(name).transform.Find("SteamHiss").gameObject;
    new GameObject("FreezeRunner").AddComponent<Runner>().StartCoroutine(Run(hiss));
    return "waiting";
  }
  static System.Collections.IEnumerator Run(GameObject hiss) {
    float t0 = Time.realtimeSinceStartup;
    while (!hiss.activeInHierarchy && Time.realtimeSinceStartup - t0 < 8f) yield return null;
    yield return new WaitForSecondsRealtime(0.25f);
    Time.timeScale = 0f;
  }
  public static string Resume() { Time.timeScale = 1f; return "resumed"; }
}
