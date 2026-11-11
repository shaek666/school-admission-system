// Black-box test of the running API. Node 22, no dependencies.
// Usage: BASE_URL=http://localhost:8080 node scripts/blackbox.mjs
// Prints one line for each step and exits 1 on the first failure.

const BASE_URL = (process.env.BASE_URL ?? "http://localhost:8080").replace(/\/$/, "");
const STARTUP_TIMEOUT_MS = 60_000;
const RETRY_DELAY_MS = 1_000;
const HEADER = "X-Student-Id";

let step = 0;

function fail(message) {
  console.error(`FAIL ${message}`);
  process.exit(1);
}

function pass(message) {
  step += 1;
  console.log(`ok ${step} ${message}`);
}

function studentId() {
  let digits = "";
  for (let i = 0; i < 17; i += 1) {
    digits += Math.floor(Math.random() * 10);
  }
  return digits;
}

async function call(method, path, { student, body } = {}) {
  const headers = {};
  if (student !== undefined) headers[HEADER] = student;
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const response = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  return { status: response.status, json: text ? JSON.parse(text) : null };
}

// options.code: the expected error code in { error: { code } }.
async function expect(label, method, path, expectedStatus, options = {}) {
  const result = await call(method, path, options);
  if (result.status !== expectedStatus) {
    fail(`${label}: expected ${expectedStatus}, got ${result.status} ${JSON.stringify(result.json)}`);
  }
  if (options.code !== undefined && result.json?.error?.code !== options.code) {
    fail(`${label}: expected code ${options.code}, got ${JSON.stringify(result.json)}`);
  }
  pass(`${label} -> ${result.status}${options.code ? ` ${options.code}` : ""}`);
  return result.json;
}

async function waitForApi() {
  const deadline = Date.now() + STARTUP_TIMEOUT_MS;
  while (Date.now() < deadline) {
    try {
      const result = await call("GET", "/classes");
      if (result.status === 200) {
        pass("GET /classes answers");
        return;
      }
    } catch {
      // The API is not up yet.
    }
    await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
  }
  fail(`GET /classes did not answer 200 within ${STARTUP_TIMEOUT_MS / 1000} s at ${BASE_URL}`);
}

async function main() {
  await waitForApi();

  const created = await expect("create class with 2 seats", "POST", "/classes", 201, {
    body: { name: `Blackbox ${Date.now()}`, totalSeats: 2 },
  });
  const classId = created.id;

  const [first, second, third] = [studentId(), studentId(), studentId()];
  const holdA = await expect("student 1 holds", "POST", `/classes/${classId}/holds`, 201, { student: first });
  const holdB = await expect("student 2 holds", "POST", `/classes/${classId}/holds`, 201, { student: second });
  await expect("student 3 holds a full class", "POST", `/classes/${classId}/holds`, 409, {
    student: third,
    code: "NO_SEATS_AVAILABLE",
  });

  for (const attempt of ["first", "second"]) {
    const confirmed = await expect(`${attempt} confirm`, "POST", `/holds/${holdA.holdId}/confirm`, 200, { student: first });
    if (confirmed.status !== "CONFIRMED") fail(`${attempt} confirm status: ${JSON.stringify(confirmed)}`);
  }

  await expect("cancel the other hold", "DELETE", `/holds/${holdB.holdId}`, 204, { student: second });

  const seats = await expect("GET /classes/{id}", "GET", `/classes/${classId}`, 200);
  const counts = { confirmedSeats: 1, heldSeats: 0, availableSeats: 1 };
  for (const [field, expected] of Object.entries(counts)) {
    if (seats[field] !== expected) fail(`${field}: expected ${expected}, got ${JSON.stringify(seats)}`);
  }

  for (const [hold, expected] of [[holdA, "CONFIRMED"], [holdB, "CANCELLED"]]) {
    const read = await expect(`GET /holds/{id} reads ${expected}`, "GET", `/holds/${hold.holdId}`, 200);
    if (read.status !== expected) fail(`hold ${hold.holdId}: expected ${expected}, got ${JSON.stringify(read)}`);
  }

  const mine = await expect("GET /me/holds", "GET", "/me/holds", 200, { student: first });
  if (!Array.isArray(mine)) fail(`/me/holds is not an array: ${JSON.stringify(mine)}`);

  await expect("bad student header", "GET", "/me/holds", 400, { student: "123", code: "INVALID_STUDENT_ID" });

  console.log(`PASS ${step} steps`);
}

main().catch((error) => fail(error.stack ?? String(error)));
