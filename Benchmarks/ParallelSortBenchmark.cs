using Algorithms;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public class ParallelSortBenchmark
{
    private string[] _words = null!;
    private char[] _chars = null!;

    [Params(5_000_000, 10_000_000, 20_000_000)]
    public int N;

    [Params(16)]
    public int C;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _words = File.ReadAllLines("./words.txt");
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
    public char[] ParallelSort()
    {
        var symbols = new char[_chars.Length];
        Array.Copy(_chars, symbols, _chars.Length);
        return ParallelSorter.Sort(_chars, C);
    }
    
    //[Benchmark]
    public char[] ImprovedParallelSort()
    {
        var symbols = new char[_chars.Length];
        Array.Copy(_chars, symbols, _chars.Length);
        return ParallelSorter.ImprovedSort(_chars, C);
    }
}