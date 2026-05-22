using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using IotRouter.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace IotRouterTests;

public class DraginoUdpParserRegressionFromSampleDataTest
{
    private static readonly Regex DataLineRegex = new(@"Data:\s+(\{.+\})", RegexOptions.Compiled);

    [Fact]
    public void SampleDataDumpProducesStableParsedOutputFingerprint()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var logger = Mock.Of<ILogger<DraginoUdpParser>>();
        serviceProviderMock.Setup(m => m.GetService(It.IsAny<Type>())).Returns(logger);

        var parser = new DraginoUdpParser(serviceProviderMock.Object, null, "test");
        var sampleFile = FindSampleDataFile();

        var signatures = new List<string>();

        foreach (var line in File.ReadLines(sampleFile))
        {
            var match = DataLineRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var json = match.Groups[1].Value;
            var parsed = parser.Parse(Encoding.UTF8.GetBytes(json));

            Assert.NotNull(parsed);

            var keyValueSignature = string.Join(",", parsed.KeyValues
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}={Convert.ToDecimal(kv.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)}"));

            signatures.Add($"{parsed.DevEUI}|{parsed.FPort}|{parsed.DateTime?.Ticks}|{keyValueSignature}");
        }

        Assert.NotEmpty(signatures);

        var canonicalOutput = string.Join("\n", signatures);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalOutput));
        var hash = Convert.ToHexString(hashBytes);

        Assert.Equal("8AF1D33BCCB9F927EC0B397BD7431C9786A71FAC496A1855AD0EF90CA05B2FEC", hash);
    }

    private static string FindSampleDataFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Sample data.txt");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Unable to locate Sample data.txt");
    }
}
