using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DataDesigner
{
	public class OutputAttribute : Attribute
	{
		public readonly Color? color;

		public OutputAttribute(string color = null)
		{
			if (color != null)
			{
				Color outColor;
				if (ColorUtility.TryParseHtmlString(color, out outColor))
					this.color = outColor;
			}
		}
	}
}