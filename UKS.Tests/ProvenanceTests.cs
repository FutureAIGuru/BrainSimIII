using System;
using UKS;
using Xunit;

namespace UKS.Tests;

public class ProvenanceTests
{
    [Fact]
    public void AddStatement_StoresProvenanceOnRelationship()
    {
        var uks = new UKS.UKS(true);

        Relationship relationship = uks.AddStatement("Fido", "is-a", "dog", provenance: "Bill");

        Assert.NotNull(relationship);
        Assert.Contains(relationship.Provenance, t => string.Equals(t.Label, "Bill", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddStatement_AddsAdditionalProvenanceForExistingRelationship()
    {
        var uks = new UKS.UKS(true);

        Relationship initial = uks.AddStatement("Fido", "is-a", "dog", provenance: "Bill");
        Relationship same = uks.AddStatement("Fido", "is-a", "dog", provenance: "Mary");

        Assert.Same(initial, same);
        Assert.Equal(2, initial.Provenance.Count);
        Assert.Contains(initial.Provenance, t => string.Equals(t.Label, "Bill", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(initial.Provenance, t => string.Equals(t.Label, "Mary", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddStatement_DifferentStatementKeepsIndependentProvenance()
    {
        var uks = new UKS.UKS(true);

        Relationship dog = uks.AddStatement("Fido", "is-a", "dog", provenance: "Bill");
        Relationship cat = uks.AddStatement("Fido", "is-a", "cat", provenance: "Mary");

        Assert.Single(dog.Provenance);
        Assert.Single(cat.Provenance);
        Assert.Contains(dog.Provenance, t => string.Equals(t.Label, "Bill", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cat.Provenance, t => string.Equals(t.Label, "Mary", StringComparison.OrdinalIgnoreCase));
    }
}

