using Application.Services;
using Common.Data;
using Common.Middleware;
using Common.Options;
using Common.Services;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Services;
using Infrastructure.Services.ActionHandlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;



var builder = WebApplication.CreateBuilder(args);

// authentication
var tenantId = builder.Configuration["AzureAd:TenantId"];
var clientId = builder.Configuration["AzureAd:ClientId"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => 
    { options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0"; 
        options.Audience = $"api://{clientId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[] {
        $"https://sts.windows.net/{tenantId}/",
        $"https://login.microsoftonline.com/{tenantId}/v2.0"
    },
            ValidateAudience = true,
            ValidateLifetime = true
        };
    });

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));


builder.Services.Configure<AzureOptions>(
    builder.Configuration.GetSection("AzureAd"));
builder.Services.Configure<BotOptions>(
    builder.Configuration.GetSection("BotOptions"));
builder.Services.Configure<DefaultValuesOption>(
    builder.Configuration.GetSection("DefaultValues"));
builder.Services.Configure<AzureAiOptions>(
    builder.Configuration.GetSection("AzureAiOptions"));

builder.Services.AddSingleton(sp =>
    GraphClientFactory.Create(sp.GetRequiredService<IOptions<Common.Options.AzureOptions>>()));

// Client Agents (singleton)
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<Common.Options.AzureOptions>>();
    return GraphClientFactory.CreateAzureAiClient(options);
});


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserGraphService, UserGraphService>();
builder.Services.AddScoped<ISyncService, SyncService>();

builder.Services.AddScoped<IGraphMetadataService, GraphMetadataService>();

builder.Services.AddScoped<ITeamsMessageHandler, TeamsMessageHandler>(); 

builder.Services.AddScoped<ITaskGraphService, TaskGraphService>();

builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();

builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IBotService, BotService>();
builder.Services.AddScoped<IAgentClientService, AgentClientService>();
builder.Services.AddScoped<RegexMessageParser>();
builder.Services.AddScoped<Application.Services.OpenAIMessageParser>();
builder.Services.AddScoped<IMessageParser, CompositeMessageParser>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskFacade, TaskFacade>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAdaptiveCardService, AdaptiveCardService>();
builder.Services.AddScoped<IBot, MyBot>();

/*builder.Services.AddSingleton<ICredentialProvider, MySimpleCredentialProvider>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var appId = configuration["MicrosoftAppId"];
    var appPassword = configuration["MicrosoftAppPassword"];
    return new MySimpleCredentialProvider(appId, appPassword);
});*/

builder.Services.AddSingleton<IBotFrameworkHttpAdapter, BotAdapterWithErrorHandler>();



// Action Handlers and Strategies
builder.Services.AddScoped<IActionHandler, ActionHandler>();
builder.Services.AddScoped<ActionHandlerFactory>(); 
builder.Services.AddSingleton<List<PendingAction>>();
builder.Services.AddScoped<IActionHandlerStrategy, AddTaskHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, CommentTaskHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, GetOverallReportHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, SmalltalkHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, UnknownActionHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, GetTaskReportHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, GetUserReportHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, AddTimeToTaskHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, StopTaskHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, StartTaskHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, ParsedNumbersHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, UpdateTaskHandlerStrategy>();
builder.Services.AddScoped<IActionHandlerStrategy, DeleteTaskHandlerStrategy>();



builder.Services.AddHttpClient();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"),

                Scopes = new Dictionary<string, string>
                {
                    { $"api://{clientId}/App.ReadWrite", "Read/Write Access to your API" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new [] { $"api://{clientId}/App.ReadWrite" }
        }
    });
});






var app = builder.Build();
app.UseMiddleware<ApiKeyAndSafeHostMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API V1");
        c.OAuthClientId("076a8358-8cd1-4b0c-9b4a-653c6f0550be");
        c.OAuthScopes("api://076a8358-8cd1-4b0c-9b4a-653c6f0550be/App.ReadWrite");
        c.OAuthUsePkce();
    });
}

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
