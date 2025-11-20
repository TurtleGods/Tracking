# 使用者行為埋點分析系統（POC）

## 🎯 目標

建立一個 POC 等級的後端系統，用於接收與查詢前端送出的使用者行為事件，強調：

- 資料格式正確性
- 行為操作可追蹤性
- 查詢便利性

本系統**不需支援**高吞吐量、聚合統計、視覺化、驗證機制、資料清理或持久化 DB。

---

## 📦 核心功能

---

## 1. 行為事件紀錄（Tracking）

後端提供 API 接受前端批次事件，**最多 10 筆**。

每筆事件需包含：

- `productId`
- `companyId`
- `employeeId`
- `sessionId`
- `screenId`
- `eventType`
- `timestamp`（Epoch milliseconds）
- `eventId`（前端產生）
- `metadata`（依 eventType 定義必填）
    - `enter_screen`：需包含 `view`
    - `click`：需包含 `componentId`
        
- `deviceInfo`
    - `deviceType`（`Android` | `IOS` | `Browser`）
    - `os`（`Android` | `IOS` | `Browser`）
        

### ✔ Ingestion Rules

1. **批次內 eventId 重複 → 拒絕後者，接受第一筆。**
2. **跨批次 eventId 重複 → 接受，但標記 `flags.duplicate_eventId=true`。**
3. **metadata 缺少 eventType 必填欄位 → 拒絕。**
4. **未知的 eventType / deviceType / os → 接受，並於 flags 標示，如 `flags.unknown_eventType=true`。**
5. **未知欄位（多餘 metadata 或 deviceInfo 欄位） → 接受，但標記 `flags.extra_fields=true`。**
6. **事件需依前端送入順序紀錄（server 產生 seqId 時保持原順序）。**
7. **Raw events 為 append-only，不可修改。**

---

## 1.1 停留時間寫入

- 停留時間（duration）由 **前端在 leave_screen 時自行計算並上報**。
- 事件 schema 新增欄位（僅 leave_screen 需要）：
    - `metadata.duration`（秒數或毫秒，格式由前端決定，POC 不做校驗）
- 後端不需自動推算 enter/leave。

---

## 2. 查詢功能（Analytics Query）

## 2.1 事件歷程查詢（Cursor-based）

查詢僅回傳 **append-only raw events（含 flags）**，保持儲存原樣。

### API 形式：

- 查詢所有公司事件  
    `GET /events?t={cursor}&size=100`
    
- 查詢某公司事件  
    `GET /companies/{companyId}/events?t={cursor}&size=100`
    

### ✔ Opaque Cursor 規格

- 格式強制為：`{timestamp}|{opaqueUUID}`
- 伺服器不解析 opaqueUUID 的語義，只視為不透明 token。
- 若 cursor 無效 / 不存在：
    - **自動 fallback 從頭開始**（不回 400）。

### Response 範例：

```json

{
  "events": [/* raw events as stored */],
  "next_cursor": "1768888894123|opaque123",
  "size": 100
}

```

---

## 🔩 API Schema

### `POST /events/batch`

**Request**

```json
{   "events": [ /* up to 10 */ ] }
```

**部分成功 Response（HTTP 200）**

- `accepted[i].seqId`：伺服器遞增流水號
- `rejected[i].error_code` 必須符合 **全大寫 + 底線** 命名格式
    

**範例：**

```json
{
  "accepted": [
    {
      "eventId": "uuid-123",
      "seqId": 1001
    }
  ],
  "rejected": [
    {
      "eventId": "uuid-123",
      "error_code": "DUPLICATE_EVENT_ID_IN_BATCH",
      "message": "Duplicate eventId in the same batch."
    },
    {
      "eventId": "uuid-456",
      "error_code": "MISSING_METADATA",
      "message": "metadata.view required for enter_screen"
    }
  ]
}
```

---

## 👥 使用者故事

### 前端工程師

希望能方便批次上報事件並帶入 metadata，使後端可重建使用流程。

### 產品經理 / PM

希望能查詢特定畫面停留及事件歷程。

### 資料分析者

希望取得 raw events 進行後續分析。

---

## 🚫 非功能需求(NFR)

- 無需 authentication（POC）。
- multi-tenant 僅依 `companyId` 區分。
- **使用 in-memory 資料，且每次系統重啟資料清空（deterministic reset）。**
- 不需資料清理、存放策略。
- 不需高性能或高吞吐能力。