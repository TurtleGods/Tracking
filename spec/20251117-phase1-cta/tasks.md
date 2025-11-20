# task.md — Implementation Task Breakdown
(derived from **spec.md** and **plan.md**)

## 開發任務優先順序與相依性

### 第一階段：基礎架構 (Phase 1)
1. **Project Setup** - 建立 .NET 10 專案基礎
   - [ ] 建立 .NET 10 ASP.NET Core 專案 (`Mayo.Platform.Tracklix.WebAPI`)
   - [ ] 建立 Solution (`Project.sln`)
   - [ ] 設定目錄結構（Controllers / Services / Models / Dtos）
   - [ ] 建立 `appsettings.Development.json`

2. **Domain Models** - 建立領域模型
   - [ ] 建立 `Event.cs` (參考 [Domain Model](domain-model.md) 和 [Data Model](data-model.md))
   - [ ] 建立 `EventMetadata.cs` (參考 [Domain Model](domain-model.md) 和 [Data Model](data-model.md))
   - [ ] 建立 `DeviceInfo.cs` (參考 [Domain Model](domain-model.md) 和 [Data Model](data-model.md))
   - [ ] 建立 `EventResponseModels.cs` (參考 [Domain Model](domain-model.md) 和 [Data Model](data-model.md))

3. **DTOs** - 建立資料傳輸物件
   - [ ] 建立 `BatchRequestDto.cs`
     - `{ events: [...] }`（最多 10 筆）
   - [ ] 建立 `QueryResponseDto.cs`
     - `events`, `next_cursor`, `size`

### 第二階段：核心功能 (Phase 2)
4. **Event Store** - 建立事件儲存機制
   - [ ] 定義 `IEventStore` 介面
   - [ ] 實作 `InMemoryEventStore` 類別（POC 實作）
   - [ ] 實作 append-only list 儲存
   - [ ] 實作事件查詢（全域 & by companyId）
   - [ ] 實作跨批次 timestamp-CompanyId-EmployeeId-DeviceId lookup
   - [ ] 確保介面設計易於未來切換其他儲存方案

5. **EventCursorHandler** - 建立游標處理服務
   - [ ] 實作 cursor 格式 `{timestamp}|{companyId}|{employeeId}|{deviceId}`
   - [ ] 實作 parse（無效則 fallback）
   - [ ] 實作 next cursor（取最後事件 timestamp|companyId|employeeId|deviceId）

### 第三階段：業務邏輯 (Phase 3)
6. **Validation Service** - 建立驗證服務
   - [ ] 實作 ingestion rules：
     - [ ] 批次內 `timestamp-CompanyId-EmployeeId-DeviceId` 重複 → 拒絕後者
     - [ ] 跨批次 `timestamp-CompanyId-EmployeeId-DeviceId` 重複 → 直接忽略事件
     - [ ] eventType 必填欄位檢查（enter_screen, click, leave_screen）
     - [ ] 未知 eventType / deviceType / os → flags.*
     - [ ] 多餘 metadata / deviceInfo 欄位 → flags.extra_fields=true
   - [ ] 驗證結構完整性（必要欄位是否存在）

### 第四階段：API 實作 (Phase 4)
7. **Controllers** - 建立控制器
   - [ ] 建立 `POST /events/batch` (EventsController.cs)
   - [ ] 建立 `GET /events?t={cursor}&size=100` (EventsController.cs)
   - [ ] 建立 `GET /companies/{companyId}/events?t={cursor}&size=100` (CompaniesController.cs)
   - [ ] 呼叫 ValidationService → EventStore
   - [ ] 回傳：
     - `accepted[] {eventId}`
     - `rejected[] {eventId, error_code, message}`
   - [ ] 實作以 timestamp-CompanyId-EmployeeId-DeviceId 為基礎的查詢遍歷 & size 限制

### 第五階段：整合與測試 (Phase 5)
8. **Infrastructure Setup** - 設定基礎建設
   - [ ] 設定 ASP.NET Core Program.cs
   - [ ] 加入 DI：EventStore / ValidationService / EventCursorHandler
   - [ ] 啟用 Controllers

9. **Testing** - 建立測試
   - [ ] EventStoreTests
     - append-only
     - interface contract tests
   - [ ] CursorTests
     - cursor parse
     - fallback
   - [ ] IngestionRulesTests
     - batch duplicate
     - cross-batch duplicate (直接忽略)
     - metadata missing
     - unknown fields

10. **Docker** - 容器化部署
    - [ ] 建立 multi-stage Dockerfile（位於 `/build/Dockerfile`）
    - [ ] 完成 multi-stage Dockerfile（sdk→aspnet）
    - [ ] 測試本地 build & run

---

## 錯誤處理任務 (獨立於主要流程)

- [ ] 建立全域錯誤處理中間件
- [ ] 實作 API 錯誤回應格式
- [ ] 實作 ValidationService 中的錯誤代碼處理
- [ ] 建立 Logging 服務以記錄錯誤

---

## 相依關係說明

- **EventStore 實作完成** ⇒ 才能實作 Controllers 進行測試
- **ValidationService 依賴 Domain Models** ⇒ 必須先建立領域模型
- **Controllers 依賴 DTOs** ⇒ 需要傳輸物件才能定義 API 合約
- **Integration Tests 依賴 Controllers** ⇒ 必須先完成 API 端點
- **Docker 化依賴完整專案** ⇒ 只能在所有功能完成後進行

---

## 任務相依性圖

```
Phase 1 (Project Setup)
    ↓
Phase 2 (Domain Models & DTOs)
    ↓
Phase 3 (Event Store)
    ↓
Phase 4 (Cursor Service)
    ↓
Phase 5 (Validation Service)
    ↓
Phase 6 (Controllers)
    ↓
Phase 7 (Infrastructure Setup)
    ↓
Phase 8 (Testing & Docker)
```

---

## Manual Verification Checklist

- [ ] POST /events/batch 部分成功邏輯正確
- [ ] 查詢 API 依 timestamp-CompanyId-EmployeeId-DeviceId 順序回傳
- [ ] timestamp-CompanyId-EmployeeId-DeviceId 重複事件正確處理（批次內拒絕，跨批次忽略）
- [ ] cursor iteration 正確運作
- [ ] restart 後記憶體資料清空

