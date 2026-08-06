namespace SENG302.Api.Services;

public class EmailBackgroundService : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;

    public EmailBackgroundService(IEmailQueue queue, IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Executes the background service that reads email jobs from the queue and then sends them using the email service.
    /// This method runs continuously until the service is stopped or cancelled.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var job in _queue.ReadAllAsync(cancellationToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await emailService.SendEmailAsync(job.toEmail, job.Template, job.Model);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to send queued email to {job.toEmail}: {exception.Message}");
            }
        }
    }
}