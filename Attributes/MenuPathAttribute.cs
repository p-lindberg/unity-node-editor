using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DataDesigner
{
	public class MenuPathAttribute : Attribute
	{
		public readonly string path;

		public MenuPathAttribute(string path)
		{
			this.path = path;
		}
	}
}