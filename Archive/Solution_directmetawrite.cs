using System.Buffers;
using System.Diagnostics;
using System.Threading.Channels;

const string BigFileName = "./words.txt";
const int BlockSize = 50_000_000;
const int MaxWordsCount = 3_000_000;
var blockCount = 0;
var parallelismDegree = Environment.ProcessorCount;

using var fileStream = File.OpenRead(BigFileName);
using var bufferedStream = new BufferedStream(fileStream);
using var reader = new StreamReader(bufferedStream);

var startSplitProcessTimestamp = Stopwatch.GetTimestamp();
var tailSize = 0;
var totalWords = 0;

var channel = Channel.CreateBounded<Job>(1);
var callbackChannel = Channel.CreateUnbounded<(int WordsCount, Job Job)>();
var writer = channel.Writer;

var sortingTasks = Enumerable
    .Range(0, parallelismDegree)
    .Select(_ => new Sorter(MaxWordsCount * 2, channel.Reader, callbackChannel.Writer).Do())
    .ToArray();

var handledBlocks = 0;

var freeBlockCount = parallelismDegree + 2;
var arrayStack = new int[freeBlockCount];
for (var i = 0; i < freeBlockCount; ++i) arrayStack[i] = i;

var lastHandledMemoryBlock = -1;

var buffer = ArrayPool<char>.Shared.Rent(BlockSize * freeBlockCount);
var temp = ArrayPool<char>.Shared.Rent(BlockSize * freeBlockCount);
while (!reader.EndOfStream)
{
    if (freeBlockCount == 0)
    {
        while (callbackChannel.Reader.TryRead(out var item))
        {
            totalWords += item.WordsCount;
            handledBlocks++;
            
            arrayStack[freeBlockCount++] = item.Job.MemoryBlockIndex;
        }
    }

    var currentMemoryBlock = arrayStack[--freeBlockCount];
    if (tailSize > 0)
    {
        Array.Copy(buffer, lastHandledMemoryBlock * BlockSize + (BlockSize - tailSize), buffer, currentMemoryBlock * BlockSize, tailSize);
    }
    var memoryBlockSize = reader.ReadBlock(buffer.AsSpan(currentMemoryBlock * BlockSize + tailSize, BlockSize - tailSize)) + tailSize;
    var memoryBlockEndPosition = currentMemoryBlock * BlockSize + memoryBlockSize;
    var memoryBlockLastWordEndPosition = memoryBlockEndPosition;
    while (buffer[memoryBlockLastWordEndPosition - 1] != '\n')
        memoryBlockLastWordEndPosition--;

    lastHandledMemoryBlock = currentMemoryBlock;
    tailSize = memoryBlockEndPosition - memoryBlockLastWordEndPosition;

    var job = new Job(
        new ArraySegment<char>(buffer, currentMemoryBlock * BlockSize, memoryBlockLastWordEndPosition - currentMemoryBlock * BlockSize),
        new ArraySegment<char>(temp, currentMemoryBlock * BlockSize, memoryBlockLastWordEndPosition - currentMemoryBlock * BlockSize),
        blockCount,
        currentMemoryBlock);
    while (await writer.WaitToWriteAsync())
    {
        if (writer.TryWrite(job))
            break;
    } 

    blockCount++;
}

await foreach ((int WordsCount, Job Job) result in callbackChannel.Reader.ReadAllAsync())
{
    totalWords += result.WordsCount;
    handledBlocks++;
    if (handledBlocks == blockCount)
        callbackChannel.Writer.Complete();
}
writer.Complete();
await Task.WhenAll(sortingTasks);

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

readonly struct Job
{
    public readonly ArraySegment<char> Input;
    public readonly ArraySegment<char> Buffer;
    public readonly int JobIdentifier;
    public readonly int MemoryBlockIndex;

    public Job(ArraySegment<char> input, ArraySegment<char> buffer, int jobIdentifier, int memoryBlockIndex)
    {
        Input = input;
        Buffer = buffer;
        JobIdentifier = jobIdentifier;
        MemoryBlockIndex = memoryBlockIndex;
    }
}

readonly struct Sorter
{
    private readonly ChannelReader<Job> _channelReader;
    private readonly ChannelWriter<(int, Job)> _callback;
    private readonly List<ImprovedWord> _words;

    public Sorter(int capacity, ChannelReader<Job> channelReader, ChannelWriter<(int, Job)> callback)
    {
        _channelReader = channelReader;
        _callback = callback;
        _words = new List<ImprovedWord>(capacity);
    }

    public async Task Do()
    {
        await foreach (var job in _channelReader.ReadAllAsync())
        {
            var sortedWordCount = await Sort(job);
            await _callback.WriteAsync((sortedWordCount, job));
        }
    }
    
    public Task<int> Sort(Job job)
    {
        var blockCount = job.JobIdentifier;
        var input = job.Input;
        var buffer = job.Buffer;
        
        var path = string.Format("part_{0}.txt", blockCount);
        var metaPath = string.Format("meta_{0}.txt", blockCount);
        var startIterationTimestamp = Stopwatch.GetTimestamp();
        
        _words.Clear();
        int wordIndex = 0, symbolIndex = 0;
        
        for (; symbolIndex < input.Count; ++symbolIndex, ++wordIndex)
        {
            var wordBeginAt = symbolIndex;
            while (input[symbolIndex] != '.') symbolIndex++;
            var dividerPosition = symbolIndex++;
            while (input[symbolIndex] != '\n') symbolIndex++;
            _words.Add(new ImprovedWord(input.Array!, input.Offset + wordBeginAt, input.Offset + dividerPosition, input.Offset + symbolIndex));
        }

        _words.Sort();
        
        using var metaWriter = new BinaryWriter(new BufferedStream(File.OpenWrite(metaPath), 4 + 2 * 4 * _words.Capacity));
        metaWriter.Write((byte)((wordIndex >> 0) & 255));
        metaWriter.Write((byte)((wordIndex >> 8) & 255));
        metaWriter.Write((byte)((wordIndex >> 16) & 255));
        metaWriter.Write((byte)((wordIndex >> 24) & 255));

        var totalLength = 0;
        for (int i = 0, index = buffer.Offset, j = 4; i < wordIndex; ++i, j += 8)
        {
            _words[i].CopyTo(buffer.Array!, index);
            totalLength += _words[i].Length;
            index += _words[i].Length;

            metaWriter.Write((byte)((_words[i].Length >> 0) & 255));
            metaWriter.Write((byte)((_words[i].Length >> 8) & 255));
            metaWriter.Write((byte)((_words[i].Length >> 16) & 255));
            metaWriter.Write((byte)((_words[i].Length >> 24) & 255));
            metaWriter.Write((byte)((_words[i].WordPrefixLength >> 0) & 255));
            metaWriter.Write((byte)((_words[i].WordPrefixLength >> 8) & 255));
            metaWriter.Write((byte)((_words[i].WordPrefixLength >> 16) & 255));
            metaWriter.Write((byte)((_words[i].WordPrefixLength >> 24) & 255));
        }

        using var writer = new BinaryWriter(File.OpenWrite(path));
        writer.Write(new ReadOnlySpan<char>(buffer.Array!, buffer.Offset, totalLength));
        Console.WriteLine($"{path} WORDS: {wordIndex}; SORT {Stopwatch.GetElapsedTime(startIterationTimestamp).TotalMilliseconds}ms; ");
        return Task.FromResult(wordIndex);
    }
}

public class Heap<T> where T: IComparable<T>
{
    private readonly List<T> _nodes;

    public Heap(int capacity)
    {
        _nodes = new(capacity);
    }
    public void Add(T value)
    {
        _nodes.Add(value);
        HeapifyUp(_nodes.Count - 1);
    }

    public T PeekTop()
    {
        return _nodes[0];
    }

    public T PopTop()
    {
        var result = PeekTop();
        (_nodes[0], _nodes[^1]) = (_nodes[^1], _nodes[0]);
        _nodes.RemoveAt(_nodes.Count - 1);
        HeapifyDown(0);
        return result;
    }

    public void Remove(T value)
    {
        if (_nodes[^1].Equals(value))
        {
            _nodes.RemoveAt(_nodes.Count - 1);
            return;
        }

        var index = _nodes.IndexOf(value);
        (_nodes[index], _nodes[^1]) = (_nodes[^1], _nodes[index]);
        _nodes.RemoveAt(_nodes.Count - 1);

        var parentIndex = (index - 1) / 2;
        if (_nodes[index].CompareTo(_nodes[parentIndex]) < 0)
        {
            HeapifyUp(index);
        }
        else
        {
            HeapifyDown(index);
        }
    }

    public int Count => _nodes.Count;

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            var parentIndex = (index - 1) / 2;
            if (_nodes[index].CompareTo(_nodes[parentIndex]) < 0)
            {
                
                (_nodes[index], _nodes[parentIndex]) = (_nodes[parentIndex], _nodes[index]);
                index = parentIndex;
            }
            else
                break;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            var leftChildIndex = index * 2 + 1;
            if (leftChildIndex >= _nodes.Count)
                return;

            var bestChildIndex = leftChildIndex;

            var rightChildIndex = index * 2 + 2;
            if (rightChildIndex < _nodes.Count)
                if (_nodes[rightChildIndex].CompareTo(_nodes[leftChildIndex]) < 0)
                    bestChildIndex = rightChildIndex;

            if (_nodes[bestChildIndex].CompareTo(_nodes[index]) < 0)
            {
                (_nodes[bestChildIndex], _nodes[index]) = (_nodes[index], _nodes[bestChildIndex]);
                index = bestChildIndex;
            }
            else
                break;
        }
    }
}