package com.debugsdk.core

import java.util.UUID

class SessionManager {
    val sessionId: String = "session_${UUID.randomUUID().toString().take(12)}"
    var userId: String? = null
        private set
    private val tags = mutableMapOf<String, String>()

    fun setUser(id: String) {
        this.userId = id
    }

    fun setTag(key: String, value: String) {
        tags[key] = value
    }

    fun getTags(): Map<String, String> = tags.toMap()
}
