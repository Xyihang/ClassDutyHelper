package com.aicamera.app.ui.screens

import android.app.Application
import android.content.Context
import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import android.os.VibrationEffect
import android.os.Vibrator
import android.util.Log
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.camera.core.toBitmap
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.aicamera.app.domain.ai.CompositionEngine
import com.aicamera.app.domain.ai.CompositionResult
import com.aicamera.app.domain.filter.FilmFilters
import com.aicamera.app.domain.model.*
import com.google.mlkit.vision.common.InputImage
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import java.io.File
import kotlin.math.abs
import kotlin.math.atan2
import kotlin.math.sqrt
import kotlin.math.toDegrees

/**
 * 相机ViewModel
 * 连接CameraManager和CompositionEngine，实现完整的业务逻辑
 */
class CameraViewModel(application: Application) : AndroidViewModel(application) {
    
    companion object {
        private const val TAG = "CameraViewModel"
        private const val ALIGNMENT_THRESHOLD = 0.85f
    }
    
    // CameraManager 延迟初始化，由 Activity 层注入
    var cameraManager: com.aicamera.app.domain.camera.CameraManager? = null
    
    private val compositionEngine = CompositionEngine(application)
    
    private val sensorManager = application.getSystemService(Context.SENSOR_SERVICE) as SensorManager
    private val accelerometer = sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER)
    
    private val _uiState = MutableStateFlow(CameraUiState())
    val uiState: StateFlow<CameraUiState> = _uiState.asStateFlow()
    
    private val _compositionState = MutableStateFlow(CompositionState())
    val compositionState: StateFlow<CompositionState> = _compositionState.asStateFlow()
    
    private val _captureState = MutableStateFlow<CaptureState>(CaptureState.Idle)
    val captureState: StateFlow<CaptureState> = _captureState.asStateFlow()
    
    private var isAiEnabled = false
    
    private val sensorListener = object : SensorEventListener {
        override fun onSensorChanged(event: SensorEvent?) {
            event?.let {
                if (it.sensor.type == Sensor.TYPE_ACCELEROMETER) {
                    val roll = toDegrees(atan2(it.values[0].toDouble(), it.values[1].toDouble())).toFloat()
                    val pitch = toDegrees(atan2(it.values[2].toDouble(), sqrt(it.values[0] * it.values[0] + it.values[1] * it.values[1]).toDouble())).toFloat()
                    
                    cameraManager?.updateLevelState(roll, pitch)
                    _compositionState.update { state ->
                        state.copy(
                            levelState = state.levelState.copy(
                                pitch = pitch,
                                roll = roll,
                                isLevel = abs(roll) + abs(pitch) < 3f,
                                deviation = abs(roll) + abs(pitch)
                            )
                        )
                    }
                }
            }
        }
        
        override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) {}
    }
    
    fun initCameraManager(cm: com.aicamera.app.domain.camera.CameraManager) {
        cameraManager = cm
        cm.setImageAnalyzer(createImageAnalyzer())
    }
    
    private fun createImageAnalyzer(): ImageAnalysis.Analyzer {
        return ImageAnalysis.Analyzer { imageProxy ->
            if (isAiEnabled) {
                analyzeImage(imageProxy)
            } else {
                imageProxy.close()
            }
        }
    }
    
    private fun analyzeImage(imageProxy: ImageProxy) {
        viewModelScope.launch(Dispatchers.IO) {
            try {
                val bitmap = imageProxy.toBitmap()
                val inputImage = InputImage.fromBitmap(bitmap, imageProxy.imageInfo.rotationDegrees)
                
                val result = compositionEngine.analyzeComposition(inputImage)
                handleCompositionResult(result)
            } catch (e: Exception) {
                Log.e(TAG, "Image analysis failed", e)
            } finally {
                imageProxy.close()
            }
        }
    }
    
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
        
        val currentFocal = cameraManager?.currentFocalLength?.value ?: 1.0f
        if (isAiEnabled && result.guide.suggestedFocalLength != currentFocal) {
            cameraManager?.smoothZoomTo(result.guide.suggestedFocalLength)
        }
        
        if (calculateAlignmentProgress(result) >= ALIGNMENT_THRESHOLD) {
            triggerAlignmentFeedback()
        }
    }
    
    private fun calculateAlignmentProgress(result: CompositionResult): Float {
        if (result.subjects.isEmpty()) return 0f
        
        val guide = result.guide
        val mainSubject = result.subjects[0]
        
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
        
        val deviationX = abs(mainSubject.bounds.centerX - targetX)
        val deviationY = abs(mainSubject.bounds.centerY - targetY)
        
        return (1f - (deviationX + deviationY) * 2f).coerceIn(0f, 1f)
    }
    
    private fun triggerAlignmentFeedback() {
        val context = getApplication<Application>()
        val vibrator = context.getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
        if (vibrator?.hasVibrator() == true) {
            vibrator.vibrate(VibrationEffect.createOneShot(50, VibrationEffect.DEFAULT_AMPLITUDE))
        }
    }
    
    fun toggleAiComposition(enabled: Boolean) {
        isAiEnabled = enabled
        _compositionState.update { state ->
            if (enabled) state.copy(aiEnabled = true)
            else state.copy(aiEnabled = false, guide = null, subjects = emptyList(), alignmentProgress = 0f)
        }
    }
    
    fun capturePhoto() {
        val cm = cameraManager ?: return
        viewModelScope.launch {
            _captureState.value = CaptureState.Capturing
            
            val fileName = "photo_${System.currentTimeMillis()}.jpg"
            val outputDir = getApplication<Application>().getExternalFilesDir(null)
            val outputFile = File(outputDir, fileName)
            
            cm.capturePhoto(
                outputFile = outputFile,
                onSuccess = { file ->
                    _captureState.value = CaptureState.Success(file.path)
                    viewModelScope.launch {
                        kotlinx.coroutines.delay(500)
                        _captureState.value = CaptureState.Idle
                    }
                },
                onError = { e ->
                    Log.e(TAG, "Capture failed", e)
                    _captureState.value = CaptureState.Error(e.message ?: "拍摄失败")
                    viewModelScope.launch {
                        kotlinx.coroutines.delay(1000)
                        _captureState.value = CaptureState.Idle
                    }
                }
            )
        }
    }
    
    fun setFocalLength(focalLength: Float) {
        cameraManager?.smoothZoomTo(focalLength)
    }
    
    fun applyFilter(filter: FilmFilter) {
        _uiState.update { state -> state.copy(currentFilter = filter) }
    }
    
    fun selectSubject(subjectId: Int?) {
        _compositionState.update { state -> state.copy(selectedSubjectId = subjectId) }
    }
    
    fun getRecommendedFilter(): FilmFilter {
        return FilmFilters.getRecommendedFilter(_compositionState.value.sceneType)
    }
    
    override fun onCleared() {
        super.onCleared()
        sensorManager.unregisterListener(sensorListener)
        cameraManager?.release()
        compositionEngine.release()
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
)

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