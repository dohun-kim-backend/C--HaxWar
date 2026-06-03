namespace HexWar.Domain.Entities;

using HexWar.Domain.Enums;
using HexWar.Domain.ValueObjects;

public class Edge
{
    public EdgeId Id { get; }
    public NodeId From { get; }
    public NodeId To { get; }
    public Distance Distance { get; }

    // 현재 간선 내 이동 중인 유닛 배치 정보
    // Key : 남은 라운드 수 Value 이동 정보
    // 라운드 별로 빠른 접근이 가능하고, 정렬되어있어 순회하기 용이함으로 해당 컬렉션 객체 선택 
    public SortedList<int, List<TravelingGroup>> TravelingUnits { get; } = new();

    public Edge(NodeId from, NodeId to, Distance distance)
    {
        Id = new EdgeId(from, to);
        From = from;
        To = to;
        Distance = distance;
    }

    // 유닛 이동 시작 시작
    public void StartTravel(PlayerSide side, int unitCount, NodeId destination)
    {

        int remainingRounds = Distance.RoundRequired;

        // 잔여 라운드 정보가 없는 경우 추가
        if (!TravelingUnits.ContainsKey(remainingRounds))
        {
            TravelingUnits.Add(remainingRounds, new List<TravelingGroup>());
        }

        // 잔여 라운드 정보에 등록 
        TravelingUnits[remainingRounds].Add(new TravelingGroup(side, unitCount, destination));
    }

    // 라운드 경과 처리에 따른 
    public List<TravelingGroup> AdvanceRound()
    {
        // 도착 그룹 정보 저장
        var arrived = new List<TravelingGroup>();

        // 이전 key 정보 정리
        var KeyToRemove = new List<int>();

        foreach (var kvp in TravelingUnits)
        {
            // 변경된 라운드 정보 복사
            int newRemainingRounds = kvp.Key - 1;

            // 0 라운드가 맞는 경우
            if (newRemainingRounds <= 0)
            {
                arrived.AddRange(kvp.Value);
                KeyToRemove.Add(kvp.Key);
            }
            else
            {
                TravelingUnits[newRemainingRounds] = kvp.Value;
                KeyToRemove.Add(kvp.Key);
            }
        }

        // 라운드 연산 완료 이후 
        foreach (var key in KeyToRemove)
        {
            TravelingUnits.Remove(key);
        }

        return arrived;
    }

    // 조우 여부 확인
    public bool HasEncounter(int roundRemaining, out List<TravelingGroup>? groups)
    {
        // 현재 라운드 정보가 존재하는 경우
        // 1. 도착 지점이 동일한 유닛이 2개 이상인 경우
        if (TravelingUnits.TryGetValue(roundRemaining, out groups) && groups.Count > 1)
        {
            // Side 기준으로 선택하여 중복 제거된 리스트 생성
            // 1 이상이라면 다른 Side 존재 -> true
            // 1 이하라면 같은 Side 존재 -> false
            var sides = groups.Select(g => g.Side).Distinct();
            return sides.Count() > 1;
        }
        groups = null;
        return false;
    }

}