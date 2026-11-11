# Assumptions

The brief does not cover every case. This file lists each assumption and the reason for it.

## Classes

- IDs are UUID strings. The brief allows any format.
- A malformed ID returns 404, the same as an unknown ID. A client gets one answer for any ID that does not exist.
- A class name is 1 to 200 characters after trim. Seats are 1 to 100000.
- Class names do not have to be unique. The brief does not require it, and test suites often reuse one example name.
- There are no class edit or delete endpoints, and no pagination. The contract has none, and the lists stay small.
- `GET /classes` sorts by name, then by ID. The brief gives no order, and the classes table has no creation time.
- The database starts empty, with no seed data.
- Errors use `{ "error": { "code", "message" } }`. Success bodies have no envelope.
- `GET /classes/{classId}` returns the same shape as one item of `GET /classes`.

## Holds

- The API has no authentication. The brief identifies the student by the `X-Student-Id` header, so the API trusts that header. In production the student ID comes from a verified login, and a gateway adds rate limits.
- The student ID must be exactly 17 ASCII digits. The server does not trim it. A missing, repeated or invalid header returns 400 `INVALID_STUDENT_ID`, before any 404.
- Confirm and `DELETE` require `X-Student-Id`. Rule 4 says only the student's own hold can be confirmed, and it cannot hold without the header. Another student's hold returns 403 `NOT_HOLD_OWNER`.
- `GET /holds/{holdId}` needs no header, never returns 403 and does not return `studentId`.
- `GET /me/holds` returns an array, newest first. The brief describes it as all applications.
- A successful hold returns 201.
- A student can hold seats in several classes at once. Rule 3 names the same class only.
- A cancelled or expired hold lets the student apply again.
- Confirm on a confirmed hold of the same student returns 200 with the same body. Confirm on an expired hold returns 409 `HOLD_EXPIRED`. Confirm on a cancelled hold returns 409 `HOLD_CANCELLED`.
- `DELETE` on a cancelled hold returns 204. `DELETE` on a confirmed hold returns 409 `HOLD_CONFIRMED`, and on an expired hold 409 `HOLD_EXPIRED`.
- Hold IDs follow the class ID rules. A malformed or unknown ID returns 404 `HOLD_NOT_FOUND`.
- There is no rate limiting. The row lock keeps seats safe, and a gateway owns rate limiting in production.
- Under heavy load, requests queue for up to 60 seconds for a connection and 30 seconds for the class lock. Beyond that, the API returns 503 `SERVICE_BUSY` with `Retry-After`.
- Each hold keeps the expiry it was given. A new `HOLD_DURATION_SECONDS` applies only to new holds.
- The hold time starts when the insert runs, by database time (`statement_timestamp()`). A wait for the class lock never shortens a hold. Every expiry check uses the same clock.
- Confirm takes the same class row lock as a create. A confirm that commits just after expiry can therefore never race a new hold for the same seat.

## Expiry

- Expiry is enforced at read time by database time. The background job only tidies the stored status, so correctness never depends on it.
- `EXPIRY_SWEEP_SECONDS` must be a whole number from 1 to 86400 (one day). A longer period has no use, and the startup check rejects it with the key name.
