using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RosterSync.Api;
using RosterSync.Api.Endpoints;
using RosterSync.Api.Endpoints.Authentication;
using RosterSync.Api.Endpoints.Authentication.Internals;
using RosterSync.Api.Endpoints.Calendars;
using RosterSync.Api.Endpoints.SyncConfigs;
using RosterSync.Core;
using RosterSync.Core.Internals.Google;
using RosterSync.Core.Internals.Google.Calendar;
using RosterSync.Core.SyncConfig;
using RosterSync.Model;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["Auth:FrontendUrl"]!)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddOpenApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Auth:Jwt:Secret"]!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

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
builder.Services.AddScoped<ISyncConfigService, SyncConfigService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection("Auth"));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.AddRosterEndpoints()
    .AddAuthenticationEndpoints()
    .AddSyncConfigEndpoints()
    .AddCalendarEndpoints();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RosterSyncDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();