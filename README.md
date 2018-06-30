# unity-node-editor

This is a work-in-progress data design-oriented node editor for Unity. It is designed to work seamlessly with existing Unity features - graphs and nodes are just ScriptableObjects.

In fact, any ScriptableObject can be turned into a node or used as a graph (container for nodes) by simply adding an attribute to its script. 

## What sets it apart?

There are a gazillion similar node editors available, both open source and for purchase on the Unity Asset Store. The reason I created this one is that I was unable to find
one that solved the problem that I was faced with - data design. Almost all node editors that can be found (for Unity) are more geared towards visual scripting; they
model processes instead of data objects. The most common implementation has each node represent some type of fundamental operation, such as addition or multiplication,
and connections between nodes represent passed parameters and return values. This is great if you want to create code visually, but not if you want to design data objects.

Moreover - many existing solutions are entire frameworks, requiring you to adopt a specific workflow and creating a heavy dependence on their code. A core design concept 
for this plugin is that it leave a minimal footprint on your codebase - you could remove the plugin and keep the graphs created with it; it would only require removing some
attributes here and there.

## What can it be used for?

In the context of game design, a common use case for something like this is creating dialogues. Multiple option response type dialogues form tree structures, and managing complex
such dialogues requires a way to visualize the data. With this plugin it is easy to design dialogues as data objects.

It could also be used to create things such as skill trees, crafting systems, and so on.

Visual scripting (such as behaviour trees and state machines) is of course possible too, but it requires that you implement the executive logic on your own.

## How to use it?

IT IS NOT YET READY FOR PRODUCTION USE. Some essential features are not implemented yet, and some things may still change which could cause anything created with the plugin
to become unusable after an update. With that in mind...

### Defining and creating a Graph

To create a graph, one must first define a ScriptableObject and tag it with the `[NodeGraph]` attribute. To get access to the nodes inside the graph, the graph must have
atleast one serialized field (either public or with the `[SerializeField]`) attribute, with the [ExposedNode] attribute.

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Example Node Graph")]
[NodeGraph]
public class ExampleNodeGraph : ScriptableObject
{
	[ExposedNode] public ExampleNode rootNode;
}
```

Note that the `[ExposeNode]` attribute is not supported for nested fields. Once the Graph has been defined, we can create an instance of it using the Create Asset
menu. Double clicking the asset in the project view will then open the Node Editor window with focus set to that graph.

### Defining and creating a Node

Similarly, any ScriptableObject can be turned into a node:

```csharp
using UnityEngine;

[Node(graphType: typeof(ExampleNodeGraph))]
public class ExampleNode : ScriptableObject
{

}
```

Here, we have to specify which type of graph this scriptable object should be a node in. It is possible to have multiple `[Node]` attributes - allowing for multiple graphs to utilize the same node.

To add a node to a Graph, ensure that you have selected the Graph (double clicked it, so that the editor window is locked to it), and then right click anywhere inside the window. The node should show up
in the context menu, under Create. This will create an instance of the node, and attach its asset as a child on the Graph asset.

### Designing graphs

Using the Node Editor to design graphs is fairly straight-forward. The nodes can be expanded to show the inspector of the underlying ScriptableObject. Clicking a node also targets it in the Inspector.

If the ScriptableObject contains a serialized member referring to a type which is a node in the same graph, a connector will appear next to that field when the node is in expanded mode. Clicking the 
connector will let you drag a line to another node in the graph; releasing on that node will set the target of the field to be that node - creating a connection. Connections can also be made the regular
Unity way, by clicking the field and selecting the target in the list. 

Right clicking a node will also allow you to set it in one of the fields on the graph tagged with the `[ExposedNode]` attribute with matching type. This way, you can specify a root node or similar. Note 
that it is possible to have multiple exposed nodes.

That's the basics.

### Current Drawbacks

Drawing the inspectors on nodes is an expensive operation and will slow down the editor window when there are many fields to be drawn, so it is best to keep nodes closed if one cares about smooth performance.
EditorGUILayout.PropertyField() seems to be the main problem (which must be used to support custom property drawers). It will soon be possible to create custom inspectors for your nodes, where you can use explicit
type fields (like EditorGUILayout.IntField()), which should mitigate this.