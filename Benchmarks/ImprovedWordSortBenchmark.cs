using Algorithms;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public class ImprovedWordSortBenchmark
{
    private string[] _words = null!;
    private char[] _chars = null!;

    [Params(10_000_000, 8_000_000, 4_000_000, 2_000_000, 1_000_000, 500_000, 250_000, 125_000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _words = File.ReadAllLines("./words.txt");
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        var symbolsCount = _words[..N].Sum(word => word.Length) + N;
        _chars = new char[symbolsCount];
        for (int i = 0, pos = 0; i < N; i++, pos++)
        {
            _words[i].AsSpan().CopyTo(_chars.AsSpan(pos, _words[i].Length));
            pos = pos + _words[i].Length;
            _chars[pos] = '\n';
        }
    }
    
    [Benchmark]
    public char[] IntegerWordSort()
    {
        return SimpleSorter.IntegerWordSort(_chars);
    }
    
    [Benchmark]
    public char[] ImprovedWordSort()
    {
        return SimpleSorter.ImprovedWordSort(_chars);
    }
}