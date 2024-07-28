using System;
using ViewSystem.Button.Animation;

namespace ViewSystem.Button
{
    public class BaseButton<TButtonAnimation> : AbstractButton where TButtonAnimation : IButtonAnimation
    {
        protected TButtonAnimation Animation;

        private Action _pressAction;

        public void BindPressAction(Action action)
        {
            _pressAction = action;
        }
        
        public void Clear()
        {
            Animation?.Clear();
            _pressAction = null;
        }
        
        public override void Activate()
        {
            Animation?.AnimatePress(_pressAction);
        }
    }
}
