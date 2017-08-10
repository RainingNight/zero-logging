using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Logging.File.Internal
{
    /// <summary>
    /// Provides an abstraction for a writer of messages.
    /// </summary>
    public interface IMessageWriter : IDisposable
    {
        /// <summary>
        /// Write messages
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task WriteMessagesAsync(string message, CancellationToken cancellationToken = default(CancellationToken));
    }
}
