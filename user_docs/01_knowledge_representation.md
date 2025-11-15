# Knowledge Representation in BrainSim III

## Overview

BrainSim III implements knowledge representation through the Universal Knowledge Store (UKS), a custom in-memory graph database that models knowledge as a network of interconnected concepts. This document explores how the system represents and organizes information without relying on traditional symbolic programming.

## Core Architecture

### The Graph Model

At its foundation, the UKS uses a property graph model with three fundamental elements:

1. **Thing** (Node) - Represents any concept, object, attribute, action, or entity
2. **Relationship** (Edge) - Connects two Things with semantic meaning
3. **Clause** - Meta-relationship connecting two Relationships (conditional logic)

```
[Thing A] --[Relationship Type]--> [Thing B]
```

## Thing: Universal Node Abstraction

### Concept

A `Thing` is the most fundamental abstraction in BrainSim III. It can represent:
- Physical objects ("dog", "Fido", "table")
- Abstract concepts ("color", "happiness", "justice")
- Actions ("run", "eat", "think")
- Attributes ("red", "big", "fast")
- Relationship types themselves ("has", "is-a", "likes")
- Numbers, properties, categories

**Key Insight**: The system is **self-referential** - relationship types are themselves Things in the graph. This eliminates the need for a separate schema layer.

### Implementation

**File**: [UKS/Thing.cs](../UKS/Thing.cs)

```csharp
public partial class Thing
{
    // Core identity
    private string label = "";                          // Human-readable identifier
    object value;                                       // Arbitrary attached data (V property)

    // Relationship lists (bidirectional navigation)
    private List<Relationship> relationships;           // Outgoing edges
    private List<Relationship> relationshipsFrom;       // Incoming edges
    private List<Relationship> relationshipsAsType;     // Where this is the rel type

    // Usage tracking (for learning/weighting)
    public int useCount = 0;
    public DateTime lastFiredTime;

    // Implicit string conversion for easy API usage
    public static implicit operator Thing(string label)
}
```

### Key Properties

**Label Management**:
- Case-insensitive (internally lowercase)
- Unique across the UKS
- Maintained in hash table ([ThingLabels.cs](../UKS/ThingLabels.cs)) for O(1) lookup
- Can contain dots (.) for attribute notation: "dog.brown.4legs"

**Hierarchy Navigation**:
```csharp
public IList<Thing> Parents        // Direct ancestors via "is-a"
public IList<Thing> Children       // Direct descendants via "is-a"
public IEnumerable<Thing> Ancestors    // Recursive upward traversal
public IEnumerable<Thing> Descendents  // Recursive downward traversal
```

**Usage Tracking**:
- `useCount`: Increments when Thing is accessed
- `lastFiredTime`: Timestamp of last activation
- Used for determining relevance and pruning

## Relationship: Semantic Edge

### Concept

A `Relationship` is a **weighted, directed edge** connecting two Things with semantic meaning defined by a relationship type (which is itself a Thing).

**Structure**: `[source Thing] --[relType Thing]--> [target Thing]`

Example:
```
[Fido] --[is-a]--> [Dog]
[Dog] --[has]--> [Leg]
[Sky] --[has.color]--> [Blue]
```

### Implementation

**File**: [UKS/Relationship.cs](../UKS/Relationship.cs)

```csharp
public class Relationship
{
    // Core triple
    public Thing source;              // Subject
    public Thing reltype;             // Predicate (relationship type)
    public Thing target;              // Object

    // Conditional logic
    private List<Clause> clauses;     // IF/BECAUSE conditions
    public List<Relationship> clausesFrom;  // Reverse links

    // Confidence/weight management
    private float weight = 1;         // Explicit confidence
    private int hits = 0;             // Usage success count
    private int misses = 0;           // Usage failure count

    // Temporal properties
    public DateTime lastUsed;
    public DateTime created;
    private TimeSpan timeToLive;      // For transient relationships

    // Meta properties
    public bool GPTVerified;          // LLM verification flag
    public bool isStatement;          // True statement vs. query pattern
}
```

### Key Features

**1. Weight-Based Confidence**

The system uses a hybrid weighting approach:
- **Explicit weight** (0-1 range): Set by programmer or agent
- **Implicit weight** (hits/misses ratio): Learned from usage

```csharp
public float Value
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

**2. Bidirectional Navigation**

Every relationship automatically maintains three linkages:
- Added to `source.relationships` (outgoing)
- Added to `target.relationshipsFrom` (incoming)
- Added to `reltype.relationshipsAsType` (all uses of this type)

This enables efficient queries in any direction.

**3. Transient Relationships**

Relationships can have a Time-To-Live (TTL):
```csharp
relationship.TimeToLive = TimeSpan.FromSeconds(30);
```

A background timer ([UKS.cs:46](../UKS/UKS.cs#L46)) removes expired relationships, enabling:
- Working memory (temporary associations)
- Attention mechanisms
- Short-term context

## Clause: Conditional Meta-Relationships

### Concept

A `Clause` is a **relationship between relationships**, enabling conditional logic without symbolic programming.

**Structure**: `[Relationship A] --[clause type]--> [Relationship B]`

Clause types include:
- `IF`: Conditional dependency
- `BECAUSE`: Causal relationship
- `UNLESS`: Exception handling
- Custom clause types (extensible)

### Implementation

```csharp
public class Clause
{
    public Thing clauseType;          // "IF", "BECAUSE", etc.
    public Relationship clause;       // Target relationship
}
```

**Example**:
```
Statement: "Birds can fly"
[Bird] --[can]--> [Fly]

Conditional: "Birds can fly IF they are not penguins"
[Bird] --[can]--> [Fly]  IF  [Bird] --[is-not]--> [Penguin]
```

### Usage in Code

From [UKS.cs:586](../UKS/UKS.cs#L586):
```csharp
public Relationship AddClause(Relationship r1, Thing clauseType,
                               Thing source, Thing relType, Thing target)
{
    // Create conditional relationship (not a statement)
    Relationship rTemp = new() {
        source = source,
        reltype = relType,
        target = target,
        Weight = .9f,
        isStatement = false  // This is a condition, not a fact
    };

    // Handle IF clauses by creating instances
    if (clauseType.Label.ToLower() == "if" && r1.isStatement)
    {
        // Create a new instance of the source
        Thing newInstance = GetOrAddThing(r1.source.Label + "*", r1.source);
        // Move relationship to instance level
        rRoot = newInstance.AddRelationship(r1.target, r1.relType, false);
        AddStatement(rRoot.source.Label, "hasProperty", "isInstance");
    }

    // Attach the clause
    rRoot.AddClause(clauseType, rTemp);
    return rRoot;
}
```

## Knowledge Organization Principles

### 1. Information is Singular

**Concept**: Each piece of information exists in exactly one canonical location.

**Implementation**:
- Labels are unique identifiers
- Hash table lookup ensures single Thing per concept
- Relationships deduplicated on creation ([Thing.cs:322](../UKS/Thing.cs#L322))

**Example**:
```csharp
Thing dog1 = uks.GetOrAddThing("dog");  // Creates if doesn't exist
Thing dog2 = uks.GetOrAddThing("dog");  // Returns same instance
// dog1 == dog2  (same object reference)
```

### 2. Structure + Interconnection = Facts

**Concept**: Knowledge emerges from the structure of the graph and how things interconnect, not from data stored in nodes.

**Example**:
```
[Fido]
  --[is-a]--> [Dog]
  --[has]--> [Color.Brown]
  --[has]--> [Tail]

[Dog]
  --[is-a]--> [Animal]
  --[has]--> [Leg] (count: 4)
  --[can]--> [Bark]
```

Facts derivable:
- Fido is an animal (transitive is-a)
- Fido can bark (inherited capability)
- Fido has 4 legs (inherited structure)
- Fido is brown (direct attribute)

### 3. Directionality

**Concept**: Relationships are directed, but the system maintains inverse relationships automatically.

**Implementation**:
Certain relationship types have `inverseOf` property. When creating a relationship with an inverse type, the system automatically creates the reverse relationship.

**File**: [UKS.Statement.cs](../UKS/UKS.Statement.cs) (part of UKS partial class)

**Example**:
```
"parent-of" inverseOf "child-of"

Adding: [John] --[parent-of]--> [Mary]
Automatically creates: [Mary] --[child-of]--> [John]
```

### 4. Self-Referential Type System

**Concept**: Relationship types are themselves Things in the UKS, eliminating the need for a rigid schema.

**Bootstrap** ([UKS.cs:CreateInitialStructure](../UKS/UKS.cs)):
```
Thing (root)
├── RelationshipType
│   ├── is-a
│   ├── has
│   ├── has-child
│   └── ... (custom types)
├── Object
├── Action
└── Properties
    ├── isTransitive
    ├── isCommutative
    ├── isExclusive
    └── allowMultiple
```

This enables:
- Runtime definition of new relationship types
- Properties on relationship types (transitive, commutative, etc.)
- Query relationships by their properties

## Knowledge Representation Without Symbolic Programming

### Traditional Approach (Symbolic AI)

```python
# Explicit rules and structures
class Animal:
    def __init__(self, name, legs):
        self.name = name
        self.legs = legs

class Dog(Animal):
    def bark(self):
        return "woof"

# Hard-coded logic
if isinstance(animal, Dog):
    animal.bark()
```

### BrainSim III Approach (Structural AI)

```
# Structure defines behavior
[Dog]
  --[is-a]--> [Animal]
  --[has]--> [Leg] (count: 4)
  --[can]--> [Bark]

# Query determines behavior
Query: What can a dog do?
Result: Follow "can" relationships → Bark
```

**Advantages**:
1. **Flexibility**: Add new concepts without modifying code
2. **Learning**: Relationships can be created from experience
3. **Gradual confidence**: Weight-based certainty vs. binary true/false
4. **Exception handling**: Conditional clauses override inherited attributes
5. **Natural reasoning**: Query traversal mimics associative thinking

## Reasoning Through Structure

### Inheritance Chains

The system follows `is-a` relationships to inherit properties:

```
Query: "What color is Fido?"

Graph:
[Fido] --[is-a]--> [Dog]
[Dog] --[has.color]--> [Brown]

Process:
1. Check Fido's direct relationships for "has.color" → Not found
2. Follow "is-a" to Dog
3. Check Dog's relationships for "has.color" → Found: Brown
4. Return: Brown (with inherited confidence weight)
```

**Implementation**: [Thing.cs:276](../UKS/Thing.cs#L276) - `FollowTransitiveRelationships()`

### Conflict Resolution

When multiple inherited or direct attributes conflict, the system uses:

1. **Exclusivity detection**: Common parents with `isExclusive` property
2. **Weight comparison**: Higher-weighted relationship wins
3. **Specificity**: Direct attributes override inherited ones
4. **Negation handling**: "not X" conflicts with "X"

**Implementation**: [UKS.cs:196](../UKS/UKS.cs#L196) - `RelationshipsAreExclusive()`

**Example**:
```
[Animal] --[has.color]--> [Brown]
[Bird] --[is-a]--> [Animal]
[Bluebird] --[is-a]--> [Bird]
[Bluebird] --[has.color]--> [Blue]  (Weight: 1.0, direct)

Query: "What color is Bluebird?"
Result: Blue (direct wins over inherited Brown)
```

### Compound Logic

Complex logical expressions are built through clause chains:

```
"Birds fly IF they have wings UNLESS they are penguins"

[Bird] --[can]--> [Fly]
  ├─ IF: [Bird] --[has]--> [Wings]
  └─ UNLESS: [Bird] --[is-a]--> [Penguin]
```

## Distributed Representation

### Concept

Knowledge is distributed across the graph rather than localized in individual nodes. This provides:
- **Redundancy**: Multiple paths to same conclusion
- **Resilience**: Loss of nodes doesn't eliminate knowledge
- **Associative recall**: Query spreads activation across related concepts

### Example

Knowledge about "Fido":
```
Direct node:
[Fido] V: <image data>

Distributed properties:
[Fido] --[is-a]--> [Dog]
[Fido] --[has.owner]--> [John]
[Fido] --[has.color]--> [Brown]
[Fido] --[likes]--> [Tennis-Ball]

Inherited properties (via Dog):
[Dog] --[has]--> [Leg] (4)
[Dog] --[can]--> [Bark]
[Dog] --[is-a]--> [Animal]

Contextual associations:
[John] --[has.pet]--> [Fido]
[Park] --[contains]--> [Fido] (transient, TTL: 1 hour)
```

Querying "Fido" activates:
- Direct Thing node
- All relationships (outgoing/incoming)
- Inherited attributes from Dog/Animal
- Associated contexts (owner, location)

## Comparison to Neural Networks

| Aspect | Neural Networks (PyTorch) | BrainSim III UKS |
|--------|---------------------------|-------------------|
| **Representation** | Distributed activations in weight matrices | Explicit graph structure |
| **Learning** | Gradient descent on continuous functions | Weight adjustment + structure modification |
| **Interpretability** | Black box (hard to explain) | White box (traceable reasoning) |
| **Symbolic reasoning** | Poor (requires neurosymbolic hybrid) | Native (graph traversal) |
| **Generalization** | Pattern matching, interpolation | Inheritance, transitive properties |
| **Memory** | Compressed in weights | Explicit storage of facts |
| **Reasoning type** | Associative, implicit | Logical, explicit |

## Implementing in PyTorch/PyTorch Geometric

### Strategy 1: Graph Neural Network Representation

Use PyTorch Geometric to learn embeddings while preserving structure:

```python
import torch
from torch_geometric.nn import GCNConv
from torch_geometric.data import Data

# Node features: Thing embeddings
x = torch.tensor([[...], [...], ...])  # One vector per Thing

# Edge index: Relationships
edge_index = torch.tensor([[source_ids...], [target_ids...]])

# Edge features: Relationship types + weights
edge_attr = torch.tensor([[rel_type_embedding, weight], ...])

# Graph data object
data = Data(x=x, edge_index=edge_index, edge_attr=edge_attr)

# GNN layer
class UKS_GNN(torch.nn.Module):
    def __init__(self):
        super().__init__()
        self.conv1 = GCNConv(in_channels, hidden_channels)
        self.conv2 = GCNConv(hidden_channels, out_channels)

    def forward(self, x, edge_index, edge_attr):
        x = self.conv1(x, edge_index, edge_attr)
        x = F.relu(x)
        x = self.conv2(x, edge_index, edge_attr)
        return x
```

### Strategy 2: Hybrid Symbolic-Neural

Maintain explicit graph structure in Python + use neural networks for:
- Thing embeddings (learned representations)
- Relationship weight prediction
- Query relevance scoring

```python
import networkx as nx
import torch

class HybridUKS:
    def __init__(self):
        self.graph = nx.DiGraph()  # Explicit structure
        self.embeddings = {}       # Learned representations
        self.encoder = ThingEncoder()  # Neural encoder

    def add_thing(self, label):
        self.graph.add_node(label)
        self.embeddings[label] = self.encoder.encode(label)

    def add_relationship(self, source, reltype, target, weight=1.0):
        self.graph.add_edge(source, target,
                            reltype=reltype, weight=weight)

    def query(self, source, reltype):
        # Use neural network to rank candidate targets
        candidates = self.graph.successors(source)
        scores = self.score_model(
            self.embeddings[source],
            self.embeddings[reltype],
            [self.embeddings[c] for c in candidates]
        )
        return sorted(zip(candidates, scores),
                      key=lambda x: -x[1])
```

### Strategy 3: Knowledge Graph Embeddings

Use embedding techniques (TransE, RotatE, DistMult):

```python
class TransE(torch.nn.Module):
    def __init__(self, num_entities, num_relations, dim):
        super().__init__()
        self.entity_embeddings = nn.Embedding(num_entities, dim)
        self.relation_embeddings = nn.Embedding(num_relations, dim)

    def forward(self, head, relation, tail):
        # TransE: h + r ≈ t
        h = self.entity_embeddings(head)
        r = self.relation_embeddings(relation)
        t = self.entity_embeddings(tail)
        return -torch.norm(h + r - t, p=2, dim=1)
```

## Key Takeaways for PyTorch Implementation

1. **Preserve explicit structure**: Don't compress everything into embeddings
2. **Hybrid approach**: Use neural networks for learning, graphs for reasoning
3. **Maintain interpretability**: Keep traceable paths for explaining decisions
4. **Implement weight dynamics**: hits/misses tracking for relationship confidence
5. **Support transient connections**: TTL mechanisms for working memory
6. **Enable meta-relationships**: Clauses as higher-order graph structures
7. **Build incrementally**: Start with Thing/Relationship, add inheritance, then clauses

## References

- [Thing.cs](../UKS/Thing.cs) - Node implementation
- [Relationship.cs](../UKS/Relationship.cs) - Edge implementation
- [UKS.cs](../UKS/UKS.cs) - Main knowledge store
- [UKS.Query.cs](../UKS/UKS.Query.cs) - Query engine
- [UKS.Statement.cs](../UKS/UKS.Statement.cs) - Statement API
