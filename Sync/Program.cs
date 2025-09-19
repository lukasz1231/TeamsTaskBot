using Common.Options;
using Domain.Interfaces;
using Grpc.Net.ClientFactory;
using Common.Data;
using Infrastructure.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

var builder = FunctionsApplication.CreateBuilder(args);

// Service configuration
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));


builder.Services.Configure<AzureOptions>(
    builder.Configuration.GetSection("AzureAd"));
builder.Services.Configure<DefaultValuesOption>(
    builder.Configuration.GetSection("DefaultValues"));
builder.Services.AddSingleton(sp =>
    GraphClientFactory.Create(sp.GetRequiredService<IOptions<AzureOptions>>()));

builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskGraphService, TaskGraphService>();
builder.Services.AddScoped<IUserGraphService, UserGraphService>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();


// Application Insights (optional)
// builder.Services.AddApplicationInsightsTelemetryWorkerService();

var host = builder.Build();

// Host run
await host.RunAsync();
