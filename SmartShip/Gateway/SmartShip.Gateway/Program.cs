using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information(" --> Starting SmartShip Gateway....");

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json",
                      optional: true, reloadOnChange: true);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Gateway")
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

    var jwt = builder.Configuration.GetSection("JwtSettings");
    var jwtKey = jwt["Key"];
    var jwtIssuer = jwt["Issuer"];
    var jwtAudience = jwt["Audience"];

    if (string.IsNullOrWhiteSpace(jwtKey))
        throw new InvalidOperationException("JwtSettings:Key is missing.");

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", opt =>
        {
            opt.RequireHttpsMetadata = false;
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    builder.Services.AddCors(opt =>
        opt.AddPolicy("AllowAll", p =>
            p.WithOrigins("http://localhost:3000")
             .AllowAnyHeader()
             .AllowAnyMethod()));

    builder.Services.AddHttpClient("HealthCheckClient", c =>
    {
        c.Timeout = TimeSpan.FromSeconds(3);
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Gateway",
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

    builder.Services.AddOcelot(builder.Configuration);//registers
    builder.Services.AddSwaggerForOcelot(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate =
            "GATEWAY {RequestMethod} {RequestPath} -> {StatusCode} in {Elapsed:0.0000}ms");

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSwagger();
    app.UseSwaggerForOcelotUI(opt =>
    {
        opt.PathToSwaggerGenerator = "/swagger/docs";
    },
    uiOpt =>
    {
        uiOpt.OAuthClientId("swagger-ui");
        uiOpt.OAuthAppName("SmartShip Swagger UI");
        uiOpt.OAuthUsePkce();
        uiOpt.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });

    app.MapGet("/", () => " --> SmartShip Gateway Running");

    app.UseWhen(
        ctx => ctx.Request.Path.StartsWithSegments("/gateway"),
        ocelotBranch => ocelotBranch.UseOcelot().Wait()//runs 
    );

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, " !! Gateway crashed on startup.");
}
finally
{
    Log.CloseAndFlush();
}