using Microsoft.EntityFrameworkCore;
using OutboxPattern.Api.BackgroundServices;
using OutboxPattern.Api.Data;
using OutboxPattern.Api.Services;


var builder = WebApplication.CreateBuilder(args);

// ─── EF Core (In-Memory for demo; swap connection string for SQL Server) ────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("OutboxPatternDemo"));

// ─── Application Services ───────────────────────────────────────────────────
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Singleton broker so all scopes see the same published messages list
builder.Services.AddSingleton<IMessageBroker, FakeMessageBroker>();

// ─── Outbox Relay Background Worker ─────────────────────────────────────────
builder.Services.AddHostedService<OutboxRelayWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Outbox Pattern Demo API",
        Version = "v1",
        Description = """
            Demonstrates the Transactional Outbox Pattern in .NET 8.

            Flow:
              POST /api/orders          → creates Order + OutboxMessage atomically
              GET  /api/outbox          → inspect all outbox messages
              GET  /api/outbox/pending  → messages not yet relayed
              GET  /api/outbox/stats    → summary statistics
              GET  /api/outbox/published→ events delivered to the (fake) broker
            """
    });
});


var app = builder.Build();

// ─── Seed demo data ──────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Outbox Pattern Demo v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
