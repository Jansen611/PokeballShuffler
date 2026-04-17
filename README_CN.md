# 来吧宝可梦 (PokeballShuffler)

[English Version](README.md)

这是一个使用 .NET MAUI 开发的移动端精灵球洗牌小游戏，包含抽球动画、双模式玩法和角色技能机制。

## 诞生小故事

这个 App 的起点，是一次和孩子们的露营。
当时我们带了“来吧！宝可梦”桌游，结果玩到一半发现有几个 Pokeball Token 不见了，游戏流程就卡住了。

我当场灵机一动，直接用 AI 开始做一个数字版。
大概一个半小时，standard 版本就跑起来了。

回家后想想既然都开了头，就顺手把 extended 版本也做出来了。

希望这个小项目也能帮到开源社区里同样爱折腾、爱玩游戏的朋友们。

## 功能

- 从 15 个球中按 `4 -> 3 -> 2 -> 1` 进行 4 回合抽取
- 两种游戏模式：
  - **Normal 模式**：标准洗牌与展示流程
  - **Extended 模式**：
    - **1号角色**：Basket1 的第 4 个球会被隐藏，点击 1号角色 槽位可揭示
    - **2号角色**：第 4 回合后，可将 Basket4 与 Undrawn 中随机一个球交换（每局一次）
- 每轮抽球采用错峰展示动画
- 最终结果展示时，Undrawn 面板带有动画
- 支持运行时切换模式与主题
- Android 端以横屏体验为主

## 技术栈

- .NET MAUI
- C#
- 代码构建 U

## 项目结构

- `PokeballShuffler/MainPage.cs`：UI 构建、主题应用、动画编排
- `PokeballShuffler/ViewModels/MainViewModel.cs`：游戏流程、隐藏球逻辑、2号角色 重抽、命令状态
- `PokeballShuffler/Models/`：数据模型与枚举（`BallType`、`GameMode`、`Pokeball`）

## 快速开始

1. 安装 .NET SDK（包含 MAUI workload）及 Android 开发环境。
2. 构建 Android 目标。
3. 在已连接的 Android 设备上安装生成的 APK。
4. 以横屏模式启动并开始游戏。

## 许可证

本项目使用 MIT License，详见 [LICENSE](LICENSE)。
