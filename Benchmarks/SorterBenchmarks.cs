using Algorithms;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public class SorterBenchmarks
{
    private string[] _words = null!;
    private char[] _chars = null!;

    [Params(5_000_000)]
    public int N;
    
    [IterationSetup]
    public void IterationSetup()
    {
        _words = File.ReadAllLines("./words.txt")[..N];

        var symbolsCount = _words.Sum(word => word.Length) + _words.Length;
        _chars = new char[symbolsCount];
        for (int i = 0, pos = 0; i < _words.Length; i++, pos++)
        {
            _words[i].AsSpan().CopyTo(_chars.AsSpan(pos, _words[i].Length));
            pos = pos + _words[i].Length;
            _chars[pos] = '\n';
        }
    }

    [Benchmark]
    public char[] SimpleSort()
    {
        return SimpleSorter.Sort(_chars);
    }
    
    [Benchmark]
    public char[] ImprovedComparerSort()
    {
        return SimpleSorter.ImprovedComparerSort(_chars);
    }
    
    [Benchmark]
    public char[] WordSort()
    {
        return SimpleSorter.WordSort(_chars);
    }
    
    [Benchmark]
    public char[] ImprovedWordSort()
    {
        return SimpleSorter.ImprovedWordSort(_chars);
    }
    
    [Benchmark]
    public char[] IntegerWordSort()
    {
        return SimpleSorter.IntegerWordSort(_chars);
    }
}