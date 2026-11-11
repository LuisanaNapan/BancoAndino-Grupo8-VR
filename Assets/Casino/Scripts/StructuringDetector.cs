using System.Collections.Generic;

public static class StructuringDetector
{
    // Aggregate by origin, rather than checking each operation in isolation.
    public static bool Detect(IEnumerable<long> amounts, long threshold, out long total, out int count)
    {
        total = 0; count = 0;
        foreach (long amount in amounts)
        {
            if (amount <= 0) continue;
            total += amount; count++;
        }
        return count > 1 && total >= threshold;
    }
}
