namespace POSOpen.Application.Abstractions.Services;

public interface IEncryptionKeyProvider
{
	ValueTask<string> GetKeyAsync(CancellationToken cancellationToken = default);
}