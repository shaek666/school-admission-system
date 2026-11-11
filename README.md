# school-admission-system

A first-come, first-served school admission system with time-limited seat holds. It uses .NET 8, PostgreSQL and React (Vite).

## Run

```
docker compose up
```

The first build takes a few minutes. The frontend opens at <http://localhost:3000>. The API answers at <http://localhost:8080>.

Docker and Docker Compose are the only tools you need. Nothing is installed on the host.

The database starts empty.

## Demo

1. In the "School admin" panel, enter a class name and a seat count, then press "Create class".
2. Enter a 17-digit birth registration number at the top of the page, press "Save", then press "Apply" on the class.

The class list shows how many seats are free, held and confirmed. The counts refresh every few seconds.

The "Guardian: my applications" panel shows the application, the time left, and the buttons "Pay admission fee" and "Cancel". When the time runs out the card shows EXPIRED and the Pay button is disabled. The browser keeps the student number, so a refresh shows the same applications.

## Settings

Every setting has a default. Set an environment variable before `docker compose up` to change one.

| Variable | Default | Meaning |
|---|---|---|
| `HOLD_DURATION_SECONDS` | `600` | How long a seat is held before it expires. |
| `EXPIRY_SWEEP_SECONDS` | `30` | How often the background job tidies expired holds. |
| `DB_CONNECTION_STRING` | `Host=postgres;Port=5432;Database=admission;Username=postgres;Password=postgres;Maximum Pool Size=40;Timeout=60` | The Postgres connection string. |
| `API_BASE_URL` | `http://localhost:8080` | The backend URL that the frontend calls. |

For a quick look at expiry, use a ten second hold:

```
HOLD_DURATION_SECONDS=10 docker compose up
```

## Tests

Every test runs in Docker. The database tests use a real Postgres database.

```
docker compose --profile test run --build --rm backend-tests
docker compose --profile test run --build --rm frontend-tests
docker compose --profile test run --build --rm blackbox
```

- `backend-tests` is the main one. It runs the API and business test suites, including the concurrency tests.
- `frontend-tests` runs the Vitest suite for the React code.
- `blackbox` runs a script that calls the API without a browser.

The `--build` flag rebuilds the test image. Drop it to run the image that is already there.

## Data

The database lives in a Docker volume named `pgdata`.

- `docker compose down` stops the containers and keeps the data.
- `docker compose down -v` stops the containers and deletes the data.

## Bonus

Two backend processes can share one database without overselling seats. Start the second one with:

```
docker compose --profile multi up --build
```

The second API answers at <http://localhost:8081>. The frontend keeps using port 8080. The details are in ASSUMPTIONS.md.

## Documents

- ASSUMPTIONS.md lists each choice the brief left open, with the reason.
- AI_USAGE.md lists the AI tool, the parts it wrote, and the mistakes it made.
