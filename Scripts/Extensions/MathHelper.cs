using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public static class MathHelper
	{
        static Fix64 EPSILON_LOWER = (Fix64)0.0001f;
        static Fix64 EPSILON_LOWER_2 = (Fix64)0.001f;
        static Fix64 EPSILON_LOWER_3 = (Fix64)0.003f;

        public static int GetSideThick(Plane plane, FixVector3 point)
        {
            Fix64 dot = FixVector3.Dot((FixVector3)plane.normal, point) + (Fix64)plane.distance;

            if (dot > (Fix64)0.02f)
            {
                return 1;
            }
            else if (dot < -(Fix64)0.02f)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public static Vector2 Vector2Cross(Vector2 vector)
        {
            return new Vector2(vector.y, -vector.x);
        }

        public static Fix64 InverseLerpNoClamp(Fix64 from, Fix64 to, Fix64 value)
	    {
	        if (from < to)
	        {
	            value -= from;
	            value /= to - from;
	            return value;
	        }
	        else
	        {
	            return Fix64.One - (value - to) / (from - to);
	        }
	    }

	    public static FixVector3 VectorInDirection(FixVector3 sourceVector, FixVector3 direction)
	    {
	        return direction * FixVector3.Dot(sourceVector, direction);
	    }

	    public static FixVector3 ClosestPointOnPlane(FixVector3 point, Plane plane)
	    {
	        Fix64 signedDistance = (Fix64)plane.GetDistanceToPoint((Vector3)point);

	        return point - (FixVector3)plane.normal * signedDistance;
	    }

	    // From http://answers.unity3d.com/questions/344630/how-would-i-find-the-closest-FixVector3-point-to-a-gi.html
	    public static Fix64 DistanceToRay(FixVector3 X0, Ray ray)
	    {
	        FixVector3 X1 = (FixVector3)ray.origin; // get the definition of a line from the ray
	        FixVector3 X2 = (FixVector3)ray.origin + (FixVector3)ray.direction;
	        FixVector3 X0X1 = (X0 - X1);
	        FixVector3 X0X2 = (X0 - X2);

	        return (FixVector3.Cross(X0X1, X0X2).magnitude / (X1 - X2).magnitude); // magic
	    }

	    // From: http://wiki.unity3d.com/index.php/3d_Math_functions
	    // Two non-parallel lines which may or may not touch each other have a point on each line which are closest
	    // to each other. This function finds those two points. If the lines are not parallel, the function 
	    // outputs true, otherwise false.
	    public static bool ClosestPointsOnTwoLines(out FixVector3 closestPointLine1, out FixVector3 closestPointLine2, FixVector3 linePoint1, FixVector3 lineVec1, FixVector3 linePoint2, FixVector3 lineVec2)
	    {
	        closestPointLine1 = FixVector3.zero;
	        closestPointLine2 = FixVector3.zero;

	        Fix64 a = FixVector3.Dot(lineVec1, lineVec1);
	        Fix64 b = FixVector3.Dot(lineVec1, lineVec2);
	        Fix64 e = FixVector3.Dot(lineVec2, lineVec2);

	        Fix64 d = (a * e) - (b * b);

	        //lines are not parallel
	        if (d != Fix64.Zero)
	        {
	            FixVector3 r = linePoint1 - linePoint2;
	            Fix64 c = FixVector3.Dot(lineVec1, r);
	            Fix64 f = FixVector3.Dot(lineVec2, r);

	            Fix64 s = (b * f - c * e) / d;
	            Fix64 t = (a * f - c * b) / d;

	            closestPointLine1 = linePoint1 + lineVec1 * Fix64.Clamp01(s);
				closestPointLine2 = linePoint2 + lineVec2 * Fix64.Clamp01(t);

	            return true;
	        }
	        else
	        {
	            return false;
	        }
	    }

		// From: http://wiki.unity3d.com/index.php/3d_Math_functions
		//This function finds out on which side of a line segment the point is located.
		//The point is assumed to be on a line created by linePoint1 and linePoint2. If the point is not on
		//the line segment, project it on the line using ProjectPointOnLine() first.
		//Returns 0 if point is on the line segment.
		//Returns 1 if point is outside of the line segment and located on the side of linePoint1.
		//Returns 2 if point is outside of the line segment and located on the side of linePoint2.
		public static int PointOnWhichSideOfLineSegment(FixVector3 linePoint1, FixVector3 linePoint2, FixVector3 point){

			FixVector3 lineVec = linePoint2 - linePoint1;
			FixVector3 pointVec = point - linePoint1;

			Fix64 dot = FixVector3.Dot(pointVec, lineVec);

			//point is on side of linePoint2, compared to linePoint1
			if(dot > Fix64.Zero){

				//point is on the line segment
				if(pointVec.magnitude <= lineVec.magnitude){

					return 0;
				}

				//point is not on the line segment and it is on the side of linePoint2
				else{

					return 2;
				}
			}

			//Point is not on side of linePoint2, compared to linePoint1.
			//Point is not on the line segment and it is on the side of linePoint1.
			else{

				return 1;
			}
		}
		
		// From: http://wiki.unity3d.com/index.php/3d_Math_functions
		//This function returns a point which is a projection from a point to a line.
		//The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
		public static FixVector3 ProjectPointOnLine(FixVector3 linePoint, FixVector3 lineVec, FixVector3 point){		

			//get vector from point on line to point in space
			FixVector3 linePointToPoint = point - linePoint;

			Fix64 t = FixVector3.Dot(linePointToPoint, lineVec);

			return linePoint + lineVec * t;
		}
		
		// From: http://wiki.unity3d.com/index.php/3d_Math_functions
		//This function returns a point which is a projection from a point to a line segment.
		//If the projected point lies outside of the line segment, the projected point will 
		//be clamped to the appropriate line edge.
		//If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
		public static FixVector3 ProjectPointOnLineSegment(FixVector3 linePoint1, FixVector3 linePoint2, FixVector3 point){

			FixVector3 vector = linePoint2 - linePoint1;

			FixVector3 projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

			int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

			//The projected point is on the line segment
			if(side == 0){

				return projectedPoint;
			}

			if(side == 1){

				return linePoint1;
			}

			if(side == 2){

				return linePoint2;
			}

			//output is invalid
			return FixVector3.zero;
		}

		public static FixVector3 ClosestPointOnLine(Ray ray, FixVector3 lineStart, FixVector3 lineEnd)
		{
			FixVector3 rayStart = (FixVector3)ray.origin;
			FixVector3 rayDirection = (FixVector3)ray.direction * (Fix64)10000;

			// Outputs
			FixVector3 closestPointLine1;
			FixVector3 closestPointLine2;
			
			MathHelper.ClosestPointsOnTwoLines(out closestPointLine1, out closestPointLine2, rayStart, rayDirection, lineStart, lineEnd - lineStart);

			// Only interested in the closest point on the line (lineStart -> lineEnd), not the ray
			return closestPointLine1;
		}



	    public static Fix64 RoundFix64(Fix64 value, Fix64 gridScale)
	    {
	        Fix64 reciprocal = Fix64.One / gridScale;
	        return gridScale * Fix64.Round(reciprocal * value);
		}
		
		public static FixVector3 RoundFixVector3(FixVector3 vector)
		{
			vector.x = Fix64.Round(vector.x);
			vector.y = Fix64.Round(vector.y);
			vector.z = Fix64.Round(vector.z);
			return vector;
		}
		
		public static FixVector3 RoundFixVector3(FixVector3 vector, Fix64 gridScale)
		{
			// By dividing the source value by the scale, rounding it, then rescaling it, we calculate the rounding
			Fix64 reciprocal = Fix64.One / gridScale;
			vector.x = gridScale * Fix64.Round(reciprocal * vector.x);
			vector.y = gridScale * Fix64.Round(reciprocal * vector.y);
			vector.z = gridScale * Fix64.Round(reciprocal * vector.z);
			return vector;
		}
		
		public static Vector2 RoundVector2(Vector2 vector)
		{
			vector.x = Mathf.Round(vector.x);
			vector.y = Mathf.Round(vector.y);
			return vector;
		}
		
		public static Vector2 RoundVector2(Vector2 vector, float gridScale)
		{
            // By dividing the source value by the scale, rounding it, then rescaling it, we calculate the rounding
            float reciprocal = 1f / gridScale;
			vector.x = gridScale * Mathf.Round(reciprocal * vector.x);
			vector.y = gridScale * Mathf.Round(reciprocal * vector.y);
			return vector;
		}

	    public static FixVector3 VectorAbs(FixVector3 vector)
	    {
	        vector.x = Fix64.Abs(vector.x);
	        vector.y = Fix64.Abs(vector.y);
	        vector.z = Fix64.Abs(vector.z);
	        return vector;
	    }

        public static Vector3 Abs(this Vector3 vector)
        {
            vector.x = Mathf.Abs(vector.x);
            vector.y = Mathf.Abs(vector.y);
            vector.z = Mathf.Abs(vector.z);
            return vector;
        }

        public static int Wrap(int i, int range)
	    {
	        if (i < 0)
	        {
	            i = range - 1;
	        }
	        if (i >= range)
	        {
	            i = 0;
	        }
	        return i;
	    }

		public static Fix64 Wrap(Fix64 i, Fix64 range)
		{
			if (i < Fix64.Zero)
			{
				i = range - Fix64.One;
			}
			if (i >= range)
			{
				i = Fix64.Zero;
			}
			return i;
		}


		public static Fix64 WrapAngle(Fix64 angle)
		{
			while(angle > (Fix64)180)
			{
				angle -= (Fix64)360;
			}
			while(angle <= -(Fix64)180)
			{
				angle += (Fix64)360;
			}
			return angle;
		}

		public static bool PlaneEqualsLooser(Plane plane1, Plane plane2)
		{
			if(
				Fix64.Abs((Fix64)plane1.distance - (Fix64)plane2.distance) < EPSILON_LOWER
				&& Fix64.Abs((Fix64)plane1.normal.x - (Fix64)plane2.normal.x) < EPSILON_LOWER 
				&& Fix64.Abs((Fix64)plane1.normal.y - (Fix64)plane2.normal.y) < EPSILON_LOWER 
				&& Fix64.Abs((Fix64)plane1.normal.z - (Fix64)plane2.normal.z) < EPSILON_LOWER)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

        public static bool PlaneEqualsLooserWithFlip(Plane plane1, Plane plane2)
        {
            if (
                Fix64.Abs((Fix64)plane1.distance - (Fix64)plane2.distance) < EPSILON_LOWER_3
                && Fix64.Abs((Fix64)plane1.normal.x - (Fix64)plane2.normal.x) < EPSILON_LOWER_2
                && Fix64.Abs((Fix64)plane1.normal.y - (Fix64)plane2.normal.y) < EPSILON_LOWER_2
                && Fix64.Abs((Fix64)plane1.normal.z - (Fix64)plane2.normal.z) < EPSILON_LOWER_2)
            {
                return true;
            }
            else if (
                Fix64.Abs(-(Fix64)plane1.distance - (Fix64)plane2.distance) < EPSILON_LOWER_3
                && Fix64.Abs(-(Fix64)plane1.normal.x - (Fix64)plane2.normal.x) < EPSILON_LOWER_2
                && Fix64.Abs(-(Fix64)plane1.normal.y - (Fix64)plane2.normal.y) < EPSILON_LOWER_2
                && Fix64.Abs(-(Fix64)plane1.normal.z - (Fix64)plane2.normal.z) < EPSILON_LOWER_2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool PlaneEquals(Plane plane1, Plane plane2)
        {
            if (((Fix64)plane1.distance).EqualsWithEpsilon(((Fix64)plane2.distance)) && ((FixVector3)plane1.normal).EqualsWithEpsilon((FixVector3)plane2.normal))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsVectorInteger(FixVector3 vector)
		{
			if(vector.x % Fix64.One != Fix64.Zero
                || vector.y % Fix64.One != Fix64.Zero
                || vector.z % Fix64.One != Fix64.Zero)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public static bool IsVectorOnGrid(FixVector3 position, FixVector3 mask, Fix64 gridScale)
		{
			if(mask.x != Fix64.Zero)
			{
				if(position.x % gridScale != Fix64.Zero)
				{
					return false;
				}
			}

			if(mask.y != Fix64.Zero)
			{
				if(position.y % gridScale != Fix64.Zero)
				{
					return false;
				}
			}

			if(mask.z != Fix64.Zero)
			{
				if(position.z % gridScale != Fix64.Zero)
				{
					return false;
				}
			}

			return true;
		}
	}
}