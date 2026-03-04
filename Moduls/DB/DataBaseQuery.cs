using System;
using System.Collections.Generic;
using System.Data;

namespace WpfNastolSystem.Moduls.DB
{
    class DataBaseQuery
    {
        DbManager dbManager = new DbManager();
        public string? AtorizationUser(string login, string password)
        {
            object? result = dbManager.Scalar(
                "SELECT login FROM accounts WHERE password = @password",
                new Dictionary<string, object> { { "@password", password } }
            );
            return result?.ToString();
        }
        public DataTable GetTableData(string tableName)
        {
            string query = $"SELECT * FROM {tableName}";
            return dbManager.Select(query);
        }
        public DataTable GetGameById(int id)
        {
            string query = "SELECT * FROM games WHERE game_id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return dbManager.Select(query, parameters);
        }
        public void InsertGame(Dictionary<string, object> parameters)
        {
            string query = @"INSERT INTO games 
                    (title, description, publish_year, publisher, category_id, 
                     min_players, max_players, play_time_min, age_rating, bgg_rating) 
                    VALUES 
                    (@title, @description, @publish_year, @publisher, @category_id,
                     @min_players, @max_players, @play_time_min, @age_rating, @bgg_rating)";
            dbManager.NonQuery(query, parameters);
        }
        public void UpdateGame(Dictionary<string, object> parameters)
        {
            string query = @"UPDATE games SET 
                    title = @title,
                    description = @description,
                    publish_year = @publish_year,
                    publisher = @publisher,
                    category_id = @category_id,
                    min_players = @min_players,
                    max_players = @max_players,
                    play_time_min = @play_time_min,
                    age_rating = @age_rating,
                    bgg_rating = @bgg_rating
                    WHERE game_id = @game_id";
            dbManager.NonQuery(query, parameters);
        }
        public void DeleteRecord(string table, string idColumn, int id)
        {
            string query = $"DELETE FROM {table} WHERE {idColumn} = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            dbManager.NonQuery(query, parameters);
        }
    }
}