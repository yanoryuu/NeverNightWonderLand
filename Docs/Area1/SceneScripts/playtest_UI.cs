// UI シーン分離 (StageUI / ShopUI) のプレイ確認。Play 中に run_script で呼ぶ (SceneBuildGuide §11)。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/playtest_UI.cs --entry PlayUI.State
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayUI
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static PlayerController Player => Object.FindAnyObjectByType<PlayerController>();

    static string Label(Component view)
    {
        var f = view.GetType().GetField("_label", F);
        var t = f != null ? f.GetValue(view) as TMP_Text : null;
        return t != null ? t.text.Replace("\n", "/") : "-";
    }

    public static string State()
    {
        Application.runInBackground = true;
        var sb = new StringBuilder();
        sb.Append("scenes: ");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            sb.Append(s.name + (s.isLoaded ? "" : "(loading)") + (s == SceneManager.GetActiveScene() ? "*" : "") + " ");
        }
        sb.Append($"\nstage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")} StageInfo={(StageInfo.Current ? StageInfo.Current.AreaName : "null")} frame={Time.frameCount} ut={Time.unscaledTime:F2} udt={Time.unscaledDeltaTime:F3} paused={GamePause.IsPaused} menuOpen={MenuPanelView.AnyOpen}");
        var p = Player;
        if (p) sb.Append($" player={p.transform.position}");
        sb.Append("\ncanvases (root, overlay): ");
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace).OrderBy(c => c.sortingOrder))
            sb.Append($"{c.gameObject.scene.name}/{c.name}({c.sortingOrder}) ");
        foreach (var v in Object.FindObjectsByType<AreaTitleView>(FindObjectsSortMode.None))
            sb.Append($"\nAreaTitle[{v.gameObject.scene.name}] enabled={v.enabled} alpha={v.GetComponent<CanvasGroup>().alpha:F2} text={Label(v)}");
        foreach (var v in Object.FindObjectsByType<TutorialMessageView>(FindObjectsSortMode.None))
            sb.Append($"\nMessage[{v.gameObject.scene.name}] alpha={v.GetComponent<CanvasGroup>().alpha:F2} text={Label(v)}");
        foreach (var v in Object.FindObjectsByType<ShopView>(FindObjectsSortMode.None))
        {
            var menu = (MenuPanelView)typeof(ShopView).GetField("_menu", F).GetValue(v);
            sb.Append($"\nShop[{v.gameObject.scene.name}] open={menu.IsOpen}");
        }
        return sb.ToString();
    }

    /// <summary>現在のステージの n 番目の TutorialHintZone の中へ飛ぶ。</summary>
    public static string Hint(int n)
    {
        var stage = SceneManager.GetActiveScene();
        var zones = stage.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TutorialHintZone>()).ToList();
        if (zones.Count == 0) return "no hint zones\n" + State();
        var z = zones[Mathf.Clamp(n, 0, zones.Count - 1)];
        var b = z.GetComponent<Collider2D>().bounds;
        Player.Teleport(new Vector2(b.center.x, b.min.y + 0.8f));
        return $"tp to {z.name} ({zones.Count} zones) {b.center}\n" + State();
    }

    public static string Tp(float x, float y) { Player.Teleport(new Vector2(x, y)); return State(); }

    /// <summary>名前に name を含む遷移ゾーンへ飛ぶ。</summary>
    public static string Exit(string name)
    {
        var z = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneTransitionZone>()).FirstOrDefault(t => t.name.Contains(name));
        if (z == null) return "no exit " + name;
        Player.Teleport(z.GetComponent<Collider2D>().bounds.center);
        return "tp to " + z.name + "\n" + State();
    }

    public static string Shop()
    {
        var k = Object.FindAnyObjectByType<ShopKeeper>();
        if (k == null) return "no ShopKeeper";
        k.Interact(Player.gameObject);
        return "shop interact\n" + State();
    }

    public static string ShopClose()
    {
        foreach (var v in Object.FindObjectsByType<ShopView>(FindObjectsSortMode.None))
        {
            var menu = (MenuPanelView)typeof(ShopView).GetField("_menu", F).GetValue(v);
            if (menu.IsOpen) typeof(ShopView).GetMethod("CloseMenu", F).Invoke(v, null);
        }
        return "shop close\n" + State();
    }

    public static string Npc()
    {
        // エリア1 の NPC は Utage 会話 (NpcTalkPoint)。旧シーンの NpcTalk も探す
        var n = (MonoBehaviour)Object.FindAnyObjectByType<NpcTalkPoint>() ?? Object.FindAnyObjectByType<NpcTalk>();
        if (n == null) return "no NpcTalkPoint / NpcTalk";
        Player.Teleport(n.transform.position + Vector3.up * 0.6f);
        ((IInteractable)n).Interact(Player.gameObject);
        return $"npc {n.name} interact dialogue={DialogueService.IsPlaying}\n" + State();
    }

    public static string Pause(bool open)
    {
        var v = Object.FindAnyObjectByType<PauseMenuView>();
        typeof(PauseMenuView).GetMethod(open ? "OpenMenu" : "CloseMenu", F).Invoke(v, null);
        return $"pause {open}\n" + State();
    }

    public static string Home(bool open)
    {
        var v = Object.FindAnyObjectByType<HomeUIView>();
        if (open)
        {
            var sp = Object.FindAnyObjectByType<SavePoint>();
            if (sp != null) sp.Interact(Player.gameObject);
            else v.Open(Player.gameObject, null);
        }
        else typeof(HomeUIView).GetMethod("CloseMenu", F).Invoke(v, null);
        var menu = (MenuPanelView)typeof(HomeUIView).GetField("_menu", F).GetValue(v);
        return $"home {open} homeOpen={menu.IsOpen}\n" + State();
    }
}
