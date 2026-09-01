using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.Email;

namespace SENG302.Api.Tests.Unit.Services;

public class EmailBackgroundServiceUnitTests
{
    private static EmailJob CreateJob(string email)
    {
        return new EmailJob(
            email,
            EmailTemplate.VerifyEmailCode,
            new Dictionary<string, string> { ["CODE"] = "123456" }
        );
    }

    private static EmailBackgroundService CreateWorker(
        IEmailQueue queue,
        IEmailService emailService
    )
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IEmailService)).Returns(emailService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return new EmailBackgroundService(queue, scopeFactory);
    }

    [Fact]
    public async Task ExecuteAsync_QueuedEmail_SendsEmail()
    {
        var queue = new EmailQueue();
        var emailService = Substitute.For<IEmailService>();
        var processed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var job = CreateJob("test@example.com");
        emailService.SendEmailAsync(job.toEmail, job.Template, job.Model)
            .Returns(_ =>
            {
                processed.TrySetResult();
                return Task.CompletedTask;
            });
        await queue.QueueEmailAsync(job);
        var worker = CreateWorker(queue, emailService);

        await worker.StartAsync(CancellationToken.None);
        await processed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        await emailService.Received(1).SendEmailAsync(
            job.toEmail,
            job.Template,
            job.Model
        );
    }

    [Fact]
    public async Task ExecuteAsync_FirstEmailFails_ProcessesNextEmail()
    {
        var queue = new EmailQueue();
        var emailService = Substitute.For<IEmailService>();
        var secondProcessed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var firstJob = CreateJob("first@example.com");
        var secondJob = CreateJob("second@example.com");
        emailService.SendEmailAsync(
                firstJob.toEmail,
                firstJob.Template,
                firstJob.Model
            )
            .Returns(Task.FromException(new Exception("Email failed")));
        emailService.SendEmailAsync(
                secondJob.toEmail,
                secondJob.Template,
                secondJob.Model
            )
            .Returns(_ =>
            {
                secondProcessed.TrySetResult();
                return Task.CompletedTask;
            });
        await queue.QueueEmailAsync(firstJob);
        await queue.QueueEmailAsync(secondJob);
        var worker = CreateWorker(queue, emailService);

        await worker.StartAsync(CancellationToken.None);
        await secondProcessed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        await emailService.Received(1).SendEmailAsync(
            secondJob.toEmail,
            secondJob.Template,
            secondJob.Model
        );
    }
}
