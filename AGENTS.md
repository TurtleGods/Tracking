# Repository Guidelines

## Project Structure & Module Organization
- Current contents: root `.gitignore` tuned for Visual Studio artifacts and `DBSchema.md` describing analytics/tracking tables.
- Add executable SQL under `db/` (e.g., `db/001_init.sql`, `db/002_updates.sql`) and keep docs/specs under `docs/`.
- Place helper scripts in `scripts/` and keep any sample data in `seeds/`; avoid committing generated binaries.

## Build, Test, and Development Commands
- Apply the schema to a Postgres database: `psql -d $DB_NAME -f db/001_init.sql` once the SQL file exists.
- Format SQL snippets before committing: `psql -f db/001_init.sql -P format=unaligned` or use `sqlfluff lint db` if available.
- Quick sanity check of docs: `markdownlint DBSchema.md AGENTS.md` when markdownlint is installed.

## Coding Style & Naming Conventions
- Tables remain plural lower_snake_case (e.g., `tracking_events`); columns lower_snake_case with explicit types.
- Prefer `uuid` for identifiers, `timestamptz` for time fields, and `jsonb` for event payloads as in `tracking_events.properties`.
- Keep documentation concise in Markdown with fenced code blocks for SQL examples; wrap lines near 100 characters.

## Testing Guidelines
- Run targeted queries to validate constraints (unique emails in `analytics_dashboards`, foreign keys between sessions/events) before merges.
- Add lightweight fixtures under `seeds/` and verify counts with `SELECT COUNT(*)` after loading.
- Consider pgTAP or similar when automated database tests are added; mirror migration file names with test files.

## Commit & Pull Request Guidelines
- Commit messages: short imperative subject (?72 chars) plus optional body explaining schema rationale or data migration steps.
- Each PR should describe schema changes, expected impacts (e.g., new nullable columns), and any migration order.
- Include proof of validation (psql output, pgTAP summary) and note whether breaking changes require downtime.

## Security & Configuration Tips
- Do not commit credentials; `.env` files are ignored by `.gitignore`. Use environment variables for connection strings.
- When sharing sample data, anonymize user identifiers and strip IPs or tokens.
