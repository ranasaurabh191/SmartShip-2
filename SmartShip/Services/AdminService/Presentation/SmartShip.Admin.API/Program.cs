using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using Serilog;
using SmartShip.Admin.Application.Services;
using SmartShip.Admin.Application.Validators;
using SmartShip.Admin.Infrastructure.Context;
using SmartShip.Admin.Infrastructure.Persistence;
using SmartShip.Admin.Infrastructure.Repositories;
using System.Text;
using SmartShip.Admin.Infrastructure.Consumers;
using SmartShip.Shared.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
try
{
    Log.Information(" --> Starting AdminService API...");
    var builder = WebApplication.CreateBuilder(args); 

    // Add services to the container.
    builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AdminService")
    .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

    
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
                return new BadRequestObjectResult(new { message = "Validation failed.", errors });
            };
        });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateHubValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<ReportValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<UpdateHubValidator>();

    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<UserCreatedConsumer>();
        x.AddConsumer<UserDeletedConsumer>();
        x.AddConsumer<ShipmentCreatedMetricsConsumer>();
        x.AddConsumer<ShipmentDeliveredConsumer>();
        x.AddConsumer<ShipmentCancelledConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });
            cfg.ReceiveEndpoint("admin-shipment-delivered", e =>
            {
                e.ConfigureConsumer<ShipmentDeliveredConsumer>(context);
            });
            cfg.ReceiveEndpoint("admin-user-created", e =>
            {
                e.ConfigureConsumer<UserCreatedConsumer>(context);
            });
            cfg.ReceiveEndpoint("admin-user-deleted", e =>
            {
                e.ConfigureConsumer<UserDeletedConsumer>(context);
            });
            cfg.ReceiveEndpoint("admin-shipment-created", e =>
            {
                e.ConfigureConsumer<ShipmentCreatedMetricsConsumer>(context);
            });
            cfg.ReceiveEndpoint("admin-shipment-cancelled", e =>
            {
                e.ConfigureConsumer<ShipmentCancelledConsumer>(context);
            });
        });
    });
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Admin Service",
            Version = "v1"
        });

        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter your token."
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });


    builder.Services.AddDbContext<AdminDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    var jwt = builder.Configuration.GetSection("JwtSettings");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt => opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        });

    builder.Services.AddAuthorization();

    builder.Services.AddScoped<IHubRepository, HubRepository>();
    builder.Services.AddScoped<IReportRepository, ReportRepository>();
    builder.Services.AddScoped<IDashboardMetricsRepository, DashboardMetricsRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddCors(opt => opt.AddPolicy("AllowAll", p => p.WithOrigins("http://localhost:5000").AllowAnyHeader().AllowAnyMethod()));

    builder.Services.AddSingleton<IConnection>(sp =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var factory = new ConnectionFactory
        {
            Uri = new Uri($"amqp://guest:guest@{host}:5672"),
            AutomaticRecoveryEnabled = true
        };
        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
    });

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms");

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AdminDbContext>().Database.Migrate();
    }
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, " !! AdminService crashed on startup.");
}
finally
{
    Log.CloseAndFlush();
}