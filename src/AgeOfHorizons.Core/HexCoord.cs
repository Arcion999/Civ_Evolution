using System.Collections.Generic;

namespace AgeOfHorizons.Core;

public readonly record struct HexCoord(int Q, int R)
{
    public int S => -Q - R;

    public IEnumerable<HexCoord> Neighbors()
    {
        yield return new HexCoord(Q + 1, R);
        yield return new HexCoord(Q - 1, R);
        yield return new HexCoord(Q, R + 1);
        yield return new HexCoord(Q, R - 1);
        yield return new HexCoord(Q + 1, R - 1);
        yield return new HexCoord(Q - 1, R + 1);
    }

    public int DistanceTo(HexCoord other)
    {
        return (System.Math.Abs(Q - other.Q) + System.Math.Abs(R - other.R) + System.Math.Abs(S - other.S)) / 2;
    }

    public override string ToString() => $"{Q},{R}";

    public static HexCoord Parse(string value)
    {
        var parts = value.Split(',');
        return new HexCoord(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}
