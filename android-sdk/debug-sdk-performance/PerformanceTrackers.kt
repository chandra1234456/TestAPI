package com.debugsdk.performance

import android.os.Handler
import android.os.Looper
import com.debugsdk.core.DebugEvent
import com.debugsdk.core.DebugSDK

class ANRDetector(
    private val thresholdMs: Long = 5000L,
    private val checkIntervalMs: Long = 2000L
) : Thread("ANR-Watchdog-Thread") {

    @Volatile
    private var isTick = false
    private val mainHandler = Handler(Looper.getMainLooper())
    private val ticker = Runnable { isTick = true }

    override fun run() {
        while (!isInterrupted) {
            isTick = false
            mainHandler.post(ticker)

            try {
                sleep(thresholdMs)
            } catch (e: InterruptedException) {
                return
            }

            if (!isTick) {
                // Main thread blocked for > thresholdMs -> ANR
                val mainThread = Looper.getMainLooper().thread
                val stackTrace = mainThread.stackTrace.joinToString("\n") { "  at ${it.className}.${it.methodName}(${it.fileName}:${it.lineNumber})" }

                val anrEvent = DebugEvent.ANR(
                    threadDump = stackTrace,
                    durationMs = thresholdMs
                )
                DebugSDK.recordEvent(anrEvent)
            }
        }
    }
}

object StartupTracker {
    private var appStartTime: Long = 0

    fun onAppStart() {
        appStartTime = System.currentTimeMillis()
    }

    fun onFirstActivityReady() {
        if (appStartTime > 0) {
            val duration = System.currentTimeMillis() - appStartTime
            val perfEvent = DebugEvent.Performance(
                metricName = "AppStartupTime",
                valueMs = duration
            )
            DebugSDK.recordEvent(perfEvent)
        }
    }
}
