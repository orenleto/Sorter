using Algorithms.Comparers;
using Algorithms.DataStructures;

namespace UnitTests;

public class WordsComparerTest
{
    [TestCase("25269.'un", "176.q.e.")]
    [TestCase("49780.Г", "8416.哥")]
    [TestCase("8529.0来", "33804.C")]
    [TestCase("33804.C", "176.q.e")]
    [TestCase("7343.Alkoholgewinnung", "98068.Alkoholgewinnung")]
    public void WordCompareToIsCorrect(string word, string biggerWord)
    {
        var biggerWordStruct = Word.Create(new ArraySegment<char>(biggerWord.ToCharArray()));
        var wordStruct = Word.Create(new ArraySegment<char>(word.ToCharArray()));
        
        Assert.That(wordStruct.CompareTo(biggerWordStruct), Is.LessThan(0));
    }

    [TestCase("25269.'un", "176.q.e.")]
    [TestCase("49780.Г", "8416.哥")]
    [TestCase("8529.0来", "33804.C")]
    [TestCase("33804.C", "176.q.e")]
    [TestCase("7343.Alkoholgewinnung", "98068.Alkoholgewinnung")]
    public void ImprovedWordCompareToIsCorrect(string word, string biggerWord)
    {
        var biggerWordStruct = new ImprovedWord(biggerWord.ToCharArray(), 0, biggerWord.IndexOf('.'), biggerWord.Length);
        var wordStruct = new ImprovedWord(word.ToCharArray(), 0, word.IndexOf('.'), word.Length);
        
        Assert.That(wordStruct.CompareTo(biggerWordStruct), Is.LessThan(0));
    }
    
    [TestCase("25269.'un", "176.q.e.")]
    [TestCase("49780.Г", "8416.哥")]
    [TestCase("8529.0来", "33804.C")]
    [TestCase("33804.C", "176.q.e")]
    [TestCase("7343.Alkoholgewinnung", "98068.Alkoholgewinnung")]
    public void ImproveComparerSortingIsCorrect(string word, string biggerWord)
    {
        Assert.That(ImprovedComparer.Instance.Compare(word, biggerWord), Is.LessThan(0));
    }
    
    [TestCase("25269.'un", "176.q.e.")]
    [TestCase("49780.Г", "8416.哥")]
    [TestCase("8529.0来", "33804.C")]
    [TestCase("33804.C", "176.q.e")]
    [TestCase("7343.Alkoholgewinnung", "98068.Alkoholgewinnung")]
    public void SimpleComparerSortingIsCorrect(string word, string biggerWord)
    {
        Assert.That(SimpleComparer.Instance.Compare(word, biggerWord), Is.LessThan(0));
    }
}