using ClinicOps.Application.Services.Pdf;
using ClinicOps.Domain.Entities;
using ClinicOps.Domain.Enums;
using ClinicOps.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace ClinicOps.Application.Services.Patient
{
    public class PatientCasePdfFacadeService : IPatientCasePdfFacadeService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICaseReportPdfService _pdfService;
        private readonly IWebHostEnvironment _env;

        public PatientCasePdfFacadeService(ApplicationDbContext db, ICaseReportPdfService pdfService, IWebHostEnvironment env)
        {
            _db = db;
            _pdfService = pdfService;
            _env = env;
        }

        public async Task<(byte[] fileBytes, string fileName)> GenerateCaseReportPdfAsync(Guid caseId, Guid clinicId, string baseUrl, string? fallbackDoctorUserId)
        {
            var isSoloDoctor = await IsSoloDoctorClinicAsync(clinicId);
            var @case = await _db.PatientCases
                .Include(pc => pc.Patient)
                .Include(pc => pc.Clinic)
                .FirstOrDefaultAsync(pc => pc.Id == caseId && pc.ClinicId == clinicId);

            if (@case == null)
                throw new KeyNotFoundException("Patient case not found.");

            var latestVitals = await _db.VitalSigns
                .Where(v => v.PatientCaseId == caseId)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();
            var report = await _db.MedicalReports.FirstOrDefaultAsync(m => m.PatientCaseId == caseId);

            string? doctorDisplayName = null;
            string? signatureUrl = null;
            string? stampUrl = null;
            string? signatureDataUri = null;
            string? stampDataUri = null;
            byte[]? signatureBytes = null;
            byte[]? stampBytes = null;
            var resolvedDoctorUserId = report?.DoctorUserId;
            if (string.IsNullOrWhiteSpace(resolvedDoctorUserId))
                resolvedDoctorUserId = fallbackDoctorUserId;

            if (!string.IsNullOrWhiteSpace(resolvedDoctorUserId))
            {
                var doctorUser = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == resolvedDoctorUserId);
                if (doctorUser != null)
                {
                    doctorDisplayName = doctorUser.DoctorDisplayName ?? doctorUser.Email;
                    signatureUrl = doctorUser.SignatureUrl;
                    stampUrl = doctorUser.StampUrl;
                    signatureDataUri = TryReadFileAsDataUri(_env, signatureUrl);
                    stampDataUri = TryReadFileAsDataUri(_env, stampUrl);
                    signatureBytes = TryReadFileBytes(_env, signatureUrl);
                    stampBytes = TryReadFileBytes(_env, stampUrl);
                }
            }

            if (signatureBytes == null && !string.IsNullOrWhiteSpace(signatureUrl))
                signatureBytes = await TryDownloadImageBytesAsync(baseUrl, signatureUrl);
            if (stampBytes == null && !string.IsNullOrWhiteSpace(stampUrl))
                stampBytes = await TryDownloadImageBytesAsync(baseUrl, stampUrl);

            var model = new PatientCaseReportModel
            {
                ClinicName = @case.Clinic?.Name ?? "",
                ClinicAddress = @case.Clinic?.Address,
                ClinicPhone = @case.Clinic?.Phone,
                ClinicLogoUrl = @case.Clinic?.LogoUrl,
                ClinicLogoDataUri = TryReadFileAsDataUri(_env, @case.Clinic?.LogoUrl),
                PatientFirstName = @case.Patient.FirstName,
                PatientLastName = @case.Patient.LastName,
                PatientDateOfBirth = @case.Patient.DateOfBirth,
                PatientGender = @case.Patient.Gender,
                PatientPhone = @case.Patient.Phone,
                Status = @case.Status.ToString(),
                ProtocolNumber = @case.ProtocolNumber,
                CreatedAt = @case.CreatedAt,
                Notes = @case.Notes,
                LatestVitals = latestVitals == null ? null : new VitalsModel
                {
                    WeightKg = latestVitals.WeightKg,
                    SystolicPressure = latestVitals.SystolicPressure,
                    DiastolicPressure = latestVitals.DiastolicPressure,
                    TemperatureC = latestVitals.TemperatureC,
                    HeartRate = latestVitals.HeartRate,
                    RecordedAt = latestVitals.RecordedAt
                },
                MedicalReport = report == null ? null : new MedicalReportModel
                {
                    Anamneza = report.Anamneza,
                    Diagnosis = report.Diagnosis,
                    Therapy = report.Therapy,
                    CreatedAt = report.CreatedAt
                },
                BaseUrl = baseUrl,
                DoctorDisplayName = doctorDisplayName,
                SignatureUrl = signatureUrl,
                StampUrl = stampUrl,
                SignatureDataUri = signatureDataUri,
                StampDataUri = stampDataUri,
                SignatureBytes = signatureBytes,
                StampBytes = stampBytes
            };

            var pdfBytes = await _pdfService.GenerateCaseReportPdfAsync(model);

            var labResults = await _db.LabResults
                .Where(l => l.PatientCaseId == caseId)
                .OrderBy(l => l.UploadedAt)
                .ToListAsync();
            if (!isSoloDoctor && labResults.Count > 0)
                pdfBytes = MergeReportWithLabPdfs(pdfBytes, labResults);

            var fileName = $"CaseReport_{@case.Patient.LastName}_{@case.Patient.FirstName}_{caseId:N}.pdf";
            return (pdfBytes, fileName);
        }

        private byte[] MergeReportWithLabPdfs(byte[] reportPdfBytes, List<LabResult> labResults)
        {
            using var outputDoc = new PdfDocument();
            using (var reportStream = new MemoryStream(reportPdfBytes))
            using (var reportDoc = PdfReader.Open(reportStream, PdfDocumentOpenMode.Import))
            {
                for (int i = 0; i < reportDoc.PageCount; i++)
                    outputDoc.AddPage(reportDoc.Pages[i]);
            }
            var contentRoot = _env.ContentRootPath ?? "";
            foreach (var lab in labResults)
            {
                var fullPath = Path.Combine(contentRoot, lab.FilePath.Replace('/', Path.DirectorySeparatorChar));
                if (!System.IO.File.Exists(fullPath)) continue;
                using (var labStream = System.IO.File.OpenRead(fullPath))
                using (var labDoc = PdfReader.Open(labStream, PdfDocumentOpenMode.Import))
                {
                    for (int i = 0; i < labDoc.PageCount; i++)
                        outputDoc.AddPage(labDoc.Pages[i]);
                }
            }
            using var ms = new MemoryStream();
            outputDoc.Save(ms, false);
            return ms.ToArray();
        }

        private async Task<bool> IsSoloDoctorClinicAsync(Guid clinicId)
        {
            var mode = await _db.Clinics
                .Where(c => c.Id == clinicId)
                .Select(c => c.ClinicMode)
                .FirstOrDefaultAsync();
            return mode == ClinicMode.SoloDoctor;
        }

        private static string? TryReadFileAsDataUri(IWebHostEnvironment env, string? relativePath)
        {
            var path = ResolveLocalFilePath(env, relativePath);
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;
            try
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                var base64 = Convert.ToBase64String(bytes);
                var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                var mime = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "image/png"
                };
                return $"data:{mime};base64,{base64}";
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? TryReadFileBytes(IWebHostEnvironment env, string? relativePath)
        {
            var path = ResolveLocalFilePath(env, relativePath);
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;
            try
            {
                return System.IO.File.ReadAllBytes(path);
            }
            catch
            {
                return null;
            }
        }

        private static string? ResolveLocalFilePath(IWebHostEnvironment env, string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;
            var trimmed = relativePath.TrimStart('/', '\\');

            if (!string.IsNullOrWhiteSpace(env.WebRootPath))
            {
                var webRootPath = Path.Combine(env.WebRootPath, trimmed);
                if (System.IO.File.Exists(webRootPath)) return webRootPath;
            }

            if (!string.IsNullOrWhiteSpace(env.ContentRootPath))
            {
                var contentWwwRootPath = Path.Combine(env.ContentRootPath, "wwwroot", trimmed);
                if (System.IO.File.Exists(contentWwwRootPath)) return contentWwwRootPath;

                var contentRootPath = Path.Combine(env.ContentRootPath, trimmed);
                if (System.IO.File.Exists(contentRootPath)) return contentRootPath;
            }

            return null;
        }

        private static async Task<byte[]?> TryDownloadImageBytesAsync(string baseUrl, string relativeOrAbsoluteUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
                return null;

            try
            {
                var url = relativeOrAbsoluteUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? relativeOrAbsoluteUrl
                    : $"{baseUrl.TrimEnd('/')}/{relativeOrAbsoluteUrl.TrimStart('/')}";

                using var http = new HttpClient();
                return await http.GetByteArrayAsync(url);
            }
            catch
            {
                return null;
            }
        }
    }
}
