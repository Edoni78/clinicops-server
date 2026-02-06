using ClinicOps.API.DTOs.Patient;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _db;

        public PatientService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PatientResponseDto> RegisterPatientAtReceptionAsync(
            Guid clinicId,
            RegisterPatientRequest request)
        {
            // Verify clinic exists and is active
            var clinic = await _db.Clinics
                .FirstOrDefaultAsync(c => c.Id == clinicId && c.IsActive);

            if (clinic == null)
                throw new InvalidOperationException("Clinic not found or inactive.");

            // Check if patient already exists in this clinic
            // (by name, DOB, and phone if provided)
            var existingPatient = await _db.Patients
                .FirstOrDefaultAsync(p =>
                    p.ClinicId == clinicId &&
                    p.FirstName.ToLower() == request.FirstName.ToLower() &&
                    p.LastName.ToLower() == request.LastName.ToLower() &&
                    p.DateOfBirth.Date == request.DateOfBirth.Date &&
                    (string.IsNullOrEmpty(request.Phone) || p.Phone == request.Phone) &&
                    p.IsActive);

            Domain.Entities.Patient patient;
            PatientCase? patientCase = null;

            if (existingPatient != null)
            {
                // Patient exists - use existing patient
                patient = existingPatient;

                // Check if there's an active waiting case
                var activeCase = await _db.PatientCases
                    .FirstOrDefaultAsync(pc =>
                        pc.PatientId == patient.Id &&
                        pc.ClinicId == clinicId &&
                        pc.Status == PatientCaseStatus.Waiting);

                if (activeCase == null)
                {
                    // Create new case with Waiting status (reception)
                    patientCase = new PatientCase
                    {
                        ClinicId = clinicId,
                        PatientId = patient.Id,
                        Status = PatientCaseStatus.Waiting,
                        Notes = request.Notes,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.PatientCases.Add(patientCase);
                }
                else
                {
                    // Update existing waiting case notes if provided
                    if (!string.IsNullOrEmpty(request.Notes))
                    {
                        activeCase.Notes = request.Notes;
                    }
                    patientCase = activeCase;
                }
            }
            else
            {
                // New patient - create patient and case
                patient = new Domain.Entities.Patient
                {
                    ClinicId = clinicId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Phone = request.Phone,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _db.Patients.Add(patient);

                // Create patient case with Waiting status (reception)
                patientCase = new PatientCase
                {
                    ClinicId = clinicId,
                    PatientId = patient.Id,
                    Status = PatientCaseStatus.Waiting,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _db.PatientCases.Add(patientCase);
            }

            await _db.SaveChangesAsync();

            return new PatientResponseDto
            {
                Id = patient.Id,
                ClinicId = patient.ClinicId,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                Phone = patient.Phone,
                CreatedAt = patient.CreatedAt,
                IsActive = patient.IsActive,
                PatientCaseId = patientCase?.Id,
                PatientCaseStatus = patientCase?.Status.ToString()
            };
        }
    }
}
