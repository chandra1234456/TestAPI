package com.debugsdk.crash

import com.debugsdk.core.BreadcrumbManager
import com.debugsdk.core.DebugEvent
import com.debugsdk.core.DeviceInfoCollector
import com.debugsdk.core.SessionManager
import com.debugsdk.storage.EventRepository
import java.io.PrintWriter
import java.io.StringWriter

class CrashHandler(
    private val defaultHandler: Thread.UncaughtExceptionHandler?,
    private val sessionManager: SessionManager,
    private val breadcrumbManager: BreadcrumbManager,
    private val deviceInfoCollector: DeviceInfoCollector,
    private val eventRepository: EventRepository
) : Thread.UncaughtExceptionHandler {

    override fun uncaughtException(thread: Thread, throwable: Throwable) {
        try {
            val stringWriter = StringWriter()
            throwable.printStackTrace(PrintWriter(stringWriter))
            val stackTrace = stringWriter.toString()

            val crashEvent = DebugEvent.Crash(
                exception = throwable.javaClass.name,
                message = throwable.localizedMessage ?: "No message",
                stackTrace = stackTrace,
                breadcrumbs = breadcrumbManager.getBreadcrumbs(),
                deviceInfo = deviceInfoCollector.getDeviceInfo()
            )

            // Save crash synchronously to local storage before app process terminates
            eventRepository.saveEventSync(
                projectId = "default_project",
                sessionId = sessionManager.sessionId,
                userId = sessionManager.userId,
                event = crashEvent
            )
        } catch (e: Exception) {
            e.printStackTrace()
        } finally {
            // Forward exception to default system uncaught exception handler
            defaultHandler?.uncaughtException(thread, throwable)
        }
    }

    companion object {
        fun install(
            sessionManager: SessionManager,
            breadcrumbManager: BreadcrumbManager,
            deviceInfoCollector: DeviceInfoCollector,
            eventRepository: EventRepository
        ) {
            val defaultHandler = Thread.getDefaultUncaughtExceptionHandler()
            val customHandler = CrashHandler(
                defaultHandler,
                sessionManager,
                breadcrumbManager,
                deviceInfoCollector,
                eventRepository
            )
            Thread.setDefaultUncaughtExceptionHandler(customHandler)
        }
    }
}
