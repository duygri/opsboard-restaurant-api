using Microsoft.EntityFrameworkCore;
using OpsBoard.Application.Tables;
using OpsBoard.Infrastructure.Persistence;

namespace OpsBoard.Infrastructure.Services;

public sealed class TableQueryService
{
    private readonly OpsBoardDbContext _dbContext;

    public TableQueryService(OpsBoardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TableResponse>> GetTablesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.RestaurantTables
            .OrderBy(table => table.Name)
            .Select(table => new TableResponse(table.Id, table.Name, table.Status))
            .ToArrayAsync(cancellationToken);
    }
}
