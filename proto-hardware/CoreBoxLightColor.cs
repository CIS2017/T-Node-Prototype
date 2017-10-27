using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proto_ui
{
    public class CoreBoxLightColor
    {
        private string colorId;
        private byte[] byteValue;

        private CoreBoxLightColor(string colorId, byte[] lineValue)
        {
            this.colorId = colorId;
            this.byteValue = lineValue;
        }

        public static CoreBoxLightColor[] All
        {
            get
            {
                return new[] {
                    CoreBoxLightColor.Off,
                    CoreBoxLightColor.Blue,
                    CoreBoxLightColor.Green,
                    CoreBoxLightColor.Yellow,
                    CoreBoxLightColor.Red,
                    CoreBoxLightColor.CycleBlueOff,
                    CoreBoxLightColor.CycleGreenOff,
                    CoreBoxLightColor.CycleYellowOff,
                    CoreBoxLightColor.CycleRedOff,
                    CoreBoxLightColor.CycleGreenRed,
                    CoreBoxLightColor.CycleRedBlue,
                    CoreBoxLightColor.CycleGreenYellow,
                    CoreBoxLightColor.CycleGreenBlue,
                    CoreBoxLightColor.CycleRedYellow,
                    CoreBoxLightColor.CycleYellowBlue,
                    CoreBoxLightColor.CycleAll
                };
            }
        }

        public static CoreBoxLightColor FromString(string colorName)
        {
            return All.SingleOrDefault(s => s.colorId == colorName);
        }

        public static CoreBoxLightColor Off { get { return new CoreBoxLightColor("off", new byte[] { 0x00 }); } }
        public static CoreBoxLightColor Blue { get { return new CoreBoxLightColor("blue", new byte[] { 0x20 }); } }
        public static CoreBoxLightColor Green { get { return new CoreBoxLightColor("green", new byte[] { 0x10 }); } }
        public static CoreBoxLightColor Yellow { get { return new CoreBoxLightColor("yellow", new byte[] { 0x08 }); } }
        public static CoreBoxLightColor Red { get { return new CoreBoxLightColor("red", new byte[] { 0x04 }); } }
        public static CoreBoxLightColor CycleBlueOff { get { return new CoreBoxLightColor("cycle-blue-off", new byte[] { 0x24 }); } }
        public static CoreBoxLightColor CycleGreenOff { get { return new CoreBoxLightColor("cycle-green-off", new byte[] { 0x18 }); } }
        public static CoreBoxLightColor CycleYellowOff { get { return new CoreBoxLightColor("cycle-yellow-off", new byte[] { 0x0C }); } }
        public static CoreBoxLightColor CycleRedOff { get { return new CoreBoxLightColor("cycle-red-off", new byte[] { 0x14 }); } }
        public static CoreBoxLightColor CycleGreenRed { get { return new CoreBoxLightColor("cycle-green-red", new byte[] { 0x30 }); } }
        public static CoreBoxLightColor CycleRedBlue { get { return new CoreBoxLightColor("cycle-red-blue", new byte[] { 0x28 }); } }
        public static CoreBoxLightColor CycleGreenYellow { get { return new CoreBoxLightColor("cycle-green-yellow", new byte[] { 0x38 }); } }
        public static CoreBoxLightColor CycleGreenBlue { get { return new CoreBoxLightColor("cycle-green-blue", new byte[] { 0x34 }); } }
        public static CoreBoxLightColor CycleRedYellow { get { return new CoreBoxLightColor("cycle-red-yellow", new byte[] { 0x2C }); } }
        public static CoreBoxLightColor CycleYellowBlue { get { return new CoreBoxLightColor("cycle-yellow-blue", new byte[] { 0x1C }); } }
        public static CoreBoxLightColor CycleAll { get { return new CoreBoxLightColor("cycle-all", new byte[] { 0x3C }); } }

        public static bool operator ==(CoreBoxLightColor a, CoreBoxLightColor b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        public static bool operator !=(CoreBoxLightColor a, CoreBoxLightColor b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            CoreBoxLightColor other = obj as CoreBoxLightColor;
            if (other == null) return false;

            return other.colorId == this.colorId;
        }

        public override int GetHashCode()
        {
            return this.colorId.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("{0}", this.colorId);
        }

        public byte[] GetBytes()
        {
            return this.byteValue;
        }
    }
}
