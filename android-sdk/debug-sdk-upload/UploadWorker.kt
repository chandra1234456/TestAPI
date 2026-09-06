package com.debugsdk.upload

import android.content.Context
import androidx.work.Constraints
import androidx.work.CoroutineWorker
import androidx.work.NetworkType
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.WorkerParameters
import com.debugsdk.storage.EventRepository
import java.io.OutputStreamWriter
import java.net.HttpURLConnection
import java.net.URL
import java.util.concurrent.TimeUnit

class UploadWorker(
    context: Context,
    params: WorkerParameters
) : CoroutineWorker(context, params) {

    override async suspend fun doWork(): Result {
        val repository = EventRepository.getInstance(applicationContext)
        val pendingEvents = repository.getPendingEvents(limit = 100)

        if (pendingEvents.isEmpty()) {
            return Result.success()
        }

        val success = BatchUploader.uploadBatch(
            backendUrl = "https://your-render-app.onrender.com/api/events/batch",
            events = pendingEvents
        )

        return if (success) {
            val uploadedIds = pendingEvents.map { it.id }
            repository.markUploaded(uploadedIds)
            Result.success()
        } else {
            Result.retry()
        }
    }

    companion object {
        fun schedulePeriodicUpload(context: Context, intervalMinutes: Long = 15) {
            val constraints = Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .build()

            val uploadRequest = PeriodicWorkRequestBuilder<UploadWorker>(intervalMinutes, TimeUnit.MINUTES)
                .setConstraints(constraints)
                .build()

            WorkManager.getInstance(context).enqueueUniquePeriodicWork(
                "DebugSDKUploadWork",
                androidx.work.ExistingPeriodicWorkPolicy.KEEP,
                uploadRequest
            )
        }
    }
}

object BatchUploader {
    fun uploadBatch(backendUrl: String, events: List<com.debugsdk.storage.EventEntity>): Boolean {
        var connection: HttpURLConnection? = null
        try {
            val url = URL(backendUrl)
            connection = url.openConnection() as HttpURLConnection
            connection.requestMethod = "POST"
            connection.setRequestProperty("Content-Type", "application/json")
            connection.doOutput = true
            connection.connectTimeout = 10000
            connection.readTimeout = 10000

            val jsonBody = buildJsonPayload(events)
            val writer = OutputStreamWriter(connection.outputStream)
            writer.write(jsonBody)
            writer.flush()
            writer.close()

            val responseCode = connection.responseCode
            return responseCode in 200..299
        } catch (e: Exception) {
            e.printStackTrace()
            return false
        } finally {
            connection?.disconnect()
        }
    }

    private fun buildJsonPayload(events: List<com.debugsdk.storage.EventEntity>): String {
        val first = events.firstOrNull()
        val projectId = first?.projectId ?: "default_project"
        val sessionId = first?.sessionId ?: "unknown_session"

        val eventsJsonArray = events.joinToString(",") { e ->
            "{\"id\":\"${e.id}\",\"type\":\"${e.type}\",\"timestamp\":${e.timestamp},\"payload\":${e.payload}}"
        }

        return "{\"projectId\":\"$projectId\",\"sessionId\":\"$sessionId\",\"events\":[$eventsJsonArray]}"
    }
}
