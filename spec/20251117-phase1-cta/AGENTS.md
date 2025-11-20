# 使用者行為埋點分析系統（POC）

## 簡介
這個資料夾包含了使用者行為埋點分析系統第一階段（CTA - Call to Action）的規格文件，建立於 2025 年 11 月 17 日。

## 專案目標
建立一個 POC（概念驗證）等級的後端系統，用於接收與查詢前端送出的使用者行為事件，強調：
- 資料格式正確性
- 行為操作可追蹤性
- 查詢便利性

本系統**不需支援**高吞吐量、聚合統計、視覺化、驗證機制、資料清理或持久化 DB。

## 檔案說明
- `spec.md`: 需求規格文件，定義系統功能與行為
- `plan.md`: 技術實作計畫，包含技術選型與系統設計
- `tasks.md`: 實作任務拆解，追蹤開發進度
- `domain-model.md`: 領域模型定義
- `data-model.md`: 資料模型定義
- `openapi-spec.yaml`: API 的 OpenAPI 規格定義
- `integration_points.md`: 服務整合機制與相依性
- `error_codes.md`: 錯誤代碼與訊息清單
- `validation_rules.md`: 多重 Flag 處理與驗證規則優先順序
- `README.md`: 文件目錄與說明

## 主要功能
1. **行為事件紀錄**：接受前端批次事件提交（最多 10 筆）
2. **事件驗證**：實施資料驗證規則（Ingestion Rules）
3. **事件查詢**：支援 key-based cursor 查詢功能
4. **多租戶支援**：基於 companyId 的資料隔離

## API 端點
- `POST /events/batch`: 批次提交事件
- `GET /events`: 查詢所有事件
- `GET /companies/{companyId}/events`: 查詢特定公司的事件

## 技術架構
- **後端框架**：.NET 10 (ASP.NET Core)
- **資料儲存**：記憶體內儲存（POC 階段）
- **API 風格**：RESTful Controllers
- **容器化**：Docker 支援

## 文件目錄

[需求規格](spec.md)
[實作計畫](plan.md)
[任務展開](task.md)
[領域模型](domain-model.md)
[資料模型](data-model.md)
[OpenAPI 規格](openapi-spec.yaml)
[integration_points.md](integration_points.md)
[error_codes.md](error_codes.md)
[validation_rules.md](validation_rules.md)
