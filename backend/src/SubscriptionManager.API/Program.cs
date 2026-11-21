// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using Microsoft.EntityFrameworkCore;
using SubscriptionManager.API.Infrastructure;
using SubscriptionManager.API.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB upload limit.
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<ArgumentExceptionHandler>();
builder.Services.AddExceptionHandler<DbUpdateExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton(TimeProvider.System);

var connectionString = DatabaseConnectionStringProvider.Resolve(
    builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddDbContext<SubscriptionManagerDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// This API has no authentication, so it must never be reachable from the network —
// fail fast at startup instead of silently listening somewhere it shouldn't.
StartupSecurity.EnsureLoopbackOnly(app.Urls);

app.UseHostFiltering();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

// The Testing environment swaps in its own in-memory database, created directly via
// EnsureCreated() — applying migrations here as well would race against that setup.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SubscriptionManagerDbContext>();
    var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    DatabaseMigrationService.ApplyPendingMigrations(dbContext, connectionString, timeProvider);
}

app.Run();