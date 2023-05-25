using Algorithms.DataStructures;

namespace Algorithms.Comparers;

public class WordStartPositionComparer : IComparer<IntegerWord>
{
    private readonly char[] _symbols;

    public WordStartPositionComparer(char[] symbols)
    {
        _symbols = symbols;
    }
    
    public int Compare(IntegerWord x, IntegerWord y)
    {
        var firstWordLetterPosition = x.WordStartPosition;
        var secondWordLetterPosition = y.WordStartPosition;

        while (_symbols[firstWordLetterPosition] == _symbols[secondWordLetterPosition] && _symbols[firstWordLetterPosition] != '\n')
        {
            firstWordLetterPosition++;
            secondWordLetterPosition++;
        }

        if (_symbols[firstWordLetterPosition] == '\n')
        {
            if (_symbols[secondWordLetterPosition] != '\n')
            {
                return -1;
            }

            var firstWordPrefixLength = x.WordStartPosition - x.PrefixStartPosition;
            var secondWordPrefixLength = y.WordStartPosition - y.PrefixStartPosition;

            var prefixLengthComparisonResult = firstWordPrefixLength.CompareTo(secondWordPrefixLength);
            if (prefixLengthComparisonResult != 0)
            {
                return prefixLengthComparisonResult;
            }

            firstWordLetterPosition = x.PrefixStartPosition;
            secondWordLetterPosition = y.PrefixStartPosition;
            while (_symbols[firstWordLetterPosition] == _symbols[secondWordLetterPosition] && _symbols[firstWordLetterPosition] != '.')
            {
                firstWordLetterPosition++;
                secondWordLetterPosition++;
            }

            if (_symbols[firstWordLetterPosition] == '.')
                return 0;
            return _symbols[firstWordLetterPosition].CompareTo(_symbols[secondWordLetterPosition]);

        }

        if (_symbols[secondWordLetterPosition] == '\n')
        {
            return 1;
        }

        return _symbols[firstWordLetterPosition].CompareTo(_symbols[secondWordLetterPosition]);
    }
}