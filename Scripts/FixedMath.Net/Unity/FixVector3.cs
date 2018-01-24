using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>Representation of 3D vectors and points using <see cref="Fix64"/>.</summary>
    public struct FixVector3
    {
        public Fix64 kEpsilon { get { return Fix64.Epsilon; } }

        /// <summary>The X component of the vector.</summary>
        public Fix64 x;
        /// <summary>The Y component of the vector.</summary>
        public Fix64 y;
        /// <summary>The Z component of the vector.</summary>
        public Fix64 z;

        /// <summary>Creates a new vector with given x, y components and sets z to zero.</summary>
        /// <param name="x">The x component.</param>
        /// <param name="y">The y component.</param>
        public FixVector3(Fix64 x, Fix64 y)
        {
            this.x = x;
            this.y = y;
            this.z = Fix64.Zero;
        }

        /// <summary>Creates a new vector with given x, y, z components.</summary>
        /// <param name="x">The x component.</param>
        /// <param name="y">The y component.</param>
        /// <param name="z">The z component.</param>
        public FixVector3(Fix64 x, Fix64 y, Fix64 z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>Creates a new vector with given x, y components and sets z to zero.</summary>
        /// <param name="x">The x component.</param>
        /// <param name="y">The y component.</param>
        public FixVector3(int x, int y)
        {
            this.x = new Fix64(x);
            this.y = new Fix64(y);
            this.z = Fix64.Zero;
        }

        /// <summary>Creates a new vector with given x, y, z components.</summary>
        /// <param name="x">The x component.</param>
        /// <param name="y">The y component.</param>
        /// <param name="z">The z component.</param>
        public FixVector3(int x, int y, int z)
        {
            this.x = new Fix64(x);
            this.y = new Fix64(y);
            this.z = new Fix64(z);
        }

        public Fix64 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default: return Fix64.Zero;
                }
            }
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                }
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(0, 0, 0).
        /// </summary>
        public static FixVector3 zero
        {
            get
            {
                return new FixVector3(0, 0, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(1, 1, 1).
        /// </summary>
        public static FixVector3 one
        {
            get
            {
                return new FixVector3(1, 1, 1);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(0, 0, 1).
        /// </summary>
        public static FixVector3 forward
        {
            get
            {
                return new FixVector3(0, 0, 1);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(0, 0, -1).
        /// </summary>
        public static FixVector3 back
        {
            get
            {
                return new FixVector3(0, 0, -1);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(-1, 0, 0).
        /// </summary>
        public static FixVector3 left
        {
            get
            {
                return new FixVector3(-1, 0, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(0, -1, 0).
        /// </summary>
        public static FixVector3 down
        {
            get
            {
                return new FixVector3(0, -1, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(1, 0, 0).
        /// </summary>
        public static FixVector3 right
        {
            get
            {
                return new FixVector3(1, 0, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(Fix64.MaxValue, Fix64.MaxValue, Fix64.MaxValue).
        /// </summary>
        public static FixVector3 positiveInfinity
        {
            get
            {
                // fixed point cannot represent positive infinity but we can use the largest number possible.
                return new FixVector3(Fix64.MaxValue, Fix64.MaxValue, Fix64.MaxValue);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(0, 1, 0).
        /// </summary>
        public static FixVector3 up
        {
            get
            {
                return new FixVector3(0, 1, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(Fix64.MaxValue, Fix64.MaxValue, Fix64.MaxValue).
        /// </summary>
        public static FixVector3 negativeInfinity
        {
            get
            {
                // fixed point cannot represent negative infinity but we can use the smallest number possible.
                return new FixVector3(Fix64.MinValue, Fix64.MinValue, Fix64.MinValue);
            }
        }

        public FixVector3 normalized
        {
            get
            {
                Fix64 length = magnitude;
                if (length != Fix64.Zero)
                    return new FixVector3(x / length, y / length, z / length);
                return zero;
            }
        }

        public Fix64 magnitude
        {
            get
            {
                return Magnitude(this);
            }
        }

        public static FixVector3 Lerp(FixVector3 start, FixVector3 end, Fix64 percent)
        {
            return (start + percent * (end - start));
        }

        public static Fix64 Magnitude(FixVector3 vector)
        {
            return Fix64.Sqrt((vector.x * vector.x) + (vector.y * vector.y) + (vector.z * vector.z));
        }

        public static Fix64 Dot(FixVector3 lhs, FixVector3 rhs)
        {
            return (lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/>, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FixVector3))
            {
                return false;
            }

            var vector = (FixVector3)obj;
            return x.Equals(vector.x) &&
                   y.Equals(vector.y) &&
                   z.Equals(vector.z);
        }

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures
        /// like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            var hashCode = 373119288;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Fix64>.Default.GetHashCode(x);
            hashCode = hashCode * -1521134295 + EqualityComparer<Fix64>.Default.GetHashCode(y);
            hashCode = hashCode * -1521134295 + EqualityComparer<Fix64>.Default.GetHashCode(z);
            return hashCode;
        }

        public static explicit operator Vector3(FixVector3 value)
        {
            return new Vector3((float)value.x, (float)value.y, (float)value.z);
        }

        public static explicit operator FixVector3(Vector3 value)
        {
            return new FixVector3((Fix64)value.x, (Fix64)value.y, (Fix64)value.z);
        }

        public static FixVector3 operator +(FixVector3 a, FixVector3 b)
        {
            return new FixVector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static FixVector3 operator -(FixVector3 a)
        {
            return new FixVector3(-a.x, -a.y, -a.z);
        }

        public static FixVector3 operator -(FixVector3 a, FixVector3 b)
        {
            return new FixVector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static FixVector3 operator *(Fix64 d, FixVector3 a)
        {
            return new FixVector3(d * a.x, d * a.y, d * a.z);
        }

        public static FixVector3 operator *(FixVector3 a, Fix64 d)
        {
            return new FixVector3(a.x * d, a.y * d, a.z * d);
        }

        public static FixVector3 operator /(FixVector3 a, Fix64 d)
        {
            return new FixVector3(a.x / d, a.y / d, a.z / d);
        }

        public static bool operator ==(FixVector3 lhs, FixVector3 rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
        }

        public static bool operator !=(FixVector3 lhs, FixVector3 rhs)
        {
            return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;
        }
    }
}