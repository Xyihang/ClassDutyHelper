package com.aicamera.app.domain.ai

import android.content.Context
import android.graphics.Rect
import android.util.Log
import com.aicamera.app.domain.model.*
import com.google.mlkit.vision.common.InputImage
import com.google.mlkit.vision.label.ImageLabeling
import com.google.mlkit.vision.label.defaults.ImageLabelerOptions
import com.google.mlkit.vision.objects.DetectedObject
import com.google.mlkit.vision.objects.ObjectDetection
import com.google.mlkit.vision.objects.defaults.ObjectDetectorOptions
import kotlinx.coroutines.channels.awaitClose
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.callbackFlow
import kotlinx.coroutines.flow.flow
import java.util.concurrent.atomic.AtomicBoolean
import kotlin.math.abs

/**
 * AI构图引擎
 * 集成场景检测、主体识别、构图分析
 */
class CompositionEngine(private val context: Context) {
    
    companion object {
        private const val TAG = "CompositionEngine"
        private const val ALIGNMENT_THRESHOLD = 0.85f
        private const val MIN_CONFIDENCE = 0.6f
    }
    
    // ML Kit检测器
    private val objectDetector by lazy {
        ObjectDetection.getClient(
            ObjectDetectorOptions.Builder()
                .setExecutor(MlKitExecutor)
                .enableClassification()
                .enableMultipleObjects()
                .build()
        )
    }
    
    private val imageLabeler by lazy {
        ImageLabeling.getClient(
            ImageLabelerOptions.Builder()
                .setExecutor(MlKitExecutor)
                .setConfidenceThreshold(0.5f)
                .build()
        )
    }
    
    private val isProcessing = AtomicBoolean(false)
    
    /**
     * 分析图像并生成构图建议
     */
    fun analyzeComposition(image: InputImage): Flow<CompositionResult> = callbackFlow {
        if (isProcessing.getAndSet(true)) {
            close()
            return@callbackFlow
        }
        
        val startTime = System.currentTimeMillis()
        val objects = mutableListOf<DetectedObject>()
        val labels = mutableListOf<String>()
        
        // 并行执行目标检测和场景分类
        val objectTask = objectDetector.process(image)
            .addOnSuccessListener { detectedObjects ->
                objects.addAll(detectedObjects)
            }
            .addOnFailureListener { e ->
                Log.e(TAG, "Object detection failed", e)
            }
        
        val labelTask = imageLabeler.process(image)
            .addOnSuccessListener { imageLabels ->
                labels.addAll(imageLabels.map { it.text })
            }
            .addOnFailureListener { e ->
                Log.e(TAG, "Scene labeling failed", e)
            }
        
        // 等待所有任务完成
        com.google.android.gms.tasks.Tasks.whenAllComplete(objectTask, labelTask)
            .addOnSuccessListener {
                val processingTime = System.currentTimeMillis() - startTime
                
                // 确定场景类型
                val sceneType = determineSceneType(labels, objects)
                
                // 提取主体
                val subjects = extractSubjects(objects, image.width, image.height)
                
                // 生成构图建议
                val compositionGuide = generateCompositionGuide(
                    subjects = subjects,
                    sceneType = sceneType,
                    imageWidth = image.width,
                    imageHeight = image.height
                )
                
                val result = CompositionResult(
                    sceneType = sceneType,
                    subjects = subjects,
                    guide = compositionGuide,
                    processingTimeMs = processingTime
                )
                
                trySend(result)
                isProcessing.set(false)
                close()
            }
            .addOnFailureListener { e ->
                Log.e(TAG, "Composition analysis failed", e)
                isProcessing.set(false)
                close(e)
            }
        
        awaitClose { 
            isProcessing.set(false)
        }
    }
    
    /**
     * 根据标签和检测物体确定场景类型
     */
    private fun determineSceneType(labels: List<String>, objects: List<DetectedObject>): SceneType {
        // 优先级检测
        for (label in labels) {
            when (label.lowercase()) {
                "person", "people", "human", "face" -> return SceneType.PORTRAIT
                "dog", "cat", "pet", "animal" -> return SceneType.PET
                "food", "meal", "dish", "restaurant" -> return SceneType.FOOD
                "building", "architecture", "house", "skyscraper" -> return SceneType.ARCHITECTURE
                "street", "road", "city", "urban" -> return SceneType.STREET
                "night", "dark", "evening" -> return SceneType.NIGHT
                "mountain", "landscape", "nature", "sky", "tree", "water" -> return SceneType.LANDSCAPE
            }
        }
        
        // 基于检测物体判断
        for (obj in objects) {
            val category = classifyObjectCategory(obj)
            when (category) {
                SubjectCategory.PERSON -> return SceneType.PORTRAIT
                SubjectCategory.PET -> return SceneType.PET
                SubjectCategory.FOOD -> return SceneType.FOOD
                SubjectCategory.ARCHITECTURE -> return SceneType.ARCHITECTURE
                else -> {}
            }
        }
        
        return SceneType.AUTO
    }
    
    /**
     * 分类检测物体
     */
    private fun classifyObjectCategory(obj: DetectedObject): SubjectCategory {
        if (obj.labels.isNotEmpty()) {
            val label = obj.labels[0].text.lowercase()
            when {
                label.contains("person") || label.contains("face") -> return SubjectCategory.PERSON
                label.contains("dog") || label.contains("cat") -> return SubjectCategory.PET
                label.contains("food") -> return SubjectCategory.FOOD
                label.contains("building") -> return SubjectCategory.ARCHITECTURE
                label.contains("car") || label.contains("vehicle") -> return SubjectCategory.VEHICLE
                label.contains("plant") || label.contains("tree") -> return SubjectCategory.PLANT
            }
        }
        return SubjectCategory.OBJECT
    }
    
    /**
     * 提取主体信息
     */
    private fun extractSubjects(
        objects: List<DetectedObject>,
        imageWidth: Int,
        imageHeight: Int
    ): List<SubjectDetection> {
        return objects
            .filter { it.labels.isNotEmpty() && it.labels[0].confidence >= MIN_CONFIDENCE }
            .mapIndexed { index, obj ->
                SubjectDetection(
                    bounds = RectF(
                        left = obj.boundingBox.left.toFloat() / imageWidth,
                        top = obj.boundingBox.top.toFloat() / imageHeight,
                        right = obj.boundingBox.right.toFloat() / imageWidth,
                        bottom = obj.boundingBox.bottom.toFloat() / imageHeight
                    ),
                    confidence = obj.labels[0].confidence,
                    category = classifyObjectCategory(obj),
                    trackingId = obj.trackingId
                )
            }
            .sortedByDescending { it.confidence }
    }
    
    /**
     * 生成构图建议
     */
    private fun generateCompositionGuide(
        subjects: List<SubjectDetection>,
        sceneType: SceneType,
        imageWidth: Int,
        imageHeight: Int
    ): CompositionGuide {
        if (subjects.isEmpty()) {
            // 无主体时返回默认三分法构图
            return generateDefaultComposition(imageWidth, imageHeight)
        }
        
        val mainSubject = subjects[0]
        val rule = determineCompositionRule(sceneType, mainSubject)
        val frameBounds = calculateOptimalFrame(mainSubject, rule, imageWidth, imageHeight)
        val gridLines = generateGridLines(rule, imageWidth, imageHeight)
        val focalLength = calculateOptimalFocalLength(sceneType, mainSubject)
        val hints = generateCompositionHints(mainSubject, rule)
        
        return CompositionGuide(
            frameBounds = frameBounds,
            rule = rule,
            suggestedFocalLength = focalLength,
            gridLines = gridLines,
            hints = hints
        )
    }
    
    /**
     * 根据场景和主体确定构图规则
     */
    private fun determineCompositionRule(
        sceneType: SceneType,
        subject: SubjectDetection
    ): CompositionRule {
        return when (sceneType) {
            SceneType.PORTRAIT -> {
                // 人像推荐三分法或黄金分割
                if (subject.bounds.width < 0.4f) {
                    CompositionRule.RULE_OF_THIRDS
                } else {
                    CompositionRule.CENTER
                }
            }
            SceneType.FOOD -> CompositionRule.CENTER
            SceneType.ARCHITECTURE -> {
                // 建筑推荐对称或引导线
                if (abs(subject.bounds.centerX - 0.5f) < 0.1f) {
                    CompositionRule.SYMMETRY
                } else {
                    CompositionRule.LEADING_LINE
                }
            }
            SceneType.LANDSCAPE -> CompositionRule.RULE_OF_THIRDS
            SceneType.STREET -> CompositionRule.LEADING_LINE
            SceneType.NIGHT -> CompositionRule.CENTER
            SceneType.PET -> CompositionRule.GOLDEN_RATIO
            SceneType.AUTO -> CompositionRule.RULE_OF_THIRDS
        }
    }
    
    /**
     * 计算最优取景框
     */
    private fun calculateOptimalFrame(
        subject: SubjectDetection,
        rule: CompositionRule,
        imageWidth: Int,
        imageHeight: Int
    ): RectF {
        val subjectCenterX = subject.bounds.centerX
        val subjectCenterY = subject.bounds.centerY
        val subjectWidth = subject.bounds.width
        val subjectHeight = subject.bounds.height
        
        // 根据构图规则计算目标位置
        val (targetX, targetY) = when (rule) {
            CompositionRule.RULE_OF_THIRDS -> {
                // 三分法 - 主体放在交点
                val x = if (subjectCenterX < 0.5f) 0.333f else 0.667f
                val y = if (subjectCenterY < 0.5f) 0.333f else 0.667f
                Pair(x, y)
            }
            CompositionRule.GOLDEN_RATIO -> {
                // 黄金分割
                val golden = 0.618f
                val x = if (subjectCenterX < 0.5f) golden else (1f - golden)
                val y = if (subjectCenterY < 0.5f) golden else (1f - golden)
                Pair(x, y)
            }
            CompositionRule.CENTER -> Pair(0.5f, 0.5f)
            CompositionRule.SYMMETRY -> Pair(0.5f, 0.5f)
            else -> Pair(0.5f, 0.5f)
        }
        
        // 计算取景框（留白）
        val padding = when {
            subjectWidth > 0.5f -> 0.05f
            subjectWidth > 0.3f -> 0.1f
            else -> 0.15f
        }
        
        return RectF(
            left = maxOf(0f, targetX - subjectWidth / 2 - padding),
            top = maxOf(0f, targetY - subjectHeight / 2 - padding),
            right = minOf(1f, targetX + subjectWidth / 2 + padding),
            bottom = minOf(1f, targetY + subjectHeight / 2 + padding)
        )
    }
    
    /**
     * 生成网格线
     */
    private fun generateGridLines(
        rule: CompositionRule,
        imageWidth: Int,
        imageHeight: Int
    ): List<GridLine> {
        return when (rule) {
            CompositionRule.RULE_OF_THIRDS -> {
                listOf(
                    // 垂直线
                    GridLine(PointF(0.333f, 0f), PointF(0.333f, 1f), GridLineType.VERTICAL),
                    GridLine(PointF(0.667f, 0f), PointF(0.667f, 1f), GridLineType.VERTICAL),
                    // 水平线
                    GridLine(PointF(0f, 0.333f), PointF(1f, 0.333f), GridLineType.HORIZONTAL),
                    GridLine(PointF(0f, 0.667f), PointF(1f, 0.667f), GridLineType.HORIZONTAL)
                )
            }
            CompositionRule.GOLDEN_RATIO -> {
                val golden = 0.618f
                listOf(
                    GridLine(PointF(golden, 0f), PointF(golden, 1f), GridLineType.VERTICAL),
                    GridLine(PointF(1f - golden, 0f), PointF(1f - golden, 1f), GridLineType.VERTICAL),
                    GridLine(PointF(0f, golden), PointF(1f, golden), GridLineType.HORIZONTAL),
                    GridLine(PointF(0f, 1f - golden), PointF(1f, 1f - golden), GridLineType.HORIZONTAL)
                )
            }
            CompositionRule.CENTER -> {
                listOf(
                    GridLine(PointF(0.5f, 0f), PointF(0.5f, 1f), GridLineType.VERTICAL),
                    GridLine(PointF(0f, 0.5f), PointF(1f, 0.5f), GridLineType.HORIZONTAL)
                )
            }
            CompositionRule.SYMMETRY -> {
                listOf(
                    GridLine(PointF(0.5f, 0f), PointF(0.5f, 1f), GridLineType.VERTICAL)
                )
            }
            else -> emptyList()
        }
    }
    
    /**
     * 计算最优焦段
     */
    private fun calculateOptimalFocalLength(
        sceneType: SceneType,
        subject: SubjectDetection
    ): Float {
        return when (sceneType) {
            SceneType.PORTRAIT -> {
                // 人像推荐中长焦
                when {
                    subject.bounds.width > 0.5f -> 2.0f
                    subject.bounds.width > 0.3f -> 1.5f
                    else -> 1.0f
                }
            }
            SceneType.FOOD -> 1.0f // 主摄
            SceneType.ARCHITECTURE -> 0.5f // 超广角
            SceneType.LANDSCAPE -> 0.5f // 超广角
            SceneType.STREET -> 1.0f
            SceneType.NIGHT -> 1.0f
            SceneType.PET -> 2.0f // 长焦抓拍
            SceneType.AUTO -> 1.0f
        }
    }
    
    /**
     * 生成构图提示
     */
    private fun generateCompositionHints(
        subject: SubjectDetection,
        rule: CompositionRule
    ): List<CompositionHint> {
        val hints = mutableListOf<CompositionHint>()
        
        // 根据主体位置和构图规则生成提示
        when {
            subject.bounds.centerX < 0.2f -> {
                hints.add(CompositionHint("向右移动，让主体位于画面中心区域", HintPriority.HIGH))
            }
            subject.bounds.centerX > 0.8f -> {
                hints.add(CompositionHint("向左移动，让主体位于画面中心区域", HintPriority.HIGH))
            }
            subject.bounds.centerY < 0.2f -> {
                hints.add(CompositionHint("向下移动相机", HintPriority.MEDIUM))
            }
            subject.bounds.centerY > 0.8f -> {
                hints.add(CompositionHint("向上移动相机", HintPriority.MEDIUM))
            }
        }
        
        if (rule == CompositionRule.RULE_OF_THIRDS) {
            hints.add(CompositionHint("将主体对准三分法交点", HintPriority.LOW))
        }
        
        return hints
    }
    
    /**
     * 默认构图（无主体时）
     */
    private fun generateDefaultComposition(
        imageWidth: Int,
        imageHeight: Int
    ): CompositionGuide {
        return CompositionGuide(
            frameBounds = RectF(0.1f, 0.1f, 0.9f, 0.9f),
            rule = CompositionRule.RULE_OF_THIRDS,
            suggestedFocalLength = 1.0f,
            gridLines = generateGridLines(CompositionRule.RULE_OF_THIRDS, imageWidth, imageHeight),
            hints = listOf(CompositionHint("未检测到明显主体，请对准拍摄对象", HintPriority.HIGH))
        )
    }
    
    /**
     * 检测主体追踪（用于动态场景）
     */
    fun trackSubject(
        image: InputImage,
        trackingId: Int
    ): Flow<SubjectDetection?> = flow {
        // 实现主体追踪逻辑
        emit(null)
    }
    
    /**
     * 释放资源
     */
    fun release() {
        try {
            objectDetector.close()
            imageLabeler.close()
        } catch (e: Exception) {
            Log.e(TAG, "Error closing detectors", e)
        }
    }
}

/**
 * 构图分析结果
 */
data class CompositionResult(
    val sceneType: SceneType,
    val subjects: List<SubjectDetection>,
    val guide: CompositionGuide,
    val processingTimeMs: Long
)