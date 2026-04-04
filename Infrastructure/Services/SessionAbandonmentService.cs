using Application;
using Domain.Model.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class SessionAbandonmentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionAbandonmentService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AbandonAfter = TimeSpan.FromHours(1);

    public SessionAbandonmentService(
        IServiceScopeFactory scopeFactory,
        ILogger<SessionAbandonmentService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AbandonStaleSessions(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error abandoning stale sessions");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task AbandonStaleSessions(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - AbandonAfter;

        var staleSessions = await db.UserSessions
            .Where(s => s.Status == SessionStatus.InProgress && s.StartedAt < cutoff)
            .ToListAsync(ct);

        if (staleSessions.Count == 0) return;

        foreach (var session in staleSessions)
            session.Abandon();

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Abandoned {Count} stale sessions", staleSessions.Count);
    }
}
