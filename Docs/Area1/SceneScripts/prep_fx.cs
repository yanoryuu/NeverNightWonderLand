// 効果素材 (Area1Gen/fx) の生成とインポート。--args '[false]' (true で作り直す)
public static class PrepFx
{
    public static string Run(bool force) => NeverNight.StageBuild.Area1AssetPrep.PrepareFx(force);
}
