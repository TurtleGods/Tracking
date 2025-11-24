# 錯誤代碼與訊息清單

## 批次事件提交錯誤 (POST /events/batch)

### 批次驗證錯誤
- **錯誤代碼**: `BATCH_SIZE_EXCEEDED`
- **訊息**: "Batch size exceeds maximum allowed events of 10"
- **說明**: 當批次提交的事件數量超過 10 筆時觸發

### 事件唯一性錯誤
- **錯誤代碼**: `DUPLICATE_EVENT_ID_IN_BATCH`
- **訊息**: "Duplicate eventId in the same batch: {eventId}"
- **說明**: 批次內存在重複的 eventId 時觸發

### 必填欄位缺失錯誤
- **錯誤代碼**: `MISSING_PRODUCT_ID`
- **訊息**: "productId is required"
- **說明**: 事件缺少必要的 productId 欄位時觸發

- **錯誤代碼**: `MISSING_COMPANY_ID`
- **訊息**: "companyId is required"
- **說明**: 事件缺少必要的 companyId 欄位時觸發

- **錯誤代碼**: `MISSING_EMPLOYEE_ID`
- **訊息**: "employeeId is required"
- **說明**: 事件缺少必要的 employeeId 欄位時觸發

- **錯誤代碼**: `MISSING_SESSION_ID`
- **訊息**: "sessionId is required"
- **說明**: 事件缺少必要的 sessionId 欄位時觸發

- **錯誤代碼**: `MISSING_SCREEN_ID`
- **訊息**: "screenId is required"
- **說明**: 事件缺少必要的 screenId 欄位時觸發

- **錯誤代碼**: `MISSING_EVENT_TYPE`
- **訊息**: "eventType is required"
- **說明**: 事件缺少必要的 eventType 欄位時觸發

- **錯誤代碼**: `MISSING_TIMESTAMP`
- **訊息**: "timestamp is required"
- **說明**: 事件缺少必要的 timestamp 欄位時觸發

- **錯誤代碼**: `MISSING_DEVICE_ID`
- **訊息**: "deviceId is required"
- **說明**: 事件缺少必要的 deviceId 欄位時觸發

### Metadata 欄位驗證錯誤
- **錯誤代碼**: `MISSING_METADATA_VIEW`
- **訊息**: "metadata.view required for enter_screen event"
- **說明**: enter_screen 事件缺少必要的 metadata.view 欄位時觸發

- **錯誤代碼**: `MISSING_METADATA_COMPONENT_ID`
- **訊息**: "metadata.componentId required for click event"
- **說明**: click 事件缺少必要的 metadata.componentId 欄位時觸發

- **錯誤代碼**: `MISSING_METADATA_DURATION`
- **訊息**: "metadata.duration required for leave_screen event"
- **說明**: leave_screen 事件缺少必要的 metadata.duration 欄位時觸發

### 列舉值驗證錯誤
- **錯誤代碼**: `INVALID_EVENT_TYPE`
- **訊息**: "Invalid eventType: {eventType}. Valid values are enter_screen, click, leave_screen"
- **說明**: eventType 不是有效值時觸發

- **錯誤代碼**: `INVALID_DEVICE_TYPE`
- **訊息**: "Invalid deviceType: {deviceType}. Valid values are Android, IOS, Browser"
- **說明**: deviceType 不是有效值時觸發



### 欄位格式錯誤
- **錯誤代碼**: `INVALID_TIMESTAMP_FORMAT`
- **訊息**: "Invalid timestamp format. Must be epoch milliseconds"
- **說明**: timestamp 格式不正確時觸發

- **錯誤代碼**: `INVALID_DURATION_FORMAT`
- **訊息**: "Invalid duration format. Must be a number representing milliseconds"
- **說明**: duration 格式不正確時觸發

- **錯誤代碼**: `INVALID_EVENT_ID_FORMAT`
- **訊息**: "Invalid eventId format. Expected format: {timestamp}-{companyId}-{employeeId}-{deviceType}-{deviceId}"
- **說明**: eventId 格式不符合預期格式時觸發

### 多餘欄位錯誤
- **錯誤代碼**: `EXTRA_FIELDS_IN_METADATA`
- **訊息**: "Extra fields found in metadata: {fieldNames}"
- **說明**: metadata 包含規格外的欄位時觸發

- **錯誤代碼**: `EXTRA_FIELDS_IN_DEVICEINFO`
- **訊息**: "Extra fields found in deviceInfo: {fieldNames}"
- **說明**: deviceInfo 包含規格外的欄位時觸發

## 查詢錯誤 (GET /events, GET /companies/{companyId}/events)

### 參數驗證錯誤
- **錯誤代碼**: `INVALID_CURSOR_FORMAT`
- **訊息**: "Invalid cursor format. Expected format: {timestamp}|{companyId}|{employeeId}|{deviceType}|{deviceId}"
- **說明**: 游標格式不正確時觸發（自動 fallback 到開頭）

- **錯誤代碼**: `INVALID_SIZE_PARAMETER`
- **訊息**: "Invalid size parameter. Must be between 1 and 100"
- **說明**: size 參數超出有效範圍時觸發

### 公司 ID 錯誤
- **錯誤代碼**: `INVALID_COMPANY_ID_FORMAT`
- **訊息**: "Invalid companyId format. Must be a valid GUID"
- **說明**: companyId 格式不正確時觸發

## 錯誤訊息格式標準 (RFC 9457)

所有錯誤訊息應遵循 RFC 9457 標準：
- 包含 human-readable 訊息
- 包含機器可讀的錯誤代碼
- 包含可能的解決方案或額外資訊（如果適用）
- 使用一致的語法和格式