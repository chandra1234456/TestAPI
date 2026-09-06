package com.debugsdk.core

import java.util.concurrent.ConcurrentLinkedQueue

class BreadcrumbManager(private val maxCapacity: Int = 50) {
    private val buffer = ConcurrentLinkedQueue<String>()

    fun addBreadcrumb(message: String) {
        if (message.isBlank()) return
        val timestamped = "[${System.currentTimeMillis()}] $message"
        buffer.add(timestamped)
        while (buffer.size > maxCapacity) {
            buffer.poll()
        }
    }

    fun getBreadcrumbs(): List<String> {
        return buffer.toList()
    }

    fun clear() {
        buffer.clear()
    }
}
