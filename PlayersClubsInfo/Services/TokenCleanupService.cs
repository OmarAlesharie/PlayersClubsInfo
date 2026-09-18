using PlayersClubsInfo.Data;
using Microsoft.EntityFrameworkCore;

namespace PlayersClubsInfo.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(30); // adjust as needed

        public TokenCleanupService(IServiceProvider services, ILogger<TokenCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TokenCleanupService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during token cleanup.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
            _logger.LogInformation("TokenCleanupService stopping.");
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PlayersClubsInfoContext>();

            // Remove expired refresh tokens (safe)
            var expiredRefreshTokens = await db.RefreshTokens
                .Where(t => t.Expires <= DateTime.UtcNow)
                .ToListAsync(ct);

            if (expiredRefreshTokens.Count > 0)
            {
                db.RefreshTokens.RemoveRange(expiredRefreshTokens);
            }

            // Optionally: remove revoked refresh tokens older than X days
            var retentionDays = 30;
            var oldRevoked = await db.RefreshTokens
                .Where(t => t.Revoked && t.RevokedAt != null && t.RevokedAt <= DateTime.UtcNow.AddDays(-retentionDays))
                .ToListAsync(ct);

            if (oldRevoked.Count > 0)
            {
                db.RefreshTokens.RemoveRange(oldRevoked);
            }

            // Remove expired revoked access tokens
            var expiredRevokedJtis = await db.RevokedAccessTokens
                .Where(r => r.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync(ct);

            if (expiredRevokedJtis.Count > 0)
            {
                db.RevokedAccessTokens.RemoveRange(expiredRevokedJtis);
            }

            if (expiredRefreshTokens.Count + oldRevoked.Count + expiredRevokedJtis.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Token cleanup removed {r} expired refresh tokens, {o} old revoked refresh tokens, {j} expired revoked JTIs.",
                    expiredRefreshTokens.Count, oldRevoked.Count, expiredRevokedJtis.Count);
            }
        }
    }
}