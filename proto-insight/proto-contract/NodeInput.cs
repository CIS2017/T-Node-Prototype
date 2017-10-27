using System;

namespace proto_contract
{
    public abstract class NodeInput
    {
        public DateTimeOffset? Timestamp { get; set; }
        public String EventType { get { return this.GetType().Name.ToLower(); } }
        public string NodeId { get; set; }
        public abstract override string ToString();
    }

    public class DigitalNodeInput : NodeInput
    {
        public enum EdgeType { Rising, Falling }
        public int InputIndex { get; set; }
        public EdgeType Edge { get; set; }

        public override string ToString()
        {
            return $"DI: {InputIndex}_{Edge} @ {Timestamp:yyyy.MM.dd HH.mm.ss.fff zzz}";
        }
    }

    public class BarcodeNodeInput : NodeInput
    {
        public string Barcode { get; set; }
        public override string ToString()
        {
            return $"B: {Barcode} @ {Timestamp:yyyy.MM.dd HH.mm.ss.fff zzz}";
        }
    }
}
