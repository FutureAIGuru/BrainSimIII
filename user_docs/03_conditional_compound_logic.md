# Conditional and Compound Logic in BrainSim III

## Overview

BrainSim III implements complex logical reasoning through **structural relationships** rather than symbolic programming. The system supports conjunctive logic, conditional statements, temporal sequences, causal relationships, and nested logic—all represented as graph patterns with clauses.

## Clause System: Relationships Between Relationships

### Core Concept

A **Clause** is a meta-relationship that connects two Relationships, enabling conditional and compound logic:

```
[Relationship A] --[Clause Type]--> [Relationship B]
```

This creates a second-order graph where edges themselves have edges.

### Implementation

**File**: [Relationship.cs:50-65](../UKS/Relationship.cs#L50-L65)

```csharp
public class Clause
{
    public Thing clauseType;          // "IF", "BECAUSE", "UNLESS", etc.
    public Relationship clause;       // The target relationship
}

public class Relationship
{
    // ... source, reltype, target ...

    private List<Clause> clauses = new();
    public List<Clause> Clauses { get => clauses; set => clauses = value; }

    // Relationships that have this as a clause target
    public List<Relationship> clausesFrom = new();
}
```

### Clause Types

The system defines several built-in clause types during initialization:

**File**: UKS initialization creates:
```
ClauseType (Thing)
├── IF        (conditional prerequisite)
├── BECAUSE   (causal explanation)
├── UNLESS    (exception handling)
├── WHILE     (temporal co-occurrence)
├── THEN      (sequential consequence)
└── ... (extensible)
```

## Conditional Logic (IF Clauses)

### Concept

IF clauses make statements conditional. A relationship is only valid when its IF clause is satisfied.

```
Statement: R1
Condition: R2
Result: R1 IF R2
```

### Implementation

**File**: [UKS.cs:586-627](../UKS/UKS.cs#L586-L627)

```csharp
public Relationship AddClause(
    Relationship r1,      // Main statement
    Thing clauseType,     // "IF", "BECAUSE", etc.
    Thing source,         // Clause relationship components
    Thing relType,
    Thing target)
{
    // Create the conditional relationship (not a statement itself)
    Relationship rTemp = new() {
        source = source,
        reltype = relType,
        target = target,
        Weight = .9f,
        isStatement = false  // This is a condition, not a fact
    };

    // Check if this clause already exists
    foreach (Thing t in r1.source.Children)
    {
        foreach (Relationship r in t.Relationships)
        {
            Clause? c = r.Clauses.FindFirst(
                x => x?.clauseType == clauseType && x?.clause == rTemp);
            if (c != null) return r;  // Already exists
        }
    }

    // Determine if a new instance is needed
    bool newInstanceNeeded = false;
    if (clauseType.Label.ToLower() == "if" && r1.isStatement)
        newInstanceNeeded = true;
    if (clauseType.Label.ToLower() == "because")
        r1.isStatement = true;

    Relationship rRoot = r1;

    if (newInstanceNeeded)
    {
        // Create a new instance of the source
        Thing newInstance = GetOrAddThing(r1.source.Label + "*", r1.source);

        // Move the existing relationship to the instance level
        r1.source.RemoveRelationship(r1);
        rRoot = newInstance.AddRelationship(r1.target, r1.relType, false);
        AddStatement(rRoot.source.Label, "hasProperty", "isInstance");
    }

    // Make rTemp into a real relationship
    rTemp = rTemp.source.AddRelationship(rTemp.target, rTemp.relType, r1.isStatement);

    // Add the clause
    rRoot.AddClause(clauseType, rTemp);
    rRoot.isStatement = r1.isStatement;
    return rRoot;
}
```

### Example: Birds Fly IF They Have Wings

**Statement API**:
```csharp
// Base statement
Relationship r1 = uks.AddStatement("Bird", "can", "Fly");

// Add condition
uks.AddClause(r1, "IF", "Bird", "has", "Wings");
```

**Graph Structure**:
```
[Bird] --[can]--> [Fly]
   └── IF: [Bird] --[has]--> [Wings]
```

**Reasoning Process**:
```
Query: "Can a Bird fly?"
1. Find relationship: [Bird] --[can]--> [Fly]
2. Check clauses: IF [Bird] --[has]--> [Wings]
3. Verify condition: Does Bird have Wings?
4. If YES → Result: Bird can fly
   If NO → Result: Uncertain or No
```

## Instance Creation for Conditionals

### Concept

When adding an IF clause to a general statement, the system creates an **instance** to avoid modifying the universal relationship.

### Example: Penguins Can't Fly

```
Initial: [Bird] --[can]--> [Fly]

Adding: [Bird] --[can]--> [Fly] IF [Bird] --[is-not]--> [Penguin]

Results in:
[Bird]
  ├── [Bird0] (instance)
  │   --[can]--> [Fly]  (isStatement: false)
  │       └── IF: [Bird0] --[is-not]--> [Penguin]
  │   --[hasProperty]--> [isInstance]
  └── (Original Bird unchanged)
```

This preserves the general concept while creating a conditional variant.

## Causal Logic (BECAUSE Clauses)

### Concept

BECAUSE clauses explain why a relationship exists, representing causal connections.

```
Effect: R1
Cause: R2
Result: R1 BECAUSE R2
```

### Example: Motion Causes Blur

```
[Photo] --[has.property]--> [Blurry]
    └── BECAUSE: [Camera] --[had]--> [Motion]
```

**Code**:
```csharp
Relationship effect = uks.AddStatement("Photo", "has.property", "Blurry");
uks.AddClause(effect, "BECAUSE", "Camera", "had", "Motion");
```

**Reasoning**:
```
Query: "Why is the photo blurry?"
1. Find: [Photo] --[has.property]--> [Blurry]
2. Follow BECAUSE clause: [Camera] --[had]--> [Motion]
3. Result: "Because the camera had motion"
```

## Temporal Logic

### 1. Sequential Relationships (THEN)

**Concept**: One event follows another in time.

```
[Event A] --[leads-to]--> [Event B]
    └── THEN: [Event B] --[occurs-after]--> [Event A]
```

**Example: Action Sequence**
```
[Press-Button] --[causes]--> [Door-Opens]
    └── THEN: [Door-Opens] --[occurs-after]--> [Press-Button]
```

### 2. Co-occurrence (WHILE)

**Concept**: Two events happen simultaneously.

```
[Walk] --[simultaneous]--> [Chew-Gum]
    └── WHILE: [Person] --[does]--> [Chew-Gum]
```

### 3. Temporal TTL Implementation

**File**: [Relationship.cs:180-193](../UKS/Relationship.cs#L180-L193)

```csharp
private TimeSpan timeToLive = TimeSpan.MaxValue;

public TimeSpan TimeToLive
{
    get { return timeToLive; }
    set
    {
        timeToLive = value;
        if (timeToLive != TimeSpan.MaxValue)
            AddToTransientList();  // Register for cleanup
    }
}

private void AddToTransientList()
{
    if (!UKS.transientRelationships.Contains(this))
        UKS.transientRelationships.Add(this);
}
```

**Cleanup Timer** [UKS.cs:46-78](../UKS/UKS.cs#L46-L78):
```csharp
static public List<Relationship> transientRelationships = new List<Relationship>();
static Timer stateTimer;

public UKS(bool clear = false)
{
    // Start timer: check every 1 second
    stateTimer = new Timer(RemoveExpiredRelationships, autoEvent, 0, 1000);
}

private void RemoveExpiredRelationships(Object stateInfo)
{
    if (isRunning) return;
    isRunning = true;

    for (int i = transientRelationships.Count - 1; i >= 0; i--)
    {
        Relationship r = transientRelationships[i];

        // Check if expired
        if (r.TimeToLive != TimeSpan.MaxValue &&
            r.LastUsed + r.TimeToLive < DateTime.Now)
        {
            r.source.RemoveRelationship(r);

            // Clean up orphaned Things
            if (r.reltype.Label == "has-child" && r.target?.Parents.Count == 0)
            {
                r.target.AddParent(ThingLabels.GetThing("unknownObject"));
            }

            transientRelationships.Remove(r);
        }
    }

    isRunning = false;
}
```

**Example: Working Memory**
```csharp
// Robot sees object
Relationship sees = uks.AddStatement("Robot", "sees", "Ball");
sees.TimeToLive = TimeSpan.FromSeconds(30);  // Forget after 30 seconds

// After 30 seconds with no access: relationship auto-deleted
```

## Compound Logic Patterns

### 1. Conjunctive Logic (AND)

**Concept**: Multiple conditions must all be true.

**Graph Pattern**:
```
[Result] --[requires]--> [Condition1]
         --[requires]--> [Condition2]
         --[requires]--> [Condition3]
```

**Example: Recipe Requirements**
```
[Cake] --[requires]--> [Flour]
       --[requires]--> [Sugar]
       --[requires]--> [Eggs]
       --[requires]--> [Heat]
```

**Query Implementation**:
```csharp
public bool AllConditionsMet(Thing result, Thing requiresRelType)
{
    List<Relationship> requirements =
        result.Relationships.FindAll(x => x.relType == requiresRelType);

    foreach (Relationship req in requirements)
    {
        if (!VerifyCondition(req.target))
            return false;  // Missing a requirement
    }
    return true;  // All requirements met
}
```

### 2. Disjunctive Logic (OR)

**Concept**: Any one of multiple conditions can be true.

**Graph Pattern**:
```
[Goal] --[achieved-by]--> [Method1]
       --[achieved-by]--> [Method2]
       --[achieved-by]--> [Method3]
```

**Example: Multiple Paths**
```
[Open-Door]
  --[achieved-by]--> [Use-Key]
  --[achieved-by]--> [Use-Code]
  --[achieved-by]--> [Biometric-Scan]
```

**Query**: Any successful path returns true.

### 3. Nested Conditionals

**Concept**: Clauses on clauses create multi-level logic.

**Structure**:
```
R1 IF R2
R2 IF R3
R3 IF R4
```

**Example: Permission Hierarchy**
```
[User] --[can]--> [Delete-File]
    └── IF: [User] --[has]--> [Admin-Role]
        └── IF: [User] --[passed]--> [Security-Check]
            └── IF: [User] --[has]--> [Valid-Token]
```

**Recursive Clause Verification**:
```csharp
public bool VerifyClauseChain(Relationship r, HashSet<Relationship> visited)
{
    if (visited.Contains(r)) return false;  // Circular dependency
    visited.Add(r);

    // No clauses → unconditional truth
    if (r.Clauses.Count == 0) return true;

    foreach (Clause c in r.Clauses)
    {
        if (c.clauseType.Label == "IF")
        {
            // Recursively verify IF clause
            if (!VerifyClauseChain(c.clause, visited))
                return false;
        }
    }
    return true;
}
```

## Exception Handling (UNLESS)

### Concept

UNLESS clauses create exceptions that override general rules.

```
General Rule: R1
Exception: R2
Result: R1 UNLESS R2
```

### Example: Typical vs. Exceptional Cases

```
[Bird] --[can]--> [Fly] (Weight: 0.9)
    └── UNLESS: [Bird] --[is-a]--> [Penguin]

[Bird] --[can]--> [Fly] (Weight: 0.9)
    └── UNLESS: [Bird] --[is-a]--> [Ostrich]
```

**Query Logic**:
```csharp
public bool CheckWithExceptions(Thing subject, Thing capability)
{
    // Find general capability
    Relationship r = subject.Relationships.FindFirst(
        x => x.relType == "can" && x.target == capability);

    if (r == null) return false;

    // Check UNLESS clauses
    foreach (Clause c in r.Clauses)
    {
        if (c.clauseType.Label == "UNLESS")
        {
            // If exception applies, return false
            if (VerifyRelationship(c.clause))
                return false;
        }
    }

    return true;  // General rule applies
}
```

## Emotional/Affective Causality

### Concept

Events cause emotional states, represented as causal relationships.

**Pattern**:
```
[Event] --[causes]--> [Emotion]
    └── BECAUSE: [Event] --[has.property]--> [Unpleasant]
```

### Example: Emotional Responses

```
[Lost-Job] --[causes]--> [Sad]
    └── BECAUSE: [Lost-Job] --[has.property]--> [Negative]

[Won-Lottery] --[causes]--> [Happy]
    └── BECAUSE: [Won-Lottery] --[has.property]--> [Positive]
```

**Reasoning**:
```
Query: "Why does losing a job cause sadness?"
1. Find: [Lost-Job] --[causes]--> [Sad]
2. Follow BECAUSE: [Lost-Job] --[has.property]--> [Negative]
3. Generalize: Negative events cause sad emotions
```

## Spatial and Social Context

### 1. Spatial Co-occurrence

**Concept**: Things that exist in the same space are associated.

```
[Kitchen] --[contains]--> [Stove]
          --[contains]--> [Refrigerator]
          --[contains]--> [Sink]

[Stove] --[co-occurs-with]--> [Refrigerator]
    └── BECAUSE: [Common-Location] --[is]--> [Kitchen]
```

### 2. Social Context

**Concept**: Relationships depend on social setting.

```
[Person] --[calls]--> [Doctor]
    └── IF: [Person] --[has]--> [Illness]

[Person] --[calls]--> [Lawyer]
    └── IF: [Person] --[has]--> [Legal-Problem]
```

## Sensor-Monitor Mapping

### Concept

Sensor readings trigger conditional responses.

**Pattern**:
```
[Sensor-Reading] --[triggers]--> [Action]
    └── IF: [Value] --[exceeds]--> [Threshold]
```

### Example: Temperature Control

```
[Temperature-Sensor] --[reads]--> [Temperature-Value]

[Temperature-Value] --[triggers]--> [Turn-On-AC]
    └── IF: [Temperature-Value] --[exceeds]--> [75°F]

[Temperature-Value] --[triggers]--> [Turn-On-Heat]
    └── IF: [Temperature-Value] --[below]--> [65°F]
```

**Implementation**:
```csharp
public void MonitorSensor(Thing sensor, Thing value)
{
    // Get all triggered actions
    List<Relationship> triggers = value.Relationships.FindAll(
        x => x.relType.Label == "triggers");

    foreach (Relationship trigger in triggers)
    {
        // Check IF clauses
        bool conditionsMet = true;
        foreach (Clause c in trigger.Clauses)
        {
            if (c.clauseType.Label == "IF")
            {
                conditionsMet &= EvaluateCondition(c.clause);
            }
        }

        if (conditionsMet)
        {
            ExecuteAction(trigger.target);
        }
    }
}
```

## Routine Sequencing (Animal Behavior)

### Concept

Actions follow predictable sequences, modeled as chained relationships.

```
[Wake-Up] --[then]--> [Eat-Breakfast]
[Eat-Breakfast] --[then]--> [Brush-Teeth]
[Brush-Teeth] --[then]--> [Get-Dressed]
```

### Implementation: Sequential Chains

```csharp
public List<Thing> GetActionSequence(Thing startAction)
{
    List<Thing> sequence = new() { startAction };
    Thing current = startAction;

    while (true)
    {
        // Find "then" relationship
        Relationship next = current.Relationships.FindFirst(
            x => x.relType.Label == "then");

        if (next == null) break;  // End of sequence

        sequence.Add(next.target);
        current = next.target;
    }

    return sequence;
}
```

### Example: Morning Routine

```
[Person] --[performs]--> [Morning-Routine]

[Morning-Routine] --[starts-with]--> [Wake-Up]

[Wake-Up] --[then]--> [Shower]
[Shower] --[then]--> [Dress]
[Dress] --[then]--> [Eat-Breakfast]
[Eat-Breakfast] --[then]--> [Leave-Home]
```

## Parallel Actions and Correlation

### Concept

Multiple actions occur simultaneously, possibly correlated.

```
[Event-A] --[occurs-simultaneously]--> [Event-B]
          --[possibly-causes]--> [Event-C]
```

### Example: Correlated Behaviors

```
[Rain] --[correlates-with]--> [Umbrella-Use]
       --[correlates-with]--> [Traffic-Increase]
```
w
**Correlation Weight**:
```csharp
// Track co-occurrence frequency
Relationship r = uks.AddStatement("Rain", "correlates-with", "Umbrella-Use");
r.Weight = 0.85f;  // 85% correlation observed
```

## Unlimited Compounding

### Concept

Clauses can be chained indefinitely to represent arbitrarily complex logic.

```
R1 IF R2 UNLESS R3 BECAUSE R4 IF R5 ...
```

### Example: Complex Medical Diagnosis

```
[Patient] --[has]--> [Disease-X]
    └── IF: [Patient] --[has]--> [Symptom-A]
        └── IF: [Patient] --[has]--> [Symptom-B]
            └── UNLESS: [Patient] --[has]--> [Symptom-C]
                └── BECAUSE: [Symptom-C] --[indicates]--> [Disease-Y]
                    └── IF: [Test-Result] --[is]--> [Positive]
```

## Implementing in PyTorch/PyTorch Geometric

### Strategy 1: Hypergraph Representation

Use hyperedges to represent clauses (edges on edges):

```python
import torch
from torch_geometric.data import Data

class ClauseData:
    def __init__(self):
        # Node features
        self.node_features = []  # Thing embeddings

        # Edge features (Relationships)
        self.edge_index = []  # [source, target] pairs
        self.edge_features = []  # Relationship type embeddings

        # Hyperedge features (Clauses)
        self.clause_index = []  # [rel1_id, rel2_id] pairs
        self.clause_types = []  # "IF", "BECAUSE", etc.

    def add_clause(self, rel1_id, clause_type, rel2_id):
        self.clause_index.append([rel1_id, rel2_id])
        self.clause_types.append(clause_type)
```

### Strategy 2: Nested GNN for Multi-Order Relationships

```python
class ClauseGNN(torch.nn.Module):
    def __init__(self, node_dim, edge_dim, clause_dim):
        super().__init__()
        self.node_encoder = GCNConv(node_dim, node_dim)
        self.edge_encoder = nn.Linear(edge_dim * 3, edge_dim)  # src+rel+tgt
        self.clause_encoder = nn.Linear(edge_dim * 2, clause_dim)

    def forward(self, x, edge_index, edge_attr, clause_index, clause_type):
        # 1. Node-level propagation
        x = self.node_encoder(x, edge_index)

        # 2. Edge-level embedding (relationship representation)
        src_emb = x[edge_index[0]]
        tgt_emb = x[edge_index[1]]
        edge_emb = self.edge_encoder(
            torch.cat([src_emb, edge_attr, tgt_emb], dim=-1))

        # 3. Clause-level embedding (meta-relationship)
        if clause_index.size(0) > 0:
            rel1_emb = edge_emb[clause_index[:, 0]]
            rel2_emb = edge_emb[clause_index[:, 1]]
            clause_emb = self.clause_encoder(
                torch.cat([rel1_emb, rel2_emb], dim=-1))
            return x, edge_emb, clause_emb

        return x, edge_emb, None
```

### Strategy 3: Rule-Based Clause Evaluation

```python
class ClauseEvaluator:
    def __init__(self, knowledge_graph):
        self.kg = knowledge_graph

    def evaluate(self, relationship, context):
        """
        Evaluate if a relationship holds given context
        """
        # Check IF clauses
        if_clauses = self.kg.get_clauses(relationship, clause_type="IF")
        for clause in if_clauses:
            if not self.verify_relationship(clause, context):
                return False  # Condition not met

        # Check UNLESS clauses
        unless_clauses = self.kg.get_clauses(relationship, clause_type="UNLESS")
        for clause in unless_clauses:
            if self.verify_relationship(clause, context):
                return False  # Exception applies

        return True  # All conditions satisfied

    def verify_relationship(self, relationship, context):
        """
        Recursively verify relationship with clause chain
        """
        # Base case: check if relationship exists in context
        if relationship in context:
            return True

        # Recursive case: check clauses
        return self.evaluate(relationship, context)
```

### Strategy 4: Temporal Graph Network

```python
class TemporalKG:
    def __init__(self):
        self.static_graph = nx.DiGraph()
        self.temporal_edges = {}  # edge -> (timestamp, ttl)

    def add_temporal_relationship(self, source, reltype, target, ttl_seconds):
        edge = (source, reltype, target)
        timestamp = time.time()
        self.temporal_edges[edge] = (timestamp, ttl_seconds)
        self.static_graph.add_edge(source, target, reltype=reltype)

    def cleanup_expired(self):
        current_time = time.time()
        expired = []

        for edge, (timestamp, ttl) in self.temporal_edges.items():
            if current_time - timestamp > ttl:
                expired.append(edge)

        for edge in expired:
            source, reltype, target = edge
            self.static_graph.remove_edge(source, target)
            del self.temporal_edges[edge]
```

## Key Takeaways

1. **Clauses as meta-edges**: Relationships between relationships enable conditional logic
2. **IF clauses**: Create instances to avoid modifying universal statements
3. **BECAUSE clauses**: Represent causal explanations
4. **Temporal logic**: TTL mechanism for working memory
5. **Compound patterns**: Multiple clauses create AND/OR/nested logic
6. **Exception handling**: UNLESS clauses override general rules
7. **Recursive evaluation**: Clause chains require depth-first verification
8. **Context-dependent reasoning**: Social, spatial, emotional context affects truth

## References

- [Relationship.cs](../UKS/Relationship.cs) - Clause implementation
- [UKS.cs](../UKS/UKS.cs) - AddClause method, TTL cleanup
- [UKS.Statement.cs](../UKS/UKS.Statement.cs) - Statement API with clause support
