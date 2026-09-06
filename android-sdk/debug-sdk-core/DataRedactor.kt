package com.debugsdk.core

class DataRedactor {
    private val sensitiveKeys = setOf(
        "authorization", "cookie", "set-cookie", "password", 
        "token", "access_token", "refresh_token", "secret", "api_key"
    )

    fun redactHeaders(headers: Map<String, String>): Map<String, String> {
        return headers.mapValues { (key, value) ->
            if (sensitiveKeys.contains(key.lowercase())) "[REDACTED]" else value
        }
    }

    fun redactText(text: String?): String? {
        if (text == null) return null
        var redacted = text
        sensitiveKeys.forEach { key ->
            val regex = Regex("(?i)(\"$key\"\\s*:\\s*\")[^\"]+(\")")
            redacted = redacted?.replace(regex, "$1[REDACTED]$2")
        }
        return redacted
    }
}
