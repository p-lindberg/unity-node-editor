using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DataDesigner
{
	[CreateAssetMenu(menuName = "Node Editor/Editor Settings")]
	public class NodeEditorSettings : ScriptableObject
	{
		[SerializeField] Texture2D windowBackground = null;

		public Texture2D WindowBackground { get { return windowBackground; } }

		[SerializeField] NodeViewSettings defaultNodeViewSettings = null;

		public NodeViewSettings DefaultNodeViewSettings { get { return defaultNodeViewSettings; } }

		[SerializeField] GUIStyle graphHeaderStyle = null;

		public GUIStyle GraphHeaderStyle { get { return graphHeaderStyle; } }
	}
}