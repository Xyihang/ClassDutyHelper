package com.aicamera.app.ui.screens

import android.app.Application
import android.graphics.Bitmap
import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import android.util.Log
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.aicamera.app.domain.ai.CompositionEngine
import com.aicamera.app.domain.ai.CompositionResult
import com.aicamera.app.domain.camera.CameraManager
import com.aicamera.app.domain.filter.FilmFilters
import com.aicamera.app.domain.model.*
import com.google.mlkit.vision.common.InputImage
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import java.io.File

/**
 * 相机ViewModel
 * 连接CameraManager和CompositionEngine，实现完整的业务逻辑
 */
class CameraViewModel(application: Application) : AndroidViewModel(application) {
    
    companion object {
        private const val TAG = "CameraViewModel"
        private const val ANALYSIS_INTERVAL_MS = 100L // AI分析间隔
        private const val ALIGNMENT_THRESHOLD = 0.85f
    }
    
    // 依赖组件
    private val cameraManager = CameraManager(application, this)
    private val compositionEngine = CompositionEngine(application)
    
    // 传感器管理器（用于水平仪）
    private val sensorManager = application.getSystemService(SensorManager::class.java)
    private val accelerometer = sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER)
    
    // UI状态
    private val _uiState = MutableStateFlow<CameraUiState>(CameraUiState.Initial)
    val uiState: StateFlow<CameraUiState> = _uiState.asStateFlow()
    
    // AI分析状态
    private val _compositionState = MutableStateFlow(CompositionState())
    val compositionState: StateFlow<CompositionState> = _compositionState.asStateFlow()
    
    // 拍摄状态
    private val _captureState = MutableStateFlow<CaptureState>(CaptureState.Idle)
    val captureState: StateFlow<CaptureState> = _captureState.asStateFlow()
    
    // AI分析任务
    private var analysisJob: Job? = null
    private var isAiEnabled = false
    
    // 传感器监听器
    private val sensorListener = object : SensorEventListener {
        override fun onSensorChanged(event: SensorEvent?) {
            event?.let {
                if (it.sensor.type == Sensor.TYPE_ACCELEROMETER) {
                    val roll = Math.toDegrees(Math.atan2(it.values[0], it.values[1]).toDouble()).toFloat()
                    val pitch = Math.toDegrees(Math.atan2(it.values[2], Math.sqrt(it.values[0] * it.values[0] + it.values[1] * it.values[1]).toDouble()).toDouble()).toFloat()
                    
                    cameraManager.updateLevelState(roll, pitch)
                    _compositionState.update { state ->
                        state.copy(
                            levelState = state.levelState.copy(
                                pitch = pitch,
                                roll = roll,
                                isLevel = Math.abs(roll) + Math.abs(pitch) < 3f,
                                deviation = Math.abs(roll) + Math.abs(pitch)
                            )
                        )
                    }
                }
            }
        }
        
        override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) {}
    }
    
    init {
        // 注册传感器监听
        sensorManager.registerListener(
            sensorListener,
            accelerometer,
            SensorManager.SENSOR_DELAY_UI
        )
        
        // 设置图像分析器
        setupImageAnalyzer()
    }
    
    /**
     * 设置图像分析器
     */
    private fun setupImageAnalyzer() {
        val analyzer = ImageAnalysis.Analyzer { imageProxy ->
            if (isAiEnabled) {
                analyzeImage(imageProxy)
            } else {
                imageProxy.close()
            }
        }
        
        cameraManager.setImageAnalyzer(analyzer)
    }
    
    /**
     * 分析图像
     */
    private fun analyzeImage(imageProxy: ImageProxy) {
        viewModelScope.launch(Dispatchers.IO) {
            try {
                val inputImage = InputImage.fromMediaImage(
                    imageProxy.image!!,
                    imageProxy.imageInfo.rotationDegrees
                )
                
                compositionEngine.analyzeComposition(inputImage)
                    .collect { result ->
                        handleCompositionResult(result)
                    }
            } catch (e: Exception) {
                Log.e(TAG, "Image analysis failed", e)
            } finally {
                imageProxy.close()
            }
        }
    }
    
    /**
     * 处理构图分析结果
     */
    private fun handleCompositionResult(result: CompositionResult) {
        _compositionState.update { state ->
            state.copy(
                sceneType = result.sceneType,
                subjects = result.subjects,
                guide = result.guide,
                alignmentProgress = calculateAlignmentProgress(result),
                processingTimeMs = result.processingTimeMs
            )
        }
        
        // 自动调整焦段
        if (isAiEnabled && result.guide.suggestedFocalLength != cameraManager.currentFocalLength.value) {
            cameraManager.smoothZoomTo(result.guide.suggestedFocalLength)
        }
        
        // 对齐完成反馈
        if (calculateAlignmentProgress(result) >= ALIGNMENT_THRESHOLD) {
            triggerAlignmentFeedback()
        }
    }
    
    /**
     * 计算对齐进度
     */
    private fun calculateAlignmentProgress(result: CompositionResult): Float {
        if (result.subjects.isEmpty()) return 0f
        
        val guide = result.guide
        val mainSubject = result.subjects[0]
        
        // 计算主体位置与推荐位置的偏差
        val targetX = when (guide.rule) {
            CompositionRule.RULE_OF_THIRDS -> if (mainSubject.bounds.centerX < 0.5f) 0.333f else 0.667f
            CompositionRule.GOLDEN_RATIO -> 0.618f
            CompositionRule.CENTER -> 0.5f
            CompositionRule.SYMMETRY -> 0.5f
            else -> 0.5f
        }
        
        val targetY = when (guide.rule) {
            CompositionRule.RULE_OF_THIRDS -> if (mainSubject.bounds.centerY < 0.5f) 0.333f else 0.667f
            CompositionRule.GOLDEN_RATIO -> 0.618f
            CompositionRule.CENTER -> 0.5f
            CompositionRule.SYMMETRY -> 0.5f
            else -> 0.5f
        }
        
        val deviationX = Math.abs(mainSubject.bounds.centerX - targetX)
        val deviationY = Math.abs(mainSubject.bounds.centerY - targetY)
        
        val alignmentScore = 1f - (deviationX + deviationY) * 2f
        
        return alignmentScore.coerceIn(0f, 1f)
    }
    
    /**
     * 触发对齐完成反馈
     */
    private fun triggerAlignmentFeedback() {
        viewModelScope.launch {
            val context = getApplication<Application>()
            val vibrator = context.getSystemService(android.content.Context.VIBRATOR_SERVICE) as android.os.Vibrator
            
            if (vibrator.hasVibrator()) {
                vibrator.vibrate(
                    android.os.VibrationEffect.createOneShot(
                        50,
                        android.os.VibrationEffect.DEFAULT_AMPLITUDE
                    )
                )
            }
        }
    }
    
    /**
     * 启用/禁用AI构图
     */
    fun toggleAiComposition(enabled: Boolean) {
        isAiEnabled = enabled
        
        if (enabled) {
            startAiAnalysis()
        } else {
            stopAiAnalysis()
        }
        
        _compositionState.update { state ->
            state.copy(aiEnabled = enabled)
        }
    }
    
    /**
     * 启动AI分析
     */
    private fun startAiAnalysis() {
        analysisJob?.cancel()
        
        analysisJob = viewModelScope.launch {
            while (isAiEnabled) {
                // 分析由图像分析器自动触发
                delay(ANALYSIS_INTERVAL_MS)
            }
        }
    }
    
    /**
     * 停止AI分析
     */
    private fun stopAiAnalysis() {
        analysisJob?.cancel()
        analysisJob = null
        
        _compositionState.update { state ->
            state.copy(
                guide = null,
                subjects = emptyList(),
                alignmentProgress = 0f
            )
        }
    }
    
    /**
     * 拍照
     */
    fun capturePhoto() {
        viewModelScope.launch {
            _captureState.value = CaptureState.Capturing
            
            val fileName = "photo_${System.currentTimeMillis()}.jpg"
            val outputDir = getApplication<Application>().getExternalFilesDir(null)
            val outputFile = File(outputDir, fileName)
            
            cameraManager.capturePhoto(
                outputFile = outputFile,
                onSuccess = { file ->
                    _captureState.value = CaptureState.Success(file.path)
                    
                    // 保存照片信息
                    val photo = Photo(
                        id = fileName,
                        uri = file.path,
                        timestamp = System.currentTimeMillis(),
                        sceneType = _compositionState.value.sceneType,
                        focalLength = cameraManager.currentFocalLength.value,
                        filter = FilmFilter.ORIGINAL,
                        width = 1920,
                        height = 1080
                    )
                    
                    // 重置状态
                    viewModelScope.launch {
                        delay(500)
                        _captureState.value = CaptureState.Idle
                    }
                },
                onError = { e ->
                    Log.e(TAG, "Capture failed", e)
                    _captureState.value = CaptureState.Error(e.message ?: "拍摄失败")
                    
                    viewModelScope.launch {
                        delay(1000)
                        _captureState.value = CaptureState.Idle
                    }
                }
            )
        }
    }
    
    /**
     * 设置焦段
     */
    fun setFocalLength(focalLength: Float) {
        cameraManager.smoothZoomTo(focalLength)
    }
    
    /**
     * 切换滤镜
     */
    fun applyFilter(filter: FilmFilter) {
        _uiState.update { state ->
            state.copy(currentFilter = filter)
        }
    }
    
    /**
     * 选择主体（多主体场景）
     */
    fun selectSubject(subjectId: Int?) {
        _compositionState.update { state ->
            state.copy(selectedSubjectId = subjectId)
        }
    }
    
    /**
     * 获取推荐的滤镜
     */
    fun getRecommendedFilter(): FilmFilter {
        val sceneType = _compositionState.value.sceneType
        return FilmFilters.getRecommendedFilter(sceneType)
    }
    
    override fun onCleared() {
        super.onCleared()
        
        // 释放资源
        sensorManager.unregisterListener(sensorListener)
        cameraManager.release()
        compositionEngine.release()
        
        analysisJob?.cancel()
    }
}

/**
 * 相机UI状态
 */
data class CameraUiState(
    val currentFilter: FilmFilter = FilmFilter.ORIGINAL,
    val currentFocalLength: Float = 1.0f,
    val showGrid: Boolean = true,
    val showLevel: Boolean = true
) {
    object Initial : CameraUiState()
}

/**
 * 构图状态
 */
data class CompositionState(
    val aiEnabled: Boolean = false,
    val sceneType: SceneType = SceneType.AUTO,
    val subjects: List<SubjectDetection> = emptyList(),
    val guide: CompositionGuide? = null,
    val alignmentProgress: Float = 0f,
    val levelState: LevelState = LevelState(0f, 0f, true, 0f),
    val selectedSubjectId: Int? = null,
    val processingTimeMs: Long = 0L
)

/**
 * 拍摄状态
 */
sealed class CaptureState {
    object Idle : CaptureState()
    object Capturing : CaptureState()
    data class Success(val photoPath: String) : CaptureState()
    data class Error(val message: String) : CaptureState()
}

// 数学转换工具
private fun Math.toRadians(angle: Double): Double = angle * Math.PI / 180
private fun Math.toDegrees(angle: Double): Double = angle * 180 / Math.PI