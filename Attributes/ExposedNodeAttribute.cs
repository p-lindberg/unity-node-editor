using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field)]
public class ExposedNodeAttribute : Attribute
{
	public readonly string Name;

	public ExposedNodeAttribute(string name = null)
	{
		Name = name;
	}
}