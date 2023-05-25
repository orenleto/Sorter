using System.Globalization;
using System.Text;
using Algorithms.DataStructures;

namespace Algorithms;

public static class ParallelSorter
{
    public static char[] ImprovedSort(char[] symbols, int segmentCount = 16)
    {
        var segmentSymbolsLength = (symbols.Length + segmentCount - 1) / segmentCount;

        var segmentStartPositions = new int[segmentCount];
        var segmentEndPositions = new int[segmentCount];
        var segmentLength = new int[segmentCount];

        Parallel.For(0, segmentCount, index =>
        {
            var firstSymbolAt = index * segmentSymbolsLength;
            var lastSymbolAt = Math.Min(firstSymbolAt + segmentSymbolsLength, symbols.Length);
            var wordsCounter = 0;
            for (var i = firstSymbolAt; i < lastSymbolAt; ++i)
                if (symbols[i] == '\n')
                {
                    wordsCounter++;
                    segmentEndPositions[index] = i;
                }

            segmentLength[index] = wordsCounter;
        });

        segmentStartPositions[0] = 0;
        for (var i = 1; i < segmentCount; ++i)
            segmentStartPositions[i] = segmentLength[i - 1] + segmentStartPositions[i - 1];
        
        var wordsCount = segmentStartPositions[^1] + segmentLength[^1];
        var words = new ImprovedWord[wordsCount];

        Parallel.For(0, segmentCount, index =>
        {
            int segmentStartAt = 0,
                segmentEndAt = segmentEndPositions[index],
                wordStartIndex = 0,
                wordIndex = 0;
            if (index > 0)
            {
                segmentStartAt = segmentEndPositions[index - 1] + 1;
                wordStartIndex = segmentStartPositions[index];
                wordIndex = segmentStartPositions[index];
            }

            for (int currentSymbolIndex = segmentStartAt, currentWordStartPosition = segmentStartAt; currentSymbolIndex < segmentEndAt;)
            {
                while (symbols[currentSymbolIndex] != '.') currentSymbolIndex++;
                var dividerPosition = currentSymbolIndex++;
                while (symbols[currentSymbolIndex] != '\n') currentSymbolIndex++;
                words[wordIndex++] = new ImprovedWord(symbols, currentWordStartPosition, dividerPosition, currentSymbolIndex++);
                currentWordStartPosition = currentSymbolIndex;
            }
            
            Array.Sort(words, wordStartIndex, segmentLength[index]);
        });
        
        var sortedWords = new ImprovedWord[wordsCount];

        var heap = new Heap<(ImprovedWord, int)>(segmentCount);
        for (var i = 0; i < segmentCount; ++i)
            heap.Add((words[segmentStartPositions[i]++], i));

        int index = 0;
        while (index < wordsCount)
        {
            var (word, segmentIndex) = heap.PopTop();
            sortedWords[index++] = word;
            if (--segmentLength[segmentIndex] > 0)
                heap.Add((words[segmentStartPositions[segmentIndex]++], segmentIndex));
        }

        var result = new char[symbols.Length];
        for (int i = 0, position = 0; i < wordsCount; ++i)
        {
            sortedWords[i].CopyTo(result, position);
            position += sortedWords[i].Length;
            result[position++] = '\n';
        }

        return result;
    }

    public static char[] Sort(char[] symbols, int segmentCount = 16)
    {
        var segmentSymbolsLength = (symbols.Length + segmentCount - 1) / segmentCount;

        var segmentStartPositions = new int[segmentCount];
        var segmentEndPositions = new int[segmentCount];
        var segmentLength = new int[segmentCount];

        Parallel.For(0, segmentCount, index =>
        {
            var firstSymbolAt = index * segmentSymbolsLength;
            var lastSymbolAt = Math.Min(firstSymbolAt + segmentSymbolsLength, symbols.Length);
            var wordsCounter = 0;
            for (var i = firstSymbolAt; i < lastSymbolAt; ++i)
                if (symbols[i] == '\n')
                {
                    wordsCounter++;
                    segmentEndPositions[index] = i;
                }

            segmentLength[index] = wordsCounter;
        });
        
        segmentStartPositions[0] = 0;
        for (var i = 1; i < segmentCount; ++i)
            segmentStartPositions[i] = segmentLength[i - 1] + segmentStartPositions[i - 1];
        
        var wordsCount = segmentStartPositions[^1] + segmentLength[^1];
        var words = new ImprovedWord[wordsCount];

        Parallel.For(0, segmentCount, index =>
        {
            int segmentStartAt = 0,
                segmentEndAt = segmentEndPositions[index],
                wordStartIndex = 0,
                wordIndex = 0;
            if (index > 0)
            {
                segmentStartAt = segmentEndPositions[index - 1] + 1;
                wordStartIndex = segmentStartPositions[index];
                wordIndex = segmentStartPositions[index];
            }

            for (int currentSymbolIndex = segmentStartAt, currentWordStartPosition = segmentStartAt; currentSymbolIndex < segmentEndAt;)
            {
                while (symbols[currentSymbolIndex] != '.') currentSymbolIndex++;
                var dividerPosition = currentSymbolIndex++;
                while (symbols[currentSymbolIndex] != '\n') currentSymbolIndex++;
                words[wordIndex++] = new ImprovedWord(symbols, currentWordStartPosition, dividerPosition, currentSymbolIndex++);
                currentWordStartPosition = currentSymbolIndex;
            }
            
            Array.Sort(words, wordStartIndex, segmentLength[index]);
        });

        var mergeResult = new ImprovedWord[wordsCount];
        
        Array.Copy(words, mergeResult, wordsCount);
        int step = 1, 
            shift = 2, 
            remainedUnsortedSegments = segmentCount;
        
        while (remainedUnsortedSegments > 1)
        {
            Parallel.For(0, remainedUnsortedSegments / 2, index =>
            {
                var firstBlockIndex = index * shift;
                var secondBlockIndex = firstBlockIndex + step;
                Merge(words, segmentStartPositions[firstBlockIndex], segmentLength[firstBlockIndex], segmentLength[secondBlockIndex], ref mergeResult);
                segmentLength[firstBlockIndex] += segmentLength[secondBlockIndex];
            });
            (words, mergeResult) = (mergeResult, words);
            
            remainedUnsortedSegments = (remainedUnsortedSegments + 1) / 2;
            step <<= 1;
            shift <<= 1;
        }


        var result = new char[symbols.Length];
        for (int i = 0, position = 0; i < wordsCount; ++i)
        {
            words[i].CopyTo(result, position);
            position += words[i].Length;
            result[position++] = '\n';
        }

        return result;
    }

    private static void Merge(
        ImprovedWord[] words,
        int firstSegmentStartAt,
        int firstSegmentLength,
        int secondSegmentLength,
        ref ImprovedWord[] merged)
    {
        var secondSegmentStartAt = firstSegmentStartAt + firstSegmentLength;
        
        var firstPart = new ArraySegment<ImprovedWord>(words, firstSegmentStartAt, firstSegmentLength);
        var secondPart = new ArraySegment<ImprovedWord>(words, secondSegmentStartAt, secondSegmentLength);

        int index = firstSegmentStartAt,
            firstPartIndex = 0,
            secondPartIndex = 0;

        for (; firstPartIndex < firstSegmentLength && secondPartIndex < secondSegmentLength; index++)
        {
            var comparisonResult = firstPart[firstPartIndex].CompareTo(secondPart[secondPartIndex]);
            if (comparisonResult > 0)
                merged[index] = secondPart[secondPartIndex++];
            else
                merged[index] = firstPart[firstPartIndex++];
        }

        var tailSize = firstSegmentStartAt + firstSegmentLength + secondSegmentLength - index;
        if (firstPartIndex < firstSegmentLength)
            Array.Copy(words, firstSegmentStartAt + firstPartIndex, merged, index, tailSize);
        else
            Array.Copy(words, secondSegmentStartAt + secondPartIndex, merged, index, tailSize);
    }
}