using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Backend.Data;
using Backend.Middleware;
using Backend.Services;
using Backend.Services.RabbitMQ;
using Backend.Services.Workflow;
using Backend.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/backend-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Converters.Add(new Backend.Services.Json.ObjectToInferredTypesConverter());
    });

// Configure PostgreSQL Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        )
    ));

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  // Local frontend (Vite) — localhost / 127.0.0.1 / ::1
                  if (Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                      (uri.Host is "localhost" or "127.0.0.1" or "::1"))
                  {
                      return true;
                  }

                  // Extra origins from config (e.g. https://app.example.com) when API is called cross-origin
                  if (allowedOrigins.Length > 0 &&
                      allowedOrigins.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase)))
                  {
                      return true;
                  }

                  return false;
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register Security Services
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddSingleton<IPasswordHashService, PasswordHashService>();
builder.Services.AddSingleton<Backend.Services.RabbitMQ.IRabbitMQMessageEncryptionService, Backend.Services.RabbitMQ.RabbitMQMessageEncryptionService>();

// Configure Rate Limiting (защита от brute-force)
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
    options.RejectionStatusCode = 429;
});

// Register RabbitMQ Service
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddHttpClient(nameof(Backend.Services.RabbitMQ.RabbitMQManagementService));
builder.Services.AddSingleton<Backend.Services.RabbitMQ.IRabbitMQManagementService, Backend.Services.RabbitMQ.RabbitMQManagementService>();

// Host + agent lifecycle
builder.Services.AddScoped<Backend.Services.Hosts.IHostProvisioningService, Backend.Services.Hosts.HostProvisioningService>();
builder.Services.AddScoped<Backend.Services.Hosts.StandardHostAgentCommandService>();
builder.Services.AddScoped<Backend.Services.Hosts.SpecificHostAgentCommandService>();
builder.Services.AddScoped<Backend.Services.Hosts.IHostAgentCommandDispatcher, Backend.Services.Hosts.HostAgentCommandDispatcher>();
builder.Services.AddScoped<Backend.Services.Agents.IAgentLifecycleService, Backend.Services.Agents.AgentLifecycleService>();

// Register Repositories
builder.Services.AddScoped<IWFListRepository, WFListRepository>();
builder.Services.AddScoped<ITimetableRepository, TimetableRepository>();

// Register Workflow Services
builder.Services.AddScoped<IWorkflowExecutionService, WorkflowExecutionService>();
builder.Services.AddScoped<IMessageProcessingService, MessageProcessingService>();
builder.Services.AddScoped<IParameterValidationService, ParameterValidationService>();

// Register Workflow Functions (для выполнения в workflow)
builder.Services.AddScoped<Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions>();
builder.Services.AddScoped<Backend.Services.WorkflowFunctions.RabbitMQFunctions>();
builder.Services.AddScoped<Backend.Services.WorkflowFunctions.AgentInfoFunctions>();
builder.Services.AddScoped<Backend.Services.WorkflowFunctions.TimeTableFunctions>();
builder.Services.AddScoped<Backend.Services.WorkflowFunctions.WFListFunctions>();
builder.Services.AddScoped<Backend.Services.WorkflowFunctions.TechProcessFunctions>();
builder.Services.AddScoped<Backend.Services.WorkflowFunctions.WorkflowControlFunctions>();
builder.Services.AddScoped<Backend.Services.WorkflowFunctions.PlanningFunctions>();

// Drawing analysis (OrcaRouter GLM free / Gemini vision)
builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});
builder.Services.AddHttpClient("OrcaRouter", client =>
{
    client.Timeout = TimeSpan.FromMinutes(4);
});
builder.Services.AddScoped<Backend.Services.Drawings.IDrawingAnalysisService, Backend.Services.Drawings.GeminiDrawingAnalysisService>();
builder.Services.AddScoped<Backend.Services.Settings.IAppSettingsService, Backend.Services.Settings.AppSettingsService>();

// Register RabbitMQ Background Service
builder.Services.AddHostedService<RabbitMQHostedService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Factory Agent Backend API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Factory Agent Backend API v1");
    });
}

app.UseSerilogRequestLogging();

app.UseRateLimiter();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "OK", timestamp = DateTime.UtcNow }));

// Configure port — 0.0.0.0 принимает и localhost, и 127.0.0.1 (без двойного bind)
var port = builder.Configuration.GetValue<int?>("Port") ?? 3001;
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

Log.Information("Starting Factory Agent Backend on port {Port}", port);

// Dev seed + password migration on startup
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
    var passwordHashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();

    // Schema patches first so later scripts/API can use new tables
    await Backend.Scripts.EnsureDrawingAnalysesTable.EnsureAsync(context);
    await Backend.Scripts.EnsureAppSettingsTable.EnsureAsync(context);

    if (app.Environment.IsDevelopment())
    {
        await Backend.Scripts.EnsureDevAdminScript.EnsureAsync(context, encryptionService, passwordHashService);
    }

    var migrationScript = new Backend.Scripts.MigrateUserPasswordsScript(context, encryptionService, passwordHashService);
    await migrationScript.MigrateAsync();
}
catch (Exception ex)
{
    Log.Warning(ex, "Startup DB bootstrap failed (non-critical)");
}

app.Run();
