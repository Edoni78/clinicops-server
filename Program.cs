using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ClinicOps.Application.Services.Auth;
using ClinicOps.Application.Services.Patient;
using ClinicOps.Application.Services.Pdf;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Services
// =======================

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    );
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<ICaseReportPdfService, CaseReportPdfService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var key = builder.Configuration["Jwt:Key"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        
        // Prevent redirects for API calls
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return Task.CompletedTask;
            },
            // SignalR: read JWT from query string (e.g. ?access_token=...)
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                if (path.StartsWithSegments("/hubs/clinic"))
                {
                    var token = context.Request.Query["access_token"].FirstOrDefault()
                        ?? context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                    if (!string.IsNullOrEmpty(token))
                        context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSignalR();

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClinicOpsCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// =======================
// App
// =======================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("ClinicOpsCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ClinicOps.API.Hubs.ClinicHub>("/hubs/clinic");

await ClinicOps.Infrastructure.Data.Seed.RoleSeeder.SeedAsync(app.Services);

app.Run();