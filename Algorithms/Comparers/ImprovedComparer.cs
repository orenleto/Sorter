namespace Algorithms.Comparers;

public class ImprovedComparer : IComparer<string>
{
    public static readonly IComparer<string> Instance = new ImprovedComparer();

    public int Compare(string x, string y)
    {
        var firstWord = x.AsSpan();
        var secondWord = y.AsSpan();
        
        var firstWordPointPosition = firstWord.IndexOf('.');
        var secondWordPointPosition = secondWord.IndexOf('.');

        for (int i = firstWordPointPosition, j = secondWordPointPosition; i < firstWord.Length && j < secondWord.Length; ++i, ++j)
        {
            var symbolComparingResult = firstWord[i].CompareTo(secondWord[j]);
            if (symbolComparingResult != 0)
                return symbolComparingResult;
        }

        var lengthComparingResult = (firstWord.Length - firstWordPointPosition).CompareTo(secondWord.Length - secondWordPointPosition);
        if (lengthComparingResult != 0)
            return lengthComparingResult;

        var numberLengthComparingResult = firstWordPointPosition.CompareTo(secondWordPointPosition);
        if (numberLengthComparingResult != 0)
            return numberLengthComparingResult;

        for (var i = 0; i < firstWordPointPosition; ++i)
        {
            var symbolComparingResult = firstWord[i].CompareTo(secondWord[i]);
            if (symbolComparingResult != 0)
                return symbolComparingResult;
        }
        
        return 0;
    }
}