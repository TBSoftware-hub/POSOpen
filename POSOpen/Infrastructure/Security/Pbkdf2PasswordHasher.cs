using System.Security.Cryptography;
using System.Text;
using POSOpen.Application.Abstractions.Services;

namespace POSOpen.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
	private const int SaltSize = 16;
	private const int HashSize = 32;
	private const int IterationCount = 100_000;

	public (string Hash, string Salt) Hash(string plaintext)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

		var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
		var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
			Encoding.UTF8.GetBytes(plaintext),
			saltBytes,
			IterationCount,
			HashAlgorithmName.SHA256,
			HashSize);

		return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
	}

	public bool Verify(string plaintext, string storedHash, string storedSalt)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
		ArgumentException.ThrowIfNullOrWhiteSpace(storedHash);
		ArgumentException.ThrowIfNullOrWhiteSpace(storedSalt);

		byte[] storedHashBytes;
		byte[] saltBytes;
		try
		{
			storedHashBytes = Convert.FromBase64String(storedHash);
			saltBytes = Convert.FromBase64String(storedSalt);
		}
		catch (FormatException)
		{
			return false;
		}

		var computedHash = Rfc2898DeriveBytes.Pbkdf2(
			Encoding.UTF8.GetBytes(plaintext),
			saltBytes,
			IterationCount,
			HashAlgorithmName.SHA256,
			HashSize);

		return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
	}
}
