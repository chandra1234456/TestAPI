package com.debugsdk.core

import android.content.Context
import com.debugsdk.crash.CrashHandler
import com.debugsdk.storage.EventRepository
import java.util.concurrent.Executors

object DebugSDK {
    private var config: DebugSDKConfig? = null
    private var appContext: Context? = null
    private var breadcrumbManager: BreadcrumbManager? = null
    private var sessionManager: SessionManager? = null
    private var deviceInfoCollector: DeviceInfoCollector? = null
    private var eventRepository: EventRepository? = null
    private val executor = Executors.newSingleThreadExecutor()

    fun initialize(context: Context, config: DebugSDKConfig) {
        val app = context.applicationContext
        this.appContext = app
        this.config = config

        this.breadcrumbManager = BreadcrumbManager(config.maxBreadcrumbs)
        this.sessionManager = SessionManager()
        this.deviceInfoCollector = DeviceInfoCollector(app)
        this.eventRepository = EventRepository.getInstance(app)

        if (config.autoCaptureCrashes) {
            CrashHandler.install(
                sessionManager = sessionManager!!,
                breadcrumbManager = breadcrumbManager!!,
                deviceInfoCollector = deviceInfoCollector!!,
                eventRepository = eventRepository!!
            )
        }

        log("DebugSDK initialized for project: ${config.projectKey}")
    }

    fun log(message: String) = logInternal(message, LogLevel.INFO)
    fun info(message: String) = logInternal(message, LogLevel.INFO)
    fun warning(message: String) = logInternal(message, LogLevel.WARNING)
    fun error(message: String) = logInternal(message, LogLevel.ERROR)

    private fun logInternal(message: String, level: LogLevel) {
        val event = DebugEvent.Log(message = message, level = level)
        recordEvent(event)
    }

    fun breadcrumb(message: String) {
        breadcrumbManager?.addBreadcrumb(message)
        val event = DebugEvent.Breadcrumb(message = message)
        recordEvent(event)
    }

    fun setUser(userId: String) {
        sessionManager?.setUser(userId)
        breadcrumb("User set: $userId")
    }

    fun setTag(key: String, value: String) {
        sessionManager?.setTag(key, value)
    }

    fun recordEvent(event: DebugEvent) {
        val repo = eventRepository ?: return
        val session = sessionManager ?: return
        val cfg = config ?: return

        executor.execute {
            repo.saveEvent(
                projectId = cfg.projectKey,
                sessionId = session.sessionId,
                userId = session.userId,
                event = event
            )
        }
    }

    internal fun getBreadcrumbManager(): BreadcrumbManager? = breadcrumbManager
    internal fun getSessionManager(): SessionManager? = sessionManager
    internal fun getDeviceInfoCollector(): DeviceInfoCollector? = deviceInfoCollector
}
