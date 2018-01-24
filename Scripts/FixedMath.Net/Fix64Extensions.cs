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
    }
}
