package com.aicamera.app.domain.model

/**
 * 拍摄场景类型
 */
enum class SceneType(val displayName: String, val description: String) {
    PORTRAIT("人像", "优化人物肤色与背景虚化"),
    LANDSCAPE("风景", "广角呈现壮美风光"),
    FOOD("美食", "俯拍与微距展现美味"),
    ARCHITECTURE("建筑", "矫正透视与线条"),
    STREET("街景", "捕捉城市人文"),
    NIGHT("夜景", "低光降噪优化"),
    PET("宠物", "动态追踪抓拍"),
    AUTO("自动", "AI智能识别场景")
}

/**
 * 构图规则
 */
enum class CompositionRule(val displayName: String) {
    RULE_OF_THIRDS("三分法"),
    GOLDEN_RATIO("黄金分割"),
    SYMMETRY("对称构图"),
    LEADING_LINE("引导线"),
    DIAGONAL("对角线"),
    CENTER("中心构图"),
    FRAME("框架构图")
}

/**
 * 焦段信息
 */
data class FocalLength(
    val value: Float,      // 焦段值 (如 1.0x, 2.0x)
    val displayLabel: String, // 显示标签 (如 "1x", "2x")
    val lensType: LensType
)

enum class LensType {
    ULTRA_WIDE,    // 超广角
    WIDE,          // 主摄
    TELEPHOTO,     // 长焦
    MACRO          // 微距
}

/**
 * 主体检测结果
 */
data class SubjectDetection(
    val bounds: RectF,          // 主体边界框
    val confidence: Float,       // 置信度
    val category: SubjectCategory, // 主体类别
    val trackingId: Int? = null  // 追踪ID（用于动态主体）
)

data class RectF(
    val left: Float,
    val top: Float,
    val right: Float,
    val bottom: Float
) {
    val width: Float get() = right - left
    val height: Float get() = bottom - top
    val centerX: Float get() = (left + right) / 2
    val centerY: Float get() = (top + bottom) / 2
}

enum class SubjectCategory {
    PERSON,
    PET,
    FOOD,
    ARCHITECTURE,
    VEHICLE,
    PLANT,
    OBJECT,
    UNKNOWN
}

/**
 * 构图建议
 */
data class CompositionGuide(
    val frameBounds: RectF,          // 推荐取景框
    val rule: CompositionRule,       // 构图规则
    val suggestedFocalLength: Float, // 推荐焦段
    val alignmentScore: Float = 0f,   // 对齐分数 (0-1)
    val gridLines: List<GridLine> = emptyList(), // 网格线
    val hints: List<CompositionHint> = emptyList() // 构图提示
)

data class GridLine(
    val start: PointF,
    val end: PointF,
    val type: GridLineType
)

data class PointF(val x: Float, val y: Float)

enum class GridLineType {
    HORIZONTAL,
    VERTICAL,
    DIAGONAL,
    GOLDEN_CURVE
}

data class CompositionHint(
    val message: String,
    val priority: HintPriority
)

enum class HintPriority {
    LOW,
    MEDIUM,
    HIGH
}

/**
 * 水平仪状态
 */
data class LevelState(
    val pitch: Float,      // 俯仰角 (度)
    val roll: Float,       // 横滚角 (度)
    val isLevel: Boolean,   // 是否水平
    val deviation: Float    // 偏差角度
)

/**
 * 滤镜类型
 */
enum class FilmFilter(
    val id: String,
    val displayName: String,
    val description: String,
    val recommendedScenes: List<SceneType>
) {
    KODAK_GOLD("kodak_gold", "柯达金", "温暖经典，色彩饱满", listOf(SceneType.PORTRAIT, SceneType.LANDSCAPE, SceneType.STREET)),
    KODAK_PORTRA("kodak_portra", "柯达Portra", "柔和自然，人像首选", listOf(SceneType.PORTRAIT)),
    FUJI_400H("fuji_400h", "富士400H", "清新淡雅，日系风格", listOf(SceneType.PORTRAIT, SceneType.LANDSCAPE)),
    AGFA_VISTA("agfa_vista", "爱克发Vista", "复古胶片质感", listOf(SceneType.STREET, SceneType.ARCHITECTURE)),
    ILFORD_HP5("ilford_hp5", "伊尔福HP5", "经典黑白，艺术感强", listOf(SceneType.ARCHITECTURE, SceneType.STREET)),
    CINESTILL("cinestill", "CineStill", "电影感色彩", listOf(SceneType.NIGHT, SceneType.STREET)),
    ORIGINAL("original", "原片", "无滤镜原始效果", listOf(SceneType.AUTO))
}

/**
 * 拍摄模式
 */
enum class CaptureMode {
    PHOTO,
    VIDEO
}

/**
 * 拍摄照片
 */
data class Photo(
    val id: String,
    val uri: String,
    val timestamp: Long,
    val sceneType: SceneType,
    val focalLength: Float,
    val filter: FilmFilter,
    val width: Int,
    val height: Int
)

/**
 * 设备姿态
 */
data class DevicePose(
    val rotationMatrix: FloatArray,
    val orientation: FloatArray, // azimuth, pitch, roll
    val timestamp: Long
) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false
        other as DevicePose
        if (!rotationMatrix.contentEquals(other.rotationMatrix)) return false
        if (!orientation.contentEquals(other.orientation)) return false
        if (timestamp != other.timestamp) return false
        return true
    }

    override fun hashCode(): Int {
        var result = rotationMatrix.contentHashCode()
        result = 31 * result + orientation.contentHashCode()
        result = 31 * result + timestamp.hashCode()
        return result
    }
}