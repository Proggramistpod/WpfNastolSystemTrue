using System;
using System.Collections.Generic;
using System.Data;

namespace WpfNastolSystem.Moduls.DB
{
    class DataBaseQuery
    {
        private readonly DbManager dbManager = new DbManager();

        #region АВТОРИЗАЦИЯ
        public string? AuthorizationUser(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                return null;

            object? result = dbManager.Scalar(
                @"SELECT login
                  FROM accounts
                  WHERE login = @login AND password = @password",
                new Dictionary<string, object>
                {
                    { "@login", login },
                    { "@password", password }
                });
            return result as string;
        }
        #endregion

        #region ИГРЫ
        public DataTable GetGamesForGrid()
        {
            string query = @"
                SELECT
                    game_id,
                    title,
                    publish_year,
                    publisher,
                    min_players,
                    max_players,
                    play_time_min,
                    age_rating,
                    bgg_rating,
                    is_active,
                    description
                FROM games
                ORDER BY title";
            return dbManager.Select(query);
        }

        public DataTable GetGameById(int id)
        {
            string query = "SELECT * FROM games WHERE game_id = @id";
            return dbManager.Select(query, new Dictionary<string, object> { { "@id", id } });
        }

        public void InsertGame(Dictionary<string, object> parameters)
        {
            string query = @"
        INSERT INTO games
        (title, publish_year, publisher, min_players, max_players,
         play_time_min, age_rating, bgg_rating, is_active, description, category_id)
        VALUES
        (@title, @publish_year, @publisher, @min_players, @max_players,
         @play_time_min, @age_rating, @bgg_rating, @is_active, @description, @category_id)";
            dbManager.NonQuery(query, parameters);
        }


        public void UpdateGame(Dictionary<string, object> parameters)
        {
            string query = @"
        UPDATE games SET
            title = @title,
            publish_year = @publish_year,
            publisher = @publisher,
            min_players = @min_players,
            max_players = @max_players,
            play_time_min = @play_time_min,
            age_rating = @age_rating,
            bgg_rating = @bgg_rating,
            is_active = @is_active,
            description = @description,
            category_id = @category_id
        WHERE game_id = @game_id";
            dbManager.NonQuery(query, parameters);
        }

        public void DeleteGame(int id)
        {
            string query = "DELETE FROM games WHERE game_id = @id";
            dbManager.NonQuery(query, new Dictionary<string, object> { { "@id", id } });
        }
        #endregion

        #region ПЕРСОНЫ
        public DataTable GetPersonsForGrid()
        {
            string query = @"
                SELECT
                    p.person_id,
                    p.full_name,
                    r.name AS role_name,
                    p.phone,
                    p.email,
                    p.birth_date,
                    p.registered_at,
                    p.is_banned,
                    p.notes
                FROM persons p
                LEFT JOIN roles r ON p.role_id = r.role_id
                ORDER BY p.full_name";
            return dbManager.Select(query);
        }
        #endregion

        #region СЕССИИ (предполагаемая таблица)
        public DataTable GetSessionsForGrid()
        {
            string query = @"
        SELECT
            s.session_id,
            p.full_name AS organizer_name,
            t.table_number,
            s.started_at,
            s.ended_at,
            s.cost,
            s.paid,
            s.payment_method,
            s.notes
        FROM sessions s
        LEFT JOIN persons p ON s.person_id = p.person_id
        LEFT JOIN tables t ON s.table_id = t.table_id
        ORDER BY s.started_at DESC";
            return dbManager.Select(query);
        }
        #endregion

        #region КАТЕГОРИИ
        public DataTable GetAllCategories()
        {
            string query = "SELECT category_id, name FROM categories ORDER BY name";
            return dbManager.Select(query);
        }
        #endregion

        #region КОПИИ ИГР
        public DataTable GetGameCopiesForGrid()
        {
            string query = @"
                SELECT
                    gc.copy_id,
                    g.title AS game_title,
                    gc.inventory_number,
                    gc.acquired_date,
                    gc.location,
                    gc.is_available,
                    gc.notes
                FROM game_copies gc
                LEFT JOIN games g ON gc.game_id = g.game_id
                ORDER BY gc.inventory_number";
            return dbManager.Select(query);
        }
        #endregion

        #region СТОЛЫ
        public DataTable GetTablesForGrid()
        {
            string query = @"
                SELECT
                    table_id,
                    table_number,
                    capacity,
                    zone,
                    is_available,
                    notes
                FROM tables
                ORDER BY table_number";
            return dbManager.Select(query);
        }
        #endregion

        #region АККАУНТЫ
        public DataTable GetAccountsForGrid()
        {
            string query = @"
                SELECT
                    a.account_id,
                    a.login,
                    p.full_name AS person_name,
                    a.created_at
                FROM accounts a
                LEFT JOIN persons p ON a.person_id = p.person_id";
            return dbManager.Select(query);
        }
        #endregion

        #region РОЛИ
        public DataTable GetRolesForGrid()
        {
            string query = "SELECT role_id, code, name, description FROM roles ORDER BY name";
            return dbManager.Select(query);
        }
        #endregion

        #region УНИВЕРСАЛЬНЫЙ ВЫВОД ТАБЛИЦ
        public DataTable GetTableForGrid(string tableName)
        {
            return tableName switch
            {
                "games" => GetGamesForGrid(),
                "persons" => GetPersonsForGrid(),
                "sessions" => GetSessionsForGrid(),
                "categories" => GetAllCategories(),
                "game_copies" => GetGameCopiesForGrid(),
                "tables" => GetTablesForGrid(),
                "accounts" => GetAccountsForGrid(),
                "roles" => GetRolesForGrid(),
                _ => throw new Exception($"Неизвестная таблица: {tableName}")
            };
        }
        #endregion

        #region УДАЛЕНИЕ
        public void DeleteRecord(string tableName, int id)
        {
            string query = tableName switch
            {
                "games" => "DELETE FROM games WHERE game_id = @id",
                "persons" => "DELETE FROM persons WHERE person_id = @id",
                "sessions" => "DELETE FROM sessions WHERE session_id = @id",
                "categories" => "DELETE FROM categories WHERE category_id = @id",
                "game_copies" => "DELETE FROM game_copies WHERE copy_id = @id",
                "tables" => "DELETE FROM tables WHERE table_id = @id",
                "accounts" => "DELETE FROM accounts WHERE account_id = @id",
                "roles" => "DELETE FROM roles WHERE role_id = @id",
                _ => throw new Exception($"Неизвестная таблица для удаления: {tableName}")
            };
            dbManager.NonQuery(query, new Dictionary<string, object> { { "@id", id } });
        }
        #endregion
    }
}