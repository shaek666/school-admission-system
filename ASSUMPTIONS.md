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
