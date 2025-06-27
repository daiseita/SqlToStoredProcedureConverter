1. 概觀
SqlToStoredProcedureConverter 是一個 C# 工具類別，其主要目的是將包含硬編碼（Hard-coded）值的原始 SQL 字串，自動轉換為一個結構化、參數化的 T-SQL 預存程序（Stored Procedure）。

這個工具解決了需要手動將現有查詢改寫為預存程序的繁瑣工作，特別適合用於將舊有系統中的動態 SQL 語句標準化，以提高資料庫的效能、安全性與可維護性。

2. 主要功能與特色
支援多語句處理：能夠解析以分號 (;) 分隔的多個 SQL 命令（如 UPDATE...; UPDATE...;）。
自動參數化：
自動偵測 WHERE 子句中的條件（欄位 = '值' 或 欄位 >= 數字）。
自動偵測 UPDATE 語句中 SET 的賦值（SET 欄位 = '值'）。
將偵測到的硬編碼值轉換為預存程序的參數。
廣泛的運算子支援：支援 =, like, >, <, >=, <=, <>, != 等多種常用比較運算子。
多樣的值類型支援：可解析的值包括單引號包圍的字串（'string'）和數值（123 或 123.45）。
結構化腳本生成：產生格式工整、可讀性高的 CREATE OR ALTER PROCEDURE T-SQL 腳本。
提升程式碼品質：透過將複雜的轉換邏輯拆分為多個獨立的輔助函式，大幅提升了類別本身的可讀性和可維護性。
3. 如何使用
使用方式非常直觀，只需建立 SqlToStoredProcedureConverter 的實體，然後呼叫其 ConvertToStoredProcedure 方法即可。

C# 使用範例:

C#

using System;

namespace SqlProcedureConverterWinForm
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. 建立轉換器物件的實體
            var converter = new SqlToStoredProcedureConverter();

            // 2. 定義你的原始 SQL 字串和預存程序名稱
            string originalSql = "update D01 set D01F01NV0064 ='新地點' where D01I02UV0010 ='ID001';" +
                                 "update D11 set D11I03CV0015 = 100 where D11I07JJD01I02 = 'ID001';";
            string procedureName = "usp_UpdateDeviceInfo";

            // 3. 呼叫轉換方法
            string generatedScript = converter.ConvertToStoredProcedure(originalSql, procedureName);

            // 4. 輸出結果
            Console.WriteLine("--- Generated T-SQL Script ---");
            Console.WriteLine(generatedScript);
        }
    }
}
4. API 參考
public string ConvertToStoredProcedure(string sql, string procedureName)
這是唯一的公開方法，負責執行整個轉換過程。

描述：
接收一個原始 SQL 字串和一個預存程序名稱，回傳一個完整的 CREATE OR ALTER PROCEDURE T-SQL 腳本字串。

參數：

sql (string)：
必要。 包含一個或多個 SQL 命令的字串。
多個命令之間必須使用分號 (;) 分隔。
支援的參數化語法包括 WHERE 欄位 op '值/數值' 和 SET 欄位 = '值/數值'。
procedureName (string)：
必要。 您希望產生的預存程序名稱。
返回值 (string)：

一個格式化後的 T-SQL 腳本字串。
如果無法從輸入的 SQL 中解析出任何參數，將回傳提示錯誤訊息的字串。
5. 範例
範例 1：包含多個 UPDATE 語句
輸入 SQL:

SQL

update D01 set D01F01NV0064 ='新地點' where D01I02UV0010 ='ID001';update D11 set D11I03CV0015 = 100 where D11I07JJD01I02 ='ID001';
產生腳本:

SQL

CREATE OR ALTER PROCEDURE usp_UpdateDeviceInfo
    @D01F01NV0064 nvarchar(max) = NULL,
    @D01I02UV0010 nvarchar(max) = NULL,
    @D11I03CV0015 nvarchar(max) = NULL,
    @D11I07JJD01I02 nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE D01 SET D01F01NV0064 = @D01F01NV0064 WHERE D01I02UV0010 = @D01I02UV0010;
    UPDATE D11 SET D11I03CV0015 = @D11I03CV0015 WHERE D11I07JJD01I02 = @D11I07JJD01I02;

END
範例 2：包含 SELECT 語句與多種條件
輸入 SQL:

SQL

SELECT * FROM L82 WHERE L82D01 >= 20220711 AND L82F01NV0064 LIKE '%台北%'
產生腳本:

SQL

CREATE OR ALTER PROCEDURE usp_GetL82Data
    @L82D01 nvarchar(max) = NULL,
    @L82F01NV0064 nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM L82 WHERE L82D01 >= @L82D01 AND L82F01NV0064 LIKE @L82F01NV0064;

END
6. 重要事項與限制
參數類型簡化：

為了通用性，所有產生的參數類型均為 NVARCHAR(MAX)。這意味著當與 INT 或 DATETIME 等數值/日期類型的資料庫欄位進行比較時，SQL Server 會進行隱含的型別轉換。這可能會影響索引的使用效率，在極端情況下可能導致效能問題。
邏輯的安全性：

此工具在參數化 UPDATE 或 DELETE 語句時，不會產生 (@Param IS NULL OR ...) 的選擇性篩選邏輯。這是一個刻意的安全設計，以防止因傳入 NULL 參數而意外更新或刪除整個資料表。產生的預存程序會忠實地執行 WHERE Field = @Field 的比較。
SQL 語法支援：

目前的解析能力基於正則表達式，僅支援特定模式。對於更複雜的 SQL 語法，例如 IN (...)、BETWEEN ... AND ...、IS NULL、子查詢等，目前版本尚不支援。
語法正確性：

本工具不會驗證輸入的 SQL 語法是否正確。它僅負責尋找並替換符合模式的條件。如果輸入的 SQL 本身有誤，產生的預存程序也將包含錯誤的語法。
7. 未來可能的改進方向
智慧型別推斷：連接資料庫，查詢系統資料表 (sys.columns) 來自動判斷欄位的確切資料類型，並產生對應的參數類型（如 INT, DATETIME2）。
擴充語法支援：增強正則表達式或引入更專業的 SQL 解析器（Parser）來支援 IN、BETWEEN 等更複雜的子句。
可配置的參數化邏輯：允許使用者選擇為 SELECT 語句啟用 (@Param IS NULL OR ...) 的可選篩選邏輯，同時為 UPDATE/DELETE 保持目前的安全邏輯。
