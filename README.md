# Routly — Backend

**Routly is a B2B2B lead-qualification and conversion platform.** This is its backend: a .NET 8 API that lets a business design branching, DAG-based qualification flows, score and tier the leads (themselves businesses) who take them, capture their contact/company details, route them to a personalised offer or a disqualification page, and measure which acquisition channels actually bring leads that close.

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
Domain/           Core entities (Flow, Node, Edge, Option, Offer, UserSession, Lead, LeadChannel)
Application/      EF Core DbContext, migrations, repositories
Infrastructure/   Use cases, request/response DTOs, mappers, AI prompt
Hackaton_INT20'26_Task/   API host, controllers
```

---

## AI-Powered Flow Generation

Admins can generate an entire qualification flow from a single natural-language prompt using the Anthropic Claude API.

### How It Works

1. **Admin submits a prompt** describing the business, target lead, and offers via `POST /api/admin/flows/generate`.
2. The server returns a **job ID** immediately (HTTP 202) and spawns a background task.
3. Claude receives the prompt along with a system prompt (`Infrastructure/Prompts/survey_flow_prompt.txt`, ~960 lines) that encodes the full BANT-based qualification framework, the domain schema for every node type, layout rules, and structural constraints.
4. Claude returns a complete flow plan as structured JSON — flow metadata, nodes (including lead-capture fields and redirect reasons), options with score deltas, edges with conditions, and inline offers.
5. The backend **orchestrates creation** in sequence: create flow → create nodes (mapping temp IDs to real IDs) → create options → create edges (with condition validation) → set entry node.
6. Admin polls `GET /api/admin/flows/generate/status/{jobId}` until status is `Done` or `Failed`.

### The BANT Qualification Model

The generation prompt builds every flow around **BANT** (Budget, Authority, Need, Timeline) — the standard B2B qualification framework — and always produces four possible outcomes:

| Outcome | Terminal node | Meaning |
|---|---|---|
| **Hot** | `Offer` | Highly qualified, ready to talk — routed through `LeadCapture` first, then shown a calendar-booking offer |
| **Warm** | `Redirect` | Interested but not ready — shown nurture content/resource links |
| **Cold** | `Redirect` | Not a fit right now (budget, size, or timeline is off) |
| **Disqualified** | `Redirect` | Clear mismatch — exits immediately, often on the first question |

### AI Constraints & Quality Guarantees

- **Max 15 nodes** per generated flow (questions + info pages + lead capture + offers + redirects).
- **Branching-first topology** — the first question must immediately split into different paths (typically the NEED/FIT check, so a bad fit exits in one question); linear chains are rejected.
- Every path must terminate at an **Offer** or **Redirect** node — no dead ends. At least one path must reach an Offer (the Hot outcome).
- `LeadCapture` only ever appears on Hot paths, placed immediately before the Offer — contact info is collected only after a lead has qualified, and must include at least one `Email` field.
- Every `Redirect` node must carry a non-empty `disqualificationReason`, plus either a `redirectUrl` or at least one resource link (never neither).
- `attributeKey` on each question is a strict contract — edge conditions reference these keys (or the reserved `__score__` key for the accumulated score), and operators are validated against the node's `valueKind` (Text or Numeric).
- Every option on every question is assigned a `scoreDelta` so the accumulated score can drive tier-based routing (e.g. `>= 75 → Hot`, `50–74 → Warm`, `< 50 → Cold`).
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
| `Model` | `claude-sonnet-4-5-20250929` | Claude model ID |
| `MaxTokens` | `16000` | Hard cap on response tokens |
| `Temperature` | API default | Sampling temperature 0–1 |
| `Timeout` | 300s | HTTP timeout for non-streaming requests |

---

## Domain Model

The system is built around a **DAG (Directed Acyclic Graph)** architecture. Admins design "flows" — graphs of nodes connected by edges with conditional routing. End-users navigate these flows in quiz sessions, answering questions, getting scored, and — if they qualify — having a `Lead` record created for the flow owner's sales pipeline. Every flow can also carry any number of trackable `LeadChannel` links, so admins know which acquisition channel a given lead came from.

### Entity Relationship Diagram

```
ApplicationUser
  └─(1:1)─► UserProfile                    [CASCADE]

Flow  ◄── aggregate root ──────────────────────────────
  ├─(1:N)─► Node                            [CASCADE]
  │   ├─(1:N)─► Option                      [CASCADE]      (Question nodes — carries ScoreDelta)
  │   ├─(1:1)─► NodeLeadCapture              [CASCADE]      (LeadCapture nodes)
  │   │     └─(1:N)─► NodeLeadCaptureField   [CASCADE]
  │   ├─(1:1)─► NodeRedirect                 [CASCADE]      (Redirect nodes)
  │   │     └─(1:N)─► NodeRedirectLink       [CASCADE]
  │   └─(1:N)─► NodeOffer ─(M:1)─► Offer    [RESTRICT]      (Offer nodes)
  ├─(1:N)─► Edge                            [CASCADE]
  │   ├─(M:1 SourceNodeId)─► Node           [RESTRICT]
  │   └─(M:1 TargetNodeId)─► Node           [RESTRICT]
  ├─(1:N)─► LeadChannel                     [—]
  ├─(1:N)─► UserSession                     [RESTRICT]
  │   ├─(M:1 CurrentNodeId)─► Node          [RESTRICT]
  │   ├─(M:1 LeadChannelId)─► LeadChannel   [—]             (optional attribution)
  │   ├─(1:N)─► UserAnswer                  [CASCADE]
  │   │   └─(M:1 NodeId)─► Node             [RESTRICT]
  │   └─(1:N)─► SessionOffer                [CASCADE]
  │         └─(M:1 OfferId)─► Offer         [RESTRICT]
  └─(1:N)─► Lead                            [—]
        ├─(1:1 SessionId)─► UserSession     [unique]
        └─(M:1 LeadChannelId)─► LeadChannel [optional]
```

---

## Entities

### Flow (Aggregate Root)

The top-level container for a quiz. A flow owns all its nodes, edges, and lead channels.

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

A single step in the flow. Every node has a **type** that determines its behavior, and up to one type-specific detail record (`NodeLeadCapture`, `NodeRedirect`) or offer link (`NodeOffer`).

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `FlowId` | `Guid` | FK to parent Flow |
| `Type` | `NodeType` | `Question`, `InfoPage`, `Offer`, `LeadCapture`, or `Redirect` |
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

| Type | Purpose | Detail record | Terminal? |
|---|---|---|---|
| **Question** | Collects user input; each option can carry a score delta | — | No |
| **InfoPage** | Displays static content (text, media); exactly one unconditional outgoing edge | — | No |
| **LeadCapture** | A configurable form (up to 7 fields) that collects the respondent's contact/company details once they've qualified | `NodeLeadCapture` | No |
| **Offer** | Presents a product/service offer; reaching this is a **qualified** lead | `NodeOffer` link | Yes |
| **Redirect** | Sends the respondent to a disqualification page with a reason; reaching this is a **disqualified** lead | `NodeRedirect` | Yes |

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
- Edge conditions referencing a node's `AttributeKey` must use operators compatible with its `ValueKind` (e.g., `gt`/`lt`/`between` only for Numeric). The reserved key `__score__` (the session's accumulated score) is always numeric.

---

### NodeLeadCapture / NodeLeadCaptureField

The type-specific detail record for a `LeadCapture` node — a configurable, ordered set of fields to collect.

**NodeLeadCapture:**

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `NodeId` | `Guid` | FK to parent Node |
| `IsRequired` | `bool` | Whether the respondent can skip the whole step |

Holds up to **7** `NodeLeadCaptureField` children.

**NodeLeadCaptureField:**

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `NodeLeadCaptureId` | `Guid` | FK to parent NodeLeadCapture |
| `FieldType` | `LeadCaptureFieldType` | `FullName`, `Email`, `Phone`, `CompanyName`, `JobTitle`, `CompanySize`, or `Website` |
| `IsRequired` | `bool` | Whether this specific field is mandatory (Email is always effectively required) |
| `DisplayOrder` | `int` | Sort order |
| `Placeholder` | `string` | Placeholder text (default empty) |

Each `FieldType` maps to a reserved answer `AttributeKey` (e.g. `Email` → `lead_email`, `CompanyName` → `lead_company`) that the Lead-creation logic looks for when the session terminates.

**Constraints:** unique `(NodeLeadCaptureId, FieldType)` — a field type can only appear once per node.

---

### NodeRedirect / NodeRedirectLink

The type-specific detail record for a `Redirect` node — the disqualification/off-ramp page.

**NodeRedirect:**

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `NodeId` | `Guid` | FK to parent Node |
| `DisqualificationReason` | `string` | Required, human-readable reason this lead didn't qualify — propagated onto the `Lead` record |
| `RedirectUrl` | `string?` | Optional external URL to send the respondent to |
| `AutoRedirectAfterSeconds` | `int?` | If set, auto-redirects after this many seconds (minimum 3); null disables it |

Holds up to **3** `NodeRedirectLink` children (resource links shown on the page).

**Rule (`EnsureHasDestination`):** a Redirect node must have either a non-null `RedirectUrl` or at least one link — never neither, so the respondent is never shown a dead-end page.

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
| `ComparisonType` | `AND` \| `OR` | How multiple conditions on this edge combine |
| `CreatedAt` | `DateTime` | UTC creation timestamp |

**Rules:**
- Self-loops are forbidden (`SourceNodeId` ≠ `TargetNodeId`).
- Duplicate edges (same source + target) are prevented.
- Edges use `Restrict` delete on both source and target nodes — you must remove edges before deleting the nodes they reference.
- `Offer` and `Redirect` nodes have no outgoing edges (they're leaves); `InfoPage` and `LeadCapture` nodes only ever have a single unconditional outgoing edge.

#### Condition Format

```json
[
  { "AttributeKey": "budget_range", "Operator": "eq", "Value": "50k_plus" },
  { "AttributeKey": "__score__", "Operator": "gte", "Value": "75" }
]
```

**Supported operators:** `eq`, `neq`, `in`, `not_in`, `contains`, `between`, `gt`, `gte`, `lt`, `lte` (numeric-only operators are validated against the referenced attribute's `ValueKind`).

**Combining conditions:** each edge's `ComparisonType` decides whether **all** conditions must pass (`AND`) or **any** condition passing is enough (`OR`) for the edge to fire. Admins pick this per-edge in the DAG editor.

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
| `ScoreDelta` | `int` | Points added to (or subtracted from) the session's running score when this option is selected (default 0) |

`ScoreDelta` is what powers lead scoring — see [Scoring & Qualification](#scoring--qualification) below.

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
| `PhysicalWellnessKitName` | `string` | Name of an optional physical deliverable (max 300) |
| `PhysicalWellnessKitItems` | `string` | List of physical deliverable items (max 4000) |
| `Price` | `decimal?` | Purchase price — null means free (numeric 18,2) |
| `ImageUrl` | `string` | Marketing image URL (max 1000) |
| `CtaText` | `string` | Call-to-action button text (max 300) |
| `CtaUrl` | `string` | Where the CTA button links to (max 1000) |

**Deletion rules:**
- Cannot delete an offer while it is linked to any node (`NodeOffer` FK is Restrict).
- Must unlink from all nodes first, then delete.

---

### NodeOffer (Join: Node ↔ Offer)

Links an offer to a node, and carries the **tier** that a lead reaching this offer through this node is assigned.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `NodeId` | `Guid` | FK to Node (cascade — removing node removes link) |
| `OfferId` | `Guid` | FK to Offer (restrict — offer must be unlinked before deletion) |
| `IsPrimary` | `bool` | Marks this as the "recommended" offer when multiple exist |
| `Tier` | `QualificationTier` | Tier (typically `Hot`) assigned to a lead that reaches this offer |

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
| `Score` | `int` | Running total of `Option.ScoreDelta` from every answered question so far |
| `UtmSource` | `string` | UTM campaign source (max 200) |
| `UtmCampaign` | `string` | UTM campaign name (max 200) |
| `LeadChannelId` | `Guid?` | FK to the `LeadChannel` this session was attributed to, if it arrived via a trackable short link |
| `StartedAt` | `DateTime` | When session was created |
| `CompletedAt` | `DateTime?` | When session was completed (null if still in progress) |
| `UserNodePath` | `string?` | Ordered list of node IDs the user visited (serialized JSON) — enables path analytics and "go back" |

#### Session Status

| Status | Meaning |
|---|---|
| `InProgress` | User is actively navigating the flow |
| `Completed` | User reached a terminal node (Offer or Redirect) |
| `Abandoned` | User left without completing |

UTM parameters and `LeadChannelId` are tracked independently — a session can carry both. When a `Lead` is created, it inherits the session's `LeadChannelId` for channel-performance analytics.

---

### UserAnswer

Records one answer per question (or lead-capture field) per session.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `SessionId` | `Guid` | FK to UserSession (cascade — deleted with session) |
| `NodeId` | `Guid` | FK to the Node that was answered (restrict — preserves history) |
| `AttributeKey` | `string` | Copy of the node's attribute key (required, max 200) |
| `Value` | `string` | The user's answer (required, max 4000) |
| `AnsweredAt` | `DateTime` | When the answer was submitted |

Created for **Question** nodes and for each field submitted on a **LeadCapture** node (using that field's reserved `AttributeKey`, e.g. `lead_email`). Slider values, text input, and multiple selections are all stored as a single string in `Value`.

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
| `ConvertedAt` | `DateTime?` | When the conversion was recorded |

---

### Lead

Created automatically when a session reaches a terminal node (`Offer` or `Redirect`) with at least an email captured. This is the record a sales team actually works from.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `SessionId` | `Guid` | FK to the originating UserSession — **unique**, one lead per session |
| `FlowId` | `Guid` | FK to Flow |
| `FlowOwnerId` | `Guid` | The admin user who owns the flow (and therefore this lead) |
| `FullName` | `string?` | From the `lead_name` captured field |
| `Email` | `string` | Always present (leads require an email to be created); stored lowercase |
| `Phone` | `string?` | From `lead_phone` |
| `CompanyName` | `string?` | From `lead_company` |
| `JobTitle` | `string?` | From `lead_title` |
| `CompanySize` | `string?` | From `lead_company_size` |
| `Website` | `string?` | From `lead_website` |
| `Score` | `int` | The session's final accumulated score |
| `Tier` | `QualificationTier?` | `Hot` / `Warm` / `Cold` — taken from the `NodeOffer.Tier` for qualified leads; not set for disqualified leads |
| `LeadType` | `LeadType` | `Qualified` (reached an Offer) or `Disqualified` (reached a Redirect) |
| `DisqualificationReason` | `string?` | Copied from the `Redirect` node's reason, for disqualified leads only |
| `TerminalNodeId` | `Guid` | The Offer or Redirect node the session ended on |
| `TerminalNodeType` | `NodeType` | `Offer` or `Redirect` |
| `LeadChannelId` | `Guid?` | FK to the attributing `LeadChannel`, if any |
| `Status` | `LeadStatus` | Sales workflow status: `New` (default), `Contacted`, `Booked`, `Closed`, `Rejected` |
| `Notes` | `string?` | Free-text notes added by the sales team |
| `AssignedToId` | `Guid?` | Optional assignment to a specific admin user |
| `CreatedAt` | `DateTime` | Lead creation timestamp |
| `TimeToCompleteSeconds` | `int` | Time from session start to completion |

**Tier is not auto-computed from score at the moment** — it's set from the `NodeOffer.Tier` the qualifying path leads to (so the flow designer decides tier per-offer), and can be manually overridden afterwards via `PATCH .../leads/{leadId}`. Score is still recorded on the lead and surfaced in analytics (score distribution, per-lead score ring in the UI).

---

### LeadChannel

A trackable short link for one acquisition channel pointing at one flow — lets a business compare which channels (LinkedIn ad, cold email campaign, partner referral, etc.) actually bring in qualified leads.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `FlowId` | `Guid` | FK to parent Flow |
| `Name` | `string` | Internal display name — shown to the admin, not the lead (required, trimmed) |
| `ShortCode` | `string` | 8-character code, **globally unique** across all flows |
| `IsArchived` | `bool` | Archived channels stop resolving but keep their historical data |
| `CreatedAt` | `DateTime` | UTC creation timestamp |

**Methods:** `Create(flowId, name, shortCode, time)`, `Rename(name)`, `Archive()`, `Restore()`.

**Attribution flow:** a public request to `GET /api/public/lead-channels/{shortCode}` resolves the code to a `FlowId` + `LeadChannelId`; the client starts a session carrying that `LeadChannelId`; if the session eventually produces a `Lead`, the lead inherits it too — so channel performance can be measured all the way through to qualification, not just clicks.

**Deletion:** application-level rule (enforced in `LeadChannelsController`, not a DB constraint) — a channel can only be deleted while it has zero attributed sessions; otherwise archive it instead to preserve history.

---

## Scoring & Qualification

1. Every `Option` on a `Question` node carries a `ScoreDelta` (typically `+10`/`+20`/`+30` for positive signals, negative for red flags).
2. When an answer is submitted for a `SingleChoice`/`MultipleChoice` question, `SubmitAnswerUseCase` sums the `ScoreDelta` of every selected option and adds it to `UserSession.Score`.
3. The running score is exposed to edge-condition evaluation under the reserved attribute key **`__score__`**, so edges can branch on accumulated score (e.g. `__score__ >= 75 → Hot path`) in addition to individual answers.
4. When the session reaches a terminal node:
   - **Offer** → the session/lead is **Qualified**; `Lead.Tier` comes from that offer's `NodeOffer.Tier`.
   - **Redirect** → the session/lead is **Disqualified**; `Lead.DisqualificationReason` comes from the `NodeRedirect.DisqualificationReason`.
5. A lead is only created if an email was captured earlier in the session (via a `LeadCapture` node) — the `LeadCapture` step itself does not create the `Lead`; it stages the data that gets pulled in at termination.
6. There is currently no automatic score-based Tier assignment — Tier is a property of the offer the flow designer connects the Hot path to. A `ScoreDistributionDto.QualificationThreshold` field exists in the analytics DTO for a future score-cutoff feature but nothing sets it yet.

---

## How the Quiz Engine Works

### 1. Starting a Session

```
POST /api/quiz/sessions  { flowId, utmSource?, utmCampaign?, leadChannelId? }
```

1. Validates the flow exists and `IsPublished = true`.
2. Checks the flow has an `EntryNodeId` set.
3. Creates a `UserSession` with `Status = InProgress`, `Score = 0`, positioned at the entry node, carrying UTM and/or `LeadChannelId` if provided.
4. Returns the entry node details (title, description, options, linked offers).

### 2. Submitting an Answer

```
POST /api/quiz/sessions/{id}/answers  { nodeId, value }
```

1. Loads the session and verifies `Status = InProgress`.
2. Verifies `nodeId` matches the session's `CurrentNodeId`.
3. **For Question nodes:** creates a `UserAnswer`; sums selected options' `ScoreDelta` into `UserSession.Score`.
4. **For LeadCapture nodes:** creates a `UserAnswer` per submitted field, keyed by that field's reserved `AttributeKey`.
5. Builds the full **answer context** — a dictionary of all answers in this session keyed by `AttributeKey`, plus `__score__` for the running total.
6. **Edge resolution** — finds the next node:
   - Queries all edges where `SourceNodeId` = current node.
   - Sorts by `Priority` descending (highest first).
   - For each edge, evaluates its conditions (`AND`/`OR` per `ComparisonType`) against the answer context.
   - First edge whose conditions pass (or has no conditions) wins.
7. **If an edge matched:** moves session to the target node, returns new node state.
8. **If no edge matched:** marks session `Completed`, sets `CompletedAt`, and — if the terminal node is an `Offer` or `Redirect` and an email was captured — creates the `Lead` record (see [Scoring & Qualification](#scoring--qualification)).

### 3. Edge Condition Evaluation

```
edges = all edges FROM current node, sorted by Priority DESC
for each edge:
    if edge has no conditions → MATCH (unconditional)
    if conditions pass per ComparisonType (AND: all pass, OR: any passes) → MATCH
    otherwise → skip
if no edge matched → session is COMPLETE
```

Conditions reference previous answers by `AttributeKey`, or the running score via `__score__`. If a condition references an `AttributeKey` that hasn't been answered yet, the condition fails. Numeric operators (`gt`, `lt`, `between`, etc.) parse values as decimals for comparison.

### 4. Going Back

```
POST /api/quiz/sessions/{id}/back
```

Removes the last `UserAnswer` from the session, reverses its score contribution, and moves back to the previous node.

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
    "attributeKey": "budget_range",
    "title": "What's your budget for solving this?",
    "description": "Choose the range that best fits your plans.",
    "mediaUrl": null,
    "options": [
      { "id": "guid", "label": "Under $10k", "value": "under_10k", "displayOrder": 0, "mediaUrl": null },
      { "id": "guid", "label": "$10k–$50k", "value": "10k_50k", "displayOrder": 1, "mediaUrl": null },
      { "id": "guid", "label": "$50k+", "value": "50k_plus", "displayOrder": 2, "mediaUrl": null }
    ],
    "offers": []
  }
}
```

---

## Cascade & Restrict Delete Behaviors

| When deleting... | What happens |
|---|---|
| **Flow** | Nodes and Edges cascade-delete. UserSessions must be removed explicitly first (Restrict). LeadChannels and Leads have no DB-level cascade — remove/archive channels first. |
| **Node** | Options, `NodeLeadCapture`/`NodeLeadCaptureField`, and `NodeOffer` links cascade-delete. `NodeRedirect`/`NodeRedirectLink` cascade-delete. Edges referencing this node must be removed first (Restrict). Sessions on this node block deletion (Restrict). |
| **Offer** | Cannot delete while linked to any node or referenced by any SessionOffer (Restrict on both). |
| **UserSession** | UserAnswers and SessionOffers cascade-delete. |
| **LeadChannel** | Application-level rule only: `DELETE` is rejected (409) while any session/lead is attributed — archive it instead. |

---

## API

### Public — Quiz (`/api/quiz`) — No auth required

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/sessions` | Start a new quiz session (optionally with `leadChannelId` for attribution) |
| `GET` | `/sessions/{id}` | Get current session state |
| `POST` | `/sessions/{id}/answers` | Submit an answer (or LeadCapture form) |
| `POST` | `/sessions/{id}/back` | Go back one node |
| `POST` | `/sessions/{id}/convert` | Mark an offer as converted |

### Public — Lead Channels (`/api/public`) — No auth required

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/lead-channels/{shortCode}` | Resolve a short code to `{ flowId, leadChannelId }`; 404 if unknown, archived, or the flow is unpublished |

### Admin (authenticated) — `/api/admin`

| Resource | Endpoint | Methods | Description |
|---|---|---|---|
| **Flows** | `/flows` | GET, POST, PUT, DELETE | CRUD flows |
| | `/flows/{id}/entry-node` | PUT | Set flow entry point |
| | `/flows/{id}/publish` | POST | Publish flow |
| | `/flows/{id}/unpublish` | POST | Unpublish flow |
| **Nodes** | `/flows/{flowId}/nodes` | POST, PUT, DELETE | CRUD nodes (any type, including LeadCapture/Redirect detail payloads) |
| | `/flows/{flowId}/nodes/{id}/position` | PUT | Move node on canvas |
| **Edges** | `/flows/{flowId}/edges` | POST, PUT, DELETE | CRUD edges (conditions, `ComparisonType`, priority) |
| **Options** | `/nodes/{nodeId}/options` | POST, PUT, DELETE | CRUD options (incl. `ScoreDelta`) |
| | `/nodes/{nodeId}/options/reorder` | PUT | Reorder options |
| **Lead Capture fields** | `/nodes/{nodeId}/lead-capture-fields` | POST, PUT, DELETE | CRUD fields on a LeadCapture node |
| | `/nodes/{nodeId}/lead-capture-fields/reorder` | PUT | Reorder fields |
| **Redirect links** | `/nodes/{nodeId}/redirect-links` | POST, PUT, DELETE | CRUD resource links on a Redirect node |
| | `/nodes/{nodeId}/redirect-links/reorder` | PUT | Reorder links |
| **Offers** | `/offers` | GET, POST, PUT, DELETE | CRUD offers |
| **Node-Offers** | `/nodes/{nodeId}/offers` | GET, POST, PUT, DELETE | Link/unlink offers to nodes (incl. `Tier`) |
| **Leads** | `/flows/{flowId}/leads` | GET | List leads — filter by `tier`, `status`, `search`, `from`/`to`, sort by `CreatedAt`/`Score`/`CompanyName`/`FullName`/`TimeToComplete`, paginate |
| | `/flows/{flowId}/leads/{leadId}` | GET | Lead detail |
| | `/flows/{flowId}/leads/{leadId}` | PATCH | Update `Status`, `Tier`, `Notes`, or assignment |
| **Lead Channels** | `/flows/{flowId}/lead-channels` | GET | List a flow's channels with session/qualification counts |
| | `/flows/{flowId}/lead-channels` | POST | Create a channel (server generates the unique short code) |
| | `/flows/{flowId}/lead-channels/{channelId}` | PATCH | Rename or archive/restore a channel |
| | `/flows/{flowId}/lead-channels/{channelId}` | DELETE | Delete a channel (only if zero sessions attributed) |
| **AI Generation** | `/flows/generate` | POST | Generate a full flow from a natural-language prompt (returns job ID) |
| | `/flows/generate/status/{jobId}` | GET | Poll generation job status |
| **Analytics** | `/analytics/sessions` | GET | Account-wide session stats |
| | `/analytics/offers` | GET | Account-wide offer performance |
| | `/analytics/drop-offs` | GET | Account-wide drop-off analysis |
| | `/analytics/lead-quality` | GET | Account-wide tier distribution + top disqualification reasons |
| | `/analytics/channels` | GET | Account-wide channel performance, tagged per flow |
| **Per-flow stats** | `/flows/{flowId}/stats` | GET | Full per-flow analytics payload (summary, timing, funnel, daily series, score distribution, node stats, disqualification breakdown, path distribution, tier distribution, channel stats, conversion timing) |

### Auth — `/api/auth`

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/login` | Admin login |
| `GET` | `/me` | Current user info |

---

## Analytics Conventions

Two DTO families exist, each with its own percentage convention — worth knowing before consuming either:

| DTO family | Used by | Rate convention |
|---|---|---|
| `FlowAdminStats` (flow list stats) | `GET /api/admin/flows` | 0–100 scale, 2 decimal places (e.g. `75.50`) |
| `FlowStatsResponse` family (`FlowSummaryStats`, `TierDistributionEntryDto`, `ChannelStatsEntryDto`, `GlobalChannelStatItem`, `LeadQualityResponse`) | `GET /flows/{id}/stats`, `GET /analytics/lead-quality`, `GET /analytics/channels` | 0–1 ratio, typically rendered with 4 decimal places by consumers that multiply by 100 |

---

## Database Indexes

| Table | Index | Purpose |
|---|---|---|
| `Flows` | `IsPublished` | Quick lookup of published flows |
| `Edges` | `(FlowId, SourceNodeId)` | Find outgoing edges from a node |
| `Options` | `(NodeId, DisplayOrder)` | Render options in order |
| `Offers` | `Slug` (unique) | URL-based lookup |
| `NodeOffers` | `(NodeId, OfferId)` (unique) | Prevent duplicate links |
| `NodeLeadCaptureFields` | `(NodeLeadCaptureId, FieldType)` (unique) | Prevent duplicate field types per node |
| `NodeLeadCaptureFields` | `(NodeLeadCaptureId, DisplayOrder)` | Render fields in order |
| `NodeRedirectLinks` | `(NodeRedirectId, DisplayOrder)` | Render links in order |
| `UserSessions` | `FlowId`, `Status`, `LeadChannelId` | Analytics + channel performance queries |
| `UserAnswers` | `SessionId` | Load all answers for a session |
| `SessionOffers` | `OfferId`, `SessionId` | Offer performance analytics |
| `Leads` | `FlowId` | List leads for a flow |
| `Leads` | `(FlowId, LeadChannelId)` | Channel performance rollups |
| `Leads` | `(FlowId, Tier)`, `(FlowId, Status)` | Dashboard filters |
| `Leads` | `(FlowId, Email)` | Dedup check when creating a lead |
| `Leads` | `SessionId` (unique) | Enforces one lead per session |
| `LeadChannels` | `FlowId` | List a flow's channels |
| `LeadChannels` | `ShortCode` (unique) | Public short-link resolution |

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
