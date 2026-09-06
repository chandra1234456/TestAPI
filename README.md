# DebugSDK & Telemetry Backend API

A complete, production-ready **Android Debug SDK**, **.NET 8 Web API Backend** connected to **Supabase PostgreSQL**, deployed on **Render**, featuring interactive **Swagger API Documentation**.

---

## 🚀 Environment & Supabase Configuration

### Connection String Format
Configure `appsettings.json` or set the `DATABASE_URL` environment variable on Render (using the IPv4/IPv6 Dual-Stack Supabase Connection Pooler):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.cxxugsvxwkmlvcegkhkg;Password=YOUR_SUPABASE_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

> **Note for Render Deployment**: Setting `DATABASE_URL` in environment variables automatically takes precedence and supports both standard Key-Value connection strings and `postgres://user:pass@host:port/db` URI strings.

---

## 📡 Events Controller API Reference

The `EventsController` (`/api/events`) manages telemetry event ingestion, crash reports, network tracking, session analytics, and data maintenance.

### 📋 Endpoints Overview

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | [`/api/events/batch`](#1-ingest-event-batch-post-apieventsbatch) | Batch ingest telemetry events (Logs, Crashes, Networks, Breadcrumbs) |
| `GET` | [`/api/events`](#2-query-paginated-events-get-apievents) | Query paginated raw events with optional `type`, `projectId`, `sessionId` filters |
| `GET` | [`/api/events/summary`](#3-get-telemetry-analytics-summary-get-apieventssummary) | Analytics metrics summary, log breakdowns, network latency, and recent crashes |
| `GET` | [`/api/events/crashes`](#4-fetch-detailed-crash-reports-get-apieventscrashes) | Detailed crash logs with stack traces, breadcrumb timelines, and device properties |
| `GET` | [`/api/events/network`](#5-fetch-network-interceptor-logs-get-apieventsnetwork) | Network activity logs (HTTP methods, URLs, status codes, latency, errors) |
| `GET` | [`/api/events/sessions`](#6-fetch-active-device-sessions-get-apieventssessions) | Active device sessions tracking last activity, event counts, and crash flags |
| `POST` | [`/api/events/clear`](#7-clear-all-events-post-apieventsclear) | Purge all stored SDK events and sessions (Demo / Reset) |

---

### 1. Ingest Event Batch (`POST /api/events/batch`)
Receives a batch of telemetry events from the Android SDK's `WorkManager` background sync worker. Automatically indexes logs, crashes, and network calls, and updates active device sessions in Supabase PostgreSQL.

- **Headers**: `Content-Type: application/json`

#### 💻 cURL Example:
```bash
curl -X POST "https://testapi-t5ie.onrender.com/api/events/batch" \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": "android_checkout_app",
    "sessionId": "session_a8f93k1b9",
    "userId": "user_84920",
    "deviceModel": "Google Pixel 7 Pro",
    "appVersion": "1.4.2-debug",
    "events": [
      {
        "id": "evt_001",
        "type": "Log",
        "timestamp": 1757164800000,
        "message": "User navigated to Checkout Screen",
        "level": "INFO"
      },
      {
        "id": "evt_002",
        "type": "Network",
        "timestamp": 1757164805000,
        "method": "POST",
        "url": "https://api.myapp.com/v1/payments",
        "statusCode": 200,
        "durationMs": 340
      },
      {
        "id": "evt_003",
        "type": "Crash",
        "timestamp": 1757164808000,
        "exception": "NullPointerException",
        "message": "Attempt to invoke virtual method on null object reference",
        "stackTrace": "at com.myapp.checkout.CheckoutActivity.onPaymentSuccess(CheckoutActivity.kt:142)",
        "breadcrumbs": ["User opened app", "User navigated to Checkout Screen"],
        "deviceInfo": { "manufacturer": "Google", "model": "Pixel 7 Pro", "androidVersion": "14" }
      }
    ]
  }'
```

#### 📤 Sample 200 OK Response:
```json
{
  "success": true,
  "processedCount": 3,
  "sessionId": "session_a8f93k1b9",
  "timestamp": "2026-09-06T13:17:00.123Z"
}
```

---

### 2. Query Paginated Events (`GET /api/events`)
Fetches paginated telemetry events stored in Supabase. Supports optional filtering by `type`, `projectId`, or `sessionId`.

#### 📌 Query Parameters:
| Parameter | Type | Required | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| `type` | `string` | No | `null` | Filter by event type (`Log`, `Crash`, `Network`, `Breadcrumb`, `ANR`) |
| `projectId` | `string` | No | `null` | Filter by project key |
| `sessionId` | `string` | No | `null` | Filter by specific session ID |
| `page` | `integer` | No | `1` | Page number for pagination |
| `pageSize` | `integer` | No | `50` | Number of items per page |

#### 💻 cURL Example:
```bash
curl -X GET "https://testapi-t5ie.onrender.com/api/events?type=Crash&page=1&pageSize=10"
```

#### 📤 Sample 200 OK Response:
```json
{
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1,
  "items": [
    {
      "id": "evt_003",
      "projectId": "android_checkout_app",
      "sessionId": "session_a8f93k1b9",
      "type": "Crash",
      "timestamp": 1757164808000,
      "payload": "{\"id\":\"evt_003\",\"type\":\"Crash\",\"exception\":\"NullPointerException\",\"message\":\"Attempt to invoke virtual method...\"}",
      "deviceInfo": "{\"manufacturer\":\"Google\",\"model\":\"Pixel 7 Pro\"}",
      "userId": "user_84920",
      "createdUtc": "2026-09-06T13:17:00Z"
    }
  ]
}
```

---

### 3. Get Telemetry Analytics Summary (`GET /api/events/summary`)
Returns high-level system telemetry metrics, error rates, log level counts, event breakdowns, and recent crashes.

#### 💻 cURL Example:
```bash
curl -X GET "https://testapi-t5ie.onrender.com/api/events/summary"
```

#### 📤 Sample 200 OK Response:
```json
{
  "totalEvents": 142,
  "totalSessions": 18,
  "totalCrashes": 3,
  "totalLogs": 98,
  "totalNetworkRequests": 41,
  "networkErrors": 2,
  "averageLatencyMs": 312.45,
  "logLevelBreakdown": {
    "INFO": 70,
    "WARNING": 20,
    "ERROR": 8
  },
  "eventTypeBreakdown": {
    "Log": 98,
    "Crash": 3,
    "Network": 41
  },
  "recentCrashes": [
    {
      "id": "c7a2b910-3841-4e89-9182-12009ab1848b",
      "projectId": "android_checkout_app",
      "sessionId": "session_a8f93k1b9",
      "exceptionName": "NullPointerException",
      "exceptionMessage": "Attempt to invoke virtual method on null object reference",
      "stackTrace": "at com.myapp.checkout.CheckoutActivity.onPaymentSuccess(CheckoutActivity.kt:142)",
      "breadcrumbsJson": "[\"User opened app\",\"User navigated to Checkout Screen\"]",
      "deviceInfoJson": "{\"model\":\"Pixel 7 Pro\",\"androidVersion\":\"14\"}",
      "timestamp": 1757164808000,
      "createdUtc": "2026-09-06T13:17:00Z"
    }
  ]
}
```

---

### 4. Fetch Detailed Crash Reports (`GET /api/events/crashes`)
Retrieves structured crash records with complete exception names, stack traces, breadcrumb trail JSON arrays, and device metadata.

#### 📌 Query Parameters:
| Parameter | Type | Required | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| `page` | `integer` | No | `1` | Page number |
| `pageSize` | `integer` | No | `20` | Items per page |

#### 💻 cURL Example:
```bash
curl -X GET "https://testapi-t5ie.onrender.com/api/events/crashes?page=1&pageSize=10"
```

#### 📤 Sample 200 OK Response:
```json
{
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "items": [
    {
      "id": "c7a2b910-3841-4e89-9182-12009ab1848b",
      "projectId": "android_checkout_app",
      "sessionId": "session_a8f93k1b9",
      "exceptionName": "NullPointerException",
      "exceptionMessage": "Attempt to invoke virtual method on null object reference",
      "stackTrace": "at com.myapp.checkout.CheckoutActivity.onPaymentSuccess(CheckoutActivity.kt:142)",
      "breadcrumbsJson": "[\"User opened app\",\"User navigated to Checkout Screen\"]",
      "deviceInfoJson": "{\"manufacturer\":\"Google\",\"model\":\"Pixel 7 Pro\"}",
      "timestamp": 1757164808000,
      "createdUtc": "2026-09-06T13:17:00Z"
    }
  ]
}
```

---

### 5. Fetch Network Interceptor Logs (`GET /api/events/network`)
Lists HTTP request execution logs captured by the Android OkHttp `DebugInterceptor` or backend request middleware.

#### 📌 Query Parameters:
| Parameter | Type | Required | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| `page` | `integer` | No | `1` | Page number |
| `pageSize` | `integer` | No | `20` | Items per page |

#### 💻 cURL Example:
```bash
curl -X GET "https://testapi-t5ie.onrender.com/api/events/network?page=1&pageSize=10"
```

#### 📤 Sample 200 OK Response:
```json
{
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "items": [
    {
      "id": "net_918204",
      "projectId": "android_checkout_app",
      "sessionId": "session_a8f93k1b9",
      "method": "POST",
      "url": "https://api.myapp.com/v1/payments",
      "statusCode": 200,
      "durationMs": 340,
      "errorMessage": null,
      "timestamp": 1757164805000,
      "createdUtc": "2026-09-06T13:17:00Z"
    }
  ]
}
```

---

### 6. Fetch Active Device Sessions (`GET /api/events/sessions`)
Lists telemetry sessions initiated by mobile devices, including start time, last activity, total event count, and crash presence.

#### 📌 Query Parameters:
| Parameter | Type | Required | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| `page` | `integer` | No | `1` | Page number |
| `pageSize` | `integer` | No | `20` | Items per page |

#### 💻 cURL Example:
```bash
curl -X GET "https://testapi-t5ie.onrender.com/api/events/sessions?page=1&pageSize=10"
```

#### 📤 Sample 200 OK Response:
```json
{
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "items": [
    {
      "sessionId": "session_a8f93k1b9",
      "projectId": "android_checkout_app",
      "userId": "user_84920",
      "deviceModel": "Google Pixel 7 Pro",
      "appVersion": "1.4.2-debug",
      "startTimeUtc": "2026-09-06T13:15:00Z",
      "lastActivityUtc": "2026-09-06T13:17:00Z",
      "eventCount": 3,
      "hasCrash": true
    }
  ]
}
```

---

### 7. Clear All Events (`POST /api/events/clear`)
Test endpoint to purge stored telemetry events, crash logs, network records, and sessions from the Supabase database.

- **Headers**: `Content-Type: application/json`

#### 💻 cURL Example:
```bash
curl -X POST "https://testapi-t5ie.onrender.com/api/events/clear"
```

#### 📤 Sample 200 OK Response:
```json
{
  "success": true,
  "message": "All Debug SDK events cleared successfully."
}
```

---

## 📱 Android SDK Quick Setup

### 1. Initialize SDK
```kotlin
DebugSDK.initialize(
    context = applicationContext,
    config = DebugSDKConfig(
        projectKey = "android_checkout_app",
        backendUrl = "https://your-app.onrender.com/api/events/batch"
    )
)
```

### 2. Log & Capture Breadcrumbs
```kotlin
DebugSDK.log("User opened screen")
DebugSDK.breadcrumb("Clicked Checkout Button")
```

### 3. Add OkHttp Interceptor
```kotlin
val okHttpClient = OkHttpClient.Builder()
    .addInterceptor(DebugInterceptor()) // Automatically redacts Authorization/Token headers
    .build()
```

---

## 📖 Swagger API Documentation

The interactive Swagger API Documentation is hosted directly at the root URL of your Render service:
`https://testapi-t5ie.onrender.com/`

Includes:
- **Interactive OpenAPI Explorer**: Test all `.NET 8 Web API` endpoints directly from your browser.
- **Telemetry & Event Logs API**: `/api/events`, `/api/logs`
- **Quote & Weather Endpoints**: `/api/quotes`, `/home/api`
