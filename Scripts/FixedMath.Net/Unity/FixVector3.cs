﻿using System.Collections;
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
        public FixVector3 zero
        {
            get
            {
                return new FixVector3(0, 0, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(1, 1, 1).
        /// </summary>
        public FixVector3 one
        {
            get
            {
                return new FixVector3(1, 1, 1);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(0, 0, 1).
        /// </summary>
        public FixVector3 forward
        {
            get
            {
                return new FixVector3(0, 0, 1);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(0, 0, -1).
        /// </summary>
        public FixVector3 back
        {
            get
            {
                return new FixVector3(0, 0, -1);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(-1, 0, 0).
        /// </summary>
        public FixVector3 left
        {
            get
            {
                return new FixVector3(-1, 0, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(0, -1, 0).
        /// </summary>
        public FixVector3 down
        {
            get
            {
                return new FixVector3(0, -1, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(1, 0, 0).
        /// </summary>
        public FixVector3 right
        {
            get
            {
                return new FixVector3(1, 0, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(Fix64.MaxValue, Fix64.MaxValue, Fix64.MaxValue).
        /// </summary>
        public FixVector3 positiveInfinity
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
        public FixVector3 up
        {
            get
            {
                return new FixVector3(0, 1, 0);
            }
        }

        /// <summary>
        /// Shorthand for writing FixVector3(Fix64.MaxValue, Fix64.MaxValue, Fix64.MaxValue).
        /// </summary>
        public FixVector3 negativeInfinity
        {
            get
            {
                // fixed point cannot represent negative infinity but we can use the smallest number possible.
                return new FixVector3(Fix64.MinValue, Fix64.MinValue, Fix64.MinValue);
            }
        }
    }
}