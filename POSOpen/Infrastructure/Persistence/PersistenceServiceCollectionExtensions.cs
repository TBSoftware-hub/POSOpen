using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSOpen.Application.Abstractions.Persistence;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Infrastructure.Security;
using POSOpen.Infrastructure.Services;

namespace POSOpen.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
	public static IServiceCollection AddPosOpenPersistence(this IServiceCollection services)
	{
		services.AddSingleton<IEncryptionKeyProvider, SecureStorageEncryptionKeyProvider>();
		services.AddSingleton<IUtcClock, SystemUtcClock>();
		services.AddSingleton<IOperationContextFactory, OperationContextFactory>();
		services.AddSingleton<IAuthorizationPolicyService, AuthorizationPolicyService>();
		services.AddSingleton<ICurrentSessionService, AppStateCurrentSessionService>();
		services.AddSingleton(CreateDatabasePathOptions());
		services.AddDbContextFactory<PosOpenDbContext>((serviceProvider, options) =>
		{
			var databasePathOptions = serviceProvider.GetRequiredService<PosOpenDatabasePathOptions>();
			var keyProvider = serviceProvider.GetRequiredService<IEncryptionKeyProvider>();
			var encryptionKey = keyProvider.GetKeyAsync().AsTask().GetAwaiter().GetResult();
			var connectionString = SqliteConnectionStringFactory.Create(databasePathOptions.DatabasePath, encryptionKey);

			options.UseSqlite(connectionString, sqlite =>
			{
				sqlite.MigrationsAssembly(typeof(PosOpenDbContext).Assembly.FullName);
			});
		});

		services.AddSingleton<IAppDbContextInitializer, AppDbContextInitializer>();
		services.AddTransient<IOperationLogRepository, OperationLogRepository>();
		services.AddTransient<IOutboxRepository, OutboxRepository>();
		services.AddTransient<IAdmissionCheckInRepository, AdmissionCheckInRepository>();
		services.AddTransient<IFamilyProfileRepository, FamilyProfileRepository>();
		services.AddTransient<ICartSessionRepository, CartSessionRepository>();
		services.AddTransient<ICheckoutPaymentAttemptRepository, CheckoutPaymentAttemptRepository>();
		services.AddTransient<IRefundRepository, RefundRepository>();
		services.AddTransient<IPartyBookingRepository, PartyBookingRepository>();
		services.AddTransient<IReceiptMetadataRepository, ReceiptMetadataRepository>();
		services.AddTransient<ITransactionOperationRepository, TransactionOperationRepository>();
		services.AddTransient<IStaffAccountRepository, StaffAccountRepository>();
		services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

		return services;
	}

	private static PosOpenDatabasePathOptions CreateDatabasePathOptions()
	{
		var databaseDirectory = Path.Combine(FileSystem.AppDataDirectory, "Data");
		Directory.CreateDirectory(databaseDirectory);
		return new PosOpenDatabasePathOptions(Path.Combine(databaseDirectory, "posopen.db"));
	}
}