using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebArsip.Api;
using Xunit;

public class SerialNumberConcurrencyTests
{
    private readonly HttpClient _client;

    public SerialNumberConcurrencyTests()
    {
        var appFactory = new WebApplicationFactory<Program>();
        _client = appFactory.CreateClient();
    }

    [Fact]
    public async Task SerialNumber_Should_Not_Duplicate_Under_High_Concurrency()
    {
        const int TOTAL_REQUEST = 1000; // bisa naik ke 500–1000
        string serialKey = "SURAT_TUGAS";

        var results = new ConcurrentBag<string>();
        var tasks = new List<Task>();

        await Parallel.ForEachAsync(
            Enumerable.Range(1, TOTAL_REQUEST),
            new ParallelOptions { MaxDegreeOfParallelism = 40 },
            async (_, _) =>
            {
                var res = await _client.PostAsJsonAsync("/api/SerialNumber/generate", new
                {
                    key = serialKey
                });

                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<SerialNumberGenerateResponseDto>();
                    if (data != null && data.Success)
                        results.Add(data.Generated);
                }
            });

        // 🔍 CHECK: No duplicate values
        var duplicates = results
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate serial numbers found: {string.Join(", ", duplicates.Select(d => d.Key))}");
    }
}

public class SerialNumberGenerateResponseDto
{
    public bool Success { get; set; }
    public string Generated { get; set; } = "";
    public long UsedNumber { get; set; }
}