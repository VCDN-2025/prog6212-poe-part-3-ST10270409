# CMCS — Contract Monthly Claim System (POE Part 1)

Non-functional **ASP.NET Core MVC (.NET 8)** prototype for PROG6212.

## How to run
1. Open `CMCS.Web.sln` in Visual Studio 2022.
2. Set **CMCS.Web** as Startup project.
3. Press **F5** and navigate:
   - `/` — Dashboard
   - `/Claims/New`
   - `/Claims/My`
   - `/Approvals`

## Screens (Part 1)
- New Claim: capture items (date, hours, activity, rate) with actions disabled.
- My Claims: month, total hours, amount, status badges + “Open”.
- Approvals: list pending claims with stage and disabled Approve/Reject.

## Architecture
- MVC with Bootstrap. View-models only (no DB).
- Entities for Part 2 (UML): Lecturer, Claim, ClaimItem, Approval, Attachment.

## Docs
- `docs/uml/` — UML PNG + .drawio
- `docs/screens/` — screenshots used in the report
- `docs/plan/` — WBS / schedule

## Scope (Part 1)
- UI prototype only; actions are intentionally disabled.

## License
Academic use only.
