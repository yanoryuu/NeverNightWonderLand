// 2026-10-09 PendingChanges 51 の確認 (NPC をプレイヤーの後ろに描く / 拠点メニューの「糸: n」と 1 行目の重なり)。
// セーブは事前に退避し、後でバイト一致で戻すこと (拠点で休むとセーブが更新される, PendingChanges 47)。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/playtest_batch8.cs --entry Batch8.Start --args '["A1_H"]'
//   → Batch8.Tp(x, y) → Batch8.Order() (近くの NPC・プレイヤーの描画順) → Batch8.Shot(name)
//   → Batch8.HomeOpen() / Batch8.HomeLayout() (説明文の文字の下端と 1 行目の上端) / Batch8.HomeAttacks() / Batch8.HomeClose()
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

public static class Batch8
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    public static string Start(string stage)
    {
        Application.runInBackground = true;
        GameSession.PendingLoad = null;
        GameSession.SaveBelongsToRun = false;
        GameSession.RunSlotChosen = false;
        GameProgress.Clear();
        GameSession.ClearRespawnBench();
        StageLoader.LoadWithPlayerScene(stage);
        return "start " + stage;
    }

    public static string Tp(float x, float y)
    {
        P.Teleport(new Vector2(x, y));
        return $"tp ({P.transform.position.x:F2},{P.transform.position.y:F2})";
    }

    /// <summary>プレイヤーの Spine と、近くの NPC (NpcTalkPoint / NpcTalk / ShopKeeper / Blacksmith / SavePoint) の描画順。</summary>
    public static string Order()
    {
        var p = P;
        var mr = p.GetComponentInChildren<MeshRenderer>();
        var sb = new StringBuilder($"player mesh layer={mr.sortingLayerName} order={mr.sortingOrder} pos=({p.transform.position.x:F1},{p.transform.position.y:F1})");
        var targets = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(m => m is NpcTalkPoint || m is NpcTalk || m is ShopKeeper || m is Blacksmith || m is SavePoint);
        foreach (var t in targets)
        {
            var sr = t.GetComponent<SpriteRenderer>();
            sb.Append($" | {t.gameObject.name} ({t.GetType().Name}) order={(sr != null ? sr.sortingOrder.ToString() : "-")} at ({t.transform.position.x:F1},{t.transform.position.y:F1})");
        }
        var bp = p.GetComponent<InteractButtonPrompt>();
        if (bp != null) sb.Append($" | prompt visible={bp.IsVisible}");
        return sb.ToString();
    }

    public static string Shot(string name)
    {
        Directory.CreateDirectory("Assets/Screenshots/Area1");
        ScreenCapture.CaptureScreenshot($"Assets/Screenshots/Area1/{name}.png");
        return "shot " + name;
    }

    static HomeUIView Home => Object.FindFirstObjectByType<HomeUIView>();

    public static string HomeOpen()
    {
        var sp = Object.FindAnyObjectByType<SavePoint>();
        if (sp == null || Home == null) return "no savepoint/home";
        Home.Open(P.gameObject, sp);
        return "home open";
    }

    public static string HomeAttacks()
    {
        typeof(HomeUIView).GetMethod("ShowAttacks", F).Invoke(Home, null);
        return "attacks";
    }

    public static string HomeClose()
    {
        typeof(HomeUIView).GetMethod("CloseMenu", F).Invoke(Home, null);
        return "closed";
    }

    /// <summary>説明文 (Body) の文字の下端と 1 行目 (Row0) の文字の上端・行の上端 (画面のピクセル, 上が大きい)。</summary>
    public static string HomeLayout()
    {
        var menu = (MenuPanelView)typeof(HomeUIView).GetField("_menu", F).GetValue(Home);
        var window = menu.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Window");
        var body = window.Find("Body").GetComponent<TMP_Text>();
        var row0 = window.Find("Rows/Row0").GetComponent<TMP_Text>();
        body.ForceMeshUpdate();
        row0.ForceMeshUpdate();
        float BodyBottom() => body.transform.TransformPoint(new Vector3(0, body.textBounds.min.y, 0)).y;
        float Row0TextTop() => row0.transform.TransformPoint(new Vector3(0, row0.textBounds.max.y, 0)).y;
        var rc = new Vector3[4];
        row0.rectTransform.GetWorldCorners(rc);
        var canvas = menu.GetComponentInParent<Canvas>().rootCanvas;
        return $"screen {Screen.width}x{Screen.height} scale={canvas.scaleFactor:F3} body='{body.text}' bodyTextBottom={BodyBottom():F1} " +
               $"row0='{row0.text}' row0TextTop={Row0TextTop():F1} row0RectTop={rc[1].y:F1} gap={(BodyBottom() - Row0TextTop()) / canvas.scaleFactor:F1}(canvas px)";
    }
}
