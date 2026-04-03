using Microsoft.EntityFrameworkCore;
using RosterSync.Api;
using RosterSync.Api.Endpoints;
using RosterSync.Core;
using RosterSync.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("MariaDb");
var serverVersion = ServerVersion.AutoDetect(connectionString);
builder.Services.AddDbContext<RosterSyncDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddDbContextFactory<RosterSyncDbContext>(options =>
    options.UseMySql(connectionString, serverVersion), 
    ServiceLifetime.Scoped);

builder.Services.AddScoped<IDbContext>(sp =>
    sp.GetRequiredService<RosterSyncDbContext>());

builder.Services.AddHttpClient<RosterScraper>();
builder.Services.AddTransient<IRosterScraper, RosterScraper>();


var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.AddRosterEndpoints();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RosterSyncDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();