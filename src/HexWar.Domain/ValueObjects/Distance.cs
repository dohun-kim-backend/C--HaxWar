namespace HexWar.Domain.ValueObjects;

public readonly record struct Distance(int RoundRequired)
{
    // 즉시 도달 가능 여부 확인
    public bool IsInstant => RoundRequired == 0;

    // 비용 계산 메서드
    public Distance AddRounds(int round) => new(RoundRequired + round);

}