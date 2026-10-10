// UI の動作確認 (タイトル / ポーズ / アイテム画面)。Play 中に run_script で呼ぶ (Docs/UI/UIImplementation.md §5)。
// unity command run_script --caller plugin --skill unity-cli --file Docs/UI/Build/playtest_ui.cs --entry PlayUIx.State
// 操作は仮想入力 (VirtualPlayerInput: PlayerInputSource.Override) で行う。1 回の呼び出し = 1 回の押下で、次のフレームに効く。
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayUIx
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

    static VirtualPlayerInput Input
    {
        get
        {
            Application.runInBackground = true;
            if (PlayerInputSource.Override is VirtualPlayerInput v) return v;
            v = new VirtualPlayerInput { MergeDevice = true };
            PlayerInputSource.Override = v;
            return v;
        }
    }

    static T Get<T>(object o, string field) => (T)o.GetType().GetField(field, F).GetValue(o);

    public static string State()
    {
        Application.runInBackground = true;
        var sb = new StringBuilder();
        sb.Append("scenes: ");
        for (int i = 0; i < SceneManager.sceneCount; i++) sb.Append(SceneManager.GetSceneAt(i).name + " ");
        sb.Append($"\nstage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : "-")} loading={(StageLoader.Instance && StageLoader.Instance.IsLoading)} paused={GamePause.IsPaused} timeScale={Time.timeScale} anyOpen={MenuPanelView.AnyOpen} playTime={GameSession.PlayTimeSeconds:F2} activeSlot={SaveSystem.ActiveSlot}");
        var item = Object.FindAnyObjectByType<ItemMenuView>();
        if (item) sb.Append($"\nItemMenu open={item.IsOpen} tab={item.CurrentTab}");
        var pause = Object.FindAnyObjectByType<PauseMenuView>();
        if (pause)
        {
            var menu = Get<ImageMenuView>(pause, "_menu");
            var confirm = Get<ImageMenuView>(pause, "_confirm");
            sb.Append($"\nPause menu open={menu.IsOpen} sel={menu.SelectedIndex} confirm open={(confirm ? confirm.IsOpen : false)} sel={(confirm ? confirm.SelectedIndex : -1)}");
        }
        var p = Object.FindAnyObjectByType<PlayerController>();
        if (p) sb.Append($"\nplayer={p.transform.position}");
        return sb.ToString();
    }

    // ---- 仮想入力 ----
    public static string ItemKey() { Input.Tap(PlayerButton.ItemMenu); return "tab"; }
    public static string PauseKey() { Input.Tap(PlayerButton.Pause); return "esc"; }
    public static string TabNext() { Input.MenuTab(+1); return "E"; }
    public static string TabPrev() { Input.MenuTab(-1); return "Q"; }
    public static string Right() { Input.MenuMoveX(+1); return "right"; }
    public static string Left() { Input.MenuMoveX(-1); return "left"; }
    public static string Down() { Input.MenuMove(+1); return "down"; }
    public static string Up() { Input.MenuMove(-1); return "up"; }
    public static string Submit() { Input.MenuSubmit(); return "submit"; }
    public static string Cancel() { Input.MenuCancel(); return "cancel"; }
    /// <summary>実機の Esc と同じ: ポーズ (PausePressed) とメニューの戻る (Cancel) を同じフレームに押す。</summary>
    public static string Esc() { Input.Tap(PlayerButton.Pause); Input.MenuCancel(); return "esc (pause+cancel)"; }
    public static string Release() { PlayerInputSource.Override = null; return "device input"; }

    /// <summary>収集品・日誌のフラグを一部立てる (入手済みの表示確認用)。</summary>
    public static string Flags()
    {
        foreach (var f in new[] { "A1_HeartPin1", "A1_HeartPin3", "A1_Spool2", "A1_Spool3", "A1_Lore_Journal1", "A1_Lore_Journal2", "A1_Lore_Journal5" })
            GameProgress.Set(f);
        var p = Object.FindAnyObjectByType<PlayerController>();
        p?.Progression?.Grant(ScissorUpgrade.Yellow);
        return "flags set";
    }

    /// <summary>タイトル: (1) を飛ばして (2) のメニューを出す。</summary>
    public static string TitleMenu()
    {
        Application.runInBackground = true;
        var t = Object.FindAnyObjectByType<TitleScreen>();
        typeof(TitleScreen).GetMethod("ShowMenu", F).Invoke(t, new object[] { 0 });
        return "title menu";
    }

    /// <summary>アイテム画面の文字の一覧 (表示中の TMP)。</summary>
    public static string Texts()
    {
        var item = Object.FindAnyObjectByType<ItemMenuView>();
        if (!item) return "no item menu";
        return string.Join("\n", item.GetComponentsInChildren<TMP_Text>(false).Where(t => !string.IsNullOrEmpty(t.text))
            .Select(t => $"{t.transform.parent.name}/{t.name}: {t.text.Replace("\n", "/")} (size {t.fontSize:F0})"));
    }

    /// <summary>今のステージの案内 (TutorialHintZone) の一覧。</summary>
    public static string Hints()
    {
        var sb = new StringBuilder();
        foreach (var z in Object.FindObjectsByType<TutorialHintZone>(FindObjectsSortMode.None))
            sb.AppendLine($"{z.gameObject.scene.name}/{z.name}: {Get<string>(z, "_keyboardText")}");
        return sb.ToString();
    }
}

public static class PlayUIcal
{
    /// <summary>資料の詳細に完成形と同じ見本の文字を入れる (文字の大きさ・行間の照合用)。</summary>
    public static string JournalSample()
    {
        var tab = Object.FindAnyObjectByType<ItemJournalTab>();
        var f = BindingFlags.Instance | BindingFlags.NonPublic;
        var title = (TMP_Text)typeof(ItemJournalTab).GetField("_detailTitle", f).GetValue(tab);
        var body = (TMP_Text)typeof(ItemJournalTab).GetField("_detailBody", f).GetValue(tab);
        title.text = "タイトル";
        var line = "内容がないよう内容がないよう内容がないよう内容がないよう";
        body.text = line + line + line + line + "内容がないよう内容がないよう内容がないよう";
        return $"title {title.fontSize} body {body.fontSize} spacing {body.lineSpacing}";
    }
}

public static class PlayUIhome
{
    /// <summary>拠点画面の状態 (開いているか・選択位置)。</summary>
    public static string State()
    {
        var v = Object.FindAnyObjectByType<HomeUIView>();
        var f = BindingFlags.Instance | BindingFlags.NonPublic;
        var menu = (MenuPanelView)typeof(HomeUIView).GetField("_menu", f).GetValue(v);
        var slots = Object.FindAnyObjectByType<SaveSlotView>();
        return $"home open={menu.IsOpen} index={typeof(MenuPanelView).GetField("_index", f).GetValue(menu)} slots open={(slots ? slots.IsOpen : false)} playTime={GameSession.PlayTimeSeconds:F2}";
    }
}

/// <summary>3 回目の決定の確認 (アイテムスロットの付け替え・収集品の絵・資料の本文送り・R3 長押し・入手通知)。</summary>
public static class PlayUIr3
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    /// <summary>所持アイテムを用意する (カタログの先頭から n 種を 1 つずつ)。</summary>
    public static string GiveItems(string n = "3")
    {
        var inv = P.Inventory;
        var k = int.Parse(n);
        var names = new StringBuilder();
        foreach (var item in inv.Catalog.Take(k))
        {
            inv.TryAddItem(item);
            names.Append($"{item.DisplayName}x{inv.GetCount(item)} ");
        }
        return "items: " + names;
    }

    /// <summary>スロットの中身と HUD の表示。</summary>
    public static string Slots()
    {
        var inv = P.Inventory;
        var sb = new StringBuilder("slots: ");
        foreach (ItemSlot s in System.Enum.GetValues(typeof(ItemSlot)))
            sb.Append($"{s}={(inv.GetSlotItem(s) ? inv.GetSlotItem(s).DisplayName : "-")} ");
        var hud = Object.FindAnyObjectByType<ItemSlotView>();
        if (hud)
        {
            var labels = (TMP_Text[])typeof(ItemSlotView).GetField("_labels", F).GetValue(hud);
            sb.Append("\nHUD: " + string.Join(" | ", labels.Select(l => l ? l.text : "null")));
        }
        var tab = Object.FindAnyObjectByType<ItemEquipTab>();
        if (tab)
            sb.Append($"\nequip index={typeof(ItemEquipTab).GetField("_index", F).GetValue(tab)} picking={typeof(ItemEquipTab).GetField("_picking", F).GetValue(tab)} pickIndex={typeof(ItemEquipTab).GetField("_pickIndex", F).GetValue(tab)} pickSlot={typeof(ItemEquipTab).GetField("_pickSlot", F).GetValue(tab)}");
        return sb.ToString();
    }

    /// <summary>資料の詳細の本文を長い仮文に差し替えて測り直す (送りの確認用。アセットは書き換えない)。</summary>
    public static string LongBody()
    {
        var tab = Object.FindAnyObjectByType<ItemJournalTab>();
        var body = (TMP_Text)typeof(ItemJournalTab).GetField("_detailBody", F).GetValue(tab);
        var sb = new StringBuilder();
        for (var i = 1; i <= 14; i++) sb.Append($"{i}行目 内容がないよう内容がないよう内容がないよう内容\n");
        body.text = sb.ToString().TrimEnd();
        typeof(ItemJournalTab).GetMethod("MeasureBody", F).Invoke(tab, null);
        typeof(ItemJournalTab).GetMethod("RenderBodyScroll", F).Invoke(tab, null);
        return Journal();
    }

    public static string Journal()
    {
        var tab = Object.FindAnyObjectByType<ItemJournalTab>();
        var body = (TMP_Text)typeof(ItemJournalTab).GetField("_detailBody", F).GetValue(tab);
        var ind = (GameObject)typeof(ItemJournalTab).GetField("_bodyScrollIndicator", F).GetValue(tab);
        var thumb = (RectTransform)typeof(ItemJournalTab).GetField("_bodyScrollThumb", F).GetValue(tab);
        return $"open={typeof(ItemJournalTab).GetField("_detailOpen", F).GetValue(tab)} step={typeof(ItemJournalTab).GetField("_bodyStep", F).GetValue(tab)}/{typeof(ItemJournalTab).GetField("_bodyMaxStep", F).GetValue(tab)} pitch={typeof(ItemJournalTab).GetField("_bodyPitch", F).GetValue(tab)} bodyY={body.rectTransform.anchoredPosition.y} lines={body.textInfo.lineCount} indicator={(ind ? ind.activeSelf : false)} thumb={(thumb ? thumb.anchoredPosition.y + "/" + thumb.sizeDelta.y : "-")}";
    }

    /// <summary>収集品の枠と詳細の絵。</summary>
    public static string Icons()
    {
        var tab = Object.FindAnyObjectByType<ItemCollectionTab>();
        var cells = (ItemMenuTab.Cell[])typeof(ItemCollectionTab).GetField("_cells", F).GetValue(tab);
        var sb = new StringBuilder();
        for (var i = 0; i < 8; i++)
            sb.Append($"[{i}] icon={(cells[i].Icon && cells[i].Icon.gameObject.activeSelf ? cells[i].Icon.sprite.name : "-")} label='{cells[i].Label.text}' ");
        var d = (Image)typeof(ItemCollectionTab).GetField("_detailIcon", F).GetValue(tab);
        sb.Append($"\ndetail icon={(d && d.gameObject.activeInHierarchy ? d.sprite.name : "-")}");
        return sb.ToString();
    }

    // ---- 仮想ゲームパッド (R3 / Select) ----
    static UnityEngine.InputSystem.Gamepad Pad
    {
        get
        {
            var pad = UnityEngine.InputSystem.InputSystem.devices.OfType<UnityEngine.InputSystem.Gamepad>().FirstOrDefault(g => g.name.StartsWith("TestPad"));
            if (pad == null)
            {
                pad = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>("TestPad");
            }
            pad.MakeCurrent();
            return pad;
        }
    }

    static string PadState(params UnityEngine.InputSystem.LowLevel.GamepadButton[] buttons)
    {
        var st = new UnityEngine.InputSystem.LowLevel.GamepadState();
        foreach (var b in buttons) st = st.WithButton(b);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(Pad, st);
        return "pad: " + string.Join("+", buttons);
    }

    /// <summary>R3 を指定秒数 (実時間) だけ押して離す。押す前と離した直後のオーバーレイの表示を PlayerPrefs "r3tap" に残す (HoldResult で読む)。</summary>
    public static string HoldR3(string seconds)
    {
        var sec = float.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture);
        var before = TestPlayOverlay.Visible;
        var start = UnityEditor.EditorApplication.timeSinceStartup;
        PadR3();
        UnityEditor.EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            var held = UnityEditor.EditorApplication.timeSinceStartup - start;
            if (held < sec) return;
            UnityEditor.EditorApplication.update -= tick;
            var atRelease = TestPlayOverlay.Visible;
            PadNone();
            PlayerPrefs.SetString("r3tap", $"held={held:F2}s before={before} atRelease={atRelease}");
        };
        PlayerPrefs.DeleteKey("r3tap");
        UnityEditor.EditorApplication.update += tick;
        return $"holding R3 {sec}s (overlay before={before})";
    }

    public static string HoldResult() { var r = PlayerPrefs.GetString("r3tap", "pending"); PlayerPrefs.DeleteKey("r3tap"); return r; }

    /// <summary>キーボードの何でもないキー (右 Ctrl) を押す → InputDeviceTracker をキーボードに戻す。KeyNone で離す。</summary>
    public static string KeyAny() { UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.RightCtrl)); return "key"; }
    public static string KeyNone() { UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState()); return "key up"; }

    /// <summary>InputDeviceTracker の現在のデバイスを直接切り替える (CLI からキーボードの仮想入力が届かない時用)。</summary>
    public static string ForceDevice(string kind)
    {
        typeof(InputDeviceTracker).GetProperty("Current").SetValue(null, kind == "Gamepad" ? InputDeviceKind.Gamepad : InputDeviceKind.Keyboard);
        return "device=" + InputDeviceTracker.Current;
    }

    public static string PadR3() => PadState(UnityEngine.InputSystem.LowLevel.GamepadButton.RightStick);
    public static string PadSelect() => PadState(UnityEngine.InputSystem.LowLevel.GamepadButton.Select);
    public static string PadSelectR3() => PadState(UnityEngine.InputSystem.LowLevel.GamepadButton.Select, UnityEngine.InputSystem.LowLevel.GamepadButton.RightStick);
    public static string PadNone() => PadState();
    public static string PadSouth() => PadState(UnityEngine.InputSystem.LowLevel.GamepadButton.South);
    public static string PadRemove()
    {
        foreach (var g in UnityEngine.InputSystem.InputSystem.devices.OfType<UnityEngine.InputSystem.Gamepad>().Where(g => g.name.StartsWith("TestPad")).ToArray())
            UnityEngine.InputSystem.InputSystem.RemoveDevice(g);
        return "removed";
    }

    /// <summary>Game ビューにフォーカス (InputSystem の実デバイス入力は Game ビューのフォーカスに従う)。</summary>
    public static string Focus()
    {
        var t = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
        UnityEditor.EditorWindow.FocusWindowIfItsOpen(t);
        return "focused " + (UnityEditor.EditorWindow.focusedWindow ? UnityEditor.EditorWindow.focusedWindow.GetType().Name : "-");
    }

    static UnityEngine.InputSystem.InputSettings _savedSettings;

    /// <summary>
    /// 仮想パッドの確認用: 入力設定を複製して「フォーカスに関係なく入力を受ける」にし、実機のパッドを一時的に無効にする
    /// (実機のパッドがつながっていると Gamepad.current が実機に戻るため)。PadEnd で元に戻す (設定アセットは書き換えない)。
    /// </summary>
    public static string PadBegin()
    {
        if (_savedSettings == null)
        {
            _savedSettings = UnityEngine.InputSystem.InputSystem.settings;
            var copy = Object.Instantiate(_savedSettings);
            copy.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            copy.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            UnityEngine.InputSystem.InputSystem.settings = copy;
        }
        foreach (var g in UnityEngine.InputSystem.InputSystem.devices.OfType<UnityEngine.InputSystem.Gamepad>().Where(g => !g.name.StartsWith("TestPad")).ToArray())
            UnityEngine.InputSystem.InputSystem.DisableDevice(g);
        var pad = Pad;
        return PadInfo();
    }

    public static string PadEnd()
    {
        PadRemove();
        foreach (var d in UnityEngine.InputSystem.InputSystem.devices.Where(d => !d.enabled).ToArray())
            UnityEngine.InputSystem.InputSystem.EnableDevice(d);
        // run_script は呼ぶたびに別アセンブリ (static は残らない) なので、プロジェクトの設定アセットから戻す
        if (UnityEditor.EditorBuildSettings.TryGetConfigObject<UnityEngine.InputSystem.InputSettings>("com.unity.input.settings", out var original))
            UnityEngine.InputSystem.InputSystem.settings = original;
        else
            UnityEngine.InputSystem.InputSystem.settings = ScriptableObject.CreateInstance<UnityEngine.InputSystem.InputSettings>(); // 設定アセットが無い = 既定値
        return PadInfo();
    }

    public static string PadInfo()
    {
        var g = UnityEngine.InputSystem.Gamepad.current;
        var all = string.Join(",", UnityEngine.InputSystem.InputSystem.devices.Select(d => d.name));
        return g == null ? "no pad; devices=" + all : $"current={g.name} select={g.selectButton.isPressed} r3={g.rightStickButton.isPressed} south={g.buttonSouth.isPressed} bg={UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior} focus={Application.isFocused} devices={all}";
    }

    public static string Overlay() => $"overlay={TestPlayOverlay.Visible} device={InputDeviceTracker.Current} item={(Object.FindAnyObjectByType<ItemMenuView>()?.IsOpen)}";
    public static string SetOverlay(string v) { TestPlayOverlay.Visible = v == "1"; return Overlay(); }

    // ---- 入手通知 ----
    public static string Grant(string what)
    {
        switch (what)
        {
            case "Red": TestPlayActions.SetUpgrade(ScissorUpgrade.Red, false); TestPlayActions.SetUpgrade(ScissorUpgrade.Red, true); break;
            default:
                var s = (PlayerSkill)System.Enum.Parse(typeof(PlayerSkill), what);
                TestPlayActions.SetSkill(s, false); TestPlayActions.SetSkill(s, true); break;
        }
        return Notice();
    }

    public static string Notice()
    {
        var v = Object.FindAnyObjectByType<NotificationView>();
        var l = (TMP_Text)typeof(NotificationView).GetField("_label", F).GetValue(v);
        return $"device={InputDeviceTracker.Current} notice='{l.text.Replace("\n", " / ")}' lines={l.textInfo.lineCount}";
    }

    public static string Shot(string name)
    {
        System.IO.Directory.CreateDirectory("Assets/Screenshots/UI");
        ScreenCapture.CaptureScreenshot($"Assets/Screenshots/UI/{name}.png");
        return "shot " + name;
    }
}

/// <summary>4回目の決定の確認 (選び直しの一覧の仮素材・0 個の灰色・入れ替え・入手通知の 2 行)。</summary>
public static class PlayUIr4
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    /// <summary>カタログの先頭 n 種を 1 つずつ持たせ、残りは 0 個、スロットは全部空にする。</summary>
    public static string Setup(string n = "3")
    {
        var inv = P.Inventory;
        var k = int.Parse(n);
        for (var i = 0; i < inv.Catalog.Count; i++)
        {
            var item = inv.Catalog[i];
            while (inv.TryConsume(item)) { }
            if (i < k) inv.TryAddItem(item);
        }
        foreach (ItemSlot s in System.Enum.GetValues(typeof(ItemSlot)))
            inv.SetSlot(s, null);
        return string.Join(" ", inv.Catalog.Select(c => $"{c.DisplayName}x{inv.GetCount(c)}"));
    }

    /// <summary>選び直しの一覧の各行 (板・アイコン・文字・透明度)。</summary>
    public static string Picker()
    {
        var tab = Object.FindAnyObjectByType<ItemEquipTab>();
        var rows = (ItemMenuTab.Cell[])typeof(ItemEquipTab).GetField("_pickerRows", F).GetValue(tab);
        var heading = (TMP_Text)typeof(ItemEquipTab).GetField("_pickerHeading", F).GetValue(tab);
        var sb = new StringBuilder($"heading='{heading.text}' picking={typeof(ItemEquipTab).GetField("_picking", F).GetValue(tab)}");
        foreach (var r in rows)
        {
            if (!r.Root.gameObject.activeInHierarchy) { sb.Append("\n  (hidden)"); continue; }
            var plate = r.Root.GetComponent<Image>();
            sb.Append($"\n  plate={plate.sprite.name} scale={r.Root.localScale.x:F2} icon={(r.Icon.gameObject.activeSelf ? r.Icon.sprite.name : "-")} label='{r.Label.text}' a={r.Label.color.a:F2}");
        }
        return sb + "\n" + PlayUIr3.Slots();
    }

    /// <summary>通知を出して、表示されていた実時間を PlayerPrefs "noticeTime" に残す (NoticeResult で読む)。</summary>
    public static string Notice(string twoLines)
    {
        var msg = twoLines == "1" ? "赤いハサミを手に入れた!\n空中で [Space] (×) で二段ジャンプ" : "糸を 10 手に入れた (所持: 10)";
        var v = Object.FindAnyObjectByType<NotificationView>();
        var showing = typeof(NotificationView).GetField("_showing", F);
        PlayerPrefs.DeleteKey("noticeTime");
        double start = -1;
        UnityEditor.EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (v == null) { UnityEditor.EditorApplication.update -= tick; return; }
            var on = (bool)showing.GetValue(v);
            var now = UnityEditor.EditorApplication.timeSinceStartup;
            if (on && start < 0) start = now;
            if (!on && start >= 0)
            {
                var label = (TMP_Text)typeof(NotificationView).GetField("_label", F).GetValue(v);
                PlayerPrefs.SetString("noticeTime", $"shown {now - start:F2}s lines={label.textInfo.lineCount} band={((RectTransform)v.transform).sizeDelta}");
                UnityEditor.EditorApplication.update -= tick;
            }
        };
        UnityEditor.EditorApplication.update += tick;
        Notifier.Notify(msg);
        return "notify: " + msg.Replace("\n", " / ");
    }

    public static string NoticeResult() => PlayerPrefs.GetString("noticeTime", "pending");
}
