# 多重 Flag 處理與驗證規則優先順序

## 多重 Flag 處理機制

當單一事件同時觸發多種標記條件時，系統會將所有符合的 flags 標記為 true，並進行累積處理。

### Flag 組合範例

**範例 1：同時觸發 unknown_eventType 和 extra_fields**
```json
{
  "eventType": "custom_event",  // 不在允許值內
  "custom_field": "value",      // 多餘欄位
  "flags": {
    "unknown_eventType": true,
    "extra_fields": true
  }
}
```



### Flag 處理策略

1. **累積標記**：所有符合的 flags 都會被設為 true
2. **不互斥**：不同類型的 flags 可以同時存在
3. **記錄優先**：即使有多個問題，只要不是拒絕條件，事件仍會被記錄

## 驗證規則優先順序

當單一事件觸發多項驗證規則時，系統按照以下順序執行驗證：

### 第一層：結構性驗證（拒絕事件）
1. **批次大小檢查** (`BATCH_SIZE_EXCEEDED`)
   - 錯誤代碼: `BATCH_SIZE_EXCEEDED`
   - 訊息: "Batch size exceeds maximum allowed events of 10"

2. **批次內重複檢查** (`DUPLICATE_EVENT_ID_IN_BATCH`)
   - 錯誤代碼: `DUPLICATE_EVENT_ID_IN_BATCH`
   - 訊息: "Duplicate eventId in the same batch: {eventId}"

### 第二層：必要欄位驗證（拒絕事件）
3. **必填欄位檢查**
   - `MISSING_PRODUCT_ID`: "productId is required"
   - `MISSING_COMPANY_ID`: "companyId is required"
   - `MISSING_EMPLOYEE_ID`: "employeeId is required"
   - `MISSING_SESSION_ID`: "sessionId is required"
   - `MISSING_SCREEN_ID`: "screenId is required"
   - `MISSING_EVENT_TYPE`: "eventType is required"
   - `MISSING_TIMESTAMP`: "timestamp is required"
   - `MISSING_DEVICE_ID`: "deviceId is required"

4. **特定 eventType 必填欄位檢查**
   - `MISSING_METADATA_VIEW`: "metadata.view required for enter_screen event"
   - `MISSING_METADATA_COMPONENT_ID`: "metadata.componentId required for click event"
   - `MISSING_METADATA_DURATION`: "metadata.duration required for leave_screen event"

### 第三層：格式與列舉值驗證（標記但接受）
5. **列舉值驗證**（標記為 unknown_*）
   - `INVALID_EVENT_TYPE`: "Invalid eventType: {eventType}. Valid values are enter_screen, click, leave_screen"
      - `INVALID_DEVICE_TYPE`: "Invalid deviceType: {deviceType}. Valid values are Android, IOS, Browser"
   

6. **格式驗證**
   - `INVALID_TIMESTAMP_FORMAT`: "Invalid timestamp format. Must be epoch milliseconds"
   - `INVALID_DURATION_FORMAT`: "Invalid duration format. Must be a number representing milliseconds"

### 第四層：多餘欄位檢查（標記但接受）
7. **多餘欄位檢查**（標記為 extra_fields）
   - `EXTRA_FIELDS_IN_METADATA`: "Extra fields found in metadata: {fieldNames}"
   - `EXTRA_FIELDS_IN_DEVICEINFO`: "Extra fields found in deviceInfo: {fieldNames}"

## 執行流程圖

```
收到事件
    ↓
結構性驗證（批次大小、批次內重複）
    ↓
是否被拒絕？ → 是 → 加入 rejected[]，結束處理
    ↓ 否
必要欄位驗證
    ↓
是否被拒絕？ → 是 → 加入 rejected[]，結束處理
    ↓ 否
格式與列舉值驗證（標記 flags）
    ↓
多餘欄位檢查（標記 extra_fields flag）
    ↓
加入 accepted[]，儲存事件
```

## 決策矩陣

| 條件 | 處理方式 | 結果 |
|------|----------|------|
| 批次內重複 | 拒絕後者，接受第一筆 | 拒絕 |
| 必填欄位缺失 | 拒絕事件 | 拒絕 |
| 未知 eventType | 標記 unknown_eventType flag | 接受 |
| 多餘欄位 | 標記 extra_fields flag | 接受 |
| 多種問題同時發生 | 標記所有對應 flags | 根據最優先規則處理 |

## 實作建議

在 ValidationService 中建立分層驗證方法：

```csharp
public ValidationResult ValidateEvent(Event @event)
{
    var result = new ValidationResult();
    
    // 第一層：結構性問題（拒絕）
    if (HasStructuralIssues(@event))
    {
        result.IsRejected = true;
        result.ErrorCode = GetStructuralErrorCode(@event);
        result.Message = GetStructuralErrorMessage(@event);
        return result;
    }
    
    // 第二層：必要欄位（拒絕）
    if (HasMissingRequiredFields(@event))
    {
        result.IsRejected = true;
        result.ErrorCode = GetRequiredFieldErrorCode(@event);
        result.Message = GetRequiredFieldErrorMessage(@event);
        return result;
    }
    
    // 第三層與第四層：標記問題（接受）
    result.Flags = GetEventFlags(@event);
    
    return result;
}
```