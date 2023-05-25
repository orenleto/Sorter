using Algorithms.Comparers;
using Algorithms.DataStructures;

namespace Algorithms;

public static class SimpleSorter
{
    public static char[] Sort(char[] symbols)
    {
        return Sort(symbols, SimpleComparer.Instance);
    }

    public static char[] ImprovedComparerSort(char[] symbols)
    {
        return Sort(symbols, ImprovedComparer.Instance);
    }

    private static char[] Sort(char[] symbols, IComparer<string> comparer)
    {
        var words = new string(symbols).Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
        Array.Sort(words, comparer);

        var result = new char[symbols.Length];
        for (int i = 0, position = 0; i < words.Length; ++i)
        {
            words[i].CopyTo(result.AsSpan(position));
            position += words[i].Length;
            result[position++] = '\n';
        }
        
        return result;
    }

    public static char[] WordSort(char[] symbols)
    {
        var wordsCount = 0;
        for (var i = 0; i < symbols.Length; ++i)
        {
            if (symbols[i] == '\n')
            {
                ++wordsCount;
            }
        }

        var words = new Word[wordsCount];
        for (int i = 0, wordsBeginAt = 0, wordIndex = 0; i < symbols.Length; ++i)
        {
            if (symbols[i] == '\n')
            {
                var word = Word.Create(new ArraySegment<char>(symbols, wordsBeginAt, i - wordsBeginAt));
                words[wordIndex++] = word;
                wordsBeginAt = i + 1;
            }
        }
        
        Array.Sort(words);

        
        var result = new char[symbols.Length];
        for (int i = 0, position = 0; i < wordsCount; ++i)
        {
            words[i].CopyTo(result, position);
            position += words[i].Length;
            result[position++] = '\n';
        }
        return result;
    }

    public static char[] ImprovedWordSort(ArraySegment<char> symbols)
    {
        var wordsCount = 0;
        for (var i = 0; i < symbols.Count; ++i)
        {
            if (symbols[i] == '\n')
            {
                ++wordsCount;
            }
        }

        var words = new ImprovedWord[wordsCount];
        for (int i = 0, wordBeginAt = 0, wordDividerAt = 0, wordIndex = 0; i < symbols.Count; ++i)
        {
            if (symbols[i] == '.' && wordDividerAt <= wordBeginAt)
            {
                wordDividerAt = i;
            }
            else if (symbols[i] == '\n')
            {
                words[wordIndex++] = new ImprovedWord(symbols.Array!, wordBeginAt, wordDividerAt, i);
                wordBeginAt = i + 1;
            }
        }
        
        Array.Sort(words);

        var result = new char[symbols.Count];
        for (int i = 0, position = 0; i < wordsCount; ++i)
        {
            words[i].CopyTo(result, position);
            position += words[i].Length;
            result[position++] = '\n';
        }
        return result;
    }
    
    public static char[] IntegerWordSort(char[] symbols)
    {
        var wordsCount = 0;
        for (var i = 0; i < symbols.Length; ++i)
        {
            if (symbols[i] == '\n')
                wordsCount++;
        }

        var integerWords = new IntegerWord[wordsCount];
        for (int i = 0, wordIndex = 0; i < symbols.Length; ++wordIndex)
        {
            integerWords[wordIndex].PrefixStartPosition = i++;
            while (symbols[i] != '.') i++;
            integerWords[wordIndex].WordStartPosition = ++i;
            while (symbols[i] != '\n') i++;
            integerWords[wordIndex].WordEndPosition = i++;
        }
        
        Array.Sort(integerWords, new WordStartPositionComparer(symbols));

        var result = new char[symbols.Length];
        for (int i = 0, position = 0; i < wordsCount; ++i)
        {
            var wordLength = integerWords[i].WordEndPosition - integerWords[i].PrefixStartPosition;
            Array.Copy(symbols, integerWords[i].PrefixStartPosition, result, position, wordLength);
            position += wordLength;
            result[position++] = '\n';
        }
        return result;
    }
}