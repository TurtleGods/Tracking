# Mayo.Platform.Tracklix.WebAPI 執行教學

這份文件提供了如何建置、執行及測試 `Mayo.Platform.Tracklix.WebAPI` 專案的指引。

## 目錄
1.  [前置需求](#1-前置需求)
2.  [建置專案](#2-建置專案)
3.  [執行 Web API](#3-執行-web-api)
    *   [直接執行](#直接執行)
    *   [使用 Docker 執行](#使用-docker-執行)
4.  [執行測試](#4-執行測試)

---

## 1. 前置需求

在開始之前，請確保您的開發環境已安裝以下工具：

*   **.NET SDK 10.0 (或更新版本)**：用於建置和執行 .NET 應用程式。
    *   您可以從 [Microsoft .NET 下載頁面](https://dotnet.microsoft.com/download/dotnet) 安裝。
*   **Docker (選用)**：如果您希望使用 Docker 容器化執行應用程式，則需要安裝 Docker Desktop。
    *   您可以從 [Docker 官方網站](https://www.docker.com/products/docker-desktop) 安裝。

## 2. 建置專案

1.  開啟您的終端機或命令提示字元。
2.  導航到專案的根目錄 `C:\Users\kevin_wang\RiderProjects\Mayo.Platform.Tracklix`。
3.  執行以下命令來還原專案依賴項並建置專案：

    ```bash
    dotnet restore
    dotnet build src/Mayo.Platform.Tracklix.WebAPI/Mayo.Platform.Tracklix.WebAPI.csproj -c Release
    ```

    這將會編譯您的 Web API 專案。

## 3. 執行 Web API

您可以選擇直接執行應用程式，或透過 Docker 容器執行。

### 直接執行

1.  在專案根目錄下，導航到 Web API 專案目錄：

    ```bash
    cd src/Mayo.Platform.Tracklix.WebAPI
    ```

2.  執行以下命令來啟動 Web API：

    ```bash
    dotnet run --project Mayo.Platform.Tracklix.WebAPI.csproj
    ```

3.  API 啟動後，您應該會在終端機中看到應用程式監聽的 URL (例如：`https://localhost:XXXX` 或 `http://localhost:YYYY`)。
    *   在開發模式下，您通常可以透過瀏覽器訪問 `https://localhost:XXXX/swagger` 來查看 Swagger UI，探索可用的 API 端點。

### 使用 Docker 執行

1.  確保您的 Docker Desktop 正在運行。
2.  在專案的根目錄下，執行以下命令來建置 Docker 映像：

    ```bash
    docker build -t mayo.platform.tracklix.webapi -f build/Dockerfile .
    ```
    *   這將使用 `build/Dockerfile` 來建置一個名為 `mayo.platform.tracklix.webapi` 的 Docker 映像。

3.  建置完成後，執行以下命令來運行 Docker 容器：

    ```bash
    docker run -d -p 8080:80 -p 8081:443 --name tracklix-api mayo.platform.tracklix.webapi
    ```
    *   `-d`：在背景運行容器。
    *   `-p 8080:80`：將容器的 80 埠映射到主機的 8080 埠 (HTTP)。
    *   `-p 8081:443`：將容器的 443 埠映射到主機的 8081 埠 (HTTPS)。
    *   `--name tracklix-api`：為容器指定一個名稱。

4.  容器啟動後，您可以透過 `http://localhost:8080` 或 `https://localhost:8081` 訪問 API。
    *   如果 Docker 運行在開發模式，您可以嘗試訪問 `https://localhost:8081/swagger` 來查看 Swagger UI。

## 4. 執行測試

1.  在專案的根目錄下，導航到測試專案目錄：

    ```bash
    cd tests/Mayo.Platform.Tracklix.WebAPI.Tests
    ```

2.  執行以下命令來運行所有測試：

    ```bash
    dotnet test
    ```

    這將會執行 `Mayo.Platform.Tracklix.WebAPI.Tests` 專案中的所有單元測試和整合測試，並顯示結果。
