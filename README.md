# JobShodho — AI Job Application Management Portal

An ASP.NET Core MVC application for managing job applications end-to-end: import jobs from Excel,
generate a personalized application email and resume match with AI, generate interview prep
questions, review everything by hand, and send emails in controlled batches of 10 with full
tracking and retry.

Nothing is ever sent automatically. Every job goes through a manual review step before you click Send.

## Requirements

- .NET 10 SDK
- SQL Server LocalDB (ships with Visual Studio, or install "SQL Server Express LocalDB" separately)
- An OpenAI-compatible Chat Completions API key (OpenAI, Azure OpenAI, or a compatible provider such
  as Gemini's OpenAI-compatibility endpoint) if you want AI generation to work
- An SMTP account (e.g. Gmail with an App Password) if you want to actually send email

## Project layout

```
JobShodho/                 ASP.NET Core MVC app
  Controllers/              Dashboard, Jobs, Import, Email, Resume, Interview, Settings
  Data/                      ApplicationDbContext, EF configurations, DataSeeder, migrations
  Models/                    Entities + Enums (EmailStatus, GenerationType, GenerationStatus, EmailLogStatus)
  ViewModels/                Page-specific view models (dashboard stats, paging, review, etc.)
  Services/                  Business logic (Excel import, AI, email, resumes, interview, PDF)
    Ai/                       Prompt builder + AI response DTOs
    Interfaces/               Service contracts (IAiService, IEmailService, ...)
  BackgroundJobs/            Hangfire entry point for background batch sending
  Options/                   Strongly-typed config (OpenAiOptions, EmailOptions, ...)
  Views/                     Razor views, one folder per controller
  wwwroot/uploads/           resumes/, generated/, interview/ (git-ignored except for .gitkeep)
JobShodho.Tests/            xUnit tests (Excel validation, email validation, status transitions,
                             batch-cap and duplicate-send prevention, resume selection JSON parsing)
```

## Database

Connection string (already set in `appsettings.json`):

```
Server=(localdb)\MSSQLLocalDB;Database=JobApplicationManagerDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Create/update the schema with EF Core migrations:

```
cd JobShodho
dotnet ef database update
```

This creates `JobApplicationManagerDb` on your local LocalDB instance with all application tables
(`JobApplications`, `Resumes`, `EmailSendLogs`, `InterviewContents`, `AiGenerationLogs`,
`CandidateProfiles`, `AppSettings`) plus Hangfire's own job-storage tables in the same database.

In the **Development** environment, the app seeds a small amount of demo data on first run if the
tables are empty: 1 candidate profile, 3 sample jobs (fake `*.example` recipient addresses — never
real ones), and 2 placeholder resumes. Seeding is idempotent and safe to leave on.

## Configuration

`appsettings.json` holds structure and non-secret defaults only. **Never put real API keys or SMTP
passwords in `appsettings.json`** — it's committed to source control. Use .NET User Secrets (already
initialized for this project) or environment variables instead:

```
cd JobShodho
dotnet user-secrets set "OpenAI:ApiKey" "<your-api-key>"
dotnet user-secrets set "Email:Username" "<your-smtp-username>"
dotnet user-secrets set "Email:Password" "<your-smtp-password-or-app-password>"
```

User Secrets are only loaded when `ASPNETCORE_ENVIRONMENT=Development` (the default when running via
`dotnet run` with the provided launch profile). Key settings:

```jsonc
"OpenAI": {
  "ApiKey": "",                 // set via user-secrets, never here
  "Model": "gpt-4o-mini",       // any Chat Completions model; swap BaseUrl for non-OpenAI providers
  "BaseUrl": "https://api.openai.com/v1/"
},
"Email": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "",               // set via user-secrets
  "Password": "",                // set via user-secrets — Gmail requires an App Password if 2FA is on
  "FromEmail": "",
  "FromName": "",
  "UseStartTls": true
}
```

If `OpenAI:ApiKey` or `Email:Host`/`FromEmail` are left empty, the app still runs fine — AI generation
and email sending simply return a clear "not configured" error instead of crashing.

The candidate's own name, experience, skills, and links are **not** configured here — they live in
**Settings** inside the app (`CandidateProfile` table) and are reused by every AI prompt.

## Run

```
dotnet restore
cd JobShodho
dotnet ef database update
dotnet run
```

Then open the URL shown in the console (e.g. `https://localhost:5001`). The Hangfire dashboard for
background job status is available at `/hangfire`.

## Tests

```
dotnet test
```

Covers: Excel import validation (required columns, empty-row skipping, invalid emails, duplicate
detection), email address validation, the `EmailStatus` enum values the raw batch-claim SQL depends
on, AI response JSON parsing, and — against a real disposable LocalDB database — the batch-sending
guarantees: **"Send Next 10" never claims more than 10 rows, an already-sent or already-failed job is
never claimed again, two overlapping claims never grab the same row, and one failed send never aborts
the rest of the batch.**

## Excel import format

Upload a `.xlsx` file with these column headers in row 1 (any order):

| Column          | Required | Notes                                   |
|-----------------|----------|------------------------------------------|
| CompanyName     | Yes      |                                            |
| JobTitle        | Yes      |                                            |
| RecipientEmail  | Yes      | Must be a valid email address             |
| JobDescription  | Yes      |                                            |
| JobUrl          | No       |                                            |
| Location        | No       |                                            |
| Source          | No       | e.g. LinkedIn, Naukri, referral           |

Completely empty rows are skipped silently. Rows missing a required field, with an invalid email, or
duplicating an existing `CompanyName + JobTitle + RecipientEmail` combination (either already in the
database or elsewhere in the same file) are skipped and reported, not imported. The import screen
shows exactly how many rows were imported, skipped as duplicates, or rejected as invalid.

## Workflow

1. **Import** — upload an Excel file on the Import Jobs page. Nothing is sent at this stage.
2. **Upload resumes** — add PDF/DOCX resumes on the Resumes page; text is extracted automatically for AI matching.
3. **Generate** — open a job and click Generate (or "Generate All") to have AI produce the application
   email, pick the best-matching resume, and draft interview Q&A, all logged to `AiGenerationLogs`.
4. **Review** — the Review page shows everything (subject, body, selected resume, interview prep) for
   you to edit and mark reviewed. A job cannot be sent until it's in the Ready for Review state.
5. **Send** — click **Send Next 10** on the Email Queue page to atomically claim and send up to 10
   reviewed, not-yet-sent jobs in the background. Individual jobs can also be sent from the Review page.
6. **Track results** — the Dashboard, Email History, and Failed Emails pages show what was sent, what
   failed and why, and attempt counts, all backed by an `EmailSendLog` row per attempt.
7. **Retry** — the Failed Emails page lets you retry a failed job; retry never touches already-sent jobs.

## Security notes

- Uploaded files are stored under a generated GUID filename; the original filename is only ever used
  as a display/download label, never as a path.
- Upload extension allowlist and max size are enforced server-side (`Uploads` config section).
- The batch-send "claim" step is an atomic SQL `UPDATE ... OUTPUT` (`ReadyForReview → Sending`), so two
  concurrent "Send Next 10" clicks can never claim or send the same job twice.
- All state-changing endpoints require anti-forgery tokens.
- No secrets are ever logged; `AiGenerationLog`/`EmailSendLog` store prompts, responses, and error
  messages only.
