# Services – Backend Implementation and Frontend Prompt

This document describes the **Services** feature (clinic service names and prices) implemented in the ClinicOps backend and provides a prompt you can use to implement the frontend.

---

## What Was Done in the Backend

1. **Services are per-clinic**
   - Each clinic has its own list of services (e.g. "Kontrolle", "Kontrolle + Analiza", "Kontrolle + Analiza + ultraze").
   - Each service has a **name** (max 300 chars) and a **price** (decimal ≥ 0).

2. **Anyone with clinic access can manage services**
   - Any authenticated user who has access to the clinic (Doctor, Nurse, LabTechnician, ClinicAdmin) can add, update, and delete services.
   - SuperAdmin can manage services for any clinic by passing `clinicId` as a query parameter.

3. **CRUD**
   - **List** – GET list of active services for the clinic.
   - **Get by id** – GET a single service.
   - **Create** – POST a new service (name + price).
   - **Update** – PUT to change name and/or price.
   - **Delete** – DELETE soft-deactivates the service (sets `IsActive = false`); it no longer appears in the list but the row remains in the database.

4. **Database**
   - New entity: **Service** (`Domain/Entities/Service.cs`).
   - Columns: `Id` (Guid), `ClinicId` (Guid), `Name` (string, max 300), `Price` (decimal), `CreatedAt` (DateTime), `IsActive` (bool).
   - Migration: **AddServicesTable**.

---

## API Summary for Services

Base URL and auth: same as the rest of the API (e.g. `http://localhost:5258`, header `Authorization: Bearer <access_token>`).

| Action        | Method   | Endpoint              | Description |
|---------------|----------|------------------------|-------------|
| List services | **GET**  | `/api/Service`        | Returns active services for the clinic. |
| Get one       | **GET**  | `/api/Service/{id}`   | Returns one service by id. |
| Create        | **POST** | `/api/Service`        | Body: `{ "name": "string", "price": number }`. |
| Update        | **PUT**  | `/api/Service/{id}`   | Body: `{ "name": "string?", "price": number? }` (both optional; only sent fields are updated). |
| Delete        | **DELETE** | `/api/Service/{id}` | Soft-delete (service disappears from list). |

- **Clinic scope:** Clinic users get services for their clinic from the JWT. **SuperAdmin** (no `clinicId` in token) must pass **`?clinicId={guid}`** on list/create/update/delete to choose the clinic; if omitted, the default test clinic is used when available.

---

## Request and Response Shapes

### List (GET /api/Service)

**Response:** array of:

```json
{
  "id": "guid",
  "clinicId": "guid",
  "name": "Kontrolle + Analiza",
  "price": 25.00,
  "createdAt": "2026-03-10T22:00:00Z",
  "isActive": true
}
```

### Get one (GET /api/Service/{id})

**Response:** same object as one element of the list.

### Create (POST /api/Service)

**Body:**

```json
{
  "name": "Kontrolle + Analiza + ultraze",
  "price": 35.50
}
```

- `name`: required, max 300 characters.
- `price`: required, ≥ 0.

**Response:** 201 Created with the created service object (same shape as list item).

### Update (PUT /api/Service/{id})

**Body:** (all optional; only include fields to change)

```json
{
  "name": "Kontrolle + Analiza",
  "price": 28.00
}
```

**Response:** 200 OK with the updated service object.

### Delete (DELETE /api/Service/{id})

**Response:** 204 No Content. The service is soft-deleted and will no longer appear in the list.

---

## Frontend Prompt (copy this to build the frontend)

You can paste the following (or adapt it) when asking to implement the Services UI:

---

**Context:** ClinicOps backend supports clinic services: each clinic has a list of services with a name and a price (e.g. "Kontrolle", "Kontrolle + Analiza", "Kontrolle + Analiza + ultraze"). Any authenticated user with access to the clinic can add, edit, and delete them. See `docs/SERVICES.md` for the API.

**Implement the frontend for Services:**

1. **Services page/section**
   - Add a **Services** page or section (e.g. under clinic settings or a dedicated "Services" menu) where the user can:
     - **List** – Call `GET /api/Service` (and for SuperAdmin, `GET /api/Service?clinicId={id}` when managing another clinic) and show a table or list of service name and price.
     - **Add** – A form or modal with fields: Name, Price. Submit via `POST /api/Service` with body `{ "name": "...", "price": number }`. On success, refresh the list or append the new item.
     - **Edit** – For each row, an Edit action that opens a form (or inline edit) with current name and price. Submit via `PUT /api/Service/{id}` with body `{ "name": "...", "price": number }` (send both or only changed fields). On success, refresh or update the row.
     - **Delete** – A Delete action that calls `DELETE /api/Service/{id}`. On success, remove the row from the list or refresh the list.

2. **Clinic scope**
   - For clinic users, do not send `clinicId`; the backend uses the clinic from the token.
   - For SuperAdmin, when viewing/managing a specific clinic, pass `?clinicId={clinicId}` on GET/POST/PUT/DELETE as documented.

3. **Validation**
   - Name: required, max 300 characters.
   - Price: required, ≥ 0 (show validation errors if invalid).

4. **UX**
   - Empty state when there are no services (e.g. "No services yet. Add one below.").
   - Optional: confirm before delete ("Are you sure you want to remove this service?").
   - Optional: show loading states and error messages for list/create/update/delete.

---

## Backend Details (for reference)

- **Entity:** `Domain/Entities/Service.cs` – `ClinicId`, `Name`, `Price`, `CreatedAt`, `IsActive`.
- **DTOs:** `API/DTOs/Service/ServiceDto.cs` – `ServiceDto`, `CreateServiceRequest`, `UpdateServiceRequest`.
- **Controller:** `API/Controllers/ServiceController.cs` – List, GetById, Create, Update, Delete; clinic resolved from JWT or `clinicId` query for SuperAdmin.
- **DbContext:** `Data/ApplicationDbContext.cs` – `DbSet<Service>`, relationship to `Clinic` (Restrict on delete).
- **Migration:** `AddServicesTable` – run `dotnet ef database update` to apply.

After applying the migration, the backend is ready for the frontend to consume the Services API.
