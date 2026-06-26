package com.aicamera.app.domain.ai

import android.os.Handler
import android.os.HandlerThread
import java.util.concurrent.Executor

/**
 * ML Kit专用执行器
 * 在独立线程执行AI检测任务，避免阻塞主线程
 */
object MlKitExecutor : Executor {
    
    private val handlerThread = HandlerThread("MLKitThread").apply {
        start()
    }
    
    private val handler = Handler(handlerThread.looper)
    
    override fun execute(command: Runnable) {
        handler.post(command)
    }
    
    fun quit() {
        handlerThread.quitSafely()
    }
}