// シーンを開く/作る (塗りとは別の呼び出しで実行する)。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/open.cs --entry Open.Run --args '["A1_T3"]'
public static class Open
{
    public static string Run(string sceneId) => NeverNight.StageBuild.Area1StageBuilder.OpenOrCreate(sceneId);
}
