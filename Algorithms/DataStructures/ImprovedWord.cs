namespace Algorithms.DataStructures;

public readonly struct ImprovedWord : IComparable<ImprovedWord>
{
    private readonly char[] _text;
    private readonly int _wordStartPosition;

    public int WordStartPosition => _wordStartPosition;

    private readonly int _wordLength;
    private readonly int _wordPrefixLength;
    public int Length { get; }

    public ImprovedWord(char[] text, int wordStartPosition, int wordDividerPosition, int wordFinishPosition)
    {
        _text = text;
        _wordStartPosition = wordStartPosition;
        _wordPrefixLength = wordDividerPosition - wordStartPosition;
        _wordLength = wordFinishPosition - wordDividerPosition;
        Length = _wordLength + _wordPrefixLength;
    }
    
    public int CompareTo(ImprovedWord other)
    {
        var wordStartPosition = _wordStartPosition + _wordPrefixLength;
        var otherWordStartPosition = other._wordStartPosition + other._wordPrefixLength;
        
        var wordFinishPosition = _wordStartPosition + Length;
        var otherWordFinishPosition = other._wordStartPosition + other.Length;
        
        for (int i = wordStartPosition, j = otherWordStartPosition; i < wordFinishPosition && j < otherWordFinishPosition; ++i, ++j)
        {
            var symbolsComparingResult = _text[i].CompareTo(other._text[j]);
            if (symbolsComparingResult != 0)
                return symbolsComparingResult;
        }
        
        var wordLengthComparingResult = _wordLength.CompareTo(other._wordLength);
        if (wordLengthComparingResult != 0)
            return wordLengthComparingResult;
        
        var wordBeginPositionComparingResult = _wordPrefixLength.CompareTo(other._wordPrefixLength);
        if (wordBeginPositionComparingResult != 0)
            return wordBeginPositionComparingResult;

        for (int i = _wordStartPosition, j = other._wordStartPosition; i < wordStartPosition; ++i, ++j)
        {
            var symbolsComparingResult = _text[i].CompareTo(other._text[j]);
            if (symbolsComparingResult != 0)
                return symbolsComparingResult;
        }

        return 0;
    }

    public override string ToString()
    {
        return new string(_text, _wordStartPosition, Length);
    }

    public void CopyTo(char[] destination, int destinationIndex)
    {
        Array.Copy(_text, _wordStartPosition, destination, destinationIndex, Length + 1);
    }
}