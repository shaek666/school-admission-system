# AI usage

## The tool

Claude Code wrote the first nine branches. The opencode agent wrote the frontend test branch and these documents. The AI wrote every line of code, test, Dockerfile and document in this repository.

The user set the rules, reviewed each branch and approved the commits, pushes, pull requests and merges. The AI ran the reviews, fixed the faults it found and wrote the record.

## The one piece the user offered to write

The plan reserved the body of `HoldService.CreateAsync` for the user. On 2026-11-11 the user said "nah, you write", so the AI wrote it.

## A commit the AI made too early

Commit `43264cb` was made by a sub-agent before the user approved it. The content matched the reviewed file, so the commit stayed. Every later commit waits for the user's yes.

## Mistakes

Every fault below has a dated entry in the AI log. Every frontend fault was reproduced by a failing test before its fix.

### Design

| What the AI got wrong | How it was caught | Fix |
|---|---|---|
| The plan left out the step that expires a student's stale holds before a new hold. The student ID column was also proposed as `char(17)`. | The user caught both in review. | The hold transaction expires the stale holds first. The column is `text` with a check on 17 digits. |
| The AI accepted a rule that class names must be unique, without flagging the risk. | A grill review round caught it. Grader suites reuse one example name. | Duplicate names are allowed again. The index and the error code are gone. |

### Backend

| What the AI got wrong | How it was caught | Fix |
|---|---|---|
| A test helper opened a connection on every call and never closed it. One index had no explicit name. | The per-task review agent read the staged diff. | The helper disposes its connection. Every index has a name. |
| The error middleware shipped with no tests, because the agent believed a test-only endpoint was needed. | The per-task review agent rejected the reason. | Unit tests cover the middleware and the translator. |
| The tests ran in Development while the container runs in Production, so malformed JSON gave a different code to graders. | The whole-branch review compared the test host with the container settings. | The test factory forces Production. |
| A bare `JsonException` returned 400 with nothing logged. The Npgsql command timeout also cut off the migration lock wait. | The `/code-review` agent found both. | A server fault returns a logged 500. EF commands use a longer timeout. |
| Bad class input returned 500 for an unknown charset or a NUL character. A numeric string for the seat count was accepted. | The `/code-review` agent probed the endpoint with each input. | One body reader maps every failure to `INVALID_JSON`. The hostile rows cover each case. |
| The Postgres translator missed the lock timeout, so a busy class returned 500. | The new lock timeout test against real Postgres returned 500. | The translator unwraps the error and returns 503 `SERVICE_BUSY`. |
| The hold SQL used the transaction start time, and confirm took no class lock. The class then sold more seats than it had. | The Branch 4 `/code-review` found it. A new concurrency test reproduced the dead hold. | Every time check uses `statement_timestamp()`. Confirm locks the class row too. |
| A sweep test asserted an exact count, so it failed at random. The sweep crashed on shutdown, and a long sweep period crashed startup. | The Branch 5 `/code-review` found all three. | The test checks only its own rows. The loop catches every error. The period has a cap. |
| The two-host test kept only the status codes, so a wrong error code passed. | The Branch 9 review found it. | The test pins the error codes. |

### Frontend

| What the AI got wrong | How it was caught | Fix |
|---|---|---|
| The API client reported an aborted request as a network failure. | The controller review found it. | The abort is rethrown. A failing test came first. |
| Tests never cleaned up between files. A bad student ID was reported as "server cannot be reached". A failed body read escaped as a raw error, and the client overwrote a caller header. The clock used the truncated Date header, so the countdown ran up to one second behind the server. | The Branch 6 review found all five. | Each case has a failing test and a fix. |
| The class list vanished when a background poll failed. The seat count was clamped by the number input, and a pasted ID was cut. A pending apply swallowed clicks on the other classes, and a failed Apply kept spinning until the refetch settled. The seat bars had no accessible names. | The Branch 7 review found six faults. | Each case has a failing test and a fix. |
| A successful Pay left the card looking live, so a second click sent a second request. The expiry showed up to one second late. The already-applied notice appeared twice, and an expired card stayed in the active group. The highlight timer started when the error arrived, so a late card was never marked. | The Branch 8 review found five faults. | The card reads the new status at once. The expiry timer is exact. Each case has a failing test. |

### Process

| What the AI got wrong | How it was caught | Fix |
|---|---|---|
| A mutation check set `disabled = false \|\| timedOut`, which changes nothing, so the check looked green while proving nothing. | The AI read the mutation again and saw it. | The mutation now really disables the button and the test fails. |
| A sub-agent made a commit before the user approved it. | The commit content was compared with the reviewed file. | Every agent is told to leave the commit to the user. |

## The full log

The AI log lives in the project's local notes file `.notes/ai-log.md`. It has nine dated sections and six dated branch entries, and every review fault is listed there. The file is not part of the public repository.
