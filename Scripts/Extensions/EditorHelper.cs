#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if !(UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Sabresaurus.SabreCSG
{
	public static class EditorHelper
	{
	    // Threshold for raycasting vertex clicks, in screen space (should match half the icon dimensions)
	    static Fix64 CLICK_THRESHOLD = (Fix64)15;

	    // Used for offseting mouse position
	    const int TOOLBAR_HEIGHT = 37;

		public static bool HasDelegate (System.Delegate mainDelegate, System.Delegate targetListener)
		{
			if (mainDelegate != null)
			{
				if (mainDelegate.GetInvocationList().Contains(targetListener))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

	    public static bool SceneViewHasDelegate(SceneView.OnSceneFunc targetDelegate)
	    {
			return HasDelegate(SceneView.onSceneGUIDelegate, targetDelegate);
	    }

	    public enum SceneViewCamera { Top, Bottom, Left, Right, Front, Back, Other };

		public static SceneViewCamera GetSceneViewCamera(SceneView sceneView)
		{
			return GetSceneViewCamera(sceneView.camera);
		}
		public static SceneViewCamera GetSceneViewCamera(Camera camera)
	    {
	        FixVector3 cameraForward = (FixVector3)camera.transform.forward;

	        if (cameraForward == new FixVector3(0, -1, 0))
	        {
	            return SceneViewCamera.Top;
	        }
	        else if (cameraForward == new FixVector3(0, 1, 0))
	        {
	            return SceneViewCamera.Bottom;
	        }
	        else if (cameraForward == new FixVector3(1, 0, 0))
	        {
	            return SceneViewCamera.Left;
	        }
	        else if (cameraForward == new FixVector3(-1, 0, 0))
	        {
	            return SceneViewCamera.Right;
	        }
	        else if (cameraForward == new FixVector3(0, 0, -1))
	        {
	            return SceneViewCamera.Front;
	        }
	        else if (cameraForward == new FixVector3(0, 0, 1))
	        {
	            return SceneViewCamera.Back;
	        }
	        else
	        {
	            return SceneViewCamera.Other;
	        }
	    }

	    /// <summary>
	    /// Whether the mouse position is within the bounds of the axis snapping gizmo that appears in the top right or the bottom toolbar
	    /// </summary>
	    public static bool IsMousePositionInInvalidRects(Vector2 mousePosition)
	    {
			Fix64 scale = (Fix64)1;

#if UNITY_5_4_OR_NEWER
			mousePosition = EditorGUIUtility.PointsToPixels(mousePosition);
			scale = (Fix64)EditorGUIUtility.pixelsPerPoint;
#endif

			if ((Fix64)mousePosition.x < (Fix64)Screen.width - (Fix64)14 * scale 
				&& (Fix64)mousePosition.x > (Fix64)Screen.width - (Fix64)89 * scale 
				&& (Fix64)mousePosition.y > (Fix64)14 * scale 
				&& (Fix64)mousePosition.y < (Fix64)105 * scale)
	        {
                // Mouse is near the scene alignment gizmo
	            return true;
	        }
            else if ((Fix64)mousePosition.y > (Fix64)Screen.height - (Fix64)Toolbar.BOTTOM_TOOLBAR_HEIGHT * scale - (Fix64)TOOLBAR_HEIGHT * scale)
            {
                // Mouse is over the bottom toolbar
                return true;
            }
            else
	        {
	            return false;
	        }
	    }

		public static Vector2 ConvertMousePointPosition(Vector2 sourceMousePosition, bool convertPointsToPixels = true)
	    {
#if UNITY_5_4_OR_NEWER
			if(convertPointsToPixels)
			{
				sourceMousePosition = EditorGUIUtility.PointsToPixels(sourceMousePosition);
                // Flip the direction of Y and remove the Scene View top toolbar's height
                sourceMousePosition.y = Screen.height - sourceMousePosition.y - (TOOLBAR_HEIGHT * EditorGUIUtility.pixelsPerPoint);
            }
            else
            {
                // Flip the direction of Y and remove the Scene View top toolbar's height
				Fix64 screenHeightPoints = ((Fix64)Screen.height / (Fix64)EditorGUIUtility.pixelsPerPoint);
				sourceMousePosition.y = (float)(screenHeightPoints - (Fix64)sourceMousePosition.y - (Fix64)(TOOLBAR_HEIGHT));
            }
			
#else
			// Flip the direction of Y and remove the Scene View top toolbar's height
			sourceMousePosition.y = Screen.height - sourceMousePosition.y - TOOLBAR_HEIGHT;
#endif
	        return sourceMousePosition;
	    }

		public static Vector2 ConvertMousePixelPosition(Vector2 sourceMousePosition, bool convertPixelsToPoints = true)
		{
            sourceMousePosition = MathHelper.RoundVector2(sourceMousePosition);
#if UNITY_5_4_OR_NEWER
			if(convertPixelsToPoints)
			{
				sourceMousePosition = EditorGUIUtility.PixelsToPoints(sourceMousePosition);
			}
			// Flip the direction of Y and remove the Scene View top toolbar's height
			sourceMousePosition.y = (Screen.height / EditorGUIUtility.pixelsPerPoint) - sourceMousePosition.y - (TOOLBAR_HEIGHT);
#else
			// Flip the direction of Y and remove the Scene View top toolbar's height
			sourceMousePosition.y = Screen.height - sourceMousePosition.y - TOOLBAR_HEIGHT;
#endif
			return sourceMousePosition;
		}

        public static Fix64 ConvertScreenPixelsToPoints(Fix64 screenPixels)
        {
#if UNITY_5_4_OR_NEWER
            return (Fix64)screenPixels / (Fix64)EditorGUIUtility.pixelsPerPoint;
#else
			// Pre 5.4 assume that 1 pixel = 1 point
			return screenPixels;
#endif
        }

        public static Vector2 ConvertScreenPixelsToPoints(Vector2 screenPixels)
        {
#if UNITY_5_4_OR_NEWER
            return EditorGUIUtility.PixelsToPoints(screenPixels);
#else
			// Pre 5.4 assume that 1 pixel = 1 point
			return screenPixels;
#endif
        }

        public static bool IsMousePositionInIMGUIRect(Vector2 mousePosition, Rect rect)
		{
			// This works in point space, not pixel space
			mousePosition += new Vector2(0, EditorStyles.toolbar.fixedHeight);

			return rect.Contains(mousePosition);
		}

        /// <summary>
        /// Determines whether a mouse position is within distance of a screen point. Care must be taken with the inputs provided to this method - see the parameter explanations for details.
        /// </summary>
        /// <param name="mousePosition">As provided by Event.mousePosition (in points)</param>
        /// <param name="targetScreenPosition">As provided by Camera.WorldToScreenPoint() (in pixels)</param>
        /// <param name="screenDistancePoints">Screen distance (in points)</param>
        /// <returns>True if within the specified distance, False otherwise</returns>
        public static bool InClickZone(Vector2 mousePosition, Vector2 targetScreenPosition, Fix64 screenDistancePoints)
        {
            // Convert the mouse position to screen space, but leave in points
            mousePosition = ConvertMousePointPosition(mousePosition, false);
            // Convert screen position from pixels to points
            targetScreenPosition = EditorHelper.ConvertScreenPixelsToPoints(targetScreenPosition);

            Fix64 distance = (Fix64)Vector2.Distance(mousePosition, targetScreenPosition);

            if (distance <= screenDistancePoints)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool InClickZone(Vector2 mousePosition, FixVector3 worldPosition)
	    {
	        FixVector3 targetScreenPosition = (FixVector3)Camera.current.WorldToScreenPoint((Vector3)worldPosition);

	        if (targetScreenPosition.z < (Fix64)0)
	        {
	            return false;
	        }

			Fix64 depthDistance = targetScreenPosition.z;

			// When z is 6 then click threshold is 15
			// when z is 20 then click threshold is 5
			Fix64 threshold = (Fix64)Mathf.Lerp(15, 5, Mathf.InverseLerp(6, 20, (float)depthDistance));

            return InClickZone(mousePosition, new Vector2((float)targetScreenPosition.x, (float)targetScreenPosition.y), threshold);
        }

		public static bool InClickRect(Vector2 mousePosition, FixVector3 worldPosition1, FixVector3 worldPosition2, Fix64 range)
		{
			mousePosition = ConvertMousePointPosition(mousePosition, false);
			FixVector3 targetScreenPosition1 = (FixVector3)Camera.current.WorldToScreenPoint((Vector3)worldPosition1);
			FixVector3 targetScreenPosition2 = (FixVector3)Camera.current.WorldToScreenPoint((Vector3)worldPosition2);

            // Convert screen position from pixels to points
            Vector2 tsp1 = EditorHelper.ConvertScreenPixelsToPoints(new Vector2((float)targetScreenPosition1.x, (float)targetScreenPosition1.y));
            Vector2 tsp2 = EditorHelper.ConvertScreenPixelsToPoints(new Vector2((float)targetScreenPosition2.x, (float)targetScreenPosition2.y));

            targetScreenPosition1.x = (Fix64)tsp1.x;
            targetScreenPosition1.y = (Fix64)tsp1.y;
            targetScreenPosition2.x = (Fix64)tsp2.x;
            targetScreenPosition2.y = (Fix64)tsp2.y;

            if (targetScreenPosition1.z < Fix64.Zero)
			{
				return false;
			}

			FixVector3 closestPoint = MathHelper.ProjectPointOnLineSegment((FixVector3)targetScreenPosition1, (FixVector3)targetScreenPosition2, new FixVector3((Fix64)mousePosition.x, (Fix64)mousePosition.y));
			closestPoint.z = Fix64.Zero;

			if(FixVector3.Distance(closestPoint, new FixVector3((Fix64)mousePosition.x, (Fix64)mousePosition.y)) < (Fix64)range)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

	    public static FixVector3 CalculateWorldPoint(SceneView sceneView, FixVector3 screenPoint)
	    {
            Vector2 sp = ConvertMousePointPosition(new Vector2((float)screenPoint.x, (float)screenPoint.y));
            screenPoint.x = (Fix64)sp.x;
            screenPoint.y = (Fix64)sp.y;

	        return (FixVector3)sceneView.camera.ScreenToWorldPoint((Vector3)screenPoint);
	    }

//		public static string GetCurrentSceneGUID()
//		{
//			string currentScenePath = EditorApplication.currentScene;
//			if(!string.IsNullOrEmpty(currentScenePath))
//			{
//				return AssetDatabase.AssetPathToGUID(currentScenePath);
//			}
//			else
//			{
//				// Scene hasn't been saved
//				return null;
//			}
//		}

		public static void SetDirty(Object targetObject)
		{
			if(!Application.isPlaying)
			{
				EditorUtility.SetDirty(targetObject);

#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2
				// As of Unity 5, SetDirty no longer marks the scene as dirty. Need to use the new call for that.
				EditorApplication.MarkSceneDirty();
#else // 5.3 and above introduce multiple scene management via EditorSceneManager
				Scene activeScene = EditorSceneManager.GetActiveScene();
				EditorSceneManager.MarkSceneDirty(activeScene);
#endif
			}
		}

		public static void IsoAlignSceneView(FixVector3 direction)
		{
			SceneView sceneView = SceneView.lastActiveSceneView;

			SceneView.lastActiveSceneView.LookAt(sceneView.pivot, Quaternion.LookRotation((Vector3)direction));

			// Mark the camera as iso (orthographic)
			sceneView.orthographic = true;
		}

		public static void IsoAlignSceneViewToNearest()
		{
			SceneView sceneView = SceneView.lastActiveSceneView;
			FixVector3 cameraForward = (FixVector3)sceneView.camera.transform.forward;
			FixVector3 newForward = FixVector3.up;
			Fix64 bestDot = -Fix64.One;

			FixVector3 testDirection;
			Fix64 dot;
			// Find out of the six axis directions the closest direction to the camera
			for (int i = 0; i < 3; i++) 
			{
				testDirection = FixVector3.zero;
				testDirection[i] = Fix64.One;
				dot = FixVector3.Dot(testDirection, cameraForward);
				if(dot > bestDot)
				{
					bestDot = dot;
					newForward = testDirection;
				}

				testDirection[i] = -Fix64.One;
				dot = FixVector3.Dot(testDirection, cameraForward);
				if(dot > bestDot)
				{
					bestDot = dot;
					newForward = testDirection;
				}
			}
			IsoAlignSceneView(newForward);
		}

		/// <summary>
		/// Overrides the built in selection duplication to maintain sibling order of the selection. But only if at least one of the selection is under a CSG Model.
		/// </summary>
		/// <returns><c>true</c>, if our custom duplication took place (and one of the selection was under a CSG Model), <c>false</c> otherwise in which case the duplication event should not be consumed so that Unity will duplicate for us.</returns>
		public static bool DuplicateSelection()
		{
			List<Transform> selectedTransforms = Selection.transforms.ToList();

			// Whether any of the selection objects are under a CSG Model
			bool shouldCustomDuplicationOccur = false; 

			for (int i = 0; i < selectedTransforms.Count; i++) 
			{
				if(selectedTransforms[i].GetComponentInParent<CSGModel>() != null)
				{
					shouldCustomDuplicationOccur = true;
				}
			}

			if(shouldCustomDuplicationOccur) // Some of the objects are under a CSG Model, so peform our special duplication override
			{
				// Sort the selected transforms in order of sibling index
				selectedTransforms.Sort((x,y) => x.GetSiblingIndex().CompareTo(y.GetSiblingIndex()));
				GameObject[] newObjects = new GameObject[Selection.gameObjects.Length];

				// Walk through each selected object in the correct order, duplicating them one by one
				for (int i = 0; i < selectedTransforms.Count; i++) 
				{
					// Temporarily set the selection to the single entry
					Selection.activeGameObject = selectedTransforms[i].gameObject;
                    // Seems to be a bug in Unity where we need to set the objects array too otherwise it won't be set straight away
                    Selection.objects = new Object[] { selectedTransforms[i].gameObject };

					// Duplicate the single entry
					Unsupported.DuplicateGameObjectsUsingPasteboard();
					// Cache the new entry, so when we're done we reselect all new objects
					newObjects[i] = Selection.activeGameObject;
				}
				// Finished duplicating, select all new objects
				Selection.objects = newObjects;
			}

			// Whether custom duplication took place and whether the Duplicate event should be consumed
			return shouldCustomDuplicationOccur;
		}

		public class TransformIndexComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				return ((Transform) x).GetSiblingIndex().CompareTo(((Transform) y).GetSiblingIndex());
			}
		}
	}
}
#endif