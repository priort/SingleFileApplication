using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace PlayerApiTests;

public class PlayerApiTests : IClassFixture<ApiTestServer<Program>>
{
    private readonly ApiTestServer<Program> _testServer;
    
    public PlayerApiTests(ApiTestServer<Program> testServer)
    {
        _testServer = testServer;
    }

    [Fact]
    public async Task RetrievesAllPlayersForAGivenAgeGroup()
    {
        var httpClient = _testServer.CreateClient();
        using var responseU8S = await httpClient.GetAsync("/players?ageGroup=8");
        responseU8S.EnsureSuccessStatusCode();

        var responseBodyU8S = await responseU8S   .Content.ReadAsStringAsync();
        List<Player> playersU8S = JsonSerializer.Deserialize<List<Player>>(responseBodyU8S)!
            .OrderBy(p => p.DateOfBirth).ToList();
        Assert.Equal(4, playersU8S.Count);

        var playerNamesDoBsU8S = playersU8S.Select(p => (p.Name, p.DateOfBirth)).ToList();
        
        Assert.Equal(
            ("Susan", new DateOnly(2016, 1, 2)), 
            playerNamesDoBsU8S[0]);
        Assert.Equal(
            ("Bill", new DateOnly(2016, 2, 4)), 
            playerNamesDoBsU8S[1]);
        Assert.Equal(
            ("Mary", new DateOnly(2016, 4, 3)), 
            playerNamesDoBsU8S[2]);
        Assert.Equal(
            ("James", new DateOnly(2017, 1, 5)), 
            playerNamesDoBsU8S[3]);

        using var responseU10S = await httpClient.GetAsync("/players?ageGroup=10");
        responseU10S.EnsureSuccessStatusCode();

        var responseBodyU10S = await responseU10S.Content.ReadAsStringAsync();
        if (responseBodyU10S == null) throw new ArgumentNullException(nameof(responseBodyU10S));
        List<Player> playersU10S = JsonSerializer.Deserialize<List<Player>>(responseBodyU10S)!
            .OrderBy(p => p.DateOfBirth).ToList();
        Assert.Equal(5, playersU10S.Count);

        var playerNamesDoBsU10S = playersU10S.Select(p => (p.Name, p.DateOfBirth)).ToList();
        
        Assert.Equal(("John", new DateOnly(2014, 1, 2)),
            playerNamesDoBsU10S[0]);
        Assert.Equal(("Emily", new DateOnly(2014, 1, 3)),
            playerNamesDoBsU10S[1]);
        Assert.Equal(("Elliot", new DateOnly(2014, 2, 3)),
            playerNamesDoBsU10S[2]);
        Assert.Equal(("Fergal", new DateOnly(2015, 4, 2)),
            playerNamesDoBsU10S[3]);
        Assert.Equal(("Emma", new DateOnly(2016, 1, 1)),
            playerNamesDoBsU10S[4]);
        
        
        
        using var responseU12S = await httpClient.GetAsync("/players?ageGroup=12");
        responseU12S.EnsureSuccessStatusCode();

        var responseBodyU12S = await responseU12S.Content.ReadAsStringAsync();
        if (responseBodyU12S == null) throw new ArgumentNullException(nameof(responseBodyU12S));
        List<Player> playersU12S = JsonSerializer.Deserialize<List<Player>>(responseBodyU12S)!
            .OrderBy(p => p.DateOfBirth).ToList();
        Assert.Equal(5, playersU12S.Count);

        var playerNamesDoBsU12S = playersU12S.Select(p => (p.Name, p.DateOfBirth)).ToList();
        Assert.Equal(("Maeve", new DateOnly(2013, 2, 4)),playerNamesDoBsU12S[0]);
        Assert.Equal(("Mark", new DateOnly(2013, 4, 3)),playerNamesDoBsU12S[1]);
        Assert.Equal(("Ben", new DateOnly(2013, 7, 8)),playerNamesDoBsU12S[2]);
        Assert.Equal(("Anna", new DateOnly(2013, 12, 31)),playerNamesDoBsU12S[3]);
        Assert.Equal(("Grace", new DateOnly(2014, 1, 1)),playerNamesDoBsU12S[4]);
    }
}

record Player(
    [property: JsonPropertyName("id")] Guid Id, 
    [property: JsonPropertyName("name")]string Name, 
    [property: JsonPropertyName("dateOfBirth")]DateOnly DateOfBirth);

class PlayerDbContext : DbContext
{
    public PlayerDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
}

public class ApiTestServer<T> : WebApplicationFactory<T>, IAsyncLifetime where T : class
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres")
        .WithName("player-db")
        .WithPortBinding(5432, 5432)
        .WithWaitStrategy(
            Wait.ForUnixContainer()
                .UntilPortIsAvailable(5432))
        .Build();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DB.PlayerDbContext));
            services.AddDbContext<DB.PlayerDbContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));
        });
        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync()
            .ConfigureAwait(false);
        SetUpDB();
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

    public Task DisposeAsync() => _postgresContainer.DisposeAsync().AsTask();
}
