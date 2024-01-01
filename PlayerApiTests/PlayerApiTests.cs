using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace PlayerApiTests;

public class PlayerApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres")
        .WithName("player-db")
        .WithPortBinding(5432, 5432)
        .WithWaitStrategy(
            Wait.ForUnixContainer()
                .UntilPortIsAvailable(5432))
        .Build(); 
    
    [Fact]
    public async Task RetrievesAllPlayersForAGivenAgeGroup()
    {
        var x = "tom";
    }

    private void SetUpDB()
    {
        var postgresConnection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        var createTableCommand = postgresConnection.CreateCommand();
        createTableCommand.CommandText =
            """CREATE TABLE IF NOT EXISTS "Players"("Id" uuid, "Name" varchar, "DateOfBirth" DATE NOT NULL)""";
        createTableCommand.Connection?.Open();
        createTableCommand.ExecuteNonQuery();
        
        var dbContextOptionsBuilder = new DbContextOptionsBuilder();
        dbContextOptionsBuilder.UseNpgsql(_postgresContainer.GetConnectionString());
        var dbContext = new PlayerDbContext(dbContextOptionsBuilder.Options);
        AddUnder8s(dbContext);
        AddUnder10s(dbContext);
        AddUnder12s(dbContext);
        dbContext.SaveChanges();
    }

    private void AddUnder8s(PlayerDbContext dbContext)
    {
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Susan", new DateOnly(2016, 1, 2)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Mary", new DateOnly(2016, 4, 3)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Bill", new DateOnly(2016, 2, 4)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "James", new DateOnly(2017, 1, 5)));
    }
    
    private void AddUnder10s(PlayerDbContext dbContext)
    {
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Emma", new DateOnly(2016, 1, 1)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "John", new DateOnly(2014, 1, 2)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Emily", new DateOnly(2014, 1, 3)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Elliot", new DateOnly(2014, 2, 3)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Fergal", new DateOnly(2015, 4, 2)));
    }

    private void AddUnder12s(PlayerDbContext dbContext)
    {
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Grace", new DateOnly(2014, 1, 1)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Anna", new DateOnly(2013, 12, 31)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Mark", new DateOnly(2013, 4, 3)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Maeve", new DateOnly(2013, 2, 4)));
        dbContext.Players.Add(new Player(Guid.NewGuid(), "Ben", new DateOnly(2013, 7, 8)));
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync()
            .ConfigureAwait(false);
        SetUpDB();
    }

    public Task DisposeAsync() => _postgresContainer.DisposeAsync().AsTask();
}

record Player(Guid Id, string Name, DateOnly DateOfBirth);

class PlayerDbContext : DbContext
{
    public PlayerDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
}