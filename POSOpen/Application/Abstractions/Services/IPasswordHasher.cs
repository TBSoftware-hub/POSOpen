namespace POSOpen.Application.Abstractions.Services;

public interface IPasswordHasher
{
	(string Hash, string Salt) Hash(string plaintext);

	bool Verify(string plaintext, string storedHash, string storedSalt);
}
