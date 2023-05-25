using System.Text;
using Algorithms;

namespace UnitTests;

public class SorterTest
{
    private byte[] _rawData = null!;

    [SetUp]
    public void Setup()
    {
        _rawData = File.ReadAllBytes("./words_small.txt");
    }

    [Test]
    public void ImprovedParallelSortIsCorrect()
    {
        var symbols = Encoding.UTF8.GetChars(_rawData);
        var actual = ParallelSorter.ImprovedSort(symbols, 16);
        AssertFunction(actual);
    }
    
    [Test]
    public void ParallelSortIsCorrect()
    {
        var symbols = Encoding.UTF8.GetChars(_rawData);
        var actual = ParallelSorter.Sort(symbols, 3);
        AssertFunction(actual);
    }

    [Test]
    public void SortingIsCorrect()
    {
        var symbols = Encoding.UTF8.GetChars(_rawData);
        var actual = SimpleSorter.Sort(symbols);
        AssertFunction(actual);
    }

    [Test]
    public void SecondSortingIsCorrect()
    {
        var symbols = Encoding.UTF8.GetChars(_rawData);
        var actual = SimpleSorter.WordSort(symbols);
        AssertFunction(actual);
    }

    [Test]
    public void ThirdSortingIsCorrect()
    {
        var symbols = Encoding.UTF8.GetChars(_rawData);
        var actual = SimpleSorter.ImprovedComparerSort(symbols);
        AssertFunction(actual);
    }

    [Test]
    public void FourthSortingIsCorrect()
    {
        var symbols = Encoding.UTF8.GetChars(_rawData);
        var actual = SimpleSorter.ImprovedWordSort(symbols);
        AssertFunction(actual);
    }

    [Test]
    public void FifthSortingIsCorrect()
    {
        var symbols = Encoding.UTF8.GetChars(_rawData);
        var actual = SimpleSorter.IntegerWordSort(symbols);
        AssertFunction(actual);
    }

    private static void AssertFunction(char[] actual)
    {
        var expected = File.ReadAllText("./sortedWords.txt");
        Assert.AreEqual(expected, actual);
    }
}