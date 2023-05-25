using System.Buffers;
using System.Diagnostics;
using Algorithms.DataStructures;

const string BigFileName = "./words.txt";
const int BlockSize = 50_000_000;
var blockCount = 0;

using var fileStream = File.OpenRead(BigFileName);
using var bufferedStream = new BufferedStream(fileStream);
using var reader = new StreamReader(bufferedStream);

var startSplitProcessTimestamp = Stopwatch.GetTimestamp();
var buffer = ArrayPool<char>.Shared.Rent(BlockSize);
var pointer = 0;
var totalWords = 0;
while (!reader.EndOfStream)
{
    var path = string.Format("part_{0}.txt", blockCount);
    var metaPath = string.Format("meta_{0}.txt", blockCount);
    
    var startIterationTimestamp = Stopwatch.GetTimestamp();
    
    var readSymbols = reader.ReadBlock(buffer.AsSpan(pointer, buffer.Length - pointer)) + pointer;
    var lastWordEndsAt = readSymbols - 1;
    while (buffer[lastWordEndsAt] != '\n')
        lastWordEndsAt--;
    var readTimestamp = Stopwatch.GetTimestamp();
    
    var sortedWords = Sort(new ArraySegment<char>(buffer, 0, ++lastWordEndsAt));
    var sortingTimestamp = Stopwatch.GetTimestamp();

    var result = ArrayPool<char>.Shared.Rent(lastWordEndsAt);
    var meta = ArrayPool<byte>.Shared.Rent(2 * 4 * sortedWords.Length + 4);

    meta[0] = (byte)((sortedWords.Length >> 0) & 255);
    meta[1] = (byte)((sortedWords.Length >> 8) & 255);
    meta[2] = (byte)((sortedWords.Length >> 16) & 255);
    meta[3] = (byte)((sortedWords.Length >> 24) & 255);
    
    for (int i = 0, index = 0, j = 4; i < sortedWords.Length; ++i, j += 8)
    {
        sortedWords[i].CopyTo(result, index);
        index += sortedWords[i].Length;

        meta[j + 0] = (byte)((sortedWords[i].Length >> 0) & 255);
        meta[j + 1] = (byte)((sortedWords[i].Length >> 8) & 255);
        meta[j + 2] = (byte)((sortedWords[i].Length >> 16) & 255);
        meta[j + 3] = (byte)((sortedWords[i].Length >> 24) & 255);
        meta[j + 4] = (byte)((sortedWords[i].WordPrefixLength >> 0) & 255);
        meta[j + 5] = (byte)((sortedWords[i].WordPrefixLength >> 8) & 255);
        meta[j + 6] = (byte)((sortedWords[i].WordPrefixLength >> 16) & 255);
        meta[j + 7] = (byte)((sortedWords[i].WordPrefixLength >> 24) & 255);
    }

    File.WriteAllBytes(metaPath, meta);
    using var writer = new StreamWriter(File.OpenWrite(path));
    writer.Write(new ReadOnlySpan<char>(result, 0, lastWordEndsAt));
    
    ArrayPool<char>.Shared.Return(result);
    ArrayPool<byte>.Shared.Return(meta);

    pointer = readSymbols - lastWordEndsAt;
    Array.Copy(buffer, lastWordEndsAt, buffer, 0, pointer);
    blockCount++;
    totalWords += sortedWords.Length;
    var writingTimestamp = Stopwatch.GetTimestamp();
    
    Console.WriteLine($"{path} WORDS: {sortedWords.Length} " +
                      $"READ {Stopwatch.GetElapsedTime(startIterationTimestamp, readTimestamp).TotalMilliseconds}ms; " +
                      $"SORT {Stopwatch.GetElapsedTime(readTimestamp, sortingTimestamp).TotalMilliseconds}ms; " +
                      $"WRITE {Stopwatch.GetElapsedTime(sortingTimestamp, writingTimestamp).TotalMilliseconds}ms;");
}
Console.WriteLine($"ELAPSED ON SPLIT {totalWords}: {Stopwatch.GetElapsedTime(startSplitProcessTimestamp).TotalMilliseconds}ms");

var startMergeProcessTimestamp = Stopwatch.GetTimestamp();
var wordReaders = new ImprovedWordReader[blockCount];
var heap = new Heap<(ImprovedWord, int)>(blockCount);
for (var i = 0; i < blockCount; ++i)
{
    var path = String.Format("part_{0}.txt", i);
    var metaPath = String.Format("meta_{0}.txt", i);
    var fileReader = new StreamReader(File.OpenRead(path));
    var metaReader = File.OpenRead(metaPath);
    wordReaders[i] = new ImprovedWordReader(fileReader, metaReader);
    wordReaders[i].Init();
    heap.Add((wordReaders[i].Read(), i));
}

using var resultFileStream = File.OpenWrite("./result.txt");
using var resultBufferedWriter = new BufferedStream(resultFileStream);
using var resultWriter = new StreamWriter(resultBufferedWriter);

var mergedWords = 0;
while (heap.Count > 0)
{
    var (word, index) = heap.PopTop();
    resultWriter.WriteLine(word.AsReadOnlySpan());
    mergedWords++;
    if (wordReaders[index].HasWord)
    {
        word = wordReaders[index].Read();
        heap.Add((word, index));
    }
}

foreach(var wordReader in wordReaders)
    wordReader.Dispose();
Console.WriteLine($"ELAPSED ON MERGE {mergedWords} WORDS: {Stopwatch.GetElapsedTime(startMergeProcessTimestamp).TotalMilliseconds}ms");

static ImprovedWord[] Sort(ArraySegment<char> symbols)
{
    var wordsCount = 0;
    for (var i = 0; i < symbols.Count; ++i)
        if (symbols[i] == '\n')
            ++wordsCount;

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

struct ImprovedWordReader : IDisposable
{
    private readonly FileStream _metaReader;
    private readonly StreamReader _fileReader;
    private readonly byte[] _buffer;
    private readonly char[] _symbols;

    private int _remainedWords;
    private int _bufferPosition;
    private int _symbolsPosition;

    public ImprovedWordReader(StreamReader fileReader, FileStream metaReader)
    {
        _fileReader = fileReader;
        _metaReader = metaReader;
        _symbols = ArrayPool<char>.Shared.Rent(10 * 1024 * 1024);
        _buffer = ArrayPool<byte>.Shared.Rent(2 * 1024 * 1024);

        _remainedWords = 0;
        _symbolsPosition = 0;
        _bufferPosition = 0;
    }

    public bool HasWord => _remainedWords > 0;

    public void Init()
    {
         _metaReader.Read(_buffer);
         _fileReader.ReadBlock(_symbols);
        
        _remainedWords = (_buffer[_bufferPosition + 0] << 0) |
                     (_buffer[_bufferPosition + 1] << 8) |
                     (_buffer[_bufferPosition + 2] << 16) |
                     (_buffer[_bufferPosition + 3] << 24);
        _bufferPosition = 4;
        _symbolsPosition = 0;
    }

    public ImprovedWord Read()
    {
        _remainedWords--;
        var length = (_buffer[_bufferPosition + 0] << 0) |
                     (_buffer[_bufferPosition + 1] << 8) |
                     (_buffer[_bufferPosition + 2] << 16) |
                     (_buffer[_bufferPosition + 3] << 24);
        _bufferPosition += 4;
        if (_bufferPosition >= _buffer.Length)
        {
            _bufferPosition = 0;
            _metaReader.Read(_buffer);
        }
        var prefixLength = (_buffer[_bufferPosition + 0] << 0) |
                           (_buffer[_bufferPosition + 1] << 8) |
                           (_buffer[_bufferPosition + 2] << 16) |
                           (_buffer[_bufferPosition + 3] << 24);
        _bufferPosition += 4;
        if (_bufferPosition >= _buffer.Length)
        {
            _bufferPosition = 0;
            _metaReader.Read(_buffer);
        }

        if (_symbolsPosition + length >= _symbols.Length)
        {
            var tailSize = _symbols.Length - _symbolsPosition;
            Array.Copy(_symbols, _symbolsPosition, _symbols, 0, tailSize);
            
            _symbolsPosition = 0;
             _fileReader.ReadBlock(_symbols, tailSize, _symbols.Length - tailSize);
        }
        
        var wordStartPosition = _symbolsPosition;
        _symbolsPosition += length;

        return new ImprovedWord(_symbols, wordStartPosition, prefixLength, wordStartPosition + length);
    }

    public void Dispose()
    {
         ArrayPool<char>.Shared.Return(_symbols);
         ArrayPool<byte>.Shared.Return(_buffer);
        _metaReader.Dispose();
        _fileReader.Dispose();
    }
}

struct ImprovedWord : IComparable<ImprovedWord>
{
    public readonly int Length;
    public readonly int WordLength;
    public readonly int WordPrefixLength;
    
    private readonly char[] _text;
    private readonly int _wordStartPosition;

    public ImprovedWord(char[] text, int wordStartPosition, int wordDividerPosition, int wordFinishPosition)
    {
        _text = text;
        _wordStartPosition = wordStartPosition;
        WordPrefixLength = wordDividerPosition - wordStartPosition;
        WordLength = wordFinishPosition - wordDividerPosition;
        Length = wordFinishPosition - wordStartPosition;
    }
    
    public int CompareTo(ImprovedWord other)
    {
        var wordStartPosition = _wordStartPosition + WordPrefixLength;
        var otherWordStartPosition = other._wordStartPosition + other.WordPrefixLength;
        
        var wordFinishPosition = _wordStartPosition + Length;
        var otherWordFinishPosition = other._wordStartPosition + other.Length;
        
        for (int i = wordStartPosition, j = otherWordStartPosition; i < wordFinishPosition && j < otherWordFinishPosition; ++i, ++j)
        {
            var symbolsComparingResult = _text[i].CompareTo(other._text[j]);
            if (symbolsComparingResult != 0)
                return symbolsComparingResult;
        }
        
        var wordLengthComparingResult = WordLength.CompareTo(other.WordLength);
        if (wordLengthComparingResult != 0)
            return wordLengthComparingResult;
        
        var wordBeginPositionComparingResult = WordPrefixLength.CompareTo(other.WordPrefixLength);
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
        Array.Copy(_text, _wordStartPosition, destination, destinationIndex, Length);
    }

    public ReadOnlySpan<char> AsReadOnlySpan()
    {
        return new ReadOnlySpan<char>(_text, _wordStartPosition, Length);
    }
}