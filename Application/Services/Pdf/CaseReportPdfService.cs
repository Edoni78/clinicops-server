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

            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
            if (!string.IsNullOrEmpty(m.BaseUrl))
            {
                var baseHref = m.BaseUrl.TrimEnd('/') + "/";
                sb.Append("<base href=\"").Append(System.Net.WebUtility.HtmlEncode(baseHref)).Append("\">");
            }
            sb.Append("<style>");

            sb.Append(@"
                @page {
                    size: A4;
                    margin: 15mm;
                }

                body {
                    font-family: Arial, sans-serif;
                    font-size: 12px;
                    color: #111;
                    margin: 0;
                }

                .container {
                    width: 100%;
                }

                .header {
                    text-align: center;
                    margin-bottom: 15px;
                }

                .header h1 {
                    font-size: 18px;
                    margin: 0;
                    letter-spacing: 1px;
                }

                .clinic {
                    font-size: 13px;
                    margin-top: 4px;
                }

                .meta {
                    font-size: 11px;
                    margin-top: 4px;
                }

                h2 {
                    font-size: 13px;
                    margin-top: 15px;
                    margin-bottom: 5px;
                    border-bottom: 1px solid #000;
                    padding-bottom: 3px;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-top: 5px;
                }

                th, td {
                    border: 1px solid #000;
                    padding: 5px;
                    text-align: left;
                    vertical-align: top;
                }

                th {
                    width: 35%;
                    background: #f2f2f2;
                }

                .text-block {
                    border: 1px solid #000;
                    padding: 6px;
                    min-height: 60px;
                    margin-top: 5px;
                    white-space: pre-wrap;
                }

                .signature-section {
                    margin-top: 25px;
                }

                .signature-table {
                    width: 100%;
                }

                .signature-table td {
                    border: none;
                    vertical-align: top;
                }

                .signature-box {
                    height: 70px;
                    border: 1px dashed #000;
                    width: 120px;
                    margin-top: 5px;
                }

                .signature-line {
                    margin-top: 50px;
                    border-top: 1px solid #000;
                    width: 220px;
                }

                .signature-img {
                    max-height: 70px;
                    max-width: 160px;
                    object-fit: contain;
                }

                .doctor-name {
                    margin-top: 4px;
                    font-size: 11px;
                }
            ");

            sb.Append("</style></head><body>");
            sb.Append("<div class='container'>");

            sb.Append("<div class='header'>");
            sb.Append("<h1>RAPORT MJEKËSOR</h1>");

            if (!string.IsNullOrEmpty(m.ClinicName))
            {
                sb.Append("<div class='clinic'>")
                  .Append(System.Net.WebUtility.HtmlEncode(m.ClinicName))
                  .Append("</div>");
            }

            sb.Append("<div class='meta'>Data e lëshimit: ")
              .Append(DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
              .Append("</div>");

            sb.Append("</div>");

            sb.Append("<h2>Të dhënat e pacientit</h2>");
            sb.Append("<table>");
            sb.AppendRow("Emri dhe mbiemri", $"{m.PatientFirstName} {m.PatientLastName}");
            sb.AppendRow("Data e lindjes", m.PatientDateOfBirth?.ToString("dd.MM.yyyy") ?? "—");
            sb.AppendRow("Gjinia", m.PatientGender ?? "—");
            sb.AppendRow("Numri i telefonit", m.PatientPhone ?? "—");
            sb.AppendRow("Data e vizitës", m.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
            sb.Append("</table>");

            if (m.LatestVitals != null)
            {
                var v = m.LatestVitals;

                sb.Append("<h2>Shenjat vitale</h2>");
                sb.Append("<table>");
                if (v.WeightKg.HasValue)
                    sb.AppendRow("Pesha (kg)", v.WeightKg.Value.ToString("F1"));
                if (v.SystolicPressure.HasValue || v.DiastolicPressure.HasValue)
                    sb.AppendRow("Tensioni i gjakut",
                        $"{v.SystolicPressure ?? 0}/{v.DiastolicPressure ?? 0} mmHg");
                if (v.TemperatureC.HasValue)
                    sb.AppendRow("Temperatura (°C)", v.TemperatureC.Value.ToString("F1"));
                if (v.HeartRate.HasValue)
                    sb.AppendRow("Rrahjet e zemrës", v.HeartRate.Value + " bpm");
                sb.Append("</table>");
            }

            if (m.MedicalReport != null)
            {
                var r = m.MedicalReport;

                sb.Append("<h2>Anamneza</h2>");
                sb.Append("<div class='text-block'>")
                  .Append(System.Net.WebUtility.HtmlEncode(r.Anamneza ?? ""))
                  .Append("</div>");

                sb.Append("<h2>Diagnoza</h2>");
                sb.Append("<div class='text-block'>")
                  .Append(System.Net.WebUtility.HtmlEncode(r.Diagnosis ?? ""))
                  .Append("</div>");

                sb.Append("<h2>Terapia</h2>");
                sb.Append("<div class='text-block'>")
                  .Append(System.Net.WebUtility.HtmlEncode(r.Therapy ?? ""))
                  .Append("</div>");
            }

            sb.Append("<div class='signature-section'>");
            sb.Append("<table class='signature-table'><tr>");

            sb.Append("<td style='width:50%;'>");
            sb.Append("<div>Vula</div>");
            if (!string.IsNullOrEmpty(m.StampDataUri))
            {
                sb.Append("<img src=\"").Append(m.StampDataUri).Append("\" alt=\"Vula\" class=\"signature-img\" />");
            }
            else if (!string.IsNullOrEmpty(m.StampUrl))
            {
                sb.Append("<img src=\"").Append(System.Net.WebUtility.HtmlEncode(m.StampUrl)).Append("\" alt=\"Vula\" class=\"signature-img\" />");
            }
            else
            {
                sb.Append("<div class='signature-box'></div>");
            }
            sb.Append("</td>");

            sb.Append("<td style='width:50%; text-align:right;'>");
            sb.Append("<div>Mjeku përgjegjës</div>");
            if (!string.IsNullOrEmpty(m.SignatureDataUri))
            {
                sb.Append("<img src=\"").Append(m.SignatureDataUri).Append("\" alt=\"Nënshkrim\" class=\"signature-img\" />");
            }
            else if (!string.IsNullOrEmpty(m.SignatureUrl))
            {
                sb.Append("<img src=\"").Append(System.Net.WebUtility.HtmlEncode(m.SignatureUrl)).Append("\" alt=\"Nënshkrim\" class=\"signature-img\" />");
            }
            else
            {
                sb.Append("<div class='signature-line'></div>");
            }
            if (!string.IsNullOrEmpty(m.DoctorDisplayName))
            {
                sb.Append("<div class='doctor-name'>").Append(System.Net.WebUtility.HtmlEncode(m.DoctorDisplayName)).Append("</div>");
            }
            sb.Append("</td>");

            sb.Append("</tr></table>");
            sb.Append("</div>");

            sb.Append("</div>");
            sb.Append("</body></html>");

            return sb.ToString();
        }
    }

    internal static class PdfHtmlExtensions
    {
        public static void AppendRow(this StringBuilder sb, string label, string value)
        {
            sb.Append("<tr><th>")
              .Append(System.Net.WebUtility.HtmlEncode(label))
              .Append("</th><td>")
              .Append(System.Net.WebUtility.HtmlEncode(value ?? "—"))
              .Append("</td></tr>");
        }
    }
}