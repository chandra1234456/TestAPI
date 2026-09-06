package com.debugsdk.storage

import android.content.Context
import androidx.room.Dao
import androidx.room.Database
import androidx.room.Entity
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.PrimaryKey
import androidx.room.Query
import androidx.room.Room
import androidx.room.RoomDatabase
import com.debugsdk.core.DebugEvent
import java.util.UUID

@Entity(tableName = "events")
data class EventEntity(
    @PrimaryKey
    val id: String = UUID.randomUUID().toString(),
    val projectId: String,
    val sessionId: String,
    val type: String,
    val timestamp: Long,
    val payload: String,
    val uploaded: Boolean = false
)

@Dao
interface EventDao {
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    fun insert(event: EventEntity)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    fun insertAll(events: List<EventEntity>)

    @Query("SELECT * FROM events WHERE uploaded = 0 ORDER BY timestamp ASC LIMIT :limit")
    fun getPendingEvents(limit: Int = 50): List<EventEntity>

    @Query("UPDATE events SET uploaded = 1 WHERE id IN (:ids)")
    fun markUploaded(ids: List<String>)

    @Query("DELETE FROM events WHERE uploaded = 1")
    fun deleteUploadedEvents()
}

@Database(entities = [EventEntity::class], version = 1, exportSchema = false)
abstract class DebugDatabase : RoomDatabase() {
    abstract fun eventDao(): EventDao
}

class EventRepository private constructor(context: Context) {
    private val db = Room.databaseBuilder(
        context.applicationContext,
        DebugDatabase::class.java,
        "debug_sdk_events.db"
    ).allowMainThreadQueries().build() // Allowed for emergency crash logging

    fun saveEvent(projectId: String, sessionId: String, userId: String?, event: DebugEvent) {
        val payload = formatPayload(event)
        val entity = EventEntity(
            projectId = projectId,
            sessionId = sessionId,
            type = event.javaClass.simpleName,
            timestamp = event.timestamp,
            payload = payload,
            uploaded = false
        )
        db.eventDao().insert(entity)
    }

    fun saveEventSync(projectId: String, sessionId: String, userId: String?, event: DebugEvent) {
        saveEvent(projectId, sessionId, userId, event)
    }

    fun getPendingEvents(limit: Int = 50): List<EventEntity> {
        return db.eventDao().getPendingEvents(limit)
    }

    fun markUploaded(ids: List<String>) {
        db.eventDao().markUploaded(ids)
    }

    private fun formatPayload(event: DebugEvent): String {
        return when (event) {
            is DebugEvent.Log -> "{\"message\":\"${event.message}\",\"level\":\"${event.level}\"}"
            is DebugEvent.Crash -> "{\"exception\":\"${event.exception}\",\"message\":\"${event.message}\"}"
            is DebugEvent.Network -> "{\"method\":\"${event.method}\",\"url\":\"${event.url}\",\"statusCode\":${event.statusCode},\"durationMs\":${event.durationMs}}"
            is DebugEvent.Breadcrumb -> "{\"message\":\"${event.message}\"}"
            is DebugEvent.ANR -> "{\"durationMs\":${event.durationMs}}"
            is DebugEvent.Performance -> "{\"metricName\":\"${event.metricName}\",\"valueMs\":${event.valueMs}}"
            is DebugEvent.Device -> "{\"metadata\":\"${event.metadata}\"}"
        }
    }

    companion object {
        @Volatile
        private var INSTANCE: EventRepository? = null

        fun getInstance(context: Context): EventRepository {
            return INSTANCE ?: synchronized(this) {
                INSTANCE ?: EventRepository(context).also { INSTANCE = it }
            }
        }
    }
}
