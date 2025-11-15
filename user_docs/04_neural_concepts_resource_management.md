# Neural Concepts and Resource Management in BrainSim III

## Overview

While BrainSim III is primarily a knowledge representation system, it incorporates several neural-inspired concepts including sparse coding, lateral inhibition, gating mechanisms, and resource management (neuron tracking and column selection). This document explores how these concepts are implemented through the UKS graph structure.

## Sparse Coding Through Singularity

### Concept

In neuroscience, **sparse coding** means representing information with a small number of active neurons from a large population. BrainSim III achieves this through:

1. **Information Singularity**: Each concept exists in exactly one location
2. **Unique Labels**: Hash table ensures O(1) lookup without redundancy
3. **Distributed Relationships**: Information spreads through edges, not nodes

### Implementation

**File**: [ThingLabels.cs](../UKS/ThingLabels.cs)

```csharp
public class ThingLabels
{
    private static Dictionary<string, Thing> thingLabels = new();

    public static Thing GetThing(string label)
    {
        if (string.IsNullOrEmpty(label)) return null;
        string labelLower = label.ToLower();

        if (thingLabels.TryGetValue(labelLower, out Thing? value))
            return value;
        return null;
    }

    public static string AddThingLabel(string label, Thing t)
    {
        string labelLower = label.ToLower();

        // Enforce singularity: one Thing per concept
        if (thingLabels.ContainsKey(labelLower))
        {
            if (thingLabels[labelLower] != t)
                throw new Exception("Duplicate label");
            return thingLabels[labelLower].Label;
        }

        thingLabels[labelLower] = t;
        return label;  // Preserve original case
    }
}
```

### Benefits

1. **Memory Efficiency**: No duplicate representations
2. **Fast Access**: O(1) lookup vs. O(n) search
3. **Consistency**: Single source of truth
4. **Sparse Activation**: Only referenced Things are "active" at query time

### Comparison to Neural Sparse Coding

| Neural Sparse Coding | BrainSim III |
|---------------------|--------------|
| Few active neurons | Few active Things in query |
| High-dimensional space | Large graph space |
| Energy efficient | Computationally efficient |
| Distributed representation | Distributed relationships |
| Redundancy for resilience | Multiple paths for resilience |

## Lateral Inhibition Through Exclusivity

### Concept

**Lateral inhibition** in neuroscience occurs when neurons suppress their neighbors, creating winner-take-all competition. BrainSim III implements this through:

1. **Exclusive properties**: Conflicting attributes suppress each other
2. **Weight competition**: Higher-weighted relationships win
3. **Redundancy removal**: Duplicate attributes are pruned

### Exclusive Properties

**File**: [UKS.cs:196-295](../UKS/UKS.cs#L196-L295)

```csharp
private bool RelationshipsAreExclusive(Relationship r1, Relationship r2)
{
    // Check if targets have common parent with "isExclusive" property
    if (r1.relType == r2.relType && r1.target != r2.target)
    {
        List<Thing> commonParents = FindCommonParents(r1.target, r2.target);
        foreach (Thing parent in commonParents)
        {
            if (parent.HasProperty("isExclusive"))
                return true;  // Only one can be true
        }
    }

    // Check for negation conflicts
    IList<Thing> r1Attrs = r1.relType.GetAttributes();
    IList<Thing> r2Attrs = r2.relType.GetAttributes();
    Thing r1Not = r1Attrs.FindFirst(x => x.Label == "not" || x.Label == "no");
    Thing r2Not = r2Attrs.FindFirst(x => x.Label == "not" || x.Label == "no");

    if ((r1Not != null && r2Not == null) || (r1Not == null && r2Not != null))
    {
        if (r1.target == r2.target)
            return true;  // "can fly" vs "cannot fly"
    }

    return false;
}
```

### Example: Color Exclusivity

**Setup**:
```csharp
Thing color = uks.GetOrAddThing("Color", "Attribute");
color.AddRelationship("isExclusive", "hasProperty");

Thing red = uks.GetOrAddThing("Red", color);
Thing blue = uks.GetOrAddThing("Blue", color);
Thing green = uks.GetOrAddThing("Green", color);
```

**Usage**:
```
[Ball] --[has.color]--> [Red]  (Weight: 0.9)
[Ball] --[has.color]--> [Blue] (Weight: 0.6)  ← Suppressed

Result: Ball is Red (Blue relationship has lower weight and conflicts)
```

### Conflict Resolution Algorithm

```csharp
public List<Relationship> ResolveConflicts(List<Relationship> relationships)
{
    List<Relationship> resolved = new();

    foreach (Relationship r1 in relationships)
    {
        bool suppressed = false;

        foreach (Relationship r2 in relationships)
        {
            if (r1 == r2) continue;

            // Check if they conflict
            if (RelationshipsAreExclusive(r1, r2))
            {
                // Higher weight wins
                if (r2.Weight > r1.Weight)
                {
                    suppressed = true;
                    break;
                }
            }
        }

        if (!suppressed)
            resolved.Add(r1);
    }

    return resolved;
}
```

## Gating Mechanisms

### Concept

**Gating** controls information flow, determining what gets processed or propagated. BrainSim III implements gating through:

1. **Conditional clauses**: IF/UNLESS act as gates
2. **Weight thresholds**: Low-weight relationships are gated out
3. **Property-based filtering**: hasProperty gates inheritance
4. **Transience**: TTL gates temporal information

### IF Clause as Gate

```
[Action] --[triggers]--> [Response]
    └── IF: [Condition] --[is]--> [True]

Gate open: Condition is true → Response triggered
Gate closed: Condition is false → Response blocked
```

**Implementation**:
```csharp
public bool IsGateOpen(Relationship r)
{
    // Check IF clauses (gates)
    foreach (Clause c in r.Clauses)
    {
        if (c.clauseType.Label == "IF")
        {
            // Gate closed if condition not met
            if (!VerifyCondition(c.clause))
                return false;
        }
    }
    return true;  // All gates open
}
```

### Weight Threshold Gating

**File**: [ModuleAttributeBubble.cs:167-174](../BrainSimulator/Modules/Agents/ModuleAttributeBubble.cs#L167-L174)

```csharp
float newWeight = currentWeight + targetWeight;

if (newWeight < 0.5)  // Threshold gate
{
    // Below threshold: gate closed, remove relationship
    if (r != null)
    {
        t.RemoveRelationship(r);
        debugString += $"Removed {r.ToString()} \n";
    }
}
else
{
    // Above threshold: gate open, keep/add relationship
    r = t.AddRelationship(rr.target, rr.relType);
    r.Weight = newWeight;
}
```

### Property-Based Gating

Certain properties gate whether a relationship is inherited or transitive:

```
[has-child] --[hasProperty]--> [isTransitive]  ← Gates recursive following

Query: "What does X have?"
- If relType has isTransitive: Follow inheritance chain
- If not: Only direct relationships
```

## Tracking Free/In-Use Things (Neuron Allocation)

### Concept

Similar to how neural circuits track available neurons, BrainSim III tracks Thing usage through:

1. **useCount**: How many times a Thing has been accessed
2. **lastFiredTime**: When it was last activated
3. **Relationship hits/misses**: Usage statistics

### Implementation

**File**: [Thing.cs:65-66](../UKS/Thing.cs#L65-L66)

```csharp
public int useCount = 0;
public DateTime lastFiredTime = new();

public void SetFired()
{
    lastFiredTime = DateTime.Now;
    useCount++;
}
```

**File**: [Relationship.cs:158-167](../UKS/Relationship.cs#L158-L167)

```csharp
private int hits = 0;      // Successful uses
private int misses = 0;    // Failed uses

public int Hits { get => hits; set => hits = value; }
public int Misses { get => misses; set => misses = value; }

public float Value  // Computed confidence
{
    get
    {
        float retVal = Weight;
        if (Hits != 0 && Misses != 0)
        {
            retVal = Hits / (Misses == 0 ? 0.1f : Misses);
        }
        return retVal;
    }
}
```

### Usage Tracking Example

```csharp
// Query for a Thing
Thing dog = uks.Labeled("Dog");
dog.SetFired();  // Mark as accessed

// Query a relationship
Relationship r = dog.Relationships.FindFirst(x => x.relType.Label == "has");
r.Hits++;  // Successful query
r.Fire();  // Update lastUsed timestamp

// Failed query
Relationship r2 = dog.Relationships.FindFirst(x => x.relType.Label == "unknown");
if (r2 == null)
{
    foreach (Relationship rel in dog.Relationships)
        rel.Misses++;  // All relationships "missed"
}
```

**File**: [Thing.cs:189-191](../UKS/Thing.cs#L189-L191)

```csharp
public IList<Relationship> Relationships
{
    get
    {
        lock (relationships)
        {
            // Automatically increment misses on access
            foreach (Relationship r in relationships)
                r.Misses++;
            return new List<Relationship>(relationships.AsReadOnly());
        }
    }
}
```

### Resource Reclamation

Things with low usage could be pruned (not currently implemented, but framework exists):

```csharp
public void PruneUnusedThings(TimeSpan inactivityThreshold)
{
    DateTime cutoff = DateTime.Now - inactivityThreshold;
    List<Thing> toRemove = new();

    foreach (Thing t in uks.UKSList)
    {
        if (t.lastFiredTime < cutoff && t.useCount < 5)
        {
            toRemove.Add(t);
        }
    }

    foreach (Thing t in toRemove)
    {
        uks.DeleteThing(t);
    }
}
```

## Selecting Columns to Use

### Concept

In cortical columns, specific columns are selected for processing. BrainSim III achieves similar selection through:

1. **Dynamic instance creation**: Auto-numbered Things act as "columns"
2. **Instance tracking**: Finding the next available instance
3. **Context-specific selection**: Choosing the right instance for a situation

### Dynamic Instance Creation

**File**: [UKS.cs:511-527](../UKS/UKS.cs#L511-L527)

```csharp
public Thing GetOrAddThing(string label, object parent = null, Thing source = null)
{
    if (label.EndsWith("*"))
    {
        string baseLabel = label.Substring(0, label.Length - 1);
        Thing newParent = ThingLabels.GetThing(baseLabel);

        // Find next available instance number
        if (source != null)
        {
            int digit = 0;
            while (source.Relationships.FindFirst(
                x => x.reltype.Label == baseLabel + digit) != null)
            {
                digit++;
            }

            // Check if this instance already exists
            Thing labeled = ThingLabels.GetThing(baseLabel + digit);
            if (labeled != null)
                return labeled;  // Reuse existing "column"
        }

        if (newParent == null)
            newParent = AddThing(baseLabel, correctParent);
        correctParent = newParent;
    }

    thingToReturn = AddThing(label, correctParent);
    return thingToReturn;
}
```

### Example: Column Allocation

```csharp
// Create base "column type"
Thing bird = uks.GetOrAddThing("Bird", "Animal");

// Allocate specific "columns" (instances)
Thing bird0 = uks.GetOrAddThing("Bird*");  // Creates "Bird0"
Thing bird1 = uks.GetOrAddThing("Bird*");  // Creates "Bird1"
Thing bird2 = uks.GetOrAddThing("Bird*");  // Creates "Bird2"

// Each instance can have different properties
bird0.AddRelationship("Blue", "has.color");
bird1.AddRelationship("Red", "has.color");
bird2.AddRelationship("Green", "has.color");

// All share base class properties
foreach (Thing instance in bird.Children)
{
    // Inherits from Bird: can fly, has feathers, etc.
}
```

### Context-Based Column Selection

```csharp
public Thing SelectAppropriateInstance(Thing baseClass, List<Thing> requiredAttributes)
{
    // Find instance matching context
    foreach (Thing instance in baseClass.Children)
    {
        bool matches = true;
        foreach (Thing attr in requiredAttributes)
        {
            if (instance.Relationships.FindFirst(x => x.target == attr) == null)
            {
                matches = false;
                break;
            }
        }

        if (matches)
            return instance;  // Found appropriate "column"
    }

    // No match: allocate new instance
    Thing newInstance = uks.GetOrAddThing(baseClass.Label + "*");
    foreach (Thing attr in requiredAttributes)
    {
        newInstance.AddRelationship(attr, "has");
    }
    return newInstance;
}
```

## In/Out Neurons (Relationship Direction)

### Concept

Neurons have input and output connections. BrainSim III models this through:

1. **relationships**: Outgoing edges (outputs)
2. **relationshipsFrom**: Incoming edges (inputs)
3. **relationshipsAsType**: Where this Thing is the connection type

### Implementation

**File**: [Thing.cs:38-61](../UKS/Thing.cs#L38-L61)

```csharp
private List<Relationship> relationships = new();       // Output connections
private List<Relationship> relationshipsFrom = new();   // Input connections
private List<Relationship> relationshipsAsType = new(); // Type connections

// Safe "input neuron" access
public IList<Relationship> RelationshipsFrom
{
    get
    {
        lock (relationshipsFrom)
        {
            return new List<Relationship>(relationshipsFrom.AsReadOnly());
        }
    }
}

// Safe "output neuron" access
public IList<Relationship> Relationships
{
    get
    {
        lock (relationships)
        {
            foreach (Relationship r in relationships)
                r.Misses++;  // Track access
            return new List<Relationship>(relationships.AsReadOnly());
        }
    }
}
```

### Bidirectional Navigation

```csharp
// Forward propagation (outputs)
public List<Thing> GetOutputs(Thing t, Thing relType)
{
    List<Thing> outputs = new();
    foreach (Relationship r in t.Relationships)
    {
        if (r.relType == relType)
            outputs.Add(r.target);
    }
    return outputs;
}

// Backward propagation (inputs)
public List<Thing> GetInputs(Thing t, Thing relType)
{
    List<Thing> inputs = new();
    foreach (Relationship r in t.RelationshipsFrom)
    {
        if (r.relType == relType)
            inputs.Add(r.source);
    }
    return inputs;
}
```

### Example: Causal Chain

```
[Rain] --[causes]--> [Wet-Ground]
[Wet-Ground] --[causes]--> [Slippery-Surface]

Query: "What causes Wet-Ground?"
Process:
1. Access Wet-Ground.RelationshipsFrom (inputs)
2. Filter by relType "causes"
3. Result: Rain

Query: "What does Wet-Ground cause?"
Process:
1. Access Wet-Ground.Relationships (outputs)
2. Filter by relType "causes"
3. Result: Slippery-Surface
```

## Cortical Columns (Hierarchical Thing Groups)

### Concept

Cortical columns are vertical structures processing related information. BrainSim III creates analogous structures through:

1. **Hierarchical Thing groupings**: Parent-child trees
2. **Shared attribute inheritance**: Common properties bubble up
3. **Specialized instances**: Children with unique properties

### Example: Animal Hierarchy as Cortical Structure

```
[Animal] ← "Column root"
  |
  ├─ [Mammal]
  │   ├─ [Dog]
  │   │   ├─ [Retriever] ← "Sub-column"
  │   │   │   ├─ [Fido0] (instance)
  │   │   │   └─ [Fido1] (instance)
  │   │   └─ [Poodle] ← "Sub-column"
  │   │       ├─ [Rex0] (instance)
  │   │       └─ [Rex1] (instance)
  │   └─ [Cat]
  │       ├─ [Siamese]
  │       └─ [Persian]
  └─ [Bird]
      ├─ [Sparrow]
      └─ [Eagle]
```

Each level represents a cortical layer:
- **Root**: Broadest category
- **Intermediate**: Specialized groups
- **Leaf**: Specific instances

### Column Activation Pattern

```csharp
public void ActivateColumn(Thing root, Thing stimulus)
{
    // Activate root
    root.SetFired();

    // Propagate down to matching children
    foreach (Thing child in root.Children)
    {
        // Check if stimulus matches this sub-column
        if (MatchesStimulus(child, stimulus))
        {
            child.SetFired();
            ActivateColumn(child, stimulus);  // Recursive descent
        }
    }
}

private bool MatchesStimulus(Thing thing, Thing stimulus)
{
    // Check if thing has attributes matching stimulus
    foreach (Relationship r in thing.Relationships)
    {
        if (r.target == stimulus)
            return true;
    }
    return false;
}
```

## Redundancy and Resilience

### Concept

Neural systems are resilient due to redundant pathways. BrainSim III achieves this through:

1. **Multiple inheritance**: Multiple paths to information
2. **Distributed representation**: Information in structure, not single node
3. **Relationship backup**: Same fact represented multiple ways

### Example: Redundant Knowledge Paths

```
Path 1: [Fido] --[is-a]--> [Dog] --[is-a]--> [Animal]
Path 2: [Fido] --[is-a]--> [Pet] --[is-a]--> [Animal]
Path 3: [Fido] --[has.owner]--> [John] --[has.pet]--> [Animal-Type]

Query: "Is Fido an animal?"
Can be answered via:
- Direct is-a chain (Path 1)
- Alternative is-a chain (Path 2)
- Inference from ownership (Path 3)
```

### Resilience to Node Loss

```csharp
public bool CanReachTarget(Thing source, Thing target, HashSet<Thing> blockedNodes)
{
    if (source == target) return true;
    if (blockedNodes.Contains(source)) return false;

    HashSet<Thing> visited = new() { source };
    Queue<Thing> queue = new();
    queue.Enqueue(source);

    while (queue.Count > 0)
    {
        Thing current = queue.Dequeue();

        // Try all outgoing relationships
        foreach (Relationship r in current.Relationships)
        {
            Thing next = r.target;
            if (blockedNodes.Contains(next)) continue;
            if (next == target) return true;

            if (!visited.Contains(next))
            {
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
    }

    return false;  // No path found
}
```

Even if intermediate nodes are blocked/deleted, multiple paths provide resilience.

## Implementing in PyTorch/PyTorch Geometric

### Strategy 1: Sparse Graph Attention

```python
import torch
from torch_geometric.nn import GATConv

class SparseAttentionKG(torch.nn.Module):
    def __init__(self, in_channels, out_channels):
        super().__init__()
        self.attention = GATConv(in_channels, out_channels, heads=8, dropout=0.6)

    def forward(self, x, edge_index):
        # Attention mechanism acts as lateral inhibition
        # High attention weights suppress low attention neighbors
        return self.attention(x, edge_index)
```

### Strategy 2: Gating with Neural Networks

```python
class GatedRelationship(torch.nn.Module):
    def __init__(self, feature_dim):
        super().__init__()
        self.gate = nn.Sequential(
            nn.Linear(feature_dim * 2, feature_dim),
            nn.Sigmoid()  # Gate value [0, 1]
        )

    def forward(self, source_features, target_features, condition_features):
        # Compute gate value
        combined = torch.cat([source_features, condition_features], dim=-1)
        gate_value = self.gate(combined)

        # Apply gate to target
        gated_target = gate_value * target_features
        return gated_target
```

### Strategy 3: Dynamic Instance Pool

```python
class InstancePool:
    def __init__(self, base_class, max_instances=100):
        self.base_class = base_class
        self.instances = []
        self.available = list(range(max_instances))
        self.in_use = {}

    def allocate(self, attributes):
        """Allocate an instance (column) with given attributes"""
        if not self.available:
            raise RuntimeError("No available instances")

        instance_id = self.available.pop(0)
        self.instances.append({
            'id': instance_id,
            'base_class': self.base_class,
            'attributes': attributes,
            'use_count': 0,
            'last_used': time.time()
        })
        self.in_use[instance_id] = len(self.instances) - 1
        return instance_id

    def release(self, instance_id):
        """Release instance back to pool"""
        if instance_id in self.in_use:
            idx = self.in_use[instance_id]
            del self.in_use[instance_id]
            self.available.append(instance_id)
```

### Strategy 4: Usage Tracking in PyG

```python
class UsageTracker:
    def __init__(self, num_nodes, num_edges):
        self.node_use_count = torch.zeros(num_nodes, dtype=torch.long)
        self.node_last_fired = torch.zeros(num_nodes, dtype=torch.float32)
        self.edge_hits = torch.zeros(num_edges, dtype=torch.long)
        self.edge_misses = torch.zeros(num_edges, dtype=torch.long)

    def fire_node(self, node_id):
        self.node_use_count[node_id] += 1
        self.node_last_fired[node_id] = time.time()

    def hit_edge(self, edge_id):
        self.edge_hits[edge_id] += 1

    def miss_edge(self, edge_id):
        self.edge_misses[edge_id] += 1

    def get_edge_confidence(self, edge_id):
        hits = self.edge_hits[edge_id].float()
        misses = self.edge_misses[edge_id].float()
        return hits / (misses + 1e-6)  # Confidence score
```

## Key Takeaways

1. **Sparse coding**: Unique labels ensure singular representation
2. **Lateral inhibition**: Exclusive properties create competition
3. **Gating**: IF/UNLESS clauses, weight thresholds control flow
4. **Resource tracking**: useCount, lastFiredTime monitor usage
5. **Column selection**: Dynamic instances with auto-numbering
6. **In/Out neurons**: Bidirectional relationship navigation
7. **Cortical columns**: Hierarchical Thing groups
8. **Redundancy**: Multiple paths provide resilience

## References

- [Thing.cs](../UKS/Thing.cs) - Usage tracking, relationship navigation
- [Relationship.cs](../UKS/Relationship.cs) - Hits/misses, weight computation
- [UKS.cs](../UKS/UKS.cs) - Conflict detection, instance creation
- [ThingLabels.cs](../UKS/ThingLabels.cs) - Sparse label management
- [ModuleAttributeBubble.cs](../BrainSimulator/Modules/Agents/ModuleAttributeBubble.cs) - Weight-based gating
