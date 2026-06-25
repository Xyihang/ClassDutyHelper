# Add project specific ProGuard rules here.

# Keep AI model classes
-keep class com.aicamera.app.domain.model.** { *; }
-keep class com.aicamera.app.domain.ai.** { *; }

# Keep ML Kit related classes
-keep class com.google.mlkit.** { *; }
-keep class com.google.android.gms.internal.** { *; }

# Keep CameraX classes
-keep class androidx.camera.** { *; }

# Keep Compose related classes
-keep class androidx.compose.** { *; }
-keep class kotlin.** { *; }

# TensorFlow Lite
-keep class org.tensorflow.** { *; }
-keep class com.google.flatbuffers.** { *; }

# Optimization settings
-optimizationpasses 5
-dontusemixedcaseclassnames
-dontskipnonpubliclibraryclasses
-dontpreverify
-verbose

# Optimizations
-optimizations !code/simplification/arithmetic,!field/removal/writeonly,!class/merging/vertical,!class/merging/horizontal,!code/simplification/advanced

# Allow optimization
-allowaccessmodification

# Remove logging in release
-assumenosideeffects class android.util.Log {
    public static boolean isLoggable(java.lang.String, int);
    public static int v(...);
    public static int d(...);
    public static int i(...);
}