package com.aicamera.app

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build

class AICameraApp : Application() {
    
    override fun onCreate() {
        super.onCreate()
        instance = this
        initNotificationChannels()
    }

    private fun initNotificationChannels() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                "AI相机",
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = "拍摄完成通知"
            }
            
            val manager = getSystemService(NotificationManager::class.java)
            manager.createNotificationChannel(channel)
        }
    }

    companion object {
        const val CHANNEL_ID = "ai_camera_channel"
        lateinit var instance: AICameraApp
            private set
    }
}