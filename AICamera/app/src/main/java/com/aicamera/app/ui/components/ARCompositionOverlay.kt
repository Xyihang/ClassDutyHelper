package com.aicamera.app.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.graphics.*
import androidx.compose.ui.graphics.drawscope.Stroke
import com.aicamera.app.domain.model.*
import com.aicamera.app.ui.theme.*

/**
 * AR构图引导覆盖层
 * 显示取景框、网格线、水平仪等引导元素
 */
@Composable
fun ARCompositionOverlay(
    modifier: Modifier = Modifier,
    compositionGuide: CompositionGuide?,
    levelState: LevelState?,
    alignmentProgress: Float = 0f,
    showGrid: Boolean = true,
    showLevel: Boolean = true,
    onAlignmentComplete: () -> Unit = {}
) {
    Canvas(
        modifier = modifier.fillMaxSize()
    ) {
        // 绘制网格线
        if (showGrid && compositionGuide?.gridLines != null) {
            drawGridLines(compositionGuide.gridLines)
        }
        
        // 绘制取景框
        compositionGuide?.let { guide ->
            drawCompositionFrame(guide.frameBounds, alignmentProgress)
        }
        
        // 绘制水平仪
        if (showLevel && levelState != null) {
            drawLevelIndicator(levelState)
        }
        
        // 对齐完成效果
        if (alignmentProgress >= 1f) {
            drawAlignmentSuccessEffect()
        }
    }
}

/**
 * 绘制网格线
 */
private fun DrawScope.drawGridLines(gridLines: List<GridLine>) {
    val canvasWidth = size.width
    val canvasHeight = size.height
    
    gridLines.forEach { line ->
        val startX = line.start.x * canvasWidth
        val startY = line.start.y * canvasHeight
        val endX = line.end.x * canvasWidth
        val endY = line.end.y * canvasHeight
        
        drawLine(
            color = ARGridLine,
            start = Offset(startX, startY),
            end = Offset(endX, endY),
            strokeWidth = 1.dp.toPx(),
            pathEffect = PathEffect.dashPathEffect(
                floatArrayOf(10f, 10f),
                0f
            )
        )
    }
}

/**
 * 绘制构图取景框
 */
private fun DrawScope.drawCompositionFrame(
    frameBounds: RectF,
    alignmentProgress: Float
) {
    val canvasWidth = size.width
    val canvasHeight = size.height
    
    // 转换为屏幕坐标
    val left = frameBounds.left * canvasWidth
    val top = frameBounds.top * canvasHeight
    val right = frameBounds.right * canvasWidth
    val bottom = frameBounds.bottom * canvasHeight
    
    val rect = Rect(left, top, right, bottom)
    
    // 根据对齐进度改变颜色
    val frameColor = when {
        alignmentProgress >= 1f -> ARFrameAligned
        alignmentProgress >= 0.7f -> Color(0xFF00FF99)
        alignmentProgress >= 0.4f -> Color(0xFFFFFFAA)
        else -> ARFrameNormal
    }
    
    val strokeWidth = when {
        alignmentProgress >= 1f -> 3.dp.toPx()
        alignmentProgress >= 0.7f -> 2.5.dp.toPx()
        else -> 2.dp.toPx()
    }
    
    // 绘制主框
    drawRect(
        color = frameColor,
        topLeft = Offset(left, top),
        size = androidx.compose.ui.geometry.Size(right - left, bottom - top),
        style = Stroke(width = strokeWidth)
    )
    
    // 绘制角标记（增强视觉引导）
    val cornerLength = 20.dp.toPx()
    
    // 左上角
    drawLine(frameColor, Offset(left, top), Offset(left + cornerLength, top), strokeWidth)
    drawLine(frameColor, Offset(left, top), Offset(left, top + cornerLength), strokeWidth)
    
    // 右上角
    drawLine(frameColor, Offset(right - cornerLength, top), Offset(right, top), strokeWidth)
    drawLine(frameColor, Offset(right, top), Offset(right, top + cornerLength), strokeWidth)
    
    // 左下角
    drawLine(frameColor, Offset(left, bottom - cornerLength), Offset(left, bottom), strokeWidth)
    drawLine(frameColor, Offset(left, bottom), Offset(left + cornerLength, bottom), strokeWidth)
    
    // 右下角
    drawLine(frameColor, Offset(right - cornerLength, bottom), Offset(right, bottom), strokeWidth)
    drawLine(frameColor, Offset(right, bottom - cornerLength), Offset(right, bottom), strokeWidth)
    
    // 对齐进度指示器（进度条）
    if (alignmentProgress > 0f && alignmentProgress < 1f) {
        val progressWidth = (right - left) * alignmentProgress
        drawRect(
            color = ARFrameAligned.copy(alpha = 0.5f),
            topLeft = Offset(left, bottom + 8.dp.toPx()),
            size = androidx.compose.ui.geometry.Size(progressWidth, 4.dp.toPx())
        )
    }
}

/**
 * 绘制水平仪指示器
 */
private fun DrawScope.drawLevelIndicator(levelState: LevelState) {
    val canvasWidth = size.width
    val canvasHeight = size.height
    
    // 水平仪位置 - 屏幕顶部中央
    val centerX = canvasWidth / 2
    val centerY = 60.dp.toPx()
    
    val indicatorWidth = 100.dp.toPx()
    val indicatorHeight = 20.dp.toPx()
    
    // 背景
    drawRect(
        color = OverlayDark,
        topLeft = Offset(centerX - indicatorWidth / 2, centerY - indicatorHeight / 2),
        size = androidx.compose.ui.geometry.Size(indicatorWidth, indicatorHeight),
        cornerRadius = 4.dp.toPx()
    )
    
    // 水平线基准
    drawLine(
        color = Color.White.copy(alpha = 0.3f),
        start = Offset(centerX - indicatorWidth / 2 + 10.dp.toPx(), centerY),
        end = Offset(centerX + indicatorWidth / 2 - 10.dp.toPx(), centerY),
        strokeWidth = 1.dp.toPx()
    )
    
    // 当前倾斜角度指示
    val rollOffset = levelState.roll * 5f // 角度到偏移的映射
    
    val indicatorColor = if (levelState.isLevel) {
        Success
    } else {
        Warning
    }
    
    // 倾斜指示线
    drawLine(
        color = indicatorColor,
        start = Offset(centerX - indicatorWidth / 2 + 10.dp.toPx() + rollOffset, centerY),
        end = Offset(centerX + indicatorWidth / 2 - 10.dp.toPx() + rollOffset, centerY),
        strokeWidth = 2.dp.toPx()
    )
    
    // 中心标记
    drawCircle(
        color = if (levelState.isLevel) Success else Color.White.copy(alpha = 0.5f),
        center = Offset(centerX, centerY),
        radius = 3.dp.toPx()
    )
    
    // 倾斜角度数字显示
    if (!levelState.isLevel) {
        drawContext.canvas.nativeCanvas.apply {
            val text = "${String.format("%.1f", levelState.deviation)}°"
            val paint = android.graphics.Paint().apply {
                color = indicatorColor.toArgb()
                textSize = 12.sp.toPx()
                textAlign = android.graphics.Paint.Align.CENTER
            }
            drawText(text, centerX, centerY + indicatorHeight + 10.dp.toPx(), paint)
        }
    }
}

/**
 * 绘制对齐成功效果
 */
private fun DrawScope.drawAlignmentSuccessEffect() {
    val canvasWidth = size.width
    val canvasHeight = size.height
    
    // 中心闪烁效果
    drawCircle(
        brush = Brush.radialGradient(
            colors = listOf(
                ARFrameAligned.copy(alpha = 0.3f),
                ARFrameAligned.copy(alpha = 0f)
            ),
            center = Offset(canvasWidth / 2, canvasHeight / 2),
            radius = 50.dp.toPx()
        ),
        center = Offset(canvasWidth / 2, canvasHeight / 2),
        radius = 50.dp.toPx()
    )
    
    // 成功文字提示
    drawContext.canvas.nativeCanvas.apply {
        val text = "构图完美"
        val paint = android.graphics.Paint().apply {
            color = ARFrameAligned.toArgb()
            textSize = 18.sp.toPx()
            textAlign = android.graphics.Paint.Align.CENTER
            typeface = android.graphics.Typeface.DEFAULT_BOLD
        }
        drawText(text, canvasWidth / 2, canvasHeight / 2, paint)
    }
}

/**
 * 绘制主体追踪框（用于动态主体）
 */
@Composable
fun SubjectTrackingOverlay(
    modifier: Modifier = Modifier,
    subjects: List<SubjectDetection>,
    selectedSubjectId: Int? = null
) {
    Canvas(
        modifier = modifier.fillMaxSize()
    ) {
        val canvasWidth = size.width
        val canvasHeight = size.height
        
        subjects.forEachIndexed { index, subject ->
            val left = subject.bounds.left * canvasWidth
            val top = subject.bounds.top * canvasHeight
            val right = subject.bounds.right * canvasWidth
            val bottom = subject.bounds.bottom * canvasHeight
            
            val isSelected = subject.trackingId == selectedSubjectId || 
                (selectedSubjectId == null && index == 0)
            
            val boxColor = if (isSelected) {
                ARFrameAligned
            } else {
                ARFrameNormal.copy(alpha = 0.5f)
            }
            
            // 绘制主体框
            drawRect(
                color = boxColor,
                topLeft = Offset(left, top),
                size = androidx.compose.ui.geometry.Size(right - left, bottom - top),
                style = Stroke(width = if (isSelected) 3.dp.toPx() else 2.dp.toPx())
            )
            
            // 显示主体类别标签
            val label = when (subject.category) {
                SubjectCategory.PERSON -> "人物"
                SubjectCategory.PET -> "宠物"
                SubjectCategory.FOOD -> "美食"
                SubjectCategory.ARCHITECTURE -> "建筑"
                SubjectCategory.VEHICLE -> "车辆"
                SubjectCategory.PLANT -> "植物"
                else -> "物体"
            }
            
            drawContext.canvas.nativeCanvas.apply {
                val paint = android.graphics.Paint().apply {
                    color = boxColor.toArgb()
                    textSize = 12.sp.toPx()
                    textAlign = android.graphics.Paint.Align.LEFT
                }
                drawText(label, left, top - 5.dp.toPx(), paint)
            }
        }
    }
}