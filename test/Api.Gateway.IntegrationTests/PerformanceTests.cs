using System.Collections.Concurrent;
using System.Diagnostics;

namespace Api.Gateway.IntegrationTests;

public class PerformanceTests(IntegrationTestFactory factory)
    : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task Gateway_ShouldHandleConsistentTraffic()
    {
        const int targetThroughputPerMinute = 50_000;
        const int maxConcurrentRequests = 50;
        var maxTestDuration = TimeSpan.FromSeconds(10);

        // Arrange
        var totalRequests = (int)(targetThroughputPerMinute * maxTestDuration.TotalSeconds / 60);
        var concurrencyLimiter = new SemaphoreSlim(maxConcurrentRequests);
        var results = new ConcurrentBag<(bool Success, double ResponseTimeMs)>();
        var tokenSource = new CancellationTokenSource(maxTestDuration);
        var startTime = Stopwatch.GetTimestamp();
        
        // Act
        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, totalRequests),
                new ParallelOptions 
                { 
                    MaxDegreeOfParallelism = maxConcurrentRequests * 2,
                    CancellationToken = tokenSource.Token
                },
                async (_, token) =>
                {
                    try
                    {
                        await concurrencyLimiter.WaitAsync(token);
                        
                        try
                        {
                            var requestStart = Stopwatch.GetTimestamp();
                            var response = await factory.Client.GetAsync($"/{factory.Api1Name}/echo", token);
                            var requestDuration = Stopwatch.GetElapsedTime(requestStart).TotalMilliseconds;
                            results.Add((response.IsSuccessStatusCode, requestDuration));
                        }
                        finally
                        {
                            concurrencyLimiter.Release();
                        }
                    }
                    catch (Exception)
                    {
                        results.Add((false, -1));
                    }
                });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Test duration limit reached");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during test execution: {ex}");
        }
        finally
        {
            var totalDuration = Stopwatch.GetElapsedTime(startTime);
            
            // Output test results for analysis
            var successfulRequestsDuration = results
                .Where(r => r.Success)
                .Select(r => r.ResponseTimeMs)
                .OrderBy(t => t)
                .ToList();
            var successRate = successfulRequestsDuration.Count * 100 / (double)results.Count;

            var avgResponseTime = 0d;
            var p99ResponseTime = 0d;
            if (successfulRequestsDuration.Count > 0)
            {
                avgResponseTime = successfulRequestsDuration.Average();
                p99ResponseTime = successfulRequestsDuration.ElementAt((int)(successfulRequestsDuration.Count * 0.99));
            }
            var perMinuteThroughput = results.Count * 60 / totalDuration.TotalSeconds;

            Console.WriteLine("---------- PERFORMANCE TEST RESULTS ----------");
            Console.WriteLine($"Test completed in: {totalDuration.TotalSeconds:F2}s");
            Console.WriteLine($"Success rate: {successRate:F2}%");
            Console.WriteLine($"Actual throughput: {perMinuteThroughput:N0} requests/minute");
            Console.WriteLine($"Avg response time: {avgResponseTime:F2}ms");
            Console.WriteLine($"P99 response time: {p99ResponseTime}ms");
            Console.WriteLine("----------------------------------------------");
            
            // Assert
            successRate.ShouldBeGreaterThanOrEqualTo(99);
            perMinuteThroughput.ShouldBeGreaterThanOrEqualTo(targetThroughputPerMinute);
            avgResponseTime.ShouldBeLessThanOrEqualTo(100);
            p99ResponseTime.ShouldBeLessThanOrEqualTo(500);
            results.Count.ShouldBe(totalRequests);
        }
    }
}
