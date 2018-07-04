using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Node Editor/Editor Settings")]
public class NodeEditorSettings : ScriptableObject
{
	[SerializeField] Texture2D windowBackground;

	public Texture2D WindowBackground { get { return windowBackground; } }

	[SerializeField] NodeViewSettings defaultNodeViewSettings;

	public NodeViewSettings DefaultNodeViewSettings { get { return defaultNodeViewSettings; } }

	[SerializeField] GUIStyle graphHeaderStyle;

	public GUIStyle GraphHeaderStyle { get { return graphHeaderStyle; } }
}