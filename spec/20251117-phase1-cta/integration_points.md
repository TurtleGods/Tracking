# 服務整合機制

## 整體架構

後端系統採用分層架構，各服務之間通過相依性注入 (Dependency Injection) 進行整合。

## 服務間整合關係

### 1. Controllers 整合
- **EventsController** 整合：
  - 依賴 `IEventStore` 進行事件儲存與查詢
  - 依賴 `ValidationService` 進行事件驗證
  - 依賴 `EventCursorHandler` 進行游標處理

- **CompaniesController** 整合：
  - 依賴 `IEventStore` 進行特定公司事件查詢
  - 依賴 `EventCursorHandler` 進行游標處理

### 2. 服務間資料流

#### POST /events/batch 整合流程：
```
1. EventsController.ReceiveBatchEvents()
   ↓
2. ValidationService.ValidateBatchEvents()
   - 驗證批次大小
   - 驗證事件唯一性
   - 驗證必要欄位
   - 標記未知值和多餘欄位
   ↓
3. IEventStore.AppendEvents()
   - 將有效事件加入記憶體儲存
   ↓
4. EventsController.BuildBatchResponse()
   - 整合驗證結果 (accepted/rejected)
   - 返回部分成功響應
```

#### GET /events 整合流程：
```
1. EventsController.GetEvents()
   - 接收 cursor 和 size 參數
   ↓
2. EventCursorHandler.ParseCursor()
   - 解析 cursor 參數
   - 驗證格式，錯誤時自動 fallback
   ↓
3. IEventStore.GetEvents()
   - 根據 cursor 查詢事件
   - 應用 size 限制
   ↓
4. EventCursorHandler.BuildNextCursor()
   - 基於最後一筆事件建立下一個 cursor
   ↓
5. EventsController.BuildQueryResponse()
   - 建立完整的查詢回應
```

#### GET /companies/{companyId}/events 整合流程：
```
1. CompaniesController.GetCompanyEvents()
   - 接收 companyId, cursor 和 size 參數
   ↓
2. EventCursorHandler.ParseCursor()
   - 解析 cursor 參數
   ↓
3. IEventStore.GetEventsByCompany()
   - 根據 companyId 和 cursor 查詢事件
   - 應用 size 限制
   ↓
4. EventCursorHandler.BuildNextCursor()
   - 基於最後一筆事件建立下一個 cursor
   ↓
5. CompaniesController.BuildQueryResponse()
   - 建立完整的查詢回應
```

## 整合點詳細說明

### 1. IEventStore 介面整合
- **設計原則**：使用介面隔離實作相依
- **生命週期**：Singleton，實現連線池效果
- **整合點**：
  - EventsController: AppendEvents(), GetEvents()
  - CompaniesController: GetEventsByCompany()
  - ValidationService: CheckDuplicateEvent() (跨批次檢查)

### 2. ValidationService 整合
- **設計原則**：獨立的驗證邏輯，不依賴其他服務
- **生命週期**：Transient
- **整合點**：
  - 接收事件陣列進行驗證
  - 返回驗證結果包含 accepted 和 rejected 事件
  - 與 IEventStore 整合進行跨批次重複檢查

### 3. EventCursorHandler 整合
- **設計原則**：獨立的游標處理邏輯
- **生命週期**：Transient
- **整合點**：
  - 解析和驗證 cursor 格式
  - 建立下一個 cursor
  - 處理 fallback 邏輯

### 4. AppSettings 整合
- **配置項目**：
  - Batch size limits (default 10, max 10)
  - Query size limits (default 100, min 1, max 100)
  - Logging configuration
- **整合點**：所有服務透過 IConfiguration 介面存取設定值

## 相依性注入配置

在 Program.cs 中配置：

```csharp
// Services
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddTransient<ValidationService>();
builder.Services.AddTransient<EventCursorHandler>();

// Controllers
builder.Services.AddControllers();
```

## 整合測試重點

確保批次提交後可以透過查詢 API 取得事件，驗證各服務間的整合是否正確。