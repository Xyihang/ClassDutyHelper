package com.aicamera.app.ui.screens

import android.Manifest
import android.content.pm.PackageManager
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.camera.view.PreviewView
import androidx.core.content.ContextCompat
import androidx.lifecycle.viewmodel.compose.viewModel
import com.aicamera.app.domain.camera.CameraManager
import com.aicamera.app.domain.model.*
import com.aicamera.app.ui.components.ARCompositionOverlay
import com.aicamera.app.ui.components.SubjectTrackingOverlay
import com.aicamera.app.ui.theme.AICameraTheme
import com.aicamera.app.ui.theme.Accent
import com.aicamera.app.ui.theme.OverlayDark

/**
 * 主界面 - 相机拍摄界面
 */
class MainActivity : ComponentActivity() {
    
    private var cameraManager: CameraManager? = null
    
    private val permissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { permissions ->
        val allGranted = permissions.all { it.value }
        if (allGranted) {
            initializeCamera()
        }
    }
    
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        
        checkPermissions()
        
        setContent {
            AICameraTheme {
                val viewModel: CameraViewModel = viewModel()
                
                // 初始化 CameraManager 并注入 ViewModel
                LaunchedEffect(cameraManager) {
                    cameraManager?.let { cm ->
                        viewModel.initCameraManager(cm)
                    }
                }
                
                CameraScreen(
                    viewModel = viewModel,
                    cameraManager = cameraManager
                )
            }
        }
    }
    
    private fun checkPermissions() {
        val requiredPermissions = arrayOf(
            Manifest.permission.CAMERA,
            Manifest.permission.RECORD_AUDIO,
            Manifest.permission.WRITE_EXTERNAL_STORAGE
        )
        
        val allGranted = requiredPermissions.all {
            ContextCompat.checkSelfPermission(this, it) == PackageManager.PERMISSION_GRANTED
        }
        
        if (allGranted) {
            initializeCamera()
        } else {
            permissionLauncher.launch(requiredPermissions)
        }
    }
    
    private fun initializeCamera() {
        cameraManager = CameraManager(this, this)
    }
    
    override fun onDestroy() {
        super.onDestroy()
        cameraManager?.release()
    }
}

/**
 * 相机拍摄界面
 */
@Composable
fun CameraScreen(
    viewModel: CameraViewModel,
    cameraManager: CameraManager?
) {
    val context = LocalContext.current
    
    // 从 ViewModel 收集状态
    val uiState by viewModel.uiState.collectAsState()
    val compositionState by viewModel.compositionState.collectAsState()
    val captureState by viewModel.captureState.collectAsState()
    
    // UI状态
    var showGrid by remember { mutableStateOf(true) }
    var showLevel by remember { mutableStateOf(true) }
    var captureMode by remember { mutableStateOf(CaptureMode.PHOTO) }
    
    // 滤镜列表（使用 entries 替代弃用的 values()）
    val filters = FilmFilter.entries.toList()
    var filterIndex by remember { mutableStateOf(0) }
    var currentFilter by remember { mutableStateOf(FilmFilter.ORIGINAL) }
    
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Black)
    ) {
        // 相机预览
        AndroidView(
            factory = { ctx ->
                PreviewView(ctx).apply {
                    cameraManager?.initialize(this)
                }
            },
            modifier = Modifier.fillMaxSize()
        )
        
        // AR构图引导覆盖层
        if (compositionState.aiEnabled) {
            ARCompositionOverlay(
                modifier = Modifier.fillMaxSize(),
                compositionGuide = compositionState.guide,
                levelState = compositionState.levelState,
                alignmentProgress = compositionState.alignmentProgress,
                showGrid = showGrid,
                showLevel = showLevel,
                onAlignmentComplete = {
                    // 震动反馈由 ViewModel 处理
                }
            )
            
            // 主体追踪框
            if (compositionState.subjects.isNotEmpty()) {
                SubjectTrackingOverlay(
                    modifier = Modifier.fillMaxSize(),
                    subjects = compositionState.subjects
                )
            }
        }
        
        // 顶部状态栏
        TopStatusBar(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 8.dp)
                .align(Alignment.TopCenter),
            currentFocalLength = cameraManager?.currentFocalLength?.collectAsState()?.value ?: 1.0f,
            showGrid = showGrid,
            showLevel = showLevel,
            aiEnabled = compositionState.aiEnabled,
            sceneType = compositionState.sceneType,
            onGridToggle = { showGrid = !showGrid },
            onLevelToggle = { showLevel = !showLevel }
        )
        
        // 底部操作栏
        BottomControlBar(
            modifier = Modifier
                .fillMaxWidth()
                .align(Alignment.BottomCenter)
                .padding(bottom = 32.dp),
            currentFilter = currentFilter,
            aiEnabled = compositionState.aiEnabled,
            captureMode = captureMode,
            onFilterClick = { 
                filterIndex = (filterIndex + 1) % filters.size
                currentFilter = filters[filterIndex]
                viewModel.applyFilter(currentFilter)
            },
            onCaptureClick = { viewModel.capturePhoto() },
            onAIClick = { viewModel.toggleAiComposition(!compositionState.aiEnabled) },
            onGalleryClick = { /* 打开相册 */ },
            onModeChange = { mode -> captureMode = mode }
        )
        
        // 滤镜选择器（横向滚动）
        if (!compositionState.aiEnabled) {
            FilterSelector(
                modifier = Modifier
                    .fillMaxWidth()
                    .align(Alignment.BottomCenter)
                    .offset(y = (-120).dp),
                filters = filters,
                currentIndex = filterIndex,
                onSelect = { index ->
                    filterIndex = index
                    currentFilter = filters[index]
                    viewModel.applyFilter(currentFilter)
                }
            )
        }
        
        // AI分析结果提示
        if (compositionState.aiEnabled && compositionState.guide != null) {
            CompositionHintsPanel(
                modifier = Modifier
                    .fillMaxWidth()
                    .align(Alignment.TopCenter)
                    .offset(y = 100.dp),
                guide = compositionState.guide!!
            )
        }
        
        // 场景识别提示
        if (compositionState.aiEnabled && compositionState.sceneType != SceneType.AUTO) {
            SceneIndicator(
                modifier = Modifier
                    .align(Alignment.TopCenter)
                    .offset(y = 150.dp),
                sceneType = compositionState.sceneType
            )
        }
        
        // 拍摄状态提示
        when (captureState) {
            is CaptureState.Capturing -> {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .background(Color.Black.copy(alpha = 0.3f)),
                    contentAlignment = Alignment.Center
                ) {
                    CircularProgressIndicator(color = Accent)
                }
            }
            is CaptureState.Success -> {
                // 短暂显示成功提示
            }
            is CaptureState.Error -> {
                // 显示错误提示
            }
            CaptureState.Idle -> {}
        }
    }
}

/**
 * 顶部状态栏
 */
@Composable
fun TopStatusBar(
    modifier: Modifier = Modifier,
    currentFocalLength: Float,
    showGrid: Boolean,
    showLevel: Boolean,
    aiEnabled: Boolean,
    sceneType: SceneType,
    onGridToggle: () -> Unit,
    onLevelToggle: () -> Unit
) {
    Row(
        modifier = modifier,
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        // 焦段显示
        FocalLengthIndicator(focalLength = currentFocalLength)
        
        // 场景类型（AI启用时显示）
        if (aiEnabled && sceneType != SceneType.AUTO) {
            Text(
                text = sceneType.displayName,
                color = Color.White,
                style = MaterialTheme.typography.labelLarge
            )
        }
        
        // 网格和水平仪开关
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            IconButton(
                onClick = onGridToggle,
                modifier = Modifier
                    .size(36.dp)
                    .background(
                        color = if (showGrid) OverlayDark else OverlayDark.copy(alpha = 0.5f),
                        shape = CircleShape
                    )
            ) {
                Icon(
                    Icons.Default.GridOn,
                    contentDescription = "网格",
                    tint = if (showGrid) Color.White else Color.White.copy(alpha = 0.5f)
                )
            }
            
            IconButton(
                onClick = onLevelToggle,
                modifier = Modifier
                    .size(36.dp)
                    .background(
                        color = if (showLevel) OverlayDark else OverlayDark.copy(alpha = 0.5f),
                        shape = CircleShape
                    )
            ) {
                Icon(
                    Icons.Default.CompareArrows,
                    contentDescription = "水平仪",
                    tint = if (showLevel) Color.White else Color.White.copy(alpha = 0.5f)
                )
            }
        }
    }
}

/**
 * 焦段指示器
 */
@Composable
fun FocalLengthIndicator(focalLength: Float) {
    Box(
        modifier = Modifier
            .background(OverlayDark, RoundedCornerShape(4.dp))
            .padding(horizontal = 8.dp, vertical = 4.dp)
    ) {
        Text(
            text = "${focalLength}x",
            color = Color.White,
            style = MaterialTheme.typography.titleMedium
        )
    }
}

/**
 * 底部控制栏
 */
@Composable
fun BottomControlBar(
    modifier: Modifier = Modifier,
    currentFilter: FilmFilter,
    aiEnabled: Boolean,
    captureMode: CaptureMode,
    onFilterClick: () -> Unit,
    onCaptureClick: () -> Unit,
    onAIClick: () -> Unit,
    onGalleryClick: () -> Unit,
    onModeChange: (CaptureMode) -> Unit
) {
    Row(
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = 32.dp),
        horizontalArrangement = Arrangement.SpaceEvenly,
        verticalAlignment = Alignment.CenterVertically
    ) {
        // 滤镜按钮
        IconButton(onClick = onFilterClick, modifier = Modifier.size(48.dp)) {
            Icon(
                Icons.Default.Filter,
                contentDescription = "滤镜",
                tint = Color.White,
                modifier = Modifier.size(28.dp)
            )
        }
        
        // 快门按钮
        ShutterButton(onClick = onCaptureClick, captureMode = captureMode)
        
        // AI构图按钮
        IconButton(
            onClick = onAIClick,
            modifier = Modifier
                .size(48.dp)
                .background(
                    color = if (aiEnabled) Accent else OverlayDark.copy(alpha = 0.5f),
                    shape = CircleShape
                )
        ) {
            Icon(
                Icons.Default.AutoFixHigh,
                contentDescription = "AI构图",
                tint = if (aiEnabled) Color.White else Color.White.copy(alpha = 0.7f),
                modifier = Modifier.size(28.dp)
            )
        }
        
        // 相册按钮
        IconButton(onClick = onGalleryClick, modifier = Modifier.size(48.dp)) {
            Icon(
                Icons.Default.PhotoLibrary,
                contentDescription = "相册",
                tint = Color.White,
                modifier = Modifier.size(28.dp)
            )
        }
    }
}

/**
 * 快门按钮
 */
@Composable
fun ShutterButton(onClick: () -> Unit, captureMode: CaptureMode) {
    Box(
        modifier = Modifier
            .size(72.dp)
            .background(Color.White, CircleShape)
            .clip(CircleShape)
    ) {
        IconButton(onClick = onClick, modifier = Modifier.fillMaxSize()) {
            if (captureMode == CaptureMode.PHOTO) {
                Box(
                    modifier = Modifier
                        .size(60.dp)
                        .background(Color.White, CircleShape)
                        .padding(4.dp)
                        .background(Color.Black, CircleShape)
                )
            } else {
                Icon(
                    Icons.Default.Videocam,
                    contentDescription = "录像",
                    tint = Color.Red,
                    modifier = Modifier.size(32.dp)
                )
            }
        }
    }
}

/**
 * 滤镜选择器
 */
@Composable
fun FilterSelector(
    modifier: Modifier = Modifier,
    filters: List<FilmFilter>,
    currentIndex: Int,
    onSelect: (Int) -> Unit
) {
    Row(
        modifier = modifier.padding(horizontal = 16.dp),
        horizontalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        filters.forEachIndexed { index, filter ->
            FilterItem(
                filter = filter,
                isSelected = index == currentIndex,
                onClick = { onSelect(index) }
            )
        }
    }
}

/**
 * 滤镜项
 */
@Composable
fun FilterItem(filter: FilmFilter, isSelected: Boolean, onClick: () -> Unit) {
    Column(
        modifier = Modifier
            .width(60.dp)
            .clickable { onClick() },
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(
            modifier = Modifier
                .size(48.dp)
                .background(
                    color = if (isSelected) Accent else OverlayDark.copy(alpha = 0.5f),
                    shape = RoundedCornerShape(8.dp)
                )
        ) {
            Text(
                text = filter.displayName,
                color = Color.White,
                style = MaterialTheme.typography.labelSmall,
                modifier = Modifier.align(Alignment.Center)
            )
        }
        
        if (isSelected) {
            Text(
                text = filter.description,
                color = Color.White.copy(alpha = 0.7f),
                style = MaterialTheme.typography.bodySmall,
                modifier = Modifier.padding(top = 4.dp)
            )
        }
    }
}

/**
 * 构图提示面板
 */
@Composable
fun CompositionHintsPanel(modifier: Modifier = Modifier, guide: CompositionGuide) {
    if (guide.hints.isNotEmpty()) {
        Box(
            modifier = modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp)
                .background(OverlayDark, RoundedCornerShape(8.dp))
                .padding(12.dp)
        ) {
            Text(
                text = guide.hints[0].message,
                color = Color.White,
                style = MaterialTheme.typography.bodyMedium
            )
        }
    }
}

/**
 * 场景指示器
 */
@Composable
fun SceneIndicator(modifier: Modifier = Modifier, sceneType: SceneType) {
    Box(
        modifier = modifier
            .background(Accent.copy(alpha = 0.8f), RoundedCornerShape(16.dp))
            .padding(horizontal = 16.dp, vertical = 8.dp)
    ) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Icon(
                Icons.Default.PhotoCamera,
                contentDescription = null,
                tint = Color.White,
                modifier = Modifier.size(16.dp)
            )
            Text(
                text = sceneType.displayName,
                color = Color.White,
                style = MaterialTheme.typography.labelLarge
            )
        }
    }
}