# API Endpoints Documentation

**Generated:** 2026-04-19  
**Project:** Hackaton INT20'26 Task  
**Base URL:** `https://api.example.com`

---

## Table of Contents

1. [Authentication](#1-authentication-controller)
2. [Content Delivery](#2-content-delivery-controller)
3. [Quiz Engine](#3-quiz-engine-controller)
4. [Admin — Flows](#4-admin--flows-controller)
5. [Admin — Nodes](#5-admin--nodes-controller)
6. [Admin — Edges](#6-admin--edges-controller)
7. [Admin — Offers](#7-admin--offers-controller)
8. [Admin — Node Offers](#8-admin--node-offers-controller)
9. [Admin — Options](#9-admin--options-controller)
10. [Admin — Leads](#10-admin--leads-controller)
11. [Admin — Analytics](#11-admin--analytics-controller)
12. [Admin — AI Generation](#12-admin--ai-generation-controller)

---

## 1. Authentication Controller

**Route Prefix:** `/api/auth`  
**Access:** Public (except `/me` and `/logout` require authentication)  
**Purpose:** Handles user authentication, registration, and profile access. Sets HttpOnly Identity cookies on login/signup.

### Endpoints

#### POST /api/auth/login
- **Summary:** Login
- **Description:** Validates user credentials. On success, sets an HttpOnly Identity cookie automatically.
- **Auth Required:** No
- **Request Body:**
  ```json
  {
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }
  ```

**Response Examples:**

✅ **200 OK - Successful Login**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "userName": "user123"
}
```

❌ **400 Bad Request - Validation Error**
```json
{
  "message": "Email is required. Password must be at least 8 characters."
}
```

❌ **401 Unauthorized - Invalid Credentials**
```json
{
  "message": "Invalid email or password."
}
```

❌ **423 Locked - Account Locked**
```json
{
  "message": "Account is temporarily locked out due to multiple failed login attempts. Try again in 15 minutes."
}
```

---

#### POST /api/auth/signup
- **Summary:** Sign up
- **Description:** Creates a new user account with email and password. Sets the auth cookie on success.
- **Auth Required:** No
- **Request Body:**
  ```json
  {
    "email": "newuser@example.com",
    "password": "SecurePassword123!",
    "confirmPassword": "SecurePassword123!"
  }
  ```

**Response Examples:**

✅ **200 OK - Account Created**
```json
{
  "userId": "660e8400-e29b-41d4-a716-446655440000",
  "email": "newuser@example.com",
  "userName": "newuser123"
}
```

❌ **400 Bad Request - Validation Error**
```json
{
  "message": "Email 'duplicate@example.com' is already registered. Password must be at least 8 characters and contain uppercase, lowercase, number, and special character."
}
```

---

#### GET /api/auth/me
- **Summary:** Get current user
- **Description:** Returns profile information for the authenticated user. Requires valid authentication cookie.
- **Auth Required:** Yes
- **Response Codes:**
  - `200 OK` - Returns user profile information
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - User record not found

**Response Examples:**

✅ **200 OK - User Profile**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "userName": "user123",
  "profileId": "770e8400-e29b-41d4-a716-446655440000"
}
```

❌ **401 Unauthorized - No Auth**
```json
{
  "message": "Unable to identify user."
}
```

---

#### POST /api/auth/logout
- **Summary:** Logout
- **Description:** Signs out the user and clears the Identity authentication cookie.
- **Auth Required:** Yes
- **Response Codes:**
  - `200 OK` - Logged out successfully
  - `401 Unauthorized` - Not authenticated

**Response Examples:**

✅ **200 OK - Logged Out**
```json
{
  "message": "Logged out successfully."
}
```

---

## 2. Content Delivery Controller

**Route Prefix:** `/api/content`  
**Access:** Public (no authentication required)  
**Purpose:** Provides public endpoints for end-users to access published flows and content. Used by the quiz/survey engine frontend.

### Endpoints

#### GET /api/content/flow
- **Summary:** Get published flow
- **Description:** Returns the newest published flow with its complete DAG (nodes, edges, offers). Used by end-users to load content.
- **Auth Required:** No
- **Query Parameters:** None
- **Response Codes:**
  - `200 OK` - Published flow detail with full graph structure
  - `404 Not Found` - No published flow available

**Response Examples:**

✅ **200 OK - Published Flow with Complete DAG**
```json
{
  "id": "880e8400-e29b-41d4-a716-446655440000",
  "name": "Product Survey 2026",
  "description": "Gathering user feedback on new product launch",
  "isPublished": true,
  "entryNodeId": "990e8400-e29b-41d4-a716-446655440000",
  "createdAt": "2026-03-15T10:30:00Z",
  "updatedAt": "2026-04-10T15:45:00Z",
  "nodes": [
    {
      "id": "990e8400-e29b-41d4-a716-446655440000",
      "type": "Question",
      "attributeKey": null,
      "valueKind": null,
      "title": "How likely are you to recommend our product?",
      "description": "On a scale of 1-10",
      "mediaUrl": null,
      "positionX": 100.0,
      "positionY": 50.0,
      "createdAt": "2026-03-15T10:30:00Z",
      "answerType": "SingleChoice",
      "sliderMin": null,
      "sliderMax": null,
      "options": [
        {
          "id": "aa0e8400-e29b-41d4-a716-446655440000",
          "label": "Very Likely (9-10)",
          "value": "very_likely",
          "displayOrder": 0,
          "mediaUrl": null
        }
      ],
      "nodeOffers": [
        {
          "id": "bb0e8400-e29b-41d4-a716-446655440000",
          "offerId": "cc0e8400-e29b-41d4-a716-446655440000",
          "isPrimary": true,
          "offer": {
            "id": "cc0e8400-e29b-41d4-a716-446655440000",
            "slug": "premium-plan",
            "name": "Premium Plan",
            "headline": "Upgrade to Premium",
            "body": "Get access to exclusive features",
            "imageUrl": "https://cdn.example.com/premium.jpg",
            "ctaText": "Upgrade Now",
            "ctaUrl": "https://example.com/upgrade"
          }
        }
      ],
      "stats": null
    }
  ],
  "edges": [
    {
      "id": "dd0e8400-e29b-41d4-a716-446655440000",
      "sourceNodeId": "990e8400-e29b-41d4-a716-446655440000",
      "targetNodeId": "ee0e8400-e29b-41d4-a716-446655440000",
      "priority": 0,
      "conditions": null
    }
  ],
  "stats": null,
  "pathDistribution": [],
  "attributeKeys": []
}
```

❌ **404 Not Found - No Published Flow**
```json
{
  "message": "No published flow available."
}
```

---

#### GET /api/content/flow/{id}
- **Summary:** Get published flow by ID
- **Description:** Returns a specific published flow by its ID with the complete DAG (nodes, edges, offers). Allows accessing archived published flows.
- **Auth Required:** No
- **Path Parameters:**
  - `id` (GUID) - Flow ID
- **Response Codes:**
  - `200 OK` - Published flow detail with full graph structure
  - `404 Not Found` - Published flow with that ID not found

---

## 3. Quiz Engine Controller

**Route Prefix:** `/api/quiz`  
**Access:** Public (no authentication required)  
**Purpose:** Provides the quiz/survey engine endpoints. Handles session management, answer submission, navigation, and conversion tracking.

### Endpoints

#### POST /api/quiz/sessions
- **Summary:** Start a new quiz session
- **Description:** Initiates a new session for a published flow. Returns the initial state with the entry node.
- **Auth Required:** No
- **Request Body:**
  ```json
  {
    "flowId": "880e8400-e29b-41d4-a716-446655440000",
    "source": "email_campaign_001"
  }
  ```

**Response Examples:**

✅ **201 Created - Session Started**
```json
{
  "sessionId": "ff0e8400-e29b-41d4-a716-446655440000",
  "flowId": "880e8400-e29b-41d4-a716-446655440000",
  "status": "InProgress",
  "startedAt": "2026-04-19T12:30:00Z",
  "completedAt": null,
  "currentNode": {
    "id": "990e8400-e29b-41d4-a716-446655440000",
    "type": "Question",
    "attributeKey": null,
    "answerType": "SingleChoice",
    "valueKind": null,
    "sliderMin": null,
    "sliderMax": null,
    "title": "How likely are you to recommend our product?",
    "description": "On a scale of 1-10",
    "mediaUrl": null,
    "options": [
      {
        "id": "aa0e8400-e29b-41d4-a716-446655440000",
        "label": "Very Likely (9-10)",
        "value": "very_likely",
        "displayOrder": 0,
        "mediaUrl": null,
        "scoreDelta": 10
      }
    ],
    "offers": [],
    "leadCapture": null,
    "redirect": null
  }
}
```

❌ **404 Not Found - Flow Not Found**
```json
{
  "message": "Flow not found."
}
```

❌ **422 Unprocessable Entity - Flow Not Publishable**
```json
{
  "message": "Flow is not published or has no entry node configured."
}
```

---

#### GET /api/quiz/sessions/{id}
- **Summary:** Get session state
- **Description:** Returns the current state of a quiz session, including the current node and session metadata.
- **Auth Required:** No
- **Path Parameters:**
  - `id` (GUID) - Session ID
- **Response Codes:**
  - `200 OK` - Session state with current node and progress
  - `404 Not Found` - Session not found

**Response Examples:**

✅ **200 OK - Session State**
```json
{
  "sessionId": "ff0e8400-e29b-41d4-a716-446655440000",
  "flowId": "880e8400-e29b-41d4-a716-446655440000",
  "status": "InProgress",
  "startedAt": "2026-04-19T12:30:00Z",
  "completedAt": null,
  "currentNode": {
    "id": "990e8400-e29b-41d4-a716-446655440000",
    "type": "Question",
    "title": "How likely are you to recommend our product?",
    "options": [
      {
        "id": "aa0e8400-e29b-41d4-a716-446655440000",
        "label": "Very Likely (9-10)",
        "value": "very_likely",
        "displayOrder": 0,
        "scoreDelta": 10
      }
    ],
    "offers": []
  }
}
```

❌ **404 Not Found**
```json
{
  "message": "Session not found."
}
```

---

#### POST /api/quiz/sessions/{id}/answers
- **Summary:** Submit an answer
- **Description:** Records an answer to the current quiz node and automatically advances to the next node. Handles routing based on edge conditions.
- **Auth Required:** No
- **Path Parameters:**
  - `id` (GUID) - Session ID
- **Request Body:**
  ```json
  {
    "nodeId": "990e8400-e29b-41d4-a716-446655440000",
    "optionId": "aa0e8400-e29b-41d4-a716-446655440000"
  }
  ```

**Response Examples:**

✅ **200 OK - Answer Recorded, Advanced to Next Node**
```json
{
  "sessionId": "ff0e8400-e29b-41d4-a716-446655440000",
  "flowId": "880e8400-e29b-41d4-a716-446655440000",
  "status": "InProgress",
  "startedAt": "2026-04-19T12:30:00Z",
  "completedAt": null,
  "currentNode": {
    "id": "ee0e8400-e29b-41d4-a716-446655440000",
    "type": "Offer",
    "title": "Premium Plan Available",
    "options": [],
    "offers": [
      {
        "id": "cc0e8400-e29b-41d4-a716-446655440000",
        "name": "Premium Plan",
        "slug": "premium-plan",
        "headline": "Upgrade to Premium",
        "body": "Get access to exclusive features",
        "imageUrl": "https://cdn.example.com/premium.jpg",
        "ctaText": "Upgrade Now",
        "ctaUrl": "https://example.com/upgrade",
        "isPrimary": true,
        "tier": "hot"
      }
    ]
  }
}
```

❌ **422 Unprocessable Entity - Invalid Node**
```json
{
  "message": "The submitted node does not match the current session node."
}
```

---

#### POST /api/quiz/sessions/{id}/back
- **Summary:** Go back to previous node
- **Description:** Moves the session back one step and removes the last answer. Cannot go before the entry node.
- **Auth Required:** No
- **Path Parameters:**
  - `id` (GUID) - Session ID
- **Response Codes:**
  - `200 OK` - Session moved back to previous node
  - `404 Not Found` - Session not found
  - `422 Unprocessable Entity` - Session not active or already at beginning

**Response Examples:**

✅ **200 OK - Moved Back Successfully**
```json
{
  "sessionId": "ff0e8400-e29b-41d4-a716-446655440000",
  "flowId": "880e8400-e29b-41d4-a716-446655440000",
  "status": "InProgress",
  "startedAt": "2026-04-19T12:30:00Z",
  "completedAt": null,
  "currentNode": {
    "id": "990e8400-e29b-41d4-a716-446655440000",
    "type": "Question",
    "title": "How likely are you to recommend our product?",
    "options": [
      {
        "id": "aa0e8400-e29b-41d4-a716-446655440000",
        "label": "Very Likely (9-10)",
        "value": "very_likely",
        "displayOrder": 0
      }
    ],
    "offers": []
  }
}
```

❌ **422 Unprocessable Entity - Already at Beginning**
```json
{
  "message": "Cannot go back. Session is already at the first node."
}
```

---

#### POST /api/quiz/sessions/{id}/convert
- **Summary:** Convert an offer
- **Description:** Marks an offer as converted (user clicked CTA, purchased, or otherwise completed the call-to-action). Tracked for analytics.
- **Auth Required:** No
- **Path Parameters:**
  - `id` (GUID) - Session ID
- **Request Body:**
  ```json
  {
    "offerId": "cc0e8400-e29b-41d4-a716-446655440000"
  }
  ```

**Response Examples:**

✅ **200 OK - Offer Converted**
```json
{}
```

❌ **404 Not Found - Session or Offer Not Found**
```json
{
  "message": "Session or offer not found."
}
```

---

## 4. Admin — Flows Controller

**Route Prefix:** `/api/admin/flows`  
**Access:** Admin only (authentication required)  
**Purpose:** Full CRUD operations for survey/quiz flows. Handles flow lifecycle including creation, editing, publishing, and deletion.

### Endpoints

#### GET /api/admin/flows
- **Summary:** List flows
- **Description:** Returns a summary list of all flows owned by the authenticated user.
- **Auth Required:** Yes
- **Response Codes:**
  - `200 OK` - Flow list with summaries
  - `401 Unauthorized` - Not authenticated

**Response Examples:**

✅ **200 OK - Flow List**
```json
[
  {
    "id": "880e8400-e29b-41d4-a716-446655440000",
    "name": "Product Survey 2026",
    "description": "Gathering user feedback on new product launch",
    "isPublished": true,
    "entryNodeId": "990e8400-e29b-41d4-a716-446655440000",
    "createdAt": "2026-03-15T10:30:00Z",
    "updatedAt": "2026-04-10T15:45:00Z",
    "stats": {
      "nodeCount": 12,
      "edgeCount": 15,
      "questionCount": 8,
      "offerNodeCount": 2,
      "infoPageCount": 2,
      "totalSessions": 1250,
      "completedSessions": 950,
      "abandonedSessions": 250,
      "inProgressSessions": 50,
      "completionRate": 76.0,
      "abandonRate": 20.0,
      "lastSessionAt": "2026-04-19T11:20:00Z",
      "totalOfferImpressions": 1200,
      "totalOfferConversions": 180,
      "offerConversionRate": 15.0,
      "avgSessionDuration": "00:04:30",
      "medianSessionDuration": "00:03:45",
      "minSessionDuration": "00:01:10",
      "maxSessionDuration": "00:15:20",
      "avgAnswerDuration": "00:00:45",
      "medianAnswerDuration": "00:00:40",
      "minAnswerDuration": "00:00:05",
      "maxAnswerDuration": "00:02:30"
    }
  }
]
```

---

#### GET /api/admin/flows/{id}
- **Summary:** Get flow with full DAG
- **Description:** Returns complete flow details including all nodes, edges, and offers with their full structure.
- **Auth Required:** Yes
- **Path Parameters:**
  - `id` (GUID) - Flow ID
- **Response Codes:**
  - `200 OK` - Flow detail with full DAG
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Flow not found

---

#### POST /api/admin/flows
- **Summary:** Create flow
- **Description:** Creates a new empty flow. Name and description can be provided.
- **Auth Required:** Yes
- **Request Body:**
  ```json
  {
    "name": "New Product Survey",
    "description": "Feedback collection for Q2 product launch"
  }
  ```

**Response Examples:**

✅ **201 Created - Flow Created**
```json
{
  "id": "880e8400-e29b-41d4-a716-446655440000",
  "name": "New Product Survey",
  "description": "Feedback collection for Q2 product launch",
  "isPublished": false,
  "entryNodeId": null,
  "createdAt": "2026-04-19T12:30:00Z",
  "updatedAt": "2026-04-19T12:30:00Z",
  "stats": null
}
```

❌ **400 Bad Request - Validation Error**
```json
{
  "message": "Flow name is required and must be between 1 and 255 characters."
}
```

---

#### PUT /api/admin/flows/{id}
- **Summary:** Update flow
- **Description:** Updates flow metadata (name, description).
- **Auth Required:** Yes
- **Path Parameters:**
  - `id` (GUID) - Flow ID
- **Request Body:**
  ```json
  {
    "name": "string (optional)",
    "description": "string (optional)"
  }
  ```
- **Response Codes:**
  - `200 OK` - Updated flow summary
  - `400 Bad Request` - Validation error
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Flow not found

---

#### PUT /api/admin/flows/{id}/entry-node
- **Summary:** Set entry node
- **Description:** Designates which node is the starting point for the flow. Required before publishing.
- **Auth Required:** Yes
- **Path Parameters:**
  - `id` (GUID) - Flow ID
- **Request Body:**
  ```json
  {
    "nodeId": "uuid"
  }
  ```
- **Response Codes:**
  - `200 OK` - Updated flow summary
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Flow not found
  - `422 Unprocessable Entity` - Node does not belong to this flow

---

#### POST /api/admin/flows/{id}/publish
- **Summary:** Publish flow
- **Description:** Makes the flow publicly available to users. Flow must have a valid DAG with entry node and cannot already be published.
- **Auth Required:** Yes
- **Path Parameters:**
  - `id` (GUID) - Flow ID
- **Response Codes:**
  - `200 OK` - Published flow summary
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Flow not found
  - `409 Conflict` - Flow is already published
  - `422 Unprocessable Entity` - Flow cannot be published (missing entry node, invalid structure, etc.)

---

#### POST /api/admin/flows/{id}/unpublish
- **Summary:** Unpublish flow
- **Description:** Revokes public access to the flow. Existing sessions continue to work, but new sessions cannot start.
- **Auth Required:** Yes
- **Path Parameters:**
  - `id` (GUID) - Flow ID
- **Response Codes:**
  - `200 OK` - Unpublished flow summary
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Flow not found
  - `409 Conflict` - Flow is not currently published

---

#### DELETE /api/admin/flows/{id}
- **Summary:** Delete flow
- **Description:** Permanently deletes a flow and all associated data (nodes, edges, offers, leads, sessions).
- **Auth Required:** Yes
- **Path Parameters:**
  - `id` (GUID) - Flow ID
- **Response Codes:**
  - `204 No Content` - Flow deleted successfully
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Flow not found

---

## 5. Admin — Nodes Controller

**Route Prefix:** `/api/admin/flows/{flowId}/nodes`  
**Access:** Admin only (authentication required)  
**Purpose:** Manages nodes (questions, statements, terminal nodes) within a flow. Nodes are the vertices of the DAG.

### Endpoints

#### POST /api/admin/flows/{flowId}/nodes
- **Summary:** Create node
- **Description:** Creates a new node within a flow. Node type determines its behavior (question, statement, terminal, etc.).
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Parent flow ID
- **Request Body:**
  ```json
  {
    "nodeType": "Question",
    "title": "How satisfied are you with our service?",
    "description": "Rate your satisfaction level",
    "positionX": 200.5,
    "positionY": 150.3
  }
  ```

**Response Examples:**

✅ **201 Created - Node Created**
```json
{
  "id": "990e8400-e29b-41d4-a716-446655440000",
  "flowId": "880e8400-e29b-41d4-a716-446655440000",
  "type": "Question",
  "attributeKey": null,
  "title": "How satisfied are you with our service?",
  "description": "Rate your satisfaction level",
  "mediaUrl": null,
  "positionX": 200.5,
  "positionY": 150.3,
  "createdAt": "2026-04-19T12:30:00Z",
  "answerType": null,
  "sliderMin": null,
  "sliderMax": null,
  "linkedOffer": null
}
```

❌ **404 Not Found - Flow Not Found**
```json
{
  "message": "Flow not found."
}
```

---

#### PUT /api/admin/flows/{flowId}/nodes/{nodeId}
- **Summary:** Update node
- **Description:** Updates node content (title, description, node-specific properties).
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Parent flow ID
  - `nodeId` (GUID) - Node ID
- **Request Body:**
  ```json
  {
    "title": "string (optional)",
    "content": "string (optional)",
    "nodeType": "enum (optional)"
  }
  ```
- **Response Codes:**
  - `200 OK` - Updated node detail
  - `400 Bad Request` - Validation error
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Node or flow not found
  - `422 Unprocessable Entity` - Unprocessable entity

---

#### PUT /api/admin/flows/{flowId}/nodes/{nodeId}/position
- **Summary:** Move node
- **Description:** Updates the position of a node on the canvas (for visualization purposes).
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Parent flow ID
  - `nodeId` (GUID) - Node ID
- **Request Body:**
  ```json
  {
    "positionX": "number",
    "positionY": "number"
  }
  ```
- **Response Codes:**
  - `200 OK` - Updated node position
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Node or flow not found

---

#### DELETE /api/admin/flows/{flowId}/nodes/{nodeId}
- **Summary:** Delete node
- **Description:** Permanently deletes a node and all associated edges, options, and offers.
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Parent flow ID
  - `nodeId` (GUID) - Node ID
- **Response Codes:**
  - `204 No Content` - Node deleted successfully
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Node or flow not found

---

## 6. Admin — Edges Controller

**Route Prefix:** `/api/admin/flows/{flowId}/edges`  
**Access:** Admin only (authentication required)  
**Purpose:** Manages edges (connections) between nodes. Edges define the flow routing with optional conditions and priorities.

### Endpoints

#### POST /api/admin/flows/{flowId}/edges
- **Summary:** Create edge
- **Description:** Creates a new directed edge (connection) between two nodes. Both nodes must exist in the flow. Source and target must be different.
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Parent flow ID
- **Request Body:**
  ```json
  {
    "sourceNodeId": "990e8400-e29b-41d4-a716-446655440000",
    "targetNodeId": "ee0e8400-e29b-41d4-a716-446655440000",
    "condition": "score >= 7",
    "priority": 1
  }
  ```

**Response Examples:**

✅ **201 Created - Edge Created**
```json
{
  "id": "dd0e8400-e29b-41d4-a716-446655440000",
  "flowId": "880e8400-e29b-41d4-a716-446655440000",
  "sourceNodeId": "990e8400-e29b-41d4-a716-446655440000",
  "targetNodeId": "ee0e8400-e29b-41d4-a716-446655440000",
  "priority": 1,
  "conditionsJson": "score >= 7",
  "createdAt": "2026-04-19T12:30:00Z"
}
```

❌ **409 Conflict - Edge Already Exists**
```json
{
  "message": "An edge already exists between these two nodes."
}
```

---

#### PUT /api/admin/flows/{flowId}/edges/{edgeId}
- **Summary:** Update edge
- **Description:** Updates an edge's priority and/or conditions. Only provided fields are changed.
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Parent flow ID
  - `edgeId` (GUID) - Edge ID
- **Request Body:**
  ```json
  {
    "condition": "string (optional)",
    "priority": "number (optional)"
  }
  ```
- **Response Codes:**
  - `200 OK` - Updated edge detail
  - `400 Bad Request` - Validation error
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Edge or flow not found

---

#### DELETE /api/admin/flows/{flowId}/edges/{edgeId}
- **Summary:** Delete edge
- **Description:** Permanently deletes an edge (connection) between two nodes.
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Parent flow ID
  - `edgeId` (GUID) - Edge ID
- **Response Codes:**
  - `204 No Content` - Edge deleted successfully
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Edge or flow not found

---

## 7. Admin — Offers Controller

**Route Prefix:** `/api/admin/offers`  
**Access:** Admin only (authentication required)  
**Purpose:** Manages offers (products, services, CTAs) that can be linked to nodes. Offers are reusable across flows.

### Endpoints

#### GET /api/admin/offers
- **Summary:** List all offers
- **Description:** Returns a list of all offers owned by the authenticated user.
- **Auth Required:** Yes
- **Response Codes:**
  - `200 OK` - Offer list
  - `401 Unauthorized` - Not authenticated

---

#### GET /api/admin/offers/{id}
- **Summary:** Get offer details
- **Description:** Returns detailed information about a specific offer.
- **Auth Required:** Yes
- **Path Parameters:**
  - `id` (GUID) - Offer ID
- **Response Codes:**
  - `200 OK` - Offer detail
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Offer not found

---

#### POST /api/admin/offers
- **Summary:** Create offer
- **Description:** Creates a new offer with name, description, slug, and CTA information.
- **Auth Required:** Yes
- **Request Body:**
  ```json
  {
    "name": "Premium Plan",
    "description": "Unlock all premium features",
    "slug": "premium-plan",
    "ctaText": "Upgrade Now",
    "ctaUrl": "https://example.com/upgrade",
    "imageUrl": "https://cdn.example.com/premium.jpg"
  }
  ```

**Response Examples:**

✅ **201 Created - Offer Created**
```json
{
  "id": "cc0e8400-e29b-41d4-a716-446655440000",
  "slug": "premium-plan",
  "name": "Premium Plan",
  "headline": null,
  "body": "Unlock all premium features",
  "imageUrl": "https://cdn.example.com/premium.jpg",
  "calendarUrl": null,
  "ctaText": "Upgrade Now",
  "ctaUrl": "https://example.com/upgrade"
}
```

❌ **409 Conflict - Duplicate Slug**
```json
{
  "message": "Slug 'premium-plan' already exists. Please choose a different slug."
}
```

---

#### PUT /api/admin/offers/{id}
- **Summary:** Update offer
- **Description:** Updates offer information (name, description, CTA, etc.).
- **Auth Required:** Yes
- **Path Parameters:**
  - `id` (GUID) - Offer ID
- **Request Body:**
  ```json
  {
    "name": "string (optional)",
    "description": "string (optional)",
    "slug": "string (optional, unique)",
    "ctaText": "string (optional)",
    "ctaUrl": "string (optional)",
    "imageUrl": "string (optional)"
  }
  ```
- **Response Codes:**
  - `200 OK` - Updated offer detail
  - `400 Bad Request` - Validation error
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Offer not found
  - `409 Conflict` - Slug already exists

---

#### DELETE /api/admin/offers/{id}
- **Summary:** Delete offer
- **Description:** Permanently deletes an offer. Cannot delete if the offer is currently linked to nodes.
- **Auth Required:** Yes
- **Path Parameters:**
  - `id` (GUID) - Offer ID
- **Response Codes:**
  - `204 No Content` - Offer deleted successfully
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Offer not found
  - `409 Conflict` - Offer is linked to nodes

---

## 8. Admin — Node Offers Controller

**Route Prefix:** `/api/admin/nodes/{nodeId}/offers`  
**Access:** Admin only (authentication required)  
**Purpose:** Manages the linking/association of offers to nodes. Defines which offers appear at each node.

### Endpoints

#### GET /api/admin/nodes/{nodeId}/offers
- **Summary:** List node offers
- **Description:** Returns all offers linked to a specific node.
- **Auth Required:** Yes
- **Path Parameters:**
  - `nodeId` (GUID) - Node ID
- **Response Codes:**
  - `200 OK` - Node offer list
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Node not found

---

#### POST /api/admin/nodes/{nodeId}/offers
- **Summary:** Link offer to node
- **Description:** Links an existing offer to a node, making it available at that node in the survey flow.
- **Auth Required:** Yes
- **Path Parameters:**
  - `nodeId` (GUID) - Node ID
- **Request Body:**
  ```json
  {
    "offerId": "cc0e8400-e29b-41d4-a716-446655440000",
    "displayOrder": 0
  }
  ```

**Response Examples:**

✅ **201 Created - Offer Linked**
```json
{
  "id": "bb0e8400-e29b-41d4-a716-446655440000",
  "nodeId": "990e8400-e29b-41d4-a716-446655440000",
  "offerId": "cc0e8400-e29b-41d4-a716-446655440000",
  "isPrimary": true,
  "tier": "hot",
  "calendarProvider": null,
  "assignedOwnerId": null,
  "offerName": "Premium Plan",
  "offerSlug": "premium-plan"
}
```

❌ **409 Conflict - Offer Already Linked**
```json
{
  "message": "This offer is already linked to this node."
}
```

---

#### PUT /api/admin/nodes/{nodeId}/offers/{nodeOfferId}
- **Summary:** Update node offer
- **Description:** Updates properties of the node-offer link (display order, display settings, etc.).
- **Auth Required:** Yes
- **Path Parameters:**
  - `nodeId` (GUID) - Node ID
  - `nodeOfferId` (GUID) - NodeOffer ID
- **Request Body:**
  ```json
  {
    "displayOrder": "number (optional)"
  }
  ```
- **Response Codes:**
  - `200 OK` - Node offer updated
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - NodeOffer not found

---

#### DELETE /api/admin/nodes/{nodeId}/offers/{nodeOfferId}
- **Summary:** Unlink offer from node
- **Description:** Removes the link between an offer and a node.
- **Auth Required:** Yes
- **Path Parameters:**
  - `nodeId` (GUID) - Node ID
  - `nodeOfferId` (GUID) - NodeOffer ID
- **Response Codes:**
  - `204 No Content` - Offer unlinked successfully
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - NodeOffer not found

---

## 9. Admin — Options Controller

**Route Prefix:** `/api/admin/nodes/{nodeId}/options`  
**Access:** Admin only (authentication required)  
**Purpose:** Manages answer options for question-type nodes. Options define the choices users can select.

### Endpoints

#### POST /api/admin/nodes/{nodeId}/options
- **Summary:** Create option
- **Description:** Creates a new answer option for a question node. Only applicable to question-type nodes.
- **Auth Required:** Yes
- **Path Parameters:**
  - `nodeId` (GUID) - Node ID
- **Request Body:**
  ```json
  {
    "text": "Very Likely (9-10)",
    "displayOrder": 0
  }
  ```

**Response Examples:**

✅ **201 Created - Option Created**
```json
{
  "id": "aa0e8400-e29b-41d4-a716-446655440000",
  "nodeId": "990e8400-e29b-41d4-a716-446655440000",
  "label": "Very Likely (9-10)",
  "value": "very_likely",
  "displayOrder": 0,
  "mediaUrl": null,
  "scoreDelta": 0
}
```

❌ **422 Unprocessable Entity - Not a Question Node**
```json
{
  "message": "This node is not a question type and cannot have options."
}
```

---

#### PUT /api/admin/nodes/{nodeId}/options/{optionId}
- **Summary:** Update option
- **Description:** Updates an option's text or display order.
- **Auth Required:** Yes
- **Path Parameters:**
  - `nodeId` (GUID) - Node ID
  - `optionId` (GUID) - Option ID
- **Request Body:**
  ```json
  {
    "text": "string (optional)",
    "displayOrder": "number (optional)"
  }
  ```
- **Response Codes:**
  - `200 OK` - Updated option detail
  - `400 Bad Request` - Validation error
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Option or node not found

---

#### DELETE /api/admin/nodes/{nodeId}/options/{optionId}
- **Summary:** Delete option
- **Description:** Permanently deletes an answer option from a question node.
- **Auth Required:** Yes
- **Path Parameters:**
  - `nodeId` (GUID) - Node ID
  - `optionId` (GUID) - Option ID
- **Response Codes:**
  - `204 No Content` - Option deleted successfully
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Option or node not found

---

#### PUT /api/admin/nodes/{nodeId}/options/reorder
- **Summary:** Reorder options
- **Description:** Reorders all options for a question node at once. Provide new ordering.
- **Auth Required:** Yes
- **Path Parameters:**
  - `nodeId` (GUID) - Node ID
- **Request Body:**
  ```json
  {
    "optionIds": ["uuid", "uuid", ...]
  }
  ```
- **Response Codes:**
  - `200 OK` - Options reordered, returns updated option list
  - `400 Bad Request` - Validation error
  - `401 Unauthorized` - Not authenticated

---

## 10. Admin — Leads Controller

**Route Prefix:** `/api/admin/flows/{flowId}/leads`  
**Access:** Admin only (authentication required)  
**Purpose:** Manages leads captured by flows. Tracks user information, conversion status, and assignment.

### Endpoints

#### GET /api/admin/flows/{flowId}/leads
- **Summary:** List leads for a flow
- **Description:** Returns all leads captured by this flow. Supports filtering by tier, status, date range, and free-text search. Supports sorting by multiple fields.
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Flow ID
- **Query Parameters:**
  - `tier` (string, optional) - Filter by tier (e.g., "hot", "warm", "cold")
  - `status` (string, optional) - Filter by status (e.g., "new", "contacted", "converted")
  - `search` (string, optional) - Free-text search in lead information
  - `from` (datetime, optional) - Start date for date range filter
  - `to` (datetime, optional) - End date for date range filter
  - `sortBy` (string, optional) - Sort field (e.g., "createdAt", "tier", "status")
  - `sortDescending` (boolean, optional, default: true) - Sort order
- **Response Codes:**
  - `200 OK` - Lead list with filters applied
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Flow not found

**Response Examples:**

✅ **200 OK - Lead List**
```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "sessionId": "ff0e8400-e29b-41d4-a716-446655440000",
    "flowId": "880e8400-e29b-41d4-a716-446655440000",
    "email": "john.doe@example.com",
    "fullName": "John Doe",
    "phone": "+1-555-0123",
    "companyName": "Acme Corp",
    "jobTitle": "Product Manager",
    "companySize": "50-200",
    "website": "https://acme.example.com",
    "score": 85,
    "tier": "hot",
    "status": "new",
    "terminalNodeId": "ee0e8400-e29b-41d4-a716-446655440000",
    "terminalNodeType": "Terminal",
    "notes": "Interested in enterprise plan. Budget approved.",
    "assignedToId": "550e8400-e29b-41d4-a716-446655440000",
    "timeToCompleteSeconds": 240,
    "createdAt": "2026-04-19T10:15:00Z"
  }
]
```

---

#### GET /api/admin/flows/{flowId}/leads/{leadId}
- **Summary:** Get lead detail
- **Description:** Returns detailed information about a specific lead including all captured data and history.
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Flow ID
  - `leadId` (GUID) - Lead ID
- **Response Codes:**
  - `200 OK` - Lead detail
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Lead not found

---

#### PATCH /api/admin/flows/{flowId}/leads/{leadId}
- **Summary:** Update lead status, tier, notes, or assignment
- **Description:** Updates lead information using patch semantics. Only provided fields are updated. To clear assignment, pass UpdateAssignment=true with AssignedToId=null.
- **Auth Required:** Yes
- **Path Parameters:**
  - `flowId` (GUID) - Flow ID
  - `leadId` (GUID) - Lead ID
- **Request Body:**
  ```json
  {
    "status": "string (optional, e.g., 'new', 'contacted', 'converted')",
    "tier": "string (optional, e.g., 'hot', 'warm', 'cold')",
    "notes": "string (optional)",
    "assignedToId": "uuid (optional)",
    "updateAssignment": "boolean (optional, required to clear assignment)"
  }
  ```
- **Response Codes:**
  - `200 OK` - Updated lead detail
  - `400 Bad Request` - Validation error
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Lead not found

---

## 11. Admin — Analytics Controller

**Route Prefix:** `/api/admin/analytics`  
**Access:** Admin only (authentication required)  
**Purpose:** Provides analytics and reporting endpoints for flow performance, offer conversion, and user drop-off analysis.

### Endpoints

#### GET /api/admin/analytics/sessions
- **Summary:** Get session statistics
- **Description:** Returns overall session metrics including counts by status, completion rate, and abandon rate.
- **Auth Required:** Yes
- **Response Codes:**
  - `200 OK` - Session statistics
  - `401 Unauthorized` - Not authenticated

**Response Example:**

✅ **200 OK - Session Statistics**
```json
{
  "totalSessions": 5240,
  "inProgress": 120,
  "completed": 4200,
  "abandoned": 920,
  "completionRate": 80.15,
  "abandonRate": 17.55
}
```

---

#### GET /api/admin/analytics/offers
- **Summary:** Get offer statistics
- **Description:** Returns per-offer metrics including times presented, times converted, and conversion rate. Useful for identifying top-performing offers.
- **Auth Required:** Yes
- **Response Codes:**
  - `200 OK` - Offer statistics
  - `401 Unauthorized` - Not authenticated

**Response Example:**

✅ **200 OK - Offer Statistics**
```json
{
  "items": [
    {
      "offerId": "cc0e8400-e29b-41d4-a716-446655440000",
      "offerName": "Premium Plan",
      "offerSlug": "premium-plan",
      "flowId": "880e8400-e29b-41d4-a716-446655440000",
      "flowName": "Product Survey 2026",
      "timesPresented": 1850,
      "timesConverted": 315,
      "conversionRate": 17.03
    },
    {
      "offerId": "dd0e8400-e29b-41d4-a716-446655440000",
      "offerName": "Basic Plan",
      "offerSlug": "basic-plan",
      "flowId": "880e8400-e29b-41d4-a716-446655440000",
      "flowName": "Product Survey 2026",
      "timesPresented": 3200,
      "timesConverted": 285,
      "conversionRate": 8.91
    }
  ]
}
```

---

#### GET /api/admin/analytics/drop-offs
- **Summary:** Get drop-off analysis
- **Description:** Returns drop-off metrics per node, showing where users are getting stuck or abandoning the flow.
- **Auth Required:** Yes
- **Response Codes:**
  - `200 OK` - Drop-off analysis
  - `401 Unauthorized` - Not authenticated

**Response Example:**

✅ **200 OK - Drop-Off Analysis**
```json
{
  "items": [
    {
      "nodeId": "990e8400-e29b-41d4-a716-446655440000",
      "nodeTitle": "How likely are you to recommend?",
      "flowId": "880e8400-e29b-41d4-a716-446655440000",
      "flowTitle": "Product Survey 2026",
      "sessionCount": 450,
      "dropOffRate": 8.65
    },
    {
      "nodeId": "aa1e8400-e29b-41d4-a716-446655440000",
      "nodeTitle": "What is your job title?",
      "flowId": "880e8400-e29b-41d4-a716-446655440000",
      "flowTitle": "Product Survey 2026",
      "sessionCount": 320,
      "dropOffRate": 12.25
    },
    {
      "nodeId": "bb1e8400-e29b-41d4-a716-446655440000",
      "nodeTitle": "Payment Information",
      "flowId": "880e8400-e29b-41d4-a716-446655440000",
      "flowTitle": "Product Survey 2026",
      "sessionCount": 175,
      "dropOffRate": 35.80
    }
  ]
}
```

---

## 12. Admin — AI Generation Controller

**Route Prefix:** `/api/admin/flows/generate`  
**Access:** Admin only (authentication required)  
**Purpose:** Provides AI-assisted flow generation. Accepts natural language prompts and creates complete flow DAGs automatically.

### Endpoints

#### POST /api/admin/flows/generate
- **Summary:** Start flow generation (async)
- **Description:** Initiates an asynchronous job to generate a complete flow from a natural language prompt. Returns a job ID for polling status.
- **Auth Required:** Yes
- **Request Body:**
  ```json
  {
    "userPrompt": "Create a flow to collect newsletter signups with premium/free tiers and offer evaluation based on job title and company size"
  }
  ```

**Response Examples:**

✅ **202 Accepted - Job Accepted**
```json
{
  "jobId": "cc1e8400-e29b-41d4-a716-446655440000"
}
```

❌ **400 Bad Request - Missing Prompt**
```json
{
  "message": "UserPrompt is required."
}
```

---

#### GET /api/admin/flows/generate/status/{jobId}
- **Summary:** Poll generation job status
- **Description:** Returns the current status of a flow generation job (Pending/Running/Done/Failed). When Done, includes the created flowId.
- **Auth Required:** Yes
- **Path Parameters:**
  - `jobId` (GUID) - Generation job ID from POST response
- **Response Codes:**
  - `200 OK` - Job status
  - `401 Unauthorized` - Not authenticated
  - `404 Not Found` - Job not found

**Response Examples:**

⏳ **200 OK - Job Pending**
```json
{
  "jobId": "cc1e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "flowId": null,
  "error": null
}
```

⏳ **200 OK - Job Running**
```json
{
  "jobId": "cc1e8400-e29b-41d4-a716-446655440000",
  "status": "Running",
  "flowId": null,
  "error": null
}
```

✅ **200 OK - Job Completed**
```json
{
  "jobId": "cc1e8400-e29b-41d4-a716-446655440000",
  "status": "Done",
  "flowId": "880e8400-e29b-41d4-a716-446655440000",
  "error": null
}
```

❌ **200 OK - Job Failed**
```json
{
  "jobId": "cc1e8400-e29b-41d4-a716-446655440000",
  "status": "Failed",
  "flowId": null,
  "error": "The prompt was too vague. Please provide more specific details about the flow structure and user journey."
}
```

---

## Common Response Patterns

### Standard HTTP Status Codes

| Code | Meaning | Usage |
|------|---------|-------|
| 200 | OK | Successful GET, PUT, PATCH requests |
| 201 | Created | Successful POST requests |
| 202 | Accepted | Asynchronous job accepted |
| 204 | No Content | Successful DELETE requests |
| 400 | Bad Request | Validation errors, malformed input |
| 401 | Unauthorized | Missing or invalid authentication |
| 404 | Not Found | Resource does not exist |
| 409 | Conflict | Resource conflict (duplicate, locked state) |
| 422 | Unprocessable Entity | Business logic validation failed |
| 423 | Locked | Resource temporarily locked (e.g., account lockout) |

### Error Response Format

```json
{
  "message": "string describing the error"
}
```

---

## Authentication

All endpoints marked as "Auth Required: Yes" require:
- Valid HTTP cookie set by `/api/auth/login` or `/api/auth/signup`
- Cookie is automatically managed by the browser
- Include `credentials: 'include'` in fetch requests (CORS)

---

## Rate Limiting

- Currently no rate limiting implemented
- Consider implementing for production deployment

---

## Versioning

- Current API version: 1.0
- Base URL format allows for future versioning: `/api/v2/...`

---

## Error Handling Guide

### Common Error Response Scenarios

#### Authentication Failures
When a request lacks valid authentication or requires login:
```json
{
  "message": "Unable to identify user."
}
```
Status: `401 Unauthorized`

#### Not Found Errors
When requesting a resource that doesn't exist:
```json
{
  "message": "Flow not found."
}
```
Status: `404 Not Found`

#### Validation Errors
When request data fails validation:
```json
{
  "message": "Email is required. Password must be at least 8 characters and contain uppercase, lowercase, number, and special character."
}
```
Status: `400 Bad Request`

#### Business Logic Conflicts
When operation violates business rules:
```json
{
  "message": "Flow is already published."
}
```
Status: `409 Conflict`

#### Invalid State Transitions
When operation cannot proceed due to current state:
```json
{
  "message": "Flow cannot be published without a configured entry node."
}
```
Status: `422 Unprocessable Entity`

---

## Request/Response Patterns

### Authentication Pattern
All authenticated endpoints expect an HttpOnly cookie set by `/api/auth/login` or `/api/auth/signup`.

**Client Implementation (JavaScript):**
```javascript
// Cookie is automatically sent by browser
fetch('https://api.example.com/api/admin/flows', {
  method: 'GET',
  credentials: 'include'  // IMPORTANT: Include cookies
})
```

### Pagination Pattern
List endpoints currently do not implement pagination but return all results.

### Filtering Pattern
Some list endpoints support query parameters:
```
GET /api/admin/flows/{flowId}/leads?tier=hot&status=new&sortBy=createdAt&sortDescending=true
```

### Async Job Pattern
Long-running operations use job IDs for polling:
1. POST request returns `202 Accepted` with `jobId`
2. Poll with GET `/api/admin/flows/generate/status/{jobId}`
3. Status values: `Pending`, `Running`, `Done`, `Failed`

---

## Data Types Reference

### Common Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | UUID | Unique identifier (e.g., `550e8400-e29b-41d4-a716-446655440000`) |
| `createdAt` | ISO 8601 | UTC timestamp (e.g., `2026-04-19T12:30:00Z`) |
| `updatedAt` | ISO 8601 | UTC timestamp (e.g., `2026-04-19T15:45:00Z`) |
| `displayOrder` | Integer | 0-based ordering for options, offers, etc. |
| `priority` | Integer | Edge priority for routing decisions |

### Session Status Values
- `InProgress` - User is actively working through the flow
- `Completed` - User reached the terminal node
- `Abandoned` - Session was left incomplete

### Lead Tier Values
- `hot` - High-intent leads (score >= 75)
- `warm` - Medium-intent leads (score 50-74)
- `cold` - Low-intent leads (score < 50)

### Lead Status Values
- `new` - Newly captured lead
- `contacted` - Sales team has reached out
- `converted` - Lead completed purchase/signup
- `lost` - Disqualified or unresponsive

### Node Types
- `Question` - Collects user input with options
- `Statement` - Displays information only
- `Offer` - Presents products/services with CTA
- `LeadCapture` - Collects contact information
- `Terminal` - End point of the flow

### Answer Types
- `SingleChoice` - Multiple choice, one answer
- `MultipleChoice` - Multiple choice, multiple answers
- `OpenText` - Free text input
- `Slider` - Numeric scale selection

---

## Performance Considerations

### Response Times
- Simple endpoints (GET lists): ~100-200ms
- Complex queries (analytics): ~500-1000ms
- AI generation (async): 5-30 seconds depending on complexity

### Best Practices
1. Cache published flow responses on client (content rarely changes)
2. Poll AI generation status with exponential backoff
3. Use filtering parameters when retrieving large lead lists
4. Implement pagination client-side if needed (max 1000 items per request)

---

## Additional Resources

- **Swagger/OpenAPI:** Available at `/swagger/ui` for interactive testing
- **GitHub Repository:** [Link to repo]
- **Frontend Demo:** [Link to demo]

---

**Last Updated:** 2026-04-19  
**API Version:** 1.0  
**Contact:** [Support email or contact info]
