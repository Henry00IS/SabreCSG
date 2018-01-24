//#define NO_EARLYOUT

#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	internal static class BrushBuilder
	{
		static int firstPolygonUID = 0; // Not sure about this

		internal static void Build(BrushCache brushCache, int brushIndex, BrushCache[] allBrushCaches, bool isCollisionPass)
		{
			firstPolygonUID = brushCache.Polygons[0].UniqueIndex;
			if(brushCache.Mode == CSGMode.Add)
			{
				int[] polygonsRemoved = new int[brushCache.Polygons.Length];
				List<Polygon> builtPolygons = new List<Polygon>();

				// Grab the intersecting brushes so we only work with what we actually intersect
				List<BrushCache> intersectingBrushCaches = null;

				if(isCollisionPass)
				{
					intersectingBrushCaches = brushCache.IntersectingCollisionBrushCaches;
				}
				else
				{
					intersectingBrushCaches = brushCache.IntersectingVisualBrushCaches;
				}

                LinkedList<BrushChunk> brushChunks = new LinkedList<BrushChunk>();
                brushChunks.AddFirst(new BrushChunk(new List<Polygon>(brushCache.Polygons.DeepCopy())));

				for (int i = 0; i < intersectingBrushCaches.Count; i++) 
				{
					if(intersectingBrushCaches[i] == null)
					{
						continue;
					}

					int index = Array.IndexOf(allBrushCaches, intersectingBrushCaches[i]);
					if(index <= brushIndex) // Earlier brush
					{
						// Split the chunks so that polygons can be removed as necessary
						SplitBy(brushChunks, intersectingBrushCaches[i]);
					}
					else
					{
						// Split by the other brush and remove any chunks inside the later brush
						SplitAndRemove(brushChunks, intersectingBrushCaches[i], polygonsRemoved);
					}
				}

                List<Polygon> excludedPolygons = new List<Polygon>();
				for (int i = 0; i < intersectingBrushCaches.Count; i++) 
				{
					if(intersectingBrushCaches[i] == null)
					{
						continue;
					}

					int index = Array.IndexOf(allBrushCaches, intersectingBrushCaches[i]);
					if(index <= brushIndex) // Earlier brush
					{
						if(intersectingBrushCaches[i].Mode == CSGMode.Add)
						{
//							// Split the chunks so that polygons can be removed as necessary
//							SplitBy(brushChunks, intersectingBrushCaches[i]);
							// Remove any polygons that are contained in an earlier addition
							RemoveInteriorPolygons(brushChunks, intersectingBrushCaches[i], excludedPolygons, polygonsRemoved);
						}
						else
						{
							// Restore any polygons that were removed when inside an addition but are now inside a subsequent subtraction
							RestoreInteriorPolygons(brushChunks, intersectingBrushCaches[i], excludedPolygons, polygonsRemoved);
						}
					}
					else // Later brush
					{
						if(intersectingBrushCaches[i].Mode == CSGMode.Add)
						{
							// Remove any polygons that will be contrained by a later additive brush
							RemoveInteriorPolygons(brushChunks, intersectingBrushCaches[i], excludedPolygons, polygonsRemoved);
						}
						else
						{
							// If the later brush is subtractive, extract any subtractive polygons and add to these chunks
							ExtractSubtractionPolygons(brushCache, brushChunks, intersectingBrushCaches[i], isCollisionPass);
						}
					}
				}

                // Concat all the chunk polygons into one list
                for (LinkedListNode<BrushChunk> current = brushChunks.First; current != null; current = current.Next)
				{
					builtPolygons.AddRange(current.Value.Polygons);
				}

                // TODO: Is it faster without RemoveAt?
                for (int i = 0; i < polygonsRemoved.Length; i++) 
				{
					if(polygonsRemoved[i] == 0) // Found a complete polygon
					{
						int uniqueIndex = firstPolygonUID + i;
						// TODO: Remove existing polygons with UniqueID
						for (int j = 0; j < builtPolygons.Count; j++) 
						{
							if(builtPolygons[j].UniqueIndex == uniqueIndex)
							{
								builtPolygons.RemoveAt(j);
								j--;
							}
						}
						// Add in polygon from the source polygons
						builtPolygons.Add(brushCache.Polygons[i].DeepCopy());
					}
				}


                // Remove any temporary polygons
                List<Polygon> newBuiltPolygons = new List<Polygon>(builtPolygons.Count);
                for (int i = 0; i < builtPolygons.Count; i++) 
				{
					if(!builtPolygons[i].ExcludeFromFinal)
					{
                        newBuiltPolygons.Add(builtPolygons[i]);
					}
				}

                newBuiltPolygons.TrimExcess();

                builtPolygons = newBuiltPolygons;

                // Finally, provide the brush cache with the built polygons
                if (isCollisionPass)
				{
					brushCache.SetCollisionBuiltPolygons(builtPolygons);
				}
				else
				{
					brushCache.SetVisualBuiltPolygons(builtPolygons);
				}

//				for (int i = 0; i < brushChunks.Count; i++) 
//				{
//					DebugExclude.DisplayChunk(DebugExclude.hackyHolder, brushChunks[i], brushIndex);
//				}
			}
			else // Subtract
			{
				// Do nothing for subtractive brushes. This is handled by the additive brushes they interact with
				if(isCollisionPass)
				{
					brushCache.SetCollisionBuiltPolygons(brushCache.BuiltCollisionPolygons);
				}
				else
				{
					brushCache.SetVisualBuiltPolygons(brushCache.BuiltVisualPolygons);
				}
			}
		}

		private static void ExtractSubtractionPolygons(BrushCache brushCacheSource, LinkedList<BrushChunk> brushChunks, BrushCache removee, bool isCollisionPass)
		{
			Polygon[] polygons = removee.Polygons;
            for (LinkedListNode<BrushChunk> current = brushChunks.First; current != null; current = current.Next)
			{
#if !NO_EARLYOUT
                if(!current.Value.GetBounds().IntersectsApproximate(removee.Bounds))
                {
                    continue;
                }
#endif
                List<Polygon> currentPolygons = current.Value.Polygons;
				for (int j = 0; j < currentPolygons.Count; j++) 
				{
					Polygon previousPolygon = currentPolygons[j];
					if(previousPolygon.ExcludeFromFinal)
					{
						for (int k = 0; k < polygons.Length; k++) 
						{
							if(GeometryHelper.PolygonContainsPolygon(polygons[k], previousPolygon))
							{
								previousPolygon = previousPolygon.DeepCopy();
								// Transfer attributes from the source polygon to the chunk polygon
								previousPolygon.UniqueIndex = polygons[k].UniqueIndex;
								previousPolygon.Material = polygons[k].Material;
								previousPolygon.ExcludeFromFinal = false;
								previousPolygon.UserExcludeFromFinal = polygons[k].UserExcludeFromFinal;

								Vertex[] vertices = previousPolygon.Vertices;
								// TODO: Optimise the Triangulate code
								// Triangulate the polygon so that we can determine a normal from the surrounding three vertices
								Polygon[] triangles = PolygonFactory.Triangulate(polygons[k]);

								for (int l = 0; l < vertices.Length; l++) 
								{
									Vertex interpolatedVertex = CalculateInterpolated(triangles, vertices[l].Position);
									vertices[l].Normal = -interpolatedVertex.Normal; // Flip normal as face is flipped
									vertices[l].Color = interpolatedVertex.Color;
									vertices[l].UV = interpolatedVertex.UV;
								}

                                currentPolygons[j] = previousPolygon;


								if(isCollisionPass)
								{
									BrushCache.NotifyOfStolenCollisionPolygon(removee, brushCacheSource, currentPolygons[j]);
								}
								else
								{
									BrushCache.NotifyOfStolenVisualPolygon(removee, brushCacheSource, currentPolygons[j]);
								}
							}
						}
					}
				}
			}
		}

		private static Vertex CalculateInterpolated(Polygon[] triangles, FixVector3 worldPosition)
		{
			// Find which triangle contains the target point
			for (int i = 0; i < triangles.Length; i++) 
			{
				if(TriangleContainsPoint(triangles[i], worldPosition))
				{
					// Found a triangle that contains the point, so interpolate the normal using barycentric coords
					return GetVertexForPosition(triangles[i].Vertices[0],
						triangles[i].Vertices[1],
						triangles[i].Vertices[2],
						worldPosition);
				}
			}

			// Couldn't match any
			Debug.LogError("Could not match point");
			return triangles[0].Vertices[0].DeepCopy();
		}

		private static bool TriangleContainsPoint(Polygon polygon, FixVector3 point)
		{
			Vertex[] vertices = polygon.Vertices;
            FixVector3 planeNormal = (FixVector3)polygon.Plane.normal;
			for (int i = 0; i < vertices.Length; i++) 
			{
                FixVector3 point1 = vertices[i].Position;
                FixVector3 point2 = vertices[(i+1)%vertices.Length].Position;

                FixVector3 edge = point2 - point1; // Direction from a vertex to the next
                FixVector3 polygonNormal = planeNormal;

                // Cross product of the edge with the polygon's normal gives the edge's normal
                FixVector3 edgeNormal = FixVector3.Cross(edge.normalized, polygonNormal);

                FixVector3 edgeCenter = (point1+point2) * (Fix64)0.5f;

                FixVector3 pointToEdgeCentroid = edgeCenter - point;

				Fix64 dot = FixVector3.Dot(edgeNormal, pointToEdgeCentroid);
				// If the point is outside an edge this will return a negative value
				if(dot < -(Fix64)0.1f)
				{
					return false;
				}
			}

			// Point not outside any edge
			return true;
		}

		/// <summary>
		/// Adapted from GetUVForPosition
		/// </summary>
		public static Vertex GetVertexForPosition(Vertex vertex1, Vertex vertex2, Vertex vertex3,
			FixVector3 newPosition)
		{
            FixVector3 pos1 = vertex1.Position;
            FixVector3 pos2 = vertex2.Position;
            FixVector3 pos3 = vertex3.Position;

            //Plane plane = new Plane(pos1,pos2,pos3); // TODO Is it safe to replace this with the polygon Plane property?
            //Vector3 planePoint = MathHelper.ClosestPointOnPlane(newPosition, plane);
            FixVector3 planePoint = newPosition;
            // calculate vectors from point f to vertices p1, p2 and p3:
            FixVector3 f1 = pos1-planePoint;
            FixVector3 f2 = pos2-planePoint;
            FixVector3 f3 = pos3-planePoint;

            // calculate the areas (parameters order is essential in this case):
            FixVector3 va = FixVector3.Cross(pos1-pos2, pos1-pos3); // main triangle cross product
            FixVector3 va1 = FixVector3.Cross(f2, f3); // p1's triangle cross product
            FixVector3 va2 = FixVector3.Cross(f3, f1); // p2's triangle cross product
            FixVector3 va3 = FixVector3.Cross(f1, f2); // p3's triangle cross product

			Fix64 a = va.magnitude; // main triangle area

            // calculate barycentric coordinates with sign:
            Fix64 a1 = va1.magnitude/a * Fix64.FixSign(FixVector3.Dot(va, va1));
            Fix64 a2 = va2.magnitude/a * Fix64.FixSign(FixVector3.Dot(va, va2));
            Fix64 a3 = va3.magnitude/a * Fix64.FixSign(FixVector3.Dot(va, va3));

			Vertex vertex = vertex1.DeepCopy();
			// Interpolate normal and UV based on the barycentric coordinates
			vertex.Normal = vertex1.Normal * a1 + vertex2.Normal * a2 + vertex3.Normal * a3;
			vertex.UV = vertex1.UV * (float)a1 + vertex2.UV * (float)a2 + vertex3.UV * (float)a3;
			// Interpolate the color, slightly more complex as need to implicit cast from Color32 to Color and back for interpolation
			Color color1 = vertex1.Color;
			Color color2 = vertex1.Color;
			Color color3 = vertex1.Color;
			vertex.Color = color1 * (float)a1 + color2 * (float)a2 + color3 * (float)a3;

			return vertex;
		}

		private static void RemoveInteriorPolygons(LinkedList<BrushChunk> brushChunks, BrushCache removee, List<Polygon> excludedPolygons, int[] polygonsRemoved)
		{
			// TODO: If a polygon is in a subtract last rather than an add it shold not be removed
			Polygon[] polygons = removee.Polygons;
            FixVector3 brushCenter = (FixVector3)removee.Bounds.center;

            for (LinkedListNode<BrushChunk> current = brushChunks.First; current != null; current = current.Next)
			{
#if !NO_EARLYOUT
                if (!current.Value.GetBounds().IntersectsApproximate(removee.Bounds))
                {
                    continue;
                }
#endif

                List<Polygon> chunkPolygons = current.Value.Polygons;
				for (int i = 0; i < chunkPolygons.Count; i++) 
				{
					if(chunkPolygons[i].ExcludeFromFinal == false)
					{
						FixVector3 polygonCenter = chunkPolygons[i].GetCenterPoint();
						Fix64 distanceInside = GeometryHelper.PolyhedronContainsPointDistance(polygons, polygonCenter);
						if(distanceInside > (Fix64)1E-05)
						{
							int relativeIndex = chunkPolygons[i].UniqueIndex - firstPolygonUID;
							MarkPolygonRemoved(relativeIndex, polygonsRemoved);

							// Well inside the brush, so remove it
							chunkPolygons[i].ExcludeFromFinal = true;
							excludedPolygons.Add(chunkPolygons[i]);
						}
						else if(distanceInside >= -(Fix64)1E-05)
						{
							// Edge case, make sure the face is towards the brush
							if(FixVector3.Dot(brushCenter - polygonCenter, (FixVector3)chunkPolygons[i].Plane.normal) > Fix64.Zero)
							{
								int relativeIndex = chunkPolygons[i].UniqueIndex - firstPolygonUID;
								MarkPolygonRemoved(relativeIndex, polygonsRemoved);

								chunkPolygons[i].ExcludeFromFinal = true;
								excludedPolygons.Add(chunkPolygons[i]);
							}
						}
					}
				}
			}
		}

		private static void RestoreInteriorPolygons(LinkedList<BrushChunk> brushChunks, BrushCache removee, List<Polygon> excludedPolygons, int[] polygonsRemoved)
		{
			// TODO: If a polygon is in a subtract last rather than an add it shold not be removed
			Polygon[] polygons = removee.Polygons;
            FixVector3 brushCenter = (FixVector3)removee.Bounds.center;

			for (int i = 0; i < excludedPolygons.Count; i++) 
			{
                FixVector3 polygonCenter = excludedPolygons[i].GetCenterPoint();

#if !NO_EARLYOUT
                if(!removee.Bounds.ContainsApproximate(polygonCenter))
                {
                    continue;
                }
#endif

                Fix64 distanceInside = GeometryHelper.PolyhedronContainsPointDistance(polygons, polygonCenter);
				if(distanceInside > (Fix64)1E-05)
				{
					int relativeIndex = excludedPolygons[i].UniqueIndex - firstPolygonUID;
					MarkPolygonRestored(relativeIndex, polygonsRemoved);

					// Well inside the brush, so restore it
					excludedPolygons[i].ExcludeFromFinal = false;
					excludedPolygons.Remove(excludedPolygons[i]);
					i--;
				}
				else if(distanceInside >= -(Fix64)1e-5f)
				{
					// Edge case, make sure the face is towards the brush
					if(FixVector3.Dot(brushCenter - polygonCenter, (FixVector3)excludedPolygons[i].Plane.normal) > Fix64.Zero)
					{
						int relativeIndex = excludedPolygons[i].UniqueIndex - firstPolygonUID;
						MarkPolygonRestored(relativeIndex, polygonsRemoved);

						excludedPolygons[i].ExcludeFromFinal = false;
						excludedPolygons.Remove(excludedPolygons[i]);
						i--;
					}
				}
			}
		}

		private static void SplitBy(LinkedList<BrushChunk> brushChunks, BrushCache splitter)
		{
			Polygon[] polygons = splitter.Polygons;
			for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++) 
			{
				Plane splitPlane = polygons[polygonIndex].Plane;

                for (LinkedListNode<BrushChunk> current = brushChunks.First; current != null; current = current.Next)
                {
#if !NO_EARLYOUT
                    if (!current.Value.GetBounds().IntersectsApproximate(splitter.Bounds))
                    {
                        continue;
                    }
#endif

                    BrushChunk chunkIn;
                    BrushChunk chunkOut;

                    if (current.Value.SplitByPlane(splitPlane, out chunkIn, out chunkOut))
                    {
                        // TODO: If chunkIn is fully inside polygons, delete it
                        current.Value = chunkOut;
                        current = brushChunks.AddAfter(current, chunkIn);
                    }
                }
			}
		}

		private static void SplitAndRemove(LinkedList<BrushChunk> brushChunks, BrushCache removee, int[] polygonsRemoved)
		{
			Polygon[] polygons = removee.Polygons;
			Plane[] splitPlanes = removee.SplitPlanes;

			for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++) 
			{
				Plane splitPlane = splitPlanes[polygonIndex];

                for (LinkedListNode<BrushChunk> current = brushChunks.First; current != null;)
				{
#if !NO_EARLYOUT
                    if (!current.Value.GetBounds().IntersectsApproximate(removee.Bounds))
                    {
                        current = current.Next;
                        continue;
                    }
#endif

                    BrushChunk chunkIn;
					BrushChunk chunkOut;

					if(current.Value.SplitByPlane(splitPlane, out chunkIn, out chunkOut))
					{
                        // TODO: If chunkIn is fully inside polygons, delete it
                        current.Value = chunkOut;

						if(!GeometryHelper.PolyhedronContainsPolyhedron(polygons, chunkIn.Polygons))
						{
                            current = brushChunks.AddAfter(current, chunkIn);
						}
						else
						{
							for (int i = 0; i < chunkIn.Polygons.Count; i++) 
							{
								if(chunkIn.Polygons[i].UniqueIndex != -1)
								{
									int relativeIndex = chunkIn.Polygons[i].UniqueIndex - firstPolygonUID;
									MarkPolygonRemoved(relativeIndex, polygonsRemoved);
								}
							}
						}

                        // Next iteration
                        current = current.Next;
					}
					else
					{
                        LinkedListNode<BrushChunk> next = current.Next;
                        BrushChunk chunk = current.Value;
						if(GeometryHelper.PolyhedronContainsPolyhedron(polygons, chunk.Polygons))
						{
							for (int i = 0; i < chunk.Polygons.Count; i++) 
							{
								if(chunk.Polygons[i].UniqueIndex != -1)
								{
									int relativeIndex = chunk.Polygons[i].UniqueIndex - firstPolygonUID;
									MarkPolygonRemoved(relativeIndex, polygonsRemoved);
								}
							}

                            brushChunks.Remove(current);
						}
                        // Next iteration
                        current = next;
					}
				}
			}
		}

		private static void MarkPolygonRemoved(int relativeIndex, int[] polygonsRemoved)
		{
			// Because some polygons may be stolen from subtractive brushes, make sure this polygon actually belongs
			if(relativeIndex >= 0 && relativeIndex < polygonsRemoved.Length)
			{
				polygonsRemoved[relativeIndex]++;
			}
		}

		private static void MarkPolygonRestored(int relativeIndex, int[] polygonsRemoved)
		{
			// Because some polygons may be stolen from subtractive brushes, make sure this polygon actually belongs
			if(relativeIndex >= 0 && relativeIndex < polygonsRemoved.Length)
			{
				polygonsRemoved[relativeIndex]--;
			}
		}
	}
}
#endif