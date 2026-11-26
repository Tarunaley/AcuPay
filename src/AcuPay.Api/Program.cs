using System.Text;
using AcuPay.Api.Dtos;
using AcuPay.Api.Middleware;
using AcuPay.Api.Security;
using AcuPay.Data;
using AcuPay.Domain;
using AcuPay.Domain.Abstractions;
using AcuPay.Domain.Rules;
using AcuPay.Domain.Services;
using AcuPay.Domain.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging: every line carries the correlation id (added as a scope by CorrelationIdMiddleware)
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.IncludeScopes = true;
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});

// ---- Configuration: constants from appsettings.json, secrets from user-secrets / environment only
var settings = builder.Configuration.GetSection("AcuPay").Get<AcuPaySettings>()
    ?? throw new InvalidOperationException("Missing 'AcuPay' configuration section.");
settings.Validate();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<long random string>\" (or env var Jwt__Key).");

var connectionString = builder.Configuration.GetConnectionString("AcuPayDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:AcuPayDb is missing. Set it with dotnet user-secrets (or env var ConnectionStrings__AcuPayDb).");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// ---- Dependency injection
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(new SqlConnectionFactory(connectionString));
builder.Services.AddSingleton<MatchEngine>();
builder.Services.AddSingleton<ApprovalPolicy>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDatabaseHealth, DatabaseHealth>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<AuthService>();

// ---- Controllers + model validation returning our error shape (not the framework default)
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var messages = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) || e.ErrorMessage.StartsWith("The input was not valid", StringComparison.OrdinalIgnoreCase)
                ? "The request is not valid." : e.ErrorMessage)
            .Distinct();

        var body = new ErrorEnvelope(new ErrorBody(
            ErrorCodes.ValidationError, string.Join(" ", messages), context.HttpContext.GetCorrelationId()));
        return new ObjectResult(body) { StatusCode = StatusCodes.Status400BadRequest };
    };
});

// ---- JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "name",
        RoleClaimType = "role"
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            return ErrorResponseWriter.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                ErrorCodes.Unauthorized, "Authentication is required or the token is invalid or expired.");
        },
        OnForbidden = context => ErrorResponseWriter.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden,
            ErrorCodes.ForbiddenRole, "Your role is not allowed to perform this action.")
    };
});
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ---- Swagger (registered always, exposed in Development only)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AcuPay API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the token from POST /api/auth/login"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
