using UnityEditor;

namespace Utage
{
    public interface IAdvProjectTemplateSettings
    {
        
    }

    public interface IAdvProjectTemplateSettingsScene : IAdvProjectTemplateSettings
    {
        SceneAsset Scene { get; }
    }
}
