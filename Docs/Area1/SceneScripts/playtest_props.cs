// プロップス素材・糸の粒・コイン・箱の一つずつ破壊 (2026-10-03 PendingChanges 14〜17) の再生中の確認。
// シーンを開いて editor_play してから run_script で呼ぶ。操作は仮想入力 (VirtualPlayerInput)。
// 例: --entry PlayProps.State / --entry PlayProps.Tp --args '[145,-95.4]' / --entry PlayProps.Move --args '[-1]'
//     --entry PlayProps.Attack / --entry PlayProps.KillNearest / --entry PlayProps.KillBoss / --entry PlayProps.Pieces
//     --entry PlayProps.Blocks --args '[142.5,-94,4]' / --entry PlayProps.Shot --args '["props_x"]'
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayProps
{
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

    static PlayerController P => Object.FindAnyObjectByType<PlayerController>();

    public static string State()
    {
        Application.runInBackground = true;
        var p = P;
        if (p == null) return "no player";
        var inv = p.Inventory;
        var sb = new StringBuilder();
        sb.Append($"pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) grounded={p.IsGrounded} facing={p.Facing} ");
        sb.Append($"thread={inv.Thread.CurrentValue} coins={inv.Coins.CurrentValue} stage={(StageLoader.Instance ? StageLoader.Instance.CurrentStageName : SceneManager.GetActiveScene().name)} t={Time.time:F1}");
        sb.Append($" pieces={Object.FindObjectsByType<LootPiece>(FindObjectsSortMode.None).Length}");
        var spawner = p.GetComponent<ThreadDropSpawner>();
        sb.Append($" spawner={(spawner != null)}");
        return sb.ToString();
    }

    public static string Tp(float x, float y)
    {
        Input.Move = 0f;
        P.Teleport(new Vector2(x, y));
        return "tp " + State();
    }

    /// <summary>左右入力を入れたままにする (0 で離す)。向きを変えるだけなら Move(±1) → Move(0)。</summary>
    public static string Move(float dir)
    {
        Input.Move = dir;
        return "move " + dir + " " + State();
    }

    /// <summary>空中から落下攻撃 (↓ + 攻撃)。先に Tp で空中へ。Vertical は Neutral() で戻す。</summary>
    public static string Slam()
    {
        Input.Vertical = -1f;
        Input.Tap(PlayerButton.Attack);
        return "slam " + State();
    }

    /// <summary>空中にテレポートして同じフレームで落下攻撃を入れる。</summary>
    public static string TpSlam(float x, float y)
    {
        var r = Tp(x, y);
        // 着地判定が空中に切り替わってから入力する (同じフレームだと地上の攻撃になる)
        DG.Tweening.DOVirtual.DelayedCall(0.06f, () => Slam(), false);
        DG.Tweening.DOVirtual.DelayedCall(1.5f, () => Neutral(), false);
        return r;
    }

    const string LogPath = "Temp/playtest_props_log.txt"; // run_script は呼び出しごとに別アセンブリなので static は残らない

    static string StateName()
    {
        var sm = typeof(PlayerController).GetField("_stateMachine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(P) as PlayerStateMachine;
        return sm?.CurrentState?.GetType().Name ?? "?";
    }

    /// <summary>TpSlam の間の状態の記録 (0.1 秒ごと 2 秒)。</summary>
    public static string TpSlamLog(float x, float y)
    {
        System.IO.File.WriteAllText(LogPath, "");
        var r = TpSlam(x, y);
        for (var i = 1; i <= 20; i++)
            DG.Tweening.DOVirtual.DelayedCall(0.1f * i, () => System.IO.File.AppendAllText(LogPath, $"{Time.time:F2} {StateName()} y={P.transform.position.y:F2} v={Input.Vertical} grounded={P.IsGrounded}\n"), false);
        return r;
    }

    public static string Log() => System.IO.File.ReadAllText(LogPath);

    public static string Neutral()
    {
        Input.Vertical = 0f;
        Input.Move = 0f;
        return "neutral " + State();
    }

    public static string Attack()
    {
        Input.Tap(PlayerButton.Attack);
        return "attack " + State();
    }

    public static string KillNearest()
    {
        var p = P;
        var e = EnemyController.Active.Where(x => x != null && !x.IsDead)
            .OrderBy(x => Vector2.Distance(x.transform.position, p.transform.position)).FirstOrDefault();
        if (e == null) return "no enemy";
        var at = e.transform.position;
        e.TakeDamage(new DamageInfo(9999, 9999, at, p.gameObject));
        return $"killed {e.name} at ({at.x:F1},{at.y:F1}) scale={e.transform.lossyScale.y:F1} dead={e.IsDead}";
    }

    public static string KillBoss()
    {
        var p = P;
        var e = EnemyController.Active.Where(x => x != null && !x.IsDead && x.transform.lossyScale.y > 1.5f).FirstOrDefault();
        if (e == null) return "no boss";
        var at = e.transform.position;
        e.TakeDamage(new DamageInfo(9999, 9999, at, p.gameObject));
        return $"killed boss {e.name} at ({at.x:F1},{at.y:F1}) scale={e.transform.lossyScale.y:F1} dead={e.IsDead}";
    }

    public static string Enemies()
    {
        var p = P;
        return string.Join("\n", EnemyController.Active.Where(x => x != null)
            .OrderBy(x => Vector2.Distance(x.transform.position, p.transform.position))
            .Select(x => $"{x.name} ({x.transform.position.x:F1},{x.transform.position.y:F1}) scale={x.transform.lossyScale.y:F1}"));
    }

    public static string Pieces()
    {
        var list = Object.FindObjectsByType<LootPiece>(FindObjectsSortMode.None);
        return $"{list.Length} pieces\n" + string.Join("\n", list.Select(l =>
        {
            var rb = l.GetComponent<Rigidbody2D>();
            var v = l is ThreadPiece t ? $"thread v={t.Value}" : l is CoinPiece c ? "coin " + c.CoinId : l.GetType().Name;
            return $"{v} pos=({l.transform.position.x:F2},{l.transform.position.y:F2}) vel=({rb.linearVelocity.x:F2},{rb.linearVelocity.y:F2}) sim={rb.simulated} awake={rb.IsAwake()}";
        }));
    }

    /// <summary>(x, y) の半径 r 内の壊せる物 (BreakableBox / SkillBreakable)。</summary>
    public static string Blocks(float x, float y, float r)
    {
        var c = new Vector2(x, y);
        var boxes = Object.FindObjectsByType<BreakableBox>(FindObjectsSortMode.None).Where(b => Vector2.Distance(b.transform.position, c) <= r)
            .Select(b => $"box {b.name} ({b.transform.position.x:F2},{b.transform.position.y:F2})");
        var skills = Object.FindObjectsByType<SkillBreakable>(FindObjectsSortMode.None).Where(b => Vector2.Distance(b.transform.position, c) <= r)
            .Select(b => $"skill {b.transform.parent?.name}/{b.name}");
        var all = boxes.Concat(skills).ToList();
        return $"{all.Count} blocks\n" + string.Join("\n", all);
    }

    public static string Exists(string name) => $"{name} exists={GameObject.Find(name) != null}";
    public static string Flag(string f) { GameProgress.Set(f); return "set " + f; }
    public static string Unflag(string f) { GameProgress.Unset(f); return "unset " + f; }
    public static string Has(string f) => $"{f}={GameProgress.Has(f)}";
    public static string Go(string scene, string entrance) { StageLoader.Instance.TransitionTo(scene, entrance); return "go " + scene; }
    public static string SetSkill(int skill) { P.GetComponent<PlayerProgression>().GrantSkill((PlayerSkill)skill); return "skill " + (PlayerSkill)skill; }

    /// <summary>アイテム (Assets/Items/&lt;name&gt;.asset) を n 個持たせる。</summary>
    public static string GiveItem(string name, int n)
    {
        var inv = P.Inventory;
        var item = inv.Catalog.FirstOrDefault(i => i != null && i.name == name);
        if (item == null) return "no item " + name;
        for (var i = 0; i < n; i++) inv.TryAddItem(item);
        return $"{name} x{inv.GetCount(item)}";
    }

    /// <summary>セーブデータへの出し入れ: Collect の coins と、coins を変えたデータの LoadFrom で枚数が戻ること。</summary>
    public static string SaveCheck()
    {
        var bridge = P.GetComponent<PlayerSaveBridge>();
        var data = bridge.Collect();
        var collected = data.coins;
        var json = JsonUtility.ToJson(data);
        var back = JsonUtility.FromJson<SaveData>(json);
        back.coins = collected + 2;
        P.Inventory.LoadFrom(back);
        var loaded = P.Inventory.Coins.CurrentValue;
        back.coins = collected;
        P.Inventory.LoadFrom(back); // 元に戻す
        return $"collect coins={collected} json has coins={json.Contains("\"coins\":" + collected)} load(+2)={loaded} restored={P.Inventory.Coins.CurrentValue} flags={string.Join(",", data.progressFlags.Where(f => f.StartsWith("A1_Coin_")))}";
    }

    public static string ItemMenu() { Input.Tap(PlayerButton.ItemMenu); return "itemmenu"; }
    public static string MenuTab(int d) { Input.MenuTab(d); return "tab " + d; }
    public static string MenuMoveX(int d) { Input.MenuMoveX(d); return "movex " + d; }
    public static string MenuMove(int d) { Input.MenuMove(d); return "move " + d; }
    public static string Submit() { Input.MenuSubmit(); return "submit"; }
    public static string Cancel() { Input.MenuCancel(); return "cancel"; }

    /// <summary>何秒か後にスクリーンショット (DOTween の遅延呼び出し。カンマ区切りで複数)。</summary>
    public static string ShotsAfter(string name, string secondsCsv)
    {
        Application.runInBackground = true;
        System.IO.Directory.CreateDirectory("Assets/Screenshots/Area1");
        foreach (var s in secondsCsv.Split(','))
        {
            var sec = float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            var file = $"Assets/Screenshots/Area1/{name}_{s.Replace('.', 'p')}s.png";
            DG.Tweening.DOVirtual.DelayedCall(sec, () => ScreenCapture.CaptureScreenshot(file), false);
        }
        return "scheduled " + name;
    }

    /// <summary>テレポートして何秒か後に撮る (拾った直後の通知を撮る用)。</summary>
    public static string TpShots(float x, float y, string name, string secondsCsv)
    {
        var r = Tp(x, y);
        ShotsAfter(name, secondsCsv);
        return r;
    }

    /// <summary>攻撃して何秒か後に撮る。</summary>
    public static string AttackShots(string name, string secondsCsv)
    {
        var r = Attack();
        ShotsAfter(name, secondsCsv);
        return r;
    }

    /// <summary>一番近い敵を倒して、粒が散る様子を撮る (0.15 / 0.5 / 2.5 秒後)。</summary>
    public static string KillNearestShots(string name)
    {
        var r = KillNearest();
        ShotsAfter(name, "0.15,0.5,2.5");
        return r;
    }

    public static string KillBossShots(string name)
    {
        var r = KillBoss();
        ShotsAfter(name, "0.15,0.5,2.5");
        return r;
    }

    public static string Shot(string name)
    {
        Application.runInBackground = true;
        System.IO.Directory.CreateDirectory("Assets/Screenshots/Area1");
        ScreenCapture.CaptureScreenshot($"Assets/Screenshots/Area1/{name}.png");
        return "shot " + name;
    }
}
