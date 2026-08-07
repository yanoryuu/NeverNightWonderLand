using UnityEditor;

namespace Utage
{
    public class AdvScenarioPostProcessImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            //制御エディタを通して、管理対象のデータのみインポートする
            AdvScenarioDataBuilderWindow.Import(importedAssets);
        }
    }
}
