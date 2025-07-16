namespace Base.EffectSystem.Base
{
    public abstract class BaseEffectWithInputData<TEffectData> : BaseEffect, IEffectWithInputData<TEffectData> 
        where TEffectData : IEffectData
    {
        public virtual void Initialize(TEffectData data)
        {
            base.Initialize();
        }
    }
}
