# Frontend prompt: Doctor profile (name, signature, stamp)

Use this prompt to implement the **Doctor profile** in the ClinicOps frontend. The backend is ready; implement the UI that lets doctors set their display name, signature image, and stamp image, and show “who is logged in” where needed.

---

## Context

- When a user logs in with role **Doctor**, the app receives `user.id`, `user.email`, `user.role`, etc. from the login response.
- The backend exposes **Doctor profile** APIs so the doctor can set:
  - **Display name** (e.g. "Dr. John Smith") – shown in the UI and on reports instead of just email.
  - **Signature image** – uploaded image of their signature (e.g. for “signed by” on reports).
  - **Stamp image** – uploaded image of their stamp/seal.
- These endpoints **only work for users in role Doctor**. Other roles get 403 if they call them.
- Images are stored under `wwwroot/uploads/doctors/{userId}/` and served by the API (e.g. `/uploads/doctors/{userId}/signature.png`).

---

## What to build

### 1. Show “who is logged in” (Doctor only)

- **When:** The logged-in user has `user.role === 'Doctor'`.
- **Where:** e.g. header, sidebar, or top of the doctor dashboard (e.g. “Logged in as **Dr. John Smith**” or “Signed in as **john@clinic.com**” if no display name).
- **How:**  
  - On load (or after login), call **GET** `/api/DoctorProfile/profile` with `Authorization: Bearer <token>`.  
  - Use `displayName` for the label (API returns `email` as display value if `displayName` is not set).  
  - Optionally show a small preview of `signatureUrl` / `stampUrl` if you have space (e.g. in a dropdown or profile menu).

### 2. Doctor profile page (only for Doctors)

- **Route:** e.g. `/dashboard/doctor-profile`, `/doctor-profile`, or under Settings: “My profile” / “Doctor profile”.
- **Visibility:** Show in the dashboard/sidebar **only when** `user.role === 'Doctor'`. Hide for other roles.

### 3. Profile view (display mode)

- **Data:** Load with **GET** `/api/DoctorProfile/profile`.
- **Display:**
  - **Display name:** Show `displayName` (or “Not set” / email if null).
  - **Email:** Show `email` (read-only from backend).
  - **Signature:** If `signatureUrl` exists, show image: `{apiBaseUrl}{signatureUrl}` (e.g. `http://localhost:5258/uploads/doctors/xxx/signature.png`). Otherwise show “No signature uploaded” or placeholder.
  - **Stamp:** If `stampUrl` exists, show image: `{apiBaseUrl}{stampUrl}`. Otherwise show “No stamp uploaded” or placeholder.
- **Edit:** A button like “Edit profile” or “Update signature/stamp” that switches to the edit form or opens a modal.

### 4. Edit doctor profile (form)

- **Display name**
  - Text field (max 200 characters).
  - Save via **PUT** `/api/DoctorProfile/profile` with body: `{ "displayName": "Dr. John Smith" }`.
- **Signature image**
  - File input (image: jpg, jpeg, png, gif, webp).
  - Upload via **POST** `/api/DoctorProfile/profile/signature` with `multipart/form-data`, field name e.g. `file`.
  - Response returns updated profile including new `signatureUrl`; refresh the view or use the response.
- **Stamp image**
  - File input (same allowed formats).
  - Upload via **POST** `/api/DoctorProfile/profile/stamp` with `multipart/form-data`, field name e.g. `file`.
  - Response returns updated profile including new `stampUrl`.
- **Success:** Show a success message and refresh the profile (GET again or use response data).
- **Errors:** Handle 400 (e.g. no file, invalid format), 401 (not logged in), 403 (not a Doctor), 404 and show a clear message.

### 5. Use signature/stamp elsewhere (optional)

- On **case report** or **PDF view**, you can show “Signed by: **{displayName}**” and, if available, the signature and stamp images (using the same `{apiBaseUrl}{signatureUrl}` and `{apiBaseUrl}{stampUrl}`). The backend PDF may already include this in the future; the frontend can show the same data in the report/print view.

---

## API summary (for reference)

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| GET | `/api/DoctorProfile/profile` | — | `{ userId, email, displayName, signatureUrl, stampUrl }` |
| PUT | `/api/DoctorProfile/profile` | `{ "displayName": "string" }` (optional, max 200) | Updated `DoctorProfileDto` |
| POST | `/api/DoctorProfile/profile/signature` | `multipart/form-data`, file field e.g. `file` (image) | Updated `DoctorProfileDto` with new `signatureUrl` |
| POST | `/api/DoctorProfile/profile/stamp` | `multipart/form-data`, file field e.g. `file` (image) | Updated `DoctorProfileDto` with new `stampUrl` |

- **Auth:** All require `Authorization: Bearer <access_token>`.
- **Role:** User must be in role **Doctor** (backend returns 403 otherwise).
- **Image URL in UI:** If `signatureUrl` is `/uploads/doctors/xxx/signature.png`, full URL = `{apiBaseUrl}{signatureUrl}` (e.g. `http://localhost:5258/uploads/doctors/xxx/signature.png`).

---

## Technical notes

- **Base URL:** Use the same API base URL as the rest of the app (e.g. `VITE_API_URL` or `REACT_APP_API_URL`). No extra slash when concatenating: `baseUrl + profile.signatureUrl`.
- **File upload:** Use `FormData`, append the file with the same name the backend expects (e.g. `file`). Content-Type will be `multipart/form-data` (browser sets it when you use FormData).
- **Allowed image types:** jpg, jpeg, png, gif, webp. You can restrict the file input with `accept="image/jpeg,image/png,image/gif,image/webp"` and still validate on upload.

---

## Acceptance checklist

- [ ] When a Doctor is logged in, the UI shows who they are (e.g. header: “Logged in as **{displayName}**” or email).
- [ ] Doctors have a “Doctor profile” (or “My profile”) page; other roles do not see it.
- [ ] The profile page shows display name, email, signature image (or placeholder), stamp image (or placeholder) from GET profile.
- [ ] Doctor can update display name via PUT and see the change reflected.
- [ ] Doctor can upload a signature image (POST profile/signature); the page shows the new image (full URL = baseUrl + signatureUrl).
- [ ] Doctor can upload a stamp image (POST profile/stamp); the page shows the new image.
- [ ] Errors (401, 403, 400, 404) are handled and shown to the user.

For full API details see `docs/API-FRONTEND.md` (section 4.5 – Doctor profile).
