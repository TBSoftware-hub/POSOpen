using FluentAssertions;
using POSOpen.Infrastructure.Security;

namespace POSOpen.Tests.Unit.Security;

public sealed class Pbkdf2PasswordHasherTests
{
	[Fact]
	public void Hash_and_verify_round_trip_succeeds()
	{
		var hasher = new Pbkdf2PasswordHasher();
		var (hash, salt) = hasher.Hash("secret-pass");

		hash.Should().NotBeNullOrWhiteSpace();
		salt.Should().NotBeNullOrWhiteSpace();
		hasher.Verify("secret-pass", hash, salt).Should().BeTrue();
	}

	[Fact]
	public void Verify_returns_false_for_wrong_password()
	{
		var hasher = new Pbkdf2PasswordHasher();
		var (hash, salt) = hasher.Hash("secret-pass");

		hasher.Verify("wrong-pass", hash, salt).Should().BeFalse();
	}

	[Fact]
	public void Verify_returns_false_for_tampered_hash()
	{
		var hasher = new Pbkdf2PasswordHasher();
		var (hash, salt) = hasher.Hash("secret-pass");
		var tamperedBytes = Convert.FromBase64String(hash);
		tamperedBytes[0] = (byte)(tamperedBytes[0] ^ 0x0F);
		var tamperedHash = Convert.ToBase64String(tamperedBytes);

		hasher.Verify("secret-pass", tamperedHash, salt).Should().BeFalse();
	}

	[Fact]
	public void Hash_uses_random_salt_for_same_input()
	{
		var hasher = new Pbkdf2PasswordHasher();
		var first = hasher.Hash("same-input");
		var second = hasher.Hash("same-input");

		first.Salt.Should().NotBe(second.Salt);
		first.Hash.Should().NotBe(second.Hash);
	}
}
