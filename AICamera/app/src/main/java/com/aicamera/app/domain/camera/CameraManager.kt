package com.aicamera.app.domain.camera

import android.content.Context
import android.graphics.Bitmap
import android.util.Log
import androidx.camera.core.*
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.core.content.ContextCompat
import androidx.lifecycle.LifecycleOwner
import com.aicamera.app.domain.model.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import java.io.File
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors

/**
 * 相机管理器
 * 集成CameraX，提供拍摄、预览、焦段控制等功能
 */
class CameraManager(private val context: Context, private val lifecycleOwner: LifecycleOwner) {
    
    companion object {
        private const val TAG = "CameraManager"
    }
    
    private val cameraExecutor: ExecutorService = Executors.newSingleThreadExecutor()
    private var cameraProvider: ProcessCameraProvider? = null
    private var camera: Camera? = null
    private var preview: Preview? = null
    private var imageCapture: ImageCapture? = null
    private var imageAnalyzer: ImageAnalysis? = null
    
    // 相机状态
    private val _cameraState = MutableStateFlow<CameraState>(CameraState.Idle)
    val cameraState: StateFlow<CameraState> = _cameraState.asStateFlow()
    
    // 当前焦段
    private val _currentFocalLength = MutableStateFlow(1.0f)
    val currentFocalLength: StateFlow<Float> = _currentFocalLength.asStateFlow()
    
    // 可用镜头
    private val _availableLenses = MutableStateFlow<List<LensInfo>>(emptyList())
    val availableLenses: StateFlow<List<LensInfo>> = _availableLenses.asStateFlow()
    
    // 水平仪状态
    private val _levelState = MutableStateFlow(LevelState(0f, 0f, true, 0f))
    val levelState: StateFlow<LevelState> = _levelState.asStateFlow()
    
    /**
     * 初始化相机
     */
    fun initialize(previewView: PreviewView) {
        try {
            val cameraProviderFuture = ProcessCameraProvider.getInstance(context)
            
            cameraProviderFuture.addListener({
                cameraProvider = cameraProviderFuture.get()
                
                // 检测可用镜头
                detectAvailableLenses()
                
                // 启动预览
                startPreview(previewView)
                
                _cameraState.value = CameraState.Active
            }, ContextCompat.getMainExecutor(context))
        } catch (e: Exception) {
            Log.e(TAG, "Camera initialization failed", e)
            _cameraState.value = CameraState.Error(e.message ?: "初始化失败")
        }
    }
    
    /**
     * 检测可用镜头
     */
    private fun detectAvailableLenses() {
        val lenses = mutableListOf<LensInfo>()
        
        cameraProvider?.let { provider ->
            // 检查所有可用镜头
            for (selector in listOf(
                CameraSelector.LENS_FACING_BACK,
                CameraSelector.LENS_FACING_FRONT
            )) {
                if (provider.hasCamera(selector)) {
                    try {
                        val cameraInfo = provider.getCameraInfo(selector)
                        val zoomRatioRange = cameraInfo.zoomState.value?.zoomRatioRange
                        
                        // 主摄 (1x)
                        lenses.add(LensInfo(
                            focalLength = 1.0f,
                            label = "1x",
                            lensType = LensType.WIDE,
                            selector = CameraSelector.Builder()
                                .requireLensFacing(CameraSelector.LENS_FACING_BACK)
                                .build()
                        ))
                        
                        // 如果支持多焦段
                        zoomRatioRange?.let { range ->
                            // 超广角 (0.5x)
                            if (range.lower < 1.0f) {
                                lenses.add(LensInfo(
                                    focalLength = 0.5f,
                                    label = "0.5x",
                                    lensType = LensType.ULTRA_WIDE,
                                    selector = CameraSelector.Builder()
                                        .requireLensFacing(CameraSelector.LENS_FACING_BACK)
                                        .build()
                                ))
                            }
                            
                            // 长焦 (2x)
                            if (range.upper >= 2.0f) {
                                lenses.add(LensInfo(
                                    focalLength = 2.0f,
                                    label = "2x",
                                    lensType = LensType.TELEPHOTO,
                                    selector = CameraSelector.Builder()
                                        .requireLensFacing(CameraSelector.LENS_FACING_BACK)
                                        .build()
                                ))
                            }
                            
                            // 更长焦段 (3x, 4x等)
                            if (range.upper >= 3.0f) {
                                lenses.add(LensInfo(
                                    focalLength = 3.0f,
                                    label = "3x",
                                    lensType = LensType.TELEPHOTO,
                                    selector = CameraSelector.Builder()
                                        .requireLensFacing(CameraSelector.LENS_FACING_BACK)
                                        .build()
                                ))
                            }
                            
                            if (range.upper >= 4.0f) {
                                lenses.add(LensInfo(
                                    focalLength = 4.0f,
                                    label = "4x",
                                    lensType = LensType.TELEPHOTO,
                                    selector = CameraSelector.Builder()
                                        .requireLensFacing(CameraSelector.LENS_FACING_BACK)
                                        .build()
                                ))
                            }
                        }
                        
                    } catch (e: Exception) {
                        Log.e(TAG, "Error detecting lens capabilities", e)
                    }
                }
            }
        }
        
        _availableLenses.value = lenses.distinctBy { it.focalLength }.sortedBy { it.focalLength }
    }
    
    /**
     * 启动预览
     */
    private fun startPreview(previewView: PreviewView) {
        val cameraSelector = CameraSelector.Builder()
            .requireLensFacing(CameraSelector.LENS_FACING_BACK)
            .build()
        
        preview = Preview.Builder()
            .setTargetResolution(android.util.Size(1920, 1080))
            .build()
            .also {
                it.setSurfaceProvider(previewView.surfaceProvider)
            }
        
        imageCapture = ImageCapture.Builder()
            .setCaptureMode(ImageCapture.CAPTURE_MODE_MINIMIZE_LATENCY)
            .setTargetResolution(android.util.Size(1920, 1080))
            .setFlashMode(ImageCapture.FLASH_MODE_AUTO)
            .build()
        
        imageAnalyzer = ImageAnalysis.Builder()
            .setTargetResolution(android.util.Size(1280, 720))
            .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
            .setOutputImageFormat(ImageAnalysis.OUTPUT_IMAGE_FORMAT_RGBA_8888)
            .build()
        
        try {
            cameraProvider?.unbindAll()
            camera = cameraProvider?.bindToLifecycle(
                lifecycleOwner,
                cameraSelector,
                preview,
                imageCapture,
                imageAnalyzer
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error binding camera use cases", e)
        }
    }
    
    /**
     * 设置图像分析器
     */
    fun setImageAnalyzer(analyzer: ImageAnalysis.Analyzer) {
        imageAnalyzer?.setAnalyzer(cameraExecutor, analyzer)
    }
    
    /**
     * 设置焦段
     */
    fun setFocalLength(focalLength: Float) {
        camera?.let { cam ->
            val cameraInfo = cam.cameraInfo
            val zoomState = cameraInfo.zoomState.value
            
            zoomState?.zoomRatioRange?.let { range ->
                val targetRatio = focalLength
                val clampedRatio = targetRatio.coerceIn(range.lower, range.upper)
                
                cam.cameraControl.setZoomRatio(clampedRatio)
                    .addOnSuccessListener {
                        _currentFocalLength.value = clampedRatio
                        Log.d(TAG, "Zoom set to ${clampedRatio}x")
                    }
                    .addOnFailureListener { e ->
                        Log.e(TAG, "Failed to set zoom", e)
                    }
            }
        }
    }
    
    /**
     * 平滑缩放至目标焦段
     */
    fun smoothZoomTo(targetFocalLength: Float, durationMs: Long = 300) {
        val currentZoom = _currentFocalLength.value
        val steps = 20
        val stepDuration = durationMs / steps
        
        camera?.cameraControl?.let { control ->
            val zoomStep = (targetFocalLength - currentZoom) / steps
            var currentStep = 0
            
            val handler = android.os.Handler(android.os.Looper.getMainLooper())
            val runnable = object : Runnable {
                override fun run() {
                    if (currentStep < steps) {
                        val nextZoom = currentZoom + zoomStep * (currentStep + 1)
                        control.setZoomRatio(nextZoom)
                        _currentFocalLength.value = nextZoom
                        currentStep++
                        handler.postDelayed(this, stepDuration)
                    } else {
                        _currentFocalLength.value = targetFocalLength
                    }
                }
            }
            
            handler.post(runnable)
        }
    }
    
    /**
     * 拍照
     */
    fun capturePhoto(
        outputFile: File,
        onSuccess: (File) -> Unit,
        onError: (Exception) -> Unit
    ) {
        val outputOptions = ImageCapture.OutputFileOptions.Builder(outputFile).build()
        
        imageCapture?.takePicture(
            outputOptions,
            cameraExecutor,
            object : ImageCapture.OnImageSavedCallback {
                override fun onImageSaved(output: ImageCapture.OutputFileResults) {
                    Log.d(TAG, "Photo saved: ${outputFile.absolutePath}")
                    onSuccess(outputFile)
                }
                
                override fun onError(exception: ImageCaptureException) {
                    Log.e(TAG, "Photo capture failed", exception)
                    onError(exception)
                }
            }
        )
    }
    
    /**
     * 拍照并返回Bitmap（用于实时滤镜预览）
     */
    fun captureBitmap(
        onSuccess: (Bitmap) -> Unit,
        onError: (Exception) -> Unit
    ) {
        imageCapture?.takePicture(
            cameraExecutor,
            object : ImageCapture.OnImageCapturedCallback() {
                override fun onCaptureSuccess(image: ImageProxy) {
                    val bitmap = imageToBitmap(image)
                    image.close()
                    onSuccess(bitmap)
                }
                
                override fun onError(exception: ImageCaptureException) {
                    Log.e(TAG, "Bitmap capture failed", exception)
                    onError(exception)
                }
            }
        )
    }
    
    /**
     * ImageProxy转Bitmap
     */
    private fun imageToBitmap(image: ImageProxy): Bitmap {
        val buffer = image.planes[0].buffer
        val bytes = ByteArray(buffer.remaining())
        buffer.get(bytes)
        
        val bitmap = android.graphics.BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
        
        // 根据旋转角度调整
        val rotation = image.imageInfo.rotationDegrees.toFloat()
        if (rotation != 0f) {
            val matrix = android.graphics.Matrix()
            matrix.postRotate(rotation)
            return Bitmap.createBitmap(
                bitmap, 0, 0, bitmap.width, bitmap.height, matrix, true
            )
        }
        
        return bitmap
    }
    
    /**
     * 更新水平仪状态（通过设备姿态）
     */
    fun updateLevelState(roll: Float, pitch: Float) {
        val deviation = kotlin.math.abs(roll) + kotlin.math.abs(pitch)
        val isLevel = deviation < 3.0f // 偏差小于3度视为水平
        
        _levelState.value = LevelState(
            pitch = pitch,
            roll = roll,
            isLevel = isLevel,
            deviation = deviation
        )
    }
    
    /**
     * 设置网格可见性
     */
    fun setGridVisible(visible: Boolean) {
        // 网格显示由UI层控制
    }
    
    /**
     * 释放资源
     */
    fun release() {
        cameraProvider?.unbindAll()
        cameraExecutor.shutdown()
        cameraProvider = null
        camera = null
        preview = null
        imageCapture = null
        imageAnalyzer = null
    }
}

/**
 * 相机状态
 */
sealed class CameraState {
    object Idle : CameraState()
    object Active : CameraState()
    object Capturing : CameraState()
    data class Error(val message: String) : CameraState()
}

/**
 * 镜头信息
 */
data class LensInfo(
    val focalLength: Float,
    val label: String,
    val lensType: LensType,
    val selector: CameraSelector
)