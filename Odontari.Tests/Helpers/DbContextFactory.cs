using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;

namespace Odontari.Tests.Helpers;

/// <summary>
/// Crea un ApplicationDbContext respaldado por SQLite en memoria.
/// La conexión se mantiene abierta durante la vida del objeto para que
/// la base de datos persista entre operaciones dentro del mismo test.
/// </summary>
public sealed class DbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public DbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
