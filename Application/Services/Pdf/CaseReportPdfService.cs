using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace ClinicOps.Application.Services.Pdf
{
    public class CaseReportPdfService : ICaseReportPdfService
    {
        public async Task<byte[]> GenerateCaseReportPdfAsync(PatientCaseReportModel m)
        {
            var html = BuildReportHtml(m);

            var downloadPath = Path.Combine(Path.GetTempPath(), "PuppeteerSharp");
            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = downloadPath });
            var installedBrowser = await browserFetcher.DownloadAsync();
            var executablePath = installedBrowser.GetExecutablePath();

            if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
                throw new InvalidOperationException("Chromium nuk u gjet.");

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html);
            await Task.Delay(500);

            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                PreferCSSPageSize = true,
                MarginOptions = new MarginOptions
                {
                    Top = "15mm",
                    Bottom = "15mm",
                    Left = "15mm",
                    Right = "15mm"
                }
            });

            return pdfBytes ?? Array.Empty<byte>();
        }

        private static string BuildReportHtml(PatientCaseReportModel m)
        {
            var sb = new StringBuilder();
            static string H(string? value) => System.Net.WebUtility.HtmlEncode(value ?? "");
            var clinicName = H(m.ClinicName ?? "");
            var clinicAddress = H(m.ClinicAddress ?? "—");
            var clinicPhone = H(m.ClinicPhone ?? "—");
            var patientFullName = H($"{m.PatientFirstName} {m.PatientLastName}");
            var patientGender = H(m.PatientGender ?? "—");
            var patientDob = H(m.PatientDateOfBirth?.ToString("dd/MM/yyyy") ?? "—");
            var patientPhone = H(m.PatientPhone ?? "—");
            var caseStatus = H(m.Status ?? "—");
            var reportDate = DateTime.Now.ToString("dd/MM/yyyy");
            var reportTime = DateTime.Now.ToString("HH:mm");
            var doctorName = H(m.DoctorDisplayName ?? "—");
            var anamneza = H(m.MedicalReport?.Anamneza ?? "");
            var diagnosis = H(m.MedicalReport?.Diagnosis ?? "");
            var therapy = H(m.MedicalReport?.Therapy ?? "");
            var logoSrc = !string.IsNullOrWhiteSpace(m.ClinicLogoUrl)
                ? H(m.ClinicLogoUrl!)
                : "https://via.placeholder.com/120x120?text=Logo";
            var signatureSrc = !string.IsNullOrWhiteSpace(m.SignatureDataUri)
                ? m.SignatureDataUri
                : (!string.IsNullOrWhiteSpace(m.SignatureUrl)
                    ? H(m.SignatureUrl!)
                    : "https://via.placeholder.com/220x80?text=Signature");
            var stampSrc = !string.IsNullOrWhiteSpace(m.StampDataUri)
                ? m.StampDataUri
                : (!string.IsNullOrWhiteSpace(m.StampUrl)
                    ? H(m.StampUrl!)
                    : "https://via.placeholder.com/120x120?text=Stamp");
            var weight = m.LatestVitals?.WeightKg.HasValue == true ? $"{m.LatestVitals.WeightKg.Value:F1} kg" : "—";
            var bp = (m.LatestVitals?.SystolicPressure.HasValue == true || m.LatestVitals?.DiastolicPressure.HasValue == true)
                ? $"{m.LatestVitals?.SystolicPressure ?? 0} / {m.LatestVitals?.DiastolicPressure ?? 0} mmHg"
                : "—";
            var temp = m.LatestVitals?.TemperatureC.HasValue == true ? $"{m.LatestVitals.TemperatureC.Value:F1} °C" : "—";
            var hr = m.LatestVitals?.HeartRate.HasValue == true ? $"{m.LatestVitals.HeartRate.Value} bpm" : "—";

            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
            if (!string.IsNullOrEmpty(m.BaseUrl))
            {
                var baseHref = m.BaseUrl.TrimEnd('/') + "/";
                sb.Append("<base href=\"").Append(System.Net.WebUtility.HtmlEncode(baseHref)).Append("\">");
            }
            sb.Append("<style>@page{size:A4;margin:8mm;} body{margin:0;padding:0;font-family:Arial,sans-serif;} #clinic-case-report{height:279mm;overflow:hidden;} *{box-sizing:border-box;}</style></head><body>");

            sb.Append("<div id='clinic-case-report' style='width:194mm; margin:0 auto; background:#ffffff; color:#1e293b; font-family:Arial, sans-serif; box-sizing:border-box; padding:14px 16px 16px 16px; line-height:1.35;'>");
            sb.Append("<div style='display:flex; justify-content:space-between; align-items:flex-start; gap:12px; border-bottom:2px solid #dbeafe; padding-bottom:10px; margin-bottom:12px;'>");
            sb.Append("<div style='display:flex; gap:14px; align-items:flex-start;'>");
            sb.Append("<div style='width:78px; height:78px; flex-shrink:0;'>");
            sb.Append("<img src='").Append(logoSrc).Append("' alt='Clinic Logo' style='width:100%; height:100%; object-fit:contain; display:block;'/>");
            sb.Append("</div>");
            sb.Append("<div>");
            sb.Append("<h1 style='margin:0 0 3px 0; font-size:20px; color:#0f172a;'>").Append(clinicName).Append("</h1>");
            sb.Append("<p style='margin:0; font-size:12px; color:#475569;'>").Append(clinicAddress).Append("</p>");
            sb.Append("<p style='margin:1px 0 0 0; font-size:12px; color:#475569;'>Tel: ").Append(clinicPhone).Append("</p>");
            sb.Append("</div></div>");
            sb.Append("<div style='min-width:200px; border:1px solid #cbd5e1; border-radius:10px; padding:8px 10px; background:#f8fafc;'>");
            sb.Append("<div style='font-size:11px; color:#64748b; margin-bottom:4px;'>Detajet e raportit</div>");
            sb.Append("<div style='font-size:12px; color:#0f172a; margin-bottom:2px;'>Data: ").Append(reportDate).Append("</div>");
            sb.Append("<div style='font-size:12px; color:#0f172a; margin-bottom:2px;'>Ora: ").Append(reportTime).Append("</div>");
            sb.Append("<div style='font-size:12px; color:#0f172a;'>Mjeku: ").Append(doctorName).Append("</div>");
            sb.Append("</div></div>");

            sb.Append("<div style='text-align:center; margin-bottom:12px;'>");
            sb.Append("<h2 style='margin:0; font-size:18px; color:#0f172a; letter-spacing:0.2px;'>Raport i Rastit Mjekësor</h2>");
            sb.Append("<p style='margin:4px 0 0 0; font-size:11px; color:#64748b;'>Dokument zyrtar i konsultës dhe gjetjeve mjekësore</p>");
            sb.Append("</div>");

            sb.Append("<div style='border:1px solid #e2e8f0; border-radius:12px; overflow:hidden; margin-bottom:10px;'>");
            sb.Append("<div style='background:#f8fafc; border-bottom:1px solid #e2e8f0; padding:8px 12px; font-size:13px; color:#0f172a;'>Të dhënat e pacientit</div>");
            sb.Append("<div style='padding:0;'>");
            sb.Append("<div style='display:flex; justify-content:space-between; gap:12px; padding:7px 12px; border-bottom:1px solid #e2e8f0;'><span style='font-size:12px; color:#64748b;'>Emri i pacientit</span><span style='font-size:12px; color:#0f172a;'>").Append(patientFullName).Append("</span></div>");
            sb.Append("<div style='display:flex; justify-content:space-between; gap:12px; padding:7px 12px; border-bottom:1px solid #e2e8f0;'><span style='font-size:12px; color:#64748b;'>Gjinia</span><span style='font-size:12px; color:#0f172a;'>").Append(patientGender).Append("</span></div>");
            sb.Append("<div style='display:flex; justify-content:space-between; gap:12px; padding:7px 12px; border-bottom:1px solid #e2e8f0;'><span style='font-size:12px; color:#64748b;'>Data e lindjes</span><span style='font-size:12px; color:#0f172a;'>").Append(patientDob).Append("</span></div>");
            sb.Append("<div style='display:flex; justify-content:space-between; gap:12px; padding:7px 12px; border-bottom:1px solid #e2e8f0;'><span style='font-size:12px; color:#64748b;'>Telefoni</span><span style='font-size:12px; color:#0f172a;'>").Append(patientPhone).Append("</span></div>");
            sb.Append("<div style='display:flex; justify-content:space-between; gap:12px; padding:7px 12px;'><span style='font-size:12px; color:#64748b;'>Statusi i rastit</span><span style='font-size:12px; color:#0f172a;'>").Append(caseStatus).Append("</span></div>");
            sb.Append("</div></div>");

            sb.Append("<div style='border:1px solid #e2e8f0; border-radius:12px; overflow:hidden; margin-bottom:10px;'>");
            sb.Append("<div style='background:#f8fafc; border-bottom:1px solid #e2e8f0; padding:8px 12px; font-size:13px; color:#0f172a;'>Shenjat vitale</div>");
            sb.Append("<div style='padding:10px;'><div style='display:grid; grid-template-columns:repeat(2, 1fr); gap:8px;'>");
            sb.Append("<div style='border:1px solid #e2e8f0; border-radius:8px; padding:8px;'><div style='font-size:11px; color:#64748b;'>Pesha</div><div style='font-size:12px; color:#0f172a; margin-top:2px;'>").Append(H(weight)).Append("</div></div>");
            sb.Append("<div style='border:1px solid #e2e8f0; border-radius:8px; padding:8px;'><div style='font-size:11px; color:#64748b;'>Tensioni</div><div style='font-size:12px; color:#0f172a; margin-top:2px;'>").Append(H(bp)).Append("</div></div>");
            sb.Append("<div style='border:1px solid #e2e8f0; border-radius:8px; padding:8px;'><div style='font-size:11px; color:#64748b;'>Temperatura</div><div style='font-size:12px; color:#0f172a; margin-top:2px;'>").Append(H(temp)).Append("</div></div>");
            sb.Append("<div style='border:1px solid #e2e8f0; border-radius:8px; padding:8px;'><div style='font-size:11px; color:#64748b;'>Rrahjet e zemrës</div><div style='font-size:12px; color:#0f172a; margin-top:2px;'>").Append(H(hr)).Append("</div></div>");
            sb.Append("</div></div></div>");

            sb.Append("<div style='border:1px solid #e2e8f0; border-radius:12px; overflow:hidden; margin-bottom:10px;'>");
            sb.Append("<div style='background:#f8fafc; border-bottom:1px solid #e2e8f0; padding:8px 12px; font-size:13px; color:#0f172a;'>Shënime klinike</div>");
            sb.Append("<div style='padding:10px 12px;'>");
            sb.Append("<div style='margin-bottom:10px;'><div style='font-size:12px; color:#64748b; margin-bottom:4px;'>Anamneza</div><div style='font-size:12px; color:#0f172a; white-space:pre-wrap;'>").Append(anamneza).Append("</div></div>");
            sb.Append("<div style='margin-bottom:10px;'><div style='font-size:12px; color:#64748b; margin-bottom:4px;'>Diagnoza</div><div style='font-size:12px; color:#0f172a; white-space:pre-wrap;'>").Append(diagnosis).Append("</div></div>");
            sb.Append("<div><div style='font-size:12px; color:#64748b; margin-bottom:4px;'>Terapia / Rekomandimi</div><div style='font-size:12px; color:#0f172a; white-space:pre-wrap;'>").Append(therapy).Append("</div></div>");
            sb.Append("</div></div>");

            sb.Append("<div style='margin-top:10px; padding-top:10px; border-top:2px dashed #cbd5e1;'>");
            sb.Append("<div style='display:grid; grid-template-columns:1fr 1fr; gap:12px; align-items:end;'>");
            sb.Append("<div style='border:1px solid #e2e8f0; border-radius:12px; padding:10px; min-height:120px;'>");
            sb.Append("<div style='font-size:11px; color:#64748b; margin-bottom:6px;'>Nënshkrimi i mjekut</div>");
            sb.Append("<div style='height:64px; display:flex; align-items:center; justify-content:center;'>");
            sb.Append("<img src='").Append(signatureSrc).Append("' alt='Nënshkrimi i mjekut' style='max-width:100%; max-height:64px; object-fit:contain; display:block;'/>");
            sb.Append("</div><div style='margin-top:6px; border-top:1px solid #cbd5e1; padding-top:5px; text-align:center; font-size:11px; color:#0f172a;'>").Append(doctorName).Append("</div></div>");
            sb.Append("<div style='border:1px solid #e2e8f0; border-radius:12px; padding:10px; min-height:120px;'>");
            sb.Append("<div style='font-size:11px; color:#64748b; margin-bottom:6px;'>Vula zyrtare</div>");
            sb.Append("<div style='height:64px; display:flex; align-items:center; justify-content:center;'>");
            sb.Append("<img src='").Append(stampSrc).Append("' alt='Vula' style='max-width:100%; max-height:64px; object-fit:contain; display:block;'/>");
            sb.Append("</div><div style='margin-top:6px; border-top:1px solid #cbd5e1; padding-top:5px; text-align:center; font-size:11px; color:#0f172a;'>").Append(clinicName).Append("</div></div>");
            sb.Append("</div></div>");

            sb.Append("</div></body></html>");

            return sb.ToString();
        }
    }
}