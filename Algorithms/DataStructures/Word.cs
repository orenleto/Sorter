namespace Algorithms.DataStructures;

public readonly struct Word : IComparable<Word>
{
    private readonly ArraySegment<char> _segment;
    private readonly int _wordBeginPosition;
    private readonly int _wordLength;

    private Word(ArraySegment<char> segment, int dividerPosition)
    {
        _segment = segment;
        _wordBeginPosition = dividerPosition;
        _wordLength = _segment.Count - _wordBeginPosition;
    }

    public static Word Create(ArraySegment<char> wordSymbols)
    {
        for (var i = 0; i < wordSymbols.Count; ++i)
        {
            if (wordSymbols[i] == '.')
            {
                return new Word(wordSymbols, i + 1);
            }
        }

        throw new FormatException($"Not found required divider symbol at {nameof(wordSymbols)}");
    }

    public int Length => _segment.Count;
    
    public void CopyTo(char[] destination, int destinationIndex)
    {
        _segment.CopyTo(destination, destinationIndex);
    }
    
    public override string ToString()
    {
        return new string(_segment);
    }

    public int CompareTo(Word other)
    {
        for (int i = _wordBeginPosition, j = other._wordBeginPosition; i < _segment.Count && j < other._segment.Count(); ++i, ++j)
        {
            var symbolsComparingResult = _segment[i].CompareTo(other._segment[j]);
            if (symbolsComparingResult != 0)
                return symbolsComparingResult;
        }
            
        var wordLengthComparingResult = _wordLength.CompareTo(other._wordLength);
        if (wordLengthComparingResult != 0)
            return wordLengthComparingResult;

        var wordBeginPositionComparingResult = _wordBeginPosition.CompareTo(other._wordBeginPosition);
        if (wordBeginPositionComparingResult != 0)
            return wordBeginPositionComparingResult;
            
        for (var i = 0; i < _wordBeginPosition; ++i)
        {
            var symbolsComparingResult = _segment[i].CompareTo(other._segment[i]);
            if (symbolsComparingResult != 0)
                return symbolsComparingResult;
        }

        return 0;
    }
}