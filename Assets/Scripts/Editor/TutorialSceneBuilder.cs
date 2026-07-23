using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;

/// <summary>
/// ゲームのセットアップ用エディタ拡張。メニュー "NeverNight/Setup" から実行する。
/// 1. レイヤー(Player/Enemy/Ground/Interactable)と衝突マトリクスの設定
/// 2. アセット(Consts/アイテム/攻撃定義/DI設定)と Prefab(Player/Enemy/ギミック/HUD)の生成
/// 3〜5. サンプルシーン(チュートリアル/フィールド/サンドボックス)の自動構築
/// 6. PlayerUI(HUD) / 8. PauseUI / 9. HomeUI / 10. GameOverUI / 11. ResultUI の各 UI シーン構築
/// 7. 手作業で作成したステージシーンのセットアップ(スポーン地点/ブートストラップ/エリア名)
/// 12. PlayerScene(プレイヤー常駐シーン)の構築
/// 本編は「PlayerScene を土台に、ステージと UI を Additive で重ねる」方式。
/// ステージは手作業で作成して 7 で配線する。ステージ入替 (StageLoader.TransitionTo) では
/// プレイヤーが破棄されないため、ステータスが維持される。既存のアセットは再利用する。
/// </summary>
public static class TutorialSceneBuilder
{
    private const string PrefabDir = "Assets/Prefabs";
    private const string PlayerPrefabPath = PrefabDir + "/Player.prefab";
    private const string EnemyPrefabPath = PrefabDir + "/Enemy.prefab";
    private const string BoxPrefabPath = PrefabDir + "/Box.prefab";
    private const string LeverPrefabPath = PrefabDir + "/Lever.prefab";
    private const string DoorPrefabPath = PrefabDir + "/Door.prefab";
    private const string FloaterPrefabPath = PrefabDir + "/DamageFloater.prefab";
    private const string MachiNeedlePrefabPath = PrefabDir + "/MachiNeedle.prefab";
    private const string MishinNeedlePrefabPath = PrefabDir + "/MishinNeedle.prefab";
    private const string ThreadBallPrefabPath = PrefabDir + "/ThreadBall.prefab";
    private const string BombPrefabPath = PrefabDir + "/BobbinBomb.prefab";
    private const string PinCushionPrefabPath = PrefabDir + "/PinCushion.prefab";

    private const string ItemDir = "Assets/Items";
    private const string SavePointPrefabPath = PrefabDir + "/SavePoint.prefab";
    private const string BlacksmithPrefabPath = PrefabDir + "/Blacksmith.prefab";
    private const string RibbonPrefabPath = PrefabDir + "/Ribbon.prefab";

    private const string EnemyConstsPath = "Assets/Scripts/Enemy/EnemyConsts.asset";
    private const string EnemyConstsHeavyPath = "Assets/Scripts/Enemy/EnemyConsts_Heavy.asset";
    private const string EnemyConstsSwiftPath = "Assets/Scripts/Enemy/EnemyConsts_Swift.asset";
    private const string PlayerConstsPath = "Assets/Scripts/Player/PlayerConsts.asset";
    private const string GameScopePrefabPath = "Assets/Prefabs/GameLifetimeScope.prefab";
    private const string VContainerSettingsPath = "Assets/VContainerSettings.asset";
    private const string AttackDir = "Assets/Attacks";
    private const string NeedleShotPrefabPath = PrefabDir + "/NeedleShot.prefab";
    private const string HudPrefabPath = PrefabDir + "/HUD.prefab";
    private const string PauseUIPrefabPath = PrefabDir + "/PauseUI.prefab";
    private const string HomeUIPrefabPath = PrefabDir + "/HomeUI.prefab";
    private const string GameOverUIPrefabPath = PrefabDir + "/GameOverUI.prefab";
    private const string ResultUIPrefabPath = PrefabDir + "/ResultUI.prefab";
    private const string PlayerScenePath = "Assets/Scenes/PlayerScene.unity";
    private const string UISceneDir = "Assets/Scenes/UI";
    private const string PlayerUIScenePath = UISceneDir + "/PlayerUI.unity";
    private const string PauseUIScenePath = UISceneDir + "/PauseUI.unity";
    private const string HomeUIScenePath = UISceneDir + "/HomeUI.unity";
    private const string GameOverUIScenePath = UISceneDir + "/GameOverUI.unity";
    private const string ResultUIScenePath = UISceneDir + "/ResultUI.unity";
    private const string SourceScenePath = "Assets/Scenes/IngameTestScene.unity";
    private const string ScenePath = "Assets/Scenes/TutorialScene.unity";
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string FieldScenePath = "Assets/Scenes/FieldScene.unity";
    private const string SandboxScenePath = "Assets/Scenes/SandboxScene.unity";
    private const string JpFontPath = "Assets/Art/Fonts/JapaneseUI SDF.asset";

    private static readonly Color GroundColor = new(0.35f, 0.33f, 0.4f);
    private static readonly Color EnemyColor = new(0.65f, 0.35f, 0.85f);
    private static readonly Color BoxColor = new(0.65f, 0.45f, 0.25f);
    private static readonly Color LeverColor = new(0.95f, 0.85f, 0.3f);
    private static readonly Color DoorColor = new(0.55f, 0.58f, 0.62f);

    #region Menu Items

    [MenuItem("NeverNight/Setup/Build All (レイヤー→アセット→全シーン)", false, 0)]
    public static void BuildAll()
    {
        SetupLayersAndPhysics();
        CreateAssetsAndPrefabs();
        BuildTitleScene();
        BuildFieldScene();
        BuildTutorialScene();
        BuildSandboxScene(); // 最後に構築したシーンが開いた状態になる
    }

    [MenuItem("NeverNight/Setup/1. Setup Layers && Physics", false, 20)]
    public static void SetupLayersAndPhysics()
    {
        AddLayer("Player");
        AddLayer("Enemy");
        AddLayer("Ground");
        AddLayer("Interactable");

        // 敵同士は押し合わないようにする
        var enemyLayer = LayerMask.NameToLayer("Enemy");
        SetLayerCollision(enemyLayer, enemyLayer, false);

        // プレイヤーと敵は物理的にぶつからない (すり抜け)。
        // 接触ダメージは EnemyController が重なり判定で与える
        var playerLayer = LayerMask.NameToLayer("Player");
        SetLayerCollision(playerLayer, enemyLayer, false);

        Debug.Log("[TutorialSceneBuilder] レイヤーと衝突マトリクスを設定しました。");
    }

    [MenuItem("NeverNight/Setup/2. Create Assets && Prefabs", false, 21)]
    public static void CreateAssetsAndPrefabs()
    {
        EnsureDirectory(PrefabDir);

        UpdatePlayerConsts();
        EnsureGameLifetimeScopePrefab();
        GetOrCreateEnemyConsts();
        CreateEnemyVariantConsts();

        // プロジェクタイル/アイテム実体はアイテム定義が参照するため先に作る
        CreateProjectilePrefab(MachiNeedlePrefabPath, new Color(0.92f, 0.92f, 0.98f), new Vector2(0.6f, 0.1f));
        CreateProjectilePrefab(MishinNeedlePrefabPath, new Color(0.7f, 0.8f, 1f), new Vector2(0.4f, 0.14f));
        CreateProjectilePrefab(ThreadBallPrefabPath, new Color(0.85f, 0.6f, 1f), new Vector2(0.3f, 0.3f));
        CreateProjectilePrefab(NeedleShotPrefabPath, new Color(0.95f, 0.9f, 0.5f), new Vector2(0.5f, 0.12f));
        CreateBombPrefab();
        CreatePinCushionPrefab();

        // アイテム定義 (ScriptableObject)。Player Prefab のカタログが参照する
        CreateItemDefinitions();

        // 攻撃方法の定義 (ScriptableObject)。Player Prefab のロードアウトが参照する
        CreateAttackDefinitions();

        GetOrCreatePlayerPrefab();
        CreateEnemyPrefab();
        CreateBoxPrefab();
        CreateLeverPrefab();
        CreateDoorPrefab();
        CreateFloaterPrefab();
        CreateSavePointPrefab();
        CreateBlacksmithPrefab();
        CreateRibbonPrefab();

        AssetDatabase.SaveAssets();
        Debug.Log("[TutorialSceneBuilder] アセットと Prefab を生成しました。");
    }

    [MenuItem("NeverNight/Setup/3. Build Tutorial Scene", false, 22)]
    public static void BuildTutorialScene()
    {
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (playerPrefab == null || enemyPrefab == null)
        {
            Debug.LogError("[TutorialSceneBuilder] Prefab がありません。先に \"2. Create Assets & Prefabs\" を実行してください。");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var groundLayer = LayerMask.NameToLayer("Ground");
        var square = GetSquareSprite();
        var jpFont = GetOrCreateJapaneseFont();

        // ---- カメラ・ライト ----
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.14f, 0.14f, 0.2f);
        cameraGo.AddComponent<AudioListener>();
        var follow = cameraGo.AddComponent<CameraFollow>();
        cameraGo.transform.position = new Vector3(-7f, 3.5f, -10f);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ---- 地形 ----
        var stage = new GameObject("Stage");

        // 床 (x: -12〜68) と左右の壁、ジャンプで越える段差
        CreateBlock(stage.transform, "Floor", square, GroundColor, groundLayer,
            new Vector2(28f, -0.5f), new Vector2(80f, 1f));
        CreateBlock(stage.transform, "WallLeft", square, GroundColor, groundLayer,
            new Vector2(-12.5f, 3.5f), new Vector2(1f, 9f));
        CreateBlock(stage.transform, "WallRight", square, GroundColor, groundLayer,
            new Vector2(68.5f, 3.5f), new Vector2(1f, 9f));
        CreateBlock(stage.transform, "JumpBlock", square, GroundColor, groundLayer,
            new Vector2(26f, 1f), new Vector2(2.5f, 2f));

        // ---- 通過ゾーン (チュートリアル判定) ----
        var walkGoal = CreateStepTrigger("WalkGoal", new Vector2(8f, 1.5f), new Vector2(1f, 3f));
        var jumpGoal = CreateStepTrigger("JumpGoal", new Vector2(31f, 1.5f), new Vector2(1f, 3f));
        var endGoal = CreateStepTrigger("EndGoal", new Vector2(65f, 1.5f), new Vector2(1f, 3f));

        // ---- プレイヤー ----
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(-9f, 1.5f, 0f);
        SetRef(follow, "_target", player.transform);

        var playerController = player.GetComponent<PlayerController>();
        var playerHealth = player.GetComponent<PlayerHealth>();
        var playerGauge = player.GetComponent<PlayerHealGauge>();
        var playerInteractor = player.GetComponent<PlayerInteractor>();
        var playerInventory = player.GetComponent<PlayerItemInventory>();

        // ---- 箱 (攻撃練習) ----
        var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoxPrefabPath);
        var boxes = new[]
        {
            InstantiateAt(boxPrefab, new Vector3(34f, 0.5f, 0f)),
            InstantiateAt(boxPrefab, new Vector3(36.5f, 0.5f, 0f)),
            InstantiateAt(boxPrefab, new Vector3(39f, 0.5f, 0f)),
        };

        // ---- 敵 ----
        var enemies = new[]
        {
            InstantiateAt(enemyPrefab, new Vector3(46f, 1f, 0f)),
            InstantiateAt(enemyPrefab, new Vector3(52f, 1f, 0f)),
        };

        // ---- レバーとドア ----
        var lever = InstantiateAt(AssetDatabase.LoadAssetAtPath<GameObject>(LeverPrefabPath), new Vector3(58f, 0.85f, 0f));
        var door = InstantiateAt(AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath), new Vector3(61f, 1.5f, 0f));
        SetRef(lever.GetComponent<LeverSwitch>(), "_targetDoor", door.GetComponent<Door>());

        // ---- ワールド UI ----
        var spawnerGo = new GameObject("DamageFloaterSpawner");
        var spawner = spawnerGo.AddComponent<DamageFloaterSpawner>();
        SetRef(spawner, "_floaterPrefab",
            AssetDatabase.LoadAssetAtPath<GameObject>(FloaterPrefabPath).GetComponent<DamageFloater>());

        var interactPrompt = BuildInteractPrompt(playerInteractor, jpFont);

        // ---- HUD ----
        var hud = BuildHud(playerController, playerHealth, playerGauge, playerInventory,
            "準備室", jpFont, out var messageView);

        // ---- チュートリアル進行 ----
        var tutorialGo = new GameObject("TutorialManager");
        var tutorial = tutorialGo.AddComponent<TutorialManager>();
        SetRef(tutorial, "_player", playerController);
        SetRef(tutorial, "_message", messageView);
        SetArray(tutorial, "_boxes", boxes.Select(b => (Object)b.GetComponent<BreakableBox>()).ToArray());
        SetArray(tutorial, "_enemies", enemies.Select(e => (Object)e.GetComponent<EnemyController>()).ToArray());
        SetRef(tutorial, "_lever", lever.GetComponent<LeverSwitch>());
        SetRef(tutorial, "_door", door.GetComponent<Door>());
        SetRef(tutorial, "_walkGoal", walkGoal);
        SetRef(tutorial, "_jumpGoal", jumpGoal);
        SetRef(tutorial, "_endGoal", endGoal);

        // ---- 出口: フィールド (試験場) への遷移ゾーン ----
        var exitGo = new GameObject("ExitToField");
        exitGo.transform.position = new Vector3(67.3f, 2.5f, 0f);
        var exitCol = exitGo.AddComponent<BoxCollider2D>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector2(1f, 5f);
        var exitZone = exitGo.AddComponent<SceneTransitionZone>();
        SetString(exitZone, "_sceneName", "FieldScene");

        // ---- 保存と Build Settings 登録 ----
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(TitleScenePath);
        AddSceneToBuildSettings(ScenePath);
        AddSceneToBuildSettings(FieldScenePath);
        AddSceneToBuildSettings(SourceScenePath);

        Debug.Log($"[TutorialSceneBuilder] {ScenePath} を構築しました。Play で手順1〜8を確認してください。");
    }

    [MenuItem("NeverNight/Setup/4. Build Title Scene", false, 23)]
    public static void BuildTitleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var jpFont = GetOrCreateJapaneseFont();

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.08f, 0.14f);
        cameraGo.AddComponent<AudioListener>();
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);

        var titleGo = new GameObject("TitleScreen");
        var title = titleGo.AddComponent<TitleScreen>();
        SetRef(title, "_font", jpFont);

        // ---- タイトル UI (事前配置。ロゴやメニューの素材はシーン上で差し替え可能) ----
        var canvasGo = new GameObject("TitleCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var canvasRt = (RectTransform)canvasGo.transform;

        var titleLabel = CreateHudText(canvasRt, "GameTitle", "Never Night Wonderland", 84f,
            new Color(0.9f, 0.9f, 1f), jpFont);
        titleLabel.fontStyle = FontStyles.Bold;
        var titleLabelRt = titleLabel.rectTransform;
        titleLabelRt.anchorMin = new Vector2(0.5f, 1f);
        titleLabelRt.anchorMax = new Vector2(0.5f, 1f);
        titleLabelRt.pivot = new Vector2(0.5f, 1f);
        titleLabelRt.anchoredPosition = new Vector2(0f, -120f);
        titleLabelRt.sizeDelta = new Vector2(1400f, 120f);

        var titleMenu = CreateMenuPanel(canvasRt, "TitleMenu", jpFont);

        SetRef(title, "_titleLabel", titleLabel);
        SetRef(title, "_menu", titleMenu);

        EditorSceneManager.SaveScene(scene, TitleScenePath);
        AddSceneToBuildSettings(TitleScenePath);

        Debug.Log($"[TutorialSceneBuilder] {TitleScenePath} を構築しました。");
    }

    [MenuItem("NeverNight/Setup/5. Build Field Scene (試験場)", false, 24)]
    public static void BuildFieldScene()
    {
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (playerPrefab == null || enemyPrefab == null)
        {
            Debug.LogError("[TutorialSceneBuilder] Prefab がありません。先に \"2. Create Assets & Prefabs\" を実行してください。");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var groundLayer = LayerMask.NameToLayer("Ground");
        var square = GetSquareSprite();
        var jpFont = GetOrCreateJapaneseFont();

        // ---- カメラ・ライト ----
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.1f, 0.18f);
        cameraGo.AddComponent<AudioListener>();
        var follow = cameraGo.AddComponent<CameraFollow>();
        cameraGo.transform.position = new Vector3(-7f, 3.5f, -10f);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ---- 地形 ----
        var stage = new GameObject("Stage");
        CreateBlock(stage.transform, "Floor", square, GroundColor, groundLayer,
            new Vector2(45f, -0.5f), new Vector2(114f, 1f));
        CreateBlock(stage.transform, "WallLeft", square, GroundColor, groundLayer,
            new Vector2(-12.5f, 5.5f), new Vector2(1f, 13f));
        CreateBlock(stage.transform, "WallRight", square, GroundColor, groundLayer,
            new Vector2(102.5f, 5.5f), new Vector2(1f, 13f));

        // 青ハサミ (グラップル) で越える高壁: 斜め上に糸を刺して張り付き→ジャンプで越える
        CreateBlock(stage.transform, "GrappleWall", square, GroundColor, groundLayer,
            new Vector2(56f, 2f), new Vector2(2f, 6f));

        // 赤ハサミ (二段ジャンプ) で越える段差
        CreateBlock(stage.transform, "DoubleJumpBlock", square, GroundColor, groundLayer,
            new Vector2(78f, 2f), new Vector2(4f, 4f));

        // ---- プレイヤー ----
        var player = InstantiateAt(playerPrefab, new Vector3(-9f, 1.5f, 0f));
        SetRef(follow, "_target", player.transform);

        var playerController = player.GetComponent<PlayerController>();
        var playerHealth = player.GetComponent<PlayerHealth>();
        var playerGauge = player.GetComponent<PlayerHealGauge>();
        var playerInteractor = player.GetComponent<PlayerInteractor>();
        var playerInventory = player.GetComponent<PlayerItemInventory>();

        // ---- 拠点 (セーブポイント) ----
        InstantiateAt(AssetDatabase.LoadAssetAtPath<GameObject>(SavePointPrefabPath),
            new Vector3(-6f, 0.85f, 0f));

        // ---- 敵 (重装甲型 / 俊敏型) ----
        var heavyConsts = AssetDatabase.LoadAssetAtPath<EnemyConsts>(EnemyConstsHeavyPath);
        var swiftConsts = AssetDatabase.LoadAssetAtPath<EnemyConsts>(EnemyConstsSwiftPath);
        var heavyColor = new Color(0.6f, 0.65f, 0.8f);
        var swiftColor = new Color(0.5f, 0.9f, 0.5f);

        SpawnEnemy(enemyPrefab, new Vector3(8f, 1f, 0f), heavyConsts, heavyColor);
        SpawnEnemy(enemyPrefab, new Vector3(16f, 1f, 0f), swiftConsts, swiftColor);
        SpawnEnemy(enemyPrefab, new Vector3(36f, 1f, 0f), swiftConsts, swiftColor);
        // 終盤: 重装甲型と俊敏型の同時配置 (スタイル往復が起きる戦闘)
        SpawnEnemy(enemyPrefab, new Vector3(88f, 1f, 0f), heavyConsts, heavyColor);
        SpawnEnemy(enemyPrefab, new Vector3(92f, 1f, 0f), swiftConsts, swiftColor);

        // ---- 鍛冶師とリボン (黄 → 青 → 赤 の順に解禁) ----
        var blacksmithPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlacksmithPrefabPath);
        var ribbonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RibbonPrefabPath);

        SpawnBlacksmith(blacksmithPrefab, new Vector3(24f, 0.75f, 0f),
            ScissorUpgrade.Yellow, "カブトムシの鍛冶師", new Color(0.95f, 0.85f, 0.4f));
        SpawnRibbon(ribbonPrefab, new Vector3(30f, 1.5f, 0f),
            ScissorUpgrade.Yellow, new Color(1f, 0.9f, 0.35f));

        SpawnBlacksmith(blacksmithPrefab, new Vector3(42f, 0.75f, 0f),
            ScissorUpgrade.Blue, "クモの鍛冶師", new Color(0.45f, 0.65f, 1f));
        SpawnRibbon(ribbonPrefab, new Vector3(48f, 1.5f, 0f),
            ScissorUpgrade.Blue, new Color(0.45f, 0.65f, 1f));

        SpawnBlacksmith(blacksmithPrefab, new Vector3(64f, 0.75f, 0f),
            ScissorUpgrade.Red, "トビムシの鍛冶師", new Color(1f, 0.45f, 0.45f));
        SpawnRibbon(ribbonPrefab, new Vector3(70f, 1.5f, 0f),
            ScissorUpgrade.Red, new Color(1f, 0.45f, 0.45f));

        // ---- ゴール (リザルト) ----
        var goalGo = new GameObject("Goal");
        goalGo.transform.position = new Vector3(98f, 2.5f, 0f);
        var goalCol = goalGo.AddComponent<BoxCollider2D>();
        goalCol.isTrigger = true;
        goalCol.size = new Vector2(1.5f, 5f);
        goalGo.AddComponent<GoalZone>();
        // ゴールの目印
        CreateBlock(stage.transform, "GoalMark", square, new Color(0.9f, 0.8f, 0.3f, 0.5f), groundLayer,
            new Vector2(98f, 2f), new Vector2(0.3f, 4f)).GetComponent<BoxCollider2D>().isTrigger = true;

        // ---- ワールド UI ----
        var spawnerGo = new GameObject("DamageFloaterSpawner");
        var spawner = spawnerGo.AddComponent<DamageFloaterSpawner>();
        SetRef(spawner, "_floaterPrefab",
            AssetDatabase.LoadAssetAtPath<GameObject>(FloaterPrefabPath).GetComponent<DamageFloater>());

        BuildInteractPrompt(playerInteractor, jpFont);

        // ---- HUD ----
        BuildHud(playerController, playerHealth, playerGauge, playerInventory,
            "試験場", jpFont, out _);

        // ---- 保存と Build Settings 登録 ----
        EditorSceneManager.SaveScene(scene, FieldScenePath);
        AddSceneToBuildSettings(FieldScenePath);

        Debug.Log($"[TutorialSceneBuilder] {FieldScenePath} を構築しました。");
    }

    [MenuItem("NeverNight/Setup/6. Build Sandbox Scene (全機能テスト)", false, 25)]
    public static void BuildSandboxScene()
    {
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (playerPrefab == null || enemyPrefab == null)
        {
            Debug.LogError("[TutorialSceneBuilder] Prefab がありません。先に \"2. Create Assets & Prefabs\" を実行してください。");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var groundLayer = LayerMask.NameToLayer("Ground");
        var square = GetSquareSprite();
        var jpFont = GetOrCreateJapaneseFont();

        // ---- カメラ・ライト ----
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.14f, 0.12f);
        cameraGo.AddComponent<AudioListener>();
        var follow = cameraGo.AddComponent<CameraFollow>();
        cameraGo.transform.position = new Vector3(-7f, 3.5f, -10f);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ---- 地形 ----
        var stage = new GameObject("Stage");
        CreateBlock(stage.transform, "Floor", square, GroundColor, groundLayer,
            new Vector2(37f, -0.5f), new Vector2(98f, 1f));
        CreateBlock(stage.transform, "WallLeft", square, GroundColor, groundLayer,
            new Vector2(-12.5f, 5.5f), new Vector2(1f, 13f));
        CreateBlock(stage.transform, "WallRight", square, GroundColor, groundLayer,
            new Vector2(86.5f, 5.5f), new Vector2(1f, 13f));
        CreateBlock(stage.transform, "GrappleWall", square, GroundColor, groundLayer,
            new Vector2(48f, 2f), new Vector2(2f, 6f));
        CreateBlock(stage.transform, "DoubleJumpBlock", square, GroundColor, groundLayer,
            new Vector2(56f, 2f), new Vector2(4f, 4f));
        // まち針を刺して登る用の浮きブロック (通行は塞がない)
        CreateBlock(stage.transform, "NeedleTower", square, GroundColor, groundLayer,
            new Vector2(64f, 5f), new Vector2(1f, 4f));

        // ---- プレイヤー ----
        var player = InstantiateAt(playerPrefab, new Vector3(-9f, 1.5f, 0f));
        SetRef(follow, "_target", player.transform);

        var playerController = player.GetComponent<PlayerController>();
        var playerHealth = player.GetComponent<PlayerHealth>();
        var playerGauge = player.GetComponent<PlayerHealGauge>();
        var playerInteractor = player.GetComponent<PlayerInteractor>();
        var playerInventory = player.GetComponent<PlayerItemInventory>();

        // ---- 拠点 ----
        InstantiateAt(AssetDatabase.LoadAssetAtPath<GameObject>(SavePointPrefabPath),
            new Vector3(-7f, 0.85f, 0f));

        // ---- 鍛冶師3人 (最初に全強化を取れる) ----
        var blacksmithPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlacksmithPrefabPath);
        SpawnBlacksmith(blacksmithPrefab, new Vector3(-4f, 0.75f, 0f),
            ScissorUpgrade.Yellow, "カブトムシの鍛冶師", new Color(0.95f, 0.85f, 0.4f));
        SpawnBlacksmith(blacksmithPrefab, new Vector3(-2f, 0.75f, 0f),
            ScissorUpgrade.Blue, "クモの鍛冶師", new Color(0.45f, 0.65f, 1f));
        SpawnBlacksmith(blacksmithPrefab, new Vector3(0f, 0.75f, 0f),
            ScissorUpgrade.Red, "トビムシの鍛冶師", new Color(1f, 0.45f, 0.45f));

        // ---- レバーとドア ----
        var lever = InstantiateAt(AssetDatabase.LoadAssetAtPath<GameObject>(LeverPrefabPath),
            new Vector3(4f, 0.85f, 0f));
        var door = InstantiateAt(AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath),
            new Vector3(7f, 1.5f, 0f));
        SetRef(lever.GetComponent<LeverSwitch>(), "_targetDoor", door.GetComponent<Door>());

        // ---- 箱 (攻撃練習) ----
        var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoxPrefabPath);
        InstantiateAt(boxPrefab, new Vector3(10f, 0.5f, 0f));
        InstantiateAt(boxPrefab, new Vector3(11.5f, 0.5f, 0f));
        InstantiateAt(boxPrefab, new Vector3(13f, 0.5f, 0f));

        // ---- リボン3色 ----
        var ribbonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RibbonPrefabPath);
        SpawnRibbon(ribbonPrefab, new Vector3(16f, 1.5f, 0f), ScissorUpgrade.Yellow, new Color(1f, 0.9f, 0.35f));
        SpawnRibbon(ribbonPrefab, new Vector3(18.5f, 1.5f, 0f), ScissorUpgrade.Blue, new Color(0.45f, 0.65f, 1f));
        SpawnRibbon(ribbonPrefab, new Vector3(21f, 1.5f, 0f), ScissorUpgrade.Red, new Color(1f, 0.45f, 0.45f));

        // ---- 敵3種 (無限湧きスポナー: 倒すと再出現する) ----
        var normalConsts = AssetDatabase.LoadAssetAtPath<EnemyConsts>(EnemyConstsPath);
        var heavyConsts = AssetDatabase.LoadAssetAtPath<EnemyConsts>(EnemyConstsHeavyPath);
        var swiftConsts = AssetDatabase.LoadAssetAtPath<EnemyConsts>(EnemyConstsSwiftPath);
        SpawnEnemySpawner(enemyPrefab, new Vector3(27f, 1f, 0f), normalConsts, EnemyColor);
        SpawnEnemySpawner(enemyPrefab, new Vector3(33f, 1f, 0f), heavyConsts, new Color(0.6f, 0.65f, 0.8f));
        SpawnEnemySpawner(enemyPrefab, new Vector3(39f, 1f, 0f), swiftConsts, new Color(0.5f, 0.9f, 0.5f));
        // 混成戦 (重装甲 + 俊敏)
        SpawnEnemySpawner(enemyPrefab, new Vector3(72f, 1f, 0f), heavyConsts, new Color(0.6f, 0.65f, 0.8f));
        SpawnEnemySpawner(enemyPrefab, new Vector3(74f, 1f, 0f), swiftConsts, new Color(0.5f, 0.9f, 0.5f));

        // ---- ゾーン名ラベル (ワールド内) ----
        CreateWorldLabel("拠点: E で休む/セーブ", new Vector3(-7f, 2.6f, 0f), jpFont);
        CreateWorldLabel("鍛冶師: E で全強化を入手", new Vector3(-2f, 2.8f, 0f), jpFont);
        CreateWorldLabel("レバー(E)でドアが開く", new Vector3(5.5f, 3.2f, 0f), jpFont);
        CreateWorldLabel("箱: J 攻撃 / K 切替斬り", new Vector3(11.5f, 2.6f, 0f), jpFont);
        CreateWorldLabel("リボン: 対応色のハサミで切る", new Vector3(18.5f, 3.6f, 0f), jpFont);
        CreateWorldLabel("敵: 雑魚/重装甲/俊敏 (倒すと再出現)", new Vector3(33f, 3.2f, 0f), jpFont);
        CreateWorldLabel("グラップル壁: 上+F で越える", new Vector3(48f, 6f, 0f), jpFont);
        CreateWorldLabel("二段ジャンプで越える", new Vector3(56f, 5.2f, 0f), jpFont);
        CreateWorldLabel("まち針(I)を刺して登る", new Vector3(64f, 8f, 0f), jpFont);
        CreateWorldLabel("混成戦: スタイル往復", new Vector3(73f, 3.2f, 0f), jpFont);
        CreateWorldLabel("ゴール", new Vector3(81f, 3.2f, 0f), jpFont);

        // ---- ゴール (リザルト) ----
        var goalGo = new GameObject("Goal");
        goalGo.transform.position = new Vector3(81f, 2.5f, 0f);
        var goalCol = goalGo.AddComponent<BoxCollider2D>();
        goalCol.isTrigger = true;
        goalCol.size = new Vector2(1.5f, 5f);
        goalGo.AddComponent<GoalZone>();

        // ---- ワールド UI ----
        var spawnerGo = new GameObject("DamageFloaterSpawner");
        var spawner = spawnerGo.AddComponent<DamageFloaterSpawner>();
        SetRef(spawner, "_floaterPrefab",
            AssetDatabase.LoadAssetAtPath<GameObject>(FloaterPrefabPath).GetComponent<DamageFloater>());

        BuildInteractPrompt(playerInteractor, jpFont);

        // ---- HUD (+ ステータス常時表示) ----
        var hud = BuildHud(playerController, playerHealth, playerGauge, playerInventory,
            "テストルーム", jpFont, out _);
        BuildDebugStatus((RectTransform)hud.transform, playerController, jpFont);

        // ---- 保存と Build Settings 登録 ----
        EditorSceneManager.SaveScene(scene, SandboxScenePath);
        AddSceneToBuildSettings(SandboxScenePath);

        Debug.Log($"[TutorialSceneBuilder] {SandboxScenePath} を構築しました。全機能をこのシーンで確認できます。");
    }

    /// <summary>ワールド空間の説明ラベルを作る (テストシーン用)。</summary>
    private static void CreateWorldLabel(string text, Vector3 position, TMP_FontAsset jpFont)
    {
        var go = new GameObject($"Label_{text}", typeof(RectTransform));
        go.transform.position = position;
        var label = go.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = 3f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 1f, 1f, 0.75f);
        if (jpFont != null)
            label.font = jpFont;
        ((RectTransform)go.transform).sizeDelta = new Vector2(10f, 1.2f);

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sortingOrder = -5;
    }

    /// <summary>敵の無限湧きスポナーを配置する (テストシーン用)。</summary>
    private static void SpawnEnemySpawner(GameObject enemyPrefab, Vector3 position,
        EnemyConsts consts, Color tint)
    {
        var go = new GameObject("EnemySpawner");
        go.transform.position = position;
        var spawner = go.AddComponent<EnemySpawner>();
        SetRef(spawner, "_enemyPrefab", enemyPrefab);
        if (consts != null)
            SetRef(spawner, "_consts", consts);
        SetColor(spawner, "_tint", tint);
    }

    /// <summary>テストシーン用のステータス常時表示パネルを HUD に追加する。</summary>
    private static void BuildDebugStatus(RectTransform hudRoot, PlayerController player, TMP_FontAsset jpFont)
    {
        var square = GetSquareSprite();

        var statusRoot = CreateUIObject("DebugStatus", hudRoot);
        statusRoot.anchorMin = new Vector2(1f, 1f);
        statusRoot.anchorMax = new Vector2(1f, 1f);
        statusRoot.pivot = new Vector2(1f, 1f);
        statusRoot.anchoredPosition = new Vector2(-24f, -280f); // ミニマップの下
        statusRoot.sizeDelta = new Vector2(430f, 150f);
        var bg = statusRoot.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.sprite = square;

        var label = CreateHudText(statusRoot, "Label", "", 22f, Color.white, jpFont);
        label.alignment = TextAlignmentOptions.TopLeft;
        StretchWithPadding(label.rectTransform, 10f);

        var view = statusRoot.gameObject.AddComponent<DebugStatusView>();
        SetRef(view, "_player", player);
        SetRef(view, "_label", label);
    }

    private static void SpawnEnemy(GameObject prefab, Vector3 position, EnemyConsts consts, Color tint)
    {
        var enemy = InstantiateAt(prefab, position);
        if (consts != null)
            SetRef(enemy.GetComponent<EnemyController>(), "_consts", consts);
        enemy.GetComponent<SpriteRenderer>().color = tint;
    }

    private static void SpawnBlacksmith(GameObject prefab, Vector3 position,
        ScissorUpgrade upgrade, string smithName, Color tint)
    {
        var smith = InstantiateAt(prefab, position);
        var component = smith.GetComponent<Blacksmith>();
        SetEnum(component, "_upgrade", (int)upgrade);
        SetString(component, "_smithName", smithName);
        smith.GetComponent<SpriteRenderer>().color = tint;
    }

    private static void SpawnRibbon(GameObject prefab, Vector3 position,
        ScissorUpgrade required, Color tint)
    {
        var ribbon = InstantiateAt(prefab, position);
        SetEnum(ribbon.GetComponent<Ribbon>(), "_required", (int)required);
        ribbon.GetComponent<SpriteRenderer>().color = tint;
    }

    #endregion

    #region Layers & Physics

    private static void AddLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) != -1)
            return;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");

        // ビルトイン領域 (0〜7) を避けて空きスロットへ追加する
        for (var i = 8; i < layers.arraySize; i++)
        {
            var element = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }

        Debug.LogError($"[TutorialSceneBuilder] レイヤーの空きスロットがありません: {layerName}");
    }

    private static void SetLayerCollision(int layerA, int layerB, bool collide)
    {
        if (layerA < 0 || layerB < 0)
            return;

        var settings = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]);
        var matrix = settings.FindProperty("m_LayerCollisionMatrix");

        var rowA = matrix.GetArrayElementAtIndex(layerA);
        var rowB = matrix.GetArrayElementAtIndex(layerB);
        var maskA = unchecked((uint)rowA.longValue);
        var maskB = unchecked((uint)rowB.longValue);

        if (collide)
        {
            maskA |= 1u << layerB;
            maskB |= 1u << layerA;
        }
        else
        {
            maskA &= ~(1u << layerB);
            maskB &= ~(1u << layerA);
        }

        rowA.longValue = maskA;
        rowB.longValue = maskB;
        settings.ApplyModifiedProperties();
    }

    #endregion

    #region Assets & Prefabs

    /// <summary>
    /// ルート DI スコープ (GameLifetimeScope) のプレハブと VContainerSettings を用意する。
    /// VContainerSettings の RootLifetimeScope に登録しておくと、各シーンの
    /// 子スコープ (Stage/UI) の Build 時にルートが自動生成・接続されるため、
    /// GameLifetimeScope を手動でシーンに置く必要はない。
    /// </summary>
    private static void EnsureGameLifetimeScopePrefab()
    {
        var consts = AssetDatabase.LoadAssetAtPath<PlayerConsts>(PlayerConstsPath);
        if (consts == null)
            Debug.LogError($"[TutorialSceneBuilder] {PlayerConstsPath} が見つかりません。GameLifetimeScope の PlayerConsts が未設定になります。");

        // ルートスコープのプレハブを用意して PlayerConsts を配線する
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameScopePrefabPath);
        if (prefab != null && prefab.TryGetComponent<GameLifetimeScope>(out var existingScope))
        {
            SetRef(existingScope, "_playerConsts", consts);
        }
        else
        {
            EnsureDirectory(PrefabDir);
            var go = new GameObject("GameLifetimeScope");
            try
            {
                var scope = go.AddComponent<GameLifetimeScope>();
                SetRef(scope, "_playerConsts", consts);
                prefab = PrefabUtility.SaveAsPrefabAsset(go, GameScopePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // VContainerSettings に RootLifetimeScope として登録する
        var settings = AssetDatabase.LoadAssetAtPath<VContainerSettings>(VContainerSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<VContainerSettings>();
            AssetDatabase.CreateAsset(settings, VContainerSettingsPath);
        }

        settings.RootLifetimeScope = prefab.GetComponent<GameLifetimeScope>();
        EditorUtility.SetDirty(settings);

        // Preloaded Assets に登録する (実行時に VContainerSettings.Instance として読まれる)
        var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
        if (!preloadedAssets.Contains(settings))
        {
            preloadedAssets.RemoveAll(x => x is VContainerSettings);
            preloadedAssets.Add(settings);
            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }
    }

    private static void UpdatePlayerConsts()
    {
        var consts = AssetDatabase.LoadAssetAtPath<PlayerConsts>(PlayerConstsPath);
        if (consts == null)
        {
            Debug.LogError($"[TutorialSceneBuilder] {PlayerConstsPath} が見つかりません。");
            return;
        }

        var so = new SerializedObject(consts);
        // 攻撃対象 = 敵 + 箱(Ground レイヤー)。接地 = Ground + 既存シーン互換の Default
        so.FindProperty("_attackTargetLayer").intValue =
            LayerMask.GetMask("Enemy", "Ground");
        so.FindProperty("_interactableLayer").intValue = LayerMask.GetMask("Interactable");
        so.FindProperty("_groundLayer").intValue = LayerMask.GetMask("Ground", "Default");
        // 裁断の発動範囲 (x=横射程, y=高さ許容)。旧アセットの狭い値 (3,2) を上書きする
        so.FindProperty("_finisherRange").vector2Value = new Vector2(5f, 2.4f);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(consts);
    }

    private static EnemyConsts GetOrCreateEnemyConsts()
    {
        var consts = AssetDatabase.LoadAssetAtPath<EnemyConsts>(EnemyConstsPath);
        if (consts == null)
        {
            consts = ScriptableObject.CreateInstance<EnemyConsts>();
            AssetDatabase.CreateAsset(consts, EnemyConstsPath);
        }

        return consts;
    }

    /// <summary>敵タイプ別の定数アセット (重装甲型 / 俊敏型) を生成する。</summary>
    private static void CreateEnemyVariantConsts()
    {
        // 重装甲型: 防御値が高く両手持ち必須。遅いが接触ダメージが痛い
        CreateEnemyVariant(EnemyConstsHeavyPath, maxHp: 8, maxGuard: 14, moveSpeed: 1.2f,
            contactDamage: 2, chaseRange: 0f, chaseSpeed: 0f, threadDrop: 3);

        // 俊敏型: すばしっこく二刀流必須。防御値は低いが HP が多く、プレイヤーを追跡する
        CreateEnemyVariant(EnemyConstsSwiftPath, maxHp: 12, maxGuard: 3, moveSpeed: 3.5f,
            contactDamage: 1, chaseRange: 6f, chaseSpeed: 5f, threadDrop: 2);
    }

    private static void CreateEnemyVariant(string path, int maxHp, int maxGuard, float moveSpeed,
        int contactDamage, float chaseRange, float chaseSpeed, int threadDrop)
    {
        if (AssetDatabase.LoadAssetAtPath<EnemyConsts>(path) != null)
            return;

        var consts = ScriptableObject.CreateInstance<EnemyConsts>();
        AssetDatabase.CreateAsset(consts, path);

        var so = new SerializedObject(consts);
        so.FindProperty("_maxHp").intValue = maxHp;
        so.FindProperty("_maxGuard").intValue = maxGuard;
        so.FindProperty("_moveSpeed").floatValue = moveSpeed;
        so.FindProperty("_contactDamage").intValue = contactDamage;
        so.FindProperty("_chaseRange").floatValue = chaseRange;
        so.FindProperty("_chaseSpeed").floatValue = chaseSpeed;
        so.FindProperty("_threadDrop").intValue = threadDrop;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(consts);
    }

    /// <summary>汎用プロジェクタイル (まち針/ミシン針/斬撃波) の Prefab を生成する。</summary>
    private static void CreateProjectilePrefab(string path, Color color, Vector2 size)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return;

        var root = new GameObject(Path.GetFileNameWithoutExtension(path));
        try
        {
            var sprite = GetSquareSprite();
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 5;
            ApplyWorldSize(root, sprite, size);

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            root.AddComponent<Projectile>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateBombPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath) != null)
            return;

        var root = new GameObject("BobbinBomb");
        try
        {
            var sprite = GetSquareSprite();
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.35f, 0.3f, 0.3f);
            sr.sortingOrder = 5;
            ApplyWorldSize(root, sprite, new Vector2(0.4f, 0.4f));

            var rb = root.AddComponent<Rigidbody2D>();
            rb.mass = 0.2f;
            rb.freezeRotation = true;

            root.AddComponent<BoxCollider2D>(); // 非トリガー: 地面で跳ねて転がる

            root.AddComponent<BobbinBomb>();

            PrefabUtility.SaveAsPrefabAsset(root, BombPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreatePinCushionPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PinCushionPrefabPath) != null)
            return;

        var root = new GameObject("PinCushion");
        try
        {
            var sprite = GetSquareSprite();
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.75f, 0.55f, 0.45f);
            sr.sortingOrder = -1;
            ApplyWorldSize(root, sprite, new Vector2(1.4f, 0.35f));

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            root.AddComponent<PinCushionTrap>();

            PrefabUtility.SaveAsPrefabAsset(root, PinCushionPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// アイテム定義アセット (ItemDefinition 派生) を生成し、Prefab を配線する。
    /// パラメータ既定値はクラス側にあるため、ここでは表示情報と Prefab 参照だけ設定する。
    /// </summary>
    private static void CreateItemDefinitions()
    {
        EnsureDirectory(ItemDir);

        var machiPrefab = LoadComponent<Projectile>(MachiNeedlePrefabPath);
        var mishinPrefab = LoadComponent<Projectile>(MishinNeedlePrefabPath);
        var ballPrefab = LoadComponent<Projectile>(ThreadBallPrefabPath);
        var bombPrefab = LoadComponent<BobbinBomb>(BombPrefabPath);
        var trapPrefab = LoadComponent<PinCushionTrap>(PinCushionPrefabPath);

        CreateItem<MachiNeedleItem>("MachiNeedle", "まち針", "直進し、壁に刺さると足場になる",
            25, new Color(0.92f, 0.92f, 0.98f), "_projectilePrefab", machiPrefab);
        CreateItem<MishinNeedleItem>("MishinNeedle", "ミシン針", "曲射で投げる。威力が高い",
            12, new Color(0.6f, 0.75f, 1f), "_projectilePrefab", mishinPrefab);
        CreateItem<ClothCutterItem>("ClothCutter", "布カッター", "前方に突進し、敵をノックバック",
            4, new Color(1f, 0.6f, 0.3f), null, null);
        CreateItem<ThreadBallItem>("ThreadBall", "糸玉", "当たった敵を糸で絡めて拘束する",
            8, new Color(0.85f, 0.6f, 1f), "_projectilePrefab", ballPrefab);
        CreateItem<BobbinBombItem>("BobbinBomb", "ボビン爆弾", "時限爆発。範囲の防御値を大きく削る",
            5, new Color(1f, 0.75f, 0.4f), "_bombPrefab", bombPrefab);
        CreateItem<PinCushionItem>("PinCushion", "針山", "地面に設置。上の敵に継続ダメージ",
            5, new Color(0.8f, 0.65f, 0.5f), "_trapPrefab", trapPrefab);
    }

    private static void CreateItem<T>(string assetName, string displayName, string description,
        int maxCount, Color iconColor, string prefabField, Object prefab) where T : ItemDefinition
    {
        var path = $"{ItemDir}/{assetName}.asset";
        var item = AssetDatabase.LoadAssetAtPath<T>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(item, path);

            var so = new SerializedObject(item);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_maxCount").intValue = maxCount;
            so.FindProperty("_iconColor").colorValue = iconColor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Prefab 参照は毎回配線し直す (Prefab を作り直しても追従できるように)
        if (!string.IsNullOrEmpty(prefabField) && prefab != null)
            SetRef(item, prefabField, prefab);

        EditorUtility.SetDirty(item);
    }

    private static T LoadComponent<T>(string prefabPath) where T : Component
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        return go != null ? go.GetComponent<T>() : null;
    }

    /// <summary>Assets/Items 配下の全アイテム定義を読み込む。</summary>
    /// <summary>
    /// 攻撃方法の定義 (AttackDefinition) を生成する。
    /// □=近接 (スラッシュ=HP寄り / 重断ち=防御値寄り)、△=特殊 (針弾=遠距離・防御値寄り)。
    /// 数値は実行のたびに上書きして最新に保つ。
    /// </summary>
    private static void CreateAttackDefinitions()
    {
        EnsureDirectory(AttackDir);

        var slash = GetOrCreateAttack<MeleeAttackDefinition>("Slash", "スラッシュ",
            "素早い斬撃。HP(赤)を削りやすい", new Color(0.4f, 0.8f, 1f));
        SetAttackProfile(slash, "_profile", 0.25f, 0.06f, 2, 1, new Vector2(0.7f, 0f), new Vector2(1.2f, 1f));

        var heavy = GetOrCreateAttack<MeleeAttackDefinition>("HeavyCut", "重断ち",
            "大振りの一撃。防御値(白)を削りやすい", new Color(1f, 0.55f, 0.25f));
        SetAttackProfile(heavy, "_profile", 0.5f, 0.2f, 1, 3, new Vector2(0.8f, 0f), new Vector2(1.5f, 1.4f));

        var needle = GetOrCreateAttack<RangedSpecialDefinition>("NeedleShot", "針弾",
            "前方へ針を飛ばす遠距離攻撃。防御値(白)を削りやすい", new Color(0.95f, 0.9f, 0.5f));
        var so = new SerializedObject(needle);
        so.FindProperty("_projectilePrefab").objectReferenceValue = LoadComponent<Projectile>(NeedleShotPrefabPath);
        so.FindProperty("_speed").floatValue = 14f;
        so.FindProperty("_lifetime").floatValue = 0.6f;
        so.FindProperty("_hpDamage").intValue = 1;
        so.FindProperty("_guardDamage").intValue = 2;
        so.FindProperty("_useDuration").floatValue = 0.3f;
        so.FindProperty("_useDelay").floatValue = 0.1f;
        so.FindProperty("_cooldown").floatValue = 0.8f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrCreateAttack<T>(string assetName, string displayName, string description, Color iconColor)
        where T : AttackDefinition
    {
        var path = $"{AttackDir}/{assetName}.asset";
        var attack = AssetDatabase.LoadAssetAtPath<T>(path);
        if (attack == null)
        {
            attack = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(attack, path);
        }

        var so = new SerializedObject(attack);
        so.FindProperty("_displayName").stringValue = displayName;
        so.FindProperty("_description").stringValue = description;
        so.FindProperty("_iconColor").colorValue = iconColor;
        so.ApplyModifiedPropertiesWithoutUndo();
        return attack;
    }

    /// <summary>ネストされた AttackProfile ([SerializeField]) の各フィールドへ値を書き込む。</summary>
    private static void SetAttackProfile(Object target, string fieldName, float duration, float hitDelay,
        int hpDamage, int guardDamage, Vector2 offset, Vector2 boxSize)
    {
        var so = new SerializedObject(target);
        so.FindProperty($"{fieldName}._duration").floatValue = duration;
        so.FindProperty($"{fieldName}._hitDelay").floatValue = hitDelay;
        so.FindProperty($"{fieldName}._hpDamage").intValue = hpDamage;
        so.FindProperty($"{fieldName}._guardDamage").intValue = guardDamage;
        so.FindProperty($"{fieldName}._offset").vector2Value = offset;
        so.FindProperty($"{fieldName}._boxSize").vector2Value = boxSize;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object[] LoadAttackCatalog()
    {
        var guids = AssetDatabase.FindAssets("t:AttackDefinition", new[] { AttackDir });
        var attacks = new Object[guids.Length];
        for (var i = 0; i < guids.Length; i++)
            attacks[i] = AssetDatabase.LoadAssetAtPath<AttackDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
        return attacks;
    }

    private static Object[] LoadItemCatalog()
    {
        var guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { ItemDir });
        var items = new Object[guids.Length];
        for (var i = 0; i < guids.Length; i++)
            items[i] = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
        return items;
    }

    private static Object FindItem(Object[] catalog, string assetName)
    {
        foreach (var item in catalog)
        {
            if (item != null && item.name == assetName)
                return item;
        }

        return null;
    }

    private static void CreateSavePointPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SavePointPrefabPath) != null)
            return;

        var root = new GameObject("SavePoint");
        try
        {
            root.layer = LayerMask.NameToLayer("Interactable");

            var sprite = GetSquareSprite();
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.3f, 0.95f, 0.95f);
            ApplyWorldSize(root, sprite, new Vector2(0.8f, 1.4f));

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            root.AddComponent<SavePoint>();

            PrefabUtility.SaveAsPrefabAsset(root, SavePointPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateBlacksmithPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BlacksmithPrefabPath) != null)
            return;

        var root = new GameObject("Blacksmith");
        try
        {
            root.layer = LayerMask.NameToLayer("Interactable");

            var sprite = GetSquareSprite();
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.85f, 0.75f, 0.55f);
            ApplyWorldSize(root, sprite, new Vector2(0.9f, 1.5f));

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            root.AddComponent<Blacksmith>();

            PrefabUtility.SaveAsPrefabAsset(root, BlacksmithPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateRibbonPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RibbonPrefabPath) != null)
            return;

        var root = new GameObject("Ribbon");
        try
        {
            // 攻撃対象レイヤー (Ground) に置いて通行を塞ぐ
            root.layer = LayerMask.NameToLayer("Ground");

            var sprite = GetSquareSprite();
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 0.55f, 0.75f);
            ApplyWorldSize(root, sprite, new Vector2(0.6f, 3f));

            root.AddComponent<BoxCollider2D>();
            root.AddComponent<Ribbon>();

            PrefabUtility.SaveAsPrefabAsset(root, RibbonPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// Player Prefab を用意する。無ければ IngameTestScene の Warrior から自動生成し、
    /// PlayerHealth / PlayerHealGauge / PlayerInteractor と Player レイヤーを付与する。
    /// </summary>
    private static GameObject GetOrCreatePlayerPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            var scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            var warrior = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Warrior");
            if (warrior == null)
            {
                EditorSceneManager.CloseScene(scene, true);
                Debug.LogError($"[TutorialSceneBuilder] {SourceScenePath} に Warrior が見つかりません。");
                return null;
            }

            prefab = PrefabUtility.SaveAsPrefabAsset(warrior, PlayerPrefabPath);
            EditorSceneManager.CloseScene(scene, true);
        }

        // Prefab の中身を編集してコンポーネントとレイヤーを保証する
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        var playerLayer = LayerMask.NameToLayer("Player");
        SetLayerRecursively(root, playerLayer);

        if (root.GetComponent<PlayerHealth>() == null) root.AddComponent<PlayerHealth>();
        if (root.GetComponent<PlayerHealGauge>() == null) root.AddComponent<PlayerHealGauge>();
        if (root.GetComponent<PlayerInteractor>() == null) root.AddComponent<PlayerInteractor>();
        if (root.GetComponent<PlayerSaveBridge>() == null) root.AddComponent<PlayerSaveBridge>();
        if (root.GetComponent<PlayerLifetimeScope>() == null) root.AddComponent<PlayerLifetimeScope>();

        if (root.GetComponent<PlayerProgression>() == null) root.AddComponent<PlayerProgression>();

        var inventory = root.GetComponent<PlayerItemInventory>();
        if (inventory == null) inventory = root.AddComponent<PlayerItemInventory>();

        // アイテムカタログと初期スロット (下=布カッター / 左=まち針 / 右=ミシン針) を配線する
        var catalog = LoadItemCatalog();
        SetArray(inventory, "_catalog", catalog);
        SetArray(inventory, "_defaultSlots", new[]
        {
            FindItem(catalog, "ClothCutter"),
            FindItem(catalog, "MachiNeedle"),
            FindItem(catalog, "MishinNeedle"),
        });

        // 攻撃方法のロードアウト (□=スラッシュ / △=針弾)。
        // 解放ギミック (入手イベント) ができるまでは全攻撃を初期解放にしておく
        var attackLoadout = root.GetComponent<PlayerAttackLoadout>();
        if (attackLoadout == null) attackLoadout = root.AddComponent<PlayerAttackLoadout>();
        var attackCatalog = LoadAttackCatalog();
        SetArray(attackLoadout, "_catalog", attackCatalog);
        SetArray(attackLoadout, "_defaultUnlocked", attackCatalog);
        SetRef(attackLoadout, "_defaultMelee", FindItem(attackCatalog, "Slash"));
        SetRef(attackLoadout, "_defaultSpecial", FindItem(attackCatalog, "NeedleShot"));

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
    }

    private static void CreateEnemyPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath) != null)
            return;

        var square = GetSquareSprite();
        var root = new GameObject("Enemy");
        try
        {
            root.layer = LayerMask.NameToLayer("Enemy");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = EnemyColor;
            ApplyWorldSize(root, square, new Vector2(1f, 1f));

            var rb = root.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            root.AddComponent<BoxCollider2D>();

            var enemy = root.AddComponent<EnemyController>();
            SetRef(enemy, "_consts", GetOrCreateEnemyConsts());
            root.AddComponent<PatrolChaseBehaviour>(); // 標準のうごき (差し替え可能)

            BuildEnemyStatusCanvas(root, square);

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>敵頭上の World Space Canvas (防御値ゲージ・HPゲージ・BREAK! ラベル) を構築する。</summary>
    private static void BuildEnemyStatusCanvas(GameObject enemyRoot, Sprite square)
    {
        var canvasGo = new GameObject("StatusCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(enemyRoot.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 1f, 0f);
        canvasGo.transform.localScale = Vector3.one * 0.01f;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10; // 敵スプライトより手前に描く
        var canvasRt = (RectTransform)canvasGo.transform;
        canvasRt.sizeDelta = new Vector2(120f, 60f);

        // 防御値 (白) — 上段
        var guardBg = CreateUIImage(canvasRt, "GuardBg", new Color(0.1f, 0.1f, 0.1f, 0.8f), square);
        SetRect(guardBg.rectTransform, new Vector2(0f, 10f), new Vector2(110f, 10f));
        var guardFill = CreateUIImage(guardBg.rectTransform, "Fill", Color.white, square);
        MakeFilled(guardFill);

        // HP (赤) — 下段
        var hpBg = CreateUIImage(canvasRt, "HpBg", new Color(0.1f, 0.1f, 0.1f, 0.8f), square);
        SetRect(hpBg.rectTransform, new Vector2(0f, -2f), new Vector2(110f, 10f));
        var hpFill = CreateUIImage(hpBg.rectTransform, "Fill", new Color(0.9f, 0.25f, 0.25f), square);
        MakeFilled(hpFill);

        // BREAK! ラベル
        var breakGo = new GameObject("BreakLabel", typeof(RectTransform));
        breakGo.transform.SetParent(canvasRt, false);
        var breakRt = (RectTransform)breakGo.transform;
        SetRect(breakRt, new Vector2(0f, 32f), new Vector2(120f, 30f));
        var breakText = breakGo.AddComponent<TextMeshProUGUI>();
        breakText.text = "BREAK!";
        breakText.fontSize = 24f;
        breakText.fontStyle = FontStyles.Bold;
        breakText.color = new Color(1f, 0.85f, 0.2f);
        breakText.alignment = TextAlignmentOptions.Center;
        breakGo.SetActive(false);

        var view = canvasGo.AddComponent<EnemyStatusGaugeView>();
        SetRef(view, "_guardFill", guardFill);
        SetRef(view, "_hpFill", hpFill);
        SetRef(view, "_breakLabel", breakGo);
    }

    private static void CreateBoxPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BoxPrefabPath) != null)
            return;

        var root = new GameObject("Box");
        try
        {
            // 箱は足場にもなるので Ground レイヤー (攻撃対象レイヤーにも含まれる)
            root.layer = LayerMask.NameToLayer("Ground");

            var sprite = GetSquareSprite();
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = BoxColor;
            ApplyWorldSize(root, sprite, new Vector2(1f, 1f));

            root.AddComponent<BoxCollider2D>();
            root.AddComponent<BreakableBox>();

            PrefabUtility.SaveAsPrefabAsset(root, BoxPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateLeverPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LeverPrefabPath) != null)
            return;

        var root = new GameObject("Lever");
        try
        {
            root.layer = LayerMask.NameToLayer("Interactable");

            var sprite = GetSquareSprite();
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = LeverColor;
            ApplyWorldSize(root, sprite, new Vector2(0.25f, 1.2f));

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            root.AddComponent<LeverSwitch>();

            PrefabUtility.SaveAsPrefabAsset(root, LeverPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateDoorPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath) != null)
            return;

        var root = new GameObject("Door");
        try
        {
            root.layer = LayerMask.NameToLayer("Ground");

            var sprite = GetSquareSprite();
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = DoorColor;
            ApplyWorldSize(root, sprite, new Vector2(1f, 3f));

            root.AddComponent<BoxCollider2D>();
            root.AddComponent<Door>();

            PrefabUtility.SaveAsPrefabAsset(root, DoorPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateFloaterPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(FloaterPrefabPath) != null)
            return;

        var root = new GameObject("DamageFloater", typeof(RectTransform));
        try
        {
            var text = root.AddComponent<TextMeshPro>();
            text.text = "0";
            text.fontSize = 6f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            ((RectTransform)root.transform).sizeDelta = new Vector2(2f, 1f);

            var renderer = root.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sortingOrder = 20;

            root.AddComponent<DamageFloater>();

            PrefabUtility.SaveAsPrefabAsset(root, FloaterPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    #endregion

    #region Scene Pieces

    private static GameObject CreateBlock(Transform parent, string name, Sprite sprite, Color color,
        int layer, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.layer = layer;
        go.isStatic = true;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = -10;
        ApplyWorldSize(go, sprite, size);

        go.AddComponent<BoxCollider2D>();
        return go;
    }

    /// <summary>
    /// スプライトの実サイズ (PPU 依存) に関わらず、指定したワールドサイズになるようスケールを設定する。
    /// </summary>
    private static void ApplyWorldSize(GameObject go, Sprite sprite, Vector2 size)
    {
        var bounds = sprite.bounds.size;
        go.transform.localScale = new Vector3(
            size.x / Mathf.Max(bounds.x, 0.0001f),
            size.y / Mathf.Max(bounds.y, 0.0001f),
            1f);
    }

    private static TutorialStepTrigger CreateStepTrigger(string name, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.position = center;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        return go.AddComponent<TutorialStepTrigger>();
    }

    private static GameObject InstantiateAt(GameObject prefab, Vector3 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = position;
        return go;
    }

    private static GameObject BuildInteractPrompt(PlayerInteractor interactor, TMP_FontAsset jpFont)
    {
        var root = new GameObject("InteractPrompt");

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(root.transform, false);
        var text = textGo.AddComponent<TextMeshPro>();
        text.text = "[E]";
        text.fontSize = 3.5f;
        text.alignment = TextAlignmentOptions.Center;
        if (jpFont != null)
            text.font = jpFont;
        ((RectTransform)textGo.transform).sizeDelta = new Vector2(6f, 1f);

        var renderer = textGo.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sortingOrder = 20;

        var view = root.AddComponent<InteractPromptView>();
        SetRef(view, "_interactor", interactor);
        SetRef(view, "_label", text);
        SetRef(view, "_root", textGo);

        return root;
    }

    /// <summary>
    /// 共通 HUD プレハブ (Assets/Prefabs/HUD.prefab) を用意する。
    /// HPバー・回復ゲージ・装備表示・クールダウン円・アイテムスロット・糸カウント・
    /// 裁断プロンプト・通知と UILifetimeScope を含み、DI 駆動なのでシーン参照は不要。
    /// **既にプレハブがあれば再生成しない** — 見た目 (スプライト・配置) はプレハブを
    /// 直接編集すれば全シーンに反映される (素材差し替え用)。作り直す時はプレハブを削除して再実行。
    /// </summary>
    private static GameObject GetOrCreateHudPrefab(TMP_FontAsset jpFont)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
        if (existing != null)
            return existing;

        var square = GetSquareSprite();
        var circle = GetCircleSprite();

        var hudGo = new GameObject("HUD");
        var canvas = hudGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = hudGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var root = (RectTransform)hudGo.transform;

        // ---- HP バー (左上) ----
        var hpBar = CreateUIObject("HpBar", root);
        AnchorTopLeft(hpBar, new Vector2(30f, -30f), new Vector2(400f, 28f));
        var hpBg = hpBar.gameObject.AddComponent<Image>();
        hpBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        hpBg.sprite = square;
        var hpFill = CreateUIImage(hpBar, "Fill", new Color(0.9f, 0.25f, 0.25f), square);
        StretchWithPadding(hpFill.rectTransform, 3f);
        MakeFilled(hpFill);
        var hpView = hpBar.gameObject.AddComponent<PlayerHpBarView>();
        SetRef(hpView, "_fill", hpFill);

        // ---- 回復ゲージ (HP バーの下、メモリ×3) ----
        var healRoot = CreateUIObject("HealGauge", root);
        AnchorTopLeft(healRoot, new Vector2(30f, -66f), new Vector2(400f, 18f));
        var pipFills = new Image[3];
        for (var i = 0; i < 3; i++)
        {
            var pip = CreateUIObject($"Pip{i}", healRoot);
            AnchorTopLeft(pip, new Vector2(i * 100f, 0f), new Vector2(94f, 18f));
            var pipBg = pip.gameObject.AddComponent<Image>();
            pipBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            pipBg.sprite = square;
            pipFills[i] = CreateUIImage(pip, "Fill", new Color(0.45f, 0.95f, 0.55f), square);
            StretchWithPadding(pipFills[i].rectTransform, 2f);
            MakeFilled(pipFills[i]);
        }

        var healView = healRoot.gameObject.AddComponent<HealGaugeView>();
        SetArray(healView, "_pipFills", pipFills.Select(p => (Object)p).ToArray());

        // ---- 装備中の攻撃方法表示 (回復ゲージの下: □近接 / △特殊) ----
        var loadoutRoot = CreateUIObject("AttackLoadout", root);
        AnchorTopLeft(loadoutRoot, new Vector2(30f, -96f), new Vector2(260f, 64f));
        var loadoutBg = loadoutRoot.gameObject.AddComponent<Image>();
        loadoutBg.color = new Color(0.4f, 0.8f, 1f, 0.25f);
        loadoutBg.sprite = square;
        var loadoutLabel = CreateHudText(loadoutRoot, "Label", "□ ---\n△ ---", 22f, Color.white, jpFont);
        StretchWithPadding(loadoutLabel.rectTransform, 0f);
        var loadoutView = loadoutRoot.gameObject.AddComponent<AttackLoadoutView>();
        SetRef(loadoutView, "_label", loadoutLabel);
        SetRef(loadoutView, "_background", loadoutBg);

        // ---- 特殊攻撃クールダウン (装備表示の右、円形) ----
        var cooldownRoot = CreateUIObject("SpecialCooldown", root);
        AnchorTopLeft(cooldownRoot, new Vector2(300f, -96f), new Vector2(64f, 64f));
        var cooldownIcon = cooldownRoot.gameObject.AddComponent<Image>();
        cooldownIcon.color = new Color(0.95f, 0.9f, 0.5f);
        cooldownIcon.sprite = circle;
        var cooldownFill = CreateUIImage(cooldownRoot, "CooldownFill", new Color(0f, 0f, 0f, 0.6f), circle);
        StretchWithPadding(cooldownFill.rectTransform, 0f);
        cooldownFill.type = Image.Type.Filled;
        cooldownFill.fillMethod = Image.FillMethod.Radial360;
        cooldownFill.fillOrigin = (int)Image.Origin360.Top;
        cooldownFill.fillClockwise = false;
        cooldownFill.fillAmount = 0f;
        var cooldownLabel = CreateHudText(cooldownRoot, "Label", "△", 30f, Color.white, jpFont);
        StretchWithPadding(cooldownLabel.rectTransform, 0f);
        var cooldownView = cooldownRoot.gameObject.AddComponent<SpecialCooldownView>();
        SetRef(cooldownView, "_icon", cooldownIcon);
        SetRef(cooldownView, "_cooldownFill", cooldownFill);
        SetRef(cooldownView, "_label", cooldownLabel);

        // ---- 裁断プロンプト (画面中央やや下) ----
        var finisherRoot = CreateUIObject("FinisherPrompt", root);
        AnchorCenter(finisherRoot, new Vector2(0f, -180f), new Vector2(500f, 70f));
        var finisherContent = CreateUIObject("Content", finisherRoot);
        StretchWithPadding(finisherContent, 0f);
        var finisherLabel = CreateHudText(finisherContent, "Label", "[L] 裁断!", 48f, new Color(1f, 0.85f, 0.2f), jpFont);
        StretchWithPadding(finisherLabel.rectTransform, 0f);
        finisherLabel.fontStyle = FontStyles.Bold;
        var finisherView = finisherRoot.gameObject.AddComponent<FinisherPromptView>();
        SetRef(finisherView, "_root", finisherContent.gameObject);
        SetRef(finisherView, "_label", finisherLabel);

        // ---- アイテム表示 (左下、3種すべて + 使用方向) ----
        var itemSlot = CreateUIObject("ItemSlot", root);
        itemSlot.anchorMin = new Vector2(0f, 0f);
        itemSlot.anchorMax = new Vector2(0f, 0f);
        itemSlot.pivot = new Vector2(0f, 0f);
        itemSlot.anchoredPosition = new Vector2(30f, 30f);
        itemSlot.sizeDelta = new Vector2(310f, 108f);
        var itemBg = itemSlot.gameObject.AddComponent<Image>();
        itemBg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        itemBg.sprite = square;

        var itemLabels = new TMP_Text[ItemSlotExtensions.SlotCount];
        for (var i = 0; i < ItemSlotExtensions.SlotCount; i++)
        {
            var row = CreateHudText(itemSlot, $"Row{i}", "", 22f, Color.white, jpFont);
            row.alignment = TextAlignmentOptions.Left;
            AnchorTopLeft(row.rectTransform, new Vector2(12f, -6f - i * 32f), new Vector2(290f, 30f));
            itemLabels[i] = row;
        }

        var itemView = itemSlot.gameObject.AddComponent<ItemSlotView>();
        SetArray(itemView, "_labels", itemLabels.Select(l => (Object)l).ToArray());

        // ---- 糸カウント (装備表示の下) ----
        var threadRoot = CreateUIObject("ThreadCount", root);
        AnchorTopLeft(threadRoot, new Vector2(30f, -170f), new Vector2(220f, 30f));
        var threadLabel = CreateHudText(threadRoot, "Label", "糸 x0", 24f, new Color(0.9f, 0.9f, 0.7f), jpFont);
        threadLabel.alignment = TextAlignmentOptions.Left;
        StretchWithPadding(threadLabel.rectTransform, 0f);
        var threadView = threadRoot.gameObject.AddComponent<ThreadCountView>();
        SetRef(threadView, "_label", threadLabel);

        // ---- 通知トースト (下部中央) ----
        var notifyRoot = CreateUIObject("Notification", root);
        notifyRoot.anchorMin = new Vector2(0.5f, 0f);
        notifyRoot.anchorMax = new Vector2(0.5f, 0f);
        notifyRoot.pivot = new Vector2(0.5f, 0f);
        notifyRoot.anchoredPosition = new Vector2(0f, 140f);
        notifyRoot.sizeDelta = new Vector2(900f, 52f);
        var notifyBg = notifyRoot.gameObject.AddComponent<Image>();
        notifyBg.color = new Color(0f, 0f, 0f, 0.6f);
        notifyBg.sprite = square;
        var notifyLabel = CreateHudText(notifyRoot, "Label", "", 26f, Color.white, jpFont);
        StretchWithPadding(notifyLabel.rectTransform, 6f);
        notifyRoot.gameObject.AddComponent<CanvasGroup>();
        var notifyView = notifyRoot.gameObject.AddComponent<NotificationView>();
        SetRef(notifyView, "_label", notifyLabel);

        // ---- ミニマップ (右上)。ターゲットは PlayerRuntime 経由の実行時解決 ----
        var minimapRoot = CreateUIObject("Minimap", root);
        minimapRoot.anchorMin = new Vector2(1f, 1f);
        minimapRoot.anchorMax = new Vector2(1f, 1f);
        minimapRoot.pivot = new Vector2(1f, 1f);
        minimapRoot.anchoredPosition = new Vector2(-24f, -24f);
        minimapRoot.sizeDelta = new Vector2(240f, 240f);
        var minimapBorder = minimapRoot.gameObject.AddComponent<Image>();
        minimapBorder.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        minimapBorder.sprite = square;
        var minimapImageGo = new GameObject("Map", typeof(RectTransform));
        minimapImageGo.transform.SetParent(minimapRoot, false);
        StretchWithPadding((RectTransform)minimapImageGo.transform, 4f);
        var minimapImage = minimapImageGo.AddComponent<RawImage>();
        var minimapView = minimapRoot.gameObject.AddComponent<MinimapView>();
        SetRef(minimapView, "_image", minimapImage);

        // ---- DI スコープ: View を登録し Presenter を起動する ----
        var uiScope = hudGo.AddComponent<UILifetimeScope>();
        SetRef(uiScope, "_hpBarView", hpView);
        SetRef(uiScope, "_healGaugeView", healView);
        SetRef(uiScope, "_attackLoadoutView", loadoutView);
        SetRef(uiScope, "_specialCooldownView", cooldownView);
        SetRef(uiScope, "_itemSlotView", itemView);
        SetRef(uiScope, "_threadCountView", threadView);
        SetRef(uiScope, "_finisherPromptView", finisherView);
        SetRef(uiScope, "_minimapView", minimapView);

        EnsureDirectory(PrefabDir);
        var prefab = PrefabUtility.SaveAsPrefabAsset(hudGo, HudPrefabPath);
        Object.DestroyImmediate(hudGo);
        return prefab;
    }

    /// <summary>
    /// 自動生成シーンへ HUD を構築する。共通 HUD はプレハブ (GetOrCreateHudPrefab) を埋め込み、
    /// シーン固有 UI (BuildSceneUI) を配置する。DI はプレイヤープレハブの PlayerLifetimeScope が担う。
    /// 戻り値は SceneUI (デバッグ表示などの親に使う)。
    /// </summary>
    private static GameObject BuildHud(PlayerController player, PlayerHealth health, PlayerHealGauge gauge,
        PlayerItemInventory inventory, string areaName,
        TMP_FontAsset jpFont, out TutorialMessageView messageView)
    {
        // サンプルシーンは自己完結にするため、UI プレハブ一式を直接埋め込む
        // (本編の手作りステージは UISceneBootstrap による Additive ロードを使う)
        PrefabUtility.InstantiatePrefab(GetOrCreateHudPrefab(jpFont));
        PrefabUtility.InstantiatePrefab(GetOrCreatePauseUIPrefab(jpFont));
        PrefabUtility.InstantiatePrefab(GetOrCreateHomeUIPrefab(jpFont));
        PrefabUtility.InstantiatePrefab(GetOrCreateGameOverUIPrefab(jpFont));
        PrefabUtility.InstantiatePrefab(GetOrCreateResultUIPrefab(jpFont));

        return BuildSceneUI(player, areaName, jpFont, out messageView);
    }

    /// <summary>
    /// シーン固有 UI を構築する。プレイヤーやステージへの参照が必要な UI
    /// (チュートリアルメッセージ・エリア名・ミニマップ・各メニュー) を
    /// SceneUI キャンバスとしてシーン側に生成する。
    /// </summary>
    private static GameObject BuildSceneUI(PlayerController player, string areaName,
        TMP_FontAsset jpFont, out TutorialMessageView messageView)
    {
        var square = GetSquareSprite();

        var sceneUiGo = new GameObject("SceneUI");
        var canvas = sceneUiGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = sceneUiGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var root = (RectTransform)sceneUiGo.transform;

        // ---- チュートリアルメッセージ (上部中央) ----
        var messageRoot = CreateUIObject("TutorialMessage", root);
        messageRoot.anchorMin = new Vector2(0.5f, 1f);
        messageRoot.anchorMax = new Vector2(0.5f, 1f);
        messageRoot.pivot = new Vector2(0.5f, 1f);
        messageRoot.anchoredPosition = new Vector2(0f, -40f);
        messageRoot.sizeDelta = new Vector2(1100f, 70f);
        var messageBg = messageRoot.gameObject.AddComponent<Image>();
        messageBg.color = new Color(0f, 0f, 0f, 0.55f);
        messageBg.sprite = square;
        var messageLabel = CreateHudText(messageRoot, "Label", "", 30f, Color.white, jpFont);
        StretchWithPadding(messageLabel.rectTransform, 8f);
        messageRoot.gameObject.AddComponent<CanvasGroup>();
        messageView = messageRoot.gameObject.AddComponent<TutorialMessageView>();
        SetRef(messageView, "_label", messageLabel);

        // ---- エリア名タイトル (中央上寄り) ----
        var areaRoot = CreateUIObject("AreaTitle", root);
        AnchorCenter(areaRoot, new Vector2(0f, 160f), new Vector2(1200f, 100f));
        var areaLabel = CreateHudText(areaRoot, "Label", "", 64f, new Color(0.95f, 0.95f, 1f), jpFont);
        areaLabel.fontStyle = FontStyles.Bold;
        StretchWithPadding(areaLabel.rectTransform, 0f);
        areaRoot.gameObject.AddComponent<CanvasGroup>();
        var areaView = areaRoot.gameObject.AddComponent<AreaTitleView>();
        SetString(areaView, "_areaName", areaName);
        SetRef(areaView, "_label", areaLabel);

        // ミニマップは HUD プレハブ (PlayerUI) 側に含まれる (ターゲットは実行時解決)

        return sceneUiGo;
    }

    /// <summary>
    /// ポーズ UI プレハブ (Assets/Prefabs/PauseUI.prefab) を用意する。
    /// ポーズメニューと、プレイヤー解決用の LifetimeScope (PlayerRuntime 注入) を含む。
    /// UI はすべて事前配置 (MenuPanelView)。
    /// **既にプレハブがあれば再生成しない** — 調整はプレハブを直接編集する。
    /// </summary>
    private static GameObject GetOrCreatePauseUIPrefab(TMP_FontAsset jpFont)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PauseUIPrefabPath);
        if (existing != null)
            return existing;

        var menuGo = new GameObject("PauseUI");
        var canvas = menuGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // HUD より手前に描画する
        var scaler = menuGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var menuRoot = (RectTransform)menuGo.transform;

        // ポーズメニュー (ゲームオーバー/リザルトはそれぞれ専用プレハブ・シーンに分離)
        var pauseView = menuGo.AddComponent<PauseMenuView>();
        SetRef(pauseView, "_font", jpFont);
        SetRef(pauseView, "_menu", CreateMenuPanel(menuRoot, "PauseMenu", jpFont));

        // PauseMenuView へ PlayerRuntime を注入するスコープ
        menuGo.AddComponent<PauseLifetimeScope>();

        EnsureDirectory(PrefabDir);
        var prefab = PrefabUtility.SaveAsPrefabAsset(menuGo, PauseUIPrefabPath);
        Object.DestroyImmediate(menuGo);
        return prefab;
    }

    /// <summary>
    /// メニュー所有者 1 つ + 事前配置パネルだけの UI プレハブ (GameOverUI / ResultUI) を用意する。
    /// **既にプレハブがあれば再生成しない** — 調整はプレハブを直接編集する。
    /// </summary>
    private static GameObject GetOrCreateMenuOwnerPrefab<T>(string prefabPath, string rootName,
        string panelName, TMP_FontAsset jpFont) where T : Component
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
            return existing;

        var go = new GameObject(rootName);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 11; // ポーズ (10) より手前に描画する
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var owner = go.AddComponent<T>();
        SetRef(owner, "_font", jpFont);
        SetRef(owner, "_menu", CreateMenuPanel((RectTransform)go.transform, panelName, jpFont));

        EnsureDirectory(PrefabDir);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject GetOrCreateGameOverUIPrefab(TMP_FontAsset jpFont) =>
        GetOrCreateMenuOwnerPrefab<GameOverView>(GameOverUIPrefabPath, "GameOverUI", "GameOverMenu", jpFont);

    private static GameObject GetOrCreateResultUIPrefab(TMP_FontAsset jpFont) =>
        GetOrCreateMenuOwnerPrefab<ResultView>(ResultUIPrefabPath, "ResultUI", "ResultMenu", jpFont);

    /// <summary>
    /// ホーム画面 UI プレハブ (Assets/Prefabs/HomeUI.prefab) を用意する。
    /// 拠点 (SavePoint) のインタラクトで開かれ、HP回復/アイテム補充/攻撃入れ替え/セーブを行う
    /// HomeUIView と、その事前配置パネル (MenuPanelView) を含む。
    /// **既にプレハブがあれば再生成しない** — 調整はプレハブを直接編集する。
    /// </summary>
    private static GameObject GetOrCreateHomeUIPrefab(TMP_FontAsset jpFont)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HomeUIPrefabPath);
        if (existing != null)
            return existing;

        var homeGo = new GameObject("HomeUI");
        var canvas = homeGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // HUD より手前、メニュー (10) より奥
        var scaler = homeGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var homeRoot = (RectTransform)homeGo.transform;

        // 拠点画面 (HP回復/アイテム補充/攻撃入れ替え/セーブ)
        var homeView = homeGo.AddComponent<HomeUIView>();
        SetRef(homeView, "_font", jpFont);
        SetRef(homeView, "_menu", CreateMenuPanel(homeRoot, "HomePanel", jpFont));

        // ホームの View へ PlayerRuntime や Model を注入するスコープ
        homeGo.AddComponent<HomeLifetimeScope>();

        EnsureDirectory(PrefabDir);
        var prefab = PrefabUtility.SaveAsPrefabAsset(homeGo, HomeUIPrefabPath);
        Object.DestroyImmediate(homeGo);
        return prefab;
    }

    /// <summary>
    /// MenuPanelView の UI 一式 (暗幕/ウィンドウ/タイトル/本文/行テンプレート) を
    /// 事前配置で構築して配線する。実行時生成はしないので、
    /// 枠などの素材はプレハブ/シーン上の Image・行テンプレートを差し替えれば反映される。
    /// </summary>
    private static MenuPanelView CreateMenuPanel(RectTransform parent, string objectName, TMP_FontAsset jpFont)
    {
        var square = GetSquareSprite();

        var panelRt = CreateUIObject(objectName, parent);
        StretchWithPadding(panelRt, 0f);
        var view = panelRt.gameObject.AddComponent<MenuPanelView>();

        // 全面の暗幕 (開閉のルート)
        var rootRt = CreateUIObject("MenuRoot", panelRt);
        StretchWithPadding(rootRt, 0f);
        var dim = rootRt.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        dim.sprite = square;

        // ウィンドウ (枠素材の差し替え先)
        var windowRt = CreateUIObject("Window", rootRt);
        windowRt.sizeDelta = new Vector2(680f, 560f);
        var windowImage = windowRt.gameObject.AddComponent<Image>();
        windowImage.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        windowImage.sprite = square;

        // タイトル
        var titleText = CreateHudText(windowRt, "Title", "", 40f, Color.white, jpFont);
        titleText.fontStyle = FontStyles.Bold;
        var titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -24f);
        titleRt.sizeDelta = new Vector2(0f, 60f);

        // 本文
        var bodyText = CreateHudText(windowRt, "Body", "", 26f, Color.white, jpFont);
        bodyText.alignment = TextAlignmentOptions.Top;
        var bodyRt = bodyText.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 1f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.anchoredPosition = new Vector2(0f, -92f);
        bodyRt.sizeDelta = new Vector2(-60f, 190f);

        // 行コンテナ
        var rowsRt = CreateUIObject("Rows", windowRt);
        rowsRt.anchorMin = new Vector2(0f, 0f);
        rowsRt.anchorMax = new Vector2(1f, 1f);
        rowsRt.offsetMin = new Vector2(60f, 30f);
        rowsRt.offsetMax = new Vector2(-60f, -100f);

        // 行テキスト (HUD と同様にすべて事前配置し、実行時は表示切替のみ)
        const int rowCount = 10;
        var rows = new TMP_Text[rowCount];
        for (var i = 0; i < rowCount; i++)
        {
            var row = CreateHudText(rowsRt, $"Row{i}", "", 30f, Color.white, jpFont);
            row.alignment = TextAlignmentOptions.Left;
            var rowRt = row.rectTransform;
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, -i * 48f);
            rowRt.sizeDelta = new Vector2(0f, 44f);
            row.gameObject.SetActive(false);
            rows[i] = row;
        }

        // 閉じた状態で保存する
        rootRt.gameObject.SetActive(false);

        SetRef(view, "_root", rootRt.gameObject);
        SetRef(view, "_title", titleText);
        SetRef(view, "_body", bodyText);
        SetArray(view, "_rows", rows.Select(r => (Object)r).ToArray());

        return view;
    }

    /// <summary>
    /// PlayerUI シーンへ共通 HUD (プレハブ) を配置する。
    /// UISceneBootstrap で Additive ロードする UI 専用シーンを作る/更新するためのメニュー。
    /// 既存の PlayerUI.unity があれば HUD だけを置き直し、他のオブジェクトは保持する。
    /// </summary>
    [MenuItem("NeverNight/Setup/6. Build PlayerUI Scene (HUD)", false, 25)]
    public static void BuildPlayerUIScene()
    {
        var hudPrefab = GetOrCreateHudPrefab(GetOrCreateJapaneseFont());
        RebuildUIScene(PlayerUIScenePath, hudPrefab);
        Debug.Log($"[TutorialSceneBuilder] {PlayerUIScenePath} に HUD を配置しました。" +
                  "ステージシーン側に UISceneBootstrap を置くと Additive でロードされます。");
    }

    /// <summary>
    /// PauseUI シーンへポーズメニュー (プレハブ) を配置する。
    /// UISceneBootstrap で Additive ロードするポーズ専用シーンを作る/更新するためのメニュー。
    /// </summary>
    [MenuItem("NeverNight/Setup/8. Build PauseUI Scene (Pause)", false, 27)]
    public static void BuildPauseUIScene()
    {
        var pausePrefab = GetOrCreatePauseUIPrefab(GetOrCreateJapaneseFont());
        RebuildUIScene(PauseUIScenePath, pausePrefab);
        Debug.Log($"[TutorialSceneBuilder] {PauseUIScenePath} にポーズ UI を配置しました。" +
                  "ステージシーン側に UISceneBootstrap を置くと Additive でロードされます。");
    }

    /// <summary>
    /// HomeUI シーンへホーム画面 UI (プレハブ) を配置する。
    /// UISceneBootstrap で Additive ロードするホーム専用シーンを作る/更新するためのメニュー。
    /// </summary>
    [MenuItem("NeverNight/Setup/9. Build HomeUI Scene (Home)", false, 28)]
    public static void BuildHomeUIScene()
    {
        var homePrefab = GetOrCreateHomeUIPrefab(GetOrCreateJapaneseFont());
        RebuildUIScene(HomeUIScenePath, homePrefab);
        Debug.Log($"[TutorialSceneBuilder] {HomeUIScenePath} にホーム画面 UI を配置しました。" +
                  "ステージシーン側に UISceneBootstrap を置くと Additive でロードされます。");
    }

    [MenuItem("NeverNight/Setup/10. Build GameOverUI Scene", false, 29)]
    public static void BuildGameOverUIScene()
    {
        var prefab = GetOrCreateGameOverUIPrefab(GetOrCreateJapaneseFont());
        RebuildUIScene(GameOverUIScenePath, prefab);
        Debug.Log($"[TutorialSceneBuilder] {GameOverUIScenePath} にゲームオーバー UI を配置しました。");
    }

    [MenuItem("NeverNight/Setup/11. Build ResultUI Scene", false, 30)]
    public static void BuildResultUIScene()
    {
        var prefab = GetOrCreateResultUIPrefab(GetOrCreateJapaneseFont());
        RebuildUIScene(ResultUIScenePath, prefab);
        Debug.Log($"[TutorialSceneBuilder] {ResultUIScenePath} にリザルト UI を配置しました。");
    }

    /// <summary>
    /// UI 専用シーンへプレハブを配置し直す。既存シーンなら同名ルートだけ置き換え、
    /// 他のオブジェクトは保持する。シーンが無ければ新規作成する。
    /// </summary>
    private static void RebuildUIScene(string scenePath, GameObject prefab)
    {
        UnityEngine.SceneManagement.Scene scene;
        if (File.Exists(scenePath))
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                if (sceneRoot.name == prefab.name)
                    Object.DestroyImmediate(sceneRoot);
            }
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        PrefabUtility.InstantiatePrefab(prefab);
        EditorSceneManager.SaveScene(scene, scenePath);
        AddSceneToBuildSettings(scenePath);
    }

    /// <summary>
    /// 手作業で作成したステージシーンをセットアップする (PlayerScene 方式)。開いているシーンに対して:
    /// - PlayerSpawnPoint: 開始位置マーカーを配置 (プレイヤー本体は PlayerScene に常駐するため置かない)
    /// - PlayerSceneBootstrap: エディタでこのステージから直接再生しても PlayerScene が Additive で乗る
    /// - UI シーン (PlayerUI/PauseUI/HomeUI/GameOverUI/ResultUI) の確保
    /// - SceneUI: エリア名 (既にあれば保持)
    /// を整え、Build Settings へ登録する。何度実行しても安全。実行前にシーンを保存しておくこと。
    /// </summary>
    [MenuItem("NeverNight/Setup/7. Setup Current Stage Scene", false, 26)]
    public static void SetupCurrentStageScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError("[TutorialSceneBuilder] シーンが未保存です。先に保存してから実行してください。");
            return;
        }

        var jpFont = GetOrCreateJapaneseFont();

        // ---- PlayerScene 方式: ステージにはプレイヤーとカメラを置かない ----
        if (Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include) != null)
            Debug.LogWarning("[TutorialSceneBuilder] ステージ内にプレイヤーが居ます。PlayerScene 方式ではプレイヤーは PlayerScene 側に常駐するため、ステージからは削除してください。");
        if (Camera.main != null)
            Debug.LogWarning("[TutorialSceneBuilder] ステージ内にカメラが居ます。PlayerScene 側のカメラと重複するため削除を推奨します。");

        // ---- 開始位置マーカー (StageLoader がステージロード後にプレイヤーを移動する) ----
        if (Object.FindAnyObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Include) == null)
        {
            var spawn = new GameObject("PlayerSpawnPoint");
            spawn.AddComponent<PlayerSpawnPoint>();
            spawn.transform.position = new Vector3(0f, 1.5f, 0f);
            Debug.Log("[TutorialSceneBuilder] PlayerSpawnPoint を配置しました。開始位置へ移動してください。" +
                      "入り口を増やす場合は複製して _id を設定し、出口 (SceneTransitionZone) の EntranceId と対応させます。");
        }

        // ---- PlayerScene / UI シーンの確保 ----
        if (File.Exists(PlayerScenePath))
            AddSceneToBuildSettings(PlayerScenePath);
        else
            Debug.LogWarning("[TutorialSceneBuilder] PlayerScene がありません。\"12. Build Player Scene\" を実行してください。");

        EnsureUIScene(PlayerUIScenePath, GetOrCreateHudPrefab(jpFont));
        EnsureUIScene(PauseUIScenePath, GetOrCreatePauseUIPrefab(jpFont));
        EnsureUIScene(HomeUIScenePath, GetOrCreateHomeUIPrefab(jpFont));
        EnsureUIScene(GameOverUIScenePath, GetOrCreateGameOverUIPrefab(jpFont));
        EnsureUIScene(ResultUIScenePath, GetOrCreateResultUIPrefab(jpFont));

        // ---- ブートストラップ: エディタでこのステージから直接再生しても PlayerScene が乗る
        //      (PlayerScene 側の UISceneBootstrap が UI シーンをロードする) ----
        if (Object.FindAnyObjectByType<UISceneBootstrap>(FindObjectsInactive.Include) == null)
        {
            var bootstrap = new GameObject("PlayerSceneBootstrap").AddComponent<UISceneBootstrap>();
            SetStringArray(bootstrap, "_uiSceneNames", new[] { StageLoader.PlayerSceneName });
        }

        // ---- シーン固有 UI (エリア名)。手直しを保持するため既存なら触らない ----
        if (GameObject.Find("SceneUI") == null)
            BuildStageSceneUI(scene.name, jpFont);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings(scene.path);
        Debug.Log($"[TutorialSceneBuilder] ステージシーン '{scene.name}' をセットアップしました。");
    }

    /// <summary>ステージシーン用の SceneUI (エリア名のみ。ミニマップ等は PlayerScene 側)。</summary>
    private static void BuildStageSceneUI(string areaName, TMP_FontAsset jpFont)
    {
        var sceneUiGo = new GameObject("SceneUI");
        var canvas = sceneUiGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = sceneUiGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var root = (RectTransform)sceneUiGo.transform;

        var areaRoot = CreateUIObject("AreaTitle", root);
        AnchorCenter(areaRoot, new Vector2(0f, 160f), new Vector2(1200f, 100f));
        var areaLabel = CreateHudText(areaRoot, "Label", "", 64f, new Color(0.95f, 0.95f, 1f), jpFont);
        areaLabel.fontStyle = FontStyles.Bold;
        StretchWithPadding(areaLabel.rectTransform, 0f);
        areaRoot.gameObject.AddComponent<CanvasGroup>();
        var areaView = areaRoot.gameObject.AddComponent<AreaTitleView>();
        SetString(areaView, "_areaName", areaName);
        SetRef(areaView, "_label", areaLabel);
    }

    /// <summary>
    /// PlayerScene (プレイヤー常駐シーン) を構築する。プレイヤー・カメラ・StageLoader・
    /// UISceneBootstrap・ミニマップを含み、この上へステージと UI が Additive で重なる。
    /// ステージ入替でプレイヤーが破棄されないため、ステータスが維持される。
    /// </summary>
    [MenuItem("NeverNight/Setup/12. Build Player Scene", false, 31)]
    public static void BuildPlayerScene()
    {
        var playerPrefab = GetOrCreatePlayerPrefab();
        if (playerPrefab == null)
        {
            Debug.LogError("[TutorialSceneBuilder] Player プレハブを用意できません。先に \"2. Create Assets & Prefabs\" を実行してください。");
            return;
        }

        var jpFont = GetOrCreateJapaneseFont();
        var square = GetSquareSprite();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- プレイヤー (常駐) ----
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(0f, 1.5f, 0f);

        // ---- カメラ ----
        var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.14f, 0.14f, 0.2f);
        cameraGo.AddComponent<AudioListener>();
        var follow = cameraGo.AddComponent<CameraFollow>();
        cameraGo.transform.position = new Vector3(0f, 3.5f, -10f);
        SetRef(follow, "_target", player.transform);

        // ---- ステージ / UI シーンのロード ----
        new GameObject("StageLoader").AddComponent<StageLoader>();
        new GameObject("UISceneBootstrap").AddComponent<UISceneBootstrap>();

        // ミニマップは HUD プレハブ (PlayerUI) 側に含まれる (ターゲットは実行時解決)

        EditorSceneManager.SaveScene(scene, PlayerScenePath);
        AddSceneToBuildSettings(PlayerScenePath);
        Debug.Log($"[TutorialSceneBuilder] {PlayerScenePath} を構築しました。ステージと UI はこの上に Additive で重なります。");
    }

    /// <summary>
    /// UI 専用シーンが無ければ、開いているシーンを閉じずに Additive で生成して保存する。
    /// </summary>
    private static void EnsureUIScene(string scenePath, GameObject prefab)
    {
        if (File.Exists(scenePath))
        {
            AddSceneToBuildSettings(scenePath);
            return;
        }

        var uiScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        PrefabUtility.InstantiatePrefab(prefab, uiScene);
        EditorSceneManager.SaveScene(uiScene, scenePath);
        EditorSceneManager.CloseScene(uiScene, true);
        AddSceneToBuildSettings(scenePath);
    }

    #endregion

    #region UI Helpers

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image CreateUIImage(RectTransform parent, string name, Color color, Sprite sprite)
    {
        var rt = CreateUIObject(name, parent);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        return image;
    }

    private static TextMeshProUGUI CreateHudText(RectTransform parent, string name, string text,
        float fontSize, Color color, TMP_FontAsset jpFont)
    {
        var rt = CreateUIObject(name, parent);
        var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        if (jpFont != null)
            label.font = jpFont;
        return label;
    }

    private static void MakeFilled(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
    }

    private static void SetRect(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }

    private static void AnchorTopLeft(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }

    private static void AnchorCenter(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }

    private static void StretchWithPadding(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    #endregion

    #region Common Helpers

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        if (layer < 0)
            return;

        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    /// <summary>private [SerializeField] フィールドへ参照を配線する。</summary>
    private static void SetRef(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogError($"[TutorialSceneBuilder] {target.GetType().Name} にフィールド {fieldName} がありません。");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>private [SerializeField] の string 配列フィールドへ値を書き込む。</summary>
    private static void SetStringArray(Object target, string fieldName, string[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogError($"[TutorialSceneBuilder] {target.GetType().Name} にフィールド {fieldName} がありません。");
            return;
        }

        prop.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>private [SerializeField] の string フィールドへ値を書き込む。</summary>
    private static void SetString(Object target, string fieldName, string value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogError($"[TutorialSceneBuilder] {target.GetType().Name} にフィールド {fieldName} がありません。");
            return;
        }

        prop.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>private [SerializeField] の Color フィールドへ値を書き込む。</summary>
    private static void SetColor(Object target, string fieldName, Color value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogError($"[TutorialSceneBuilder] {target.GetType().Name} にフィールド {fieldName} がありません。");
            return;
        }

        prop.colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>private [SerializeField] の enum フィールドへ値を書き込む。</summary>
    private static void SetEnum(Object target, string fieldName, int value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogError($"[TutorialSceneBuilder] {target.GetType().Name} にフィールド {fieldName} がありません。");
            return;
        }

        prop.enumValueIndex = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>private [SerializeField] 配列フィールドへ参照を配線する。</summary>
    private static void SetArray(Object target, string fieldName, Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogError($"[TutorialSceneBuilder] {target.GetType().Name} にフィールド {fieldName} がありません。");
            return;
        }

        prop.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath))
            return;

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    /// <summary>
    /// 仮素材用の正方形スプライトを取得する。2D パッケージの組み込みスプライトを優先し、
    /// 見つからなければ白テクスチャを生成して使う。
    /// </summary>
    /// <summary>円形スプライト (クールダウン円などに使う)。Unity 組み込みの Knob を使う。</summary>
    private static Sprite GetCircleSprite()
    {
        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        return knob != null ? knob : GetSquareSprite();
    }

    private static Sprite GetSquareSprite()
    {
        var candidates = new[]
        {
            "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png",
            "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/Square.png",
        };

        foreach (var path in candidates)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
        }

        // フォールバック: 白い正方形テクスチャを生成する
        const string texPath = "Assets/Art/Placeholder/WhiteSquare.png";
        var generated = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        if (generated != null)
            return generated;

        EnsureDirectory("Assets/Art/Placeholder");
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = Enumerable.Repeat(Color.white, 64 * 64).ToArray();
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texPath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64f;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
    }

    /// <summary>
    /// 日本語 UI 用の TMP フォントアセットを取得する。無ければ OS フォント (游ゴシック等) から
    /// 動的フォントアセットを生成する。生成できなければ null (TMP デフォルトのまま = 日本語が□になる)。
    /// </summary>
    private static TMP_FontAsset GetOrCreateJapaneseFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JpFontPath);
        if (existing != null)
            return existing;

        EnsureDirectory("Assets/Art/Fonts");

        // TMP のバージョンにより存在しない場合があるためリフレクションで呼ぶ:
        // TMP_FontAsset.CreateFontAsset(string familyName, string styleName, int pointSize)
        var method = typeof(TMP_FontAsset).GetMethod(
            "CreateFontAsset",
            new[] { typeof(string), typeof(string), typeof(int) });

        if (method != null)
        {
            var families = new[] { "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo", "MS Gothic" };
            foreach (var family in families)
            {
                try
                {
                    if (method.Invoke(null, new object[] { family, "Regular", 90 }) is not TMP_FontAsset fontAsset)
                        continue;

                    fontAsset.name = Path.GetFileNameWithoutExtension(JpFontPath);
                    AssetDatabase.CreateAsset(fontAsset, JpFontPath);
                    if (fontAsset.material != null)
                        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                    if (fontAsset.atlasTexture != null)
                        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                    AssetDatabase.SaveAssets();

                    Debug.Log($"[TutorialSceneBuilder] 日本語フォントアセットを生成しました: {family}");
                    return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JpFontPath);
                }
                catch
                {
                    // 次の候補フォントを試す
                }
            }
        }

        Debug.LogWarning("[TutorialSceneBuilder] 日本語フォントを生成できませんでした。TMP デフォルトフォントでは日本語が □ になります。");
        return null;
    }

    #endregion
}
