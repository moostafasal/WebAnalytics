using Microsoft.EntityFrameworkCore;
using WebAnalytics.Consumer;
using WebAnalytics.Infrastructure.Data;
using WebAnalytics.Infrastructure.MessageBroker;
using WebAnalytics.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add database services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add RabbitMQ services
builder.Services.AddSingleton<RabbitMQService>();

// Add application services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Add Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();