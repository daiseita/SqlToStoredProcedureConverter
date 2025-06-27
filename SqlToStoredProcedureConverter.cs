using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlProcedureConverterWinForm
{
    public class SqlToStoredProcedureConverter
    {
        // 將正則表達式定義為靜態只讀成員，可以提升效能，避免重複編譯
        private static readonly Regex WhereClauseRegex = new Regex(
            @"([A-Za-z0-9_]+)\s*(>=|<=|<>|!=|=|>|<|like)\s*(('[^']*')|(\d+(\.\d+)?))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex UpdateSetRegex = new Regex(
            @"set\s+([A-Za-z0-9_]+)\s*=\s*(('[^']*')|(\d+(\.\d+)?))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 將包含多個SQL命令的字串轉換為一個完整的T-SQL預存程序。
        /// 此方法作為總協調者，呼叫各個輔助函式完成任務。
        /// </summary>
        /// <param name="sql">原始SQL字串</param>
        /// <param name="procedureName">預存程序名稱</param>
        /// <returns>產生的T-SQL腳本</returns>
        public string ConvertToStoredProcedure(string sql, string procedureName)
        {
            // 步驟 1: 將SQL字串分割成獨立的命令
            var statements = SplitSqlStatements(sql);
            if (!statements.Any())
            {
                return "-- 輸入的SQL為空或無效。";
            }

            // 步驟 2: 逐句解析，提取參數並改寫命令
            var allParameters = new HashSet<string>();
            var modifiedStatements = new List<string>();

            foreach (var statement in statements)
            {
                string modifiedStatement = ParseAndParameterizeStatement(statement, allParameters);
                modifiedStatements.Add(modifiedStatement);
            }

            if (!allParameters.Any())
            {
                return $"-- 無法從SQL中解析出有效的參數來建立預存程序 '{procedureName}'。";
            }

            // 步驟 3: 將所有部分組合成最終的預存程序腳本
            return BuildProcedureScript(procedureName, allParameters, modifiedStatements);
        }

        // =================================================================
        // 私有輔助函式 (Private Helper Methods)
        // =================================================================

        /// <summary>
        /// 步驟 1: 分割原始SQL字串為多個獨立的命令。
        /// </summary>
        private List<string> SplitSqlStatements(string sql)
        {
            return sql.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                      .Where(s => !string.IsNullOrWhiteSpace(s))
                      .Select(s => s.Trim())
                      .ToList();
        }

        /// <summary>
        /// 步驟 2.1: 解析單一SQL命令，處理其中的SET和WHERE子句。
        /// </summary>
        private string ParseAndParameterizeStatement(string statement, HashSet<string> parameters)
        {
            // 注意處理順序：先處理 SET，再處理 WHERE，以符合UPDATE語句的結構
            string modifiedStatement = ParameterizeSetClauses(statement, parameters);
            modifiedStatement = ParameterizeWhereClauses(modifiedStatement, parameters);
            return modifiedStatement;
        }

        /// <summary>
        /// 步驟 2.2: 尋找並參數化SET子句 (例如: SET Name = 'John')。
        /// </summary>
        private string ParameterizeSetClauses(string statement, HashSet<string> parameters)
        {
            var matches = UpdateSetRegex.Matches(statement);
            // 從後往前替換，避免因字串長度改變導致索引錯亂
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                var fieldName = match.Groups[1].Value;
                parameters.Add(fieldName);

                string replacementClause = $"SET {fieldName} = @{fieldName}";
                statement = statement.Substring(0, match.Index) + replacementClause + statement.Substring(match.Index + match.Length);
            }
            return statement;
        }

        /// <summary>
        /// 步驟 2.3: 尋找並參數化WHERE子句中的條件。
        /// </summary>
        private string ParameterizeWhereClauses(string statement, HashSet<string> parameters)
        {
            var matches = WhereClauseRegex.Matches(statement);
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match match = matches[i];
                var fieldName = match.Groups[1].Value;
                var op = match.Groups[2].Value;
                parameters.Add(fieldName);

                string replacementClause = $"{fieldName} {op} @{fieldName}";
                statement = statement.Substring(0, match.Index) + replacementClause + statement.Substring(match.Index + match.Length);
            }
            return statement;
        }

        /// <summary>
        /// 步驟 3.1: 組合所有部分，建立最終的腳本。
        /// </summary>
        private string BuildProcedureScript(string procedureName, HashSet<string> parameters, List<string> modifiedStatements)
        {
            var sb = new StringBuilder();
            sb.Append(BuildProcedureHeader(procedureName));
            sb.Append(BuildParameterBlock(parameters));
            sb.Append(BuildProcedureBody(modifiedStatements));
            return sb.ToString();
        }

        /// <summary>
        /// 步驟 3.2: 建立預存程序的頭部 (CREATE OR ALTER ...)。
        /// </summary>
        private string BuildProcedureHeader(string procedureName)
        {
            return $"CREATE OR ALTER PROCEDURE {procedureName}\n";
        }

        /// <summary>
        /// 步驟 3.3: 根據收集到的參數，建立參數定義區塊。
        /// </summary>
        private string BuildParameterBlock(HashSet<string> parameters)
        {
            var sb = new StringBuilder();
            int count = 0;
            foreach (var param in parameters.OrderBy(p => p)) // 排序讓產生的腳本更穩定
            {
                sb.Append($"    @{param} nvarchar(max) = NULL");
                count++;
                if (count < parameters.Count)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// 步驟 3.4: 建立預存程序的主體 (BEGIN ... END)。
        /// </summary>
        private string BuildProcedureBody(List<string> modifiedStatements)
        {
            var sb = new StringBuilder();
            sb.AppendLine("AS");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    SET NOCOUNT ON;");
            sb.AppendLine();

            foreach (var finalStatement in modifiedStatements)
            {
                sb.AppendLine($"    {finalStatement};");
            }

            sb.AppendLine();
            sb.AppendLine("END");
            return sb.ToString();
        }
    }
}