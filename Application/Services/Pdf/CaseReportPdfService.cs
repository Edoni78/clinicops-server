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
            page.DefaultNavigationTimeout = 120000;
            await page.SetContentAsync(
                html,
                new NavigationOptions
                {
                    Timeout = 120000,
                    WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }
                });
            // Wait for image load/error events without failing the whole PDF request.
            await page.EvaluateExpressionAsync(
                @"new Promise(resolve => {
                    const imgs = Array.from(document.images);
                    if (imgs.length === 0) { resolve(true); return; }

                    let done = 0;
                    const finishOne = () => {
                        done++;
                        if (done >= imgs.length) resolve(true);
                    };

                    imgs.forEach(img => {
                        if (img.complete) {
                            finishOne();
                            return;
                        }
                        img.addEventListener('load', finishOne, { once: true });
                        img.addEventListener('error', finishOne, { once: true });
                    });

                    // Safety: never hang forever.
                    setTimeout(() => resolve(true), 2000);
                })");

            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                PreferCSSPageSize = true,
                MarginOptions = new MarginOptions
                {
                    Top = "12mm",
                    Bottom = "12mm",
                    Left = "12mm",
                    Right = "12mm"
                }
            });

            return pdfBytes ?? Array.Empty<byte>();
        }

        private static string BuildReportHtml(PatientCaseReportModel m)
        {
            var sb = new StringBuilder();
            static string H(string? value) => System.Net.WebUtility.HtmlEncode(value ?? "");
            var clinicNameRaw = (m.ClinicName ?? "").Trim().ToUpperInvariant();
            var clinicName = H(clinicNameRaw);
            var clinicAddress = H(string.IsNullOrWhiteSpace(m.ClinicAddress) ? "—" : m.ClinicAddress);
            var clinicPhone = H(string.IsNullOrWhiteSpace(m.ClinicPhone) ? "—" : m.ClinicPhone);
            var patientFullName = H($"{m.PatientFirstName} {m.PatientLastName}".Trim());
            var patientDob = H(m.PatientDateOfBirth?.ToString("dd/MM/yyyy") ?? "—");
            var patientPhone = (m.PatientPhone ?? "").Trim();
            var protocol = (m.ProtocolNumber ?? "").Trim();
            var visitDate = m.CreatedAt.ToString("dd/MM/yyyy");
            var visitTime = m.CreatedAt.ToString("HH:mm");
            var doctorName = H(FormatDoctorLine(m.DoctorDisplayName));
            var doctorTitle = H((m.DoctorTitle ?? "").Trim());
            var doctorKo = H(FormatKoLine(m.DoctorKoNumber));
            var anamneza = H(string.IsNullOrWhiteSpace(m.MedicalReport?.Anamneza) ? "—" : m.MedicalReport!.Anamneza);
            var ekzaminimi = H(string.IsNullOrWhiteSpace(m.MedicalReport?.Ekzaminimi) ? "—" : m.MedicalReport!.Ekzaminimi);
            var diagnosis = H(string.IsNullOrWhiteSpace(m.MedicalReport?.Diagnosis) ? "—" : m.MedicalReport!.Diagnosis);
            var therapy = H(string.IsNullOrWhiteSpace(m.MedicalReport?.Therapy) ? "—" : m.MedicalReport!.Therapy);
            var logoSrc = BuildOptionalImageUrl(m.BaseUrl, m.ClinicLogoDataUri, m.ClinicLogoUrl);
            var watermarkLogoSrc = m.ShowPdfWatermark ? logoSrc : null;
            var signatureSrc = BuildOptionalImageUrl(m.BaseUrl, m.SignatureDataUri, m.SignatureUrl);
            var stampSrc = BuildOptionalImageUrl(m.BaseUrl, m.StampDataUri, m.StampUrl);

            var left = new List<(string Label, string Value)>
            {
                ("Emri", patientFullName),
                ("Data e lindjes", patientDob)
            };
            if (!string.IsNullOrWhiteSpace(patientPhone))
                left.Add(("Telefoni", H(patientPhone)));

            var middle = new List<(string Label, string Value)>();
            if (!m.IsStomatologic && m.LatestVitals != null)
            {
                var v = m.LatestVitals;
                if (v.WeightKg.HasValue)
                    middle.Add(("Pesha", H($"{v.WeightKg.Value:F1} kg")));
                if (v.SystolicPressure.HasValue || v.DiastolicPressure.HasValue)
                    middle.Add(("Tensioni", H($"{v.SystolicPressure?.ToString() ?? "—"} / {v.DiastolicPressure?.ToString() ?? "—"} mmHg")));
                if (v.TemperatureC.HasValue)
                    middle.Add(("Temperatura", H($"{v.TemperatureC.Value:F1} °C")));
                if (v.HeartRate.HasValue)
                    middle.Add(("Rrahjet e zemrës", H($"{v.HeartRate.Value} bpm")));
            }

            var right = new List<(string Label, string Value)>();
            if (!string.IsNullOrWhiteSpace(protocol))
                right.Add(("Nr. protokollit", H(protocol)));
            right.Add(("Data e vizitës", H(visitDate)));
            right.Add(("Ora", H(visitTime)));
            if (!string.IsNullOrWhiteSpace(m.ServiceName))
                right.Add(("Shërbimi", H(m.ServiceName)));

            var columns = new[] { left, middle, right }.Where(c => c.Count > 0).ToList();

            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
            if (!string.IsNullOrEmpty(m.BaseUrl))
            {
                var baseHref = m.BaseUrl.TrimEnd('/') + "/";
                sb.Append("<base href=\"").Append(System.Net.WebUtility.HtmlEncode(baseHref)).Append("\">");
            }
            sb.Append("<style>");
            sb.Append("@page{size:A4;margin:12mm;}");
            sb.Append("*{box-sizing:border-box;}");
            sb.Append("body{margin:0;padding:0;font-family:Helvetica,Arial,sans-serif;color:#000;background:#fff;}");
            sb.Append(".wm{position:fixed;inset:0;display:flex;align-items:center;justify-content:center;z-index:0;pointer-events:none;}");
            sb.Append(".wm img{width:75%;max-width:none;height:auto;opacity:0.08;margin-top:-9%;}");
            sb.Append(".content{position:relative;z-index:1;padding-bottom:10mm;}");
            sb.Append(".header{display:flex;align-items:center;gap:8px;}");
            sb.Append(".header img{max-width:20mm;max-height:20mm;object-fit:contain;display:block;}");
            sb.Append(".clinic-name{margin:0;font-size:15pt;font-weight:700;color:#0f172a;text-transform:uppercase;line-height:1.15;}");
            sb.Append(".clinic-meta{margin:1px 0 0;font-size:8pt;color:#000;line-height:1.35;}");
            sb.Append(".rule{border:0;border-top:0.7pt solid #000;margin:3mm 0 5mm;}");
            sb.Append(".doc-title{text-align:center;font-size:13pt;font-weight:700;color:#0f172a;margin:0 0 4mm;}");
            sb.Append(".sec-h{background:#f8fafc;border:0.7pt solid #000;border-bottom:0;padding:1.6mm 2.2mm;font-size:10pt;color:#000;}");
            sb.Append(".grid{width:100%;border-collapse:collapse;border:0.7pt solid #000;margin:0 0 3.5mm;table-layout:fixed;}");
            sb.Append(".grid td{border-left:0.7pt solid #000;border-top:0.7pt solid #000;padding:1.8mm 2.4mm;vertical-align:middle;font-size:9pt;}");
            sb.Append(".grid tr:first-child td{border-top:0;}");
            sb.Append(".grid td:first-child{border-left:0;}");
            sb.Append(".pair-l{font-weight:400;} .pair-v{font-weight:700;}");
            sb.Append(".notes{border:0.7pt solid #000;margin:0 0 3.5mm;}");
            sb.Append(".notes-body{padding:2.2mm 2.6mm 1.6mm;}");
            sb.Append(".field-l{font-size:9pt;font-weight:700;margin:0 0 0.8mm;}");
            sb.Append(".field-t{font-size:9pt;white-space:pre-wrap;word-wrap:break-word;margin:0 0 1.2mm;line-height:1.4;}");
            sb.Append(".div{border-top:0.7pt solid #000;margin:1.2mm 0;}");
            sb.Append(".signoff{display:table;width:100%;margin-top:4mm;page-break-inside:avoid;}");
            sb.Append(".signoff .c{display:table-cell;width:50%;vertical-align:bottom;}");
            sb.Append(".signoff .name{font-size:9pt;font-weight:700;color:#0f172a;}");
            sb.Append(".signoff .meta{font-size:8pt;margin-top:0.8mm;}");
            sb.Append(".signoff .sig{max-width:46mm;max-height:10mm;margin-top:1.4mm;display:block;}");
            sb.Append(".signoff .stamp{max-width:22mm;max-height:16mm;display:block;}");
            sb.Append(".signoff .line{border-top:0.7pt solid #000;width:62mm;margin-top:2mm;padding-top:1mm;font-size:8pt;}");
            sb.Append(".footer{position:fixed;left:0;right:0;bottom:0;font-size:8pt;color:#000;border-top:0.7pt solid #000;padding-top:1.2mm;display:flex;justify-content:space-between;gap:4mm;background:#fff;}");
            sb.Append("</style></head><body>");

            if (!string.IsNullOrWhiteSpace(watermarkLogoSrc))
            {
                sb.Append("<div class='wm' aria-hidden='true'><img src='").Append(watermarkLogoSrc).Append("' alt=''/></div>");
            }

            sb.Append("<div class='content'>");
            sb.Append("<div class='header'>");
            if (!string.IsNullOrWhiteSpace(logoSrc))
                sb.Append("<img src='").Append(logoSrc).Append("' alt=''/>");
            sb.Append("<div>");
            if (!string.IsNullOrWhiteSpace(clinicNameRaw))
                sb.Append("<p class='clinic-name'>").Append(clinicName).Append("</p>");
            sb.Append("<p class='clinic-meta'>Adresa: ").Append(clinicAddress).Append("</p>");
            sb.Append("<p class='clinic-meta'>Tel: ").Append(clinicPhone).Append("</p>");
            sb.Append("</div></div>");
            sb.Append("<hr class='rule'/>");
            sb.Append("<p class='doc-title'>RAPORT SPECIALISTIK</p>");

            sb.Append("<div class='sec-h'>Të dhënat e pacientit</div>");
            var rows = columns.Count == 0 ? 1 : columns.Max(c => c.Count);
            sb.Append("<table class='grid'><tbody>");
            for (var r = 0; r < rows; r++)
            {
                sb.Append("<tr>");
                foreach (var col in columns)
                {
                    sb.Append("<td>");
                    if (r < col.Count)
                    {
                        sb.Append("<span class='pair-l'>").Append(H(col[r].Label)).Append(" :</span> ");
                        sb.Append("<span class='pair-v'>").Append(col[r].Value).Append("</span>");
                    }
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");

            if (m.IsStomatologic)
                AppendOdontogramHtml(sb, m.DentalFindings, H);

            sb.Append("<div class='sec-h'>Shënime klinike</div>");
            sb.Append("<div class='notes'><div class='notes-body'>");
            void Field(string label, string value, bool last)
            {
                sb.Append("<p class='field-l'>").Append(H(label)).Append("</p>");
                sb.Append("<p class='field-t'>").Append(value).Append("</p>");
                if (!last) sb.Append("<div class='div'></div>");
            }
            Field("Anamneza", anamneza, false);
            Field("Ekzaminimi", ekzaminimi, false);
            Field("Diagnoza", diagnosis, false);
            var labNames = m.LabFileNames?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            Field("Terapia / Rekomandimi", therapy, labNames.Count == 0);
            if (labNames.Count > 0)
            {
                var labText = H(string.Join("\n", labNames.Select(n => $"- {n}")));
                Field("Laboratori", labText, true);
            }
            sb.Append("</div></div>");

            sb.Append("<div class='signoff'>");
            sb.Append("<div class='c'>");
            sb.Append("<div class='name'>").Append(doctorName).Append("</div>");
            if (!string.IsNullOrWhiteSpace(m.DoctorTitle))
                sb.Append("<div class='meta'>").Append(doctorTitle).Append("</div>");
            if (!string.IsNullOrWhiteSpace(doctorKo))
                sb.Append("<div class='meta'>").Append(doctorKo).Append("</div>");
            if (!string.IsNullOrWhiteSpace(signatureSrc))
                sb.Append("<img class='sig' src='").Append(signatureSrc).Append("' alt=''/>");
            sb.Append("<div class='line'>Nënshkrimi</div>");
            sb.Append("</div><div class='c'>");
            if (!string.IsNullOrWhiteSpace(stampSrc))
                sb.Append("<img class='stamp' src='").Append(stampSrc).Append("' alt=''/>");
            sb.Append("<div class='line'>Vula</div>");
            sb.Append("</div></div>");

            sb.Append("<div class='footer'><span>");
            sb.Append(clinicName);
            if (!string.IsNullOrWhiteSpace(protocol))
                sb.Append(" · Nr. protokollit: ").Append(H(protocol));
            sb.Append("</span><span>").Append(clinicAddress).Append(" · Tel: ").Append(clinicPhone).Append("</span></div>");

            sb.Append("</div></body></html>");
            return sb.ToString();
        }

        private static string BuildImageUrlLikeLogo(string? baseUrl, string? relativeOrAbsoluteUrl, string placeholder)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
                return placeholder;

            var normalized = NormalizeUrlPath(relativeOrAbsoluteUrl.Trim());
            if (Uri.TryCreate(normalized, UriKind.Absolute, out var absolute))
                return System.Net.WebUtility.HtmlEncode(absolute.ToString());

            if (!string.IsNullOrWhiteSpace(baseUrl) &&
                Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) &&
                Uri.TryCreate(baseUri, normalized.TrimStart('/'), out var combined))
                return System.Net.WebUtility.HtmlEncode(combined.ToString());

            return System.Net.WebUtility.HtmlEncode(normalized);
        }

        private static string? BuildOptionalImageUrl(string? baseUrl, params string?[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                var normalized = NormalizeUrlPath(candidate.Trim());
                if (Uri.TryCreate(normalized, UriKind.Absolute, out var absolute))
                    return System.Net.WebUtility.HtmlEncode(absolute.ToString());

                if (!string.IsNullOrWhiteSpace(baseUrl) &&
                    Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) &&
                    Uri.TryCreate(baseUri, normalized.TrimStart('/'), out var combined))
                    return System.Net.WebUtility.HtmlEncode(combined.ToString());

                return System.Net.WebUtility.HtmlEncode(normalized);
            }

            return null;
        }

        private static string NormalizeUrlPath(string url) => url.Replace("\\", "/");

        private static void AppendOdontogramHtml(
            StringBuilder sb,
            List<DentalFindingPdfModel> findings,
            Func<string?, string> H)
        {
            sb.Append("<div class='sec-h'>Odontograma</div>");
            sb.Append("<div class='notes' style='margin-bottom:3.5mm;'><div class='notes-body'>");

            var adult = findings.Where(f => !string.Equals(f.Dentition, "Kids", StringComparison.OrdinalIgnoreCase)).ToList();
            var kids = findings.Where(f => string.Equals(f.Dentition, "Kids", StringComparison.OrdinalIgnoreCase)).ToList();
            AppendArchChart(sb, "Të rritur",
                new[] { 18, 17, 16, 15, 14, 13, 12, 11, 21, 22, 23, 24, 25, 26, 27, 28 },
                new[] { 48, 47, 46, 45, 44, 43, 42, 41, 31, 32, 33, 34, 35, 36, 37, 38 },
                adult);
            if (kids.Count > 0)
                AppendArchChart(sb, "Fëmijë",
                    new[] { 55, 54, 53, 52, 51, 61, 62, 63, 64, 65 },
                    new[] { 85, 84, 83, 82, 81, 71, 72, 73, 74, 75 },
                    kids);

            sb.Append("<div style='display:flex; flex-wrap:wrap; gap:8px; margin-top:8px; font-size:8pt; color:#000;'>");
            sb.Append("<span><span style='display:inline-block;width:8px;height:8px;background:#ef4444;border-radius:99px;margin-right:4px;'></span>Karies</span>");
            sb.Append("<span><span style='display:inline-block;width:8px;height:8px;background:#0ea5e9;border-radius:99px;margin-right:4px;'></span>Mbushje</span>");
            sb.Append("<span><span style='display:inline-block;width:8px;height:8px;background:#d97706;border-radius:99px;margin-right:4px;'></span>Kurorë</span>");
            sb.Append("<span><span style='display:inline-block;width:8px;height:8px;background:#7c3aed;border-radius:99px;margin-right:4px;'></span>Endodonci</span>");
            sb.Append("<span><span style='display:inline-block;width:8px;height:8px;background:#059669;border-radius:99px;margin-right:4px;'></span>Implant</span>");
            sb.Append("<span><span style='display:inline-block;width:8px;height:8px;background:#94a3b8;border-radius:99px;margin-right:4px;'></span>Mungon / ekstraktim</span>");
            sb.Append("</div>");

            if (findings.Count > 0)
            {
                sb.Append("<div style='margin-top:8px;'>");
                foreach (var finding in findings.OrderBy(f => f.ToothNumber))
                {
                    var line = $"Dhëmbi {finding.ToothNumber}: {TranslateCondition(finding.Condition)}";
                    if (!string.IsNullOrWhiteSpace(finding.Surfaces)) line += $" · {finding.Surfaces}";
                    if (!string.IsNullOrWhiteSpace(finding.Treatment)) line += $" · {finding.Treatment}";
                    if (!string.IsNullOrWhiteSpace(finding.Notes)) line += $" — {finding.Notes}";
                    sb.Append("<div style='padding:4px 0; border-top:0.7pt solid #000; font-size:9pt;'>").Append(H(line)).Append("</div>");
                }
                sb.Append("</div>");
            }

            sb.Append("</div></div>");
        }

        private static void AppendArchChart(StringBuilder sb, string title, int[] upper, int[] lower, List<DentalFindingPdfModel> findings)
        {
            var byTooth = findings
                .GroupBy(f => f.ToothNumber)
                .ToDictionary(g => g.Key, g => g.First());
            sb.Append("<div style='margin-bottom:8px;'>");
            sb.Append("<div style='font-size:8pt; text-transform:uppercase; letter-spacing:.08em; margin-bottom:4px;'>").Append(title).Append("</div>");
            AppendToothRow(sb, upper, byTooth);
            sb.Append("<div style='height:6px;'></div>");
            AppendToothRow(sb, lower, byTooth);
            sb.Append("</div>");
        }

        private static void AppendToothRow(StringBuilder sb, int[] numbers, Dictionary<int, DentalFindingPdfModel> byTooth)
        {
            sb.Append("<div style='display:flex; justify-content:center; gap:2px;'>");
            foreach (var n in numbers)
            {
                byTooth.TryGetValue(n, out var finding);
                var condition = finding?.Condition ?? "Healthy";
                var bg = ConditionColor(condition);
                var fg = condition is "Healthy" or "Missing" ? "#334155" : "#fff";
                var mark = condition is "Extraction" or "Missing" ? "×" : n.ToString();
                sb.Append("<div style='width:18px; text-align:center;'>");
                sb.Append("<div style='height:26px; border:0.7pt solid #000; background:").Append(bg)
                    .Append("; color:").Append(fg).Append("; font-size:8px; font-weight:700; line-height:26px;'>").Append(mark).Append("</div>");
                sb.Append("</div>");
            }
            sb.Append("</div>");
        }

        private static string ConditionColor(string? condition) => (condition ?? "") switch
        {
            "Caries" => "#ef4444",
            "Filling" => "#0ea5e9",
            "Crown" => "#d97706",
            "Endo" => "#7c3aed",
            "Implant" => "#059669",
            "Extraction" => "#64748b",
            "Missing" => "#94a3b8",
            _ => "#f8fafc"
        };

        private static string TranslateCondition(string? condition) => (condition ?? "") switch
        {
            "Healthy" => "I shëndetshëm",
            "Caries" => "Karies",
            "Filling" => "Mbushje",
            "Crown" => "Kurorë",
            "Endo" => "Endodonci",
            "Implant" => "Implant",
            "Extraction" => "Ekstraktim",
            "Missing" => "Mungon",
            _ => condition ?? ""
        };

        private static string FormatDoctorLine(string? name)
        {
            var text = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text)) return "—";
            if (text.StartsWith("Dr", StringComparison.OrdinalIgnoreCase)) return text;
            return $"Dr. {text}";
        }

        private static string FormatKoLine(string? value)
        {
            var text = (value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text)) return "";
            var rest = System.Text.RegularExpressions.Regex.Replace(text, @"^ko[\s.\-:]*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            return string.IsNullOrWhiteSpace(rest) ? "" : $"KO-{rest}";
        }

    }
}