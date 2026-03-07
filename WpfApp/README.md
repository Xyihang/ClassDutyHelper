# 班级值日助手 - 完整技术文档

## 📋 目录

- [1. 项目概述](#1-项目概述)
- [2. 功能特性](#2-功能特性)
- [3. 技术架构](#3-技术架构)
- [4. 项目结构](#4-项目结构)
- [5. 核心模块说明](#5-核心模块说明)
- [6. 数据模型](#6-数据模型)
- [7. 用户界面](#7-用户界面)
- [8. 安装和部署](#8-安装和部署)
- [9. 使用指南](#9-使用指南)
- [10. 开发指南](#10-开发指南)
- [11. 配置说明](#11-配置说明)
- [12. 故障排除](#12-故障排除)

---

## 1. 项目概述

### 1.1 项目简介

班级值日助手是一款基于 WPF（Windows Presentation Foundation）开发的桌面应用程序，旨在帮助班级管理者高效地管理学生的值日安排。该应用程序提供了完整的值日管理功能，包括学生管理、项目管理、排班管理、提醒功能、轮播内容管理以及悬浮窗显示等。

### 1.2 项目信息

| 项目属性 | 值 |
|---------|-----|
| 项目名称 | 班级值日助手 (ClassDutyHelper) |
| 版本号 | 1.0.0 |
| 开发语言 | C# |
| UI 框架 | WPF |
| 目标框架 | .NET 10.0 Windows |
| 数据库 | SQLite (Entity Framework Core) |
| 许可证 | 待定 |

### 1.3 项目目标

- 提供简单易用的值日管理界面
- 支持灵活的排班方式（自动排班、手动排班）
- 实时显示当前值日信息
- 支持值日提醒功能
- 提供数据导入导出功能
- 支持云端数据同步
- 支持轮播内容管理

---

## 2. 功能特性

### 2.1 核心功能

#### 2.1.1 学生管理
- 添加、编辑、删除学生信息
- 设置学生是否启用（参与排班）
- 支持批量导入学生数据

#### 2.1.2 项目管理
- 添加、编辑、删除值日项目
- 设置项目名称和描述
- 管理项目与时段的关联

#### 2.1.3 排班管理
- **自动排班**：根据学生和项目自动生成排班
- **手动排班**：手动指定特定日期的值日生
- **排班预览**：在排班前预览排班结果
- **智能跳过**：支持跳过周末、节假日和自定义日期
- **排班历史**：查看历史排班记录

#### 2.1.4 时段管理
- 添加、编辑、删除时段
- 设置时段名称和时间段
- 管理时段与项目的关联

#### 2.1.5 提醒功能
- 添加、编辑、删除提醒
- 设置提醒时间和内容
- 弹窗提醒功能

#### 2.1.6 轮播内容管理
- 添加、编辑、删除轮播内容
- 设置轮播内容和排序
- 启用/禁用轮播项
- 支持系统默认项

#### 2.1.7 悬浮窗
- 实时显示当前值日信息
- 支持多种显示位置（顶部、底部、左侧、右侧）
- 可调节透明度和高度
- 点击悬浮窗可打开主窗口
- 左右位置时不显示时间模块

### 2.2 高级功能

#### 2.2.1 数据导入导出
- 支持 Excel 格式的数据导入
- 支持导出值日记录到 Excel

#### 2.2.2 云端同步
- 支持与云端表单同步数据
- 自动同步功能
- 同步日志记录

#### 2.2.3 系统设置
- 班级名称设置
- 顶栏显示设置（透明度、高度、位置）
- 开机启动设置
- 窗口置顶设置
- 启动时隐藏主窗口设置

---

## 3. 技术架构

### 3.1 技术栈

| 技术组件 | 版本 | 用途 |
|---------|------|------|
| .NET | 10.0 | 运行时框架 |
| WPF | - | UI 框架 |
| Entity Framework Core | 9.0.0 | ORM 框架 |
| SQLite | - | 数据库 |
| EPPlus | 7.0.0 | Excel 操作 |
| CommunityToolkit.Mvvm | 8.2.2 | MVVM 工具包 |
| Hardcodet.NotifyIcon.Wpf | 1.1.0 | 系统托盘图标 |

### 3.2 架构模式

应用程序采用 **MVVM（Model-View-ViewModel）** 架构模式：

- **Model（模型）**：数据模型和业务逻辑
- **View（视图）**：用户界面（XAML）
- **ViewModel（视图模型）**：连接 Model 和 View 的桥梁

### 3.3 数据库设计

使用 SQLite 作为本地数据库，通过 Entity Framework Core 进行数据访问。

#### 3.3.1 数据库表结构

| 表名 | 说明 |
|------|------|
| Students | 学生信息表 |
| DutyProjects | 值日项目表 |
| DutyRecords | 值日记录表 |
| Reminders | 提醒表 |
| TimeSlots | 时段表 |
| TimeSlotProjects | 时段项目关联表 |
| AppSettings | 应用设置表 |
| SyncLogs | 同步日志表 |
| CarouselItems | 轮播内容表 |

---

## 4. 项目结构

```
WpfApp/
├── Helpers/                    # 辅助工具类
│   ├── StartupHelper.cs       # 开机启动辅助类
│   └── WindowHelper.cs       # 窗口辅助类
├── Models/                    # 数据模型
│   ├── AppSettings.cs        # 应用设置模型
│   ├── CarouselItem.cs       # 轮播内容模型
│   ├── DutyProject.cs        # 值日项目模型
│   ├── DutyRecord.cs         # 值日记录模型
│   ├── Reminder.cs          # 提醒模型
│   ├── Student.cs           # 学生模型
│   ├── SyncLog.cs           # 同步日志模型
│   └── TimeSlotProject.cs   # 时段项目关联模型
├── Services/                  # 服务层
│   ├── AppDbContext.cs       # 数据库上下文
│   ├── DataService.cs       # 数据服务
│   ├── ExcelService.cs      # Excel 服务
│   ├── ReminderService.cs   # 提醒服务
│   └── SyncService.cs       # 同步服务
├── Views/                     # 视图层
│   ├── Pages/               # 页面
│   │   ├── DutyPage.xaml      # 值日页面
│   │   └── HistoryPage.xaml   # 历史页面
│   ├── MainWindow.xaml      # 主窗口
│   ├── MainWindow.xaml.cs   # 主窗口代码
│   ├── TopBarWindow.xaml    # 顶栏窗口
│   ├── TopBarWindow.xaml.cs # 顶栏窗口代码
│   ├── StudentDialog.xaml   # 学生对话框
│   ├── StudentDialog.xaml.cs # 学生对话框代码
│   ├── ProjectDialog.xaml   # 项目对话框
│   ├── ProjectDialog.xaml.cs # 项目对话框代码
│   ├── AutoScheduleDialog.xaml # 自动排班对话框
│   ├── AutoScheduleDialog.xaml.cs # 自动排班对话框代码
│   ├── ReminderDialog.xaml  # 提醒对话框
│   ├── ReminderDialog.xaml.cs # 提醒对话框代码
│   ├── TimeSlotDialog.xaml  # 时段对话框
│   ├── TimeSlotDialog.xaml.cs # 时段对话框代码
│   ├── ScheduleDialog.xaml  # 排班对话框
│   ├── ScheduleDialog.xaml.cs # 排班对话框代码
│   ├── DateRangeDialog.xaml # 日期范围对话框
│   ├── DateRangeDialog.xaml.cs # 日期范围对话框代码
│   ├── ImportColumnDialog.xaml # 导入列对话框
│   ├── ImportColumnDialog.xaml.cs # 导入列对话框代码
│   ├── CarouselDialog.xaml # 轮播内容对话框
│   ├── CarouselDialog.xaml.cs # 轮播内容对话框代码
│   ├── EditCarouselDialog.xaml # 编辑轮播内容对话框
│   ├── EditCarouselDialog.xaml.cs # 编辑轮播内容对话框代码
│   └── ReminderNotificationWindow.xaml # 提醒通知窗口
├── App.xaml                  # 应用程序资源
├── App.xaml.cs              # 应用程序入口
├── WpfApp.csproj           # 项目文件
├── AssemblyInfo.cs         # 程序集信息
└── README.md               # 项目文档
```

---

## 5. 核心模块说明

### 5.1 数据服务 (DataService)

**文件位置**: `Services/DataService.cs`

**主要功能**:
- 数据库操作（增删改查）
- 应用设置管理
- 值日排班逻辑
- 轮播内容管理
- 数据导入导出

**核心方法**:
```csharp
// 学生管理
public List<Student> GetStudents();
public void AddStudent(Student student);
public void UpdateStudent(Student student);
public void DeleteStudent(int id);

// 项目管理
public List<DutyProject> GetProjects();
public void AddProject(DutyProject project);
public void UpdateProject(DutyProject project);
public void DeleteProject(int id);

// 排班管理
public List<DutyRecord> GetDutyRecords(DateTime? date = null);
public void AutoSchedule(DateTime startDate, DateTime endDate, List<int> skipDates = null);
public void ManualSchedule(DateTime date, List<int> studentIds, int projectId);

// 提醒管理
public List<Reminder> GetReminders();
public void AddReminder(Reminder reminder);
public void UpdateReminder(Reminder reminder);
public void DeleteReminder(int id);

// 轮播内容管理
public List<CarouselItem> GetAllCarouselItems();
public void AddCarouselItem(CarouselItem item);
public void UpdateCarouselItem(CarouselItem item);
public void DeleteCarouselItem(int id);

// 应用设置
public AppSettings GetAppSettings();
public void UpdateAppSettings(AppSettings settings);
```

### 5.2 顶栏窗口 (TopBarWindow)

**文件位置**: `Views/TopBarWindow.xaml` 和 `Views/TopBarWindow.xaml.cs`

**主要功能**:
- 实时显示当前值日信息
- 支持多种显示位置（顶部、底部、左侧、右侧）
- 可调节透明度和高度
- 点击打开主窗口
- 左右位置时不显示时间模块

**核心方法**:
```csharp
// 位置设置
public void SetPosition(int position);
public void SetOpacity(double opacity);
public void SetHeight(int height);

// 信息更新
public void UpdateInfo();
public void UpdateTime();

// 事件处理
private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e);
private void OnDutyModuleClick(object sender, MouseButtonEventArgs e);
private void OnTimeModuleClick(object sender, MouseButtonEventArgs e);
```

**位置说明**:
- `0`: 顶部
- `1`: 底部
- `2`: 左侧
- `3`: 右侧

**特殊逻辑**:
- 在左侧和右侧位置时，不显示时间模块
- 点击值日信息可打开主窗口

### 5.3 主窗口 (MainWindow)

**文件位置**: `Views/MainWindow.xaml` 和 `Views/MainWindow.xaml.cs`

**主要功能**:
- 主界面导航
- 学生、项目、排班、提醒、轮播内容管理
- 系统设置
- 数据导入导出

**核心方法**:
```csharp
// 顶栏管理
public void ShowTopBar();
public void HideTopBar();
public void CloseTopBar();

// 数据加载
private void LoadData();
private void LoadSettings();
private void LoadScheduleData();
private void LoadTimeSlotData();

// 事件处理
private void OnWindowLoaded(object sender, RoutedEventArgs e);
private void OnTopBarSettingChanged(object sender, RoutedEventArgs e);
private void OnTopBarOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e);
private void OnTopBarHeightChanged(object sender, RoutedPropertyChangedEventArgs<double> e);
private void OnTopBarPositionChanged(object sender, SelectionChangedEventArgs e);
```

### 5.4 提醒服务 (ReminderService)

**文件位置**: `Services/ReminderService.cs`

**主要功能**:
- 提醒检查
- 提醒通知显示
- 提醒状态管理

**核心方法**:
```csharp
public void StartReminderCheck();
public void StopReminderCheck();
private void OnReminderCheck(object? sender, EventArgs e);
private void ShowReminderNotification(Reminder reminder);
```

---

## 6. 数据模型

### 6.1 学生模型 (Student)

**文件位置**: `Models/Student.cs`

```csharp
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string StudentNumber { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

**字段说明**:
- `Id`: 学生唯一标识
- `Name`: 学生姓名
- `StudentNumber`: 学号
- `IsEnabled`: 是否启用（参与排班）
- `CreatedAt`: 创建时间
- `UpdatedAt`: 更新时间

### 6.2 值日项目模型 (DutyProject)

**文件位置**: `Models/DutyProject.cs`

```csharp
public class DutyProject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

**字段说明**:
- `Id`: 项目唯一标识
- `Name`: 项目名称
- `Description`: 项目描述
- `CreatedAt`: 创建时间
- `UpdatedAt`: 更新时间

### 6.3 值日记录模型 (DutyRecord)

**文件位置**: `Models/DutyRecord.cs`

```csharp
public class DutyRecord
{
    public int Id { get; set; }
    public DateTime DutyDate { get; set; }
    public int StudentId { get; set; }
    public int DutyProjectId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public Student? Student { get; set; }
    public DutyProject? DutyProject { get; set; }
}
```

**字段说明**:
- `Id`: 记录唯一标识
- `DutyDate`: 值日日期
- `StudentId`: 学生 ID
- `DutyProjectId`: 项目 ID
- `CreatedAt`: 创建时间
- `UpdatedAt`: 更新时间

### 6.4 提醒模型 (Reminder)

**文件位置**: `Models/Reminder.cs`

```csharp
public class Reminder
{
    public int Id { get; set; }
    public DateTime ReminderTime { get; set; }
    public string Content { get; set; } = "";
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

**字段说明**:
- `Id`: 提醒唯一标识
- `ReminderTime`: 提醒时间
- `Content`: 提醒内容
- `IsCompleted`: 是否已完成
- `CreatedAt`: 创建时间
- `UpdatedAt`: 更新时间

### 6.5 轮播内容模型 (CarouselItem)

**文件位置**: `Models/CarouselItem.cs`

```csharp
public class CarouselItem
{
    public int Id { get; set; }
    public string Content { get; set; } = "";
    public int SortOrder { get; set; } = 0;
    public bool IsEnabled { get; set; } = true;
    public bool IsSystemDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

**字段说明**:
- `Id`: 轮播项唯一标识
- `Content`: 轮播内容
- `SortOrder`: 排序
- `IsEnabled`: 是否启用
- `IsSystemDefault`: 是否系统默认项
- `CreatedAt`: 创建时间
- `UpdatedAt`: 更新时间

### 6.6 应用设置模型 (AppSettings)

**文件位置**: `Models/AppSettings.cs`

```csharp
public class AppSettings
{
    public int Id { get; set; } = 1;
    public string? ClassName { get; set; }
    public double TopBarOpacity { get; set; } = 1.0;
    public int TopBarHeight { get; set; } = 50;
    public bool TopBarVisible { get; set; } = true;
    public int TopBarPosition { get; set; } = 0;
    public bool StartWithWindows { get; set; } = false;
    public bool WindowTopMost { get; set; } = false;
    public bool HideMainWindowOnStart { get; set; } = false;
    public double WindowOpacity { get; set; } = 0.9;
    public string? CloudFormUrl { get; set; }
    public string? AdminKey { get; set; }
    public int SyncIntervalMinutes { get; set; } = 5;
    public DateTime LastSyncTime { get; set; } = DateTime.MinValue;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

**字段说明**:
- `Id`: 设置 ID（固定为 1）
- `ClassName`: 班级名称
- `TopBarOpacity`: 顶栏透明度（0.0 - 1.0）
- `TopBarHeight`: 顶栏高度（像素）
- `TopBarVisible`: 是否显示顶栏
- `TopBarPosition`: 顶栏位置（0-顶部, 1-底部, 2-左侧, 3-右侧）
- `StartWithWindows`: 是否开机启动
- `WindowTopMost`: 是否窗口置顶
- `HideMainWindowOnStart`: 启动时是否隐藏主窗口
- `WindowOpacity`: 窗口透明度
- `CloudFormUrl`: 云端表单 URL
- `AdminKey`: 管理员密钥
- `SyncIntervalMinutes`: 同步间隔（分钟）
- `LastSyncTime`: 最后同步时间
- `UpdatedAt`: 更新时间

---

## 7. 用户界面

### 7.1 主窗口

主窗口采用现代化的深色主题设计，包含以下主要区域：

#### 7.1.1 顶部导航栏
- 应用标题
- 窗口控制按钮（最小化、最大化、关闭）

#### 7.1.2 左侧导航菜单
- 今日
- 排班
- 学生
- 项目
- 提醒
- 设置

#### 7.1.3 内容区域
根据选择的导航菜单显示不同的内容：
- **今日**: 显示今日值日信息
- **排班**: 显示排班日历和排班记录
- **学生**: 显示学生列表和管理功能
- **项目**: 显示项目列表和管理功能
- **提醒**: 显示提醒列表和管理功能
- **设置**: 显示系统设置

### 7.2 顶栏窗口

顶栏窗口是一个悬浮窗，实时显示当前值日信息。

#### 7.2.1 水平模式（顶部/底部）
- 左侧：时间模块（时间、日期、星期）
- 右侧：值日信息模块

#### 7.2.2 垂直模式（左侧/右侧）
- 不显示时间模块
- 只显示值日信息模块

#### 7.2.3 自定义选项
- 透明度：30% - 100%
- 高度：30 - 100 像素
- 位置：顶部、底部、左侧、右侧

### 7.3 对话框

应用程序包含多个对话框，用于数据输入和配置：

- **学生对话框**: 添加/编辑学生信息
- **项目对话框**: 添加/编辑项目信息
- **自动排班对话框**: 配置自动排班参数
- **提醒对话框**: 添加/编辑提醒
- **时段对话框**: 添加/编辑时段
- **排班对话框**: 手动排班
- **日期范围对话框**: 选择日期范围
- **导入列对话框**: 配置导入列映射
- **轮播内容对话框**: 管理轮播内容
- **编辑轮播内容对话框**: 编辑轮播内容

---

## 8. 安装和部署

### 8.1 系统要求

- 操作系统：Windows 10 或更高版本
- .NET Runtime：.NET 10.0 Windows Runtime
- 磁盘空间：至少 100 MB

### 8.2 安装步骤

1. 下载应用程序安装包
2. 解压到任意目录
3. 运行 `ClassDutyHelper.exe`
4. 首次运行会自动创建数据库

### 8.3 部署选项

#### 8.3.1 独立部署
将应用程序打包为独立可执行文件，无需安装 .NET Runtime。

#### 8.3.2 依赖部署
需要目标机器上安装 .NET 10.0 Windows Runtime。

---

## 9. 使用指南

### 9.1 首次使用

1. **启动应用程序**
   - 双击 `ClassDutyHelper.exe` 启动应用程序

2. **添加学生**
   - 导航到"学生"页面
   - 点击"添加学生"按钮
   - 输入学生信息（姓名、学号）
   - 点击"保存"

3. **添加项目**
   - 导航到"项目"页面
   - 点击"添加项目"按钮
   - 输入项目信息（名称、描述）
   - 点击"保存"

4. **自动排班**
   - 导航到"排班"页面
   - 点击"自动排班"按钮
   - 选择日期范围
   - 配置智能跳过选项（可选）
   - 点击"排班预览"查看结果
   - 点击"开始排班"生成排班

### 9.2 日常使用

#### 9.2.1 查看今日值日
- 启动应用程序后，顶栏会自动显示今日值日信息
- 也可以在主窗口的"今日"页面查看详细信息

#### 9.2.2 手动排班
- 导航到"排班"页面
- 在日历中选择日期
- 点击"手动排班"按钮
- 选择值日生和项目
- 点击"保存"

#### 9.2.3 添加提醒
- 导航到"提醒"页面
- 点击"添加提醒"按钮
- 设置提醒时间和内容
- 点击"保存"

#### 9.2.4 管理轮播内容
- 导航到"设置"页面
- 在"轮播内容"区域管理：
  - 添加轮播内容
  - 编辑轮播内容
  - 删除轮播内容
  - 设置排序
  - 启用/禁用轮播项

#### 9.2.5 调整顶栏设置
- 导航到"设置"页面
- 在"顶栏设置"区域调整：
  - 启用/禁用顶栏显示
  - 调整透明度
  - 调整高度
  - 选择位置

### 9.3 高级功能

#### 9.3.1 数据导入
- 导航到"学生"或"项目"页面
- 点击"导入"按钮
- 选择 Excel 文件
- 配置列映射
- 点击"导入"

#### 9.3.2 数据导出
- 导航到"排班"页面
- 点击"导出"按钮
- 选择导出格式
- 保存文件

#### 9.3.3 云端同步
- 导航到"设置"页面
- 在"云端同步"区域配置：
  - 云端表单 URL
  - 管理员密钥
  - 同步间隔
- 点击"立即同步"进行手动同步

---

## 10. 开发指南

### 10.1 开发环境搭建

#### 10.1.1 必需软件
- Visual Studio 2022 或更高版本
- .NET 10.0 SDK
- Git（可选）

#### 10.1.2 安装步骤
1. 克隆项目仓库
2. 使用 Visual Studio 打开 `WpfApp.slnx`
3. 还原 NuGet 包
4. 编译并运行项目

### 10.2 代码规范

#### 10.2.1 命名规范
- 类名：PascalCase（如 `StudentService`）
- 方法名：PascalCase（如 `GetStudents`）
- 属性名：PascalCase（如 `StudentName`）
- 私有字段：camelCase（如 `_students`）
- 常量：PascalCase（如 `MaxRetryCount`）

#### 10.2.2 注释规范
- 公共类和方法必须添加 XML 注释
- 复杂逻辑必须添加行内注释
- 注释使用中文

### 10.3 扩展开发

#### 10.3.1 添加新功能
1. 在 `Models` 中添加数据模型
2. 在 `Services/DataService.cs` 中添加数据访问方法
3. 在 `Views` 中创建或修改界面
4. 在 `MainWindow.xaml.cs` 中添加事件处理

#### 10.3.2 添加新页面
1. 在 `Views/Pages` 中创建新的 XAML 文件
2. 设计页面布局
3. 实现页面逻辑
4. 在主窗口中添加导航菜单项

---

## 11. 配置说明

### 11.1 应用设置

应用设置存储在 SQLite 数据库的 `AppSettings` 表中。

#### 11.1.1 顶栏设置

| 设置项 | 默认值 | 说明 |
|-------|-------|------|
| TopBarVisible | true | 是否显示顶栏 |
| TopBarOpacity | 1.0 | 顶栏透明度（0.0 - 1.0） |
| TopBarHeight | 50 | 顶栏高度（像素） |
| TopBarPosition | 0 | 顶栏位置（0-顶部, 1-底部, 2-左侧, 3-右侧） |

#### 11.1.2 窗口设置

| 设置项 | 默认值 | 说明 |
|-------|-------|------|
| WindowTopMost | false | 是否窗口置顶 |
| WindowOpacity | 0.9 | 窗口透明度（0.0 - 1.0） |
| HideMainWindowOnStart | false | 启动时是否隐藏主窗口 |

#### 11.1.3 系统设置

| 设置项 | 默认值 | 说明 |
|-------|-------|------|
| ClassName | "" | 班级名称 |
| StartWithWindows | false | 是否开机启动 |

#### 11.1.4 同步设置

| 设置项 | 默认值 | 说明 |
|-------|-------|------|
| CloudFormUrl | "" | 云端表单 URL |
| AdminKey | "" | 管理员密钥 |
| SyncIntervalMinutes | 5 | 同步间隔（分钟） |
| LastSyncTime | DateTime.MinValue | 最后同步时间 |

### 11.2 数据库配置

数据库文件位于应用程序目录下的 `ClassDutyHelper.db`。

#### 11.2.1 数据库初始化
应用程序首次启动时会自动创建数据库和表结构。

#### 11.2.2 数据库迁移
使用 Entity Framework Core 的迁移功能管理数据库结构变更。

---

## 12. 故障排除

### 12.1 常见问题

#### 12.1.1 应用程序无法启动
**可能原因**:
- .NET Runtime 未安装
- 数据库文件损坏

**解决方法**:
- 安装 .NET 10.0 Windows Runtime
- 删除数据库文件，重新启动应用程序

#### 12.1.2 顶栏不显示
**可能原因**:
- 顶栏设置为隐藏
- 顶栏窗口被意外关闭

**解决方法**:
- 在设置中启用顶栏显示
- 重启应用程序

#### 12.1.3 数据同步失败
**可能原因**:
- 网络连接问题
- 云端表单 URL 或管理员密钥错误

**解决方法**:
- 检查网络连接
- 验证云端表单 URL 和管理员密钥

#### 12.1.4 数据导入失败
**可能原因**:
- Excel 文件格式不正确
- 列映射配置错误

**解决方法**:
- 检查 Excel 文件格式
- 重新配置列映射

### 12.2 日志和调试

#### 12.2.1 启用调试日志
在 `App.xaml.cs` 中添加日志输出。

#### 12.2.2 查看同步日志
同步日志存储在数据库的 `SyncLogs` 表中。

---

## 附录

### A. 技术支持

如有问题或建议，请联系开发团队。

### B. 许可证

待定。

### C. 致谢

感谢所有为本项目做出贡献的开发者和用户。

---

**文档版本**: 1.0.0
**最后更新**: 2026-03-06
**文档作者**: ClassDutyHelper 开发团队
