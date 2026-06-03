namespace HexWar.Domain.ValueObjects;

public readonly record struct PlayerId(string Value)
{
    public override string ToString() => $"P:{Value}";
}