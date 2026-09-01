using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.Email;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Services;

public class EmailQueueUnitTests
{
    private static EmailJob CreateJob(string email)
    {
        return new EmailJob(
            email,
            EmailTemplate.VerifyEmailCode,
            new Dictionary<string, string> { ["CODE"] = "123456" }
        );
    }

    [Fact]
    public async Task QueueEmailAsync_ValidJob_JobCanBeRead()
    {
        var queue = new EmailQueue();
        var job = CreateJob("test@example.com");

        await queue.QueueEmailAsync(job);
        await using var reader = queue.ReadAllAsync(CancellationToken.None)
            .GetAsyncEnumerator();
        var hasJob = await reader.MoveNextAsync();

        hasJob.ShouldBeTrue();
        reader.Current.ShouldBe(job);
    }

    [Fact]
    public async Task ReadAllAsync_MultipleJobs_ReturnsJobsInOrder()
    {
        var queue = new EmailQueue();
        var firstJob = CreateJob("first@example.com");
        var secondJob = CreateJob("second@example.com");

        await queue.QueueEmailAsync(firstJob);
        await queue.QueueEmailAsync(secondJob);

        await using var reader = queue.ReadAllAsync(CancellationToken.None)
            .GetAsyncEnumerator();
        (await reader.MoveNextAsync()).ShouldBeTrue();
        reader.Current.ShouldBe(firstJob);
        (await reader.MoveNextAsync()).ShouldBeTrue();
        reader.Current.ShouldBe(secondJob);
    }
}
