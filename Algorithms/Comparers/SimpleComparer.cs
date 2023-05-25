namespace Algorithms.Comparers;

public class SimpleComparer : IComparer<string>
{
    public static readonly IComparer<string> Instance = new SimpleComparer();

    public int Compare(string x, string y)
    {
        var firstWordPointPosition = x.IndexOf('.');
        var secondWordPointPosition = y.IndexOf('.');
        
        var firstWord = x.AsSpan(firstWordPointPosition + 1);
        var secondWord = y.AsSpan(secondWordPointPosition + 1);
        
        for (var i = 0; i < Math.Min(firstWord.Length, secondWord.Length); ++i)
        {
            var symbolComparingResult = firstWord[i].CompareTo(secondWord[i]);
            if (symbolComparingResult != 0)
                return symbolComparingResult;
        }

        var lengthComparingResult = firstWord.Length.CompareTo(secondWord.Length);
        if (lengthComparingResult != 0)
            return lengthComparingResult;

        var firstIndex = 0;
        for (var i = 0; i < firstWordPointPosition; ++i)
            firstIndex = firstIndex * 10 + x[i] - '0';
        var secondIndex = 0;
        for (var i = 0; i < secondWordPointPosition; ++i)
            secondIndex = secondIndex * 10 + y[i] - '0';

        return firstIndex.CompareTo(secondIndex);
    }
}