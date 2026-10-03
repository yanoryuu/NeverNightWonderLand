using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// タイトル / セーブスロット / ポーズ / アイテム画面の UI 組み立てレシピ (Docs/UI/UIImplementation.md)。
// 座標は完成形 (4000x2250) を 1920x1080 に縮めて部品を照合した実測値 (Canvas 中心原点, 上が +y)。
// 何度流してもよい (プレハブ/シーンの該当部分を作り直す)。先に import_ui.cs を流しておくこと。
//   unity command run_script --file Docs/UI/Build/build_ui.cs --entry BuildUI.SaveSlot
//   unity command run_script --file Docs/UI/Build/build_ui.cs --entry BuildUI.Pause
//   unity command run_script --file Docs/UI/Build/build_ui.cs --entry BuildUI.Title
//   unity command run_script --file Docs/UI/Build/build_ui.cs --entry BuildUI.Catalog    (収集品・資料の一覧データ)
//   unity command run_script --file Docs/UI/Build/build_ui.cs --entry BuildUI.ItemIcons  (消耗品アイテムのアイコン = _temp/ の仮素材)
//   unity command run_script --file Docs/UI/Build/build_ui.cs --entry BuildUI.ItemMenu   (アイテム画面。Catalog の後)
//   unity command run_script --file Docs/UI/Build/build_ui.cs --entry BuildUI.StageNames (旧セーブ表示用の場所・章の対応表)
//   unity command run_script --file Docs/UI/Build/build_ui.cs --entry BuildUI.ShopIcons  (ショップの行に品物の絵。ItemIcons の後)
//   unity command run_script --file Docs/UI/Build/build_ui.cs --entry BuildUI.ShopLayout (ショップの説明文と 1 行目が重ならない位置)
public static class BuildUI
{
    const string UI = "Assets/LocalData/UI/";
    const string FontPath = "Assets/Font/YDW_bananaslip_plus_240809/YDWbananaslipplus SDF.asset";
    const string SaveSlotPrefabPath = "Assets/Prefabs/SaveSlotUI.prefab";
    const string SaveSlotScenePath = "Assets/Scenes/UI/SaveSlotUI.unity";
    const string PausePrefabPath = "Assets/Prefabs/PauseUI.prefab";
    const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    const string PlayerScenePath = "Assets/Scenes/PlayerScene.unity";

    static readonly Color Orange = new Color32(235, 160, 68, 255); // 完成形の文字色 #EBA044
    static readonly Color Maroon = new Color32(73, 0, 0, 255);     // 枠の中・スロットの文字色 #490000

    // ---- 文字の大きさ (完成形の字の高さに合わせて調整した値) ----
    public static float TitleButtonFont = 45f;
    public static float PauseButtonFont = 37f;
    public static float HeadingFont = 46f;
    public static float SlotNumberFont = 40f;
    public static float SlotChapterFont = 38f;
    public static float SlotPlaceFont = 22f;
    public static float SlotDateFont = 14f;
    public static float SlotTimeFont = 23.5f;
    public static float LabelSpacing = 3f;   // 完成形はボタンの文字を字間を空けて組んでいる
    public static float HeadingSpacing = 1f;

    static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

    static Sprite S(string rel)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(UI + rel);
        if (s == null) throw new Exception("sprite missing: " + rel);
        return s;
    }

    #region helpers

    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Stretch(string name, Transform parent)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        return rt;
    }

    static Image AddImage(GameObject go, Sprite sprite)
    {
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>素材をそのままの大きさ (1 px = 1 単位) で置く。</summary>
    static Image Img(string name, Transform parent, string sprite, Vector2 pos)
    {
        var s = S(sprite);
        var rt = Rect(name, parent, pos, s.rect.size);
        return AddImage(rt.gameObject, s);
    }

    static Image FullImg(string name, Transform parent, string sprite)
    {
        var rt = Stretch(name, parent);
        return AddImage(rt.gameObject, S(sprite));
    }

    static TextMeshProUGUI Txt(string name, Transform parent, string text, Vector2 pos, Vector2 size,
        float fontSize, Color color, TextAlignmentOptions align, float spacing = 0f)
    {
        var rt = Rect(name, parent, pos, size);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = Font;
        t.fontSharedMaterial = Font.material;
        t.text = text;
        t.fontSize = fontSize;
        t.enableAutoSizing = false;
        t.color = color;
        t.alignment = align;
        t.characterSpacing = spacing;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static GameObject NewCanvasRoot(string name, int sortOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return go;
    }

    static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field) ?? throw new Exception($"field {field} not found on {target}");
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>ImageMenuView._items を設定する。</summary>
    static void SetMenuItems(ImageMenuView menu, GameObject root, List<(RectTransform rt, Image img, TMP_Text label)> items)
    {
        var so = new SerializedObject(menu);
        so.FindProperty("_root").objectReferenceValue = root;
        var arr = so.FindProperty("_items");
        arr.arraySize = items.Count;
        for (var i = 0; i < items.Count; i++)
        {
            var e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("Root").objectReferenceValue = items[i].rt;
            e.FindPropertyRelative("Frame").objectReferenceValue = items[i].img;
            e.FindPropertyRelative("Label").objectReferenceValue = items[i].label;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>画像ボタン (枠 + 文字)。labelOffset は完成形の文字 (字面) の中心のボタン中心からのずれ。</summary>
    static (RectTransform, Image, TMP_Text) Button(string name, Transform parent, string sprite, Vector2 pos,
        string label, Vector2 labelOffset, float fontSize)
    {
        var img = Img(name, parent, sprite, pos);
        var t = Txt("Label", img.transform, label, labelOffset, new Vector2(img.rectTransform.sizeDelta.x - 40, 80),
            fontSize, Orange, TextAlignmentOptions.Midline, LabelSpacing);
        return (img.rectTransform, img, t);
    }

    static void ClearChildren(Transform t)
    {
        for (var i = t.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    #endregion

    #region SaveSlotUI

    /// <summary>セーブスロット画面のプレハブ (SaveSlotUI.prefab) と UI シーン (SaveSlotUI.unity) を作る。</summary>
    public static string SaveSlot()
    {
        var root = NewCanvasRoot("SaveSlotUI", 20); // ポーズ (10)・ゲームオーバー (11) より上
        BuildSaveSlotContents(root);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, SaveSlotPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        // UI シーン (ゲーム中に Additive で重ねる)
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PrefabUtility.InstantiatePrefab(prefab, scene);
        EditorSceneManager.SaveScene(scene, SaveSlotScenePath);
        AddToBuildSettings(SaveSlotScenePath);

        // PlayerScene の UISceneBootstrap に SaveSlotUI を足す
        var player = EditorSceneManager.OpenScene(PlayerScenePath, OpenSceneMode.Single);
        var log = "";
        foreach (var boot in player.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UISceneBootstrap>(true)))
        {
            var so = new SerializedObject(boot);
            var arr = so.FindProperty("_uiSceneNames");
            var names = Enumerable.Range(0, arr.arraySize).Select(i => arr.GetArrayElementAtIndex(i).stringValue).ToList();
            if (!names.Contains("SaveSlotUI"))
            {
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).stringValue = "SaveSlotUI";
                so.ApplyModifiedPropertiesWithoutUndo();
                log += "PlayerScene bootstrap += SaveSlotUI\n";
            }
        }

        EditorSceneManager.SaveScene(player);
        return "SaveSlotUI built\n" + log;
    }

    static void BuildSaveSlotContents(GameObject root)
    {
        var view = root.AddComponent<SaveSlotView>();
        var screen = Stretch("Root", root.transform);

        var bg = FullImg("Background", screen, "_gen/background_pause.png"); // ゲーム中に開いた時だけ (ポーズと同じ暗さ)
        Img("Paper", screen, "_gen/paper_menu.png", new Vector2(18f, -200.5f)); // 紙目・暗さ入り (gen_paper.py)
        Img("Frame", screen, "load/load_frame.png", new Vector2(0f, -47f));

        // 記録 (はじめから/セーブ) の時は枠の絵の見出しを枠の地の色で隠して差し替える
        var cover = Rect("HeadingCover", screen, new Vector2(0f, 252f), new Vector2(620f, 58f));
        AddImage(cover.gameObject, null).color = Maroon;
        var heading = Txt("HeadingText", screen, "どのデータに記録しますか?", new Vector2(-0.5f, 250f), new Vector2(760f, 60f),
            HeadingFont, Orange, TextAlignmentOptions.Midline, HeadingSpacing);

        var so = new SerializedObject(view);
        so.FindProperty("_root").objectReferenceValue = screen.gameObject;
        so.FindProperty("_background").objectReferenceValue = bg.gameObject;
        so.FindProperty("_headingCover").objectReferenceValue = cover.gameObject;
        so.FindProperty("_headingText").objectReferenceValue = heading;

        float[] slotY = { 74f, -83f, -240f };
        var slots = so.FindProperty("_slots");
        slots.arraySize = slotY.Length;
        for (var i = 0; i < slotY.Length; i++)
        {
            // スロット内の座標はスロット中心からの相対 (完成形の字面から実測)
            var frame = Img($"Slot{i + 1:00}", screen, "load/load_inframe.png", new Vector2(0f, slotY[i]));
            var t = frame.transform;
            var number = Txt("Number", t, $"{i + 1:00}", new Vector2(-291f, -3f), new Vector2(90f, 60f), SlotNumberFont, Maroon, TextAlignmentOptions.Midline);
            var chapter = Txt("Chapter", t, "第1章", new Vector2(-200f + 100f, -1f), new Vector2(200f, 50f), SlotChapterFont, Maroon, TextAlignmentOptions.MidlineLeft);
            var place = Txt("Place", t, "場所", new Vector2(33f + 92.5f, -3f), new Vector2(185f, 40f), SlotPlaceFont, Maroon, TextAlignmentOptions.MidlineLeft);
            var date = Txt("Date", t, "2000/01/01", new Vector2(325f - 80f, 31f), new Vector2(160f, 24f), SlotDateFont, Maroon, TextAlignmentOptions.MidlineRight);
            var time = Txt("PlayTime", t, "00:00:00", new Vector2(326f - 80f, -4f), new Vector2(160f, 40f), SlotTimeFont, Maroon, TextAlignmentOptions.MidlineRight);
            var empty = Txt("Empty", t, "データなし", new Vector2(-200f + 100f, -1f), new Vector2(200f, 50f), SlotChapterFont, Maroon, TextAlignmentOptions.MidlineLeft);
            empty.gameObject.SetActive(false);
            // 章「第1章 鳴りやまぬ遊園地」は 2 行 (SaveSlotView.FormatChapter) にして枠に収まる大きさまで縮める
            chapter.rectTransform.sizeDelta = new Vector2(200f, 66f);
            chapter.enableAutoSizing = true;
            chapter.fontSizeMin = 14f;
            chapter.fontSizeMax = SlotChapterFont;
            chapter.lineSpacing = -12f;
            chapter.overflowMode = TextOverflowModes.Ellipsis;
            // 場所: 長い名前は縮め、それでも入らなければ … で切る (枠からはみ出さない)
            place.enableAutoSizing = true;
            place.fontSizeMin = 14f;
            place.fontSizeMax = SlotPlaceFont;
            place.overflowMode = TextOverflowModes.Ellipsis;

            var e = slots.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("Root").objectReferenceValue = frame.rectTransform;
            e.FindPropertyRelative("Frame").objectReferenceValue = frame;
            e.FindPropertyRelative("Number").objectReferenceValue = number;
            e.FindPropertyRelative("Chapter").objectReferenceValue = chapter;
            e.FindPropertyRelative("PlaceIcon").objectReferenceValue = null;
            e.FindPropertyRelative("Place").objectReferenceValue = place;
            e.FindPropertyRelative("Date").objectReferenceValue = date;
            e.FindPropertyRelative("PlayTime").objectReferenceValue = time;
            e.FindPropertyRelative("Empty").objectReferenceValue = empty;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        Img("DownArrow", screen, "load/load_inframearrow.png", new Vector2(-6.5f, -350f));
        Img("BackArrow", screen, "load/load_arrow.png", new Vector2(-375f, -476f));
        screen.gameObject.SetActive(false);
    }

    #endregion

    #region Pause

    /// <summary>PauseUI.prefab の中身を完成形のポーズ画面に作り直す (ルートの Canvas / PauseMenuView / スコープはそのまま)。</summary>
    public static string Pause()
    {
        var root = PrefabUtility.LoadPrefabContents(PausePrefabPath);
        try
        {
            var old = root.transform.Find("PauseMenu");
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            var host = Stretch("PauseMenu", root.transform);
            var menu = host.gameObject.AddComponent<ImageMenuView>();
            var screen = Stretch("Root", host);

            FullImg("Background", screen, "_gen/background_pause.png"); // all_background + 周辺の暗さ (gen_paper.py)
            Img("Paper", screen, "_gen/paper_pause.png", new Vector2(18.5f, -235.5f)); // 紙目・暗さ入り
            Img("Sign", screen, "pause/pause.png", new Vector2(-2f, 347f));

            float[] y = { 119.5f, -3.5f, -125.5f, -248.5f, -371.5f };
            string[] labels = { "再開", "セーブ", "設定", "メインメニューに戻る", "ゲームを終了" };
            Vector2[] offsets = { new(-0.5f, 3f), new(1.5f, 4.5f), new(-0.5f, 3.5f), new(0f, 4f), new(1.5f, 4.5f) };
            var items = new List<(RectTransform, Image, TMP_Text)>();
            for (var i = 0; i < y.Length; i++)
                items.Add(Button($"Button{i}", screen, "pause/pause_button.png", new Vector2(-0.5f, y[i]), labels[i], offsets[i], PauseButtonFont));

            Img("BackArrow", screen, "pause/pause_arrow.png", new Vector2(-295f, -476f));
            SetMenuItems(menu, screen.gameObject, items);
            screen.gameObject.SetActive(false);

            SetRef(root.GetComponent<PauseMenuView>(), "_menu", menu);

            // 「メインメニューに戻る」の確認 (専用の絵が無いので既存の部品で組んだ仮: 暗幕 shushu_black +
            // 文字の無い看板 pause_frame + ボタン pause_button ×2)
            var oldConfirm = root.transform.Find("ConfirmMenu");
            if (oldConfirm != null)
                UnityEngine.Object.DestroyImmediate(oldConfirm.gameObject);
            var confirmHost = Stretch("ConfirmMenu", root.transform);
            var confirm = confirmHost.gameObject.AddComponent<ImageMenuView>();
            var cRoot = Stretch("Root", confirmHost);
            Dim(cRoot);
            var sign = Img("Sign", cRoot, "pause/pause_frame.png", new Vector2(0f, 150f));
            Txt("Question", sign.transform, "メインメニューに戻りますか?", new Vector2(0f, 0f), new Vector2(520f, 80f),
                PauseButtonFont, Orange, TextAlignmentOptions.Midline, LabelSpacing);
            var cItems = new List<(RectTransform, Image, TMP_Text)>
            {
                Button("Button0", cRoot, "pause/pause_button.png", new Vector2(0f, -60f), "はい", new Vector2(0f, 4f), PauseButtonFont),
                Button("Button1", cRoot, "pause/pause_button.png", new Vector2(0f, -183f), "いいえ", new Vector2(0f, 4f), PauseButtonFont),
            };
            SetMenuItems(confirm, cRoot.gameObject, cItems);
            cRoot.gameObject.SetActive(false);
            SetRef(root.GetComponent<PauseMenuView>(), "_confirm", confirm);
            PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return "PauseUI rebuilt";
    }

    #endregion

    #region Title

    /// <summary>TitleScene の Canvas を作り直す (画面 1 / 画面 2 / スロット画面)。</summary>
    public static string Title()
    {
        var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        foreach (var g in scene.GetRootGameObjects())
        {
            if (g.name == "TitleCanvas" || g.name == "SaveSlotUI")
                UnityEngine.Object.DestroyImmediate(g);
        }

        var title = scene.GetRootGameObjects().Select(g => g.GetComponent<TitleScreen>()).First(c => c != null);

        var canvas = NewCanvasRoot("TitleCanvas", 0);
        SceneManager.MoveGameObjectToScene(canvas, scene);

        // 画面 (1): 背景は完成形 (ロゴ・PRESS ANY BUTTON・光の枠が焼き込み) をそのまま仮に使う。
        // 部品 (枠・ロゴ・メッセージ) は本番の背景が来た時のために位置を合わせて非表示で置いておく
        var press = Stretch("PressAnyScreen", canvas.transform);
        FullImg("Background", press, "_temp/title_bg_temp.png");
        var parts = Stretch("FinalArtParts", press);
        Img("StartBackground", parts, "start/start_background.png", Vector2.zero);
        Img("Logo", parts, "start/start_logo.png", new Vector2(-0.5f, 112f));
        Img("Message", parts, "start/start_message.png", new Vector2(3f, -314.5f));
        parts.gameObject.SetActive(false);

        // 画面 (2) とスロット画面の背景: 完成形 (start2) の背景 (中央は紙で隠れる)
        var menuBg = FullImg("MenuBackground", canvas.transform, "_temp/menu_bg_temp.png");
        menuBg.gameObject.SetActive(false);

        // 画面 (2): 紙 + ボタン 4 つ + 戻る矢印
        var host = Stretch("TitleMenu", canvas.transform);
        var menu = host.gameObject.AddComponent<ImageMenuView>();
        var screen = Stretch("Root", host);
        Img("Paper", screen, "_gen/paper_menu.png", new Vector2(18f, -200.5f)); // 紙目・暗さ入り (gen_paper.py)
        float[] y = { 225.5f, 46.5f, -145.5f, -337.5f };
        string[] labels = { "はじめから", "つづきから", "設定", "トロフィー" };
        Vector2[] offsets = { new(0f, 2f), new(0f, 3f), new(2f, 4f), new(4f, 3f) }; // 「はじめから」は中央揃え (2回目の決定 10)
        var items = new List<(RectTransform, Image, TMP_Text)>();
        for (var i = 0; i < y.Length; i++)
            items.Add(Button($"Button{i}", screen, "start2/start2_frame.png", new Vector2(0f, y[i]), labels[i], offsets[i], TitleButtonFont));
        Img("BackArrow", screen, "start2/start2_arrow.png", new Vector2(-374f, -476f));
        SetMenuItems(menu, screen.gameObject, items);
        screen.gameObject.SetActive(false);

        // スロット画面 (SaveSlotUI プレハブ)
        var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SaveSlotPrefabPath);
        var slots = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, scene);

        var so = new SerializedObject(title);
        so.FindProperty("_pressAnyScreen").objectReferenceValue = press.gameObject;
        so.FindProperty("_menuBackground").objectReferenceValue = menuBg.gameObject;
        so.FindProperty("_menu").objectReferenceValue = menu;
        so.FindProperty("_slots").objectReferenceValue = slots.GetComponent<SaveSlotView>();
        so.FindProperty("_startSceneName").stringValue = "A1_T1"; // 2回目の決定 14
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "TitleScene built";
    }

    #endregion

    #region ItemMenuUI

    const string ItemPrefabPath = "Assets/Prefabs/ItemMenuUI.prefab";
    const string ItemScenePath = "Assets/Scenes/UI/ItemMenuUI.unity";
    const string CatalogPath = "Assets/Items/CollectionCatalog.asset";
    const string StageNameCatalogPath = "Assets/Resources/StageNameCatalog.asset";
    const string FallbackFontPath = "Assets/Art/Fonts/JapaneseUI SDF.asset";
    const string PropsPath = "Assets/LocalData/StageAsset/Area1Gen/props/";
    // Drive「プロップス素材」(2026-10-03 PendingChanges 14)。ステージ用の元絵 (Props/) と UI 用に縮めたアイコン (UI/icons/, import_ui.cs が設定)
    const string HudPrefabPath = "Assets/Prefabs/HUD.prefab";
    public static float CollectionCountFont = 30f;  // 収集品: 枠の右下の数 (コインの枚数)
    public static float ThreadIconSize = 64f;       // 装備品: 右上の糸の帯の糸巻きの絵
    public static float HudThreadIconSize = 36f;    // HUD: 糸の所持数の左の糸巻きの絵

    // ---- アイテム画面の文字 (完成形に文字の見本が無い所は枠に収まる大きさ。仮) ----
    public static float ItemNameFont = 40f;       // 装備品: 左下の帯 (名前)
    public static float ItemDetailFont = 30f;     // 装備品: 左の大枠 (説明)
    public static float ItemThreadFont = 44f;     // 装備品: 右上の帯 (糸)
    public static float ItemHeadingFont = 34f;    // 装備品: 中央の見出し「ハサミ強化」
    public static float ItemRowFont = 30f;        // 装備品: 中央の 3 行
    public static float ItemCaptionFont = 28f;    // 装備品: 小枠の見出し (□ 攻撃 など)
    public static float ItemSlotFont = 32f;       // 装備品: 小枠の名前
    public static float ItemPickerFont = 28f;     // 装備品: アイテムの選び直しの行
    public static int PickerRows = 7;             // 装備品: 選び直しの行数 (アイテム 6 種 + はずす。超えたら送る)
    public static float PickerRowPitch = 70f;     // 装備品: 選び直しの行の間隔
    // 選び直しの一覧の板 (仮素材 _temp/picker_*.png, 9 スライス)。中央の縦長の枠 (幅 312、両端の縫い目は x = ±126) の内側に収める
    public static float PickerWidth = 236f;       // 板の幅 (見出し・行とも)
    public static float PickerRowHeight = 54f;    // 行の板の高さ
    public static float PickerHeaderHeight = 56f; // 見出しの板の高さ
    public static float PickerPlateScale = 2f;    // 板の縁の縮め (Image.pixelsPerUnitMultiplier。素材 560 幅の縁を半分の太さで描く)
    public static float PickerIconSize = 40f;     // 行のアイコン
    public static float CollectionCellFont = 26f; // 収集品: 枠の名前
    public static float CollectionNameFont = 44f; // 収集品詳細: 名前
    public static float CollectionTextFont = 30f; // 収集品詳細: 説明
    public static float JournalRowFont = 36f;     // 資料: 一覧のタイトル
    public static float JournalTitleFont = 74f;   // 資料詳細: タイトル (完成形の「タイトル」の字の大きさに合わせた)
    public static float JournalBodyFont = 39f;    // 資料詳細: 本文 (完成形の見本文と字の高さ・幅・1 行 28 字が一致)
    public static float JournalBodyScrollY = 20f;     // 資料詳細: 本文のスクロールの目印の高さ (一覧の目印と重ならない位置。仮)
    public static float JournalBodyLineSpacing = 141f; // 資料詳細: 本文の行間 (完成形の行の間隔 93.5px に合わせた)

    /// <summary>
    /// 暗幕 (shushu_black = 不透明度 80% の黒) の代わり。このプロジェクトは Linear 色空間で UI を線形空間で重ねるため、
    /// 素材の 80% のままだと完成形 (sRGB で重ねた暗さ) より明るくなる。完成形の暗くなり方 (背景 54〜63 → 14、紙 157 → 55) に合う
    /// 不透明度 0.89 の黒を置く (sRGB の暗い所は線形に近いので一定の換算式では合わない。実測で合わせた)。
    /// </summary>
    public static float DimAlpha = 0.89f;

    static Image Dim(Transform parent)
    {
        var img = AddImage(Stretch("Dim", parent).gameObject, null);
        img.color = new Color(0f, 0f, 0f, DimAlpha);
        return img;
    }

    static TextMeshProUGUI WrapTxt(string name, Transform parent, string text, Vector2 pos, Vector2 size, float fontSize,
        Color color, TextAlignmentOptions align, float minSize = 0f)
    {
        var t = Txt(name, parent, text, pos, size, fontSize, color, align);
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Ellipsis;
        if (minSize > 0f)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = minSize;
            t.fontSizeMax = fontSize;
        }

        return t;
    }

    static void SetCell(SerializedProperty e, RectTransform root, Image icon, TMP_Text label)
    {
        e.FindPropertyRelative("Root").objectReferenceValue = root;
        e.FindPropertyRelative("Icon").objectReferenceValue = icon;
        e.FindPropertyRelative("Label").objectReferenceValue = label;
    }

    /// <summary>9 スライスの板 (素材に Border が要る = import_ui.cs)。縁は PickerPlateScale 分の 1 の太さで描く。</summary>
    static Image Sliced(string name, Transform parent, string sprite, Vector2 pos, Vector2 size)
    {
        var s = S(sprite);
        if (s.border == Vector4.zero) throw new Exception("sprite has no 9-slice border (run import_ui.cs): " + sprite);
        var img = AddImage(Rect(name, parent, pos, size).gameObject, s);
        img.type = Image.Type.Sliced;
        img.fillCenter = true;
        img.pixelsPerUnitMultiplier = PickerPlateScale;
        return img;
    }

    /// <summary>
    /// HUD (HUD.prefab) の糸の所持数の左に糸巻きの絵 (icons/icon_thread.png) を置く (PendingChanges 14)。何度流してもよい。
    /// 文字は ThreadCountPresenter が「x所持数」で出す (「糸」の字は絵に置き換え)。HUD の他の部分はレシピが無いのでここでは触らない。
    /// </summary>
    public static string HudThreadIcon()
    {
        var root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            var view = root.GetComponentInChildren<ThreadCountView>(true) ?? throw new Exception("ThreadCountView missing in HUD.prefab");
            var label = new SerializedObject(view).FindProperty("_label").objectReferenceValue as TMP_Text
                        ?? throw new Exception("ThreadCountView._label missing");
            var parent = view.transform;
            var old = parent.Find("ThreadIcon");
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            // 文字の左端 (枠の左) から少し左に絵。文字は左揃えのままなら絵のぶん右へずらす
            var lrt = label.rectTransform;
            var rt = Rect("ThreadIcon", parent, Vector2.zero, Vector2.one * HudThreadIconSize);
            rt.anchorMin = rt.anchorMax = lrt.anchorMin;
            rt.pivot = new Vector2(0f, 0.5f);
            var corners = new Vector3[4];
            lrt.GetLocalCorners(corners);
            var left = lrt.localPosition.x + corners[0].x;
            var midY = lrt.localPosition.y + (corners[0].y + corners[1].y) * 0.5f;
            rt.localPosition = new Vector3(left, midY, 0f);
            var img = AddImage(rt.gameObject, S("icons/icon_thread.png"));
            img.preserveAspect = true;
            if (label.alignment is TextAlignmentOptions.Left or TextAlignmentOptions.MidlineLeft or TextAlignmentOptions.TopLeft
                or TextAlignmentOptions.BottomLeft or TextAlignmentOptions.BaselineLeft or TextAlignmentOptions.CaplineLeft)
            {
                var m = label.margin;
                m.x = HudThreadIconSize + 6f;
                label.margin = m;
            }
            else
            {
                // 中央揃え・右揃えなら文字の位置はそのまま、絵を枠の左の外へ
                rt.localPosition = new Vector3(left - HudThreadIconSize - 4f, midY, 0f);
            }

            label.text = "x0";
            PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            return $"HUD ThreadIcon at {rt.localPosition} (label align {label.alignment}, size {lrt.rect.size})";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    const string ShopPrefabPath = "Assets/Prefabs/ShopUI.prefab";
    public static float ShopIconSize = 40f;   // ショップ: 行の品物の絵 (行の高さ 44)
    public static float ShopIconX = 40f;      // 行の左端から絵の左端まで (カーソル「▶ 」のぶん)
    public static float ShopLabelX = 92f;     // 絵がある時の文字の書き出し (MenuPanelView._iconLabelX, TMP の <pos>)

    // ショップの説明文 (Body) と品物の行 (Rows) の位置 (窓 680x560 の上端からの距離, PendingChanges 26)。
    // 以前は Body が上から 92 (26pt 2 行 ≈ 165 まで) で Rows が 100 から始まり、1 行目に重なっていた。
    public static float ShopBodyTop = 80f;     // 説明文の上端 (タイトル 24..84 の文字の下)
    public static float ShopBodyFont = 24f;    // 説明文の文字 (26 → 24。2 行で ≈ 144 まで)
    public static float ShopRowsTop = 160f;    // 1 行目の上端 (100 → 160)
    public static float ShopRowsBottom = 30f;  // 行の範囲の下端の余白 (以前のまま)

    /// <summary>
    /// ショップの説明文 (「糸を消費してアイテムを購入する / 所持している糸: n」) と 1 行目の品物が重ならないように、
    /// 説明文を少し上・小さくし、行の範囲を下げる (窓の大きさ・行の間隔 48 は変えない)。何度流してもよい。
    /// </summary>
    public static string ShopLayout()
    {
        var root = PrefabUtility.LoadPrefabContents(ShopPrefabPath);
        try
        {
            var window = root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Window") ?? throw new Exception("Window missing");
            var body = window.Find("Body") as RectTransform ?? throw new Exception("Body missing");
            var rows = window.Find("Rows") as RectTransform ?? throw new Exception("Rows missing");
            body.anchoredPosition = new Vector2(body.anchoredPosition.x, -ShopBodyTop);
            body.GetComponent<TMP_Text>().fontSize = ShopBodyFont;
            // Rows は窓いっぱいに伸ばした矩形: 上の余白 = -size.y/2 - pos.y, 下の余白 = -size.y/2 + pos.y
            rows.sizeDelta = new Vector2(rows.sizeDelta.x, -(ShopRowsTop + ShopRowsBottom));
            rows.anchoredPosition = new Vector2(rows.anchoredPosition.x, (ShopRowsBottom - ShopRowsTop) * 0.5f);
            PrefabUtility.SaveAsPrefabAsset(root, ShopPrefabPath);
            return $"ShopUI: Body top {ShopBodyTop} font {ShopBodyFont}, Rows top {ShopRowsTop} (pos {rows.anchoredPosition}, size {rows.sizeDelta})";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// ショップ (ShopUI.prefab) の各行の左に品物の絵 (ItemDefinition のアイコン) を置く (PendingChanges 23)。何度流してもよい。
    /// 見た目の素材が来るまでの最小限: 既存の行 (Row0..9) に子 Icon (Image) を足して MenuPanelView._rowIcons に繋ぐだけ。
    /// 行の文字は MenuPanelView が「▶ <pos=92>名前  糸n  (所持 a/b)」で出す (絵が無い「やめる」も同じ位置から)。
    /// </summary>
    public static string ShopIcons()
    {
        var root = PrefabUtility.LoadPrefabContents(ShopPrefabPath);
        try
        {
            var menu = root.GetComponentInChildren<MenuPanelView>(true) ?? throw new Exception("MenuPanelView missing in ShopUI.prefab");
            var so = new SerializedObject(menu);
            var rowsProp = so.FindProperty("_rows");
            var icons = new List<Image>();
            for (var i = 0; i < rowsProp.arraySize; i++)
            {
                var row = rowsProp.GetArrayElementAtIndex(i).objectReferenceValue as TMP_Text ?? throw new Exception("row missing " + i);
                var old = row.transform.Find("Icon");
                if (old != null)
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                var rt = Rect("Icon", row.transform, Vector2.zero, Vector2.one * ShopIconSize);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(ShopIconX, 0f);
                var img = AddImage(rt.gameObject, null);
                img.preserveAspect = true;
                rt.gameObject.SetActive(false);
                icons.Add(img);
            }
            var iconsProp = so.FindProperty("_rowIcons");
            iconsProp.arraySize = icons.Count;
            for (var i = 0; i < icons.Count; i++)
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
            so.FindProperty("_iconLabelX").floatValue = ShopLabelX;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ShopPrefabPath);
            return $"ShopUI: {icons.Count} row icons ({ShopIconSize}px at x {ShopIconX}, label x {ShopLabelX})";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// 消耗品アイテム (Assets/Items/*.asset の ItemDefinition) のアイコン。専用の絵が来るまで image-gen の仮素材
    /// (_temp/icon_&lt;id&gt;.png = 通常、icon_&lt;id&gt;_gray.png = 所持数 0) を入れる (UIAssignment.md「4回目の決定」3)。
    /// </summary>
    public static string ItemIcons()
    {
        var map = new (string asset, string id)[]
        {
            ("MachiNeedle", "machi_needle"), ("MishinNeedle", "mishin_needle"), ("ClothCutter", "cloth_cutter"),
            ("ThreadBall", "thread_ball"), ("BobbinBomb", "bobbin_bomb"), ("PinCushion", "pin_cushion"),
        };
        // ミシン針・まち針・布カッターは Drive の本素材 (icons/: Props の絵を 256 に縮めた物と、灰色 = 輝度 ×0.58。PendingChanges 14)。
        // 残り 3 種は仮素材 (_temp/) のまま
        var real = new HashSet<string> { "mishin_needle", "machi_needle", "cloth_cutter" };
        var log = "";
        foreach (var (asset, id) in map)
        {
            var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/Items/{asset}.asset") ?? throw new Exception("item missing: " + asset);
            var so = new SerializedObject(def);
            var dir = real.Contains(id) ? "icons" : "_temp";
            so.FindProperty("_icon").objectReferenceValue = S($"{dir}/icon_{id}.png");
            so.FindProperty("_iconEmpty").objectReferenceValue = S($"{dir}/icon_{id}_gray.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            log += $"{asset} ({def.DisplayName}) <- {dir}/icon_{id}\n";
        }

        AssetDatabase.SaveAssets();
        return log;
    }

    /// <summary>絵の置き場 (Icon)。素材が無い間は非表示 (ItemMenuTab.SetIcon が切り替える)。</summary>
    static Image IconSlot(Transform parent, Vector2 pos, Vector2 size)
    {
        var rt = Rect("Icon", parent, pos, size);
        var img = AddImage(rt.gameObject, null);
        img.preserveAspect = true;
        rt.gameObject.SetActive(false);
        return img;
    }

    /// <summary>
    /// 収集品・資料の一覧データ (CollectionCatalog.asset)。名前は各ピックアップ (KeyItemPickup._itemName) と同じ。
    /// 説明・本文は未作成なので TODO の仮文 (UIImplementation.md の質問を参照)。
    /// </summary>
    /// <summary>
    /// コインの説明 (仮文, PendingChanges 21: 世界観に合わせ、呼びかけ調にしない。交換などの仕組みは書かない)。
    /// Catalog を流し直してもこの文が入る (アセットを手で直すと次の Catalog で戻るので、変える時はここを直す)。
    /// </summary>
    public const string CoinDescription =
        "すり減った古い園内コイン。表には交差した鋏、裏には糸巻きが刻まれている。" +
        "とうに使われなくなったはずなのに、これを今も大事に集めている誰かがいるらしい。";

    public static string Catalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CollectionCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CollectionCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        const string todoDesc = "TODO: 説明文は未作成";
        const string todoBody = "TODO: 本文は未作成";
        // 絵: ステージの拾い物の絵を仮に使う (UIAssignment.md「3回目の決定」2)。秘伝書 (A1_Skill_SpinSlash) は 2026-10-03 に項目ごと削除 (屋根裏のご褒美を糸の大束に変更)
        var heartPin = AssetDatabase.LoadAssetAtPath<Sprite>(PropsPath + "heart_pin.png") ?? throw new Exception("heart_pin.png missing");
        var spool = AssetDatabase.LoadAssetAtPath<Sprite>(PropsPath + "thread_spool.png") ?? throw new Exception("thread_spool.png missing");
        // コイン (PendingChanges 16): 数えて持つ物。Flag は無く、枚数 1 以上で入手済み。絵は Drive の本素材 (Props/coin.png を 512 に縮めた icons/icon_coin.png)
        var coin = S("icons/icon_coin.png");
        CollectionCatalog.Entry E(string flag, string name, string desc, Sprite icon = null) =>
            new() { Flag = flag, Name = name, Description = desc, Icon = icon };
        catalog.SetEntries(
            new[]
            {
                E("A1_HeartPin1", "ハートピン1", todoDesc, heartPin),
                E("A1_HeartPin2", "ハートピン2", todoDesc, heartPin),
                E("A1_HeartPin3", "ハートピン3", todoDesc, heartPin),
                E("A1_HeartPin4", "ハートピン4", todoDesc, heartPin),
                E("A1_Spool1", "糸巻き1", todoDesc, spool),
                E("A1_Spool2", "糸巻き2", todoDesc, spool),
                E("A1_Spool3", "糸巻き3", todoDesc, spool),
                new() { Flag = "", Name = "コイン", Description = CoinDescription, Icon = coin, Counter = CollectionCounter.Coins },
            },
            new[]
            {
                E("A1_Lore_Journal1", "日誌「閉園しなかった夜」その1", todoBody),
                E("A1_Lore_Journal2", "日誌「閉園しなかった夜」その2", todoBody),
                E("A1_Lore_Journal3", "日誌「閉園しなかった夜」その3", todoBody),
                E("A1_Lore_Journal4", "日誌「閉園しなかった夜」その4 (噴水の碑文)", todoBody),
                E("A1_Lore_Journal5", "日誌「閉園しなかった夜」その5 (整備員の手記)", todoBody),
            });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return "CollectionCatalog: 8 collectibles (coin = count), 5 journals";
    }

    /// <summary>
    /// 旧形式のセーブ (場所・章が空) を表示するための対応表 Resources/StageNameCatalog.asset を
    /// Build Settings のシーンファイルの部屋名 (_areaName) から作り直す。章: A{n}_ はその章、旧ステージ (GameField_old) は第1章。
    /// </summary>
    public static string StageNames()
    {
        var entries = new List<StageNameCatalog.Entry>();
        var re = new System.Text.RegularExpressions.Regex("_areaName: (\"(?:[^\"\\\\]|\\\\.)*\"|.*)");
        foreach (var s in EditorBuildSettings.scenes)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(s.path);
            if (name.EndsWith("UI") || !System.IO.File.Exists(s.path))
                continue;

            var m = re.Match(System.IO.File.ReadAllText(s.path));
            if (!m.Success)
                continue;

            var raw = m.Groups[1].Value.Trim();
            var place = raw.StartsWith("\"") ? System.Text.RegularExpressions.Regex.Unescape(raw.Substring(1, raw.Length - 2)) : raw;
            var chapter = StageInfo.ChapterFromSceneName(name);
            if (string.IsNullOrEmpty(chapter) && s.path.Contains("/GameField_old/"))
                chapter = StageInfo.ChapterTitle(1); // 旧ステージ (遊園地) は第1章とみなす (仮)
            entries.Add(new StageNameCatalog.Entry { SceneName = name, PlaceName = place, ChapterName = chapter });
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        var catalog = AssetDatabase.LoadAssetAtPath<StageNameCatalog>(StageNameCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<StageNameCatalog>();
            AssetDatabase.CreateAsset(catalog, StageNameCatalogPath);
        }

        catalog.SetEntries(entries);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return $"StageNameCatalog: {entries.Count} scenes\n" + string.Join("\n", entries.Select(e => $"{e.SceneName} = {e.PlaceName} / {e.ChapterName}"));
    }

    /// <summary>
    /// 丸数字 (ハートピン①など) のために YDW フォントへ足していた JapaneseUI SDF のフォールバックを外す
    /// (名前を「ハートピン1」表記にして不要になった。UIAssignment.md「3回目の決定」9)。何度流してもよい。
    /// </summary>
    static string RemoveFontFallback()
    {
        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
        var font = Font;
        if (fallback == null || font == null || font.fallbackFontAssetTable == null || !font.fallbackFontAssetTable.Contains(fallback))
            return "";

        font.fallbackFontAssetTable.Remove(fallback);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        return "YDW font fallback -= JapaneseUI SDF\n";
    }

    /// <summary>
    /// アイテム画面のプレハブ (ItemMenuUI.prefab) と UI シーン (ItemMenuUI.unity) を作る。
    /// 座標は完成形 item/*/装備_*.png を 1920x1080 に縮めて部品を照合した実測値。先に Catalog を流しておくこと。
    /// </summary>
    public static string ItemMenu()
    {
        var log = RemoveFontFallback();
        var catalog = AssetDatabase.LoadAssetAtPath<CollectionCatalog>(CatalogPath) ?? throw new Exception("run BuildUI.Catalog first");

        var root = NewCanvasRoot("ItemMenuUI", 9); // ポーズ (10) の下、ショップ (6) の上
        var view = root.AddComponent<ItemMenuView>();
        var screen = Stretch("Root", root.transform);
        FullImg("Background", screen, "item/all_background.png"); // 完成形の背景と一致 (暗さの追加なし)

        var tabsHost = Stretch("Tabs", screen);
        var equip = BuildEquipTab(tabsHost);
        var collection = BuildCollectionTab(tabsHost, catalog);
        var journal = BuildJournalTab(tabsHost, catalog);

        // 見出し (詳細の暗幕より上に描くので最後に置く)
        var header = Img("Header", screen, "item/soubi/soubi_select.png", new Vector2(566.5f, 459f));
        var ticket = Img("TitleTicket", screen, "item/soubi/soubi_title.png", new Vector2(-660f, 389.5f));

        var so = new SerializedObject(view);
        so.FindProperty("_root").objectReferenceValue = screen.gameObject;
        so.FindProperty("_header").objectReferenceValue = header;
        so.FindProperty("_titleTicket").objectReferenceValue = ticket;
        void Arr(string field, params UnityEngine.Object[] values)
        {
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        Arr("_headerSprites", S("item/soubi/soubi_select.png"), S("item/shushu/shushu_select.png"), S("item/shiryou/shiryou_select.png"));
        Arr("_titleSprites", S("item/soubi/soubi_title.png"), S("item/shushu/shushu_title.png"), S("item/shiryou/shiryou_title.png"));
        Arr("_tabs", equip, collection, journal);
        var pos = so.FindProperty("_titlePositions");
        Vector2[] titlePos = { new(-660f, 389.5f), new(-661f, 389f), new(-660f, 389.5f) };
        pos.arraySize = titlePos.Length;
        for (var i = 0; i < titlePos.Length; i++)
            pos.GetArrayElementAtIndex(i).vector2Value = titlePos[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        screen.gameObject.SetActive(false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PrefabUtility.InstantiatePrefab(prefab, scene);
        EditorSceneManager.SaveScene(scene, ItemScenePath);
        AddToBuildSettings(ItemScenePath);
        log += AddToPlayerSceneBootstrap("ItemMenuUI");
        return "ItemMenuUI built\n" + log;
    }

    static string AddToPlayerSceneBootstrap(string sceneName)
    {
        var player = EditorSceneManager.OpenScene(PlayerScenePath, OpenSceneMode.Single);
        var log = "";
        foreach (var boot in player.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UISceneBootstrap>(true)))
        {
            var so = new SerializedObject(boot);
            var arr = so.FindProperty("_uiSceneNames");
            var names = Enumerable.Range(0, arr.arraySize).Select(i => arr.GetArrayElementAtIndex(i).stringValue).ToList();
            if (names.Contains(sceneName))
                continue;

            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).stringValue = sceneName;
            so.ApplyModifiedPropertiesWithoutUndo();
            log += $"PlayerScene bootstrap += {sceneName}\n";
        }

        EditorSceneManager.SaveScene(player);
        return log;
    }

    /// <summary>装備品タブ (案A)。</summary>
    static ItemEquipTab BuildEquipTab(Transform parent)
    {
        var host = Stretch("Equip", parent);
        var tab = host.gameObject.AddComponent<ItemEquipTab>();
        var root = Stretch("Root", host);

        // 左の大枠: 絵 + 説明
        var big = Img("DetailFrame", root, "item/soubi/soubi_frame1.png", new Vector2(-522f, -44.5f));
        var icon = IconSlot(big.transform, new Vector2(0f, 90f), new Vector2(240f, 240f));
        var detail = WrapTxt("DetailText", big.transform, "", Vector2.zero, new Vector2(560f, 420f), ItemDetailFont, Orange,
            TextAlignmentOptions.TopLeft, 18f);
        // 左下の帯: 名前
        var nameStrip = Img("NameFrame", root, "item/soubi/soubi_frame2.png", new Vector2(-522f, -397f));
        var name = Txt("Name", nameStrip.transform, "", new Vector2(0f, 4f), new Vector2(540f, 70f), ItemNameFont, Orange,
            TextAlignmentOptions.Midline, LabelSpacing);
        name.enableAutoSizing = true;
        name.fontSizeMin = 20f;
        name.fontSizeMax = ItemNameFont;
        name.overflowMode = TextOverflowModes.Ellipsis;
        // 右上の帯: 糸
        var threadStrip = Img("ThreadFrame", root, "item/soubi/soubi_frame2.png", new Vector2(522f, 308f));
        var thread = Txt("Thread", threadStrip.transform, "糸  0", new Vector2(0f, 4f), new Vector2(540f, 70f), ItemThreadFont, Orange,
            TextAlignmentOptions.Midline, LabelSpacing);
        // 糸巻きの絵 (PendingChanges 14)。帯の左寄り、文字 (中央揃え) と重ならない位置
        var threadIcon = AddImage(Rect("ThreadIcon", threadStrip.transform, new Vector2(-170f, 4f), Vector2.one * ThreadIconSize).gameObject,
            S("icons/icon_thread.png"));
        threadIcon.preserveAspect = true;

        // 中央の縦長の枠: ハサミ強化
        var mid = Img("UpgradeFrame", root, "item/soubi/soubi_frame3.png", new Vector2(0f, -132.5f));
        var upgradeGroup = Stretch("Upgrades", mid.transform);
        Txt("Heading", upgradeGroup, "ハサミ強化", new Vector2(0f, 250f), new Vector2(240f, 50f), ItemHeadingFont, Orange,
            TextAlignmentOptions.Midline, HeadingSpacing);
        var rows = new List<TMP_Text>();
        float[] rowY = { 130f, 30f, -70f };
        for (var i = 0; i < rowY.Length; i++)
        {
            var t = Txt($"Row{i}", upgradeGroup, "", new Vector2(0f, rowY[i]), new Vector2(230f, 60f), ItemRowFont, Orange,
                TextAlignmentOptions.Midline);
            t.enableAutoSizing = true;
            t.fontSizeMin = 18f;
            t.fontSizeMax = ItemRowFont;
            rows.Add(t);
        }

        // 同じ枠の中: アイテムスロットの選び直し (スロットの枠で決定した時だけ、ハサミ強化と入れ替えて出す)。
        // 一覧の絵は後で来るので、それまで image-gen の仮素材 (_temp/picker_header / picker_row*) を置く (UIAssignment.md「4回目の決定」2)。
        // 行 = 板 + アイコン (ItemDefinition.Icon / IconEmpty) + 「名前 x所持数」。選択中・所持数 0 の板は ItemEquipTab が差し替える
        var pickerGroup = Stretch("ItemPicker", mid.transform);
        var headingPlate = Sliced("HeadingPlate", pickerGroup, "_temp/picker_header.png", new Vector2(0f, 250f),
            new Vector2(PickerWidth, PickerHeaderHeight));
        // 見出しの板は橙なので文字は枠の中の文字色 (#490000)
        var pickerHeading = Txt("Heading", headingPlate.transform, "", new Vector2(0f, 2f), new Vector2(PickerWidth - 56f, 48f),
            ItemHeadingFont, Maroon, TextAlignmentOptions.Midline, HeadingSpacing);
        pickerHeading.enableAutoSizing = true;
        pickerHeading.fontSizeMin = 18f;
        pickerHeading.fontSizeMax = ItemHeadingFont;
        var pickerRows = new List<(RectTransform, Image, TMP_Text)>();
        var iconX = -PickerWidth / 2f + 42f;              // 選択中の板の左の ▶ (縁から 10〜25) を避ける
        var labelLeft = iconX + PickerIconSize / 2f + 8f;
        var labelRight = PickerWidth / 2f - 12f;
        for (var i = 0; i < PickerRows; i++)
        {
            var plate = Sliced($"Row{i}", pickerGroup, "_temp/picker_row.png", new Vector2(0f, 175f - i * PickerRowPitch),
                new Vector2(PickerWidth, PickerRowHeight));
            var ic = IconSlot(plate.transform, new Vector2(iconX, 0f), new Vector2(PickerIconSize, PickerIconSize));
            var t = Txt("Label", plate.transform, "", new Vector2((labelLeft + labelRight) / 2f, 2f),
                new Vector2(labelRight - labelLeft, PickerRowHeight - 8f), ItemPickerFont, Orange, TextAlignmentOptions.MidlineLeft);
            t.enableAutoSizing = true;
            t.fontSizeMin = 14f;
            t.fontSizeMax = ItemPickerFont;
            pickerRows.Add((plate.rectTransform, ic, t));
        }

        pickerGroup.gameObject.SetActive(false);

        // 右の小枠 4 つ: □ / △ / 下 / 左右
        Vector2[] cellPos = { new(348f, 40f), new(696f, 40f), new(348f, -312f), new(696f, -312f) };
        string[] captions = { "□ 攻撃", "△ 攻撃", "↓ アイテム", "← → アイテム" };
        var cells = new List<(RectTransform, Image, TMP_Text)>();
        for (var i = 0; i < cellPos.Length; i++)
        {
            var f = Img($"Slot{i}", root, "item/soubi/soubi_frame4.png", cellPos[i]);
            Txt("Caption", f.transform, captions[i], new Vector2(0f, 105f), new Vector2(250f, 50f), ItemCaptionFont, Orange,
                TextAlignmentOptions.Midline);
            var ic = IconSlot(f.transform, new Vector2(0f, 0f), new Vector2(150f, 150f));
            var label = WrapTxt("Name", f.transform, "", new Vector2(0f, -20f), new Vector2(250f, 170f), ItemSlotFont, Orange,
                TextAlignmentOptions.Center, 18f);
            cells.Add((f.rectTransform, ic, label));
        }

        var so = new SerializedObject(tab);
        so.FindProperty("_root").objectReferenceValue = root.gameObject;
        so.FindProperty("_detailIcon").objectReferenceValue = icon;
        so.FindProperty("_detailText").objectReferenceValue = detail;
        so.FindProperty("_nameText").objectReferenceValue = name;
        so.FindProperty("_threadText").objectReferenceValue = thread;
        var up = so.FindProperty("_upgradeRows");
        up.arraySize = rows.Count;
        for (var i = 0; i < rows.Count; i++)
            SetCell(up.GetArrayElementAtIndex(i), rows[i].rectTransform, null, rows[i]);
        so.FindProperty("_upgradeGroup").objectReferenceValue = upgradeGroup.gameObject;
        so.FindProperty("_pickerGroup").objectReferenceValue = pickerGroup.gameObject;
        so.FindProperty("_pickerHeading").objectReferenceValue = pickerHeading;
        var pr = so.FindProperty("_pickerRows");
        pr.arraySize = pickerRows.Count;
        for (var i = 0; i < pickerRows.Count; i++)
            SetCell(pr.GetArrayElementAtIndex(i), pickerRows[i].Item1, pickerRows[i].Item2, pickerRows[i].Item3);
        so.FindProperty("_pickerRowSprite").objectReferenceValue = S("_temp/picker_row.png");
        so.FindProperty("_pickerRowSelectedSprite").objectReferenceValue = S("_temp/picker_row_selected.png");
        so.FindProperty("_pickerRowEmptySprite").objectReferenceValue = S("_temp/picker_row_disabled.png");
        var sc = so.FindProperty("_slotCells");
        sc.arraySize = cells.Count;
        for (var i = 0; i < cells.Count; i++)
            SetCell(sc.GetArrayElementAtIndex(i), cells[i].Item1, cells[i].Item2, cells[i].Item3);
        so.ApplyModifiedPropertiesWithoutUndo();
        root.gameObject.SetActive(false);
        return tab;
    }

    /// <summary>収集品タブ (18 枠 × 3 ページ + 詳細)。</summary>
    static ItemCollectionTab BuildCollectionTab(Transform parent, CollectionCatalog catalog)
    {
        var host = Stretch("Collection", parent);
        var tab = host.gameObject.AddComponent<ItemCollectionTab>();
        var root = Stretch("Root", host);

        float[] xs = { -733f, -440f, -146.5f, 146.5f, 440f, 733f };
        float[] ys = { 120f, -122f, -365f };
        var cells = new List<(RectTransform, Image, TMP_Text)>();
        var countTexts = new List<TMP_Text>();
        for (var r = 0; r < ys.Length; r++)
        for (var c = 0; c < xs.Length; c++)
        {
            var f = Img($"Cell{r * xs.Length + c:00}", root, "item/shushu/shushu_frame.png", new Vector2(xs[c], ys[r]));
            var ic = IconSlot(f.transform, new Vector2(0f, 6f), new Vector2(180f, 170f));
            var label = WrapTxt("Name", f.transform, "", new Vector2(0f, 6f), new Vector2(200f, 170f), CollectionCellFont, Orange,
                TextAlignmentOptions.Center, 16f);
            // 数 (コインの枚数)。枠の右下
            var count = Txt("Count", f.transform, "", new Vector2(40f, -62f), new Vector2(120f, 44f), CollectionCountFont, Orange,
                TextAlignmentOptions.BottomRight);
            countTexts.Add(count);
            cells.Add((f.rectTransform, ic, label));
        }

        var dots = Img("PageDots", root, "item/shushu/shushu_page1.png", new Vector2(913f, -115f));
        Img("PageArrows", root, "item/shushu/shushu_arrow.png", new Vector2(913f, -115f));

        // 詳細: 暗幕 (一覧の上・見出しの下) + 左の絵の枠 + 右の文字の枠 + BACK / NEXT / 閉じる
        var detail = Stretch("Detail", root);
        Dim(detail);
        var itemFrame = Img("ItemFrame", detail, "item/shushu/shushu_detail/shushu_frame_item.png", new Vector2(-422.5f, -85.5f));
        var dIcon = IconSlot(itemFrame.transform, Vector2.zero, new Vector2(460f, 460f));
        var dIconName = WrapTxt("IconName", itemFrame.transform, "", Vector2.zero, new Vector2(460f, 400f), CollectionNameFont, Orange,
            TextAlignmentOptions.Center, 20f);
        var textFrame = Img("TextFrame", detail, "item/shushu/shushu_detail/shushu_frame_text.png", new Vector2(315.5f, -85f));
        var dName = Txt("Name", textFrame.transform, "", new Vector2(0f, 200f), new Vector2(660f, 70f), CollectionNameFont, Maroon,
            TextAlignmentOptions.Midline, HeadingSpacing);
        dName.enableAutoSizing = true;
        dName.fontSizeMin = 22f;
        dName.fontSizeMax = CollectionNameFont;
        var dText = WrapTxt("Text", textFrame.transform, "", new Vector2(0f, -45f), new Vector2(660f, 380f), CollectionTextFont, Maroon,
            TextAlignmentOptions.TopLeft, 18f);
        var back = Img("Back", detail, "item/shushu/shushu_detail/shushu_back.png", new Vector2(-818.5f, -108.5f));
        var next = Img("Next", detail, "item/shushu/shushu_detail/shushu_next.png", new Vector2(826.5f, -108.5f));
        Img("Close", detail, "item/shushu/shushu_detail/shushu_close.png", new Vector2(752.5f, 189f));
        detail.gameObject.SetActive(false);

        var so = new SerializedObject(tab);
        so.FindProperty("_root").objectReferenceValue = root.gameObject;
        so.FindProperty("_catalog").objectReferenceValue = catalog;
        var arr = so.FindProperty("_cells");
        arr.arraySize = cells.Count;
        for (var i = 0; i < cells.Count; i++)
        {
            SetCell(arr.GetArrayElementAtIndex(i), cells[i].Item1, cells[i].Item2, cells[i].Item3);
            arr.GetArrayElementAtIndex(i).FindPropertyRelative("Count").objectReferenceValue = countTexts[i];
        }
        so.FindProperty("_pageDots").objectReferenceValue = dots;
        var pages = so.FindProperty("_pageSprites");
        pages.arraySize = 3;
        for (var i = 0; i < 3; i++)
            pages.GetArrayElementAtIndex(i).objectReferenceValue = S($"item/shushu/shushu_page{i + 1}.png");
        so.FindProperty("_detailRoot").objectReferenceValue = detail.gameObject;
        so.FindProperty("_detailIcon").objectReferenceValue = dIcon;
        so.FindProperty("_detailIconName").objectReferenceValue = dIconName;
        so.FindProperty("_detailName").objectReferenceValue = dName;
        so.FindProperty("_detailText").objectReferenceValue = dText;
        so.FindProperty("_backArrow").objectReferenceValue = back;
        so.FindProperty("_nextArrow").objectReferenceValue = next;
        so.ApplyModifiedPropertiesWithoutUndo();
        root.gameObject.SetActive(false);
        return tab;
    }

    /// <summary>資料タブ (紙 + 日誌の枠の一覧 + スクロールの目印 + 詳細)。</summary>
    static ItemJournalTab BuildJournalTab(Transform parent, CollectionCatalog catalog)
    {
        var host = Stretch("Journal", parent);
        var tab = host.gameObject.AddComponent<ItemJournalTab>();
        var root = Stretch("Root", host);

        Img("Paper", root, "_gen/paper_journal.png", new Vector2(19f, -300.5f)); // load_paper + 紙目・暗さ (gen_paper.py)

        // 一覧: 紙の上端の少し下から画面の下端までで切り抜く (送った枠が紙の上にはみ出さない)
        var clip = Rect("ListClip", root, new Vector2(0f, -150f), new Vector2(960f, 780f));
        clip.gameObject.AddComponent<RectMask2D>();
        var content = Rect("Content", clip, new Vector2(0f, 150f), new Vector2(960f, 1080f)); // 中身の座標 = 画面の座標
        float[] rowY = { 134f, -41f, -216.5f, -392f, -567.5f };
        var rows = new List<(RectTransform, TMP_Text)>();
        for (var i = 0; i < rowY.Length; i++)
        {
            var f = Img($"Row{i}", content, "item/shiryou/shiryou_frame.png", new Vector2(0f, rowY[i]));
            var t = Txt("Title", f.transform, "", new Vector2(0f, 2f), new Vector2(740f, 80f), JournalRowFont, Maroon,
                TextAlignmentOptions.Midline, HeadingSpacing);
            t.enableAutoSizing = true;
            t.fontSizeMin = 20f;
            t.fontSizeMax = JournalRowFont;
            t.overflowMode = TextOverflowModes.Ellipsis;
            rows.Add((f.rectTransform, t));
        }

        // スクロールの目印: 矢印 (shiryou_arrow) + 溝 (橙) + つまみ (濃い赤)。溝とつまみは素材に無いので手続きの角丸 (_gen/pill.png)
        Img("ScrollArrows", root, "item/shiryou/shiryou_arrow.png", new Vector2(484f, -216.5f));
        var track = Rect("ScrollTrack", root, new Vector2(483.5f, -216f), new Vector2(10f, 72f));
        var trackImg = AddImage(track.gameObject, S("_gen/pill.png"));
        trackImg.type = Image.Type.Sliced;
        trackImg.color = Orange;
        var thumb = Rect("Thumb", track, Vector2.zero, new Vector2(10f, 30f));
        thumb.anchorMin = thumb.anchorMax = new Vector2(0.5f, 1f);
        thumb.pivot = new Vector2(0.5f, 1f);
        thumb.anchoredPosition = Vector2.zero;
        var thumbImg = AddImage(thumb.gameObject, S("_gen/pill.png"));
        thumbImg.type = Image.Type.Sliced;
        thumbImg.color = Maroon;

        // 詳細: 暗幕 (紙・一覧の上、見出しの下) + タイトル + 区切り + 本文 + 閉じる
        var detail = Stretch("Detail", root);
        Dim(detail);
        var title = Txt("Title", detail, "", new Vector2(5f, 254f), new Vector2(900f, 90f), JournalTitleFont, Color.white,
            TextAlignmentOptions.Midline, LabelSpacing);
        title.enableAutoSizing = true;
        title.fontSizeMin = 28f;
        title.fontSizeMax = JournalTitleFont;
        title.overflowMode = TextOverflowModes.Ellipsis;
        Img("Separator", detail, "item/shiryou/shiryou_detail/アセット 36.png", new Vector2(-0.5f, 178.5f));
        // 本文: 切り抜き (RectMask2D) の上端に付けて下へ伸ばす。長い時は上下で送る (UIAssignment.md「3回目の決定」5)
        var viewport = Rect("BodyViewport", detail, new Vector2(4.5f, -163.5f), new Vector2(870f, 560f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var body = WrapTxt("Body", viewport, "", Vector2.zero, new Vector2(870f, 560f), JournalBodyFont, Color.white,
            TextAlignmentOptions.TopLeft);
        body.rectTransform.anchorMin = body.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        body.rectTransform.pivot = new Vector2(0.5f, 1f);
        body.rectTransform.anchoredPosition = Vector2.zero;
        body.overflowMode = TextOverflowModes.Overflow; // 切り抜きで隠す (… にしない)
        body.lineSpacing = JournalBodyLineSpacing;
        // 本文のスクロールの目印: 一覧と同じ矢印 (shiryou_arrow) + 溝・つまみ (_gen/pill.png)。本文の右、上寄せ
        // (暗幕の下に透ける一覧の目印 (484, -216.5) と重ならない高さ)。送れない時は隠す
        var bodyScroll = Rect("BodyScroll", detail, new Vector2(484f, JournalBodyScrollY), new Vector2(18f, 167f));
        Img("Arrows", bodyScroll, "item/shiryou/shiryou_arrow.png", Vector2.zero);
        var bTrack = Rect("Track", bodyScroll, new Vector2(-0.5f, 0.5f), new Vector2(10f, 72f));
        var bTrackImg = AddImage(bTrack.gameObject, S("_gen/pill.png"));
        bTrackImg.type = Image.Type.Sliced;
        bTrackImg.color = Orange;
        var bThumb = Rect("Thumb", bTrack, Vector2.zero, new Vector2(10f, 30f));
        bThumb.anchorMin = bThumb.anchorMax = new Vector2(0.5f, 1f);
        bThumb.pivot = new Vector2(0.5f, 1f);
        bThumb.anchoredPosition = Vector2.zero;
        var bThumbImg = AddImage(bThumb.gameObject, S("_gen/pill.png"));
        bThumbImg.type = Image.Type.Sliced;
        bThumbImg.color = Maroon;
        bodyScroll.gameObject.SetActive(false);
        Img("Close", detail, "item/shiryou/shiryou_detail/アセット 35.png", new Vector2(751.5f, 354f));
        detail.gameObject.SetActive(false);

        var so = new SerializedObject(tab);
        so.FindProperty("_root").objectReferenceValue = root.gameObject;
        so.FindProperty("_catalog").objectReferenceValue = catalog;
        so.FindProperty("_content").objectReferenceValue = content;
        var arr = so.FindProperty("_rows");
        arr.arraySize = rows.Count;
        for (var i = 0; i < rows.Count; i++)
            SetCell(arr.GetArrayElementAtIndex(i), rows[i].Item1, null, rows[i].Item2);
        so.FindProperty("_scrollThumb").objectReferenceValue = thumb;
        so.FindProperty("_scrollTrack").objectReferenceValue = track;
        so.FindProperty("_detailRoot").objectReferenceValue = detail.gameObject;
        so.FindProperty("_detailTitle").objectReferenceValue = title;
        so.FindProperty("_detailBody").objectReferenceValue = body;
        so.FindProperty("_bodyViewport").objectReferenceValue = viewport;
        so.FindProperty("_bodyScrollIndicator").objectReferenceValue = bodyScroll.gameObject;
        so.FindProperty("_bodyScrollTrack").objectReferenceValue = bTrack;
        so.FindProperty("_bodyScrollThumb").objectReferenceValue = bThumb;
        so.ApplyModifiedPropertiesWithoutUndo();
        root.gameObject.SetActive(false);
        return tab;
    }

    #endregion

    static void AddToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path))
            return;

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
