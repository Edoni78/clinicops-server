# Frontend prompt: Clinic profile / card and media (dashboard)

Use this prompt to implement the **Clinic profile (card) and media management** in the ClinicOps frontend. The backend is ready; implement the UI that talks to it.

---

## Context

- After a **clinic user** logs in (ClinicAdmin, Doctor, Nurse, or LabTechnician), the app knows their clinic via `user.clinicId` and `user.clinicName` from the login response.
- The backend exposes **Clinic profile** APIs so the clinic can manage its “card”: name, logo, address, phone, and description.
- **SuperAdmin** has no clinic, so do not show this section to SuperAdmin.

---

## What to build

### 1. New dashboard page (only for clinic users)

- **Route:** e.g. `/dashboard/clinic-profile` or `/clinic-profile` (or under Settings: “Clinic profile” / “Clinic card”).
- **Visibility:** Show in the dashboard/sidebar **only when the logged-in user has a clinic** (i.e. `user.clinicId` is present). Hide for SuperAdmin.

### 2. Clinic card (view mode)

- **Data:** Load the clinic profile with **GET** `/api/Clinic/profile` (send `Authorization: Bearer <token>`).
- **Display a “card”** with:
  - **Logo:** If `logoUrl` exists, show image (use full URL: `{apiBaseUrl}{logoUrl}`, e.g. `http://localhost:5258/uploads/clinics/xxx/logo.png`). Otherwise show a placeholder (e.g. initials or icon).
  - **Name:** `name`
  - **Address / location:** `address`
  - **Phone:** `phone`
  - **Description / info:** `description` (can be multi-line or short “about” text)
- **Edit entry:** A button/link like “Edit profile” or “Manage clinic card” that switches to the edit form (or opens a modal).

### 3. Edit clinic profile (form)

- **Form fields:**  
  - Name (text)  
  - Address / location (text)  
  - Phone (text)  
  - Description / info (textarea, optional)  
  - Logo: either  
    - **Option A:** Upload file (image) and call **POST** `/api/Clinic/profile/logo` with `multipart/form-data` (field name e.g. `file`).  
    - **Option B:** Optional “Logo URL” text field and save via **PUT** `/api/Clinic/profile` with `logoUrl` in the body.
- **Save:** On submit, call **PUT** `/api/Clinic/profile` with body:  
  `{ "name", "address", "phone", "description", "logoUrl" }` (only include fields that the user can edit).  
  If the user uploaded a logo (Option A), first call **POST** `/api/Clinic/profile/logo` with the file; the response returns the updated profile including the new `logoUrl`, then you can optionally call **PUT** to update the rest, or just refresh the card from the POST response.
- **Success:** After save, refresh the clinic card (e.g. call GET profile again or use the response data) and show a success message.
- **Errors:** Show validation or server error messages (e.g. 400, 401, 404).

### 4. Backend API summary (for reference)

- **GET** `/api/Clinic/profile`  
  - Returns: `{ id, name, address, phone, logoUrl, description, createdAt, isActive }`
- **PUT** `/api/Clinic/profile`  
  - Body: `{ "name?", "address?", "phone?", "logoUrl?", "description?" }` (all optional)
  - Returns: same shape as GET
- **POST** `/api/Clinic/profile/logo`  
  - Body: `multipart/form-data` with one image file (e.g. `file`); allowed: jpg, jpeg, png, gif, webp  
  - Returns: updated profile with new `logoUrl` (e.g. `/uploads/clinics/{clinicId}/logo.png`)

All three require **Authorization: Bearer &lt;access_token&gt;** and only work for users that have a clinic (backend returns 400 for SuperAdmin).

---

## Technical notes

- **Base URL:** Use the same API base URL as the rest of the app (e.g. from env: `VITE_API_URL` or `REACT_APP_API_URL`).
- **Logo URL in UI:** If `logoUrl` is a path like `/uploads/clinics/xxx/logo.png`, the full image URL is `{apiBaseUrl}{logoUrl}` (no extra slash if base URL has no trailing slash).
- **Auth:** Use the same axios/fetch setup as other dashboard pages (attach the JWT from login).

---

## Acceptance checklist

- [ ] Clinic users see a “Clinic profile” / “Clinic card” page (or section) in the dashboard; SuperAdmin does not.
- [ ] The card shows logo (or placeholder), name, address, phone, description from GET profile.
- [ ] User can edit name, address, phone, description and save via PUT profile.
- [ ] User can upload a logo (POST profile/logo) and the card updates with the new logo.
- [ ] Errors (e.g. no token, 400, 404) are handled and shown to the user.

Use the existing API documentation in `docs/API-FRONTEND.md` (section 4 – Clinic profile / card) for full request/response details.
