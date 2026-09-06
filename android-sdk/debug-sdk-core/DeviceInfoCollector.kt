package com.debugsdk.core

import android.content.Context
import android.os.Build

class DeviceInfoCollector(private val context: Context) {
    fun getDeviceInfo(): Map<String, String> {
        val packageManager = context.packageManager
        val packageName = context.packageName
        var appVersion = "1.0.0"
        
        try {
            val pInfo = packageManager.getPackageInfo(packageName, 0)
            appVersion = pInfo.versionName ?: "1.0.0"
        } catch (e: Exception) {
            // Fallback
        }

        return mapOf(
            "manufacturer" to Build.MANUFACTURER,
            "model" to Build.MODEL,
            "androidVersion" to Build.VERSION.RELEASE,
            "apiLevel" to Build.VERSION.SDK_INT.toString(),
            "appPackage" to packageName,
            "appVersion" to appVersion,
            "sdkVersion" to "1.0.0-alpha"
        )
    }
}
