using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Testcontainers.PostgreSql;

namespace PlayerApiTests;

public class PlayerApiTests
{
    [Fact]
    public async Task RetrievesAllPlayersForAGivenAgeGroup()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres")
            .WithName("player-db")
            // .WithEnvironment("POSTGRES_PASSWORD", "pass")
            .WithPortBinding(5432, 5432)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(5432))
            .Build();

        await container.StartAsync()
            .ConfigureAwait(false);

        var postgresConnection = new NpgsqlConnection(container.GetConnectionString());
        var createTableCommand = postgresConnection.CreateCommand();
        createTableCommand.CommandText =
            "create table if not exists Players(Id uuid, Name varchar, DateOfBirth DATE NOT NULL)";
        createTableCommand.Connection?.Open();
        createTableCommand.ExecuteNonQuery();
        var x = "tom";


        // var dbContextOptionsBuilder = new DbContextOptionsBuilder();
        // dbContextOptionsBuilder.UseNpgsql(@"Host=localhost;Username=postgres;Password=pass;Database=postgres");
        // var dbContext = new PlayerDbContext(dbContextOptionsBuilder.Options);

    }
}

record Player(Guid Id, string Name, DateOnly DateOfBirth);

class PlayerDbContext : DbContext
{
    public PlayerDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
}