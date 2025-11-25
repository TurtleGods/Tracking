# 會議記錄草稿

[詠唱歷程](https://v0.app/chat/react-tracking-tool-kba923T5vGo)
[SourceCode](https://github.com/JJingLee2024/react-tracking-tool)
[Prompt](https://melodic-twist-c62.notion.site/Track-SDK-JS-2aabb275302180cf866dc563b4bfbcb4)
[Spec](https://melodic-twist-c62.notion.site/Mayo-React-Tracking-System-2aebb275302180949a24d90539c259af)
[Demo site](https://react-analytics-tracker.vercel.app/)

## 需求背景

User behavior is quit critical for us building Persona, but it cost too much for now.

## 初步調研

根據 [簡單調研](https://felo.ai/search/5vAbipj4VxsR2gokJ65VMf)，
開源自架方案目前只有 [CLKLog](https://clklog.com/)，
來自中國且商業版有額外費用，先不考慮。

## 追蹤的關鍵規格
- **鏈式 API 設計**: `Page().name("Home").view()`, `Button().name("Submit").click()`
- **四種事件類型**: View（頁面瀏覽）, Click（點擊）, Expose（曝光）, Disappear（消失）
- **自動命名規範**: `[Type]_[PageName]_[ComponentName]` 格式
- **批次發送機制**: 每 15 秒或 session 結束時批次發送，優化效能
- **自動資料收集**: 設備、網路、頁面資訊自動收集
- **React 整合**: 提供 Hooks 和組件實現自動追蹤
- **即時監控**: Live Log 每 2 秒刷新，實時查看事件
- **完整管理後台**: 儀表板、事件列表、Session 管理
- **進階分析功能**: 自訂儀表板、圖表配置、拖拽排序

## 階段里程碑

Phase 1

- Goal: 觀測使用者的行為
  - 使用者在哪個畫面按了什麼，停留了幾秒並於幾秒後離開該畫面

- 拆分:
	- 前端: 簡易畫面
	- 後端: Web API Spec / 收資料的技術選型
- 關鍵產出: 
	- POC 驗證可行性: 有實作持久化的操作歷程清單

Phase  2

- Goal: 進階的觀測使用者行為
  - 使用者從哪個畫面到哪個畫面，跳轉前後停留多久的歷史紀錄
  - live log
- 拆分:
	- 前端: 畫面擴充
	- 後端: 擴充 API Spec / 初步完成可內部上線的實作 和 拿到 驗證環境服務器
- 關鍵產出
	- 可被內部使用的大致技術選型版本定案

Phase 3

- Goal: 觀測使用者行為看板
  - 圖像報告: Bar chart 等
  - 指標: 群組指標
- 拆分:
	- 前端: 畫面擴充
	- 後端: 擴充 API Spec / 可內部上線的實作定版 和 拿到 內部上線用環境服務器
- 關鍵產出
	- 可被內部使用的大致技術選型版本定案

## Call to action

後端：調研一週，估算成本花費
前端：V0 code落地的可行性