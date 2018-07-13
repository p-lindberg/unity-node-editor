using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DataDesigner
{
	public class GraphAttribute : Attribute
	{
		public readonly bool ShowInDiagram;

		public GraphAttribute(bool showInGraph)
		{
			this.ShowInDiagram = showInGraph;
		}
	}
}