# Course Decider (BetterMe Hackathon INT20-26)

Survey/quiz engine backend that lets admins design dynamic question flows with branching logic, present targeted offers, and track analytics.

## Tech Stack

- .NET 8 / ASP.NET Core Web API
- PostgreSQL + Entity Framework Core 8
- ASP.NET Identity (cookie auth) + JWT
- Docker (Heroku deployment)

## Project Structure

```
Domain/           Core entities (Flow, Node, Edge, Option, Offer, UserSession)
Application/      EF Core DbContext, migrations, repositories
Infrastructure/   Use cases, request/response DTOs, mappers
Hackaton_INT20'26_Task/   API host, controllers
```

## API

### Public — Quiz (`/api/quiz`)
- `POST /sessions` — start quiz session
- `GET /sessions/{id}` — get current state
- `POST /sessions/{id}/answers` — submit answer
- `POST /sessions/{id}/back` — go back
- `POST /sessions/{id}/convert` — mark offer converted

### Admin (authenticated)
- **Flows** `/api/admin/flows` — CRUD, publish/unpublish, set entry node
- **Nodes** `/api/admin/flows/{flowId}/nodes` — CRUD (Question, InfoPage, Offer types)
- **Edges** `/api/admin/flows/{flowId}/edges` — CRUD
- **Options** `/api/admin/flows/{flowId}/nodes/{nodeId}/options` — CRUD, reorder
- **Offers** `/api/admin/offers` — CRUD
- **Analytics** `/api/admin/analytics` — sessions, offers, drop-offs

## Run

```bash
# Local
dotnet run --project "Hackaton_INT20'26_Task"

# Docker
docker build --platform linux/amd64 --provenance=false -t registry.heroku.com/course-decider-betterme/web -f "Hackaton_INT20'26_Task/Dockerfile" .
docker push registry.heroku.com/course-decider-betterme/web
heroku container:release web -a course-decider-betterme
```

## Environment Variables

- `DATABASE_URL` — PostgreSQL connection string (auto-set by Heroku)
