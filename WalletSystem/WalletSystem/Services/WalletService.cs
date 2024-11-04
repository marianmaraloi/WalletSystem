using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using WalletSystem.Data;
using WalletSystem.Models;
using WalletSystem.Protocol.Requests;
using Npgsql;
using Microsoft.Data.Sqlite;

namespace WalletSystem.Services
{
    public class WalletService : IWalletService
    {
        private readonly WalletContext _context;
        private readonly IDatabase _redisDb;
        private readonly ILogger<WalletService> _logger;

        public WalletService(WalletContext context, IConnectionMultiplexer redis, ILogger<WalletService> logger)
        {
            _context = context;
            _redisDb = redis.GetDatabase();
            _logger = logger;
        }

        public async Task CreateWalletAsync(CreateWalletRequest request)
        {
            _logger.LogInformation("Creating wallet for PlayerId {PlayerId}", request.PlayerId);

            var wallet = new Wallet
            {
                PlayerId = request.PlayerId,
                Balance = 0
            };
            _context.Wallets.Add(wallet);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Wallet created for PlayerId {PlayerId} with WalletId {WalletId}", request.PlayerId, wallet.Id);
        }

        public async Task AddBalanceAsync(AddBalanceRequest request)
        {
            if (string.IsNullOrEmpty(request.IdempotencyKey))
            {
                _logger.LogError("Idempotency-Key is required for AddBalance requests.");
                throw new InvalidOperationException("Idempotency-Key is required for this operation.");
            }

            const int maxRetries = 3;
            int retries = 0;

            while (retries < maxRetries)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                _logger.LogInformation("Attempting to add balance for PlayerId {PlayerId} with Amount {Amount}. Attempt {Attempt}", request.PlayerId, request.Amount, retries + 1);

                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId);
                if (wallet == null)
                {
                    _logger.LogError("Wallet not found for PlayerId {PlayerId}", request.PlayerId);
                    throw new InvalidOperationException("Wallet not found.");
                }

                wallet.Balance += request.Amount;

                try
                {
                    _context.Entry(wallet).OriginalValues["RowVersion"] = wallet.RowVersion;

                    await _context.Transactions.AddAsync(new Transaction
                    {
                        WalletId = wallet.Id,
                        Amount = request.Amount,
                        Timestamp = DateTime.UtcNow,
                        IdempotencyKey = request.IdempotencyKey
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Balance added successfully for PlayerId {PlayerId}. New Balance: {Balance}", request.PlayerId, wallet.Balance);
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    retries++;
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Concurrency conflict detected for PlayerId {PlayerId} on attempt {Attempt}. Retrying...", request.PlayerId, retries);

                    if (retries == maxRetries)
                    {
                        _logger.LogError("Max retries reached for PlayerId {PlayerId}. Could not complete the balance update.", request.PlayerId);
                        throw new InvalidOperationException("Concurrent update conflict. Please try again.");
                    }
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19)
                {
                    _logger.LogWarning("Duplicate transaction detected for IdempotencyKey {IdempotencyKey}", request.IdempotencyKey);
                    throw new InvalidOperationException("Duplicate transaction request detected.");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unexpected error while processing AddBalanceAsync: {Exception}", ex);
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task DeductBalanceAsync(DeductBalanceRequest request)
        {
            if (string.IsNullOrEmpty(request.IdempotencyKey))
            {
                _logger.LogError("Idempotency-Key is required for DeductBalance requests.");
                throw new InvalidOperationException("Idempotency-Key is required for this operation.");
            }

            const int maxRetries = 3;
            int retries = 0;

            while (retries < maxRetries)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                _logger.LogInformation("Attempting to deduct balance for PlayerId {PlayerId} with Amount {Amount}. Attempt {Attempt}", request.PlayerId, request.Amount, retries + 1);

                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId);
                if (wallet == null)
                {
                    _logger.LogError("Wallet not found for PlayerId {PlayerId}", request.PlayerId);
                    throw new InvalidOperationException("Wallet not found.");
                }

                if (wallet.Balance < request.Amount)
                {
                    _logger.LogError("Insufficient funds for PlayerId {PlayerId}. Current Balance: {Balance}, Requested Deduction: {Amount}", request.PlayerId, wallet.Balance, request.Amount);
                    throw new InvalidOperationException("Insufficient funds.");
                }

                wallet.Balance -= request.Amount;

                try
                {
                    _context.Entry(wallet).OriginalValues["RowVersion"] = wallet.RowVersion;

                    await _context.Transactions.AddAsync(new Transaction
                    {
                        WalletId = wallet.Id,
                        Amount = -request.Amount,
                        Timestamp = DateTime.UtcNow,
                        IdempotencyKey = request.IdempotencyKey
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Balance deducted successfully for PlayerId {PlayerId}. New Balance: {Balance}", request.PlayerId, wallet.Balance);
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    retries++;
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Concurrency conflict detected for PlayerId {PlayerId} on attempt {Attempt}. Retrying...", request.PlayerId, retries);

                    if (retries == maxRetries)
                    {
                        _logger.LogError("Max retries reached for PlayerId {PlayerId}. Could not complete the balance deduction.", request.PlayerId);
                        throw new InvalidOperationException("Concurrent update conflict. Please try again.");
                    }
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19)
                {
                    _logger.LogWarning("Duplicate transaction detected for IdempotencyKey {IdempotencyKey}", request.IdempotencyKey);
                    throw new InvalidOperationException("Duplicate transaction request detected.");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unexpected error while processing DeductBalanceAsync: {Exception}", ex);
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<decimal> GetBalanceAsync(GetBalanceRequest request)
        {
            _logger.LogInformation("Fetching balance for PlayerId {PlayerId}", request.PlayerId);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId);
            if (wallet == null)
            {
                _logger.LogError("Wallet not found for PlayerId {PlayerId}", request.PlayerId);
                throw new InvalidOperationException("Wallet not found.");
            }

            _logger.LogInformation("Balance retrieved for PlayerId {PlayerId}: {Balance}", request.PlayerId, wallet.Balance);
            return wallet.Balance;
        }
    }
}