using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// TODO: Overrides for width and height and similar basic layout properties, so that
//		 some level of customization is possible without creating a custom editor script.

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class NodeAttribute : Attribute
{
	public readonly Type GraphType;
	public readonly string NodeName;
	public readonly Vector2 ExpandedSizeOverride;

	public NodeAttribute(Type graphType, string nodeName = null, float minWidth = 0, float minHeight = 0)
	{
		GraphType = graphType;
		NodeName = nodeName;
		ExpandedSizeOverride = new Vector2(minWidth, minHeight);
	}
}
