namespace Utage
{
    public interface IAdvEngineCustomEvent
    {
    }

    public interface IAdvEngineCustomEventBootInit : IAdvEngineCustomEvent
    {
        public void OnBootInit();
    }
}
