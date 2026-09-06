# DebugSDK & Telemetry Backend API

A complete, production-ready **Android Debug SDK**, **.NET 8 Web API Backend** connected to **Supabase PostgreSQL**, deployed on **Render**, featuring interactive **Swagger API Documentation**.

---

## 🚀 Environment & Supabase Configuration

### Connection String Format
Configure `appsettings.json` or set the `DATABASE_URL` environment variable on Render:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.cxxugsvxwkmlvcegkhkg.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_SUPABASE_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

---

## 📡 API Endpoints Reference & Sample Requests/Responses

### 1. Ingest Event Batch
- **Endpoint**: `POST /api/events/batch`
- **Description**: Receives a batch of events (Logs, Crashes, Network calls, Breadcrumbs, ANRs) from the Android SDK's background `WorkManager`. Automatically writes to Supabase PostgreSQL and updates session metrics.

#### 📩 Sample Request Body:
```json
{
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
      "type": "Breadcrumb",
      "timestamp": 1757164802000,
      "message": "User tapped 'Pay with Credit Card'"
    },
    {
      "id": "evt_003",
      "type": "Network",
      "timestamp": 1757164805000,
      "method": "POST",
      "url": "https://api.myapp.com/v1/payments",
      "statusCode": 200,
      "durationMs": 340
    },
    {
      "id": "evt_004",
      "type": "Crash",
      "timestamp": 1757164808000,
      "exception": "NullPointerException",
      "message": "Attempt to invoke virtual method 'String com.myapp.PaymentResult.getTxId()' on a null object reference",
      "stackTrace": "at com.myapp.checkout.CheckoutActivity.onPaymentSuccess(CheckoutActivity.kt:142)\nat com.myapp.checkout.CheckoutActivity.access$onPaymentSuccess(CheckoutActivity.kt:28)\nat com.myapp.checkout.CheckoutActivity$1.onResponse(CheckoutActivity.kt:98)",
      "breadcrumbs": [
        "User opened app",
        "User navigated to Checkout Screen",
        "User tapped 'Pay with Credit Card'",
        "POST https://api.myapp.com/v1/payments 200 OK"
      ],
      "deviceInfo": {
        "manufacturer": "Google",
        "model": "Pixel 7 Pro",
        "androidVersion": "14",
        "apiLevel": "34",
        "appPackage": "com.myapp.checkout",
        "appVersion": "1.4.2-debug"
      }
    }
  ]
}
```

#### 📤 Sample 200 OK Response:
```json
{
  "success": true,
  "processedCount": 4,
  "sessionId": "session_a8f93k1b9",
  "timestamp": "2026-09-06T13:17:00.123Z"
}
```

---

### 2. Get Telemetry Analytics Summary
- **Endpoint**: `GET /api/events/summary`
- **Description**: Returns aggregated metrics for the Web Monitoring Dashboard.

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
      "exceptionMessage": "Attempt to invoke virtual method 'String com.myapp.PaymentResult.getTxId()' on a null object reference",
      "stackTrace": "at com.myapp.checkout.CheckoutActivity.onPaymentSuccess(CheckoutActivity.kt:142)...",
      "breadcrumbsJson": "[\"User opened app\",\"User navigated to Checkout Screen\",\"User tapped 'Pay with Credit Card'\"]",
      "deviceInfoJson": "{\"model\":\"Pixel 7 Pro\",\"androidVersion\":\"14\"}",
      "timestamp": 1757164808000,
      "createdUtc": "2026-09-06T13:17:00Z"
    }
  ]
}
```

---

### 3. Query Paginated Events
- **Endpoint**: `GET /api/events?type=Crash&page=1&pageSize=10`
- **Description**: Fetches events filtered by type (`Log`, `Crash`, `Network`, `Breadcrumb`, `ANR`), `projectId`, or `sessionId`.

#### 📤 Sample 200 OK Response:
```json
{
  "totalCount": 3,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1,
  "items": [
    {
      "id": "evt_004",
      "projectId": "android_checkout_app",
      "sessionId": "session_a8f93k1b9",
      "type": "Crash",
      "timestamp": 1757164808000,
      "payload": "{\"id\":\"evt_004\",\"type\":\"Crash\",\"exception\":\"NullPointerException\",\"message\":\"Attempt to invoke virtual method...\"}",
      "deviceInfo": "{\"model\":\"Pixel 7 Pro\"}",
      "userId": "user_84920",
      "createdUtc": "2026-09-06T13:17:00Z"
    }
  ]
}
```

---

### 4. Fetch Detailed Crash Reports
- **Endpoint**: `GET /api/events/crashes?page=1&pageSize=20`
- **Description**: Returns crash logs with full stack traces, preceding breadcrumb timelines, and device properties.

#### 📤 Sample 200 OK Response:
```json
{
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "items": [
    {
      "id": "c7a2b910-3841-4e89-9182-12009ab1848b",
      "projectId": "android_checkout_app",
      "sessionId": "session_a8f93k1b9",
      "exceptionName": "NullPointerException",
      "exceptionMessage": "Attempt to invoke virtual method 'String com.myapp.PaymentResult.getTxId()' on a null object reference",
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

### 5. Fetch OkHttp Network Interceptor Logs
- **Endpoint**: `GET /api/events/network`
- **Description**: Lists HTTP request durations, methods, status codes, and network error messages.

#### 📤 Sample 200 OK Response:
```json
{
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
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

### 6. Fetch Active Device Sessions
- **Endpoint**: `GET /api/events/sessions`
- **Description**: Lists app launch sessions with event counts and health/crash status.

#### 📤 Sample 200 OK Response:
```json
{
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "items": [
    {
      "sessionId": "session_a8f93k1b9",
      "projectId": "android_checkout_app",
      "userId": "user_84920",
      "deviceModel": "Google Pixel 7 Pro",
      "appVersion": "1.4.2-debug",
      "startTimeUtc": "2026-09-06T13:15:00Z",
      "lastActivityUtc": "2026-09-06T13:17:00Z",
      "eventCount": 4,
      "hasCrash": true
    }
  ]
}
```

---

### 7. Clear All Events (Test Endpoint)
- **Endpoint**: `POST /api/events/clear`
- **Description**: Clears stored telemetry events from the Supabase PostgreSQL database for demo reset.

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
