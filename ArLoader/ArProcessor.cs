using System.Configuration;
using System.Data.SqlClient;
using System.Text;

namespace ArLoader
{
    internal class ArProcessor
    {
        private string _definationFilePath;
        private string _arFilePath;
        private string _connectionString = "Server=USDCSHRSQLDV010;Database=AesOpsSupport;Trusted_Connection=True;";

        public ArProcessor(string definationFilePath, string arFilePath)
        {
            _definationFilePath = definationFilePath;
            _arFilePath = arFilePath;
        }

        internal void Process()
        {
            ValidateFiles();

            CreateTable();

            LoadData();
        }

        private void ValidateFiles()
        {
            //if defination file is not found, throw exception
            if (!File.Exists(_definationFilePath))
            {
                throw new FileNotFoundException("Defination file not found");
            }

            //if ar file is not found, throw exception
            if (!File.Exists(_arFilePath))
            {
                throw new FileNotFoundException("Ar file not found");
            }
        }

        private void CreateTable()
        {
            //read defination file
            var definationFile = File.ReadAllLines(_definationFilePath);

            //if defination file is empty, throw exception
            if (definationFile.Length == 0)
            {
                throw new Exception("Defination file is empty");
            }

            //create table sql from defination file
            StringBuilder tableSql = new StringBuilder();

            //get ar file name without extension
            string tableName = Path.GetFileNameWithoutExtension(_arFilePath);

            //include drop table if exists script
            tableSql.AppendLine("IF OBJECT_ID('" + tableName + "', 'U') IS NOT NULL DROP TABLE " + tableName);

            //script for create table
            tableSql.AppendLine("CREATE TABLE dbo.[" + tableName + "] (");

            //loop through defination file to create columns script
            foreach (string line in definationFile)
            {
                //split line by space
                string[] definition = line.Split(',');

                //if column length is not 3, throw exception
                if (definition.Length < 3)
                {
                    throw new Exception("Defination file is not valid");
                }

                tableSql.Append(definition[0]);

                switch (definition[1])
                {
                    case "N":
                        tableSql.Append($" decimal({definition[2]},{definition[3]})");
                        break;
                    case "C":
                        tableSql.Append(" varchar(" + definition[2] + ")");
                        break;
                    case "D":
                        tableSql.Append(" datetime");
                        break;
                    default:
                        throw new Exception("Defination file is not valid");
                }

                //add column to table script
                tableSql.AppendLine(",");
            }

            //remove last comma
            tableSql.Remove(tableSql.Length - 1, 1);

            //close table script
            tableSql.AppendLine(")");

            //create table
            ExecuteSql(tableSql.ToString());
        }

        private void LoadData()
        {
            //read defination file
            var definationFile = File.ReadAllLines(_definationFilePath);

            //get defination file name without extension
            string tableName = Path.GetFileNameWithoutExtension(_arFilePath);

            List<string> columnNames = new List<string>();
            List<int> columnWidth = new List<int>();
            List<char> columnType = new List<char>();

            //get column names from defination file
            foreach (var line in definationFile)
            {
                string[] fieldInfo = line.Split(',');

                columnNames.Add(fieldInfo[0]);
                columnType.Add(fieldInfo[1][0]);
                int width = int.Parse(fieldInfo[2]);

                columnWidth.Add(width);
            }

            //get data from ar file
            var arFile = File.ReadAllLines(_arFilePath);

            //loop through each line in ar file and skip first line
            for (int lineNumber = 1; lineNumber < arFile.Length; lineNumber++)
            {
                //get current line
                string line = arFile[lineNumber];

                //get data from each line
                string[] data = SplitByWidth(line, columnWidth.ToArray());

                //create sql command
                StringBuilder sql = new StringBuilder();

                sql.Append($"INSERT INTO dbo.[{tableName}] (");

                //loop through each column
                for (int i = 0; i < columnNames.Count; i++)
                {
                    sql.Append(columnNames[i])
                        .Append(",");
                }

                //remove last comma
                sql.Remove(sql.Length - 1, 1);

                sql.Append(") VALUES (");

                //loop through each column
                for (int i = 0; i < columnNames.Count; i++)
                {
                    sql.Append(GetValue(columnType[i], data[i]))
                        .Append(",");
                }

                //remove last comma
                sql.Remove(sql.Length - 1, 1);

                sql.Append(");");

                //execute sql command
                try
                {
                    ExecuteSql(sql.ToString());
                }
                catch
                {
                    LogError("Error in line " + lineNumber + ": " + line);
                }
            }
        }

        private void LogError(string error)
        {
            //save error to log file
            File.AppendAllText("error.log", error + "\r\n");
        }

        private void ExecuteSql(string sqlQuery)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sqlQuery, connection);
                command.ExecuteNonQuery();
            }
        }

        private string GetValue(char dataType, string value)
        {
            switch (dataType)
            {
                case 'N':
                    return string.IsNullOrEmpty(value.Trim()) ? "0" : value.Trim();
                case 'C':
                    return $"'{value.Trim()}'";
                case 'D':
                    return string.IsNullOrEmpty(value.Trim()) ? "null" : $"'{value.Trim()}'";
                default:
                    throw new InvalidOperationException("Invalid data type");
            }
        }

        private string[] SplitByWidth(string s, int[] widths)
        {
            string[] ret = new string[widths.Length];
            char[] c = s.ToCharArray();
            int startPos = 0;
            for (int i = 0; i < widths.Length; i++)
            {
                int width = widths[i];
                ret[i] = new string(c.Skip(startPos).Take(width).ToArray<char>());
                startPos += width;
            }
            return ret;
        }
    }
}