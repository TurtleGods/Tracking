# plan.md — Technical Implementation Plan
(derived from spec.md)

## 1. 技術選型（Tech Stack Decisions）

### 1.1 Runtime & Framework
- **API Server**：使用 **.NET 10 (ASP.NET Core)** 建構後端 API
- **API Style**：使用 **Controllers**（非 Minimal API）
  理由：更清楚的路由結構、較符合企業常見模式、易於擴充版本化。

### 1.2 Hosting & Container
- 需提供 **Dockerfile**，支援本地開發與部署。
- 使用 multi-stage build（`sdk` → `aspnet`）以減少映像大小。

### 1.3 Data Layer（Interface-based with In-Memory Implementation）
- **資料持久層使用介面隔離實作相依**
  → 定義 `IEventStore` 介面
  → In-Memory 實作作為 POC 方案 (`InMemoryEventStore`)
  → 所有事件維持 **in-memory append-only list**
  → 系統每次重啟會自動清空資料
- 使用 Singleton Service 管理 events buffer
- 介面設計需考慮後續擴展性（如切換至其他儲存方案）
- 高併發的資料持久層設計不在 POC 範圍，但 Key 和 Cursor 的技術規格將確保易於後續調整

### 1.4 Logging & Validation
- 使用 ASP.NET Core 內建 logging（可選擇 Console logger）。
- 使用 `System.ComponentModel.DataAnnotations` + 自訂驗證邏輯達成 spec 裡的 ingestion rules。

---

## 2. 專案目錄結構（Project Structure）

```yaml
/
├── src/
│ ├── Api/
│ │ ├── Controllers/
│ │ │ ├── EventsController.cs
│ │ │ └── CompaniesController.cs
│ │ ├── Services/
│ │ │ ├── IEventStore.cs
│ │ │ ├── InMemoryEventStore.cs
│ │ │ ├── EventCursorHandler.cs
│ │ │ └── ValidationService.cs
│ │ ├── Models/
│ │ │ ├── Event.cs
│ │ │ ├── EventMetadata.cs
│ │ │ ├── DeviceInfo.cs
│ │ │ └── EventResponseModels.cs
│ │ ├── Dtos/
│ │ │ ├── BatchRequestDto.cs
│ │ │ └── QueryResponseDto.cs
│ │ ├── Program.cs
│ │ └── Api.csproj
│ └── ...
├── tests/
│ ├── Api.Tests/
│ │ ├── EventStoreTests.cs
│ │ ├── CursorTests.cs
│ │ └── IngestionRulesTests.cs
│ └── Api.Tests.csproj
├── build/
│ └── Dockerfile
├── config/
│ └── appsettings.Development.json
└── Project.sln
```

## 3. Domain and Data Models

- [Domain Model](domain-model.md)
- [Data Model](data-model.md)

---

## 3. 系統設計（System Design）

### 3.1 Ingestion Flow（POST /events/batch）

```markdown
Pipeline：

BatchRequest → Validate events → Apply ingestion rules →
Append to in-memory store →
Produce { accepted[], rejected[] }
```

Validation / Rules 實作：

1. **批次內 timestamp-CompanyId-EmployeeId-DeviceId 重複**
   → 使用 HashSet 檢查 batch-level duplicates with format "{timestamp}-{companyId}-{employeeId}-{deviceId}"
2. **跨批次 timestamp-CompanyId-EmployeeId-DeviceId 重複**
   → 查 in-memory store index
   → 若撞到，直接忽略事件
3. **metadata 缺少必要欄位**
   → 依 eventType 動態檢查
4. **未知 eventType / deviceType / os**
   → 設 flags，例如：`unknown_eventType=true`
5. **多餘欄位**
   → 比對 schema，出現未定義欄位 → `extra_fields=true`
6. **事件需依前端送入順序紀錄。**
7. **append-only**
   → EventStore 不提供更新 API，只能新增。

```yaml
Response Model：
{
accepted: [{ eventId }],
rejected: [{ eventId, error_code, message }]
}
```

### 3.2 查詢（GET /events, GET /companies/{id}/events）

Key-based Query：

- Query 條件格式：`{timestamp}|{companyId}|{employeeId}|{deviceId}`
- 系統將查詢 <= timestamp 的指定客戶的所有裝置資訊 by 分頁大小
- 若 cursor 無效 → 自動 fallback: 從最舊事件開始。
- Query Service 實作：
  - 依 eventId (timestamp-CompanyId-EmployeeId-DeviceId) 排序
  - 推算下一個 cursor：取**最後一筆事件的 timestamp|companyId|employeeId|deviceId**。
- 回傳事件需包含儲存時的 flags。

---

## 4. API 設計概要

詳細的 API 規格請參考 [OpenAPI Specification](openapi-spec.yaml)。

### 4.1 `POST /events/batch`
- Body: `{ events: [<max 10>] }`
- Response: `200 OK`
- 只回傳部分成功資料（accepted + rejected）

### 4.2 `GET /events?t={cursor}&size=100`
- 讀取所有公司所有事件

### 4.3 `GET /companies/{companyId}/events?t={cursor}&size=100`
- 過濾 companyId

---

## 5. 服務與模組拆分（Services & Components）

| Component | Responsibility |
|----------|----------------|
| IEventStore | Event storage interface、查詢（定義契約） |
| InMemoryEventStore | Append-only raw event storage、查詢（POC 實作） |
| ValidationService | Ingestion Rules 驗證邏輯 |
| EventCursorHandler | Cursor encode/decode、fallback 處理 |
| EventsController | batch ingestion endpoint |
| CompaniesController | company-based query endpoint |

---

## 6. Dockerfile 設計

採 multi-stage：

```yaml
FROM mcr.microsoft.com/dotnet/sdk:10 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Api.dll"]
```

---

## 7. 測試計畫（Test Strategy）

### 7.1 Unit Tests
- Ingestion Rules
  - duplicate rules
  - metadata missing
  - unknown fields
- Cursor logic
- EventStore interface contract tests
- EventStore append-only 行為

### 7.2 Integration Tests
- POST /events/batch full flow
- GET /events cursor iteration
- Company filter correctness

---

## 8. 非功能需求 Mapping

- In-memory only → deterministic reset
- 不需 auth → Controllers 開放
- 不需要高效能 → 不做 queue 或 DB
- Multi-tenant via companyId → Query filter 實作即可