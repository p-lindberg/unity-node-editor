using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DataDesigner
{
	public class NodeAttribute : Attribute
	{
		public readonly Type[] Graphs;

		public NodeAttribute(params Type[] graphs)
		{
			Graphs = graphs;
		}
	}
}