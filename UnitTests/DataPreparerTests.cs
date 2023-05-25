using System.Text;

namespace UnitTests;

public class DataPreparerTests
{
    [Test]
    public void BenchmarkDataPreparer()
    {
        var words = new[]
        {
            "1.надеванные",
            "2.Verfahrensmodellen",
            "3.kapselnde",
        };

        var symbolsCount = words.Sum(word => word.Length) + words.Length - 1;
        var chars = new char[symbolsCount];
        for (int i = 0, pos = 0; i < words.Length; pos = pos + words[i++].Length)
        {
            if (i > 0)
                chars[pos++] = '\n';
            words[i].AsSpan().CopyTo(chars.AsSpan(pos, words[i].Length));
        }

        Assert.AreEqual(chars, string.Join('\n', words));
    }
}