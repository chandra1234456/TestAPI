package com.debugsdk.ui

import android.os.Bundle
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import com.debugsdk.core.DebugSDK

class DebugConsoleActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        
        val textView = TextView(this).apply {
            textSize = 14f
            setPadding(32, 32, 32, 32)
            text = buildString {
                append("=== DebugSDK In-App Console ===\n\n")
                append("Session ID: ${DebugSDK.getSessionManager()?.sessionId ?: "N/A"}\n")
                append("User ID: ${DebugSDK.getSessionManager()?.userId ?: "Anonymous"}\n\n")
                append("--- Recent Breadcrumbs ---\n")
                DebugSDK.getBreadcrumbManager()?.getBreadcrumbs()?.forEach { bc ->
                    append("• $bc\n")
                }
            }
        }

        setContentView(textView)
    }
}
