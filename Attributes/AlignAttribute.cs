using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DataDesigner
{
	public enum Alignment
	{
		Auto,
		Left,
		Right
	}

	public class AlignAttribute : Attribute
	{
		public readonly Alignment alignment;

		public AlignAttribute(Alignment alignment)
		{
			this.alignment = alignment;
		}
	}
}