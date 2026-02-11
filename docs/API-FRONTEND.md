# ClinicOps API – Frontend Integration Guide

Base URL: `https://your-api-host` (e.g. `http://localhost:5258`)

All authenticated endpoints require header:  
`Authorization: Bearer <access_token>`

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

## 4. Patients (clinic-scoped)

### 4.1 Register patient (reception)
- **POST** `/api/Patient/register`  
- **Body:** `{ "firstName", "lastName", "dateOfBirth", "gender?", "phone?", "notes?", "clinicId?" }`  
  - `clinicId` only for SuperAdmin (optional; default clinic used if omitted).  
- **Response:** Patient + `patientCaseId`, `patientCaseStatus` (e.g. Waiting).

### 4.2 List patients
- **GET** `/api/Patient`  
- **Query (SuperAdmin only):** `clinicId` (optional)  
- **Response:** Array of patient DTOs with `patientCaseId`, `patientCaseStatus`.

---

## 5. Patient cases (nurse / doctor flow)

### 5.1 List patient cases
- **GET** `/api/PatientCase`  
- **Query:** `status` (optional): `Waiting` | `InProgress` | `InConsultation` | `Completed` | `Finished`  
- **Response:** Array of `{ id, patientId, patientFirstName, patientLastName, status, createdAt }`

### 5.2 Get case detail (nurse form / doctor panel)
- **GET** `/api/PatientCase/{id}`  
- **Response:**  
  - Case + patient: `patientFirstName`, `patientLastName`, `patientDateOfBirth`, `patientPhone`, `patientGender`, `status`, `notes`, `createdAt`, `completedAt`  
  - `latestVitals`: `weightKg`, `systolicPressure`, `diastolicPressure`, `temperatureC`, `heartRate`, `recordedAt`  
  - `medicalReport`: `diagnosis`, `therapy`, `createdAt`, `doctorId`

### 5.3 Nurse – Submit vitals
- **POST** `/api/PatientCase/{id}/vitals`  
- **Body:** `{ "weightKg?", "systolicPressure?", "diastolicPressure?", "temperatureC?", "heartRate?" }`  
- **Response:** Saved vitals DTO.  
- **SignalR:** Event `VitalsUpdated(patientCaseId, vitalsDto)` is sent to the clinic group so the doctor panel can update in real time.

### 5.4 Doctor – Submit diagnosis/therapy
- **POST** `/api/PatientCase/{id}/report`  
- **Body:** `{ "diagnosis": "string", "therapy": "string" }`  
- **Response:** Medical report DTO.  
- **SignalR:** Event `ReportUpdated(patientCaseId, reportDto)` is sent to the clinic group.

### 5.5 Update case status
- **PATCH** `/api/PatientCase/{id}/status?status=InConsultation`  
- **Query:** `status`: `Waiting` | `InProgress` | `InConsultation` | `Completed` | `Finished`  
- **SignalR:** Event `CaseStatusChanged(patientCaseId, status)` is sent to the clinic group.

---

## 6. SignalR (real-time)

- **Endpoint:** `/hubs/clinic`  
- **Auth:** Pass JWT via query: `?access_token=<token>` or via `Authorization: Bearer <token>` (if supported by your client).

### 6.1 Client → Server
- **JoinClinic(clinicId)** – Join group `clinic_{clinicId}` to receive all events for that clinic.  
- **JoinPatientCase(patientCaseId)** – Optional; join `case_{patientCaseId}` for a single case.

### 6.2 Server → Client
- **VitalsUpdated(patientCaseId, vitalsDto)** – When nurse saves vitals.  
- **ReportUpdated(patientCaseId, reportDto)** – When doctor saves report.  
- **CaseStatusChanged(patientCaseId, status)** – When case status is updated.

Use these to refresh the doctor panel (and nurse view if needed) in real time.

---

## 7. Role summary

| Role           | Typical use                          |
|----------------|--------------------------------------|
| SuperAdmin     | Approve/reject clinic applications   |
| ClinicAdmin    | Create/list Doctors, Nurses, Labs   |
| Doctor         | Patient cases, diagnosis/therapy    |
| Nurse          | Patient cases, vitals               |
| LabTechnician  | Lab-related flows (extend as needed)|

After login, use `user.role` and `user.clinicId` / `user.clinicName` to show the correct dashboard and call the right APIs. Clinic users are automatically scoped to their clinic; SuperAdmin can pass `clinicId` where documented.
