using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Linq;

namespace DataDesigner
{
	public class MyAssetHandler
	{
		[OnOpenAssetAttribute(1)]
		public static bool step1(int instanceID, int line)
		{
			var asset = EditorUtility.InstanceIDToObject(instanceID);
			if (asset.GetType().GetCustomAttributes(typeof(GraphAttribute), true).Any())
			{
				NodeEditor.OpenWindow(asset);
				return true;
			}

			return false;
		}
	}
}