# ADR 0001: Seat safety with a row lock

## Status

Accepted on 2026-11-11.

## Context

A class has a fixed number of seats. A guardian holds one of them while the fee is pending. A class must never sell more seats than it has, even when many guardians apply at once.

The count lives in two places. The `holds` rows hold the applications, and the `total_seats` column holds the number of seats. Any counting scheme has to read both under one rule, or two requests can each see one free seat and both take it.

The same rule has to hold when a second backend process runs against the same database.

## Decision

Every apply request runs in one transaction at `IsolationLevel.ReadCommitted`. The first statement takes a lock on the class row:

```sql
SELECT total_seats FROM classes WHERE id = @id FOR UPDATE;
```

The lock lives in Postgres, so requests from every process wait in one queue. The transaction then marks the student's stale holds for that class as expired. It checks for an active application, counts the held and confirmed seats, and inserts the hold.

Read Committed is the right isolation level here. The count after the lock takes a fresh snapshot, so it sees the holds that the earlier lock holder committed. Under Repeatable Read a waiting transaction fails with a serialization error instead.

The lock wait is bounded. Each transaction sets `SET LOCAL lock_timeout = '30s'`. A request that waits longer returns 503 `SERVICE_BUSY` with a `Retry-After` header.

Confirm takes the same class row lock before it updates the hold. A confirm that commits just after the expiry can therefore never race a new hold for the same seat.

Every time comparison uses the database clock, so the processes never disagree.

## Rejected options

### A counter column on the class row

A `seats_taken` column with `UPDATE classes SET seats_taken = seats_taken + 1` is the shortest answer. It was rejected because the counter and the hold rows are two pieces of state that can disagree. A rollback after the counter change, or a crash between the two writes, leaves the counter wrong. The count then needs a repair job, and the repair job needs its own rules. The row lock keeps one source of truth, the hold rows.

### SERIALIZABLE with a retry

`IsolationLevel.Serializable` also prevents overselling. It was rejected because a waiting transaction fails with a serialization error instead of waiting. Under a burst of applications the requests fail one after another. Each failure then needs a retry, a backoff, and a second failure path for the client. The row lock waits instead, and a request that waits too long gets one clear answer, 503 `SERVICE_BUSY`.

## Consequences

- One row lock per class. Requests for different classes never wait for each other.
- The lock is safe across processes, because it lives in the database and not in one application instance.
- The lock wait is bounded, so a burst cannot grow without limit.
- A seat is freed by the expiry rule, not by the lock. The lock only orders the requests.
- The transaction is longer than a single statement, so the connection pool needs enough connections for the waiting requests.
