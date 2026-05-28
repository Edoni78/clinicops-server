# Backend Refactor Instructions

## Goal

Refactor the backend carefully into a cleaner monolithic architecture where controllers remain thin and all business logic is moved into services.

The system must remain a monolith.

Do not rewrite the project from scratch.

---

## Important Requirements

* Do not break any existing functionality.
* Do not change existing API routes or endpoint behavior.
* Do not remove existing authentication, authorization, tenant isolation, audit logs, uploads, or validations.
* Preserve all current request/response structures.
* Preserve frontend compatibility completely.
* Preserve database structure unless absolutely necessary.
* Refactor incrementally and safely.

---

## Architecture Direction

Controllers should only:

* Receive HTTP requests
* Validate request input when necessary
* Call services
* Return responses

Controllers must stay thin and readable.

All business logic should be moved into services, including:

* Database logic
* Validation logic
* Tenant/clinic checks
* Workflow logic
* File handling
* Audit logging
* Permissions logic
* Medical/business operations

---

## Scalability & Maintainability

Refactor the project so it becomes:

* Easier to maintain
* Easier to extend
* Easier to test
* More scalable for future modules
* Better organized for long-term growth

The architecture should support future expansion such as:

* EMR/EHR
* Billing
* Reporting
* Notifications
* Analytics
* Multi-branch support
* GDPR/security improvements

---

## Safety First

Before changing anything:

* Analyze the current structure carefully
* Understand existing flows fully
* Preserve all working behavior
* Avoid unnecessary changes
* Prefer small safe refactors over large rewrites

If something is already working correctly, preserve the behavior while improving the structure internally.

The final result should be a clean, scalable, service-based monolith with thin controllers and no broken functionality.
