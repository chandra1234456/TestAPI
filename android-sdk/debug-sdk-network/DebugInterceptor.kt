package com.debugsdk.network

import com.debugsdk.core.DataRedactor
import com.debugsdk.core.DebugEvent
import com.debugsdk.core.DebugSDK
import okhttp3.Interceptor
import okhttp3.Response
import java.io.IOException
import java.util.concurrent.TimeUnit

class DebugInterceptor(
    private val redactor: DataRedactor = DataRedactor()
) : Interceptor {

    @Throws(IOException::class)
    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val url = request.url.toString()
        val method = request.method
        val startTime = System.nanoTime()

        var response: Response? = null
        var statusCode: Int? = null
        var errorMsg: String? = null

        try {
            response = chain.proceed(request)
            statusCode = response.code
            return response
        } catch (e: Exception) {
            errorMsg = e.localizedMessage ?: e.javaClass.simpleName
            throw e
        } finally {
            val durationMs = TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - startTime)

            val networkEvent = DebugEvent.Network(
                method = method,
                url = redactor.redactText(url) ?: url,
                statusCode = statusCode,
                durationMs = durationMs,
                error = errorMsg
            )

            DebugSDK.recordEvent(networkEvent)
        }
    }
}
