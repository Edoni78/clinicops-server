using ClinicOps.API.DTOs.Patient;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<PatientResponseDto> RegisterPatientAtReceptionAsync(
            Guid clinicId,
            RegisterPatientRequest request)
        {
            var clinic = await _db.Clinics
                .FirstOrDefaultAsync(c => c.Id == clinicId && c.IsActive);

            if (clinic == null)
                throw new InvalidOperationException("Clinic not found or inactive.");

            var assignedDoctor = await ValidateAssignedDoctorAsync(clinicId, request.AssignedDoctorUserId);

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
                patient = existingPatient;
                patientCase = await CreateOrReuseWaitingCaseAsync(
                    clinicId,
                    patient.Id,
                    assignedDoctor.Id,
                    request.Notes);
            }
            else
            {
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
                await _db.SaveChangesAsync();

                patientCase = await CreateOrReuseWaitingCaseAsync(
                    clinicId,
                    patient.Id,
                    assignedDoctor.Id,
                    request.Notes);
            }

            return BuildPatientResponse(patient, patientCase, assignedDoctor);
        }

        public async Task<PatientResponseDto> OpenCaseForExistingPatientAsync(
            Guid clinicId,
            Guid patientId,
            OpenPatientCaseRequest request)
        {
            var clinic = await _db.Clinics
                .FirstOrDefaultAsync(c => c.Id == clinicId && c.IsActive);

            if (clinic == null)
                throw new InvalidOperationException("Clinic not found or inactive.");

            var patient = await _db.Patients
                .FirstOrDefaultAsync(p =>
                    p.Id == patientId &&
                    p.ClinicId == clinicId &&
                    p.IsActive);

            if (patient == null)
                throw new InvalidOperationException("Patient not found in this clinic.");

            var assignedDoctor = await ValidateAssignedDoctorAsync(clinicId, request.AssignedDoctorUserId);

            var patientCase = await CreateOrReuseWaitingCaseAsync(
                clinicId,
                patient.Id,
                assignedDoctor.Id,
                request.Notes);

            return BuildPatientResponse(patient, patientCase, assignedDoctor);
        }

        private async Task<PatientCase> CreateOrReuseWaitingCaseAsync(
            Guid clinicId,
            Guid patientId,
            string assignedDoctorUserId,
            string? notes)
        {
            var activeCase = await _db.PatientCases
                .FirstOrDefaultAsync(pc =>
                    pc.PatientId == patientId &&
                    pc.ClinicId == clinicId &&
                    pc.Status == PatientCaseStatus.Waiting);

            if (activeCase != null)
            {
                activeCase.UpdateWaitingAssignment(assignedDoctorUserId, notes);
                await _db.SaveChangesAsync();
                return activeCase;
            }

            var patientCase = PatientCase.OpenWaiting(clinicId, patientId, assignedDoctorUserId, notes);
            _db.PatientCases.Add(patientCase);
            await _db.SaveChangesAsync();
            return patientCase;
        }

        private static PatientResponseDto BuildPatientResponse(
            Domain.Entities.Patient patient,
            PatientCase? patientCase,
            ApplicationUser assignedDoctor)
        {
            var doctorName = assignedDoctor.DoctorDisplayName
                ?? assignedDoctor.Email
                ?? assignedDoctor.UserName;

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
                PatientCaseStatus = patientCase?.Status.ToString(),
                AssignedDoctorUserId = patientCase?.AssignedDoctorUserId,
                AssignedDoctorName = doctorName
            };
        }

        private async Task<ApplicationUser> ValidateAssignedDoctorAsync(Guid clinicId, string doctorUserId)
        {
            if (string.IsNullOrWhiteSpace(doctorUserId))
                throw new InvalidOperationException("Assigned doctor is required.");

            var doctor = await _userManager.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == doctorUserId &&
                    u.ClinicId == clinicId &&
                    u.IsActive);

            if (doctor == null)
                throw new InvalidOperationException("Assigned doctor not found in this clinic.");

            var roles = await _userManager.GetRolesAsync(doctor);
            if (!roles.Contains("Doctor", StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Selected user is not a doctor in this clinic.");

            return doctor;
        }
    }
}
