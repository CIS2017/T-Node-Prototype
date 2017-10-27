using proto_contract;
using System;
using System.Collections.Generic;
using System.Text;

namespace proto_domain
{
    public class ProtoCoreLine
    {
        private Dictionary<Type, Action<NodeInput>> inputHandlers;

        public ProtoCoreLine()
        {
            this.inputHandlers = new Dictionary<Type, Action<NodeInput>>();
            inputHandlers.Add(typeof(DigitalNodeInput), HandleNodeInput_Digital);
            inputHandlers.Add(typeof(BarcodeNodeInput), HandleNodeInput_Barcode);

            DigitalInputs = new Dictionary<int, bool>();

            LineState = new PlannedEvent("Boot");
        }
        public void HandleNodeInput(NodeInput input)
        {
            var inputType = input.GetType();
            inputHandlers[inputType](input);

            StateChange?.Invoke(this, this);
        }

        private void HandleNodeInput_Digital(NodeInput input)
        {
            var di = (DigitalNodeInput)input;
            if(di == null) { throw new ArgumentException("Expected DigitalNodeInput", nameof(input)); }

            if(!DigitalInputs.ContainsKey(di.InputIndex))
            {
                DigitalInputs.Add(di.InputIndex, false);
            }

            DigitalInputs[di.InputIndex] = di.Edge == DigitalNodeInput.EdgeType.Rising ? true : false;

            if(LineState is WorkEvent)
            {
                (LineState as WorkEvent).HandleDigitalInput(di);
            }
        }

        private void HandleNodeInput_Barcode(NodeInput input)
        {
            var bi = (BarcodeNodeInput)input;
            if (bi == null) { throw new ArgumentException("Expected BarcodeNodeInput", nameof(input)); }

            this.LastBarcode = bi.Barcode;

            if(bi.Barcode?.Equals("PE:IDLE") == true)
            {
                this.LineState = new PlannedEvent("Idle");
            }

            if (bi.Barcode?.Equals("WE:WORK") == true)
            {
                this.LineState = new WorkEvent("Sample Part");
            }
        }

        public event EventHandler<ProtoCoreLine> StateChange;

        public Dictionary<int, bool> DigitalInputs { get; set; }
        public string LastBarcode { get; private set; }
        public ILineState LineState { get; private set; }
    }

    public interface ILineState
    {
        string StateName { get; }
        string StateType { get; }
    }

    public class WorkEvent : ILineState
    {
        public WorkEvent(string name)
        {
            this.StateName = name;
            this.GoodCount = 0;
            this.RejectCount = 0;
        }

        public string StateName { get; set; }
        public string StateType { get => "Work"; }
        public decimal GoodCount { get; private set; }
        public decimal RejectCount { get; private set; }

        public void HandleDigitalInput(DigitalNodeInput di)
        {
            if (di.Edge == DigitalNodeInput.EdgeType.Rising)
            {
                if (di.InputIndex == 0) { GoodCount++; }
                if (di.InputIndex == 1) { RejectCount++; }
            }
        }
    }

    public class PlannedEvent : ILineState
    {
        public PlannedEvent(string name)
        {
            this.StateName = name;
        }

        public string StateName { get; set; }
        public string StateType { get => "Planned"; }
    }
}
