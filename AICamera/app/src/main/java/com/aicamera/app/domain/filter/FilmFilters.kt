package com.aicamera.app.domain.filter

import com.aicamera.app.domain.model.FilmFilter
import com.aicamera.app.domain.model.SceneType

/**
 * 滤镜系统
 * 提供胶片风格滤镜效果
 */
object FilmFilters {
    
    // 滤镜色彩参数
    data class FilterParams(
        val warmth: Float = 0f,       // 温暖度 (-1 to 1)
        val saturation: Float = 0f,    // 饱和度 (-1 to 1)
        val contrast: Float = 0f,      // 对比度 (-1 to 1)
        val shadows: Float = 0f,       // 阴影调整
        val highlights: Float = 0f,    // 高光调整
        val grain: Float = 0f,         // 颗粒感
        val fade: Float = 0f,          // 衰减
        val redShift: Float = 0f,      // 红色偏移
        val greenShift: Float = 0f,    // 绿色偏移
        val blueShift: Float = 0f      // 蓝色偏移
    )
    
    val FILTER_PARAMS = mapOf(
        FilmFilter.ORIGINAL to FilterParams(),
        
        FilmFilter.KODAK_GOLD to FilterParams(
            warmth = 0.15f,
            saturation = 0.2f,
            contrast = 0.1f,
            shadows = 0.05f,
            grain = 0.15f,
            redShift = 0.08f,
            greenShift = -0.02f
        ),
        
        FilmFilter.KODAK_PORTRA to FilterParams(
            warmth = 0.05f,
            saturation = -0.1f,
            contrast = -0.05f,
            fade = 0.1f,
            grain = 0.08f,
            redShift = 0.05f,
            blueShift = -0.03f
        ),
        
        FilmFilter.FUJI_400H to FilterParams(
            warmth = -0.05f,
            saturation = -0.15f,
            contrast = -0.08f,
            fade = 0.15f,
            grain = 0.1f,
            greenShift = 0.05f,
            blueShift = 0.08f
        ),
        
        FilmFilter.AGFA_VISTA to FilterParams(
            warmth = 0.1f,
            saturation = 0.15f,
            contrast = 0.12f,
            shadows = 0.1f,
            grain = 0.18f,
            redShift = 0.06f
        ),
        
        FilmFilter.ILFORD_HP5 to FilterParams(
            saturation = -1f,  // 黑白
            contrast = 0.2f,
            grain = 0.25f,
            fade = 0.1f
        ),
        
        FilmFilter.CINESTILL to FilterParams(
            warmth = 0.08f,
            saturation = 0.1f,
            contrast = 0.15f,
            shadows = 0.12f,
            highlights = -0.08f,
            grain = 0.12f,
            redShift = 0.1f
        )
    )
    
    /**
     * 根据场景推荐滤镜
     */
    fun getRecommendedFilter(sceneType: SceneType): FilmFilter {
        return when (sceneType) {
            SceneType.PORTRAIT -> FilmFilter.KODAK_PORTRA
            SceneType.LANDSCAPE -> FilmFilter.KODAK_GOLD
            SceneType.FOOD -> FilmFilter.KODAK_GOLD
            SceneType.ARCHITECTURE -> FilmFilter.ILFORD_HP5
            SceneType.STREET -> FilmFilter.AGFA_VISTA
            SceneType.NIGHT -> FilmFilter.CINESTILL
            SceneType.PET -> FilmFilter.FUJI_400H
            SceneType.AUTO -> FilmFilter.ORIGINAL
        }
    }
    
    /**
     * 获取滤镜参数
     */
    fun getParams(filter: FilmFilter): FilterParams {
        return FILTER_PARAMS[filter] ?: FilterParams()
    }
}