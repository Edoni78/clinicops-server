# Add GDPR Support Safely to Existing .NET Backend

Goal: add GDPR/privacy features to the existing clinic SaaS backend without breaking current working functionality.

Important rule for Cursor:
Do not rewrite existing working models, services, controllers, authentication, or patient flows.
Only add small, additive changes around the existing data.

---

## 1. General Rules

Follow these rules strictly:

- Do not rename existing tables.
- Do not rename existing columns.
- Do not remove existing properties from Patient, Appointment, Visit, EMR, User, or Clinic models.
- Do not change existing endpoint routes unless required.
- Do not change existing frontend response shapes unless a new field is optional.
- Prefer adding nullable columns instead of required columns.
- Prefer new tables for GDPR features instead of modifying core medical logic.
- Keep existing business flow working exactly as it is.

---

## 2. Add Audit Logs

Add a new table called `AuditLogs`.

Purpose: track who viewed, created, updated, deleted, exported, or anonymized patient data.

Suggested model:

```csharp
public class AuditLog
{
    public int Id { get; set; }

    public int? ClinicId { get; set; }
    public string? UserId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

Actions to log:

- PatientViewed
- PatientCreated
- PatientUpdated
- PatientDeleted
- PatientAnonymized
- PatientExported
- MedicalRecordViewed
- MedicalRecordUpdated
- Login
- Logout

Add an `IAuditLogService` and call it from existing services/controllers after important actions.

Do not block the main action if audit logging fails. Log the error, but do not break the working SaaS flow.

---

## 3. Add Consent Tracking

Add a new table called `PatientConsents`.

Purpose: store patient consent for data processing.

Suggested model:

```csharp
public class PatientConsent
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public int? ClinicId { get; set; }

    public bool HasGivenConsent { get; set; }
    public string ConsentType { get; set; } = "MedicalDataProcessing";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? WithdrawnAtUtc { get; set; }

    public string? GivenByUserId { get; set; }
    public string? Notes { get; set; }
}
```

Add endpoints:

```txt
POST /api/patients/{patientId}/consent
GET  /api/patients/{patientId}/consent
POST /api/patients/{patientId}/consent/withdraw
```

Do not force consent immediately in existing patient creation unless frontend already supports it.
For now, allow consent to be added after patient creation.

---

## 4. Add Patient Data Export

Add a new endpoint:

```txt
GET /api/patients/{patientId}/gdpr/export
```

Purpose: export all data related to one patient.

Return JSON first. PDF can be added later.

Export should include existing data if available:

- Patient profile
- Appointments
- Visits
- EMR records
- Lab results metadata
- Uploaded file paths or names
- Consent records

Important:
Only users from the same clinic can export the patient.
Log action as `PatientExported`.

Do not change existing patient GET endpoints.

---

## 5. Add Soft Delete / Anonymization

Do not hard delete patient medical data by default.

Add optional nullable fields to Patient only if they do not already exist:

```csharp
public bool IsDeleted { get; set; } = false;
public bool IsAnonymized { get; set; } = false;
public DateTime? DeletedAtUtc { get; set; }
public DateTime? AnonymizedAtUtc { get; set; }
```

If adding these fields risks breaking something, create a separate table:

```csharp
public class PatientPrivacyState
{
    public int Id { get; set; }
    public int PatientId { get; set; }

    public bool IsDeleted { get; set; }
    public bool IsAnonymized { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
    public DateTime? AnonymizedAtUtc { get; set; }
}
```

Add endpoint:

```txt
POST /api/patients/{patientId}/gdpr/anonymize
```

Anonymization should remove or replace personal identifiers:

- FullName -> "Anonymized Patient"
- Email -> null
- PhoneNumber -> null
- Address -> null
- PersonalNumber -> null
- DateOfBirth -> null if allowed by current model

Keep medical records if needed for clinic/legal reasons.

Log action as `PatientAnonymized`.

---

## 6. Enforce Clinic Isolation

For every GDPR endpoint, always verify:

```csharp
patient.ClinicId == currentUser.ClinicId
```

Never allow one clinic to access another clinic’s patients.

Do not refactor the whole app now.
Only apply this strictly to new GDPR endpoints and audit/consent/export/anonymization flows.

---

## 7. Add Access Logging for Sensitive Reads

When a user opens sensitive patient data, log it.

Add audit logging to existing endpoints where possible:

- Get patient by ID
- Get patient medical record / EMR
- Get visit details
- Download lab report or file

Do not log every list endpoint at first because it may create too many logs.
Start with detail views and exports.

---

## 8. Secure File Access

For patient files/lab reports:

- Do not expose private medical files by predictable public names.
- Check clinic ownership before returning a file.
- Log file download/open actions.
- Keep current upload logic if it works.
- Add validation only around new downloads if changing upload may break the app.

Recommended action:
Add protected download endpoint later:

```txt
GET /api/files/{fileId}/download
```

But do not break existing file display now.

---

## 9. Add Basic Retention Fields Later

Do not implement automatic deletion now.

Add only simple metadata later if needed:

```csharp
public DateTime? RetentionUntilUtc { get; set; }
```

For now, focus on audit logs, consent, export, and anonymization.

---

## 10. Migration Strategy

Create one safe migration:

```txt
AddAuditLogsPatientConsentsAndPrivacyState
```

Migration should only:

- create AuditLogs table
- create PatientConsents table
- optionally create PatientPrivacyState table
- optionally add nullable privacy fields to Patients

Do not modify existing required columns.
Do not drop anything.

---

## 11. Services to Add

Add these services:

```txt
Services/Gdpr/IAuditLogService.cs
Services/Gdpr/AuditLogService.cs
Services/Gdpr/IPatientGdprService.cs
Services/Gdpr/PatientGdprService.cs
```

Responsibilities:

`AuditLogService`
- create audit logs
- capture user id, clinic id, IP address, user agent

`PatientGdprService`
- export patient data
- anonymize patient
- manage consent
- verify clinic ownership

---

## 12. Controllers to Add

Add a new controller instead of changing existing PatientController too much:

```txt
Controllers/PatientGdprController.cs
```

Suggested routes:

```txt
GET  /api/patients/{patientId}/gdpr/export
POST /api/patients/{patientId}/gdpr/anonymize
GET  /api/patients/{patientId}/consent
POST /api/patients/{patientId}/consent
POST /api/patients/{patientId}/consent/withdraw
```

Keep existing PatientController stable.

---

## 13. Definition of Done

GDPR backend foundation is done when:

- AuditLogs table exists
- PatientConsents table exists
- Sensitive patient reads are logged
- Patient export endpoint works
- Patient anonymization endpoint works
- Consent add/view/withdraw endpoints work
- All new GDPR endpoints check ClinicId
- Existing patient creation, appointment, EMR, and login flows still work
- No existing API response is broken

---

## 14. Very Important

This is not full legal GDPR certification.
This is a strong backend technical foundation for GDPR-style compliance.

Implement safely, step by step.
Do not rewrite the whole SaaS.
Do not break existing working features.
