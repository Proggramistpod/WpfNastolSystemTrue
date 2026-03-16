using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace WpfNastolSystem.Moduls.DB
{

    internal class DbManager
    {
        private readonly string _connectionString = 
                "Server=localhost;" +
                "Database=nastolclub;" +
                "Uid=root;" +
                "Pwd=wasd222t!;" +
                "CharSet=utf8mb4;";

        public DataTable Select(string query, Dictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            var table = new DataTable();

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand(query, conn);
            AddParameters(cmd, parameters);

            using var adapter = new MySqlDataAdapter(cmd);

            conn.Open();
            adapter.Fill(table);

            return table;
        }

        public object? Scalar(string query, Dictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand(query, conn);
            AddParameters(cmd, parameters);

            conn.Open();
            return cmd.ExecuteScalar();
        }

        public int NonQuery(string query, Dictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand(query, conn);
            AddParameters(cmd, parameters);

            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        public T InTransaction<T>(Func<MySqlConnection, MySqlTransaction, T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                var result = action(conn, transaction);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public void InTransaction(Action<MySqlConnection, MySqlTransaction> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            InTransaction<object?>((conn, tx) =>
            {
                action(conn, tx);
                return null;
            });
        }

        private static void AddParameters(MySqlCommand command, Dictionary<string, object>? parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return;

            foreach (var kvp in parameters)
            {
                var value = kvp.Value ?? DBNull.Value;
                command.Parameters.AddWithValue(kvp.Key, value);
            }
        }
    }
}

