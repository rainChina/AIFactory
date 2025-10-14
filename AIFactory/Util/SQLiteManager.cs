using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.Util
{
    class SQLiteManager
    {

        SqliteConnection connection;
        SqliteCommand insertCommand;

        public SQLiteManager(string fname = "data")
        {
            string sqliteFile = string.Format("{0}.db", fname);
            // Initialize SQLite
            connection = new SqliteConnection($"Data Source={sqliteFile};");
            connection.Open();

            // Enable WAL mode
            var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = "PRAGMA journal_mode=WAL;";
            pragmaCmd.ExecuteNonQuery();

            // Create table
            var tableCmd = connection.CreateCommand();
            tableCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS RealTimeData (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                JsonData TEXT); ";
            tableCmd.ExecuteNonQuery();

            // Prepare insert command
            insertCommand = connection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO RealTimeData (JsonData) VALUES ($json)";
            insertCommand.Parameters.Add("$json", SqliteType.Text);

        }

        public int SaveJson(string json)
        {
            insertCommand.Parameters["$json"].Value = json;
            int res = insertCommand.ExecuteNonQuery();
            return res;
        }

        public void Close()
        {
            connection.Close();
        }

    }
}
