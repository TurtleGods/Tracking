# task.md — Implementation Task Breakdown  
(derived from **spec.md** and **plan.md**)

---

## 1. Project Setup

- [ ] 建立 .NET 10 ASP.NET Core 專案 (`Api`)
- [ ] 建立 Solution (`Project.sln`)
- [ ] 設定目錄結構（Controllers / Services / Models / Dtos）
- [ ] 建立 multi-stage Dockerfile（位於 `/build/Dockerfile`）
- [ ] 建立 `appsettings.Development.json`

---

## 2. Domain Models（Models/）

- [ ] 建立 `Event.cs`
  - `productId`, `companyId`, `employeeId`, `sessionId`, `screenId`
  - `eventType`, `timestamp`, `eventId`
  - `metadata`, `deviceInfo`, `flags`, `seqId`
- [ ] 建立 `EventMetadata.cs`
  - `view`（enter_screen）
  - `componentId`（click）
  - `duration`（leave_screen）
- [ ] 建立 `DeviceInfo.cs`
  - `deviceType`, `os`
- [ ] 建立 `EventResponseModels.cs`（accepted / rejected model）

---

## 3. DTOs（Dtos/）

- [ ] 建立 `BatchRequestDto.cs`
  - `{ events: [...] }`（最多 10 筆）
- [ ] 建立 `QueryResponseDto.cs`
  - `events`, `next_cursor`, `size`

---

## 4. Event Store Interface and Implementation（Services/）

- [ ] 定義 `IEventStore` 介面
- [ ] 實作 `InMemoryEventStore` 類別（POC 實作）
- [ ] 實作 append-only list 儲存
- [ ] 實作 global 自增 `seqId`（考慮併發安全）
- [ ] 實作事件查詢（全域 & by companyId）
- [ ] 實作跨批次 eventId lookup（供 duplicated 判斷）
- [ ] 確保介面設計易於未來切換其他儲存方案

---

## 5. ValidationService（Services/ValidationService.cs）

- [ ] 實作 ingestion rules：
  - [ ] 批次內 `eventId` 重複 → 拒絕後者
  - [ ] 跨批次 `eventId` 重複 → flags.duplicate_eventId=true
  - [ ] eventType 必填欄位檢查（enter_screen, click, leave_screen）
  - [ ] 未知 eventType / deviceType / os → flags.*
  - [ ] 多餘 metadata / deviceInfo 欄位 → flags.extra_fields=true
- [ ] 驗證結構完整性（必要欄位是否存在）

---

## 6. CursorService（Services/CursorService.cs）

- [ ] 實作 cursor 格式 `{timestamp}|{opaqueUUID}`
- [ ] 實作 parse（無效則 fallback）
- [ ] 實作 next cursor（取最後事件 timestamp + UUID）

---

## 7. Controllers

### 7.1 `EventsController.cs`

- [ ] 建立 `POST /events/batch`
- [ ] 呼叫 ValidationService → EventStore
- [ ] 回傳：
  - `accepted[] {eventId, seqId}`
  - `rejected[] {eventId, error_code, message}`

### 7.2 `CompaniesController.cs`

- [ ] 建立 `GET /events?t={cursor}&size=100`
- [ ] 建立 `GET /companies/{companyId}/events?t={cursor}&size=100`
- [ ] 實作 cursor 遍歷 & size 限制

---

## 8. Supporting Infrastructure

- [ ] 設定 ASP.NET Core Program.cs  
  - 加入 DI：EventStore / ValidationService / CursorService
  - 啟用 Controllers

---

## 9. Testing（tests/Api.Tests/）

- [ ] EventStoreTests
  - append-only
  - seqId 遞增
  - 併發安全測試（seqId 生成、事件寫入）
  - interface contract tests
- [ ] CursorTests
  - cursor parse
  - fallback
- [ ] IngestionRulesTests
  - batch duplicate
  - cross-batch duplicate
  - metadata missing
  - unknown fields

---

## 10. Docker

- [ ] 完成 multi-stage Dockerfile（sdk→aspnet）
- [ ] 測試本地 build & run

---

## 11. Manual Verification Checklist

- [ ] POST /events/batch 部分成功邏輯正確  
- [ ] flags 正確填寫  
- [ ] 查詢 API 依 seqId 順序回傳  
- [ ] cursor iteration 正確運作  
- [ ] restart 後記憶體資料清空

