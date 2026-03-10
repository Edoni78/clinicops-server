# Lab Results – Backend Implementation and Frontend Prompt

This document describes the **lab results** feature implemented in the ClinicOps backend and provides a prompt you can use to implement or finish the frontend.

---

## What Was Done in the Backend

1. **Lab result PDFs are tied to a patient case**
   - Any authenticated user who has access to the case (any role: Doctor, Nurse, LabTechnician, ClinicAdmin, SuperAdmin) can **upload** a PDF as a lab result for that case.
   - Each lab result is stored as a file on the server and a row in the `LabResults` table (linked to `PatientCaseId` and `ClinicId`).

2. **Case report PDF download includes lab results**
   - When you download the case report via **GET** `/api/PatientCase/{id}/pdf`:
     - **First page(s):** The existing case report (patient data, vitals, anamneza, diagnosis, therapy, doctor signature/stamp).
     - **Next pages:** If the case has any lab result PDFs, they are appended in **upload order** (oldest first). So the full PDF is: **page 1 = case report**, **page 2+ = lab result PDFs**.
   - If the case has **no** lab results, the response is unchanged: a single PDF with only the case report.

3. **APIs added**
   - **List** lab results for a case.
   - **Upload** a PDF as a lab result for a case.
   - **Download** a single lab result PDF file (with same auth as the case).

4. **Database**
   - `LabResult` entity: `Id`, `ClinicId`, `PatientCaseId`, `FileName`, `FilePath`, `ContentType`, `UploadedAt`, `UploadedById` (string, optional – Identity user id).
   - A migration was added to change `UploadedById` from `Guid` to `string` so it matches ASP.NET Identity user ids.

5. **Storage**
   - Lab PDF files are stored under the app’s **content root** in:  
     `LabUploads/{patientCaseId}/{labResultId}.pdf`  
   - They are **not** under `wwwroot`, so they are only accessible via the API (authenticated download endpoint).

---

## API Summary for Lab Results

Base URL and auth: same as the rest of the API (e.g. `http://localhost:5258`, header `Authorization: Bearer <access_token>`).

| Action | Method | Endpoint | Description |
|--------|--------|----------|-------------|
| List lab results for a case | **GET** | `/api/PatientCase/{id}/labresults` | Returns array of lab result DTOs (see below). |
| Upload a lab result PDF | **POST** | `/api/PatientCase/{id}/labresults` | Body: `multipart/form-data` with one file (PDF). Returns created lab result DTO. |
| Download one lab result file | **GET** | `/api/PatientCase/{id}/labresults/{labId}/file` | Returns the PDF file (`application/pdf`). |
| Download full case report (with labs) | **GET** | `/api/PatientCase/{id}/pdf` | Same as before; if the case has lab results, the PDF has report first, then lab PDFs as extra pages. |

- `{id}` = patient case GUID.  
- `{labId}` = lab result GUID.

### List response (GET .../labresults)

Array of objects:

```json
[
  {
    "id": "guid",
    "patientCaseId": "guid",
    "fileName": "original-name.pdf",
    "downloadUrl": "/api/PatientCase/{patientCaseId}/labresults/{id}/file",
    "contentType": "application/pdf",
    "uploadedAt": "2026-03-10T22:00:00Z",
    "uploadedById": "user-id-string-or-null"
  }
]
```

- **downloadUrl** is a relative path. The frontend should call:  
  `GET {baseUrl}{downloadUrl}` with the same `Authorization` header to get the file.

### Upload (POST .../labresults)

- **Content-Type:** `multipart/form-data`.
- **Body:** one file field (e.g. `file`) – **PDF only**.
- **Response:** 201 Created with the same shape as one list item (including `downloadUrl` and `id`).

### Download single file (GET .../labresults/{labId}/file)

- **Response:** PDF bytes with `Content-Type: application/pdf` and filename from the stored `FileName`.
- Use the same Bearer token as for other APIs.

---

## Frontend Prompt (copy this to finish the frontend)

You can paste the following (or adapt it) when asking to implement the lab results UI:

---

**Context:** ClinicOps backend already supports lab results. Any authenticated user can add PDF lab results to a patient case. The case report download (PDF) includes the report on the first page and all lab result PDFs as following pages. See `docs/LABS.md` for API details.

**Implement the frontend for lab results:**

1. **Case detail / case view**
   - On the patient case detail (or equivalent) page, add a **Lab results** section.
   - **List:** Call `GET /api/PatientCase/{caseId}/labresults` and show the list (e.g. file name, upload date). Each item should have a **Download** link/button that uses `GET {baseUrl}{downloadUrl}` with the auth token (e.g. open in new tab with token, or download via fetch with `Authorization: Bearer <token>` and blob download).
   - **Upload:** A control to select a PDF file and submit via `POST /api/PatientCase/{caseId}/labresults` with `multipart/form-data` (field name e.g. `file`). On success, refresh the lab results list (or add the new item to the list) and show a short success message.

2. **Who can use it**
   - Any role that can open the patient case (Doctor, Nurse, LabTechnician, ClinicAdmin, etc.) can list, upload, and download lab results. No extra role check is required if the user can already see the case.

3. **Case report PDF**
   - The existing “Download case report” (or “Download PDF”) button that calls `GET /api/PatientCase/{id}/pdf` does not need to change. The backend now returns a PDF that includes the case report first and then all lab result PDFs as additional pages. If there are no labs, the PDF is unchanged (single report).

4. **UX**
   - Show an empty state when there are no lab results (e.g. “No lab results yet” and the upload control).
   - Optional: show upload progress or loading state during upload.
   - Optional: confirm before deleting a lab result if you add a delete endpoint later (backend does not expose delete in this version; you can add it later if needed).

---

## Backend Details (for reference)

- **Entity:** `Domain/Entities/LabResult.cs` – `UploadedById` is now `string?` (Identity user id).
- **DTO:** `API/DTOs/LabResult/LabResultDto.cs` – used for list and upload response.
- **Endpoints:** All in `API/Controllers/PatientCaseController.cs`:
  - `ListLabResults`, `UploadLabResult`, `DownloadLabResultFile`, and `MergeReportWithLabPdfs` (used when generating the case PDF).
- **PDF merge:** Implemented with **PdfSharpCore**. The case report is generated with PuppeteerSharp as before; then, if the case has lab results, their PDF files are merged after it in upload order.
- **Migration:** `LabResultUploadedByIdString` – run `dotnet ef database update` to apply.

After applying the migration and deploying, the backend is ready for the frontend to call the lab results APIs and to use the combined case report PDF.
