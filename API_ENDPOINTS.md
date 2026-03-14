# API Endpoints — Full Specification

All endpoints return JSON. Admin endpoints require `[Authorize]`. User-facing endpoints are anonymous.

---

## 1. AUTH (`/api/auth`) — Already Implemented

### POST `/api/auth/login`
**Access:** Anonymous
**Request Body:**
```json
{ "email": "string", "password": "string" }
```
**200 OK:**
```json
{ "userId": "guid", "email": "string", "userName": "string" }
```
Sets HttpOnly `auth` cookie via Identity.
**401** — invalid credentials | **423** — locked out

### GET `/api/auth/me`
**Access:** Authorize
**200 OK:**
```json
{ "userId": "guid", "email": "string", "userName": "string", "profileId": "guid?" }
```
**401** — not authenticated

### POST `/api/auth/logout`
**Access:** Authorize
**200 OK:** `{ "message": "Logged out successfully." }`

---

## 2. ADMIN — Flow Management (`/api/admin/flows`)

### GET `/api/admin/flows`
**Access:** Authorize
**Description:** List all flows (for the admin dashboard).
**Query Params:** none
**200 OK:**
```json
[
  {
    "id": "guid",
    "name": "string",
    "description": "string",
    "isPublished": true,
    "entryNodeId": "guid?",
    "createdAt": "datetime",
    "updatedAt": "datetime"
  }
]
```

### GET `/api/admin/flows/{flowId}`
**Access:** Authorize
**Description:** Get a single flow with ALL its nodes, edges, and options — everything the visual editor needs to render the canvas.
**200 OK:**
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isPublished": true,
  "entryNodeId": "guid?",
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "nodes": [
    {
      "id": "guid",
      "type": "question | info_page | offer",
      "attributeKey": "string?",
      "title": "string",
      "description": "string?",
      "mediaUrl": "string?",
      "positionX": 0.0,
      "positionY": 0.0,
      "options": [
        {
          "id": "guid",
          "label": "string",
          "value": "string",
          "displayOrder": 0,
          "mediaUrl": "string?"
        }
      ],
      "nodeOffers": [
        {
          "id": "guid",
          "offerId": "guid",
          "isPrimary": true,
          "offerName": "string",
          "offerSlug": "string"
        }
      ]
    }
  ],
  "edges": [
    {
      "id": "guid",
      "sourceNodeId": "guid",
      "targetNodeId": "guid",
      "priority": 0,
      "conditions": { ... } | null
    }
  ]
}
```
**404** — flow not found

### POST `/api/admin/flows`
**Access:** Authorize
**Description:** Create a new flow.
**Request Body:**
```json
{
  "name": "string (required)",
  "description": "string?"
}
```
**201 Created:**
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "isPublished": false,
  "entryNodeId": null,
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```
**400** — validation error

### PUT `/api/admin/flows/{flowId}`
**Access:** Authorize
**Description:** Update flow metadata (name, description).
**Request Body:**
```json
{
  "name": "string?",
  "description": "string?"
}
```
**200 OK:** updated flow object
**404** — flow not found

### PUT `/api/admin/flows/{flowId}/entry-node`
**Access:** Authorize
**Description:** Set the entry node for a flow.
**Request Body:**
```json
{ "entryNodeId": "guid" }
```
**200 OK:** updated flow object
**400** — node doesn't belong to this flow
**404** — flow or node not found

### POST `/api/admin/flows/{flowId}/publish`
**Access:** Authorize
**Description:** Publish this flow (sets `isPublished = true`, unpublishes any other currently published flow).
**Request Body:** none
**200 OK:** updated flow object
**400** — cannot publish without entry node

### POST `/api/admin/flows/{flowId}/unpublish`
**Access:** Authorize
**Description:** Unpublish this flow.
**Request Body:** none
**200 OK:** updated flow object

### DELETE `/api/admin/flows/{flowId}`
**Access:** Authorize
**Description:** Delete a flow and all its nodes/edges/options.
**200 OK:** `{ "message": "Flow deleted." }`
**400** — cannot delete a published flow
**404** — flow not found

---

## 3. ADMIN — Node Management (`/api/admin/flows/{flowId}/nodes`)

### POST `/api/admin/flows/{flowId}/nodes`
**Access:** Authorize
**Description:** Create a new node within a flow.
**Request Body:**
```json
{
  "type": "question | info_page | offer (required)",
  "title": "string (required)",
  "attributeKey": "string? (required if type=question)",
  "description": "string?",
  "mediaUrl": "string?",
  "positionX": 0.0,
  "positionY": 0.0
}
```
**201 Created:**
```json
{
  "id": "guid",
  "flowId": "guid",
  "type": "question",
  "attributeKey": "goal",
  "title": "What's your main goal?",
  "description": null,
  "mediaUrl": null,
  "positionX": 100.0,
  "positionY": 200.0,
  "createdAt": "datetime",
  "options": [],
  "nodeOffers": []
}
```
**400** — validation error (e.g. question node without attributeKey)
**404** — flow not found

### PUT `/api/admin/flows/{flowId}/nodes/{nodeId}`
**Access:** Authorize
**Description:** Update a node's properties.
**Request Body:**
```json
{
  "title": "string?",
  "description": "string?",
  "attributeKey": "string?",
  "mediaUrl": "string?",
  "positionX": 0.0,
  "positionY": 0.0
}
```
**200 OK:** updated node object
**404** — flow or node not found

### PUT `/api/admin/flows/{flowId}/nodes/{nodeId}/position`
**Access:** Authorize
**Description:** Move a node on the canvas (lightweight endpoint for drag-and-drop).
**Request Body:**
```json
{ "positionX": 350.0, "positionY": 120.0 }
```
**200 OK:** `{ "id": "guid", "positionX": 350.0, "positionY": 120.0 }`

### DELETE `/api/admin/flows/{flowId}/nodes/{nodeId}`
**Access:** Authorize
**Description:** Delete a node and its options. Also deletes all edges connected to it. If this node is the flow's entryNodeId, clears it.
**200 OK:** `{ "message": "Node deleted." }`
**404** — not found

---

## 4. ADMIN — Option Management (`/api/admin/nodes/{nodeId}/options`)

### POST `/api/admin/nodes/{nodeId}/options`
**Access:** Authorize
**Description:** Add an answer option to a question node.
**Request Body:**
```json
{
  "label": "string (required)",
  "value": "string (required)",
  "displayOrder": 0,
  "mediaUrl": "string?"
}
```
**201 Created:**
```json
{
  "id": "guid",
  "nodeId": "guid",
  "label": "Weight Loss",
  "value": "weight_loss",
  "displayOrder": 0,
  "mediaUrl": null
}
```
**400** — node is not a question node, or validation error
**404** — node not found

### PUT `/api/admin/nodes/{nodeId}/options/{optionId}`
**Access:** Authorize
**Description:** Update an option.
**Request Body:**
```json
{
  "label": "string?",
  "value": "string?",
  "displayOrder": 0,
  "mediaUrl": "string?"
}
```
**200 OK:** updated option object
**404** — not found

### DELETE `/api/admin/nodes/{nodeId}/options/{optionId}`
**Access:** Authorize
**200 OK:** `{ "message": "Option deleted." }`
**404** — not found

### PUT `/api/admin/nodes/{nodeId}/options/reorder`
**Access:** Authorize
**Description:** Batch-update display order for all options of a node.
**Request Body:**
```json
{
  "order": [
    { "optionId": "guid", "displayOrder": 0 },
    { "optionId": "guid", "displayOrder": 1 },
    { "optionId": "guid", "displayOrder": 2 }
  ]
}
```
**200 OK:** `{ "message": "Options reordered." }`

---

## 5. ADMIN — Edge Management (`/api/admin/flows/{flowId}/edges`)

### POST `/api/admin/flows/{flowId}/edges`
**Access:** Authorize
**Description:** Create an edge (connection) between two nodes.
**Request Body:**
```json
{
  "sourceNodeId": "guid (required)",
  "targetNodeId": "guid (required)",
  "priority": 0,
  "conditions": null | {
    "operator": "AND | OR",
    "rules": [
      { "attribute": "goal", "op": "eq", "value": "weight_loss" },
      { "attribute": "context", "op": "eq", "value": "home" }
    ]
  }
}
```
Supported `op` values: `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `in`, `contains`, `not_in`.
`conditions: null` means this is the fallback/default edge.

**201 Created:**
```json
{
  "id": "guid",
  "flowId": "guid",
  "sourceNodeId": "guid",
  "targetNodeId": "guid",
  "priority": 0,
  "conditions": { ... } | null,
  "createdAt": "datetime"
}
```
**400** — source = target, nodes not in this flow, would create cycle
**404** — flow or node not found

### PUT `/api/admin/flows/{flowId}/edges/{edgeId}`
**Access:** Authorize
**Description:** Update an edge's conditions and/or priority.
**Request Body:**
```json
{
  "priority": 10,
  "conditions": { ... } | null
}
```
**200 OK:** updated edge object
**404** — not found

### DELETE `/api/admin/flows/{flowId}/edges/{edgeId}`
**Access:** Authorize
**200 OK:** `{ "message": "Edge deleted." }`
**404** — not found

---

## 6. ADMIN — Offer Catalog (`/api/admin/offers`)

### GET `/api/admin/offers`
**Access:** Authorize
**Description:** List all offers in the catalog.
**200 OK:**
```json
[
  {
    "id": "guid",
    "slug": "weight_loss_starter",
    "name": "Weight Loss Starter (Home)",
    "description": "string",
    "duration": "4 weeks",
    "digitalContent": "string",
    "kitName": "Home Fat-Burn Kit",
    "kitContents": "resistance bands, jump rope...",
    "price": 49.99,
    "imageUrl": "string?",
    "ctaText": "Start My Plan",
    "ctaUrl": "string"
  }
]
```

### GET `/api/admin/offers/{offerId}`
**Access:** Authorize
**200 OK:** single offer object
**404** — not found

### POST `/api/admin/offers`
**Access:** Authorize
**Description:** Create a new offer.
**Request Body:**
```json
{
  "slug": "string (required, unique)",
  "name": "string (required)",
  "description": "string?",
  "duration": "string?",
  "digitalContent": "string?",
  "kitName": "string?",
  "kitContents": "string?",
  "price": 0.00,
  "imageUrl": "string?",
  "ctaText": "string?",
  "ctaUrl": "string?"
}
```
**201 Created:** offer object
**400** — validation error
**409** — slug already exists

### PUT `/api/admin/offers/{offerId}`
**Access:** Authorize
**Description:** Update an offer.
**Request Body:** same fields as POST (all optional for partial update)
**200 OK:** updated offer object
**404** — not found
**409** — slug conflict

### DELETE `/api/admin/offers/{offerId}`
**Access:** Authorize
**200 OK:** `{ "message": "Offer deleted." }`
**400** — offer is referenced by node_offers
**404** — not found

---

## 7. ADMIN — Node–Offer Links (`/api/admin/nodes/{nodeId}/offers`)

### GET `/api/admin/nodes/{nodeId}/offers`
**Access:** Authorize
**Description:** List offers linked to an offer-type node.
**200 OK:**
```json
[
  {
    "id": "guid",
    "nodeId": "guid",
    "offerId": "guid",
    "isPrimary": true,
    "offer": { "slug": "string", "name": "string" }
  }
]
```

### POST `/api/admin/nodes/{nodeId}/offers`
**Access:** Authorize
**Description:** Link an offer to an offer-type node.
**Request Body:**
```json
{
  "offerId": "guid (required)",
  "isPrimary": true
}
```
**201 Created:** node_offer object
**400** — node is not type `offer`, or max 2 offers already linked
**404** — node or offer not found

### PUT `/api/admin/nodes/{nodeId}/offers/{nodeOfferId}`
**Access:** Authorize
**Description:** Update isPrimary flag.
**Request Body:**
```json
{ "isPrimary": false }
```
**200 OK:** updated node_offer object

### DELETE `/api/admin/nodes/{nodeId}/offers/{nodeOfferId}`
**Access:** Authorize
**200 OK:** `{ "message": "Node-offer link removed." }`

---

## 8. CONTENT DELIVERY API (`/api/content`)

These endpoints serve the published flow configuration to the User FE. No auth required.

### GET `/api/content/flow`
**Access:** Anonymous
**Description:** Returns the currently published flow as raw content (the full DAG config). This is what the User FE fetches on load to render the quiz.
**200 OK:**
```json
{
  "flowId": "guid",
  "name": "string",
  "entryNodeId": "guid",
  "nodes": [
    {
      "id": "guid",
      "type": "question | info_page | offer",
      "attributeKey": "string?",
      "title": "string",
      "description": "string?",
      "mediaUrl": "string?",
      "options": [
        {
          "id": "guid",
          "label": "string",
          "value": "string",
          "displayOrder": 0,
          "mediaUrl": "string?"
        }
      ]
    }
  ],
  "edges": [
    {
      "id": "guid",
      "sourceNodeId": "guid",
      "targetNodeId": "guid",
      "priority": 0,
      "conditions": { ... } | null
    }
  ]
}
```
**Note:** Does NOT include `positionX/Y` (that's admin-only). Does NOT include offer details on offer nodes (those are resolved server-side when the user reaches them).
**404** — no published flow exists

### GET `/api/content/flow/{flowId}`
**Access:** Anonymous
**Description:** Get a specific flow by ID (useful if you want to serve a non-published flow for testing/preview).
**200 OK:** same shape as above
**404** — flow not found

---

## 9. USER — Session & Quiz Engine (`/api/quiz`)

These endpoints power the user's journey through the quiz. No auth required (anonymous users from ads).

### POST `/api/quiz/sessions`
**Access:** Anonymous
**Description:** Start a new quiz session. Called when user lands on the page from an ad.
**Request Body:**
```json
{
  "flowId": "guid? (optional — if omitted, uses published flow)",
  "utmSource": "string?",
  "utmCampaign": "string?"
}
```
**201 Created:**
```json
{
  "sessionId": "guid",
  "flowId": "guid",
  "currentNode": {
    "id": "guid",
    "type": "question",
    "attributeKey": "goal",
    "title": "What's your main goal?",
    "description": "string?",
    "mediaUrl": "string?",
    "options": [
      { "id": "guid", "label": "Weight Loss", "value": "weight_loss", "displayOrder": 0, "mediaUrl": null },
      { "id": "guid", "label": "Strength", "value": "strength", "displayOrder": 1, "mediaUrl": null }
    ]
  },
  "progress": {
    "answeredCount": 0
  }
}
```
Returns the entry node immediately so the FE can render the first screen without a second request.
**404** — no published flow found (or specified flowId not found)

### POST `/api/quiz/sessions/{sessionId}/answers`
**Access:** Anonymous
**Description:** Submit an answer for the current node and get the next node. This is the core engine endpoint — called every time the user answers a question or clicks "Next" on an info page.
**Request Body:**
```json
{
  "nodeId": "guid (required — the node being answered)",
  "value": "string? (required for question nodes, null for info_page)"
}
```
**Backend logic:**
1. Validate `nodeId` matches `current_node_id` on the session
2. If question node: save `user_answer` (attribute_key from node, value from request)
3. Load all edges where `source_node_id = nodeId`
4. Sort by `priority` descending
5. Evaluate each edge's `conditions` against ALL `user_answers` for this session
6. First match → that's the next node. `conditions: null` = fallback.
7. Update `user_session.current_node_id` to the next node
8. If next node is type `offer` → resolve offers from `node_offer`, save `session_offer` rows, set session `status = completed`

**200 OK (next node is question or info_page):**
```json
{
  "sessionId": "guid",
  "currentNode": {
    "id": "guid",
    "type": "question",
    "attributeKey": "context",
    "title": "Where do you prefer to exercise?",
    "description": null,
    "mediaUrl": null,
    "options": [
      { "id": "guid", "label": "At Home", "value": "home", "displayOrder": 0, "mediaUrl": null },
      { "id": "guid", "label": "At the Gym", "value": "gym", "displayOrder": 1, "mediaUrl": null },
      { "id": "guid", "label": "Outdoors", "value": "outdoor", "displayOrder": 2, "mediaUrl": null }
    ]
  },
  "progress": {
    "answeredCount": 1
  }
}
```

**200 OK (next node is offer — quiz complete):**
```json
{
  "sessionId": "guid",
  "currentNode": {
    "id": "guid",
    "type": "offer"
  },
  "offers": [
    {
      "isPrimary": true,
      "offer": {
        "id": "guid",
        "slug": "weight_loss_starter",
        "name": "Weight Loss Starter (Home)",
        "description": "4-week weight loss plan...",
        "duration": "4 weeks",
        "digitalContent": "Home workout plan (20-30 min)...",
        "kitName": "Home Fat-Burn Kit",
        "kitContents": "resistance bands, jump rope, shaker bottle, electrolytes + healthy snack",
        "price": 49.99,
        "imageUrl": "string?",
        "ctaText": "Start My Plan",
        "ctaUrl": "https://..."
      }
    },
    {
      "isPrimary": false,
      "offer": {
        "id": "guid",
        "slug": "stress_reset",
        "name": "Stress Reset Program",
        "description": "...",
        ...
      }
    }
  ],
  "progress": {
    "answeredCount": 4
  },
  "status": "completed"
}
```
**400** — nodeId doesn't match current node, or missing value for question node
**404** — session not found
**409** — session already completed/abandoned

### GET `/api/quiz/sessions/{sessionId}`
**Access:** Anonymous
**Description:** Get current session state (for page refresh / reconnect).
**200 OK:**
```json
{
  "sessionId": "guid",
  "flowId": "guid",
  "status": "in_progress | completed | abandoned",
  "currentNode": { ... },
  "answers": [
    { "attributeKey": "goal", "value": "weight_loss", "answeredAt": "datetime" },
    { "attributeKey": "context", "value": "home", "answeredAt": "datetime" }
  ],
  "offers": null | [ ... ],
  "progress": {
    "answeredCount": 2
  }
}
```
If status is `completed`, includes the `offers` array. If `in_progress`, `offers` is null and `currentNode` shows where the user left off.
**404** — session not found

### POST `/api/quiz/sessions/{sessionId}/back`
**Access:** Anonymous
**Description:** Go back to the previous node (undo last answer). Deletes the last `user_answer` and sets `current_node_id` to the previous node.
**200 OK:**
```json
{
  "sessionId": "guid",
  "currentNode": { ... },
  "progress": {
    "answeredCount": 1
  }
}
```
**400** — already at entry node (nothing to go back to)
**404** — session not found
**409** — session completed/abandoned

### POST `/api/quiz/sessions/{sessionId}/convert`
**Access:** Anonymous
**Description:** Mark that the user clicked the CTA on an offer. Updates `session_offer.converted = true`.
**Request Body:**
```json
{ "offerId": "guid" }
```
**200 OK:** `{ "message": "Conversion recorded." }`
**404** — session or offer not found in this session

---

## 10. ADMIN — Analytics (`/api/admin/analytics`)

### GET `/api/admin/analytics/sessions`
**Access:** Authorize
**Description:** Get session statistics for a flow.
**Query Params:**
- `flowId` (guid, required)
- `from` (datetime, optional)
- `to` (datetime, optional)

**200 OK:**
```json
{
  "total": 1250,
  "completed": 940,
  "abandoned": 210,
  "inProgress": 100,
  "completionRate": 0.752,
  "conversionRate": 0.34,
  "avgAnswersBeforeCompletion": 5.2
}
```

### GET `/api/admin/analytics/offers`
**Access:** Authorize
**Description:** Get offer presentation and conversion stats.
**Query Params:**
- `flowId` (guid, required)
- `from` / `to` (optional)

**200 OK:**
```json
[
  {
    "offerId": "guid",
    "offerName": "Weight Loss Starter (Home)",
    "offerSlug": "weight_loss_starter",
    "timesPrimary": 320,
    "timesAddon": 45,
    "conversions": 112,
    "conversionRate": 0.307
  }
]
```

### GET `/api/admin/analytics/drop-offs`
**Access:** Authorize
**Description:** Shows which nodes users abandon the quiz on — helps identify problem questions.
**Query Params:**
- `flowId` (guid, required)
- `from` / `to` (optional)

**200 OK:**
```json
[
  {
    "nodeId": "guid",
    "nodeTitle": "What's your stress level?",
    "dropOffCount": 45,
    "dropOffRate": 0.12
  }
]
```

---

## Endpoint Summary Table

| # | Method | Path | Auth | Purpose |
|---|--------|------|------|---------|
| | **AUTH** | | | |
| 1 | POST | `/api/auth/login` | No | Login |
| 2 | GET | `/api/auth/me` | Yes | Current user |
| 3 | POST | `/api/auth/logout` | Yes | Logout |
| | **ADMIN — FLOWS** | | | |
| 4 | GET | `/api/admin/flows` | Yes | List flows |
| 5 | GET | `/api/admin/flows/{id}` | Yes | Get flow + full DAG |
| 6 | POST | `/api/admin/flows` | Yes | Create flow |
| 7 | PUT | `/api/admin/flows/{id}` | Yes | Update flow |
| 8 | PUT | `/api/admin/flows/{id}/entry-node` | Yes | Set entry node |
| 9 | POST | `/api/admin/flows/{id}/publish` | Yes | Publish flow |
| 10 | POST | `/api/admin/flows/{id}/unpublish` | Yes | Unpublish flow |
| 11 | DELETE | `/api/admin/flows/{id}` | Yes | Delete flow |
| | **ADMIN — NODES** | | | |
| 12 | POST | `/api/admin/flows/{id}/nodes` | Yes | Create node |
| 13 | PUT | `/api/admin/flows/{id}/nodes/{id}` | Yes | Update node |
| 14 | PUT | `/api/admin/flows/{id}/nodes/{id}/position` | Yes | Move node (drag) |
| 15 | DELETE | `/api/admin/flows/{id}/nodes/{id}` | Yes | Delete node |
| | **ADMIN — OPTIONS** | | | |
| 16 | POST | `/api/admin/nodes/{id}/options` | Yes | Add option |
| 17 | PUT | `/api/admin/nodes/{id}/options/{id}` | Yes | Update option |
| 18 | DELETE | `/api/admin/nodes/{id}/options/{id}` | Yes | Delete option |
| 19 | PUT | `/api/admin/nodes/{id}/options/reorder` | Yes | Reorder options |
| | **ADMIN — EDGES** | | | |
| 20 | POST | `/api/admin/flows/{id}/edges` | Yes | Create edge |
| 21 | PUT | `/api/admin/flows/{id}/edges/{id}` | Yes | Update edge |
| 22 | DELETE | `/api/admin/flows/{id}/edges/{id}` | Yes | Delete edge |
| | **ADMIN — OFFERS** | | | |
| 23 | GET | `/api/admin/offers` | Yes | List offers |
| 24 | GET | `/api/admin/offers/{id}` | Yes | Get offer |
| 25 | POST | `/api/admin/offers` | Yes | Create offer |
| 26 | PUT | `/api/admin/offers/{id}` | Yes | Update offer |
| 27 | DELETE | `/api/admin/offers/{id}` | Yes | Delete offer |
| | **ADMIN — NODE↔OFFER** | | | |
| 28 | GET | `/api/admin/nodes/{id}/offers` | Yes | List linked offers |
| 29 | POST | `/api/admin/nodes/{id}/offers` | Yes | Link offer to node |
| 30 | PUT | `/api/admin/nodes/{id}/offers/{id}` | Yes | Update link |
| 31 | DELETE | `/api/admin/nodes/{id}/offers/{id}` | Yes | Unlink offer |
| | **CONTENT DELIVERY** | | | |
| 32 | GET | `/api/content/flow` | No | Get published flow |
| 33 | GET | `/api/content/flow/{id}` | No | Get specific flow |
| | **USER QUIZ ENGINE** | | | |
| 34 | POST | `/api/quiz/sessions` | No | Start session |
| 35 | POST | `/api/quiz/sessions/{id}/answers` | No | Submit answer → next node |
| 36 | GET | `/api/quiz/sessions/{id}` | No | Get session state |
| 37 | POST | `/api/quiz/sessions/{id}/back` | No | Go back one step |
| 38 | POST | `/api/quiz/sessions/{id}/convert` | No | Record CTA click |
| | **ADMIN — ANALYTICS** | | | |
| 39 | GET | `/api/admin/analytics/sessions` | Yes | Session stats |
| 40 | GET | `/api/admin/analytics/offers` | Yes | Offer stats |
| 41 | GET | `/api/admin/analytics/drop-offs` | Yes | Drop-off analysis |

**Total: 41 endpoints** (3 auth + 28 admin + 2 content delivery + 5 user quiz + 3 analytics)
