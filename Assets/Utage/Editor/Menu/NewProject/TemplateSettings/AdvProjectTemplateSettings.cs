using UnityEditor;
using UnityEngine;

namespace Utage
{
    //プロジェクト作成時のテンプレート設定
    public abstract class AdvProjectTemplateSettings 
        : ScriptableObject, IAdvProjectTemplateSettings
    {
        public DefaultAsset TemplateFolder => templateFolder;
        [SerializeField] DefaultAsset templateFolder = null;
        
        public abstract AdvProjectCreator CreateProjectCreatorSettings();

        public virtual bool IsEnableCreate()
        {
            return TemplateFolder != null;
        }
    }
}
