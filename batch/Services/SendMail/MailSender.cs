using System.Linq;
using System.Threading.Tasks;
using batch.Models;

namespace batch.Services.SendMail;

public interface IMailSender
{
	Task<(bool success, string? error)> SendMailAsync(Notification notification);
}

internal interface IBatchMailSender: IMailSender
{
}

abstract class SingleRecipientMailSender : IMailSender
{
	private const double MinimumSuccessRate = 0.5;

	public async Task<(bool success, string? error)> SendMailAsync(Notification notification)
	{
		var tasks = notification.to.Select(recipient => SendMailAsync(recipient, notification));

		var results = await Task.WhenAll(tasks);

		var successCount = results.Count(result => result.success);
		var successRate = (double)successCount / results.Length;
		var overallSuccess = successRate >= MinimumSuccessRate;

		var errors = results
			.Select(result => result.error)
			.Where(error => error is not null);
		var overallError = errors.Any() ? string.Join("; ", errors) : null;

		return (overallSuccess, overallError);
	}

	public abstract Task<(bool success, string? error)> SendMailAsync(string recipient, Notification notification);
}
