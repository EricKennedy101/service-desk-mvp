using System.Text;
using System.Text.Json.Serialization;
using FRAServiceRequestPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var isPortalOnly = !string.IsNullOrWhiteSpace(builder.Configuration["BackendApi:BaseUrl"]);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
if (!isPortalOnly)
{
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDbContext<SqlServerAppDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
    else
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
}
builder.Services.AddHttpClient("BackendApi", client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl)
        && Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }
});
builder.Services.AddScoped<FRAServiceRequestPortal.Services.BackendApiClient>();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
if (!isPortalOnly)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var key = builder.Configuration["Jwt:Key"] ?? string.Empty;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            };
        });
    builder.Services.AddAuthorization();
}

var app = builder.Build();
var serviceDeskApiKey = builder.Configuration["ServiceDesk:ApiKey"];

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!isPortalOnly && (app.Environment.IsDevelopment() || app.Environment.IsStaging()))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseSession();
app.UseCors("Default");
if (!isPortalOnly)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

if (!isPortalOnly && !string.IsNullOrWhiteSpace(serviceDeskApiKey))
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api/tickets", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.Request.Headers.TryGetValue("X-API-Key", out var providedKey) ||
                !string.Equals(providedKey.ToString(), serviceDeskApiKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next();
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/db", async (IConfiguration configuration, FRAServiceRequestPortal.Services.BackendApiClient backendClient) =>
{
    if (isPortalOnly)
    {
        var result = await backendClient.GetDbHealthAsync();
        return result.ok
            ? Results.Ok(new { db = "ok" })
            : Results.Json(new { db = "fail", error = result.error }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Json(new { db = "fail", error = "Missing DefaultConnection." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync();
        return Results.Ok(new { db = "ok" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { db = "fail", error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

if (!isPortalOnly)
{
    app.MapControllers();
}
app.MapRazorPages();

app.Run();

public partial class Program { }
