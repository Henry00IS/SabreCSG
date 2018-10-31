#if UNITY_EDITOR || RUNTIME_CSG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Sabresaurus.SabreCSG
{
    [System.Serializable]
    public class Polygon : IDeepCopyable<Polygon>
    {
        [SerializeField]
        private Vertex[] vertices;

        private Plane? cachedPlane = null;

        // When a polygon is split or cloned, this number is preserved to those new objects so they can track where
        // they came from.
        [SerializeField]
        private int uniqueIndex = -1;

        [SerializeField]
        private Material material;

        // Set if the polygon is
        private bool excludeFromFinal = false;

        [SerializeField]
        private bool userExcludeFromFinal = false;

        public Vertex[] Vertices
        {
            get
            {
                return vertices;
            }
            set
            {
                vertices = value;
                CalculatePlane();
            }
        }

        public Plane CachedPlaneTest
        {
            get
            {
                return cachedPlane.Value;
            }
        }

        public Plane Plane
        {
            get
            {
                if (!cachedPlane.HasValue)
                {
                    CalculatePlane();
                }
                return cachedPlane.Value;
            }
        }

        public Material Material
        {
            get
            {
                return material;
            }
            set
            {
                material = value;
            }
        }

        public int UniqueIndex
        {
            get
            {
                return uniqueIndex;
            }
            set
            {
                uniqueIndex = value;
            }
        }

        public bool ExcludeFromFinal
        {
            get
            {
                return excludeFromFinal;
            }
            set
            {
                excludeFromFinal = value;
            }
        }

        public bool UserExcludeFromFinal
        {
            get
            {
                return userExcludeFromFinal;
            }
            set
            {
                userExcludeFromFinal = value;
            }
        }

        public Polygon(Vertex[] vertices, Material material, bool isTemporary, bool userExcludeFromFinal, int uniqueIndex = -1)
        {
            this.vertices = vertices;
            this.material = material;
            this.uniqueIndex = uniqueIndex;
            this.excludeFromFinal = isTemporary;
            this.userExcludeFromFinal = userExcludeFromFinal;
            CalculatePlane();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// </summary>
        /// <param name="vertices">The vertex positions of a <see cref="Vertex"/> that make up this polygonal shape. Normals and UVs will be zero (see <see cref="ResetVertexNormals"/> and <see cref="GenerateUvCoordinates"/> to generate them automatically).</param>
        /// <param name="material">The Unity <see cref="UnityEngine.Material"/> applied to the surface of this polygon.</param>
        /// <param name="isTemporary">If set to <c>true</c> excludes the polygon from the final CSG build, it's only temporarily created during the build process to determine whether a point is inside/outside of a convex chunk (usually you set this argument to <c>false</c>, also see <paramref name="userExcludeFromFinal"/>).</param>
        /// <param name="userExcludeFromFinal">If set to <c>true</c> the user requested that this polygon be excluded from the final CSG build (i.e. not rendered, it does affect CSG operations).</param>
        /// <param name="uniqueIndex">When a polygon is split or cloned, this number is preserved inside of those new polygons so they can track where they originally came from.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="vertices"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A polygon must have at least 3 vertices.</exception>
		public Polygon(Vector3[] vertices, Material material, bool isTemporary, bool userExcludeFromFinal, int uniqueIndex = -1)
        {
            if (vertices == null) throw new ArgumentNullException("vertices");
            if (vertices.Length < 3) throw new ArgumentOutOfRangeException("A polygon must have at least 3 vertices.");
            // consideration: check array for null elements?

            // create vertices from the vector3 array.
            this.vertices = new Vertex[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                this.vertices[i] = new Vertex(vertices[i], Vector3.zero, Vector2.zero);

            this.material = material;
            this.uniqueIndex = uniqueIndex;
            this.excludeFromFinal = isTemporary;
            this.userExcludeFromFinal = userExcludeFromFinal;

            // calculate the cached plane.
            CalculatePlane();
        }

        public Polygon DeepCopy()
        {
            return new Polygon(this.vertices.DeepCopy(), this.material, this.excludeFromFinal, this.userExcludeFromFinal, this.uniqueIndex);
        }

        public void CalculatePlane()
        {
            if (vertices.Length < 3)
            {
                cachedPlane = new Plane();
                return;
            }
            cachedPlane = new Plane(vertices[0].Position, vertices[1].Position, vertices[2].Position);

            // HACK: If the normal is zero and there's room to try another, then try alternate vertices
            if (cachedPlane.Value.normal == Vector3.zero && vertices.Length > 3)
            {
                int vertexIndex1 = 0;
                int vertexIndex2 = 1;
                int vertexIndex3 = 3;

                // Update UVs
                Vector3 pos1 = vertices[vertexIndex1].Position;
                Vector3 pos2 = vertices[vertexIndex2].Position;
                Vector3 pos3 = Vector3.zero;

                for (int i = 3; i < vertices.Length; i++)
                {
                    vertexIndex3 = i;

                    pos3 = vertices[vertexIndex3].Position;

                    cachedPlane = new Plane(pos1, pos2, pos3);

                    if (cachedPlane.Value.normal != Vector3.zero)
                    {
                        return;
                    }
                }

                //				if(cachedPlane.Value.normal == Vector3.zero)
                //				{
                //					Debug.LogError("Invalid Normal! Shouldn't be zero. Vertices count is " + vertices.Length);
                //				}
            }
        }

        public void Flip()
        {
            // Reverse winding order
            System.Array.Reverse(this.vertices);

            // Flip each vertex normal
            for (int i = 0; i < this.vertices.Length; i++)
            {
                this.vertices[i].Normal *= -1;
            }

            // Flip the cached plane
#if UNITY_2017_1_OR_NEWER
            // Unity 2017 introduces a built in Plane flipped property
            cachedPlane = cachedPlane.Value.flipped;
#else
			cachedPlane = cachedPlane.Value.Flip();
#endif
        }

        public void SetVertices(Vertex[] vertices)
        {
            this.vertices = vertices;
            CalculatePlane();
        }

        public void ResetVertexNormals()
        {
            for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                vertices[vertexIndex].Normal = Plane.normal;
            }
        }

        public Bounds GetBounds()
        {
            if (vertices.Length > 0)
            {
                Bounds polygonBounds = new Bounds(vertices[0].Position, Vector3.zero);

                for (int j = 1; j < vertices.Length; j++)
                {
                    polygonBounds.Encapsulate(vertices[j].Position);
                }
                return polygonBounds;
            }
            else
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }
        }

#if UNITY_EDITOR || RUNTIME_CSG

        public Edge[] GetEdges()
        {
            Edge[] edges = new Edge[vertices.Length];

            for (int vertexIndex1 = 0; vertexIndex1 < vertices.Length; vertexIndex1++)
            {
                // If our edge is from the last vertex it should be to the first vertex, otherwise to next vertex
                int vertexIndex2 = ((vertexIndex1 + 1) >= vertices.Length ? 0 : vertexIndex1 + 1);
                edges[vertexIndex1] = new Edge(vertices[vertexIndex1], vertices[vertexIndex2]);
            }
            return edges;
        }

#endif

        public Vector3 GetCenterPoint()
        {
            Vector3 center = vertices[0].Position;
            for (int i = 1; i < vertices.Length; i++)
            {
                center += vertices[i].Position;
            }
            center /= vertices.Length;
            return center;
        }

        public Vector3 GetCenterUV()
        {
            Vector2 centerUV = vertices[0].UV;
            for (int i = 1; i < vertices.Length; i++)
            {
                centerUV += vertices[i].UV;
            }
            centerUV *= 1f / vertices.Length;
            return centerUV;
        }

        public float GetArea()
        {
            Vector3 normal = Vector3.Normalize(Vector3.Cross(vertices[1].Position - vertices[0].Position, vertices[2].Position - vertices[0].Position));
            Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(normal));

            float totalArea = 0;

            int j = vertices.Length - 1;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 positionI = cancellingRotation * vertices[i].Position;
                Vector3 positionJ = cancellingRotation * vertices[j].Position;
                totalArea += (positionJ.x + positionI.x) * (positionJ.y - positionI.y);

                j = i;
            }
            return -totalArea * 0.5f;
        }

        public void SetColor(Color32 newColor)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Color = newColor;
            }
        }
		
        public Vector3 GetTangent()
        {
            return (vertices[1].Position - vertices[0].Position).normalized;
		}

        /// <summary>
        /// Maps all 3D vertices (x, y, z) of the polygon to 2D (x, z). Useful for 2D polygon algorithms.
        /// <para>Returns the matrix used for the 3D to 2D operation for use with <see cref="MapTo3D"/>.</para>
        /// <para>You may have to call <see cref="CalculatePlane"/> first if you modified the polygon.</para>
        /// </summary>
        /// <remarks>
        /// Special thanks to jwatte from http://xboxforums.create.msdn.com/forums/t/16529.aspx for
        /// the algorithm.
        /// </remarks>
        /// <returns>The matrix used for the 3D to 2D operation so it can be used in <see cref="MapTo3D"/>.</returns>
        public Matrix4x4 MapTo2D()
        {
            // calculate a 3d to 2d matrix.
            Vector3 right, backward;
            if (Math.Abs(Plane.normal.x) > Math.Abs(Plane.normal.z))
                right = Vector3.Cross(Plane.normal, new Vector3(0, 0, 1));
            else
                right = Vector3.Cross(Plane.normal, new Vector3(1, 0, 0));
            right = Vector3.Normalize(right);
            backward = Vector3.Cross(right, Plane.normal);
            Matrix4x4 m = new Matrix4x4(new Vector4(right.x, Plane.normal.x, backward.x, 0), new Vector4(right.y, Plane.normal.y, backward.y, 0), new Vector4(right.z, Plane.normal.z, backward.z, 0), new Vector4(0, 0, 0, 1));
            // multiply all vertices by the matrix.
            for (int p = 0; p < vertices.Length; p++)
                vertices[p].Position = m * vertices[p].Position;
            return m;
        }

        /// <summary>
        /// Maps all 2D vertices (x, z) of the polygon to 3D (x, y, z). Requires matrix from <see cref="MapTo2D"/>.
        /// </summary>
        /// <param name="matrix">The matrix from the <see cref="MapTo2D"/> operation.</param>
        public void MapTo3D(Matrix4x4 matrix)
        {
            // inverse the 3d to 2d matrix.
            Matrix4x4 m = matrix.inverse;
            // multiply all vertices by the matrix.
            for (int p = 0; p < vertices.Length; p++)
                vertices[p].Position = m * vertices[p].Position;
        }

        public class Vector3ComparerEpsilon : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 a, Vector3 b)
            {
                return Mathf.Abs(a.x - b.x) < EPSILON_LOWER
                    && Mathf.Abs(a.y - b.y) < EPSILON_LOWER
                        && Mathf.Abs(a.z - b.z) < EPSILON_LOWER;
            }

            public int GetHashCode(Vector3 obj)
            {
                // The similarity or difference between two positions can only be calculated if both are supplied
                // when Distinct is called GetHashCode is used to determine which values are in collision first
                // therefore we return the same hash code for all values to ensure all comparisons must use
                // our Equals method to properly determine which values are actually considered equal
                return 1;
            }
        }

        public class VertexComparerEpsilon : IEqualityComparer<Vertex>
        {
            public bool Equals(Vertex x, Vertex y)
            {
                return x.Position.EqualsWithEpsilon(y.Position);
            }

            public int GetHashCode(Vertex obj)
            {
                // The similarity or difference between two positions can only be calculated if both are supplied
                // when Distinct is called GetHashCode is used to determine which values are in collision first
                // therefore we return the same hash code for all values to ensure all comparisons must use
                // our Equals method to properly determine which values are actually considered equal
                return 1;
            }
        }

        public class PolygonUIDComparer : IEqualityComparer<Polygon>
        {
            public bool Equals(Polygon x, Polygon y)
            {
                return x.UniqueIndex == y.UniqueIndex;
            }

            public int GetHashCode(Polygon obj)
            {
                return base.GetHashCode();
            }
        }

        private const float EPSILON = 0.00001f;
        private const float EPSILON_LOWER = 0.001f;

        //		const float EPSILON_LOWER = 0.003f;

        #region Static Methods

        public enum PolygonPlaneRelation { InFront, Behind, Spanning, Coplanar };

        public static PolygonPlaneRelation TestPolygonAgainstPlane(Polygon polygon, UnityEngine.Plane testPlane)
        {
            if (polygon.Plane.normal == testPlane.normal && polygon.Plane.distance == testPlane.distance)
            {
                return PolygonPlaneRelation.Coplanar;
            }

            // Count the number of vertices in front and behind the clip plane
            int verticesInFront = 0;
            int verticesBehind = 0;

            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                float distance = testPlane.GetDistanceToPoint(polygon.Vertices[i].Position);
                if (distance < -EPSILON_LOWER) // Is the point in front of the plane (with thickness)
                {
                    verticesInFront++;
                }
                else if (distance > EPSILON_LOWER) // Is the point behind the plane (with thickness)
                {
                    verticesBehind++;
                }
            }

            if (verticesInFront > 0 && verticesBehind > 0) // If some are in front and some are behind, then the poly spans
            {
                return PolygonPlaneRelation.Spanning;
            }
            else if (verticesInFront > 0) // Only in front, so entire polygon is in front
            {
                return PolygonPlaneRelation.InFront;
            }
            else if (verticesBehind > 0)  // Only behind, so entire polygon is behind
            {
                return PolygonPlaneRelation.Behind;
            }
            else // No points in front or behind the plane, so assume coplanar
            {
                return PolygonPlaneRelation.Coplanar;
            }
        }

        // Loops through the vertices and removes any that share a position with any others so that
        // only uniquely positioned vertices remain
        public void RemoveExtraneousVertices()
        {
            List<Vertex> newVertices = new List<Vertex>();
            newVertices.Add(vertices[0]);

            for (int i = 1; i < vertices.Length; i++)
            {
                bool alreadyContained = false;

                for (int j = 0; j < newVertices.Count; j++)
                {
                    if (vertices[i].Position.EqualsWithEpsilonLower(newVertices[j].Position))
                    {
                        alreadyContained = true;
                        break;
                    }
                }

                if (!alreadyContained)
                {
                    newVertices.Add(vertices[i]);
                }
            }

            vertices = newVertices.ToArray();
            if (vertices.Length > 2)
            {
                CalculatePlane();
            }
        }

        /// <summary>
        /// Generates the UV coordinates for this polygon automatically. This works similarly to the
        /// "AutoUV" button in the surface editor. This method may throw warnings in the console if
        /// the normal of the polygon is zero.
        /// <para>You may have to call <see cref="CalculatePlane"/> first if you modified the polygon.</para>
        /// </summary>
        public void GenerateUvCoordinates()
        {
            // stolen code from the surface editor "AutoUV".
            Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(-Plane.normal));
            // sets the uv at each point to the position on the plane.
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].UV = (cancellingRotation * vertices[i].Position) * 0.5f;
        }

        public static bool SplitPolygon(Polygon polygon, out Polygon frontPolygon, out Polygon backPolygon, out Vertex newVertex1, out Vertex newVertex2, UnityEngine.Plane clipPlane)
        {
            newVertex1 = null;
            newVertex2 = null;

            List<Vertex> frontVertices = new List<Vertex>();
            List<Vertex> backVertices = new List<Vertex>();

            for (int i = 0; i < polygon.vertices.Length; i++)
            {
                int previousIndex = i - 1;
                if (previousIndex < 0)
                {
                    previousIndex = polygon.vertices.Length - 1;
                }

                Vertex currentVertex = polygon.vertices[i];
                Vertex previousVertex = polygon.vertices[previousIndex];

                PointPlaneRelation currentRelation = ComparePointToPlane(currentVertex.Position, clipPlane);
                PointPlaneRelation previousRelation = ComparePointToPlane(previousVertex.Position, clipPlane);

                if (previousRelation == PointPlaneRelation.InFront && currentRelation == PointPlaneRelation.InFront)
                {
                    // Front add current
                    frontVertices.Add(currentVertex);
                }
                else if (previousRelation == PointPlaneRelation.Behind && currentRelation == PointPlaneRelation.InFront)
                {
                    float interpolant = Edge.IntersectsPlane(clipPlane, previousVertex.Position, currentVertex.Position);
                    Vertex intersection = Vertex.Lerp(previousVertex, currentVertex, interpolant);

                    // Front add intersection, add current
                    frontVertices.Add(intersection);
                    frontVertices.Add(currentVertex);

                    // Back add intersection
                    backVertices.Add(intersection.DeepCopy());

                    newVertex2 = intersection;
                }
                else if (previousRelation == PointPlaneRelation.InFront && currentRelation == PointPlaneRelation.Behind)
                {
                    // Reverse order here so that clipping remains consistent for either CW or CCW testing
                    float interpolant = Edge.IntersectsPlane(clipPlane, currentVertex.Position, previousVertex.Position);
                    Vertex intersection = Vertex.Lerp(currentVertex, previousVertex, interpolant);

                    // Front add intersection
                    frontVertices.Add(intersection);

                    // Back add intersection, current
                    backVertices.Add(intersection.DeepCopy());
                    backVertices.Add(currentVertex);

                    newVertex1 = intersection;
                }
                else if (previousRelation == PointPlaneRelation.Behind && currentRelation == PointPlaneRelation.Behind)
                {
                    // Back add current
                    backVertices.Add(currentVertex);
                }
                else if (currentRelation == PointPlaneRelation.On)
                {
                    // Front add current
                    frontVertices.Add(currentVertex);

                    // Back add current
                    backVertices.Add(currentVertex.DeepCopy());

                    if (previousRelation == PointPlaneRelation.InFront)
                    {
                        newVertex1 = currentVertex;
                    }
                    else if (previousRelation == PointPlaneRelation.Behind)
                    {
                        newVertex2 = currentVertex;
                    }
                    else
                    {
                        //						throw new System.Exception("Unhandled polygon configuration");
                    }
                }
                else if (currentRelation == PointPlaneRelation.Behind)
                {
                    backVertices.Add(currentVertex);
                }
                else if (currentRelation == PointPlaneRelation.InFront)
                {
                    frontVertices.Add(currentVertex);
                }
                else
                {
                    throw new System.Exception("Unhandled polygon configuration");
                }
            }
            //			Debug.Log("done");

            frontPolygon = new Polygon(frontVertices.ToArray(), polygon.Material, polygon.ExcludeFromFinal, polygon.UserExcludeFromFinal, polygon.uniqueIndex);
            backPolygon = new Polygon(backVertices.ToArray(), polygon.Material, polygon.ExcludeFromFinal, polygon.UserExcludeFromFinal, polygon.uniqueIndex);

            // Because of some floating point issues and some edge cases relating to splitting the tip of a very thin
            // polygon we can't reliable test that the polygon intersects a plane and will produce two valid pieces
            // so after splitting we need to do an additional test to check that each polygon is valid. If it isn't
            // then we mark that polygon as null and return false to indicate the split wasn't entirely successful

            bool splitNecessary = true;

            if (frontPolygon.vertices.Length < 3 || frontPolygon.Plane.normal == Vector3.zero)
            {
                frontPolygon = null;
                splitNecessary = false;
            }

            if (backPolygon.vertices.Length < 3 || backPolygon.Plane.normal == Vector3.zero)
            {
                backPolygon = null;
                splitNecessary = false;
            }

            return splitNecessary;
        }

        public static bool PlanePolygonIntersection(Polygon polygon, out Vector3 position1, out Vector3 position2, UnityEngine.Plane testPlane)
        {
            position1 = Vector3.zero;
            position2 = Vector3.zero;

            bool position1Set = false;
            bool position2Set = false;

            for (int i = 0; i < polygon.vertices.Length; i++)
            {
                int previousIndex = i - 1;
                if (previousIndex < 0)
                {
                    previousIndex = polygon.vertices.Length - 1;
                }

                Vertex currentVertex = polygon.vertices[i];
                Vertex previousVertex = polygon.vertices[previousIndex];

                PointPlaneRelation currentRelation = ComparePointToPlane(currentVertex.Position, testPlane);
                PointPlaneRelation previousRelation = ComparePointToPlane(previousVertex.Position, testPlane);

                if (previousRelation == PointPlaneRelation.InFront && currentRelation == PointPlaneRelation.InFront)
                {
                }
                else if (previousRelation == PointPlaneRelation.Behind && currentRelation == PointPlaneRelation.InFront)
                {
                    float interpolant = Edge.IntersectsPlane(testPlane, previousVertex.Position, currentVertex.Position);
                    position2 = Vector3.Lerp(previousVertex.Position, currentVertex.Position, interpolant);
                    position2Set = true;
                }
                else if (previousRelation == PointPlaneRelation.InFront && currentRelation == PointPlaneRelation.Behind)
                {
                    // Reverse order here so that clipping remains consistent for either CW or CCW testing
                    float interpolant = Edge.IntersectsPlane(testPlane, currentVertex.Position, previousVertex.Position);
                    position1 = Vector3.Lerp(currentVertex.Position, previousVertex.Position, interpolant);
                    position1Set = true;
                }
                else if (previousRelation == PointPlaneRelation.Behind && currentRelation == PointPlaneRelation.Behind)
                {
                }
                else if (currentRelation == PointPlaneRelation.On)
                {
                    if (previousRelation == PointPlaneRelation.InFront)
                    {
                        position1 = currentVertex.Position;
                        position1Set = true;
                    }
                    else if (previousRelation == PointPlaneRelation.Behind)
                    {
                        position2 = currentVertex.Position;
                        position2Set = true;
                    }
                    else
                    {
                        //						throw new System.Exception("Unhandled polygon configuration");
                    }
                }
                else if (currentRelation == PointPlaneRelation.Behind)
                {
                }
                else if (currentRelation == PointPlaneRelation.InFront)
                {
                }
                else
                {
                }
            }

            return position1Set && position2Set;
        }

        public enum PointPlaneRelation { InFront, Behind, On };

        public static PointPlaneRelation ComparePointToPlane2(Vector3 point, Plane plane)
        {
            float distance = plane.GetDistanceToPoint(point);
            if (distance < -EPSILON)
            {
                return PointPlaneRelation.InFront;
            }
            else if (distance > EPSILON)
            {
                return PointPlaneRelation.Behind;
            }
            else
            {
                return PointPlaneRelation.On;
            }
        }

        public static PointPlaneRelation ComparePointToPlane(Vector3 point, Plane plane)
        {
            float distance = plane.GetDistanceToPoint(point);
            if (distance < -EPSILON_LOWER)
            {
                return PointPlaneRelation.InFront;
            }
            else if (distance > EPSILON_LOWER)
            {
                return PointPlaneRelation.Behind;
            }
            else
            {
                return PointPlaneRelation.On;
            }
        }

        public static bool ContainsEdge(Polygon polygon, Edge candidateEdge)
        {
            // Check if any of the edges in the polygon match the candidate edge (including reversed order)
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                Vector3 position1 = polygon.Vertices[i].Position;
                Vector3 position2 = polygon.Vertices[(i + 1) % polygon.Vertices.Length].Position;

                if ((candidateEdge.Vertex1.Position == position1 && candidateEdge.Vertex2.Position == position2)
                   || (candidateEdge.Vertex2.Position == position1 && candidateEdge.Vertex1.Position == position2))
                {
                    return true;
                }
            }
            // None found that matched
            return false;
        }

        internal static bool FindEdge(Polygon polygon, Edge candidateEdge, out Edge foundEdge)
        {
            // Check if any of the edges in the polygon match the candidate edge (including reversed order)
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                Vector3 position1 = polygon.Vertices[i].Position;
                Vector3 position2 = polygon.Vertices[(i + 1) % polygon.Vertices.Length].Position;

                if ((candidateEdge.Vertex1.Position == position1 && candidateEdge.Vertex2.Position == position2)
                   || (candidateEdge.Vertex2.Position == position1 && candidateEdge.Vertex1.Position == position2))
                {
                    foundEdge = new Edge(polygon.Vertices[i], polygon.Vertices[(i + 1) % polygon.Vertices.Length]);
                    return true;
                }
            }
            // None found that matched
            foundEdge = null;
            return false;
        }

        #endregion Static Methods
    }
}

#endif