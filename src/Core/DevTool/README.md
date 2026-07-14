# MengYou.DevTool - 开发者标定工具

## 功能

采集图像识别所需的资产：

- **UILayout 坐标**：读入/写出 `MengYou.App/Config/UILayout.json`
- **模板 PNG**：从截图裁剪并保存到 `MengYou.App/Config/Templates/`

## 运行

```
dotnet run --project src/MengYou.DevTool
```

## 使用流程

### 1. 冻结截图

1. 打开梦幻西游窗口
2. 点击"刷新窗口"，从下拉框选择游戏窗口
3. 点击"截图冻结"

游戏画面被冻结到中央画布，后续所有操作都在这张静态图上进行。

### 2. 标定 UILayout 区域

**用途**：把某个 UI 元素的屏幕坐标记录到 UILayout.json，供 `ImageGameReader` 读取。

1. 在截图上**鼠标拖拽画框**（例如围住"HP 数字"区域）
2. 右侧属性面板会显示：`区域: X=... Y=... W=... H=...`
3. 在"键名"框填入约定的键名，如 `HpNumber`
4. 类型选 **Region**
5. 点"保存到 UILayout.json"

左侧列表立即出现该元素，画布上绿框叠加显示。

**标定点位（Point）**：拖拽尺寸 < 2 像素时自动视为点，或手动切换到 Point 类型。适用于 `BagButton` 等点击目标。

### 3. 保存为模板 PNG

**用途**：把某个 UI 元素抠图存为模板，供模板匹配识别。

1. 拖拽框住目标（如 NPC 头像、技能图标、飞行符图标）
2. 键名填入约定名，如 `Scene.CombatMark` / `NPC.李靖` / `Item.飞行符`
3. 点"保存为模板 PNG"

模板文件保存到 `MengYou.App/Config/Templates/{键名}.png`。

### 4. 已标定列表

左侧列表显示所有已保存内容：
- `Region:xxx` 区域
- `Point:xxx` 点
- `Template:xxx` 模板 PNG

**点击列表项** → 属性面板回填该元素信息（用于修改）
**选中后点删除** → 从 UILayout 或磁盘删除

### 5. 可视化叠加

冻结截图上叠加所有已标定内容：
- **绿框** 已标定 Region
- **黄色十字** 已标定 Point
- **红色虚线** 当前拖拽选区

点击"切换叠加显示"可临时隐藏叠加层。

## 键名约定（速查）

| 用途 | 键名 |
|------|------|
| 玩家 HP 数字 | `HpNumber` / `MaxHpNumber` |
| 玩家 MP 数字 | `MpNumber` / `MaxMpNumber` |
| 等级 | `Level` |
| 地图名 | `MapName` |
| 当前坐标 | `CoordX` / `CoordY` |
| 小地图 | `MiniMap` |
| 背包按钮 | `BagButton`（Point） |
| 队友血条 | `TeamSlot.1.HpBar` .. `TeamSlot.5.HpBar` |
| 队友名字 | `TeamSlot.1.Name` .. |
| 敌人血条 | `EnemySlot.1.HpBar` .. `EnemySlot.5.HpBar` |
| 敌人名字 | `EnemySlot.1.Name` .. |
| 背包格子 | `BagSlot.1.Icon` / `BagSlot.1.Count` .. |
| 场景标志（模板） | `Scene.CombatMark` / `Scene.DialogMark` / `Scene.LoadingMark` / `Scene.DeadMark` |
| NPC 模板 | `NPC.{NPC名}`，如 `NPC.李靖` |
| 物品模板 | `Item.{物品名}`，如 `Item.飞行符` |
| 飞行符目标 | `FlyDest.{地图名}`，如 `FlyDest.傲来` |
| NPC 传送选项 | `NpcTeleport.{地图名}` |
| 技能图标 | `Skill.{技能名}`，如 `Skill.普通攻击` |
| 战场编号 | `Slot.{编号}` |
| 对话内容 | `DialogContent`（Region，OCR） |

## 注意事项

- **同一版本同一分辨率**：所有坐标都是绝对像素值。切换游戏窗口大小前，先把窗口尺寸调整到你固定使用的分辨率。
- **截图为客户端坐标系**：不含窗口边框和标题栏，从 (0,0) 开始。
- **保存自动写盘**：无需额外确认。
