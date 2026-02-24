# ClinicOps API – Frontend Integration Guide

Base URL: `https://your-api-host` (e.g. `http://localhost:5258`)

All authenticated endpoints require header:  
`Authorization: Bearer <access_token>`

---

## API që e merr raportin PDF të rastit (dhe profilin e mjekut)

| Çfarë merr | Metoda | API |
|------------|--------|-----|
| **Raporti PDF i rastit** (me nënshkrim dhe vulë mjeku) | GET | `/api/PatientCase/{id}/pdf` |
| **Profili i mjekut** (emri, nënshkrimi, vula) | GET | `/api/DoctorProfile/profile` |
| Ndrysho emrin e mjekut | PUT | `/api/DoctorProfile/profile` (body: `{ "displayName": "..." }`) |
| Ngarko nënshkrim (imazh) | POST | `/api/DoctorProfile/profile/signature` (multipart, `file`) |
| Ngarko vulë (imazh) | POST | `/api/DoctorProfile/profile/stamp` (multipart, `file`) |

- Për PDF: `id` = Guid i rastit (PatientCase). Përgjigjja është skedar PDF (`application/pdf`).
- Për DoctorProfile: vetëm përdorues me rol **Doctor**; header `Authorization: Bearer <token>`.

---

## 1. Authentication

### 1.1 Login (any role)
- **POST** `/api/auth/login`  
- **Body:** `{ "email": "string", "password": "string" }`  
- **Response:**  
  - `accessToken`, `expiresAtUtc`, `user`: `{ id, email, clinicId, clinicName, role }`  
- **Roles:** `SuperAdmin`, `ClinicAdmin`, `Doctor`, `Nurse`, `LabTechnician`  
- **Clinic:** For clinic users, `user.clinicId` and `user.clinicName` are set. For SuperAdmin they are null.

### 1.2 Apply for clinic (public, no auth)
- **POST** `/api/auth/apply`  
- **Body:** `{ "clinicName": "string", "email": "string", "password": "string" }`  
- **Response:** 200 + message. Application is created as **Pending**. SuperAdmin must approve before the clinic can login.

---

## 2. SuperAdmin – Clinic applications

**All require:** `Authorization: Bearer <superadmin_token>`

### 2.1 List applications
- **GET** `/api/ClinicApplication`  
- **Query:** `status` (optional): `Pending` | `Approved` | `Rejected`  
- **Response:** Array of  
  - `id`, `clinicName`, `adminEmail`, `status`, `statusDisplay`, `createdAtUtc`, `reviewedAtUtc`, `reviewNote`

### 2.2 Approve application
- **POST** `/api/ClinicApplication/{id}/approve`  
- **Body (optional):** `{ "reviewNote": "string" }`  
- **Effect:** Creates **Clinic** and **ClinicAdmin** user with the email/password from the application. Application status → **Approved**.  
- **Response:** `{ message, clinicId, adminUserId }`  
- After this, the clinic admin can login with the same email/password they used when applying.

### 2.3 Reject application
- **POST** `/api/ClinicApplication/{id}/reject`  
- **Body (optional):** `{ "reviewNote": "string" }`  
- **Response:** `{ message }`

---

## 3. ClinicAdmin – Clinic users (Doctors, Nurses, Lab)

**Require:** `Authorization: Bearer <clinic_admin_token>` (or SuperAdmin with `clinicId` for list/create)

### 3.1 List clinic users
- **GET** `/api/ClinicUser`  
- **Query (optional):**  
  - `clinicId` (SuperAdmin only): which clinic to list  
  - `role`: `Doctor` | `Nurse` | `LabTechnician`  
- **Response:** Array of  
  - `id`, `email`, `role`, `isActive`, `createdAt`  
- **Note:** ClinicAdmin sees only their clinic. SuperAdmin can pass `clinicId` or omit (default clinic).

### 3.2 Create clinic user
- **POST** `/api/ClinicUser`  
- **Body:** `{ "email": "string", "password": "string", "role": "Doctor" | "Nurse" | "LabTechnician" }`  
- **Query (SuperAdmin only, optional):** `clinicId`  
- **Response:** 201 + same shape as list item (`id`, `email`, `role`, `isActive`, `createdAt`)  
- **Note:** New user is tied to the clinic (from token or query). When they login, `user.clinicId` and `user.clinicName` are set and JWT contains `clinicId` claim.

---

## 4. Clinic profile / card (after clinic login)

**Require:** `Authorization: Bearer <clinic_user_token>` (any role with `clinicId` in token; SuperAdmin cannot use these – no clinicId).

Used for the clinic’s own dashboard: show and edit the clinic “card” (name, logo, address, phone, description).

### 4.1 Get clinic profile
- **GET** `/api/Clinic/profile`  
- **Response:** `ClinicProfileDto`: `id`, `name`, `address`, `phone`, `logoUrl`, `description`, `createdAt`, `isActive`  
- **Note:** Returns the clinic of the logged-in user. If user has no clinic (e.g. SuperAdmin), returns 400.

### 4.2 Update clinic profile
- **PUT** `/api/Clinic/profile`  
- **Body:** `UpdateClinicProfileRequest` – all optional: `name`, `address`, `phone`, `logoUrl`, `description`  
- **Response:** Updated `ClinicProfileDto`  
- **Note:** Only the logged-in clinic is updated. Send only fields you want to change (or all). Omitted fields are left unchanged.

### 4.3 Upload clinic logo
- **POST** `/api/Clinic/profile/logo`  
- **Content-Type:** `multipart/form-data`  
- **Body:** one file field (e.g. `file`) – image: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`  
- **Response:** Updated `ClinicProfileDto` with new `logoUrl` (e.g. `/uploads/clinics/{clinicId}/logo.png`)  
- **Serving:** Logo is served by the API: `GET {baseUrl}/uploads/clinics/{clinicId}/logo.{ext}` (static files from `wwwroot`).

**Backend migration:** Add columns `LogoUrl` (string, max 500) and `Description` (string, max 2000) to the `Clinics` table if not present (`dotnet ef migrations add AddClinicProfileFields` then `dotnet ef database update`).

---

## 4.5 Doctor profile (Doctor role only)

**Require:** `Authorization: Bearer <doctor_token>` and user must be in role **Doctor**.

Used so the logged-in doctor can set their display name, signature image, and stamp image (e.g. for reports and “signed by” in the UI).

### 4.5.1 Get doctor profile
- **GET** `/api/DoctorProfile/profile`  
- **Response:** `DoctorProfileDto`: `userId`, `email`, `displayName`, `signatureUrl`, `stampUrl`  
- **Note:** `displayName` is the doctor’s chosen name (e.g. "Dr. John Smith"); if not set, the API returns `email` as the display value.

### 4.5.2 Update doctor display name
- **PUT** `/api/DoctorProfile/profile`  
- **Body:** `{ "displayName": "string" }` (optional, max 200)  
- **Response:** Updated `DoctorProfileDto`

### 4.5.3 Upload signature image
- **POST** `/api/DoctorProfile/profile/signature`  
- **Content-Type:** `multipart/form-data`  
- **Body:** one file field (e.g. `file`) – image: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`  
- **Response:** Updated `DoctorProfileDto` with new `signatureUrl` (e.g. `/uploads/doctors/{userId}/signature.png`)

### 4.5.4 Upload stamp image
- **POST** `/api/DoctorProfile/profile/stamp`  
- **Content-Type:** `multipart/form-data`  
- **Body:** one file field (e.g. `file`) – image: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`  
- **Response:** Updated `DoctorProfileDto` with new `stampUrl` (e.g. `/uploads/doctors/{userId}/stamp.png`)

**Serving:** Images are under `wwwroot`: `GET {baseUrl}/uploads/doctors/{userId}/signature.{ext}` and `stamp.{ext}`.

**Frontend:** After login, if `user.role === 'Doctor'`, call `GET /api/DoctorProfile/profile` to show the current doctor’s name and optional signature/stamp in the header or report views. Use the profile page to update display name and upload signature/stamp.

---

## 5. Patients (clinic-scoped)

### 5.1 Register patient (reception)
- **POST** `/api/Patient/register`  
- **Body:** `{ "firstName", "lastName", "dateOfBirth", "gender?", "phone?", "notes?", "clinicId?" }`  
  - `clinicId` only for SuperAdmin (optional; default clinic used if omitted).  
- **Response:** Patient + `patientCaseId`, `patientCaseStatus` (e.g. Waiting).

### 5.2 List patients
- **GET** `/api/Patient`  
- **Query (SuperAdmin only):** `clinicId` (optional)  
- **Response:** Array of patient DTOs with `patientCaseId`, `patientCaseStatus`.

---

## 6. Patient cases (nurse / doctor flow)

### 6.1 List patient cases
- **GET** `/api/PatientCase`  
- **Query:** `status` (optional): `Waiting` | `InProgress` | `InConsultation` | `Completed` | `Finished`  
- **Response:** Array of `{ id, patientId, patientFirstName, patientLastName, status, createdAt }`

### 6.2 Get case detail (nurse form / doctor panel)
- **GET** `/api/PatientCase/{id}`  
- **Response:**  
  - Case + patient: `patientFirstName`, `patientLastName`, `patientDateOfBirth`, `patientPhone`, `patientGender`, `status`, `notes`, `createdAt`, `completedAt`  
  - `latestVitals`: `weightKg`, `systolicPressure`, `diastolicPressure`, `temperatureC`, `heartRate`, `recordedAt`  
  - `medicalReport`: `anamneza`, `diagnosis`, `therapy`, `createdAt`, `doctorId`

### 6.3 Nurse – Submit vitals
- **POST** `/api/PatientCase/{id}/vitals`  
- **Body:** `{ "weightKg?", "systolicPressure?", "diastolicPressure?", "temperatureC?", "heartRate?" }`  
- **Response:** Saved vitals DTO.  
- **SignalR:** Event `VitalsUpdated(patientCaseId, vitalsDto)` is sent to the clinic group so the doctor panel can update in real time.

### 6.4 Doctor – Submit anamneza, diagnosis and therapy
- **POST** `/api/PatientCase/{id}/report`  
- **Body:** `{ "anamneza?": "string", "diagnosis": "string", "therapy": "string" }` – `anamneza` (anamnesis / patient history) is optional.  
- **Response:** Medical report DTO (`anamneza`, `diagnosis`, `therapy`, `createdAt`, `doctorId`, etc.).  
- **SignalR:** Event `ReportUpdated(patientCaseId, reportDto)` is sent to the clinic group.

### 6.5 Update case status
- **PATCH** `/api/PatientCase/{id}/status?status=InConsultation`  
- **Query:** `status`: `Waiting` | `InProgress` | `InConsultation` | `Completed` | `Finished`  
- **SignalR:** Event `CaseStatusChanged(patientCaseId, status)` is sent to the clinic group.

### 6.6 Download case report as PDF
- **GET** `/api/PatientCase/{id}/pdf`  
- **Response:** PDF file (`application/pdf`) with patient case report (patient info, vitals, anamneza, diagnosis, therapy). File name: `CaseReport_{LastName}_{FirstName}_{caseId}.pdf`.  
- **Implementation:** HTML template rendered to PDF via PuppeteerSharp (Chromium).

---

## 7. SignalR (real-time)

- **Endpoint:** `/hubs/clinic`  
- **Auth:** Pass JWT via query: `?access_token=<token>` or via `Authorization: Bearer <token>` (if supported by your client).

### 7.1 Client → Server
- **JoinClinic(clinicId)** – Join group `clinic_{clinicId}` to receive all events for that clinic.  
- **JoinPatientCase(patientCaseId)** – Optional; join `case_{patientCaseId}` for a single case.

### 7.2 Server → Client
- **VitalsUpdated(patientCaseId, vitalsDto)** – When nurse saves vitals.  
- **ReportUpdated(patientCaseId, reportDto)** – When doctor saves report.  
- **CaseStatusChanged(patientCaseId, status)** – When case status is updated.

Use these to refresh the doctor panel (and nurse view if needed) in real time.

---

## 8. Role summary

| Role           | Typical use                          |
|----------------|--------------------------------------|
| SuperAdmin     | Approve/reject clinic applications   |
| ClinicAdmin    | Create/list Doctors, Nurses, Labs   |
| Doctor         | Patient cases, diagnosis/therapy    |
| Nurse          | Patient cases, vitals               |
| LabTechnician  | Lab-related flows (extend as needed)|

After login, use `user.role` and `user.clinicId` / `user.clinicName` to show the correct dashboard and call the right APIs. Clinic users are automatically scoped to their clinic; SuperAdmin can pass `clinicId` where documented.
