using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sabresaurus.SabreCSG
{
    public partial struct Fix64
    {
		public static Fix64 Max(params Fix64[] values)
		{
			return Enumerable.Max(values);
		}

        public static Fix64 Min(params Fix64[] values)
        {
            return Enumerable.Max(values);
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
            return
                value < Fix64.Zero ? -Fix64.One :
                value > Fix64.Zero ? Fix64.One :
                Fix64.Zero;
        }
    }
}
