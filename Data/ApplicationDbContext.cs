using ClinicOps.Domain.Entities;
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
        
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

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
            
            
        }
        
        
    }
    
    


}