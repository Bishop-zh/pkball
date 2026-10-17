# PKBall：弹珠决斗与肉鸽构筑

Unity / C# 游戏原型，结合弹射对战、圆环装备、道具合成与成长。包含冒险流程和共用战斗表现的主播对战模式；多人联网属于后续计划。

## 打开项目

1. 使用 Unity Hub 添加本项目目录，编辑器版本为 **Unity 2022.3.62f3c1**。
2. 等待 Unity 根据 `Packages/manifest.json` 恢复依赖（URP 14、UGUI、TextMeshPro 等）。
3. 打开 `Assets/PKBall/Scenes/MainMenu.unity`，点击 Play。

主流程：主菜单 → 准备（事件、商店、背包与装备环）→ 战斗 → 准备。
场景和界面已经保存，不需要先执行生成工具。

## 工程内容

- `Assets/`：C# 脚本、场景、预制体、配置和项目美术音频。
- `Packages/`、`ProjectSettings/`：Unity 依赖与工程配置。
- `Docs/`：设计记录、实现状态和操作说明，入口见 [文档索引](Docs/README.md)。早期设计与当前实现可能不同，以较新的专题和代码为准。
- `Tools/`：项目素材处理与内容检查工具。

Unity 缓存、构建产物、日志、IDE 自动生成文件和本地凭据不入库。下载后由 Unity 在本机重新生成缓存。

## 仓库

- GitHub：https://github.com/Bishop-zh/pkball
- Gitee：https://gitee.com/sillybishop/pkball

两个平台需要分别推送，不会自动同步。
