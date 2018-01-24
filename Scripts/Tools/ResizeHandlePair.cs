#if UNITY_EDITOR
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public enum ResizeType
    {
        Corner,
        EdgeMid,
        FaceMid
    };

    // Used by ResizeEditor to describe two handles (e.g. X axis resize handles)
    public struct ResizeHandlePair
    {
        public delegate FixVector3 PointTransformer(FixVector3 sourcePoint);

        public FixVector3 point1;
        public FixVector3 point2;
        ResizeType resizeType;

        public ResizeType ResizeType
        {
            get
            {
                return resizeType;
            }
        }

        public ResizeHandlePair(FixVector3 point1)
        {
            this.point1 = point1;
            this.point2 = -Fix64.One * point1;

            if (point1.sqrMagnitude == Fix64.One)
            {
                resizeType = ResizeType.FaceMid;
            }
            else if (point1.sqrMagnitude == (Fix64)2)
            {
                resizeType = ResizeType.EdgeMid;
            }
            else
            {
                resizeType = ResizeType.Corner;
            }
        }

        public FixVector3 GetPoint(int pointIndex)
        {
            if (pointIndex == 0)
                return point1;
            else if (pointIndex == 1)
                return point2;
            else
                throw new System.IndexOutOfRangeException("Supplied point index should be 0 or 1");
        }

        public override bool Equals(object obj)
        {
            if (obj is ResizeHandlePair)
            {
                return this == (ResizeHandlePair)obj;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(ResizeHandlePair lhs, ResizeHandlePair rhs)
        {
            return lhs.point1 == rhs.point1 && lhs.point2 == rhs.point2;
        }

        public static bool operator !=(ResizeHandlePair lhs, ResizeHandlePair rhs)
        {
            return lhs.point1 != rhs.point1 || lhs.point2 != rhs.point2;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool InClickZone(PointTransformer TransformPoint, Vector2 mousePosition, int pointIndex, Bounds bounds)
        {
            FixVector3 worldPosition = TransformPoint((FixVector3)bounds.center + GetPoint(pointIndex).Multiply((FixVector3)bounds.extents));
            FixVector3 targetScreenPosition = (FixVector3)Camera.current.WorldToScreenPoint((Vector3)worldPosition);

            Fix64 screenDistancePoints = CalculateScreenRange(TransformPoint, pointIndex, bounds);

            if (EditorHelper.InClickZone(mousePosition, new Vector2((float)targetScreenPosition.x, (float)targetScreenPosition.y), screenDistancePoints))
            {
                //Debug.Log(Mathf.Round(screenDistancePoints) + " " + Mathf.Round(screenBoundsSizePoints));

                return true;
            }
            else
            {
                return false;
            }
        }

        public Fix64 CalculateScreenRange(PointTransformer TransformPoint, int pointIndex, Bounds bounds)
        {
            Fix64 screenBoundsSizePoints = CalculateScreenSize(TransformPoint, pointIndex, bounds);

            // tolerance = (screenSize ^ 1.2) / 20, meaning 50 => 5.5, 80 => 9.6
            Fix64 screenDistancePoints = (Fix64)Mathf.Pow((float)screenBoundsSizePoints, 1.2f) / (Fix64)20f;
            // Clamp to the 5 to 15 points range
            screenDistancePoints = Fix64.Clamp(screenDistancePoints, (Fix64)5, (Fix64)12);

            return screenDistancePoints;
        }

        Fix64 CalculateScreenSize(PointTransformer TransformPoint, int pointIndex, Bounds bounds)
        {
            Fix64 minDistancPoints = Fix64.MaxValue; // positive infinity.

            FixVector3 pairPoint = GetPoint(pointIndex);

            // Process each set component separately, this way we can calculate the min screen size of each active face
            for (int i = 0; i < 3; i++)
            {
                if (pairPoint[i] != Fix64.Zero)
                {
                    FixVector3 sourceDirection = FixVector3.zero;
                    sourceDirection[i] = pairPoint[i];

                    FixVector3 extent1Positive = sourceDirection;
                    FixVector3 extent1Negative = sourceDirection;
                    FixVector3 extent2Positive = sourceDirection;
                    FixVector3 extent2Negative = sourceDirection;

                    if (i == 0) // X already set, so set Y and Z
                    {
                        extent1Positive.y = Fix64.One;
                        extent1Negative.y = -Fix64.One;
                        extent2Positive.z = Fix64.One;
                        extent2Negative.z = -Fix64.One;
                    }
                    else if (i == 1) // Y already set, so set X and Z
                    {
                        extent1Positive.x = Fix64.One;
                        extent1Negative.x = -Fix64.One;
                        extent2Positive.z = Fix64.One;
                        extent2Negative.z = -Fix64.One;
                    }
                    else // Z already set, so set X and Y
                    {
                        extent1Positive.x = Fix64.One;
                        extent1Negative.x = -Fix64.One;
                        extent2Positive.y = Fix64.One;
                        extent2Negative.y = -Fix64.One;
                    }

                    FixVector3 worldPosition1Positive = TransformPoint((FixVector3)bounds.center + extent1Positive.Multiply((FixVector3)bounds.extents));
                    FixVector3 worldPosition1Negative = TransformPoint((FixVector3)bounds.center + extent1Negative.Multiply((FixVector3)bounds.extents));
                    FixVector3 worldPosition2Positive = TransformPoint((FixVector3)bounds.center + extent2Positive.Multiply((FixVector3)bounds.extents));
                    FixVector3 worldPosition2Negative = TransformPoint((FixVector3)bounds.center + extent2Negative.Multiply((FixVector3)bounds.extents));

                    //VisualDebug.AddPoints(worldPosition1Positive, worldPosition1Negative, worldPosition2Positive, worldPosition2Negative);

                    FixVector3 screenPosition1Positive = (FixVector3)Camera.current.WorldToScreenPoint((Vector3)worldPosition1Positive);
                    FixVector3 screenPosition1Negative = (FixVector3)Camera.current.WorldToScreenPoint((Vector3)worldPosition1Negative);
                    FixVector3 screenPosition2Positive = (FixVector3)Camera.current.WorldToScreenPoint((Vector3)worldPosition2Positive);
                    FixVector3 screenPosition2Negative = (FixVector3)Camera.current.WorldToScreenPoint((Vector3)worldPosition2Negative);

                    Fix64 distance1Points = (Fix64)EditorHelper.ConvertScreenPixelsToPoints((Fix64)Vector2.Distance(new Vector2((float)screenPosition1Positive.x, (float)screenPosition1Positive.y), new Vector2((float)screenPosition1Negative.x, (float)screenPosition1Negative.y)));
                    Fix64 distance2Points = (Fix64)EditorHelper.ConvertScreenPixelsToPoints((Fix64)Vector2.Distance(new Vector2((float)screenPosition2Positive.x, (float)screenPosition2Positive.y), new Vector2((float)screenPosition2Negative.x, (float)screenPosition2Negative.y)));

                    minDistancPoints = Fix64.Min(minDistancPoints, distance1Points);
                    minDistancPoints = Fix64.Min(minDistancPoints, distance2Points);
                }
            }
            return minDistancPoints;
        }

    }
}
#endif