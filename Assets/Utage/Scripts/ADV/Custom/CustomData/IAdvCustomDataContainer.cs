using System.Collections.Generic;

namespace Utage
{
    public interface IAdvCustomDataContainer
    {
    }

    //動的にロードするファイルがある場合に使用するインターフェース
    public interface IAdvCustomDataContainerAssetFiles
    {
        IEnumerable<AssetFile> GetAllFiles();
    }
}
