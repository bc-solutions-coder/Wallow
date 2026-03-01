using Foundry.Shared.Kernel.Domain;

namespace Foundry.Configuration.Domain.ValueObjects;

public sealed class VariantWeight : ValueObject
{
    public string Name { get; }
    public int Weight { get; }

    public VariantWeight(string name, int weight)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Variant name is required.", nameof(name));
        }

        if (weight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight cannot be negative.");
        }

        Name = name;
        Weight = weight;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Weight;
    }
}
