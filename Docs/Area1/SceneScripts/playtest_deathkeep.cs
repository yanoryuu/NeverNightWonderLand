// 2026-10-04 PendingChanges 38 (死亡すると赤ハサミなどの能力が消える) の再生中の確認。Play 中に run_script で呼ぶ。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/playtest_deathkeep.cs --entry DeathKeep.State
//   Backup / Restore : 実際のセーブ (persistentDataPath/save_*.json) を退避 / 元に戻す (テストの前後に必ず呼ぶ)
//   Start(stage)     : はじめから相当 (このプレイのセーブ無し) で stage を PlayerScene ごと開始
//   Grant            : 赤・黄ハサミ + 落下攻撃 + 大ジャンプ、糸 +15、コイン +2、フラグ DeathKeepTest
//   GrantRush        : 横突進だけ取る (セーブ後に取った能力の書き足しの確認)
//   Kill             : 致死ダメージ → DeadState → ゲームオーバー画面
//   Retry            : ゲームオーバー画面の「リトライ」 (GameOverView.Retry)
//   OldRetry         : 修正前のリトライの再現 (セーブが別の部屋 → PendingLoad 無しで PlayerScene ごと読み直す)
//   BenchSave(slot)  : 拠点のセーブと同じ (SavePoint の位置でスロットへ書く, SaveBelongsToRun = true)
//   Continue(slot)   : タイトルの「つづきから」と同じ
//   SaveInfo(slot)   : スロットのセーブの中身 (シーン・位置・能力)
//   DoubleJump / DJResult : 地上でジャンプ → 0.3 秒後に空中でもう一度ジャンプ、最高到達高さ (二段なら約 6.6, 一段なら約 3.3)
// シーン移動は PlayBox.Go (playtest_boxfix.cs)。
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeathKeep
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static string BackupDir => Path.Combine(Application.persistentDataPath, "deathkeep_backup");

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

    static PlayerController P => UnityEngine.Object.FindAnyObjectByType<PlayerController>();

    public static string Backup()
    {
        Directory.CreateDirectory(BackupDir);
        var log = "";
        for (var i = 0; i < SaveSystem.SlotCount; i++)
        {
            var src = SaveSystem.PathOf(i);
            var dst = Path.Combine(BackupDir, Path.GetFileName(src));
            var marker = dst + ".none";
            if (File.Exists(dst) || File.Exists(marker)) { log += $"slot{i + 1}: already backed up; "; continue; }
            if (File.Exists(src)) { File.Copy(src, dst); log += $"slot{i + 1}: copied; "; }
            else { File.WriteAllText(marker, ""); log += $"slot{i + 1}: none; "; }
        }
        return log + " dir=" + BackupDir;
    }

    public static string Restore()
    {
        var log = "";
        for (var i = 0; i < SaveSystem.SlotCount; i++)
        {
            var src = SaveSystem.PathOf(i);
            var bak = Path.Combine(BackupDir, Path.GetFileName(src));
            if (File.Exists(bak)) { File.Copy(bak, src, true); File.Delete(bak); log += $"slot{i + 1}: restored; "; }
            else if (File.Exists(bak + ".none")) { if (File.Exists(src)) File.Delete(src); File.Delete(bak + ".none"); log += $"slot{i + 1}: removed test file; "; }
            else log += $"slot{i + 1}: no backup!; ";
        }
        return log;
    }

    static string StateName()
    {
        var sm = typeof(PlayerController).GetField("_stateMachine", F)?.GetValue(P) as PlayerStateMachine;
        return sm?.CurrentState?.GetType().Name ?? "?";
    }

    public static string State()
    {
        Application.runInBackground = true;
        var p = P;
        if (p == null) return "no player";
        var pr = p.GetComponent<PlayerProgression>();
        var inv = p.GetComponent<PlayerItemInventory>();
        var hp = p.GetComponent<PlayerHealth>();
        var ups = string.Join(",", Enum.GetValues(typeof(ScissorUpgrade)).Cast<ScissorUpgrade>().Where(pr.Has));
        var sks = string.Join(",", Enum.GetValues(typeof(PlayerSkill)).Cast<PlayerSkill>().Where(pr.HasSkill));
        return $"stage={StageLoader.Instance?.CurrentStageName} pos=({p.transform.position.x:F2},{p.transform.position.y:F2}) state={StateName()} " +
               $"hp={hp.Hp.CurrentValue}/{hp.MaxHp} upgrades=[{ups}] skills=[{sks}] forge={pr.ForgeLevel} thread={inv.Thread.CurrentValue} coins={inv.Coins.CurrentValue} " +
               $"flagTest={GameProgress.Has("DeathKeepTest")} flags={GameProgress.Collect().Length} saveBelongs={GameSession.SaveBelongsToRun} slot={SaveSystem.ActiveSlot + 1} " +
               $"gameOverOpen={GameOverOpen()} t={Time.time:F1}";
    }

    static bool GameOverOpen()
    {
        var go = UnityEngine.Object.FindFirstObjectByType<GameOverView>();
        var menu = go == null ? null : typeof(GameOverView).GetField("_menu", F).GetValue(go) as MenuPanelView;
        return menu != null && menu.IsOpen;
    }

    public static string Start(string stage)
    {
        Application.runInBackground = true;
        GameSession.PendingLoad = null;
        GameSession.SaveBelongsToRun = false;
        GameProgress.Clear();
        StageLoader.LoadWithPlayerScene(stage);
        return "start " + stage;
    }

    public static string Grant()
    {
        var p = P;
        var pr = p.GetComponent<PlayerProgression>();
        pr.Grant(ScissorUpgrade.Red);
        pr.Grant(ScissorUpgrade.Yellow);
        pr.GrantSkill(PlayerSkill.GroundSlam);
        pr.GrantSkill(PlayerSkill.SuperJump);
        var inv = p.GetComponent<PlayerItemInventory>();
        inv.AddThread(15, false);
        inv.AddCoins(2);
        GameProgress.Set("DeathKeepTest");
        return "granted " + State();
    }

    public static string GrantRush()
    {
        P.GetComponent<PlayerProgression>().GrantSkill(PlayerSkill.ChargeRush);
        return "rush " + State();
    }

    public static string Kill()
    {
        var p = P;
        typeof(PlayerHealth).GetField("_invincibleTimer", F)?.SetValue(p.GetComponent<PlayerHealth>(), 0f);
        p.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo(999, 0, p.transform.position, null, false, 0f));
        return "kill " + State();
    }

    public static string Retry()
    {
        var go = UnityEngine.Object.FindFirstObjectByType<GameOverView>();
        if (go == null) return "no GameOverView";
        typeof(GameOverView).GetMethod("Retry", F).Invoke(go, null);
        return "retry";
    }

    public static string OldRetry()
    {
        // 修正前の GameOverView.Retry (セーブが今のステージに無い場合): PendingLoad = null で PlayerScene ごと読み直す
        GameSession.PendingLoad = null;
        StageLoader.LoadWithPlayerScene(StageLoader.Instance.CurrentStageName);
        return "old retry";
    }

    public static string BenchSave(int slot)
    {
        var bridge = P.GetComponent<PlayerSaveBridge>();
        var sp = UnityEngine.Object.FindAnyObjectByType<SavePoint>();
        SaveSystem.ActiveSlot = slot - 1;
        SaveSystem.Save(slot - 1, bridge.CollectAt(sp));
        GameSession.SaveBelongsToRun = true;
        return $"saved slot{slot} at savepoint={(sp ? sp.name + " " + sp.transform.position : "none")} " + SaveInfo(slot);
    }

    public static string SaveInfo(int slot)
    {
        var d = SaveSystem.TryLoad(slot - 1);
        if (d == null) return "no save";
        return $"save{slot}: scene={d.sceneName} pos=({d.posX:F2},{d.posY:F2}) upgrades=[{string.Join(",", (d.upgrades ?? new int[0]).Select(u => (ScissorUpgrade)u))}] " +
               $"skills=[{string.Join(",", (d.skills ?? new int[0]).Select(s => (PlayerSkill)s))}] thread={d.thread} coins={d.coins} flagTest={d.progressFlags?.Contains("DeathKeepTest")}";
    }

    public static string Continue(int slot)
    {
        var save = SaveSystem.TryLoad(slot - 1);
        if (save == null) return "no save";
        SaveSystem.ActiveSlot = slot - 1;
        GameSession.PendingLoad = save;
        GameSession.SaveBelongsToRun = true;
        GameSession.PlayTimeSeconds = save.playTimeSeconds;
        StageLoader.LoadWithPlayerScene(save.sceneName);
        return "continue slot" + slot;
    }

    public static string DoubleJump() => Jumps(true);

    public static string SingleJump() => Jumps(false);

    // 毎フレーム動かす: ジャンプ長押し → 頂点 (vy <= 0) で離す → (二段なら) 次のフレームで押し直す。最高到達点を記録
    class JumpProbe : MonoBehaviour
    {
        public bool Twice;
        public VirtualPlayerInput In;
        public float Y0, Max;
        public int Frame, Phase; // 0 = 1 段目上昇, 1 = 離した, 2 = 2 段目, 3 = 終了
        public string Result = "running";

        void FixedUpdate()
        {
            var p = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (p == null || Phase == 3) return;
            var rb = p.GetComponent<Rigidbody2D>();
            Frame++;
            Max = Mathf.Max(Max, p.transform.position.y);
            if (Phase == 0 && Frame > 6 && rb.linearVelocity.y <= 0f) { In.Release(PlayerButton.Jump); Phase = Twice ? 1 : 2; }
            else if (Phase == 1) { In.Hold(PlayerButton.Jump); Phase = 2; }
            else if (Phase == 2 && In.IsHeld(PlayerButton.Jump) && Frame > 12 && rb.linearVelocity.y <= 0f) In.Release(PlayerButton.Jump); // 滑空させない
            else if (Phase == 2 && Frame > 30 && p.IsGrounded)
            {
                In.ReleaseAll();
                Phase = 3;
                Result = $"rise={Max - Y0:F2} frames={Frame}";
                AppDomain.CurrentDomain.SetData("dk_jump", Result);
            }
        }
    }

    static string Jumps(bool twice)
    {
        var p = P;
        var old = GameObject.Find("DeathKeepJumpProbe");
        if (old != null) UnityEngine.Object.Destroy(old);
        AppDomain.CurrentDomain.SetData("dk_jump", "running");
        var input = new VirtualPlayerInput();
        PlayerInputSource.Override = input;
        var probe = new GameObject("DeathKeepJumpProbe").AddComponent<JumpProbe>();
        probe.Twice = twice;
        probe.In = input;
        probe.Y0 = probe.Max = p.transform.position.y;
        input.Hold(PlayerButton.Jump);
        return $"jump from y={probe.Y0:F2} grounded={p.IsGrounded} twice={twice}";
    }

    public static string DJResult()
    {
        return AppDomain.CurrentDomain.GetData("dk_jump") + " " + State();
    }

    public static string Shot(string name)
    {
        Directory.CreateDirectory("Assets/Screenshots/Area1");
        ScreenCapture.CaptureScreenshot($"Assets/Screenshots/Area1/{name}.png");
        return "shot " + name;
    }

    public static string Off()
    {
        PlayerInputSource.Override = null;
        return "off";
    }
}
