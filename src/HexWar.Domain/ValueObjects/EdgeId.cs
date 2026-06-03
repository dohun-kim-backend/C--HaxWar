namespace HexWar.Domain.ValueObjects;

public readonly record struct EdgeId(NodeId From, NodeId To)
{
    // 무방향 그래프이므로 순서를 정해두어 중복 생성을 방지한다
    // 생성 예시 : 1-2 , 2-1 -> 1-2 로 저장
    public string Key => From.Value < To.Value
        ? $"{From.Value}-{To.Value}"
        : $"{To.Value}-{From.Value}";

    public override string ToString() => Key;
}