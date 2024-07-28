using System;
using ViewSystem.Button;
using Zenject;

namespace ViewSystem.SignalCommand
{
    public class AbstractSignalButtonCommand<TClassSignal> : AbstractButton where TClassSignal : class, new()
    {
        private SignalBus _signalBus;

        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }
        
        public override void Activate()
        {
            _signalBus.Fire(new TClassSignal());
        }
    }

    public class AbstractSignalButtonCommand<TClassSignal, TData1> : AbstractButton where TClassSignal : class
    {
        private SignalBus _signalBus;

        private TData1 _data1;
        
        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }
        
        public void Bind(TData1 data1)
        {
            _data1 = data1;
        }
        
        public void Unbind()
        {
            _data1 = default(TData1);
        }

        public override void Activate()
        {
            _signalBus.Fire((TClassSignal)Activator.CreateInstance(typeof(TClassSignal), _data1));
        }
    }

    public class AbstractSignalButtonCommand<TClassSignal, TData1, TData2> : AbstractButton where TClassSignal : class
    {
        private SignalBus _signalBus;

        private TData1 _data1;
        private TData2 _data2;
        
        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }
        
        public void Bind(TData1 data1, TData2 data2)
        {
            _data1 = data1;
            _data2 = data2;
        }
        
        public void Unbind()
        {
            _data1 = default(TData1);
            _data2 = default(TData2);
        }

        public override void Activate()
        {
            _signalBus.Fire((TClassSignal)Activator.CreateInstance(typeof(TClassSignal), _data1, _data2));
        }
    }

    public class AbstractSignalButtonCommand<TClassSignal, TData1, TData2, TData3> : AbstractButton where TClassSignal : class
    {
        private SignalBus _signalBus;

        private TData1 _data1;
        private TData2 _data2;
        private TData3 _data3;
        
        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }
        
        public void Bind(TData1 data1, TData2 data2, TData3 data3)
        {
            _data1 = data1;
            _data2 = data2;
            _data3 = data3;
        }
        
        public void Unbind()
        {
            _data1 = default(TData1);
            _data2 = default(TData2);
            _data3 = default(TData3);
        }

        public override void Activate()
        {
            _signalBus.Fire((TClassSignal)Activator.CreateInstance(typeof(TClassSignal), _data1, _data2, _data3));
        }
    }
}
