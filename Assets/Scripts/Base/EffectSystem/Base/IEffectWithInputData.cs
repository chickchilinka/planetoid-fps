namespace Base.EffectSystem.Base
{
    public interface IEffectWithInputData<in TEffectData> : IEffect
        where TEffectData : IEffectData
    {
        void Initialize(TEffectData data);
    }
}