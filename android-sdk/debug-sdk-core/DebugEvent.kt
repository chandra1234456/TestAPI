package com.debugsdk.core

enum class LogLevel {
    DEBUG,
    INFO,
    WARNING,
    ERROR
}

data class DebugSDKConfig(
    val projectKey: String,
    val backendUrl: String = "https://your-render-app.onrender.com/api/events/batch",
    val uploadBatchSize: Int = 50,
    val maxBreadcrumbs: Int = 50,
    val autoCaptureCrashes: Boolean = true
)

sealed class DebugEvent {
    abstract val timestamp: Long

    data class Log(
        val message: String,
        val level: LogLevel = LogLevel.INFO,
        val tag: String = "App",
        override val timestamp: Long = System.currentTimeMillis()
    ) : DebugEvent()

    data class Crash(
        val exception: String,
        val message: String,
        val stackTrace: String,
        val breadcrumbs: List<String>,
        val deviceInfo: Map<String, String>,
        override val timestamp: Long = System.currentTimeMillis()
    ) : DebugEvent()

    data class Network(
        val method: String,
        val url: String,
        val statusCode: Int?,
        val durationMs: Long,
        val error: String? = null,
        override val timestamp: Long = System.currentTimeMillis()
    ) : DebugEvent()

    data class Breadcrumb(
        val message: String,
        val category: String = "default",
        override val timestamp: Long = System.currentTimeMillis()
    ) : DebugEvent()

    data class ANR(
        val threadDump: String,
        val durationMs: Long,
        override val timestamp: Long = System.currentTimeMillis()
    ) : DebugEvent()

    data class Performance(
        val metricName: String,
        val valueMs: Long,
        override val timestamp: Long = System.currentTimeMillis()
    ) : DebugEvent()

    data class Device(
        val metadata: Map<String, String>,
        override val timestamp: Long = System.currentTimeMillis()
    ) : DebugEvent()
}
