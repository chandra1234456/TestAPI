# Android Debug SDK Integration Guide

Modular, offline-first Android debugging, logging, network interception, crash reporting, and performance tracking SDK with Supabase PostgreSQL & Render backend support.

## Module Structure

```
your-debug-sdk/
├── debug-sdk-core         # Public facade (DebugSDK.initialize, log, breadcrumb, setUser)
├── debug-sdk-crash        # Thread.UncaughtExceptionHandler crash reporter
├── debug-sdk-network      # OkHttp DebugInterceptor
├── debug-sdk-performance  # ANRDetector, StartupTracker
├── debug-sdk-storage      # Room database (EventEntity, uploaded = false)
├── debug-sdk-upload       # WorkManager + BatchUploader
├── debug-sdk-security     # DataRedactor for authorization/tokens
└── debug-sdk-ui           # In-app DebugConsoleActivity
```

---

## Quick Start Guide

### 1. Initialize in `MainApplication.kt`

```kotlin
class MainApplication : Application() {
    override fun onCreate() {
        super.onCreate()

        DebugSDK.initialize(
            context = applicationContext,
            config = DebugSDKConfig(
                projectKey = "my_android_app_key",
                backendUrl = "https://your-app.onrender.com/api/events/batch",
                autoCaptureCrashes = true
            )
        )

        DebugSDK.setUser("user-123")
        DebugSDK.setTag("environment", "staging")
    }
}
```

### 2. Record Custom Logs & Breadcrumbs

```kotlin
// Logging
DebugSDK.log("User opened checkout screen")
DebugSDK.info("Payment retry initiated")
DebugSDK.warning("Network connection weak")
DebugSDK.error("Payment failed to process")

// Breadcrumbs (Recorded before crashes)
DebugSDK.breadcrumb("Clicked 'Pay Now' button")
DebugSDK.breadcrumb("Payment gateway response 200 OK")
```

### 3. Attach Network Interceptor (OkHttp / Retrofit)

```kotlin
val okHttpClient = OkHttpClient.Builder()
    .addInterceptor(DebugInterceptor()) // Automatically redacts passwords & authorization headers!
    .build()

val retrofit = Retrofit.Builder()
    .baseUrl("https://api.myapp.com/")
    .client(okHttpClient)
    .build()
```

### 4. Background Upload & Supabase Integration

Events are stored locally in Room DB with `uploaded = false`. Android `WorkManager` uploads pending events in batches to your Render Web Service (`POST /api/events/batch`), which stores them in your Supabase PostgreSQL database.
