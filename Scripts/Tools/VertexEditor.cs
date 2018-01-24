#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
	public class VertexEditor : Tool
	{
		static Fix64 EDGE_SCREEN_TOLERANCE = (Fix64)15f;

		List<Edge> selectedEdges = new List<Edge>();
		Dictionary<Vertex, Brush> selectedVertices = new Dictionary<Vertex, Brush>();

		bool moveInProgress = false; 

		bool isMarqueeSelection = false; // Whether the user is (or could be) dragging a marquee box
        bool marqueeCancelled = false;

        Vector2 marqueeStart;
		Vector2 marqueeEnd;

		bool pivotNeedsReset = false;

		Dictionary<Vertex, FixVector3> startPositions = new Dictionary<Vertex, FixVector3>();

		// Configured by the user
		Fix64 weldTolerance = (Fix64)0.1f;
		Fix64 scale = (Fix64)1f;

		void ClearSelection()
		{
			selectedEdges.Clear();
			selectedVertices.Clear();
		}

		void RemoveDisjointedVertices()
		{
			List<Vertex> verticesToRemove = new List<Vertex>();

			// Calculate what selected vertices no longer exist in their brush
			foreach (KeyValuePair<Vertex, Brush> selectedVertex in selectedVertices) 
			{
				Polygon[] polygons = selectedVertex.Value.GetPolygons();

				bool vertexPresent = false;
				// Check if the vertex is actually in the brush
				for (int i = 0; i < polygons.Length; i++) 
				{
					if(System.Array.IndexOf(polygons[i].Vertices, selectedVertex.Key) != -1)
					{
						// Found the vertex, break out the loop
						vertexPresent = true;
						break;
					}
				}

				if(!vertexPresent)
				{
					// Vertex wasn't there, so let's remove from selection
					verticesToRemove.Add(selectedVertex.Key);
				}
			}

			// Now actually remove the vertices in a separate loop (can't do this while iterating over the dictionary)
			for (int i = 0; i < verticesToRemove.Count; i++) 
			{
				selectedVertices.Remove(verticesToRemove[i]);
			}
		}

		public override void OnUndoRedoPerformed ()
		{
			base.OnUndoRedoPerformed ();

			// Undo/redo may mean that some selected vertices no longer exist in brushes, so strip them out to stop errors
			RemoveDisjointedVertices();
		}

		List<PrimitiveBrush> AutoWeld()
		{
            // Track the brushes that welding has changed
            List<PrimitiveBrush> changedBrushes = new List<PrimitiveBrush>();

			// Automatically weld any vertices that have been brought too close together
			if(primaryTargetBrush != null && selectedVertices.Count > 0)
			{
				Fix64 autoWeldTolerance = (Fix64)0.001f;

				Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();

				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
				}

				bool selectionCleared = false;

				foreach (PrimitiveBrush brush in targetBrushes) 
				{
                    Polygon[] sourcePolygons = brush.GetPolygons();
                    // Make a copy so that we can differentiate newPolygons from the original, since welding updates affected polygons in place
                    Polygon[] sourcePolygonsCopy = sourcePolygons.DeepCopy();

					List<Vertex> allVertices = new List<Vertex>();
					for (int i = 0; i < sourcePolygonsCopy.Length; i++) 
					{
						allVertices.AddRange(sourcePolygonsCopy[i].Vertices);
					}

					Polygon[] newPolygons = VertexUtility.WeldNearbyVertices(autoWeldTolerance, sourcePolygonsCopy, allVertices);

                    bool hasChanged = false;

                    if(newPolygons.Length != sourcePolygons.Length)
                    {
                        hasChanged = true;
                    }

                    if(!hasChanged)
                    {
                        for (int i = 0; i < sourcePolygons.Length; i++)
                        {
                            if(sourcePolygons[i].Vertices.Length != newPolygons[i].Vertices.Length)
                            {
                                hasChanged = true;
                                break;
                            }
                        }
                    }

					if(hasChanged)
					{
						Undo.RecordObject(brush.transform, "Auto Weld Vertices");
						Undo.RecordObject(brush, "Auto Weld Vertices");

						if(!selectionCleared)
						{
							ClearSelection();
							selectionCleared = true;
						}
						brush.SetPolygons(newPolygons);

						SelectVertices(brush, newPolygons, refinedSelections[brush]);

                        // Brush has changed so mark it to be returned
                        changedBrushes.Add(brush);
                    }
				}
			}
            // Return the brushes that welding has changed
            return changedBrushes;
        }

		public void ScaleSelectedVertices(Fix64 scalar)
		{	
			FixVector3 scalarCenter = GetSelectedCenter();

			// So we know which polygons need to have their normals recalculated
			List<Polygon> affectedPolygons = new List<Polygon>();

			foreach (PrimitiveBrush brush in targetBrushes) 
			{
				Polygon[] polygons = brush.GetPolygons();

				for (int i = 0; i < polygons.Length; i++) 
				{
					Polygon polygon = polygons[i];

					int vertexCount = polygon.Vertices.Length;

					FixVector3[] newPositions = new FixVector3[vertexCount];
					Vector2[] newUV = new Vector2[vertexCount];

					for (int j = 0; j < vertexCount; j++) 
					{
						newPositions[j] = polygon.Vertices[j].Position;
						newUV[j] = polygon.Vertices[j].UV;
					}

					bool polygonAffected = false;
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						if(selectedVertices.ContainsKey(vertex))
						{
							FixVector3 newPosition = vertex.Position;
							newPosition = (FixVector3)brush.transform.TransformPoint((Vector3)newPosition);
							newPosition -= scalarCenter;
							newPosition *= scalar;
							newPosition += scalarCenter;

							newPosition = (FixVector3)brush.transform.InverseTransformPoint((Vector3)newPosition);

							newPositions[j] = newPosition;

							newUV[j] = GeometryHelper.GetUVForPosition(polygon, newPosition);

							polygonAffected = true;
						}
					}

					if(polygonAffected)
					{
						affectedPolygons.Add(polygon);
					}

					// Apply all the changes to the polygon
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						vertex.Position = newPositions[j];
						vertex.UV = newUV[j];
					}

					polygon.CalculatePlane();
				}
			}

			if(affectedPolygons.Count > 0)
			{
				for (int i = 0; i < affectedPolygons.Count; i++) 
				{
					affectedPolygons[i].ResetVertexNormals();
				}

				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					brush.Invalidate(true);

					brush.BreakTypeRelation();
				}
			}
		}

		public void TranslateSelectedVertices(FixVector3 worldDelta)
		{	
			foreach (PrimitiveBrush brush in targetBrushes) 
			{
				bool anyAffected = false;

				Polygon[] polygons = brush.GetPolygons();
				FixVector3 localDelta = (FixVector3)brush.transform.InverseTransformDirection((Vector3)worldDelta);

				for (int i = 0; i < polygons.Length; i++) 
				{
					Polygon polygon = polygons[i];

					polygon.CalculatePlane();
					FixVector3 previousPlaneNormal = (FixVector3)polygons[i].Plane.normal;

					int vertexCount = polygon.Vertices.Length;

					FixVector3[] newPositions = new FixVector3[vertexCount];
					Vector2[] newUV = new Vector2[vertexCount];

					for (int j = 0; j < vertexCount; j++) 
					{
						newPositions[j] = polygon.Vertices[j].Position;
						newUV[j] = polygon.Vertices[j].UV;
					}

					bool polygonAffected = false;

					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						if(selectedVertices.ContainsKey(vertex))
						{
							FixVector3 startPosition = startPositions[vertex];
							FixVector3 newPosition = vertex.Position + localDelta;

							FixVector3 accumulatedDelta = newPosition - startPosition;

							if(CurrentSettings.PositionSnappingEnabled)
							{
								Fix64 snapDistance = (Fix64)CurrentSettings.PositionSnapDistance;
								//							newPosition = targetBrush.transform.TransformPoint(newPosition);
								accumulatedDelta = MathHelper.RoundFixVector3(accumulatedDelta, snapDistance);
								//							newPosition = targetBrush.transform.InverseTransformPoint(newPosition);
							}

							if(accumulatedDelta != FixVector3.zero)
							{
								newPosition = startPosition + accumulatedDelta;

								newPositions[j] = newPosition;

								newUV[j] = GeometryHelper.GetUVForPosition(polygon, newPosition);

								polygonAffected = true;
								anyAffected = true;
							}
						}
					}

					// Apply all the changes to the polygon
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						vertex.Position = newPositions[j];
						vertex.UV = newUV[j];
					}

					if(polygonAffected)
					{
						// Polygon geometry has changed, inform the polygon that it needs to recalculate its cached plane
						polygons[i].CalculatePlane();

						FixVector3 newPlaneNormal = (FixVector3)polygons[i].Plane.normal;

						// Find the rotation from the original polygon plane to the new polygon plane
						Quaternion normalRotation = Quaternion.FromToRotation((Vector3)previousPlaneNormal, (Vector3)newPlaneNormal);

						// Update the affected normals so they are rotated by the rotational difference of the polygon from translation
						for (int j = 0; j < vertexCount; j++) 
						{
							Vertex vertex = polygon.Vertices[j];
							vertex.Normal = (FixVector3)(normalRotation * (Vector3)vertex.Normal);
						}
					}
				}

				if(anyAffected) // If any polygons have changed
				{
					// Mark the polygons and brush as having changed
					brush.Invalidate(true);

					// Assume that the brush no longer resembles it's base shape, this has false positives but that's not a big issue
					brush.BreakTypeRelation();
				}
			}
		}

		public void SnapSelectedVertices(bool isAbsoluteGrid)
		{
			// So we know which polygons need to have their normals recalculated
			List<Polygon> affectedPolygons = new List<Polygon>();

			foreach (PrimitiveBrush brush in targetBrushes) 
			{
				Polygon[] polygons = brush.GetPolygons();

				for (int i = 0; i < polygons.Length; i++) 
				{
					Polygon polygon = polygons[i];
					
					int vertexCount = polygon.Vertices.Length;
					
					FixVector3[] newPositions = new FixVector3[vertexCount];
					Vector2[] newUV = new Vector2[vertexCount];
					
					for (int j = 0; j < vertexCount; j++) 
					{
						newPositions[j] = polygon.Vertices[j].Position;
						newUV[j] = polygon.Vertices[j].UV;
					}

					bool polygonAffected = false;
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						if(selectedVertices.ContainsKey(vertex))
						{
							FixVector3 newPosition = vertex.Position;
							
							Fix64 snapDistance = (Fix64)CurrentSettings.PositionSnapDistance;
							if(isAbsoluteGrid)
							{
								newPosition = (FixVector3)brush.transform.TransformPoint((Vector3)newPosition);
							}
							newPosition = MathHelper.RoundFixVector3(newPosition, snapDistance);
							if(isAbsoluteGrid)
							{
								newPosition = (FixVector3)brush.transform.InverseTransformPoint((Vector3)newPosition);
							}
							
							newPositions[j] = newPosition;

							newUV[j] = GeometryHelper.GetUVForPosition(polygon, newPosition);

							polygonAffected = true;
						}
					}

					if(polygonAffected)
					{
						affectedPolygons.Add(polygon);
					}
					
					// Apply all the changes to the polygon
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						vertex.Position = newPositions[j];
						vertex.UV = newUV[j];
					}

					polygon.CalculatePlane();
				}
			}

			if(affectedPolygons.Count > 0)
			{
				for (int i = 0; i < affectedPolygons.Count; i++) 
				{
					affectedPolygons[i].ResetVertexNormals();
				}

				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					brush.Invalidate(true);

					brush.BreakTypeRelation();
				}
			}
		}

		public bool AnySelected
		{
			get
			{
				if(selectedVertices.Count > 0 || selectedEdges.Count > 0)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		
		// Used so that the gizmo for moving the points is positioned at the average between the selection
		public FixVector3 GetSelectedCenter()
		{
			FixVector3 average = FixVector3.zero;
			int numberFound = 0;

			foreach (KeyValuePair<Vertex, Brush> selectedVertex in selectedVertices) 
			{
				FixVector3 worldPosition = (FixVector3)selectedVertex.Value.transform.TransformPoint((Vector3)selectedVertex.Key.Position);
				average += worldPosition;
				numberFound++;
			}
			
			if(numberFound > 0)
			{
				return average / (Fix64)numberFound;
			}
			else
			{
				return FixVector3.zero;
			}
		}

		public override void ResetTool ()
		{
			ClearSelection();
		}

		public override void OnSceneGUI (UnityEditor.SceneView sceneView, Event e)
		{
			base.OnSceneGUI(sceneView, e); // Allow the base logic to calculate first

			if(primaryTargetBrush != null && AnySelected)
			{
				if(startPositions.Count == 0)
				{
					foreach (KeyValuePair<Vertex, Brush> selectedVertex in selectedVertices) 
					{
						startPositions.Add(selectedVertex.Key, selectedVertex.Key.Position);
					}				
				}

				// Make the handle respect the Unity Editor's Local/World orientation mode
				Quaternion handleDirection = Quaternion.identity;
				if(Tools.pivotRotation == PivotRotation.Local)
				{
					handleDirection = primaryTargetBrush.transform.rotation;
				}
				
				// Grab a source point and convert from local space to world
				FixVector3 sourceWorldPosition = GetSelectedCenter();


				if(e.type == EventType.MouseUp)
				{
					Undo.RecordObjects(targetBrushTransforms, "Moved Vertices");
					Undo.RecordObjects(targetBrushes, "Moved Vertices");

					List<PrimitiveBrush> changedBrushes = AutoWeld();

                    // Only invalidate the brushes that have actually changed
					foreach (PrimitiveBrush brush in changedBrushes) 
					{
						brush.Invalidate(true);

						brush.BreakTypeRelation();
					}
				}

				EditorGUI.BeginChangeCheck();
				// Display a handle and allow the user to determine a new position in world space
				FixVector3 newWorldPosition = (FixVector3)Handles.PositionHandle((Vector3)sourceWorldPosition, handleDirection);


				if(EditorGUI.EndChangeCheck())
				{
					Undo.RecordObjects(targetBrushTransforms, "Moved Vertices");
					Undo.RecordObjects(targetBrushes, "Moved Vertices");
					
					FixVector3 deltaWorld = newWorldPosition - sourceWorldPosition;

					//				if(deltaLocal.sqrMagnitude > 0)
					//				{
					TranslateSelectedVertices(deltaWorld);
					isMarqueeSelection = false;
					moveInProgress = true;
					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						EditorUtility.SetDirty (brush);
					}
					e.Use();
					// Shouldn't reset the pivot while the vertices are being manipulated, so make sure the pivot
					// is set to get reset at next opportunity
					pivotNeedsReset = true;
				}
				else
				{
					// The user is no longer moving a handle
					if(pivotNeedsReset)
					{
						// Pivot needs to be reset, so reset it!
						foreach (PrimitiveBrush brush in targetBrushes) 
						{
							brush.ResetPivot();	
						}

						pivotNeedsReset = false;
					}

					startPositions.Clear();
				}
			}

			if(primaryTargetBrush != null)
			{
				
				if (e.type == EventType.MouseDown) 
				{
					OnMouseDown(sceneView, e);
				}
				else if (e.type == EventType.MouseDrag) 
				{
					OnMouseDrag(sceneView, e);
				}
                // If you mouse up on a different scene view to the one you started on it's surpressed as Ignore, when
                // doing marquee selection make sure to check the real type
                else if (e.type == EventType.MouseUp || (isMarqueeSelection && e.rawType == EventType.MouseUp))
                {
					OnMouseUp(sceneView, e);
				}
			}

//			if(e.type == EventType.Repaint)
			{
				OnRepaint(sceneView, e);
			}
		}

		void SelectEdges(Brush brush, Polygon[] polygons, Edge newEdge)
		{
			// Can only select a valid edge, if it's not valid early out
			if(newEdge == null || newEdge.Vertex1 == null || newEdge.Vertex2 == null)
			{
				return;
			}
			// Select the new edge
			selectedEdges.Add(newEdge);

			for (int i = 0; i < polygons.Length; i++) 
			{
				Polygon polygon = polygons[i];

				for (int j = 0; j < polygon.Vertices.Length; j++) 
				{
					Vertex vertex = polygon.Vertices[j];

					if(newEdge.Vertex1.Position.EqualsWithEpsilon(vertex.Position)
						|| newEdge.Vertex2.Position.EqualsWithEpsilon(vertex.Position))
					{
						if(!selectedVertices.ContainsKey(vertex))
						{
							selectedVertices.Add(vertex, brush);
						}
					}

				}
			}
		}

		void SelectVertices(Brush brush, Polygon[] polygons, List<Vertex> newSelectedVertices)
		{
			for (int i = 0; i < polygons.Length; i++) 
			{
				Polygon polygon = polygons[i];

				for (int j = 0; j < polygon.Vertices.Length; j++) 
				{
					Vertex vertex = polygon.Vertices[j];

					for (int k = 0; k < newSelectedVertices.Count; k++) 
					{
						if(newSelectedVertices[k].Position == vertex.Position)
						{
							if(!selectedVertices.ContainsKey(vertex))
							{
								selectedVertices.Add(vertex, brush);
							}
							break;
						}
					}
				}
			}
		}

		void OnToolbarGUI(int windowID)
		{
			GUILayout.Label("Vertex", SabreGUILayout.GetTitleStyle());

			// Button should only be enabled if there are any vertices selected
			GUI.enabled = selectedVertices.Count > 0;

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Connect", EditorStyles.miniButton))
			{
				if(selectedVertices != null)
				{
					// Cache selection
					Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
					}

					ClearSelection();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Connect Vertices");
						Undo.RecordObject(brush, "Connect Vertices");

						List<Edge> newEdges;

//						Polygon[] newPolygons = VertexUtility.ConnectVertices(brush.GetPolygons(), refinedSelections[brush], out newEdge);
						Polygon[] newPolygons = VertexUtility.ConnectVertices(brush.GetPolygons(), refinedSelections[brush], out newEdges);
						
						if(newPolygons != null)
						{
							brush.SetPolygons(newPolygons);

							for (int i = 0; i < newEdges.Count; i++) 
							{
								SelectEdges(brush, newPolygons, newEdges[i]);
							}
						}							
					}
				}
			}

//			if (GUILayout.Button("Remove", EditorStyles.miniButton))
//			{
//				if(selectedVertices != null)
//				{
//					Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();
//
//					foreach (PrimitiveBrush brush in targetBrushes) 
//					{
//						refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
//					}
//
//					foreach (PrimitiveBrush brush in targetBrushes) 
//					{
//						Undo.RecordObject(brush.transform, "Remove Vertices");
//						Undo.RecordObject(brush, "Remove Vertices");
//
//						Polygon[] newPolygons = VertexUtility.RemoveVertices(brush.GetPolygons(), refinedSelections[brush]);
//						brush.SetPolygons(newPolygons);
//					}
//
//					ClearSelection();
//				}
//			}

			GUILayout.EndHorizontal();

			if (GUILayout.Button("Weld Selection To Mid-Point", EditorStyles.miniButton))
			{
				if(selectedVertices != null)
				{
					Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
					}

					ClearSelection();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Weld Vertices");
						Undo.RecordObject(brush, "Weld Vertices");

						Polygon[] newPolygons = VertexUtility.WeldVerticesToCenter(brush.GetPolygons(), refinedSelections[brush]);
						
						if(newPolygons != null)
						{
							brush.SetPolygons(newPolygons);
						}

						SelectVertices(brush, newPolygons, refinedSelections[brush]);
					}
				}
			}

			EditorGUILayout.BeginHorizontal();


			GUI.SetNextControlName("weldToleranceField");
			weldTolerance = (Fix64)EditorGUILayout.FloatField((float)weldTolerance);

			bool keyboardEnter = Event.current.isKey 
				&& Event.current.keyCode == KeyCode.Return 
				&& Event.current.type == EventType.KeyUp 
				&& GUI.GetNameOfFocusedControl() == "weldToleranceField";

			if (GUILayout.Button("Weld with Tolerance", EditorStyles.miniButton) || keyboardEnter)
			{
				if(selectedVertices != null)
				{
					Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
					}

					ClearSelection();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Weld Vertices");
						Undo.RecordObject(brush, "Weld Vertices");

						Polygon[] newPolygons = VertexUtility.WeldNearbyVertices(weldTolerance, brush.GetPolygons(), refinedSelections[brush]);

						if(newPolygons != null)
						{
							brush.SetPolygons(newPolygons);
						}

						SelectVertices(brush, newPolygons, refinedSelections[brush]);

					}
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Global Snap", EditorStyles.miniButton))
			{
				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					Undo.RecordObject(brush.transform, "Snap Vertices");
					Undo.RecordObject(brush, "Snap Vertices");
				}

				SnapSelectedVertices(true);
			}

			if (GUILayout.Button("Local Snap", EditorStyles.miniButton))
			{
				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					Undo.RecordObject(brush.transform, "Snap Vertices");
					Undo.RecordObject(brush, "Snap Vertices");
				}

				SnapSelectedVertices(false);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();

			GUI.SetNextControlName("scaleField");
			scale = (Fix64)EditorGUILayout.FloatField((float)scale);

			keyboardEnter = Event.current.isKey 
				&& Event.current.keyCode == KeyCode.Return 
				&& Event.current.type == EventType.KeyUp 
				&& GUI.GetNameOfFocusedControl() == "scaleField";

			if (GUILayout.Button("Scale", EditorStyles.miniButton) || keyboardEnter)
			{
				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					Undo.RecordObject(brush.transform, "Scale Vertices");
					Undo.RecordObject(brush, "Scale Vertices");
				}

				ScaleSelectedVertices(scale);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Label("Edge", SabreGUILayout.GetTitleStyle());

			GUI.enabled = selectedEdges.Count > 0;

			if (GUILayout.Button("Connect Mid-Points", EditorStyles.miniButton))
			{
				if(selectedEdges != null)
				{
					List<Edge> selectedEdgesCopy = new List<Edge>(selectedEdges);
					ClearSelection();
					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Connect Mid-Points");
						Undo.RecordObject(brush, "Connect Mid-Points");

						Polygon[] newPolygons;
						List<Edge> newEdges;
						EdgeUtility.SplitPolygonsByEdges(brush.GetPolygons(), selectedEdgesCopy, out newPolygons, out newEdges);

						brush.SetPolygons(newPolygons);

						for (int i = 0; i < newEdges.Count; i++) 
						{
							SelectEdges(brush, newPolygons, newEdges[i]);
						}
					}
				}
			}

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Split", EditorStyles.miniButton))
			{
				if(selectedEdges != null)
				{
					List<KeyValuePair<Vertex, Brush>> newSelectedVertices = new List<KeyValuePair<Vertex, Brush>>();
					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Split Edge");
						Undo.RecordObject(brush, "Split Edge");
						Polygon[] polygons = brush.GetPolygons();

						for (int j = 0; j < selectedEdges.Count; j++) 
						{
							// First check if this edge actually belongs to the brush
							Brush parentBrush = selectedVertices[selectedEdges[j].Vertex1];

							if(parentBrush == brush)
							{
								for (int i = 0; i < polygons.Length; i++) 
								{
									Vertex newVertex;
									if(EdgeUtility.SplitPolygonAtEdge(polygons[i], selectedEdges[j], out newVertex))
									{
										newSelectedVertices.Add(new KeyValuePair<Vertex, Brush>(newVertex, brush));
									}
								}
							}
						}

						brush.Invalidate(true);
					}

					ClearSelection();

					for (int i = 0; i < newSelectedVertices.Count; i++) 
					{
						Brush brush = newSelectedVertices[i].Value;
						Vertex vertex = newSelectedVertices[i].Key;

						SelectVertices(brush, brush.GetPolygons(), new List<Vertex>() { vertex } );
					}
				}
			}
		
			GUILayout.EndHorizontal();
		}

		List<Vertex> SelectedVerticesOfBrush(Brush brush)
		{
			List<Vertex> refinedSelection = new List<Vertex>();

			foreach (KeyValuePair<Vertex, Brush> selectedVertexPair in selectedVertices) 
			{
				if(selectedVertexPair.Value == brush)
				{
					refinedSelection.Add(selectedVertexPair.Key);
				}
			}
			return refinedSelection;
		}

		public void OnRepaint (SceneView sceneView, Event e)
		{
			if(isMarqueeSelection && sceneView == SceneView.lastActiveSceneView)
			{
				SabreGraphics.DrawMarquee(marqueeStart, marqueeEnd);
			}

			if(primaryTargetBrush != null)
			{
				DrawVertices(sceneView, e);
			}

			// Draw UI specific to this editor
			Rect rectangle = new Rect(0, 50, 140, 160);
			GUIStyle toolbar = new GUIStyle(EditorStyles.toolbar);
			toolbar.normal.background = SabreCSGResources.ClearTexture;
			toolbar.fixedHeight = rectangle.height;
			GUILayout.Window(140002, rectangle, OnToolbarGUI, "", toolbar);
		}

		void OnMouseDown (SceneView sceneView, Event e)
		{
			isMarqueeSelection = false;
			moveInProgress = false;

			marqueeStart = e.mousePosition;

            if (EditorHelper.IsMousePositionInInvalidRects(e.mousePosition))
            {
                marqueeCancelled = true;
            }
            else
            {
                marqueeCancelled = false;
            }
        }

		void OnMouseDrag (SceneView sceneView, Event e)
		{
			if(!CameraPanInProgress)
			{
				if(!moveInProgress && e.button == 0)
				{
                    if (!marqueeCancelled)
                    {
                        marqueeEnd = e.mousePosition;
                        isMarqueeSelection = true;
                        sceneView.Repaint();
                    }
				}
			}
		}

		// Select any vertices
		void OnMouseUp (SceneView sceneView, Event e)
		{
			if(e.button == 0 && !CameraPanInProgress)
			{
				Transform sceneViewTransform = sceneView.camera.transform;
				FixVector3 sceneViewPosition = (FixVector3)sceneViewTransform.position;
				if(moveInProgress)
				{

				}
				else
				{
					if(isMarqueeSelection) // Marquee vertex selection
					{
						selectedEdges.Clear();

						isMarqueeSelection = false;
						
						marqueeEnd = e.mousePosition;

						foreach(PrimitiveBrush brush in targetBrushes)
						{
							Polygon[] polygons = brush.GetPolygons();

							for (int i = 0; i < polygons.Length; i++) 
							{
								Polygon polygon = polygons[i];
								
								for (int j = 0; j < polygon.Vertices.Length; j++) 
								{
									Vertex vertex = polygon.Vertices[j];
									
									FixVector3 worldPosition = (FixVector3)brush.transform.TransformPoint((Vector3)vertex.Position);
									FixVector3 screenPoint = (FixVector3)sceneView.camera.WorldToScreenPoint((Vector3)worldPosition);
									
									// Point is contained within marquee box
									if(SabreMouse.MarqueeContainsPoint(marqueeStart, marqueeEnd, (Vector3)screenPoint))
									{
										if(EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control))
										{
											// Only when holding control should a deselection occur from a valid point
											selectedVertices.Remove(vertex);
										}
										else
										{
											// Point was in marquee (and ctrl wasn't held) so select it!
											if(!selectedVertices.ContainsKey(vertex))
											{
												selectedVertices.Add(vertex, brush);
											}
										}
									}
									else if(!EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control) 
									        && !EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Shift))
									{
										selectedVertices.Remove(vertex);
									}
								}
							}
						}
                        SceneView.RepaintAll();
					}
					else if (!EditorHelper.IsMousePositionInInvalidRects(e.mousePosition) && !marqueeCancelled) // Clicking style vertex selection
                    {
						Vector2 mousePosition = e.mousePosition;

						bool clickedAnyPoints = false;
//						Vertex closestVertexFound = null;
						FixVector3 closestVertexWorldPosition = FixVector3.zero;
						Fix64 closestDistanceSquare = Fix64.MaxValue; // positive infinity.

						foreach (PrimitiveBrush brush in targetBrushes) 
						{
							Polygon[] polygons = brush.GetPolygons();
							for (int i = 0; i < polygons.Length; i++) 
							{
								Polygon polygon = polygons[i];

								for (int j = 0; j < polygon.Vertices.Length; j++) 
								{
									Vertex vertex = polygon.Vertices[j];

									FixVector3 worldPosition = (FixVector3)brush.transform.TransformPoint((Vector3)vertex.Position);

									Fix64 vertexDistanceSquare = (sceneViewPosition - worldPosition).sqrMagnitude;

									if(EditorHelper.InClickZone(mousePosition, worldPosition) && vertexDistanceSquare < closestDistanceSquare)
									{
//										closestVertexFound = vertex;
										closestVertexWorldPosition = worldPosition;
										clickedAnyPoints = true;
										closestDistanceSquare = vertexDistanceSquare;
									}
								}
							}
						}

						if(!EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control) && !EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Shift))
						{
							ClearSelection();
						}

						foreach (PrimitiveBrush brush in targetBrushes) 
						{
							Polygon[] polygons = brush.GetPolygons();
							for (int i = 0; i < polygons.Length; i++) 
							{
								Polygon polygon = polygons[i];
								
								for (int j = 0; j < polygon.Vertices.Length; j++) 
								{
									Vertex vertex = polygon.Vertices[j];
									FixVector3 worldPosition = (FixVector3)brush.transform.TransformPoint((Vector3)vertex.Position);
									if(clickedAnyPoints && worldPosition == closestVertexWorldPosition)
									{
										if(EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control))
										{
											if(!selectedVertices.ContainsKey(vertex))
											{
												selectedVertices.Add(vertex, brush);
											}
											else
											{
												selectedVertices.Remove(vertex);
											}
										}
										else
										{
											if(!selectedVertices.ContainsKey(vertex))
											{
												selectedVertices.Add(vertex, brush);
											}
										}
									}
									else if(!EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control) 
									        && !EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Shift))
									{
										selectedVertices.Remove(vertex);
									}
								}
							}
						}


						if(!clickedAnyPoints) // Couldn't click any directly, next try to click an edge
						{
							Edge selectedEdge = null;
							FixVector3 selectedEdgeWorldPosition1 = FixVector3.zero;
							FixVector3 selectedEdgeWorldPosition2 = FixVector3.zero;
							// Used to track the closest edge clicked, so if we could click through several edges with
							// one click, then we only count the closest
							Fix64 closestFound = Fix64.MaxValue; // positive infinity.

							foreach (PrimitiveBrush brush in targetBrushes) 
							{
								Polygon[] polygons = brush.GetPolygons();
								for (int i = 0; i < polygons.Length; i++) 
								{
									Polygon polygon = polygons[i];
									for (int j = 0; j < polygon.Vertices.Length; j++) 
									{
										FixVector3 worldPoint1 = (FixVector3)brush.transform.TransformPoint((Vector3)polygon.Vertices[j].Position);
										FixVector3 worldPoint2 = (FixVector3)brush.transform.TransformPoint((Vector3)polygon.Vertices[(j+1) % polygon.Vertices.Length].Position);

										// Distance from the mid point of the edge to the camera
										Fix64 squareDistance = (FixVector3.Lerp(worldPoint1,worldPoint2,(Fix64)0.5f) - (FixVector3)Camera.current.transform.position).sqrMagnitude;

										Fix64 screenDistance = (Fix64)HandleUtility.DistanceToLine((Vector3)worldPoint1, (Vector3)worldPoint2);
										if(screenDistance < EDGE_SCREEN_TOLERANCE && squareDistance < closestFound)
										{
											selectedEdgeWorldPosition1 = worldPoint1;
											selectedEdgeWorldPosition2 = worldPoint2;
											selectedEdge = new Edge(polygon.Vertices[j], polygon.Vertices[(j+1) % polygon.Vertices.Length]);

											closestFound = squareDistance;
										}
									}
								}
							}

							List<Vertex> newSelectedVertices = new List<Vertex>();

							if(selectedEdge != null)
							{
								newSelectedVertices.Add(selectedEdge.Vertex1);
								newSelectedVertices.Add(selectedEdge.Vertex2);

								selectedEdges.Add(selectedEdge);

								foreach (PrimitiveBrush brush in targetBrushes) 
								{
									Polygon[] polygons = brush.GetPolygons();

									for (int i = 0; i < polygons.Length; i++) 
									{
										Polygon polygon = polygons[i];

										for (int j = 0; j < polygon.Vertices.Length; j++) 
										{
											Vertex vertex = polygon.Vertices[j];

											FixVector3 worldPosition = (FixVector3)brush.transform.TransformPoint((Vector3)vertex.Position);
											if(worldPosition == selectedEdgeWorldPosition1
												|| worldPosition == selectedEdgeWorldPosition2)
											{
												if(!selectedVertices.ContainsKey(vertex))
												{
													selectedVertices.Add(vertex, brush);
												}
											}
										}
									}
								}
							}
						}
					}
					moveInProgress = false;

					
					// Repaint all scene views to show the selection change
					SceneView.RepaintAll();
				}

				if(selectedVertices.Count > 0)
				{
					e.Use();
				}
			}
		}

		void DrawVertices(SceneView sceneView, Event e)
		{
			Camera sceneViewCamera = sceneView.camera;

			SabreCSGResources.GetVertexMaterial().SetPass (0);
			GL.PushMatrix();
			GL.LoadPixelMatrix();

			GL.Begin(GL.QUADS);

			// Draw each handle, colouring it if it's selected
			foreach (PrimitiveBrush brush in targetBrushes) 
			{
				Polygon[] polygons = brush.GetPolygons();

				FixVector3 target;

				for (int i = 0; i < polygons.Length; i++) 
				{
					for (int j = 0; j < polygons[i].Vertices.Length; j++) 
					{
						Vertex vertex = polygons[i].Vertices[j];

						if(selectedVertices.ContainsKey(vertex))
						{
							GL.Color(new Color32(0, 255, 128, 255));
						}
						else
						{
							GL.Color(Color.white);
						}

						target = (FixVector3)sceneViewCamera.WorldToScreenPoint(brush.transform.TransformPoint((Vector3)vertex.Position));
						if(target.z > Fix64.Zero)
						{
							// Make it pixel perfect
							target = MathHelper.RoundFixVector3(target);
							SabreGraphics.DrawBillboardQuad((Vector3)target, 8, 8);
						}
					}
				}
			}

			GL.End();

			// Draw lines for selected edges
			SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);

			GL.Begin(GL.LINES);
			GL.Color(Color.green);

			for (int edgeIndex = 0; edgeIndex < selectedEdges.Count; edgeIndex++) 
			{
				Edge edge = selectedEdges[edgeIndex];

				if(selectedVertices.ContainsKey(edge.Vertex1))
				{
					Brush brush = selectedVertices[edge.Vertex1];

					FixVector3 target1 = (FixVector3)sceneViewCamera.WorldToScreenPoint(brush.transform.TransformPoint((Vector3)edge.Vertex1.Position));
					FixVector3 target2 = (FixVector3)sceneViewCamera.WorldToScreenPoint(brush.transform.TransformPoint((Vector3)edge.Vertex2.Position));

					if(target1.z > Fix64.Zero && target2.z > Fix64.Zero)
					{
						SabreGraphics.DrawScreenLine((Vector3)target1, (Vector3)target2);
					}
				}
			}

			GL.End();

			GL.PopMatrix();
		}

		public override void Deactivated ()
		{
			
		}
	}
}
#endif