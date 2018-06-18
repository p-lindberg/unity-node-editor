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
	public readonly string MenuName;

	public NodeAttribute(Type graphType, string menuName = null)
	{
		this.GraphType = graphType;
		this.MenuName = menuName;
	}
}
