using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Container for information usually held by a Transform
	/// </summary>
	[Serializable]
	public class TransformData
	{
		public FixVector3 LocalPosition = FixVector3.zero;
		public Quaternion LocalRotation = Quaternion.identity;
		public FixVector3 LocalScale = FixVector3.one;
		public Transform Parent;
		public int SiblingIndex;

		public TransformData (Transform sourceTransform)
		{
			this.LocalPosition = (FixVector3)sourceTransform.localPosition;
			this.LocalRotation = sourceTransform.localRotation;
			this.LocalScale = (FixVector3)sourceTransform.localScale;
			this.Parent = sourceTransform.parent;
			this.SiblingIndex = sourceTransform.GetSiblingIndex();
		}

		public bool SetFromTransform(Transform sourceTransform, bool ignoreSiblingChange = false)
		{
			bool changed = false;

			if(this.LocalPosition != (FixVector3)sourceTransform.localPosition)
			{
				this.LocalPosition = (FixVector3)sourceTransform.localPosition;
				changed = true;
			}

			if(this.LocalRotation != sourceTransform.localRotation)
			{
				this.LocalRotation = sourceTransform.localRotation;
				changed = true;
			}

			if(this.LocalScale != (FixVector3)sourceTransform.localScale)
			{
				this.LocalScale = (FixVector3)sourceTransform.localScale;
				changed = true;
			}

			if(this.Parent != sourceTransform.parent)
			{
				this.Parent = sourceTransform.parent;
				changed = true;
			}

			if(this.SiblingIndex != sourceTransform.GetSiblingIndex() 
				&& !ignoreSiblingChange)
			{
				this.SiblingIndex = sourceTransform.GetSiblingIndex();
				changed = true;
			}

			return changed;
		}
	}
}