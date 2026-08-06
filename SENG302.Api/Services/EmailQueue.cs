using System.Threading.Channels;
using SENG302.Api.Models.Jobs;

namespace SENG302.Api.Services;

public interface IEmailQueue
{
    ValueTask QueueEmailAsync(EmailJob email, CancellationToken cancellationToken = default);
    IAsyncEnumerable<EmailJob> ReadAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Provides a bounded in-memoryt queue for emails that are pending. It being bounded
/// means that the amount of emails that can be added is limited if the SMTP service cant keep up.
/// </summary>
public class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailJob> _queue = Channel.CreateBounded<EmailJob>(
        new BoundedChannelOptions(100)
        {
            // Dont discard emails when the queue is full
            FullMode = BoundedChannelFullMode.Wait,
            // EMail jobs are only processed by one background worker
            SingleReader = true
        }
    );

    /// <summary>
    /// Adds a email job to the queue, if the queue is full it waits until space
    /// becomes avaliable
    /// </summary>
    /// <param name="email">the email job to add</param>
    /// <param name="cancellationToken">the token to cancel while waiiting for queue space</param>
    /// <returns></returns>
    public ValueTask QueueEmailAsync(EmailJob email, CancellationToken cancellationToken = default)
    {
        return _queue.Writer.WriteAsync(email, cancellationToken);
    }

    /// <summary>
    /// Returns the queued email job as they become avaliable, this allows
    /// the queue to be used by the email background service
    /// </summary>
    /// <param name="cancellationToken"> token used to stop reading the jobs</param>
    /// <returns></returns>
    public IAsyncEnumerable<EmailJob> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }

}