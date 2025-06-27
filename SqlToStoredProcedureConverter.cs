using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlProcedureConverterWinForm
{
    public class SqlToStoredProcedureConverter
    {
        // �N���h��F���w�q���R�A�uŪ�����A�i�H���ɮį�A�קK���ƽsĶ
        private static readonly Regex WhereClauseRegex = new Regex(
            @"([A-Za-z0-9_]+)\s*(>=|<=|<>|!=|=|>|<|like)\s*(('[^']*')|(\d+(\.\d+)?))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex UpdateSetRegex = new Regex(
            @"set\s+([A-Za-z0-9_]+)\s*=\s*(('[^']*')|(\d+(\.\d+)?))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// �N�]�t�h��SQL�R�O���r���ഫ���@�ӧ��㪺T-SQL�w�s�{�ǡC
        /// ����k�@���`��ժ̡A�I�s�U�ӻ��U�禡�������ȡC
        /// </summary>
        /// <param name="sql">��lSQL�r��</param>
        /// <param name="procedureName">�w�s�{�ǦW��</param>
        /// <returns>���ͪ�T-SQL�}��</returns>
        public string ConvertToStoredProcedure(string sql, string procedureName)
        {
            // �B�J 1: �NSQL�r����Φ��W�ߪ��R�O
            var statements = SplitSqlStatements(sql);
            if (!statements.Any())
            {
                return "-- ��J��SQL���ũεL�ġC";
            }

            // �B�J 2: �v�y�ѪR�A�����Ѽƨç�g�R�O
            var allParameters = new HashSet<string>();
            var modifiedStatements = new List<string>();

            foreach (var statement in statements)
            {
                string modifiedStatement = ParseAndParameterizeStatement(statement, allParameters);
                modifiedStatements.Add(modifiedStatement);
            }

            if (!allParameters.Any())
            {
                return $"-- �L�k�qSQL���ѪR�X���Ī��Ѽƨӫإ߹w�s�{�� '{procedureName}'�C";
            }

            // �B�J 3: �N�Ҧ������զX���̲ת��w�s�{�Ǹ}��
            return BuildProcedureScript(procedureName, allParameters, modifiedStatements);
        }

        // =================================================================
        // �p�����U�禡 (Private Helper Methods)
        // =================================================================

        /// <summary>
        /// �B�J 1: ���έ�lSQL�r�ꬰ�h�ӿW�ߪ��R�O�C
        /// </summary>
        private List<string> SplitSqlStatements(string sql)
        {
            return sql.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                      .Where(s => !string.IsNullOrWhiteSpace(s))
                      .Select(s => s.Trim())
                      .ToList();
        }

        /// <summary>
        /// �B�J 2.1: �ѪR��@SQL�R�O�A�B�z�䤤��SET�MWHERE�l�y�C
        /// </summary>
        private string ParseAndParameterizeStatement(string statement, HashSet<string> parameters)
        {
            // �`�N�B�z���ǡG���B�z SET�A�A�B�z WHERE�A�H�ŦXUPDATE�y�y�����c
            string modifiedStatement = ParameterizeSetClauses(statement, parameters);
            modifiedStatement = ParameterizeWhereClauses(modifiedStatement, parameters);
            return modifiedStatement;
        }

        /// <summary>
        /// �B�J 2.2: �M��ðѼƤ�SET�l�y (�Ҧp: SET Name = 'John')�C
        /// </summary>
        private string ParameterizeSetClauses(string statement, HashSet<string> parameters)
        {
            var matches = UpdateSetRegex.Matches(statement);
            // �q�᩹�e�����A�קK�]�r����ק��ܾɭP���޿���
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
        /// �B�J 2.3: �M��ðѼƤ�WHERE�l�y��������C
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
        /// �B�J 3.1: �զX�Ҧ������A�إ̲߳ת��}���C
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
        /// �B�J 3.2: �إ߹w�s�{�Ǫ��Y�� (CREATE OR ALTER ...)�C
        /// </summary>
        private string BuildProcedureHeader(string procedureName)
        {
            return $"CREATE OR ALTER PROCEDURE {procedureName}\n";
        }

        /// <summary>
        /// �B�J 3.3: �ھڦ����쪺�ѼơA�إ߰ѼƩw�q�϶��C
        /// </summary>
        private string BuildParameterBlock(HashSet<string> parameters)
        {
            var sb = new StringBuilder();
            int count = 0;
            foreach (var param in parameters.OrderBy(p => p)) // �Ƨ������ͪ��}����í�w
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
        /// �B�J 3.4: �إ߹w�s�{�Ǫ��D�� (BEGIN ... END)�C
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