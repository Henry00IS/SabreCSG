#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Holds two vectors that are used to map UV directions to 3D space, along with scaled versions
	/// </summary>
	public class UVOrientation
	{
		public FixVector3 NorthVector;
		public FixVector3 EastVector;

		public Fix64 NorthScale = (Fix64)0.5f;
		public Fix64 EastScale = (Fix64)0.5f;
	}

	/// <summary>
	/// Provides utility methods for working with the surfaces of brushes, such as materials, UVs and normals
	/// </summary>
	public static class SurfaceUtility
	{
		/// <summary>
		/// Set all the vertex normals of the polygon to the polygon normal, when applied to multiple polygons this creates a faceted (not smooth) appearance
		/// </summary>
		/// <param name="polygon">Polygon to facet.</param>
		public static void FacetPolygon(Polygon polygon)
		{
			for (int i = 0; i < polygon.Vertices.Length; i++) 
			{
				Vertex vertex = polygon.Vertices[i];
				vertex.Normal = (FixVector3)polygon.Plane.normal;
			}
		}

		/// <summary>
		/// Smooths the vertex normals on a polygon where a vertex in the supplied polygon shares a position with other vertices in allPolygons. Vertices from other polygons in allPolygons are only considered if they share the same position and the angle between the two parent polygons is smaller than the smoothing angle. If no other vertices are matched, the vertex normal remains unchanged.
		/// </summary>
		/// <param name="polygon">Polygon whose vertices will be smoothed.</param>
		/// <param name="allPolygons">All polygons used to provide context for how they vertex normal should be smoothed, typically the brush's polygons. Note that this can include the provided Polygon as it will simply be skipped.</param>
		/// <param name="smoothingAngle">Maximum angle between polygons that can be considered for smoothing.</param>
		public static void SmoothPolygon(Polygon polygon, Polygon[] allPolygons, float smoothingAngle = 60)
		{
			for (int i = 0; i < polygon.Vertices.Length; i++) 
			{
				Vertex vertex = polygon.Vertices[i];

				Vector3 sourceNormal = polygon.Plane.normal;

				Vector3 newNormal = sourceNormal;
				int totalNormalCount = 1;

				for (int j = 0; j < allPolygons.Length; j++) 
				{
					Polygon otherPolygon = allPolygons[j];
					// Ignore the same polygon
					if(otherPolygon != polygon)
					{
						for (int k = 0; k < otherPolygon.Vertices.Length; k++) 
						{
							Vertex otherVertex = otherPolygon.Vertices[k];
							if(otherVertex.Position == vertex.Position)
							{
								if(Vector3.Angle(sourceNormal, otherPolygon.Plane.normal) <= smoothingAngle)
								{
									newNormal += otherPolygon.Plane.normal;
									totalNormalCount++;
								}
							}
						}
					}
				}

				vertex.Normal = (FixVector3)newNormal * (Fix64.One / (Fix64)totalNormalCount);
			} 
		}

		/// <summary>
		/// Creates a brush by extruding a supplied polygon by a specified extrusion distance.
		/// </summary>
		/// <param name="sourcePolygon">Source polygon, typically transformed into world space.</param>
		/// <param name="extrusionDistance">Extrusion distance, this is the height (or depth) of the created geometry perpendicular to the source polygon.</param>
		/// <param name="outputPolygons">Output brush polygons.</param>
		/// <param name="rotation">The rotation to be supplied to the new brush transform.</param>
		public static void ExtrudePolygon(Polygon sourcePolygon, Fix64 extrusionDistance, out Polygon[] outputPolygons, out Quaternion rotation)
		{
			bool flipped = false;
			if(extrusionDistance < Fix64.Zero)
			{
				sourcePolygon.Flip();
				extrusionDistance = -extrusionDistance;
				flipped = true;
			}

			// Create base polygon
			Polygon basePolygon = sourcePolygon.DeepCopy();
			basePolygon.UniqueIndex = -1;

			rotation = Quaternion.LookRotation(basePolygon.Plane.normal);
			Quaternion cancellingRotation = Quaternion.Inverse(rotation);

			Vertex[] vertices = basePolygon.Vertices;

			for (int i = 0; i < vertices.Length; i++) 
			{
				vertices[i].Position = (FixVector3)(cancellingRotation * (Vector3)vertices[i].Position);
				vertices[i].Normal = (FixVector3)(cancellingRotation * (Vector3)vertices[i].Normal);
            }

			basePolygon.SetVertices(vertices);

			// Create the opposite polygon by duplicating the base polygon, offsetting and flipping
			FixVector3 normal = (FixVector3)basePolygon.Plane.normal;
			Polygon oppositePolygon = basePolygon.DeepCopy();
			oppositePolygon.UniqueIndex = -1;

			basePolygon.Flip();

			vertices = oppositePolygon.Vertices;
			for (int i = 0; i < vertices.Length; i++) 
			{
				vertices[i].Position += normal * extrusionDistance;
//				vertices[i].UV.x *= -1; // Flip UVs
			}
			oppositePolygon.SetVertices(vertices);

			// Now create each of the brush side polygons
			Polygon[] brushSides = new Polygon[sourcePolygon.Vertices.Length];

			for (int i = 0; i < basePolygon.Vertices.Length; i++) 
			{
				Vertex vertex1 = basePolygon.Vertices[i].DeepCopy();
				Vertex vertex2 = basePolygon.Vertices[(i+1)%basePolygon.Vertices.Length].DeepCopy();

				// Create new UVs for the sides, otherwise we'll get distortion

				Fix64 sourceDistance = FixVector3.Distance(vertex1.Position, vertex2.Position);
				float uvDistance = Vector2.Distance(vertex1.UV, vertex2.UV);

				float uvScale = (float)sourceDistance / uvDistance;

				vertex1.UV = Vector2.zero;
				if(flipped)
				{
					vertex2.UV = new Vector2(-(float)sourceDistance / uvScale,0);
				}
				else
				{
					vertex2.UV = new Vector2((float)sourceDistance / uvScale,0);
				}

				Vector2 uvDelta = vertex2.UV - vertex1.UV;

				Vector2 rotatedUVDelta = uvDelta.Rotate(90) * ((float)extrusionDistance / (float)sourceDistance);

				Vertex vertex3 = vertex1.DeepCopy();
				vertex3.Position += normal * extrusionDistance;
				vertex3.UV += rotatedUVDelta;

				Vertex vertex4 = vertex2.DeepCopy();
				vertex4.Position += normal * extrusionDistance;
				vertex4.UV += rotatedUVDelta;

				Vertex[] newVertices = new Vertex[] { vertex1, vertex2, vertex4, vertex3 };

				brushSides[i] = new Polygon(newVertices, sourcePolygon.Material, false, false);
				brushSides[i].Flip();
				brushSides[i].ResetVertexNormals();
			}

			List<Polygon> polygons = new List<Polygon>();
			polygons.Add(basePolygon);
			polygons.Add(oppositePolygon);
			polygons.AddRange(brushSides);

			outputPolygons = polygons.ToArray();
		}

		/// <summary>
		/// Finds three vertices from a polygon that are not colinear and can be used for describing the normal or UV with a high chance of reliability
		/// </summary>
		/// <param name="polygon">Source polygon.</param>
		/// <param name="vertex1">Vertex1.</param>
		/// <param name="vertex2">Vertex2.</param>
		/// <param name="vertex3">Vertex3.</param>
		public static void GetPrimaryPolygonDescribers(Polygon polygon, out Vertex vertex1, out Vertex vertex2, out Vertex vertex3)
		{
			// Start with the first three vertices
			int vertexIndex1 = 0;
			int vertexIndex2 = 1;
			int vertexIndex3 = 2;

			FixVector3 pos1 = polygon.Vertices[vertexIndex1].Position;
            FixVector3 pos2 = polygon.Vertices[vertexIndex2].Position;
            FixVector3 pos3 = polygon.Vertices[vertexIndex3].Position;

			Plane testPlane = new Plane((Vector3)pos1, (Vector3)pos2, (Vector3)pos3);

			// If we didn't find a good normal on the first attempt and there are more vertices to try
			if(testPlane.normal == Vector3.zero && polygon.Vertices.Length > 3)
			{
				// Walk through the remaining vertices until we find one that produces a valid normal, or there are no further vertices
				for (vertexIndex3 = 3; vertexIndex3 < polygon.Vertices.Length; vertexIndex3++) 
				{
					pos3 = polygon.Vertices[vertexIndex3].Position;

					testPlane = new Plane((Vector3)pos1, (Vector3)pos2, (Vector3)pos3);

					if(testPlane.normal != Vector3.zero)
					{
						// Found a valid normal, break out
						break;
					}
				}
			}

			// Return the best three vertices we could find
			vertex1 = polygon.Vertices[vertexIndex1];
			vertex2 = polygon.Vertices[vertexIndex2];
			vertex3 = polygon.Vertices[vertexIndex3];
		}

		/// <summary>
		/// Extract the world vectors that correspond to UV north (0,1) and UV east (1,0). 
		/// </summary>
		/// <returns>The normalized world vectors for UV north and east, as well as vectors scaled by UV scale in that direction.</returns>
		/// <param name="polygon">Source polygon local to the brush.</param>
		/// <param name="brushTransform">Brush transform, used for transforming to world space.</param>
		public static UVOrientation GetNorthEastVectors(Polygon polygon, Transform brushTransform)
		{
			Vertex vertex1;
			Vertex vertex2;
			Vertex vertex3;
			// Get three vertices which will reliably give us good UV information (i.e. not collinear)
			GetPrimaryPolygonDescribers(polygon, out vertex1, out vertex2, out vertex3);

			// Take 3 positions and their corresponding UVs
			FixVector3 pos1 = (FixVector3)brushTransform.TransformPoint((Vector3)vertex1.Position);
            FixVector3 pos2 = (FixVector3)brushTransform.TransformPoint((Vector3)vertex2.Position);
            FixVector3 pos3 = (FixVector3)brushTransform.TransformPoint((Vector3)vertex3.Position);

			Vector2 uv1 = vertex1.UV;
			Vector2 uv2 = vertex2.UV;
			Vector2 uv3 = vertex3.UV;

			// Construct a matrix to map to the triangle's UV space
			Matrix2x2 uvMatrix = new Matrix2x2()
			{
				m00 = (Fix64)uv2.x - (Fix64)uv1.x, m10 = (Fix64)uv3.x - (Fix64)uv1.x,
				m01 = (Fix64)uv2.y - (Fix64)uv1.y, m11 = (Fix64)uv3.y - (Fix64)uv1.y,
			};

			// Invert the matrix to map from UV space
			Matrix2x2 uvMatrixInverted = uvMatrix.Inverse;

			// Construct a matrix to map to the triangle's world space
			Matrix3x2 positionMatrix = new Matrix3x2()
			{
				m00 = pos2.x - pos1.x, m10 = pos3.x - pos1.x,
				m01 = pos2.y - pos1.y, m11 = pos3.y - pos1.y,
				m02 = pos2.z - pos1.z, m12 = pos3.z - pos1.z,
			};

			// Multiply the inverted UVs by the positional matrix to get the UV vectors in world space
			Matrix3x2 multipliedMatrix = positionMatrix.Multiply(uvMatrixInverted);

            // Extract the world vectors that correspond to UV north (0,1) and UV east (1,0). Note that these aren't
            // normalized and their magnitude is the reciprocal of tiling
            FixVector3 eastVectorScaled = new FixVector3(multipliedMatrix.m00, multipliedMatrix.m01, multipliedMatrix.m02);
            FixVector3 northVectorScaled = new FixVector3(multipliedMatrix.m10, multipliedMatrix.m11, multipliedMatrix.m12);

			return new UVOrientation()
			{
				NorthVector = northVectorScaled.normalized,
				EastVector = eastVectorScaled.normalized,
				NorthScale = northVectorScaled.magnitude,
				EastScale = eastVectorScaled.magnitude,
			};
		}

		/// <summary>
		/// Returns the UV from the center of the polygon
		/// </summary>
		/// <returns>The UV at the polygon center.</returns>
		/// <param name="polygon">Source polygon.</param>
		public static Vector2 GetUVOffset(Polygon polygon)
		{
			Vertex vertex1;
			Vertex vertex2;
			Vertex vertex3;
			// Get three vertices which will reliably give us good UV information (i.e. not collinear)
			SurfaceUtility.GetPrimaryPolygonDescribers(polygon, out vertex1, out vertex2, out vertex3);

			Vector2 newUV = GeometryHelper.GetUVForPosition(vertex1.Position,
				vertex2.Position,
				vertex3.Position,
				vertex1.UV,
				vertex2.UV,
				vertex3.UV,
				polygon.GetCenterPoint());

			return newUV;
		}

		/// <summary>
		/// Returns the center UV of a set of polygons if the UV is the same for all polygons. Works per component, so if all offsets are the same in one axis but not the other, the first axis value will be returned but the second will be <c>null</c>.
		/// </summary>
		/// <returns>A Pair of nullable floats, representing U and V offsets, if a component was not matched it will be null.</returns>
		/// <param name="polygons">Source polygons.</param>
		public static Pair<float?, float?> GetUVOffset(List<Polygon> polygons)
		{
			float? northOffset = 0;
			float? eastOffset = 0;

			for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++) 
			{
				Polygon polygon = polygons[polygonIndex];

				Vector2 uvOffset = SurfaceUtility.GetUVOffset(polygon);

				if(polygonIndex == 0)
				{
					northOffset = uvOffset.y;
					eastOffset = uvOffset.x;
				}
				else
				{
					if(!northOffset.HasValue || !((Fix64)northOffset.Value).EqualsWithEpsilon((Fix64)uvOffset.y))
					{
						northOffset = null;
					}

					if(!eastOffset.HasValue || !((Fix64)eastOffset.Value).EqualsWithEpsilon((Fix64)uvOffset.x))
					{
						eastOffset = null;
					}
				}
			}

			return new Pair<float?, float?> (eastOffset, northOffset);
		}

		/// <summary>
		/// Sets the material of all polygons on a brush to the supplied material
		/// </summary>
		/// <param name="brush">Brush to apply material to.</param>
		/// <param name="material">Material to apply.</param>
		public static void SetAllPolygonsMaterials(Brush brush, Material material)
		{
			Polygon[] polygons = brush.GetPolygons();
			for (int i = 0; i < polygons.Length; i++) 
			{
				polygons[i].Material = material;
			}
		}
	}
}
#endif