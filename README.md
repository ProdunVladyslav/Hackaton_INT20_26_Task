# FlowForge AI (BetterMe Hackathon INT20-26)

Survey/quiz engine backend that lets admins design dynamic question flows with branching logic, present targeted offers, and track analytics.
ADMIN CREDS
```
EMAIL = admin@example.com
PASSWORD = Admin123!
```

DEPLOYED - [here](https://hackaton-task-b8310a8937f5.herokuapp.com/swagger/index.html)

FRONTEND DEPLOY - [here](https://better-me-admin2-lzwt.vercel.app/login)

FRONTEND REPO - [here](https://github.com/IlyaKrasulia/betterMeAdmin2)

AI DEMO - [here](https://www.youtube.com/watch?v=cMJUmX57-AE)

STATS DEMO - [here](https://www.youtube.com/watch?v=p3VSdWqglmk)

## Local start instructions (via Docker)
1. In root directory (where docker-compose.yml is located) run this:
```
docker compose up --build
```
2. Visit [local Swagger](http://localhost:8080/swagger/index.html)

## What changed
- **AI flow generation** — admins now able to describe a quiz in plain text and Claude generates the full flow automatically (background job + polling).
- **Statistics & user path tracking** — covers the analytics endpoints and the new UserNodePath field for observing user journeys.
- **Parameters manual handling** - comparing to older version, where user could choose attribute only from dropdown list, now user can create their own. He just needs to write down its name(AttributeName) and determine its ValueType(Text/Numeric). Then it can be used as a conditions in edges with proper operators (which are available for specific ValueType). 
- **Added OR comparison type between edge conditions** - now user can choose how to operate between different edge conditions using simple dropdown.
- **DAG constructor & survey bug fixes** — covers the edge condition, duplicate-edge, node validation, and routing fixes.  
## Admin Flow
https://github.com/user-attachments/assets/1948e358-88c4-425b-afd5-963338445980

## User Flow
https://github.com/user-attachments/assets/491d5340-3f30-4b3d-b274-ca7be1b48cc1

## Full Demo
https://github.com/user-attachments/assets/b324ab39-d578-4b10-a65f-d8ee50ce15da

## Tech Stack

- .NET 8 / ASP.NET Core Web API
- PostgreSQL + Entity Framework Core 8
- ASP.NET Identity (cookie auth) + JWT
- Anthropic Claude API (AI-powered flow generation)
- Docker (Heroku deployment)

## Project Structure

```
Domain/           Core entities (Flow, Node, Edge, Option, Offer, UserSession)
Application/      EF Core DbContext, migrations, repositories
Infrastructure/   Use cases, request/response DTOs, mappers
Hackaton_INT20'26_Task/   API host, controllers
```

---

## AI-Powered Flow Generation

Admins can generate entire survey flows from a single natural-language prompt using the Anthropic Claude API (`claude-sonnet-4-20250514`).

### How It Works

1. **Admin submits a prompt** describing the desired quiz (topic, target audience, offers) via `POST /api/admin/flows/generate`.
2. The server returns a **job ID** immediately (HTTP 202) and spawns a background task.
3. Claude receives the prompt along with a **744-line system prompt** that encodes the full domain schema, layout rules, and structural constraints.
4. Claude returns a complete flow plan as structured JSON — flow metadata, nodes, options, edges with conditions, and offers.
5. The backend **orchestrates creation** in sequence: create flow → create nodes (mapping temp IDs to real IDs) → create options → create edges (with condition validation) → set entry node.
6. Admin polls `GET /api/admin/flows/generate/status/{jobId}` until status is `Done` or `Failed`.

### AI Constraints & Quality Guarantees

- **Max 15 nodes** per generated flow (questions + info pages + offers).
- **Branching-first topology** — the first question must immediately split into different paths; linear chains are rejected.
- Every path must terminate at an **Offer node** (no dead ends).
- `attributeKey` on each question is a strict contract — edge conditions reference these keys, and operators are validated against the node's `valueKind` (Text or Numeric).
- Layout positions follow a left-to-right tree visualization with consistent spacing.
- Duplicate edges (same source → target) are automatically deduplicated.

### Service Architecture

| Component | Responsibility |
|---|---|
| `ClaudeService` | HTTP client wrapping the Anthropic Messages API (single-turn, multi-turn, streaming) |
| `StartGenerateFlowUseCase` | Creates a background job, returns job ID for polling |
| `GenerateFlowUseCase` | Calls Claude, parses response JSON, orchestrates entity creation |
| `GetGenerateFlowStatusUseCase` | Returns job status (Pending / Running / Done / Failed) |
| `FlowGenerationJobStore` | In-memory singleton tracking active generation jobs |

### Configuration

Set the following in your `.env` or `appsettings.json` under the `Claude` section:

| Variable | Default | Description |
|---|---|---|
| `ApiKey` | — | Anthropic API key (required) |
| `Model` | `claude-sonnet-4-20250514` | Claude model ID |
| `MaxTokens` | `1024` | Max response tokens |

---

## Domain Model

The system is built around a **DAG (Directed Acyclic Graph)** architecture. Admins design "flows" — graphs of nodes connected by edges with conditional routing. End-users navigate these flows in quiz sessions, answering questions and receiving personalized offers.

### Entity Relationship Diagram

```
ApplicationUser
  └─(1:1)─► UserProfile                    [CASCADE]

Flow  ◄── aggregate root ──────────────────────────────
  ├─(1:N)─► Node                            [CASCADE]
  │   ├─(1:N)─► Option                      [CASCADE]
  │   └─(1:N)─► NodeOffer ─(M:1)─► Offer    [RESTRICT]
  ├─(1:N)─► Edge                            [CASCADE]
  │   ├─(M:1 SourceNodeId)─► Node           [RESTRICT]
  │   └─(M:1 TargetNodeId)─► Node           [RESTRICT]
  └─(1:N)─► UserSession                     [RESTRICT]
      ├─(M:1 CurrentNodeId)─► Node          [RESTRICT]
      ├─(1:N)─► UserAnswer                  [CASCADE]
      │   └─(M:1 NodeId)─► Node             [RESTRICT]
      └─(1:N)─► SessionOffer                [CASCADE]
            └─(M:1 OfferId)─► Offer         [RESTRICT]
```

---

## Entities

### Flow (Aggregate Root)

The top-level container for a quiz. A flow owns all its nodes and edges.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `Name` | `string` | Flow name (required, max 200) |
| `Description` | `string` | Longer description (max 2000) |
| `IsPublished` | `bool` | Whether end-users can access this flow |
| `EntryNodeId` | `Guid?` | Soft pointer to the starting node (no FK constraint to avoid circular ref) |
| `CreatedAt` | `DateTime` | UTC creation timestamp |
| `UpdatedAt` | `DateTime` | UTC, updated on any mutation |
| `Nodes` | `IReadOnlyCollection<Node>` | All nodes in the flow |
| `Edges` | `IReadOnlyCollection<Edge>` | All edges in the flow |

**Rules:**
- A flow must have an `EntryNodeId` set before it can be published.
- `EntryNodeId` is validated at the application level (not a DB FK) to avoid circular dependency with Node.

---

### Node

A single step in the flow. Every node has a **type** that determines its behavior.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `FlowId` | `Guid` | FK to parent Flow |
| `Type` | `NodeType` | `Question`, `InfoPage`, or `Offer` |
| `AttributeKey` | `string` | Unique key for this node's answer (required for Question nodes, max 200) |
| `Title` | `string` | Display title (required, max 500) |
| `Description` | `string` | Longer text content (max 2000) |
| `MediaUrl` | `string` | Optional image/video URL (max 1000) |
| `PositionX` | `float` | Canvas X coordinate (for admin visual editor) |
| `PositionY` | `float` | Canvas Y coordinate |
| `CreatedAt` | `DateTime` | UTC creation timestamp |
| `AnswerType` | `AnswerType?` | How the user answers — only valid on Question nodes |
| `ValueKind` | `ValueKind?` | `Text` or `Numeric` — determines which operators are legal in edge conditions referencing this node's `AttributeKey` |
| `SliderMin` | `decimal?` | Minimum slider value (Slider answer type only) |
| `SliderMax` | `decimal?` | Maximum slider value (Slider answer type only) |
| `Options` | `IReadOnlyCollection<Option>` | Answer choices (Question nodes only, not allowed for Slider) |

#### Node Types

| Type | Purpose | Has Options? | Has AnswerType? | Records Answer? |
|---|---|---|---|---|
| **Question** | Collects user input | Yes (except Slider) | Required | Yes |
| **InfoPage** | Displays static content (text, media) | No | No | No |
| **Offer** | Presents product/service offers | No | No | No (tracks via SessionOffer) |

#### Answer Types (Question nodes only)

| AnswerType | Behavior | Options? |
|---|---|---|
| `SingleChoice` | User picks exactly one option (radio/dropdown) | Yes |
| `MultipleChoice` | User picks one or more options (checkboxes) | Yes |
| `Slider` | User drags a numeric slider between `SliderMin` and `SliderMax` | No — options are cleared/forbidden |

**Validation rules:**
- Question nodes **must** have a non-empty `AttributeKey` and a `ValueKind`.
- Slider questions **require** `ValueKind = Numeric`, both `SliderMin` and `SliderMax` (min < max), and **cannot** have options.
- SingleChoice/MultipleChoice **require** `ValueKind = Text`, **cannot** have slider bounds, and must have at least 2 options.
- Only Question nodes can have options added.
- Edge conditions referencing a node's `AttributeKey` must use operators compatible with its `ValueKind` (e.g., `gt`/`lt`/`between` only for Numeric).

---

### Edge

A directed connection between two nodes, with optional conditions that control branching.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `FlowId` | `Guid` | FK to parent Flow |
| `SourceNodeId` | `Guid` | FK to the origin Node |
| `TargetNodeId` | `Guid` | FK to the destination Node |
| `Priority` | `int` | Evaluation order — higher priority edges are checked first (default 0) |
| `ConditionsJson` | `string` | Free-form JSON conditions (empty = unconditional) |
| `CreatedAt` | `DateTime` | UTC creation timestamp |

**Rules:**
- Self-loops are forbidden (`SourceNodeId` ≠ `TargetNodeId`).
- Duplicate edges (same source + target) are prevented.
- Edges use `Restrict` delete on both source and target nodes — you must remove edges before deleting the nodes they reference.

#### Condition Formats

Edges support two JSON formats for conditions:

**Array format (legacy):**
```json
[
  { "AttributeKey": "goal", "Operator": "eq", "Value": "lose_weight" },
  { "AttributeKey": "age", "Operator": "between", "Value": "18", "ValueTo": "40" }
]
```

**Object format (structured):**
```json
{
  "operator": "AND",
  "rules": [
    { "attribute": "goal", "op": "eq", "value": "lose_weight" }
  ]
}
```

**Supported operators:** `eq`, `neq`, `in`, `between`, `gt`, `gte`, `lt`, `lte`

All conditions within a set are combined with AND logic — every condition must pass for the edge to fire.

---

### Option

An answer choice belonging to a Question node.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `NodeId` | `Guid` | FK to parent Node |
| `Label` | `string` | Display text shown to user (required, max 500) |
| `Value` | `string` | Machine value sent on submission (required, max 500) |
| `DisplayOrder` | `int` | Render sequence (default 0, must be ≥ 0) |
| `MediaUrl` | `string?` | Optional image URL for visual options (max 1000) |

---

### Offer

A product or service that can be presented to users. Offers are **standalone entities** — reusable across multiple nodes and flows.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `Slug` | `string` | Unique URL-safe identifier (required, max 200, unique index) |
| `Name` | `string` | Display name (required, max 300) |
| `Description` | `string` | Long description (max 4000) |
| `Duration` | `string` | e.g. "6 weeks", "3 months" (max 200) |
| `DigitalContent` | `string` | Description of digital deliverables (max 2000) |
| `PhysicalWellnessKitName` | `string` | Kit name (max 300) |
| `PhysicalWellnessKitItems` | `string` | List of kit items (max 4000) |
| `Price` | `decimal?` | Purchase price — null means free (numeric 18,2) |
| `ImageUrl` | `string` | Marketing image URL (max 1000) |
| `CtaText` | `string` | Call-to-action button text (max 300) |
| `CtaUrl` | `string` | Where the CTA button links to (max 1000) |

**Deletion rules:**
- Cannot delete an offer while it is linked to any node (`NodeOffer` FK is Restrict).
- Must unlink from all nodes first, then delete.

---

### NodeOffer (Join: Node ↔ Offer)

Links an offer to a node. Multiple offers can be linked to a single node.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `NodeId` | `Guid` | FK to Node (cascade — removing node removes link) |
| `OfferId` | `Guid` | FK to Offer (restrict — offer must be unlinked before deletion) |
| `IsPrimary` | `bool` | Marks this as the "recommended" offer when multiple exist |

**Constraint:** Unique index on `(NodeId, OfferId)` — one offer can only be linked once per node.

---

### UserSession

Tracks a single user's journey through a flow.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Session ID |
| `FlowId` | `Guid` | FK to Flow (restrict — preserves analytics data) |
| `CurrentNodeId` | `Guid` | FK to Node the user is currently on (restrict) |
| `Status` | `SessionStatus` | `InProgress`, `Completed`, or `Abandoned` |
| `UtmSource` | `string` | UTM campaign source (max 200) |
| `UtmCampaign` | `string` | UTM campaign name (max 200) |
| `StartedAt` | `DateTime` | When session was created |
| `CompletedAt` | `DateTime?` | When session was completed (null if still in progress) |
| `UserNodePath` | `string?` | Ordered list of node IDs the user visited (serialized JSON) — enables path analytics and "go back" |

#### Session Status

| Status | Meaning |
|---|---|
| `InProgress` | User is actively navigating the flow |
| `Completed` | User reached a terminal node (no outgoing edge matched) |
| `Abandoned` | User left without completing |

---

### UserAnswer

Records one answer per question per session.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `SessionId` | `Guid` | FK to UserSession (cascade — deleted with session) |
| `NodeId` | `Guid` | FK to the Node that was answered (restrict — preserves history) |
| `AttributeKey` | `string` | Copy of the node's attribute key (required, max 200) |
| `Value` | `string` | The user's answer (required, max 4000) |
| `AnsweredAt` | `DateTime` | When the answer was submitted |

Only created for **Question** nodes. Slider values, text input, and multiple selections are all stored as a single string in `Value`.

---

### SessionOffer

Tracks which offers were presented to a user and whether they converted.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `SessionId` | `Guid` | FK to UserSession (cascade — deleted with session) |
| `OfferId` | `Guid` | FK to Offer (restrict — preserves conversion history) |
| `IsPrimary` | `bool` | Whether this was the primary/recommended offer |
| `Converted` | `bool` | Whether the user clicked the CTA |
| `PresentedAt` | `DateTime` | When the offer was shown |

---

## How the Quiz Engine Works

### 1. Starting a Session

```
POST /api/quiz/sessions  { flowId, utmSource?, utmCampaign? }
```

1. Validates the flow exists and `IsPublished = true`.
2. Checks the flow has an `EntryNodeId` set.
3. Creates a `UserSession` with `Status = InProgress`, positioned at the entry node.
4. Returns the entry node details (title, description, options, linked offers).

### 2. Submitting an Answer

```
POST /api/quiz/sessions/{id}/answers  { nodeId, value }
```

1. Loads the session and verifies `Status = InProgress`.
2. Verifies `nodeId` matches the session's `CurrentNodeId`.
3. **For Question nodes only:** creates a `UserAnswer` record.
4. Builds the full **answer context** — a dictionary of all answers in this session keyed by `AttributeKey`.
5. **Edge resolution** — finds the next node:
   - Queries all edges where `SourceNodeId` = current node.
   - Sorts by `Priority` descending (highest first).
   - For each edge, evaluates its conditions against the answer context.
   - First edge whose conditions pass (or has no conditions) wins.
6. **If an edge matched:** moves session to the target node, returns new node state.
7. **If no edge matched:** marks session as `Completed`, sets `CompletedAt`.

### 3. Edge Condition Evaluation

```
edges = all edges FROM current node, sorted by Priority DESC
for each edge:
    if edge has no conditions → MATCH (unconditional)
    if all conditions pass against answer context → MATCH
    otherwise → skip
if no edge matched → session is COMPLETE
```

Conditions reference previous answers by `AttributeKey`. If a condition references an `AttributeKey` that hasn't been answered yet, the condition fails. Numeric operators (`gt`, `lt`, `between`, etc.) parse values as decimals for comparison.

### 4. Going Back

```
POST /api/quiz/sessions/{id}/back
```

Removes the last `UserAnswer` from the session and moves back to the previous node.

### 5. Offer Conversion

```
POST /api/quiz/sessions/{id}/convert  { offerId }
```

Works on both `InProgress` and `Completed` sessions. Finds or creates a `SessionOffer` record and marks `Converted = true`.

### Session State Response Shape

```json
{
  "sessionId": "guid",
  "flowId": "guid",
  "status": "InProgress",
  "startedAt": "2026-01-01T00:00:00Z",
  "completedAt": null,
  "currentNode": {
    "id": "guid",
    "type": "Question",
    "attributeKey": "fitness_goal",
    "title": "What is your fitness goal?",
    "description": "Choose the option that best describes you.",
    "mediaUrl": "https://...",
    "options": [
      { "id": "guid", "label": "Lose weight", "value": "lose_weight", "displayOrder": 0, "mediaUrl": null },
      { "id": "guid", "label": "Build muscle", "value": "build_muscle", "displayOrder": 1, "mediaUrl": null }
    ],
    "offers": [
      {
        "id": "guid", "name": "6-Week Program", "slug": "6-week-program",
        "description": "...", "duration": "6 weeks", "price": 29.99,
        "imageUrl": "https://...", "ctaText": "Get Started", "ctaUrl": "https://...",
        "isPrimary": true
      }
    ]
  }
}
```

---

## Cascade & Restrict Delete Behaviors

| When deleting... | What happens |
|---|---|
| **Flow** | Nodes and Edges cascade-delete. UserSessions must be removed explicitly first (Restrict). |
| **Node** | Options and NodeOffer links cascade-delete. Edges referencing this node must be removed first (Restrict). Sessions on this node block deletion (Restrict). |
| **Offer** | Cannot delete while linked to any node or referenced by any SessionOffer (Restrict on both). |
| **UserSession** | UserAnswers and SessionOffers cascade-delete. |

---

## API

### Public — Quiz (`/api/quiz`) — No auth required

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/sessions` | Start a new quiz session |
| `GET` | `/sessions/{id}` | Get current session state |
| `POST` | `/sessions/{id}/answers` | Submit an answer |
| `POST` | `/sessions/{id}/back` | Go back one node |
| `POST` | `/sessions/{id}/convert` | Mark an offer as converted |

### Admin (authenticated) — `/api/admin`

| Resource | Endpoint | Methods | Description |
|---|---|---|---|
| **Flows** | `/flows` | GET, POST, PUT, DELETE | CRUD flows |
| | `/flows/{id}/entry-node` | PUT | Set flow entry point |
| | `/flows/{id}/publish` | POST | Publish flow |
| | `/flows/{id}/unpublish` | POST | Unpublish flow |
| **Nodes** | `/flows/{flowId}/nodes` | POST, PUT, DELETE | CRUD nodes |
| | `/flows/{flowId}/nodes/{id}/position` | PUT | Move node on canvas |
| **Edges** | `/flows/{flowId}/edges` | POST, PUT, DELETE | CRUD edges |
| **Options** | `/nodes/{nodeId}/options` | POST, PUT, DELETE | CRUD options |
| | `/nodes/{nodeId}/options/reorder` | PUT | Reorder options |
| **Offers** | `/offers` | GET, POST, PUT, DELETE | CRUD offers |
| **Node-Offers** | `/nodes/{nodeId}/offers` | GET, POST, PUT, DELETE | Link/unlink offers to nodes |
| **AI Generation** | `/flows/generate` | POST | Generate a full flow from a natural-language prompt (returns job ID) |
| | `/flows/generate/status/{jobId}` | GET | Poll generation job status |
| **Analytics** | `/analytics/sessions` | GET | Session stats |
| | `/analytics/offers` | GET | Offer performance |
| | `/analytics/drop-offs` | GET | Drop-off analysis |

### Auth — `/api/auth`

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/login` | Admin login |
| `GET` | `/me` | Current user info |

---

## Database Indexes

| Table | Index | Purpose |
|---|---|---|
| `Flows` | `IsPublished` | Quick lookup of published flows |
| `Edges` | `(FlowId, SourceNodeId)` | Find outgoing edges from a node |
| `Options` | `(NodeId, DisplayOrder)` | Render options in order |
| `Offers` | `Slug` (unique) | URL-based lookup |
| `NodeOffers` | `(NodeId, OfferId)` (unique) | Prevent duplicate links |
| `UserSessions` | `FlowId`, `Status` | Analytics queries |
| `UserAnswers` | `SessionId` | Load all answers for a session |
| `SessionOffers` | `OfferId`, `SessionId` | Offer performance analytics |

---

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
- `Claude__ApiKey` — Anthropic API key for AI flow generation
- `CORS_ORIGINS` — Comma-separated allowed origins (defaults to `http://localhost:3000,http://localhost:5173`)
