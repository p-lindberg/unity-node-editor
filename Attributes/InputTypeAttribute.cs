using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DataDesigner
{
	public class InputTypeAttribute : Attribute
	{
		public readonly Type type;
		public readonly Color? color;

		public InputTypeAttribute(Type type = null, string color = null)
		{
			this.type = type;

			if (color != null)
			{
				Color outColor;
				if (ColorUtility.TryParseHtmlString(color, out outColor))
					this.color = outColor;
			}
		}
	}
}