# Phase 2 - Admin users CRUD stabilization

This phase focuses on the `POST /api/admin/users` and `PUT /api/admin/users/{id}` flows that were returning HTTP 400 during manual testing.

## What changed

### Frontend

- Added `SAV-Pro/src/utils/adminUserValidation.js` using Zod.
- Validates required fields before sending the request.
- Validates password minimum length before create/update.
- Validates phone format before sending to the backend.
- Normalizes email to lowercase and trims all text fields.
- Rejects invalid role values before making the API request.
- Keeps the password omitted on update when the field is empty.

### Backend

- Added explicit validation messages to `CreateUserDto` and `UpdateUserDto`.
- Replaced fragile `[Phone]` validation with a tolerant phone regex that accepts formats like `+216 22 000 000`.
- Added enum validation for user roles.
- Normalizes email, names, phone, address, and password in `AdminUsersService`.
- Makes email duplicate checks case-insensitive.
- Adds an EF unique index declaration on `User.Email`.
- Prevents an admin from deleting their own currently authenticated account.
- Returns a clearer `ValidationProblemDetails` response for invalid request bodies.

## Manual test checklist

1. Login as `ADMIN`.
2. Open `/app/admin/users`.
3. Create a SAV user with a password shorter than 8 characters. The frontend must block it before sending.
4. Create a SAV user with valid data. The API should return `201 Created`.
5. Edit the created user without entering a password. The old password should be kept and the API should return `200 OK`.
6. Edit the user with a password shorter than 8 characters. The frontend must block it.
7. Try creating another user with the same email using different case. The backend should reject it clearly.
8. Try deleting the logged-in admin account. The backend should reject it.

## Notes

A manual EF migration file was added for the unique email index because this environment did not include the .NET SDK. In a local development environment, verify it with `dotnet ef migrations list` and `dotnet build`.
