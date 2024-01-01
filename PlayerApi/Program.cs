using DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/players", (PlayerDbContext dbContext, int ageGroup) =>
{
    bool PlayerIsCorrectAge(Player player)
    {
        var firstJanAgeGroupNumberOfYearsAgo = 
            new DateOnly(DateTime.Now.Year - ageGroup, 1, 1);
        if (ageGroup < 10)
        {
            return player.DateOfBirth > firstJanAgeGroupNumberOfYearsAgo;
        }

        var firstJanAgeGroupNumberOfYearsAgoPlus2Years = 
            new DateOnly(DateTime.Now.Year - ageGroup + 2, 1, 1);
        return player.DateOfBirth > firstJanAgeGroupNumberOfYearsAgo && 
               player.DateOfBirth <= firstJanAgeGroupNumberOfYearsAgoPlus2Years;

    }
    return dbContext.Players.Where(PlayerIsCorrectAge).ToList();
});


app.Run();

public record Player(Guid Id, string Name, DateOnly DateOfBirth);

namespace DB
{
    public class PlayerDbContext : DbContext
    {
        public PlayerDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Player> Players { get; set; }
    }
}

public partial class Program
{
}