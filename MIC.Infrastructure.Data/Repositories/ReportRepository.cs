using MIC.Core.Application.Common.Interfaces;

namespace MIC.Infrastructure.Data.Repositories;

/// <summary>
/// In-memory report repository implementation for v1.0.
/// In future versions, this will use EF Core persistence.
/// </summary>
public class ReportRepository : IReportRepository
{
    private static readonly List<dynamic> _reports = new();

    public Task AddAsync(dynamic report, CancellationToken cancellationToken = default)
    {
        _reports.Add(report);
        return Task.CompletedTask;
    }

    public Task<dynamic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = _reports.FirstOrDefault(r => (Guid)r.Id == id);
        return Task.FromResult(report);
    }

    public Task<List<dynamic>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = _reports
            .OrderByDescending(r => (DateTime)r.GeneratedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(paged);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = _reports.FirstOrDefault(r => (Guid)r.Id == id);
        if (report != null)
        {
            _reports.Remove(report);
        }

        return Task.CompletedTask;
    }

    public Task<List<dynamic>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = _reports.OrderByDescending(r => (DateTime)r.GeneratedAt).ToList();
        return Task.FromResult(all);
    }
}
