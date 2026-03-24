using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Infrastructure.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Clinic> Clinics => Set<Clinic>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<PatientCase> PatientCases => Set<PatientCase>();
        public DbSet<VitalSigns> VitalSigns => Set<VitalSigns>();
        public DbSet<MedicalReport> MedicalReports => Set<MedicalReport>();
        public DbSet<LabResult> LabResults => Set<LabResult>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<ClinicApplication> ClinicApplications => Set<ClinicApplication>();
        public DbSet<Service> Services => Set<Service>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ==============================
            // RELATIONSHIPS
            // ==============================

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Clinic)
                .WithMany()
                .HasForeignKey(u => u.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Patient>()
                .HasOne(p => p.Clinic)
                .WithMany()
                .HasForeignKey(p => p.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PatientCase>()
                .HasOne(pc => pc.Patient)
                .WithMany()
                .HasForeignKey(pc => pc.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PatientCase>()
                .HasOne(pc => pc.Clinic)
                .WithMany()
                .HasForeignKey(pc => pc.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<VitalSigns>()
                .HasOne(v => v.PatientCase)
                .WithMany()
                .HasForeignKey(v => v.PatientCaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MedicalReport>()
                .HasOne(m => m.PatientCase)
                .WithOne()
                .HasForeignKey<MedicalReport>(m => m.PatientCaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<LabResult>()
                .HasOne(l => l.PatientCase)
                .WithMany()
                .HasForeignKey(l => l.PatientCaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Payment>()
                .HasOne(p => p.PatientCase)
                .WithOne()
                .HasForeignKey<Payment>(p => p.PatientCaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Service>()
                .HasOne(s => s.Clinic)
                .WithMany()
                .HasForeignKey(s => s.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==============================
            // SEED SUPER ADMIN
            // ==============================

            var superAdminRoleId = "SuperAdmin";
            var superAdminUserId = "SuperAdmin";

            // ROLE
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = superAdminRoleId,
                    Name = "SuperAdmin",
                    NormalizedName = "SUPERADMIN"
                }
            );

            // USER
            var hasher = new PasswordHasher<ApplicationUser>();

            var superAdminUser = new ApplicationUser
            {
                Id = superAdminUserId,
                UserName = "superadmin@clinicops.local",
                NormalizedUserName = "SUPERADMIN@CLINICOPS.LOCAL",
                Email = "superadmin@clinicops.local",
                NormalizedEmail = "SUPERADMIN@CLINICOPS.LOCAL",
                EmailConfirmed = true,
                ClinicId = null,
                SecurityStamp = "STATIC-SECURITY-STAMP"
            };

            superAdminUser.PasswordHash =
                hasher.HashPassword(superAdminUser, "SuperAdmin123!");

            builder.Entity<ApplicationUser>().HasData(superAdminUser);

            // USER ↔ ROLE
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    UserId = superAdminUserId,
                    RoleId = superAdminRoleId
                }
            );

            // ==============================
            // SEED DEFAULT CLINIC FOR SUPERADMIN TESTING
            // ==============================
            var defaultClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            builder.Entity<Clinic>().HasData(
                new Clinic
                {
                    Id = defaultClinicId,
                    Name = "Default Test Clinic",
                    Address = "123 Test Street",
                    Phone = "+1234567890",
                    ClinicMode = ClinicMode.FullTeam,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            );
        }
    }
}
