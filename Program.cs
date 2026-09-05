using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ClinicOps.Application.Services.Auth;
using ClinicOps.Application.Services.ClinicApplications;
using ClinicOps.Application.Services.ClinicCatalog;
using ClinicOps.Application.Services.ClinicProfile;
using ClinicOps.Application.Services.ClinicUsers;
using ClinicOps.Application.Services.Common;
using ClinicOps.Application.Services.DoctorProfile;
using ClinicOps.Application.Services.Audit;
using ClinicOps.Application.Services.Patient;
using ClinicOps.Application.Services.Privacy;
using ClinicOps.Application.Services.Pdf;
using ClinicOps.Application.Services.PatientMigrations;
using ClinicOps.Application.Services.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IAuthLoginService, AuthLoginService>();
builder.Services.AddScoped<IAuthMfaService, AuthMfaService>();
builder.Services.AddScoped<IClinicRegistrationService, ClinicRegistrationService>();
builder.Services.AddScoped<IClinicContextService, ClinicContextService>();
builder.Services.AddScoped<IProfileImageStorage, ProfileImageStorage>();
builder.Services.AddScoped<IClinicServiceCatalogService, ClinicServiceCatalogService>();
builder.Services.AddScoped<IClinicUserService, ClinicUserService>();
builder.Services.AddScoped<IClinicProfileService, ClinicProfileService>();
builder.Services.AddScoped<IDoctorProfileService, DoctorProfileService>();
builder.Services.AddScoped<IClinicApplicationService, ClinicApplicationService>();
builder.Services.AddScoped<IClinicRealtimeNotifier, ClinicRealtimeNotifier>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IPatientExcelParser, PatientExcelParser>();
builder.Services.AddScoped<IPatientMigrationFileStore, PatientMigrationFileStore>();
builder.Services.AddScoped<IPatientMigrationService, PatientMigrationService>();
builder.Services.AddScoped<IPatientQueryService, PatientQueryService>();
builder.Services.AddScoped<IPatientCaseReportService, PatientCaseReportService>();
builder.Services.AddScoped<IPatientCaseWorkflowService, PatientCaseWorkflowService>();
builder.Services.AddScoped<IPatientCaseQueryService, PatientCaseQueryService>();
builder.Services.AddScoped<IPatientCaseCommandService, PatientCaseCommandService>();
builder.Services.AddScoped<IPatientCaseLabService, PatientCaseLabService>();
builder.Services.AddScoped<IPatientCasePdfFacadeService, PatientCasePdfFacadeService>();
builder.Services.AddScoped<ICaseReportPdfService, CaseReportPdfService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IPatientPrivacyService, PatientPrivacyService>();
builder.Services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

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

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return Task.CompletedTask;
            },
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("ClinicOpsCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ClinicOps.API.Hubs.ClinicHub>("/hubs/clinic");

await ClinicOps.Infrastructure.Data.Seed.RoleSeeder.SeedAsync(app.Services);

app.Run();
