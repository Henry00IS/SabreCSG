#if UNITY_EDITOR || RUNTIME_CSG

namespace Sabresaurus.SabreCSG
{
	public struct Matrix2x2
	{
		public Fix64 m00;
		public Fix64 m10;
		public Fix64 m01;
		public Fix64 m11;

		public static Matrix2x2 Identity
		{
			get
			{
				return new Matrix2x2()
				{
					m00 = Fix64.One,
					m10 = Fix64.Zero,
					m01 = Fix64.Zero,
					m11 = Fix64.One,
				};
			}
		}

		public static Matrix2x2 Zero
		{
			get
			{
				return new Matrix2x2()
				{
					m00 = Fix64.Zero,
					m10 = Fix64.Zero,
					m01 = Fix64.Zero,
					m11 = Fix64.Zero,
				};
			}
		}

		public Matrix2x2 Inverse
		{
			get
			{
                Fix64 reciprocalDeterminant = Fix64.One / Determinant;

				Matrix2x2 newMatrix = new Matrix2x2()
				{
					m00 = this.m11 * reciprocalDeterminant,
					m10 = -this.m10 * reciprocalDeterminant,
					m01 = -this.m01 * reciprocalDeterminant,
					m11 = this.m00 * reciprocalDeterminant,
				};

				return newMatrix;
			}
		}

		public Fix64 Determinant
		{
			get
			{
				return (m00 * m11) - (m01 * m10);
			}
		}

		public override string ToString ()
		{
			return m00+"\t"+
				m10+"\t"+
				m01+"\t"+
				m11;
		}
	}
}
#endif