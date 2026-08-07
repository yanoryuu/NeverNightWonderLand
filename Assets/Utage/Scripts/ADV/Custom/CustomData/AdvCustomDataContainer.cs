using System.Collections.Generic;

namespace Utage
{
    public abstract class AdvCustomDataContainer
    {
        public AdvCustomDataManager CustomDataManager { get; }

        protected List<StringGrid> DataList { get; }= new ();

        protected AdvCustomDataContainer(AdvCustomDataManager customDataManager)
        {
            CustomDataManager = customDataManager;
        }
        public virtual void AddAndInitData(StringGrid customData)
        {
            DataList.Add(customData);
        }
    }
}
