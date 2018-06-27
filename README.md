# unity-node-editor
A node editor for unity!

### To-do:
- Change NodeGraph class into an attribute, so that any scriptable object can be made into a graph by simply adding the attribute. 
- Create an attribute for marking root nodes, so that graph object can have a reference to one or more root nodes. Make it possible to set these via right clicking a node.
- Create GUI for when a node is a root node, and when a node is selected / moused over (especially when creating a connection - hovered node should light up if connection is possible).
- Implement click + drag selection box for moving / deleting multiple nodes at the same time.
- Implement node groups (box around nodes) that can be dragged around, copied and pasted, saved as template.
- Make connector lines terminate slightly outside target node (possibly as an arrowhead or similar); this should make it easier to see that a line connects to a node, as opposed to going through it.
- ... come up with more features!