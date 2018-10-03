using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DataDesigner
{
	public interface IView
	{
		Rect GetWindowRect();
		IEnumerable<IView> SubViews { get; }
	}
}