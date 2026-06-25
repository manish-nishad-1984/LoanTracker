using FluentValidation;
using LoanTracker.Api.Middleware;
using LoanTracker.Application.DTOs.Lenders;
using LoanTracker.Application.DTOs.Loans;
using LoanTracker.Application.DTOs.Payments;
using LoanTracker.Infrastructure.Extensions;
using LoanTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/loantracker-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ── Services ─────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LoanTracker API", Version = "v1" });
});

// Infrastructure (DbContext + all services)
builder.Services.AddInfrastructure(builder.Configuration);

// FluentValidation
builder.Services.AddScoped<IValidator<CreateLenderRequest>, CreateLenderValidator>();
builder.Services.AddScoped<IValidator<UpdateLenderRequest>, UpdateLenderValidator>();
builder.Services.AddScoped<IValidator<CreateLoanRequest>, CreateLoanValidator>();
builder.Services.AddScoped<IValidator<UpdateLoanRequest>, UpdateLoanValidator>();
builder.Services.AddScoped<IValidator<UpdateInterestRateRequest>, UpdateInterestRateValidator>();
builder.Services.AddScoped<IValidator<CreatePaymentRequest>, CreatePaymentValidator>();
builder.Services.AddScoped<IValidator<UpdatePaymentRequest>, UpdatePaymentValidator>();

// CORS — allow React dev server + production origins.
// Extra origins can be supplied via the Cors__AllowedOrigins config array / env.
var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(new[]
            {
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:80",
                "http://motiwala.pratishthabridal.com",
                "https://motiwala.pratishthabridal.com",
            }.Concat(configuredOrigins).ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LoanTracker API v1"));
}

app.UseCors();
app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate on startup ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migration completed.");

        // Sample-data seeding disabled — the app now holds real user data.
        // To re-enable demo seeding on an empty database, uncomment below:
        // var seedLogger = scope.ServiceProvider
        //     .GetRequiredService<ILoggerFactory>()
        //     .CreateLogger("DbInitializer");
        // await DbInitializer.SeedAsync(db, seedLogger);
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Database migration failed.");
        throw;
    }
}

await app.RunAsync();
