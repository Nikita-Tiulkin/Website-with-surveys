using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using PollingSystem.Data;
using PollingSystem.Messaging;
using PollingSystem.Worker.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string? connectionString = builder.Configuration["DATABASE_URL"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Не задан DATABASE_URL.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IEventPublisher>(_ =>
    new RabbitMqEventPublisher(
        builder.Configuration["RabbitMq:Host"] ?? "localhost",
        int.TryParse(builder.Configuration["RabbitMq:Port"], out int rabbitPort)
            ? rabbitPort
            : 5672,
        builder.Configuration["RabbitMq:UserName"] ?? "guest",
        builder.Configuration["RabbitMq:Password"] ?? "guest"));

builder.Services.AddHostedService<SurveyEventConsumer>();
builder.Services.AddHostedService<ExpiredSurveyCloser>();

await builder.Build().RunAsync();
