// SceneView を部屋に寄せる (この後 capture_scene_view で撮る)。
// unity command run_script ... --file Docs/Area1/SceneScripts/frame.cs --entry Frame.Room --args '["T2"]'
public static class Frame
{
    public static string Room(string roomId) => NeverNight.StageBuild.Area1StageBuilder.FrameRoom(roomId, 2f);
    public static string Describe(string sceneId) => NeverNight.StageBuild.Area1StageBuilder.Describe(sceneId);
    public static string Validate() => NeverNight.StageBuild.Area1StageBuilder.Validate();
}
// SceneView を任意の位置・倍率に (halfH 5.5 = ゲームのカメラと同じ縦 11)
public static class FrameAt
{
    public static string Run(float cx, float cy, float halfH) => NeverNight.StageBuild.Area1StageBuilder.FrameSceneView(cx, cy, halfH);
}
