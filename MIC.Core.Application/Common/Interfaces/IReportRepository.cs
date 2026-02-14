namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for generated reports.
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// Adds a new report.
    /// </summary>
    Task AddAsync(dynamic report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a report by ID.
    /// </summary>
    Task<dynamic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reports with paging.
    /// </summary>
    Task<List<dynamic>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a report by ID.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reports.
    /// </summary>
    Task<List<dynamic>> GetAllAsync(CancellationToken cancellationToken = default);
}
