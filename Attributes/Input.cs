using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DataDesigner
{
	public class Input : Attribute
	{
		public readonly Type type;
		public readonly Color? color;

		public Input(Type type = null, string color = null)
		{
			if (type != null)
				this.type = type;
			else
				this.type = typeof(object);

			if (color != null)
			{
				Color outColor;
				if (ColorUtility.TryParseHtmlString(color, out outColor))
					this.color = outColor;
			}
		}
	}
}