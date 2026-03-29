using System.Security.Cryptography;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Security;

public sealed class SecureStorageEncryptionKeyProvider : IEncryptionKeyProvider
{
	private const string EncryptionKeyName = "posopen.sqlite.encryption-key";
	private readonly PosOpenDatabasePathOptions _databasePathOptions;

	public SecureStorageEncryptionKeyProvider(PosOpenDatabasePathOptions databasePathOptions)
	{
		_databasePathOptions = databasePathOptions;
	}

	public async ValueTask<string> GetKeyAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var existingKey = await SecureStorage.Default.GetAsync(EncryptionKeyName);
		if (!string.IsNullOrWhiteSpace(existingKey))
		{
			return existingKey;
		}

		if (File.Exists(_databasePathOptions.DatabasePath))
		{
			throw new InvalidOperationException(
				"The encryption key is missing for an existing local database. Remove the database or restore the original key before continuing.");
		}

		var buffer = RandomNumberGenerator.GetBytes(32);
		var newKey = Convert.ToBase64String(buffer);
		await SecureStorage.Default.SetAsync(EncryptionKeyName, newKey);
		return newKey;
	}
}