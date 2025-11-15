# Inheritance and Recursion in BrainSim III

## Overview

BrainSim III implements a sophisticated inheritance system that mirrors object-oriented programming concepts but operates entirely through graph structure. The system supports multiple inheritance, attribute bubbling, exception handling, and dynamic class creation—all without traditional class definitions.

## The IS-A Relationship: Foundation of Inheritance

### Concept

The `is-a` relationship is the primary mechanism for building taxonomies and enabling inheritance. It creates a directed acyclic graph (DAG) representing class hierarchies.

**File**: [Thing.cs:36](../UKS/Thing.cs#L36)

```csharp
public static Thing IsA { get => ThingLabels.GetThing("is-a"); }
```

### Hierarchy Example

```
Thing (root)
├── Object
│   ├── Animal
│   │   ├── Mammal
│   │   │   ├── Dog
│   │   │   │   ├── Fido (instance)
│   │   │   │   └── Rover (instance)
│   │   │   └── Cat
│   │   └── Bird
│   │       ├── Sparrow
│   │       └── Penguin
│   └── PhysicalObject
└── Action
    ├── Move
    │   ├── Walk
    │   └── Fly
    └── Communicate
        ├── Bark
        └── Speak
```

## Direct Parent/Child Relationships

### Implementation

[Thing.cs:154-159](../UKS/Thing.cs#L154-L159):

```csharp
// Direct ancestors
public IList<Thing> Parents { get => RelationshipsOfType(IsA, false); }

// Direct descendants
public IList<Thing> Children { get => RelationshipsOfType(IsA, true); }

private IList<Thing> RelationshipsOfType(Thing relType, bool useRelationshipFrom)
{
    IList<Thing> retVal = new List<Thing>();
    if (!useRelationshipFrom)
    {
        // Get targets of outgoing "is-a" relationships
        foreach (Relationship r in relationships)
            if (r.relType == relType && r.source == this)
                retVal.Add(r.target);
    }
    else
    {
        // Get sources of incoming "is-a" relationships
        foreach (Relationship r in relationshipsFrom)
            if (r.relType == relType && r.target == this)
                retVal.Add(r.source);
    }
    return retVal;
}
```

### Usage

```csharp
Thing dog = uks.Labeled("Dog");
IList<Thing> parents = dog.Parents;  // Returns: [Mammal]
IList<Thing> children = dog.Children; // Returns: [Fido, Rover, ...]
```

## Recursive Ancestor/Descendent Traversal

### Transitive Relationship Following

**File**: [Thing.cs:276-302](../UKS/Thing.cs#L276-L302)

```csharp
private IList<Thing> FollowTransitiveRelationships(
    Thing relType,
    bool followUpwards = true,
    Thing searchTarget = null)
{
    List<Thing> retVal = new();
    retVal.Add(this);
    if (this == searchTarget) return retVal;

    // Breadth-first traversal
    for (int i = 0; i < retVal.Count; i++)
    {
        Thing t = retVal[i];
        IList<Relationship> relationshipsToFollow =
            followUpwards ? t.Relationships : t.RelationshipsFrom;

        foreach (Relationship r in relationshipsToFollow)
        {
            Thing thingToAdd = followUpwards ? r.target : r.source;
            if (r.reltype == relType)
            {
                if (!retVal.Contains(thingToAdd))
                    retVal.Add(thingToAdd);
            }
            if (searchTarget == thingToAdd)
                return retVal;
        }
    }
    if (searchTarget != null) retVal.Clear();
    return retVal;
}
```

### Ancestor Access

[Thing.cs:199-218](../UKS/Thing.cs#L199-L218):

```csharp
public IList<Thing> AncestorList()
{
    return FollowTransitiveRelationships(IsA, true);
}

public IEnumerable<Thing> Ancestors
{
    get
    {
        IList<Thing> ancestors = AncestorList();
        for (int i = 0; i < ancestors.Count; i++)
        {
            Thing child = ancestors[i];
            yield return child;
        }
    }
}

public bool HasAncestor(Thing t)
{
    var x = FollowTransitiveRelationships(IsA, true, t);
    return x.Count != 0;
}
```

### Example Usage

```csharp
Thing fido = uks.Labeled("Fido");

// Get all ancestors
IList<Thing> ancestors = fido.AncestorList();
// Returns: [Fido, Dog, Mammal, Animal, Object, Thing]

// Check specific ancestor
bool isDog = fido.HasAncestor("Dog");        // true
bool isBird = fido.HasAncestor("Bird");      // false

// Iterate through ancestors
foreach (Thing ancestor in fido.Ancestors)
{
    Console.WriteLine(ancestor.Label);
}
```

## Attribute Inheritance

### Concept

Things inherit attributes (relationships) from their ancestors. When querying for a property not directly attached to a Thing, the system traverses upward through the `is-a` hierarchy.

### Query with Inheritance

**File**: [UKS.cs:158-167](../UKS/UKS.cs#L158-L167)

```csharp
List<Relationship> RelationshipTree(Thing t, Thing relType)
{
    List<Relationship> results = new();

    // Direct relationships
    results.AddRange(t.Relationships.FindAll(x => x.reltype == relType));

    // Inherited from ancestors
    foreach (Thing t1 in t.Ancestors)
        results.AddRange(t1.Relationships.FindAll(x => x.reltype == relType));

    // Relationships from descendants (for reverse queries)
    foreach (Thing t1 in t.Descendents)
        results.AddRange(t1.Relationships.FindAll(x => x.reltype == relType));

    return results;
}
```

### Example: Inherited Properties

```
Structure:
[Animal] --[has]--> [Leg]
[Dog] --[is-a]--> [Animal]
[Dog] --[can]--> [Bark]
[Fido] --[is-a]--> [Dog]
[Fido] --[has.color]--> [Brown]

Query: "What does Fido have?"
Results:
1. [Fido] --[has.color]--> [Brown]      (direct)
2. [Fido] --[can]--> [Bark]             (inherited from Dog)
3. [Fido] --[has]--> [Leg]              (inherited from Animal)
```

## Multiple Inheritance

### Concept

BrainSim III fully supports multiple inheritance—a Thing can have multiple parents, inheriting attributes from all of them.

### Implementation

[Thing.cs:506-514](../UKS/Thing.cs#L506-L514):

```csharp
public void AddParent(Thing newParent)
{
    if (newParent == null) return;
    if (!Parents.Contains(newParent))
    {
        AddRelationship(newParent, IsA);
    }
}
```

### Example: Diamond Problem Handling

```
        [Vehicle]
           / \
          /   \
    [LandVehicle] [WaterVehicle]
          \   /
           \ /
      [AmphibiousCar]
```

**Graph Structure**:
```csharp
Thing vehicle = uks.GetOrAddThing("Vehicle", "Object");
Thing landVehicle = uks.GetOrAddThing("LandVehicle", vehicle);
Thing waterVehicle = uks.GetOrAddThing("WaterVehicle", vehicle);
Thing amphibiousCar = uks.GetOrAddThing("AmphibiousCar", landVehicle);
amphibiousCar.AddParent(waterVehicle);  // Multiple inheritance

// AmphibiousCar.Parents → [LandVehicle, WaterVehicle]
// AmphibiousCar.AncestorList() → [AmphibiousCar, LandVehicle, WaterVehicle, Vehicle, Object, Thing]
```

**Conflict Resolution**: When both parents have the same relationship type, the system uses:
1. **Weight comparison**: Higher weight wins
2. **Exclusivity detection**: Check if targets are mutually exclusive
3. **Specificity**: Direct attributes override inherited

## Attribute Bubbling: Bottom-Up Generalization

### Concept

**Attribute bubbling** is the process of moving common attributes from children up to their parent, creating generalizations. This is a key learning mechanism in BrainSim III.

**Module**: [ModuleAttributeBubble.cs](../BrainSimulator/Modules/Agents/ModuleAttributeBubble.cs)

### Algorithm

**File**: [ModuleAttributeBubble.cs:82-201](../BrainSimulator/Modules/Agents/ModuleAttributeBubble.cs#L82-L201)

```csharp
void BubbleChildAttributes(Thing t)
{
    if (t.Children.Count == 0) return;
    if (t.Label == "unknownObject") return;

    // 1. Build a list of all relationships that children have
    List<RelDest> itemCounts = new();
    foreach (Thing t1 in t.ChildrenWithSubclasses)
    {
        foreach (Relationship r in t1.Relationships)
        {
            if (r.reltype == Thing.IsA) continue;
            Thing useRelType = GetInstanceType(r.reltype);

            RelDest foundItem = itemCounts.FindFirst(
                x => x.relType == useRelType && x.target == r.target);
            if (foundItem == null)
            {
                foundItem = new RelDest { relType = useRelType, target = r.target };
                itemCounts.Add(foundItem);
            }
            foundItem.relationships.Add(r);
        }
    }

    // 2. Sort by frequency
    var sortedItems = itemCounts.OrderByDescending(x => x.relationships.Count);

    // 3. Bubble relationships that appear in > 50% of children
    foreach (RelDest rr in sortedItems)
    {
        float totalCount = t.Children.Count;
        float positiveCount = rr.relationships.FindAll(x => x.Weight > .5f).Count;

        // Count conflicting relationships
        float negativeCount = 0;
        foreach (var other in sortedItems)
        {
            if (RelationshipsConflict(rr, other))
                negativeCount += other.relationships.Count;
        }

        // Calculate confidence weight
        float deltaWeight = positiveCount - negativeCount;
        float targetWeight = CalculateWeight(deltaWeight);

        if (positiveCount > totalCount / 2)
        {
            // Bubble the property to parent
            Relationship r = t.AddRelationship(rr.target, rr.relType);
            r.Weight = targetWeight;

            // Remove from children (now redundant)
            foreach (Thing child in t.Children)
            {
                child.RemoveRelationship(rr.target, rr.relType);
            }
        }
    }
}
```

### Example: Learning Dog Properties

**Initial State**:
```
[Dog]
├── [Fido] --[has.color]--> [Brown], --[has]--> [Tail], --[has.legs]--> [4]
├── [Rover] --[has.color]--> [Brown], --[has]--> [Tail], --[has.legs]--> [4]
├── [Spot] --[has.color]--> [White], --[has]--> [Tail], --[has.legs]--> [4]
└── [Rex] --[has.color]--> [Black], --[has]--> [Tail], --[has.legs]--> [4]
```

**After Attribute Bubbling**:
```
[Dog] --[has]--> [Tail] (Weight: 0.95)  ← Bubbled (100% have it)
      --[has.legs]--> [4] (Weight: 0.95) ← Bubbled (100% have it)

├── [Fido] --[has.color]--> [Brown]  (NOT bubbled - only 25% Brown)
├── [Rover] --[has.color]--> [Brown]
├── [Spot] --[has.color]--> [White]
└── [Rex] --[has.color]--> [Black]
```

**Reasoning**: All dogs have tails and 4 legs (generalized), but color varies (kept specific).

## Redundancy Removal: Cleaning Inherited Duplicates

### Concept

After bubbling, some children may have relationships that are now redundant with parent attributes. The **RemoveRedundancy** module cleans these up.

**Module**: [ModuleRemoveRedundancy.cs](../BrainSimulator/Modules/Agents/ModuleRemoveRedundancy.cs)

### Algorithm

[ModuleRemoveRedundancy.cs:59-81](../BrainSimulator/Modules/Agents/ModuleRemoveRedundancy.cs#L59-L81):

```csharp
private void RemoveRedundantAttributes(Thing t)
{
    foreach (Thing parent in t.Parents)
    {
        // Get all relationships including inherited ones
        List<Relationship> relationshipsWithInheritance =
            theUKS.GetAllRelationships(new List<Thing> { parent });

        // Check each relationship of this Thing
        for (int i = 0; i < t.Relationships.Count; i++)
        {
            Relationship r = t.Relationships[i];

            // Find matching relationship in inheritance chain
            Relationship rMatch = relationshipsWithInheritance.FindFirst(
                x => x.source != r.source &&   // Different source
                     x.reltype == r.reltype &&  // Same type
                     x.target == r.target);     // Same target

            // If parent has strong version, reduce child's weight
            if (rMatch != null && rMatch.Weight > 0.8f)
            {
                r.Weight -= 0.1f;
                if (r.Weight < 0.5f)
                {
                    t.RemoveRelationship(r);  // Remove redundant
                    i--;
                }
            }
        }
    }
}
```

### Example

**Before Redundancy Removal**:
```
[Dog] --[has]--> [Tail] (Weight: 0.95)
[Fido] --[is-a]--> [Dog]
       --[has]--> [Tail] (Weight: 0.80, redundant!)
```

**After**:
```
[Dog] --[has]--> [Tail] (Weight: 0.95)
[Fido] --[is-a]--> [Dog]
       (Tail relationship removed - inherited from Dog)
```

## Dynamic Class Creation

### Concept

When multiple children share specific attribute combinations, the system can automatically create **intermediate classes** to capture these patterns.

**Module**: [ModuleClassCreate.cs](../BrainSimulator/Modules/Agents/ModuleClassCreate.cs)

### Algorithm

[ModuleClassCreate.cs:68-106](../BrainSimulator/Modules/Agents/ModuleClassCreate.cs#L68-L106):

```csharp
void HandleClassWithCommonAttributes(Thing t)
{
    // 1. Build list of attribute combinations
    List<RelDest> attributes = new();
    foreach (Thing child in t.ChildrenWithSubclasses)
    {
        foreach (Relationship r in child.Relationships)
        {
            if (r.reltype == Thing.IsA) continue;

            RelDest foundItem = attributes.FindFirst(
                x => x.relType == r.reltype && x.target == r.target);
            if (foundItem == null)
            {
                foundItem = new RelDest { relType = r.reltype, target = r.target };
                attributes.Add(foundItem);
            }
            foundItem.relationships.Add(r);
        }
    }

    // 2. Create intermediate parent for common attribute sets
    foreach (var key in attributes)
    {
        if (key.relationships.Count >= minCommonAttributes)
        {
            // Create new subclass with attribute in name
            Thing newParent = theUKS.GetOrAddThing(
                t.Label + "." + key.relType + "." + key.target, t);
            newParent.AddRelationship(key.target, key.relType);

            // Move matching children to new parent
            foreach (Relationship r in key.relationships)
            {
                Thing child = r.source;
                child.AddParent(newParent);
                child.RemoveParent(t);
            }
        }
    }
}
```

### Example: Auto-Creating Bird Subcategories

**Initial**:
```
[Bird]
├── [Robin] --[has.color]--> [Red]
├── [Cardinal] --[has.color]--> [Red]
├── [Bluebird] --[has.color]--> [Blue]
└── [Bluejay] --[has.color]--> [Blue]
```

**After Dynamic Class Creation** (if 2+ birds share color):
```
[Bird]
├── [Bird.has.color.Red]  ← Auto-created
│   --[has.color]--> [Red]
│   ├── [Robin]
│   └── [Cardinal]
└── [Bird.has.color.Blue]  ← Auto-created
    --[has.color]--> [Blue]
    ├── [Bluebird]
    └── [Bluejay]
```

## Exceptions: Overriding Inheritance

### Concept

Things can have **exception relationships** that override inherited attributes. This is handled through direct attributes with higher weights and exclusivity detection.

### Conflict Detection

[UKS.cs:196-295](../UKS/UKS.cs#L196-L295):

```csharp
private bool RelationshipsAreExclusive(Relationship r1, Relationship r2)
{
    // Same source, different targets from exclusive category
    if (r1.source == r2.source && r1.relType == r2.relType)
    {
        List<Thing> commonParents = FindCommonParents(r1.target, r2.target);
        foreach (Thing parent in commonParents)
        {
            if (parent.HasProperty("isExclusive"))
                return true;  // Color, count, etc.
        }
    }

    // Negation conflicts
    bool r1HasNot = r1.relType.GetAttributes().Any(x => x.Label == "not");
    bool r2HasNot = r2.relType.GetAttributes().Any(x => x.Label == "not");
    if (r1HasNot != r2HasNot && r1.target == r2.target)
        return true;  // "can fly" vs "cannot fly"

    return false;
}
```

### Example: Penguins Don't Fly

```
[Bird] --[can]--> [Fly] (Weight: 0.95, inherited)

[Penguin] --[is-a]--> [Bird]
          --[cannot]--> [Fly] (Weight: 1.0, direct exception)

Query: "Can a penguin fly?"
Process:
1. Check Penguin relationships: cannot Fly (Weight: 1.0)
2. Check inherited from Bird: can Fly (Weight: 0.95)
3. Detect conflict: "cannot" vs "can"
4. Higher weight + direct wins → Result: No
```

## Recursion Patterns

### 1. Depth-First Ancestor Traversal

```csharp
public void PrintAncestryTree(Thing t, int depth = 0)
{
    Console.WriteLine(new string(' ', depth * 2) + t.Label);
    foreach (Thing parent in t.Parents)
    {
        PrintAncestryTree(parent, depth + 1);
    }
}
```

### 2. Breadth-First Relationship Search

[Thing.cs:276-302](../UKS/Thing.cs#L276-L302): Implemented in `FollowTransitiveRelationships()`

```csharp
// Breadth-first ensures closest ancestors checked first
List<Thing> queue = new() { startThing };
for (int i = 0; i < queue.Count; i++)
{
    Thing current = queue[i];
    foreach (Relationship r in current.Relationships)
    {
        if (r.reltype == targetRelType && !queue.Contains(r.target))
            queue.Add(r.target);
    }
}
```

### 3. Circular Reference Protection

The system prevents infinite loops through visited-set tracking:

```csharp
List<Thing> visited = new();

void TraverseGraph(Thing t)
{
    if (visited.Contains(t)) return;  // Already visited
    visited.Add(t);

    foreach (Thing child in t.Children)
    {
        TraverseGraph(child);
    }
}
```

## Implementing in PyTorch/PyTorch Geometric

### Strategy 1: Hierarchical Graph Neural Network

```python
import torch
import torch.nn as nn
from torch_geometric.nn import MessagePassing

class HierarchicalGNN(MessagePassing):
    def __init__(self, in_channels, out_channels):
        super().__init__(aggr='add')
        self.lin = nn.Linear(in_channels, out_channels)

    def forward(self, x, edge_index, edge_type):
        # Separate "is-a" edges from other relationship types
        is_a_mask = (edge_type == IS_A_TYPE_ID)
        is_a_edges = edge_index[:, is_a_mask]
        other_edges = edge_index[:, ~is_a_mask]

        # Propagate along is-a hierarchy (inheritance)
        x_inherited = self.propagate(is_a_edges, x=x, edge_type=edge_type[is_a_mask])

        # Propagate along other relationships
        x_relational = self.propagate(other_edges, x=x, edge_type=edge_type[~is_a_mask])

        # Combine inherited and direct features
        return self.lin(x + x_inherited + x_relational)

    def message(self, x_j, edge_type):
        # x_j are features of neighbor nodes
        # Weight by relationship type and inheritance distance
        return x_j
```

### Strategy 2: Explicit Inheritance in Graph Structure

```python
import networkx as nx

class InheritanceGraph:
    def __init__(self):
        self.graph = nx.DiGraph()

    def add_thing(self, label, parent=None):
        self.graph.add_node(label)
        if parent:
            self.graph.add_edge(label, parent, reltype='is-a')

    def get_ancestors(self, label):
        """Recursive ancestor collection"""
        ancestors = []
        node = label
        while True:
            parents = [n for n in self.graph.successors(node)
                       if self.graph[node][n].get('reltype') == 'is-a']
            if not parents:
                break
            ancestors.extend(parents)
            node = parents[0]  # Single inheritance
        return ancestors

    def get_inherited_attributes(self, label):
        """Collect attributes from ancestors"""
        attributes = {}
        for ancestor in [label] + self.get_ancestors(label):
            for neighbor in self.graph.successors(ancestor):
                edge_data = self.graph[ancestor][neighbor]
                reltype = edge_data.get('reltype')
                if reltype != 'is-a':
                    attributes[reltype] = neighbor
        return attributes

    def bubble_attributes(self, parent_label):
        """Move common child attributes to parent"""
        children = [n for n in self.graph.predecessors(parent_label)
                    if self.graph[n][parent_label].get('reltype') == 'is-a']

        # Count attribute frequency
        attr_counts = {}
        for child in children:
            for neighbor in self.graph.successors(child):
                edge_data = self.graph[child][neighbor]
                reltype = edge_data.get('reltype')
                if reltype != 'is-a':
                    key = (reltype, neighbor)
                    attr_counts[key] = attr_counts.get(key, 0) + 1

        # Bubble if > 50% have it
        threshold = len(children) / 2
        for (reltype, target), count in attr_counts.items():
            if count > threshold:
                self.graph.add_edge(parent_label, target, reltype=reltype)
                # Remove from children
                for child in children:
                    if self.graph.has_edge(child, target):
                        self.graph.remove_edge(child, target)
```

### Strategy 3: Neural Inheritance with Learned Weights

```python
class NeuralInheritance(nn.Module):
    def __init__(self, embedding_dim):
        super().__init__()
        self.inheritance_weight = nn.Linear(embedding_dim * 2, 1)

    def forward(self, child_emb, parent_emb, parent_attr_emb):
        """
        Determine how much of parent's attribute to inherit
        """
        # Concatenate child and parent embeddings
        combined = torch.cat([child_emb, parent_emb], dim=-1)

        # Learned inheritance weight
        weight = torch.sigmoid(self.inheritance_weight(combined))

        # Apply inheritance
        inherited_attr = weight * parent_attr_emb
        return inherited_attr
```

## Key Takeaways

1. **IS-A as directed edges**: Simple graph structure enables complex inheritance
2. **Transitive traversal**: Breadth-first search for inheritance chains
3. **Multiple inheritance**: DAG structure supports diamond patterns
4. **Attribute bubbling**: Bottom-up generalization through frequency analysis
5. **Redundancy removal**: Top-down cleanup after bubbling
6. **Dynamic classes**: Auto-create intermediate nodes for pattern grouping
7. **Exception handling**: Weight + exclusivity for override resolution
8. **Recursion protection**: Visited-set tracking prevents infinite loops

## References

- [Thing.cs](../UKS/Thing.cs) - Parent/child relationships, traversal
- [UKS.cs](../UKS/UKS.cs) - Relationship trees, conflict detection
- [ModuleAttributeBubble.cs](../BrainSimulator/Modules/Agents/ModuleAttributeBubble.cs) - Bottom-up generalization
- [ModuleRemoveRedundancy.cs](../BrainSimulator/Modules/Agents/ModuleRemoveRedundancy.cs) - Inheritance cleanup
- [ModuleClassCreate.cs](../BrainSimulator/Modules/Agents/ModuleClassCreate.cs) - Dynamic class formation
