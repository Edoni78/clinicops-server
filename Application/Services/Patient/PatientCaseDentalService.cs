using ClinicOps.API.DTOs.PatientCase;
using ClinicOps.Domain.Entities;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientCaseDentalService : IPatientCaseDentalService
    {
        private static readonly HashSet<int> AdultTeeth = new(
            new[] { 11, 12, 13, 14, 15, 16, 17, 18, 21, 22, 23, 24, 25, 26, 27, 28,
                    31, 32, 33, 34, 35, 36, 37, 38, 41, 42, 43, 44, 45, 46, 47, 48 });

        private static readonly HashSet<int> KidsTeeth = new(
            new[] { 51, 52, 53, 54, 55, 61, 62, 63, 64, 65, 71, 72, 73, 74, 75, 81, 82, 83, 84, 85 });

        private static readonly HashSet<string> Conditions = new(StringComparer.OrdinalIgnoreCase)
        {
            "Healthy", "Caries", "Filling", "Crown", "Endo", "Extraction", "Missing", "Implant"
        };

        private readonly ApplicationDbContext _db;

        public PatientCaseDentalService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<DentalFindingDto>> ListAsync(Guid caseId, Guid clinicId)
        {
            await EnsureStomatologicCaseAsync(caseId, clinicId);
            return await _db.PatientCaseDentalFindings
                .AsNoTracking()
                .Where(f => f.PatientCaseId == caseId && f.ClinicId == clinicId)
                .OrderBy(f => f.ToothNumber)
                .Select(f => Map(f))
                .ToListAsync();
        }

        public async Task<DentalFindingDto> UpsertAsync(
            Guid caseId,
            Guid clinicId,
            string? userId,
            UpsertDentalFindingRequest request)
        {
            await EnsureStomatologicCaseAsync(caseId, clinicId);

            var dentition = NormalizeDentition(request.Dentition);
            EnsureToothAllowed(request.ToothNumber, dentition);
            var condition = NormalizeCondition(request.Condition);

            var existing = await _db.PatientCaseDentalFindings.FirstOrDefaultAsync(f =>
                f.PatientCaseId == caseId &&
                f.ClinicId == clinicId &&
                f.ToothNumber == request.ToothNumber &&
                f.Dentition == dentition);

            if (existing == null)
            {
                existing = new PatientCaseDentalFinding
                {
                    ClinicId = clinicId,
                    PatientCaseId = caseId,
                    ToothNumber = request.ToothNumber,
                    Dentition = dentition
                };
                _db.PatientCaseDentalFindings.Add(existing);
            }

            existing.Condition = condition;
            existing.Surfaces = string.IsNullOrWhiteSpace(request.Surfaces) ? null : request.Surfaces.Trim();
            existing.Treatment = string.IsNullOrWhiteSpace(request.Treatment) ? null : request.Treatment.Trim();
            existing.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;

            await _db.SaveChangesAsync();
            return Map(existing);
        }

        public async Task DeleteAsync(Guid caseId, Guid clinicId, int toothNumber, string dentition)
        {
            await EnsureStomatologicCaseAsync(caseId, clinicId);
            var normalized = NormalizeDentition(dentition);
            var existing = await _db.PatientCaseDentalFindings.FirstOrDefaultAsync(f =>
                f.PatientCaseId == caseId &&
                f.ClinicId == clinicId &&
                f.ToothNumber == toothNumber &&
                f.Dentition == normalized);
            if (existing == null) return;
            _db.PatientCaseDentalFindings.Remove(existing);
            await _db.SaveChangesAsync();
        }

        private async Task EnsureStomatologicCaseAsync(Guid caseId, Guid clinicId)
        {
            var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clinicId);
            if (clinic == null)
                throw new KeyNotFoundException("Clinic not found.");
            if (!clinic.IsStomatologic)
                throw new InvalidOperationException("Odontograma është e disponueshme vetëm për ordinancën stomatologjike.");

            var exists = await _db.PatientCases.AnyAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);
            if (!exists)
                throw new KeyNotFoundException("Patient case not found.");
        }

        private static string NormalizeDentition(string? value)
        {
            var v = (value ?? "").Trim();
            if (v.Equals("Kids", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("Child", StringComparison.OrdinalIgnoreCase) ||
                v.Equals("Primary", StringComparison.OrdinalIgnoreCase))
                return "Kids";
            return "Adult";
        }

        private static string NormalizeCondition(string? value)
        {
            var v = (value ?? "Healthy").Trim();
            if (!Conditions.Contains(v))
                throw new InvalidOperationException("Gjendja e dhëmbit nuk njihet.");
            return Conditions.First(c => c.Equals(v, StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureToothAllowed(int tooth, string dentition)
        {
            var allowed = dentition == "Kids" ? KidsTeeth : AdultTeeth;
            if (!allowed.Contains(tooth))
                throw new InvalidOperationException("Numri i dhëmbit nuk është i vlefshëm për këtë denticion.");
        }

        private static DentalFindingDto Map(PatientCaseDentalFinding f) => new()
        {
            Id = f.Id,
            ToothNumber = f.ToothNumber,
            Dentition = f.Dentition,
            Condition = f.Condition,
            Surfaces = f.Surfaces,
            Treatment = f.Treatment,
            Notes = f.Notes,
            UpdatedAt = f.UpdatedAt
        };
    }
}
