using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Additional Fix64 Extensions for SabreCSG and Unity.
    /// </summary>
    public partial struct Fix64
    {
		public static Fix64 Max(params Fix64[] values)
		{
			return Enumerable.Max(values);
		}

        public static Fix64 Min(params Fix64[] values)
        {
            return Enumerable.Min(values);
        }

        public static Fix64 Clamp01(Fix64 value)
        {
            if (value < Zero) return Zero;
            if (value > One) return One;
            return value;
        }

        /// <summary>
        /// Returns a number indicating the sign of a Fix64 number.
        /// Returns 1 if the value is positive, 0 if is 0, and -1 if it is negative.
        /// </summary>
        public static Fix64 FixSign(Fix64 value)
        {
            return value < Fix64.Zero ? -Fix64.One : value > Fix64.Zero ? Fix64.One : Fix64.Zero;
        }

        public static Fix64 operator +(Fix64 a)
        {
            // completely pointless but valid.
            return a;
        }

        /// <summary>
        /// Acos turns out to be an extremely complex number.
        /// There is no fixed point equivalent I know of so I fall back to the default
        /// implementation available to the hardware and C#.
        /// </summary>
        public static Fix64 Acos(Fix64 value)
        {
            return (Fix64)Math.Acos((double)value);
        }

        public static Fix64 Clamp(Fix64 value, Fix64 min, Fix64 max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
