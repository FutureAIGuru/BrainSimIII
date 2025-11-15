# BrainSim III: Comprehensive Overview

## Introduction

**BrainSim III** is a knowledge representation system designed for Common Sense AI, built around the **Universal Knowledge Store (UKS)**—a custom in-memory graph database. The system represents knowledge as interconnected concepts (Things) linked by semantic relationships, supporting inheritance, conditional logic, and neural-inspired mechanisms.

## What Makes BrainSim III Unique?

### 1. Structural Intelligence, Not Symbolic Programming

Unlike traditional AI that uses hard-coded rules and class definitions, BrainSim III represents all knowledge through **graph structure**:

```
Traditional AI:
class Dog(Animal):
    legs = 4
    def bark(self): return "woof"

BrainSim III:
[Dog] --[is-a]--> [Animal]
      --[has]--> [Leg] (count: 4)
      --[can]--> [Bark]
```

**Advantages**:
- No code changes needed to add knowledge
- Learning happens through structure modification
- Gradual confidence through weighted relationships
- Natural exception handling through conditional clauses

### 2. True Reasoning vs. Pattern Matching

While Large Language Models (LLMs) perform pattern matching on training data, BrainSim III performs **logical reasoning** through graph traversal:

| LLM Approach | BrainSim III Approach |
|--------------|----------------------|
| "Dogs typically have 4 legs" (statistical) | `[Dog] --[has]--> [Leg] (weight: 0.95)` |
| Cannot explain reasoning | Traceable path: Dog → is-a → Animal → has → Leg |
| Hallucinates when uncertain | Returns weighted confidence or "unknown" |
| Fixed at training time | Dynamically updated during operation |

### 3. Hybrid Human-Like Reasoning

The system combines:
- **Deductive reasoning**: Following inheritance chains
- **Inductive reasoning**: Bubbling common attributes to generalizations
- **Abductive reasoning**: Causal explanations via BECAUSE clauses
- **Analogical reasoning**: Finding common patterns across structures

## Core Architecture

### The Graph Foundation

```
Thing (Node)
  │
  ├── Label: Unique identifier
  ├── V: Arbitrary attached data
  ├── Relationships: Outgoing edges (outputs)
  ├── RelationshipsFrom: Incoming edges (inputs)
  ├── Parents/Children: Inheritance hierarchy
  └── UseCount/LastFiredTime: Usage tracking

Relationship (Edge)
  │
  ├── Source: Thing
  ├── RelType: Thing (self-referential!)
  ├── Target: Thing
  ├── Weight: Confidence (0-1)
  ├── Hits/Misses: Usage statistics
  ├── Clauses: Conditional logic (IF/BECAUSE/UNLESS)
  └── TimeToLive: Transience support

Clause (Meta-Edge)
  │
  ├── ClauseType: Thing ("IF", "BECAUSE", etc.)
  └── Clause: Relationship (relationships between relationships!)
```

**Implication**: Things are unambiguous concepts with clear labels, implying cannot work with abstract concepts? And yet unclear concepts (temporal emerging) in the network?

### Self-Referential Type System

**Key Innovation**: Relationship types are themselves Things in the graph.

```
[is-a] --[is-a]--> [RelationshipType]
       --[hasProperty]--> [isTransitive]

[has] --[is-a]--> [RelationshipType]
      --[hasProperty]--> [isInheritable]

[color] --[is-a]--> [Attribute]
        --[hasProperty]--> [isExclusive]
```

This eliminates the need for schema definitions and enables runtime type creation.

**Implication**: Does this cause too much connection overload for these types? 

## Key Concepts and Their Implementations

### 1. Knowledge Representation

**Document**: [01_knowledge_representation.md](01_knowledge_representation.md)

**Core Principles**:
- **Information Singularity**: Each concept exists once, identified by unique label
- **Structure = Facts**: Knowledge emerges from graph structure, not node data
- **Directionality**: Relationships are directed but maintain bidirectional navigation
- **Distributed Representation**: Information spreads across edges, providing resilience

**Key Components**:
- Thing: Universal node abstraction (580 lines, [Thing.cs](../UKS/Thing.cs))
- Relationship: Weighted semantic edge (396 lines, [Relationship.cs](../UKS/Relationship.cs))
- Clause: Meta-relationship for conditional logic
- ThingLabels: Hash table for O(1) lookup

### 2. Inheritance and Recursion

**Document**: [02_inheritance_and_recursion.md](02_inheritance_and_recursion.md)

**Mechanisms**:
- **IS-A relationships**: Build taxonomic hierarchies (DAG structure)
- **Multiple inheritance**: Things can have multiple parents
- **Attribute bubbling**: Common child attributes generalize upward
- **Redundancy removal**: Clean up duplicate inherited properties
- **Dynamic class creation**: Auto-generate intermediate classes for patterns
- **Exception handling**: Override inherited attributes with higher-weight direct ones

**Key Algorithms**:
- `FollowTransitiveRelationships()`: Breadth-first ancestor/descendent traversal
- `RelationshipTree()`: Collect relationships including inherited
- `BubbleChildAttributes()`: Bottom-up generalization (ModuleAttributeBubble)
- `RemoveRedundantAttributes()`: Top-down cleanup (ModuleRemoveRedundancy)

### 3. Conditional and Compound Logic

**Document**: [03_conditional_compound_logic.md](03_conditional_compound_logic.md)

**Clause Types**:
- **IF**: Conditional prerequisite (creates instances to preserve universals)
- **BECAUSE**: Causal explanation
- **UNLESS**: Exception handling
- **WHILE**: Temporal co-occurrence
- **THEN**: Sequential consequence

**Logical Patterns**:
- **Conjunctive (AND)**: Multiple `requires` relationships
- **Disjunctive (OR)**: Multiple `achieved-by` relationships
- **Nested conditionals**: Clauses on clauses (recursive depth)
- **Temporal sequences**: Action chains with TTL
- **Spatial/social context**: Location and relationship-dependent truth
- **Affective causality**: Events cause emotional states

**Temporal Support**:
- `TimeToLive`: Relationship expiration for working memory
- Background timer: 1-second tick removes expired relationships
- Transient relationships: Track temporary associations

### 4. Neural Concepts and Resource Management

**Document**: [04_neural_concepts_resource_management.md](04_neural_concepts_resource_management.md)

**Neural-Inspired Mechanisms**:
- **Sparse coding**: Unique labels ensure singular representation (ThingLabels hash)
- **Lateral inhibition**: Exclusive properties create winner-take-all competition
- **Gating**: IF/UNLESS clauses and weight thresholds control information flow
- **In/Out neurons**: Relationships (outputs) vs RelationshipsFrom (inputs)
- **Cortical columns**: Hierarchical Thing groupings with shared inheritance
- **Instance selection**: Dynamic column allocation with auto-numbering

**Resource Tracking**:
- `useCount`: Access frequency per Thing
- `lastFiredTime`: Last activation timestamp
- `hits/misses`: Relationship usage statistics
- Computed confidence: `Value = Hits / Misses`

**Resilience**:
- Multiple inheritance paths provide redundancy
- Distributed representation survives node loss
- Relationship backup: Same fact represented multiple ways

## Module System: Intelligent Agents

### Agent Architecture

Modules are autonomous agents that run every 100ms, reading and modifying the UKS.

**Base Class**: [ModuleBase.cs](../BrainSimulator/Modules/ModuleBase.cs)

```csharp
public abstract class ModuleBase
{
    public abstract void Fire();        // Called every cycle
    public abstract void Initialize();  // One-time setup

    protected UKS theUKS;              // Shared knowledge store
    public bool isEnabled;             // On/off switch
}
```

### Key Agent Modules

**1. ModuleAttributeBubble** ([source](../BrainSimulator/Modules/Agents/ModuleAttributeBubble.cs))
- **Purpose**: Bottom-up generalization
- **Algorithm**: Count child attributes, bubble if >50% have it
- **Example**: 4 dogs all have tails → bubble to Dog class
- **Timer**: Runs every 10 seconds on background thread

**2. ModuleRemoveRedundancy** ([source](../BrainSimulator/Modules/Agents/ModuleRemoveRedundancy.cs))
- **Purpose**: Clean up inherited duplicates
- **Algorithm**: Remove child relationships that exist in parent with high weight
- **Example**: Dog has tail (0.95) → remove from Fido (redundant)

**3. ModuleClassCreate** ([source](../BrainSimulator/Modules/Agents/ModuleClassCreate.cs))
- **Purpose**: Auto-create intermediate classes
- **Algorithm**: Find attribute clusters, create subclass for each
- **Example**: 3+ red birds → create "Bird.has.color.Red" subclass

**4. ModuleUKS** (Browser/Editor)
- **Purpose**: Visualize and manually edit UKS
- **Features**: Tree view, relationship editing, search

**5. ModuleVision** (Perception)
- **Purpose**: Process images and create mental models
- **Features**: Corner detection, arc recognition, spatial relationships

### Agent Coordination

```
Main Loop (100ms cycle):
├─ ModuleAttributeBubble: Generalizing common patterns
├─ ModuleRemoveRedundancy: Cleaning up redundancies
├─ ModuleClassCreate: Creating intermediate classes
├─ ModuleVision: Processing sensory input
└─ ModuleUKSStatement: Adding new knowledge
```

Agents cooperate through shared UKS but run independently.

## Implementation Details

### Technology Stack

- **Language**: C# 8.0
- **Framework**: .NET 8.0
- **UI**: WPF (Windows Presentation Foundation)
- **Graph Storage**: Custom in-memory (up to 1TB capacity)
- **Serialization**: XML (SThing/SRelationship structures)
- **Python Integration**: Python.Runtime for mixed modules

### Performance Characteristics

**From stress tests and documentation**:
- **Throughput**: 100,000 relationships/second
- **Latency**: Sub-millisecond lookups via hash table
- **Capacity**: Scales to millions of Things
- **Cycle Time**: 100ms per agent execution cycle
- **TTL Cleanup**: 1-second granularity for transient relationships

### File Organization

```
BrainSimIII/
├── UKS/ (Core library)
│   ├── Thing.cs (580 lines)
│   ├── Relationship.cs (396 lines)
│   ├── UKS.cs (628 lines)
│   ├── UKS.Query.cs (513 lines)
│   ├── UKS.Statement.cs (335 lines)
│   ├── UKS.File.cs (539 lines)
│   └── ThingLabels.cs (79 lines)
├── BrainSimulator/ (Main application)
│   ├── Modules/
│   │   ├── ModuleBase.cs
│   │   ├── Agents/ (15+ agent modules)
│   │   └── Vision/ (Perception modules)
│   ├── MainWindow.xaml.cs (353 lines)
│   └── ModuleHandler.cs (284 lines)
└── DocsSource/ (40+ markdown documentation files)
```

## Comparison: BrainSim III vs. Other Approaches

### vs. Traditional Knowledge Graphs (Neo4j, RDF)

| Feature | Neo4j/RDF | BrainSim III |
|---------|-----------|--------------|
| **Schema** | Fixed schema | Self-referential, no schema |
| **Reasoning** | Cypher queries | Transitive traversal + clauses |
| **Learning** | Manual updates | Autonomous agents modify structure |
| **Confidence** | Binary true/false | Weighted relationships (0-1) |
| **Temporal** | Versioning | Built-in TTL mechanism |
| **Inheritance** | Limited | Full multiple inheritance + bubbling |

### vs. Neural Networks (PyTorch)

| Feature | Neural Networks | BrainSim III |
|---------|----------------|--------------|
| **Representation** | Distributed embeddings | Explicit graph |
| **Interpretability** | Black box | Fully traceable |
| **Symbolic reasoning** | Poor | Native |
| **Learning** | Gradient descent | Structure modification + weights |
| **Memory** | Compressed in weights | Explicit storage |
| **Generalization** | Interpolation | Inheritance + transitive properties |

### vs. Large Language Models (GPT, BERT)

| Feature | LLMs | BrainSim III |
|---------|------|--------------|
| **Knowledge source** | Training corpus | Explicit encoding |
| **Reasoning** | Pattern matching | Logical traversal |
| **Explainability** | None (black box) | Full path tracing |
| **Updates** | Retraining required | Real-time modification |
| **Certainty** | Confidence scores (unreliable) | Weight-based (transparent) |
| **Context** | Token window | Graph structure |

### vs. Prolog/Logic Programming

| Feature | Prolog | BrainSim III |
|---------|--------|--------------|
| **Representation** | Horn clauses | Graph relationships |
| **Uncertainty** | Binary | Weighted confidence |
| **Learning** | Manual rule addition | Autonomous agents |
| **Temporal** | External timestamps | Built-in TTL |
| **Exceptions** | Cut operator | Weight + exclusivity |

## Translating to PyTorch/PyTorch Geometric

### Recommended Hybrid Approach

**Preserve the best of both worlds**:

1. **Explicit graph structure** (NetworkX or PyG Data)
   - Maintain Things, Relationships, Clauses as graph
   - Enable traceable reasoning
   - Support symbolic operations

2. **Neural embeddings** (PyTorch)
   - Learn Thing representations
   - Predict relationship weights
   - Score query relevance

3. **GNN for propagation** (PyTorch Geometric)
   - Message passing for attribute inheritance
   - Attention for lateral inhibition
   - Gating networks for conditional logic

### Architecture Sketch

```python
import torch
import networkx as nx
from torch_geometric.nn import GATConv, GCNConv

class HybridUKS:
    def __init__(self):
        # Explicit structure
        self.graph = nx.DiGraph()
        self.thing_labels = {}  # Hash table for O(1) lookup

        # Neural components
        self.thing_encoder = ThingEncoder(dim=128)
        self.relation_encoder = RelationEncoder(dim=64)
        self.gnn = UKS_GNN(in_channels=128, out_channels=128)
        self.clause_evaluator = ClauseGNN(dim=128)

    def add_thing(self, label, parent=None):
        # Explicit structure
        self.graph.add_node(label, embedding=None)
        self.thing_labels[label.lower()] = label

        # Neural embedding
        embedding = self.thing_encoder.encode(label)
        self.graph.nodes[label]['embedding'] = embedding

        if parent:
            self.add_relationship(label, "is-a", parent)

    def add_relationship(self, source, reltype, target, weight=1.0):
        self.graph.add_edge(source, target,
                            reltype=reltype,
                            weight=weight,
                            hits=0, misses=0)

    def query(self, source, reltype, use_neural=True):
        if use_neural:
            # Neural query ranking
            return self.neural_query(source, reltype)
        else:
            # Symbolic traversal
            return self.symbolic_query(source, reltype)

    def symbolic_query(self, source, reltype):
        """Traditional graph traversal"""
        results = []
        # Direct relationships
        for neighbor in self.graph.successors(source):
            edge_data = self.graph[source][neighbor]
            if edge_data.get('reltype') == reltype:
                results.append((neighbor, edge_data['weight']))

        # Inherited relationships
        ancestors = self.get_ancestors(source)
        for ancestor in ancestors:
            for neighbor in self.graph.successors(ancestor):
                edge_data = self.graph[ancestor][neighbor]
                if edge_data.get('reltype') == reltype:
                    results.append((neighbor, edge_data['weight'] * 0.9))

        return sorted(results, key=lambda x: -x[1])

    def neural_query(self, source, reltype):
        """Neural ranking of candidates"""
        # Get candidates via symbolic query
        candidates = self.symbolic_query(source, reltype)

        # Re-rank using GNN
        pyg_data = self.to_pyg_data()
        node_embeddings = self.gnn(pyg_data.x, pyg_data.edge_index)

        source_idx = self.get_node_index(source)
        source_emb = node_embeddings[source_idx]
        reltype_emb = self.relation_encoder.encode(reltype)

        scores = []
        for candidate, weight in candidates:
            candidate_idx = self.get_node_index(candidate)
            candidate_emb = node_embeddings[candidate_idx]

            # Score: cosine similarity of (source + relation) to target
            query_emb = source_emb + reltype_emb
            score = F.cosine_similarity(query_emb, candidate_emb)
            scores.append((candidate, score.item()))

        return sorted(scores, key=lambda x: -x[1])
```

### Key Translation Strategies

**1. Preserve Interpretability**
```python
# Store explicit relationships
self.graph.add_edge(source, target, reltype=reltype)

# But also learn embeddings
self.embeddings[source] = neural_encoder(source)
```

**2. Implement Inheritance in GNN**
```python
class InheritanceGNN(MessagePassing):
    def forward(self, x, edge_index, edge_type):
        # Separate is-a edges from others
        is_a_mask = (edge_type == IS_A_TYPE)
        x_inherited = self.propagate(edge_index[:, is_a_mask], x=x)
        return x + x_inherited  # Combine direct + inherited
```

**3. Neural Clause Evaluation**
```python
class ClauseEvaluator(nn.Module):
    def forward(self, rel1_emb, clause_type_emb, rel2_emb):
        # IF clause: gate rel1 by rel2
        combined = torch.cat([rel1_emb, clause_type_emb, rel2_emb])
        gate = torch.sigmoid(self.gate_network(combined))
        return gate * rel1_emb
```

**4. Temporal Graph with TTL**
```python
class TemporalEdge:
    def __init__(self, source, target, reltype, ttl=None):
        self.timestamp = time.time()
        self.ttl = ttl

    def is_expired(self):
        if self.ttl is None:
            return False
        return time.time() - self.timestamp > self.ttl
```

## Getting Started with BrainSim III Concepts

### Step 1: Understand the Core Abstractions

Start with [01_knowledge_representation.md](01_knowledge_representation.md):
- Thing, Relationship, Clause
- Self-referential type system
- Weight-based confidence

### Step 2: Learn Inheritance Mechanisms

Read [02_inheritance_and_recursion.md](02_inheritance_and_recursion.md):
- IS-A relationships
- Attribute bubbling
- Multiple inheritance
- Dynamic class creation

### Step 3: Master Conditional Logic

Study [03_conditional_compound_logic.md](03_conditional_compound_logic.md):
- IF/BECAUSE/UNLESS clauses
- Temporal sequences
- Nested conditionals
- Working memory via TTL

### Step 4: Explore Neural Concepts

Review [04_neural_concepts_resource_management.md](04_neural_concepts_resource_management.md):
- Sparse coding
- Lateral inhibition
- Gating mechanisms
- Resource tracking

### Step 5: Implement in PyTorch

Use the hybrid approach:
1. Build explicit graph (NetworkX)
2. Add neural embeddings (PyTorch)
3. Implement GNN for inheritance (PyG)
4. Create clause evaluation networks

## Summary: Why BrainSim III Matters

### For Common Sense AI

BrainSim III shows that **structure can encode reasoning** without symbolic programming:
- Facts emerge from graph patterns
- Learning happens through structure modification
- Confidence is gradual, not binary
- Exceptions are natural, not special cases

### For Neural-Symbolic Integration

It demonstrates a path between pure symbolic AI and pure neural networks:
- Explicit structure provides interpretability
- Weights provide uncertainty handling
- Agents enable autonomous learning
- Graph enables both symbolic and subsymbolic reasoning

### For PyTorch/PyG Researchers

The concepts translate to:
- **Graph structure**: Preserve with NetworkX + PyG Data
- **Inheritance**: Message passing along IS-A edges
- **Weights**: Learn with neural networks
- **Clauses**: Hypergraphs or attention mechanisms
- **Temporal**: Dynamic graphs with edge lifetimes
- **Reasoning**: Hybrid symbolic + neural queries

## Conclusion

BrainSim III represents a unique approach to AI that combines:
- **Graph-based knowledge representation** (explicit structure)
- **Weight-based confidence** (uncertainty handling)
- **Autonomous agents** (self-organizing learning)
- **Neural-inspired mechanisms** (sparse coding, gating, inhibition)
- **Logical reasoning** (transitive relationships, conditional clauses)

For translation to PyTorch/PyTorch Geometric, the key is maintaining the **explicit graph structure** while leveraging neural networks for **learning and generalization**. This hybrid approach preserves the interpretability and reasoning capabilities of BrainSim III while gaining the learning power of deep learning.

## Next Steps

1. **Read detailed documentation**:
   - [01_knowledge_representation.md](01_knowledge_representation.md)
   - [02_inheritance_and_recursion.md](02_inheritance_and_recursion.md)
   - [03_conditional_compound_logic.md](03_conditional_compound_logic.md)
   - [04_neural_concepts_resource_management.md](04_neural_concepts_resource_management.md)

2. **Explore the codebase**:
   - [UKS/Thing.cs](../UKS/Thing.cs) - Node implementation
   - [UKS/Relationship.cs](../UKS/Relationship.cs) - Edge implementation
   - [UKS/UKS.cs](../UKS/UKS.cs) - Main knowledge store
   - [Modules/Agents/](../BrainSimulator/Modules/Agents/) - Intelligent agents

3. **Start implementing**:
   - Build hybrid graph (NetworkX + PyTorch)
   - Implement Thing/Relationship classes
   - Add neural embeddings
   - Create GNN for inheritance
   - Implement clause evaluation

## Additional Resources

- **Official Documentation**: [DocsSource/Content/](../DocsSource/Content/)
- **README**: [../README.md](../README.md)
- **FAQ**: [../FAQ.md](../FAQ.md)
