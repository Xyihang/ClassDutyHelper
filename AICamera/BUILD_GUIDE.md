# AI相机 - 本地构建指南

## 环境准备

### 1. 安装Android Studio
下载并安装最新版Android Studio（推荐 2023.1+ 版本）：
https://developer.android.com/studio

### 2. 安装必要组件
确保Android Studio中安装了：
- Android SDK 34 (Android 14)
- Android SDK Build-Tools 34.0.0
- Android SDK Platform-Tools
- Android SDK Command-line Tools
- NDK（可选，用于高级优化）

### 3. 配置环境变量
在 ~/.bashrc 或 ~/.zshrc 中添加：

```bash
export ANDROID_HOME=$HOME/Android/Sdk
export PATH=$PATH:$ANDROID_HOME/emulator
export PATH=$PATH:$ANDROID_HOME/tools
export PATH=$PATH:$ANDROID_HOME/tools/bin
export PATH=$PATH:$ANDROID_HOME/platform-tools
```

### 4. 安装Java JDK 17
确保Java版本为JDK 17：

```bash
# macOS
brew install openjdk@17

# Linux
sudo apt-get install openjdk-17-jdk

# Windows
下载并安装 Oracle JDK 17 或 OpenJDK 17
```

验证安装：
```bash
java -version  # 应显示 17.x.x
```

---

## 项目构建

### 方式一：使用Android Studio（推荐）

1. **导入项目**
   - 打开 Android Studio
   - 选择 `File > Open`
   - 选择 `/workspace/AICamera` 目录
   - 点击 `OK`

2. **同步项目**
   - Android Studio会自动提示同步Gradle
   - 点击 `Sync Now`
   - 等待依赖下载完成

3. **构建Debug版本**
   - 选择 `Build > Make Project (Ctrl+F9)`
   - 或点击工具栏的🔨图标
   - 等待构建完成

4. **生成APK**
   - 选择 `Build > Build Bundle(s) / APK(s) > Build APK(s)`
   - APK位置：`app/build/outputs/apk/debug/app-debug.apk`

### 方式二：使用命令行

```bash
cd /workspace/AICamera

# 初始化Gradle Wrapper
gradle wrapper

# 构建Debug版本
./gradlew assembleDebug

# 构建Release版本（需要签名配置）
./gradlew assembleRelease

# 清理项目
./gradlew clean

# 运行所有检查
./gradlew check

# 安装到连接的设备
./gradlew installDebug
```

---

## 签名配置（Release版本）

### 生成签名密钥

```bash
keytool -genkey -v -keystore aicamera-release.keystore \
  -alias aicamera \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

### 配置签名

在 `app/build.gradle.kts` 中添加：

```kotlin
android {
    signingConfigs {
        create("release") {
            storeFile = file("aicamera-release.keystore")
            storePassword = "your-store-password"
            keyAlias = "aicamera"
            keyPassword = "your-key-password"
        }
    }
    
    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("release")
            isMinifyEnabled = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
}
```

---

## 运行应用

### 在模拟器上运行

1. **创建模拟器**
   - 打开 Android Studio
   - 选择 `Tools > Device Manager`
   - 创建新设备（推荐 Pixel 5）
   - 选择系统镜像（Android 14 / API 34）

2. **运行应用**
   - 点击 ▶️ Run 按钮
   - 选择模拟器
   - 等待应用安装并启动

### 在真机上运行

1. **启用开发者选项**
   - 手机进入设置
   - 找到"关于手机"
   - 连续点击"版本号"7次
   - 进入"开发者选项"
   - 启用"USB调试"

2. **连接手机**
   - 使用USB线连接电脑
   - 手机上授权USB调试
   - Android Studio会自动识别设备

3. **运行应用**
   - 点击 ▶️ Run 按钮
   - 选择您的手机设备

---

## 测试功能

### 基本功能测试

1. **相机权限**
   - 首次启动会请求相机权限
   - 点击"允许"

2. **基本拍摄**
   - 点击快门按钮拍照
   - 检查照片是否保存成功

3. **AI构图**
   - 点击AI构图按钮（青绿色图标）
   - 观察AR取景框是否显示
   - 移动手机观察构图引导
   - 对齐时应有震动反馈

4. **滤镜切换**
   - 点击滤镜按钮
   - 在滤镜列表中滑动切换
   - 观察实时预览效果

### 场景识别测试

测试不同场景：
- 人像：对着人物拍摄
- 美食：拍摄食物
- 建筑：拍摄建筑物
- 风景：户外风景
- 宠物：拍摄猫狗等

观察：
- 场景类型是否正确识别
- 焦段是否自动调整
- 构图规则是否匹配

---

## 常见问题

### Q: Gradle同步失败
**A:** 
- 检查网络连接
- 清理Gradle缓存：`./gradlew clean`
- 删除 `.gradle` 目录重新同步
- 检查代理设置（如有）

### Q: 找不到Android SDK
**A:** 
- 在Android Studio中：`File > Project Structure > SDK Location`
- 设置正确的SDK路径
- 确保 `local.properties` 文件中配置了 `sdk.dir`

### Q: Kotlin编译错误
**A:** 
- 确保JDK版本为17
- 检查Kotlin插件版本
- 清理项目重新构建

### Q: CameraX初始化失败
**A:** 
- 检查相机权限
- 检查设备是否支持CameraX
- 查看 `CameraManager` 日志

### Q: ML Kit加载失败
**A:** 
- 首次使用需下载模型（自动）
- 检查网络连接
- 确保设备存储空间充足

---

## 性能优化建议

### 减小APK体积

1. **启用资源压缩**
   ```kotlin
   android {
       buildTypes {
           release {
               isShrinkResources = true
           }
       }
   }
   ```

2. **使用WebP格式图片**
   - 替换PNG资源为WebP
   - 减少资源大小

3. **移除未使用的依赖**
   - 定期检查依赖库
   - 移除不必要的库

### 提升运行速度

1. **启用R8优化**
   ```kotlin
   android {
       buildTypes {
           release {
               isMinifyEnabled = true
           }
       }
   }
   ```

2. **配置ProGuard规则**
   - 保留必要的类
   - 优化其他代码

---

## 发布准备

### 检查清单

- [ ] 所有功能测试完成
- [ ] 性能达标（30fps AI检测）
- [ ] APK体积 < 200MB
- [ ] 兼容性测试（多机型）
- [ ] 权限说明完整
- [ ] 签名配置正确
- [ ] 版本号更新

### 版本管理

在 `app/build.gradle.kts` 中：

```kotlin
android {
    defaultConfig {
        versionCode = 2  // 每次发布递增
        versionName = "1.1.0"
    }
}
```

---

## 获取帮助

- Android开发文档：https://developer.android.com
- CameraX文档：https://developer.android.com/training/camerax
- ML Kit文档：https://developers.google.com/ml-kit
- Kotlin文档：https://kotlinlang.org/docs

---

祝您构建成功！如有问题，请参考产品架构文档或查看代码注释。