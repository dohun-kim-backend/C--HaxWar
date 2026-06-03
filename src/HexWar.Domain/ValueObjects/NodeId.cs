namespace HexWar.Domain.ValueObjects;

// 값 객채로 선언하여 불변성 유지 
public readonly record struct NodeId(int Value)
{
    public override string ToString() => $"N{Value}";
}