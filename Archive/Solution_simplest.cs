using System.Buffers;
using System.Diagnostics;
using Algorithms.Comparers;
using Algorithms.DataStructures;

const string BigFileName = "./words.txt";
const int BlockSize = 40_000_000;
var buffer = ArrayPool<char>.Shared.Rent(BlockSize);
var pointer = 0;
var blockCount = 0;

using var fileStream = File.OpenRead(BigFileName);
using var bufferedStream = new BufferedStream(fileStream);
using var reader = new StreamReader(bufferedStream);

var startSplitProcessTimestamp = Stopwatch.GetTimestamp();
while (!reader.EndOfStream)
{
    var path = string.Format("part_{0}.txt", blockCount);
    
    var startIterationTimestamp = Stopwatch.GetTimestamp();
    
    var readSymbols = reader.ReadBlock(buffer.AsSpan(pointer, buffer.Length - pointer)) + pointer;
    var lastWordEndsAt = readSymbols - 1;
    while (buffer[lastWordEndsAt] != '\n')
        lastWordEndsAt--;
    var readTimestamp = Stopwatch.GetTimestamp();
    
    var sortedWords = Sort(new ArraySegment<char>(buffer, 0, ++lastWordEndsAt));
    var sortingTimestamp = Stopwatch.GetTimestamp();

    var result = ArrayPool<char>.Shared.Rent(lastWordEndsAt);

    for (int i = 0, index = 0; i < sortedWords.Length; ++i, ++index)
    {
        sortedWords[i].CopyTo(result, index);
        index += sortedWords[i].Length;
    }

    using var writer = new StreamWriter(File.OpenWrite(path));
    writer.Write(result);
    
    pointer = readSymbols - lastWordEndsAt;
    Array.Copy(buffer, lastWordEndsAt, buffer, 0, pointer);
    blockCount++;
    var writingTimestamp = Stopwatch.GetTimestamp();
    
    Console.WriteLine($"{path} " +
                      $"READ {Stopwatch.GetElapsedTime(startIterationTimestamp, readTimestamp).TotalMilliseconds}ms; " +
                      $"SORT {Stopwatch.GetElapsedTime(readTimestamp, sortingTimestamp).TotalMilliseconds}ms; " +
                      $"WRITE {Stopwatch.GetElapsedTime(sortingTimestamp, writingTimestamp).TotalMilliseconds}ms;");
}
Console.WriteLine($"ELAPSED ON SPLIT: {Stopwatch.GetElapsedTime(startSplitProcessTimestamp).TotalMilliseconds}ms");

var startMergeProcessTimestamp = Stopwatch.GetTimestamp();
var fileReaders = new StreamReader[blockCount];
var heap = new Heap<HeapNode>(blockCount);
for (var i = 0; i < blockCount; ++i)
{
    var path = String.Format("part_{0}.txt", i);
    fileReaders[i] = new StreamReader(new BufferedStream(File.OpenRead(path)));

    var word = fileReaders[i].ReadLine();
    heap.Add(new HeapNode(word, i));
}

using var resultFileStream = File.OpenWrite("./result.txt");
using var resultBufferedWriter = new BufferedStream(resultFileStream);
using var resultWriter = new StreamWriter(resultBufferedWriter);

while (heap.Count > 0)
{
    var (word, index) = heap.PopTop();
    resultWriter.WriteLine(word);
    if (!fileReaders[index].EndOfStream)
    {
        word = fileReaders[index].ReadLine();
        if (word is not null && word.Contains('.'))
            heap.Add(new HeapNode(word, index));
    }
}

foreach(var fileReader in fileReaders)
    fileReader.Dispose();
Console.WriteLine($"ELAPSED ON MERGE: {Stopwatch.GetElapsedTime(startMergeProcessTimestamp).TotalMilliseconds}ms");

static ImprovedWord[] Sort(ArraySegment<char> symbols)
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
    for (int i = 0, wordIndex = 0; i < symbols.Count; ++i, ++wordIndex)
    {
        var wordBeginAt = i;
        while (symbols[i] != '.') i++;
        var dividerPosition = i++;
        while (symbols[i] != '\n') i++;
        words[wordIndex] = new ImprovedWord(symbols.Array!, wordBeginAt, dividerPosition, i);
    }
        
    Array.Sort(words);
    return words;
}

readonly struct HeapNode : IComparable<HeapNode>
{
    public readonly string Word;
    public readonly int Index;

    public HeapNode(string word, int index)
    {
        Word = word;
        Index = index;
    }

    public int CompareTo(HeapNode other)
    {
        return ImprovedComparer.Instance.Compare(Word, other.Word);
    }

    public void Deconstruct(out string word, out int index)
    {
        word = Word;
        index = Index;
    }
}

readonly struct ImprovedWord : IComparable<ImprovedWord>
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
