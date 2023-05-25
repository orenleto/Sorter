using System.Diagnostics;

const string PathToVocabularies = "./Source";

var outputFileName = args[0];
var fileCapacityInBytes = 1024 * 1024 * long.Parse(args[1]);

var words = new List<string>(2_000_000);
foreach (var file in Directory.EnumerateFiles(PathToVocabularies))
{
    var lines = await File.ReadAllLinesAsync(file);
    words.AddRange(lines);
}

var startTime = Stopwatch.GetTimestamp();
await using var fileStream = File.OpenWrite(outputFileName);
await using var writer = new StreamWriter(fileStream);
var generator = new Random();
var counter = 0;
var upperIndex = words.Count - 1;
while (fileStream.Position < fileCapacityInBytes) 
{
    var index = generator.Next(0, upperIndex);
    writer.Write(++counter);
    writer.Write('.');
    writer.WriteLine(words[index]);
}
var elapsed = Stopwatch.GetElapsedTime(startTime);
Console.WriteLine($"Words count = {counter}, File size = {fileStream.Position}");
Console.WriteLine($"Time Elapsed on Generate: {elapsed:c}");