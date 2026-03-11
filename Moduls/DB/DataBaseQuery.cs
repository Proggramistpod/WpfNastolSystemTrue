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
                string query = @"
            SELECT g.*, gc.category_id
            FROM games g
            LEFT JOIN game_categories gc 
                ON g.game_id = gc.game_id
            WHERE g.game_id = @id";

                return dbManager.Select(query,
                    new Dictionary<string, object>
                    {
                { "@id", id }
                    });
            }

            public void InsertGame(Dictionary<string, object> parameters)
            {
                string insertGameQuery = @"
            INSERT INTO games
            (title, publish_year, publisher, min_players, max_players,
             play_time_min, age_rating, bgg_rating, is_active, description)
            VALUES
            (@title, @publish_year, @publisher, @min_players, @max_players,
             @play_time_min, @age_rating, @bgg_rating, 1, @description);
            SELECT LAST_INSERT_ID();";

                object? gameId = dbManager.Scalar(insertGameQuery, parameters);

                if (gameId != null)
                {
                    string insertCategoryQuery = @"
                INSERT INTO game_categories (game_id, category_id)
                VALUES (@game_id, @category_id)";

                    dbManager.NonQuery(insertCategoryQuery, new Dictionary<string, object>
            {
                { "@game_id", gameId },
                { "@category_id", parameters["@category_id"] }
            });
                }
            }


            public void UpdateGame(Dictionary<string, object> parameters)
            {
                string updateGameQuery = @"
            UPDATE games SET
                title = @title,
                publish_year = @publish_year,
                publisher = @publisher,
                min_players = @min_players,
                max_players = @max_players,
                play_time_min = @play_time_min,
                age_rating = @age_rating,
                bgg_rating = @bgg_rating,
                is_active = 1,
                description = @description
            WHERE game_id = @game_id";

                dbManager.NonQuery(updateGameQuery, parameters);

                // Удаляем старую связь
                dbManager.NonQuery(
                    "DELETE FROM game_categories WHERE game_id = @game_id",
                    new Dictionary<string, object>
                    {
                { "@game_id", parameters["@game_id"] }
                    });

                // Добавляем новую
                dbManager.NonQuery(
                    "INSERT INTO game_categories (game_id, category_id) VALUES (@game_id, @category_id)",
                    new Dictionary<string, object>
                    {
                { "@game_id", parameters["@game_id"] },
                { "@category_id", parameters["@category_id"] }
                    });
            }

            public void DeleteGame(int id)
            {
                string query = "DELETE FROM games WHERE game_id = @id";
                dbManager.NonQuery(query, new Dictionary<string, object> { { "@id", id } });
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
                string query = "SELECT category_id, name, description FROM categories ORDER BY name";
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

            #region УНИВЕРСАЛЬНЫЙ ВЫВОД ТАБЛИЦGetAllCategories
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
            #region ПЕРСОНЫ (ПОЛНЫЙ ФУНКЦИОНАЛ)
            public DataTable GetPersonsForGrid()
            {
                string query = @"
            SELECT
                p.person_id,
                p.full_name,
                r.name AS role_name,
                p.phone,
                p.email,
                DATE_FORMAT(p.birth_date, '%d.%m.%Y') as birth_date,
                DATE_FORMAT(p.registered_at, '%d.%m.%Y %H:%i') as registered_at,
                CASE WHEN p.is_banned = 1 THEN 'Да' ELSE 'Нет' END as is_banned,
                p.notes
            FROM persons p
            LEFT JOIN roles r ON p.role_id = r.role_id
            ORDER BY p.full_name";
                return dbManager.Select(query);
            }


            public void InsertPerson(Dictionary<string, object> parameters)
            {
                string insertQuery = @"
            INSERT INTO persons
            (full_name, role_id, phone, email, birth_date, is_banned, notes, registered_at)
            VALUES
            (@full_name, @role_id, @phone, @email, @birth_date, @is_banned, @notes, NOW());
            SELECT LAST_INSERT_ID();";

                dbManager.Scalar(insertQuery, parameters);
            }

            public void UpdatePerson(Dictionary<string, object> parameters)
            {
                string updateQuery = @"
            UPDATE persons SET
                full_name = @full_name,
                role_id = @role_id,
                phone = @phone,
                email = @email,
                birth_date = @birth_date,
                is_banned = @is_banned,
                notes = @notes
            WHERE person_id = @person_id";

                dbManager.NonQuery(updateQuery, parameters);
            }

            public void DeletePerson(int id)
            {
                // Сначала проверяем, есть ли связанные записи
                string checkQuery = @"
            SELECT 
                (SELECT COUNT(*) FROM accounts WHERE person_id = @id) as accounts_count,
                (SELECT COUNT(*) FROM sessions WHERE person_id = @id) as sessions_count";

                var checkParams = new Dictionary<string, object> { { "@id", id } };
                DataTable checkResult = dbManager.Select(checkQuery, checkParams);

                if (checkResult.Rows.Count > 0)
                {
                    int accountsCount = Convert.ToInt32(checkResult.Rows[0]["accounts_count"]);
                    int sessionsCount = Convert.ToInt32(checkResult.Rows[0]["sessions_count"]);

                    if (accountsCount > 0 || sessionsCount > 0)
                    {
                        throw new Exception($"Невозможно удалить пользователя. Связанные записи: аккаунтов - {accountsCount}, сессий - {sessionsCount}");
                    }
                }

                string query = "DELETE FROM persons WHERE person_id = @id";
                dbManager.NonQuery(query, new Dictionary<string, object> { { "@id", id } });
            }

            public bool IsEmailUnique(string email, int? excludePersonId = null)
            {
                string query = @"
            SELECT COUNT(*) 
            FROM persons 
            WHERE email = @email" + (excludePersonId.HasValue ? " AND person_id != @person_id" : "");

                var parameters = new Dictionary<string, object> { { "@email", email } };
                if (excludePersonId.HasValue)
                    parameters.Add("@person_id", excludePersonId.Value);

                object result = dbManager.Scalar(query, parameters);
                return Convert.ToInt32(result) == 0;
            }

            public bool IsPhoneUnique(string phone, int? excludePersonId = null)
            {
                string query = @"
            SELECT COUNT(*) 
            FROM persons 
            WHERE phone = @phone" + (excludePersonId.HasValue ? " AND person_id != @person_id" : "");

                var parameters = new Dictionary<string, object> { { "@phone", phone } };
                if (excludePersonId.HasValue)
                    parameters.Add("@person_id", excludePersonId.Value);

                object result = dbManager.Scalar(query, parameters);
                return Convert.ToInt32(result) == 0;
            }

            public DataTable SearchPersons(string searchTerm)
            {
                string query = @"
            SELECT
                p.person_id,
                p.full_name,
                r.name AS role_name,
                p.phone,
                p.email,
                DATE_FORMAT(p.birth_date, '%d.%m.%Y') as birth_date,
                CASE WHEN p.is_banned = 1 THEN 'Да' ELSE 'Нет' END as is_banned
            FROM persons p
            LEFT JOIN roles r ON p.role_id = r.role_id
            WHERE 
                p.full_name LIKE @search 
                OR p.phone LIKE @search 
                OR p.email LIKE @search
            ORDER BY p.full_name
            LIMIT 50";

                return dbManager.Select(query,
                    new Dictionary<string, object>
                    {
                { "@search", $"%{searchTerm}%" }
                    });
            }

            public DataTable GetPersonsByRole(int roleId)
            {
                string query = @"
            SELECT
                p.person_id,
                p.full_name,
                p.phone,
                p.email,
                DATE_FORMAT(p.birth_date, '%d.%m.%Y') as birth_date,
                CASE WHEN p.is_banned = 1 THEN 'Да' ELSE 'Нет' END as is_banned
            FROM persons p
            WHERE p.role_id = @role_id
            ORDER BY p.full_name";

                return dbManager.Select(query,
                    new Dictionary<string, object>
                    {
                { "@role_id", roleId }
                    });
            }

            public DataTable GetActivePersons()
            {
                string query = @"
            SELECT
                p.person_id,
                p.full_name,
                r.name AS role_name,
                p.phone,
                p.email,
                DATE_FORMAT(p.birth_date, '%d.%m.%Y') as birth_date
            FROM persons p
            LEFT JOIN roles r ON p.role_id = r.role_id
            WHERE p.is_banned = 0
            ORDER BY p.full_name";

                return dbManager.Select(query);
            }

            public int GetPersonsCount()
            {
                string query = "SELECT COUNT(*) FROM persons";
                object result = dbManager.Scalar(query);
                return Convert.ToInt32(result);
            }

            public int GetActivePersonsCount()
            {
                string query = "SELECT COUNT(*) FROM persons WHERE is_banned = 0";
                object result = dbManager.Scalar(query);
                return Convert.ToInt32(result);
            }

            public DataTable GetPersonsWithAccounts()
            {
                string query = @"
            SELECT
                p.person_id,
                p.full_name,
                a.login,
                DATE_FORMAT(a.created_at, '%d.%m.%Y %H:%i') as account_created,
                DATE_FORMAT(a.last_login, '%d.%m.%Y %H:%i') as last_login
            FROM persons p
            INNER JOIN accounts a ON p.person_id = a.person_id
            ORDER BY p.full_name";

                return dbManager.Select(query);
            }

            public DataTable GetPersonsWithoutAccounts()
            {
                string query = @"
            SELECT
                p.person_id,
                p.full_name,
                p.phone,
                p.email
            FROM persons p
            LEFT JOIN accounts a ON p.person_id = a.person_id
            WHERE a.account_id IS NULL
            ORDER BY p.full_name";

                return dbManager.Select(query);
            }

            public void BanPerson(int personId)
            {
                string query = "UPDATE persons SET is_banned = 1 WHERE person_id = @person_id";
                dbManager.NonQuery(query,
                    new Dictionary<string, object>
                    {
                { "@person_id", personId }
                    });
            }

            public void UnbanPerson(int personId)
            {
                string query = "UPDATE persons SET is_banned = 0 WHERE person_id = @person_id";
                dbManager.NonQuery(query,
                    new Dictionary<string, object>
                    {
                { "@person_id", personId }
                    });
            }

            public DataTable GetPersonStatistics(int personId)
            {
                string query = @"
            SELECT
                (SELECT COUNT(*) FROM sessions WHERE person_id = @person_id) as total_sessions,
                (SELECT COUNT(*) FROM sessions WHERE person_id = @person_id AND ended_at IS NOT NULL) as completed_sessions,
                (SELECT COUNT(*) FROM sessions WHERE person_id = @person_id AND ended_at IS NULL) as active_sessions,
                (SELECT COALESCE(SUM(cost), 0) FROM sessions WHERE person_id = @person_id AND paid = 1) as total_paid,
                (SELECT COALESCE(SUM(cost), 0) FROM sessions WHERE person_id = @person_id AND paid = 0) as total_debt,
                (SELECT MIN(started_at) FROM sessions WHERE person_id = @person_id) as first_session,
                (SELECT MAX(started_at) FROM sessions WHERE person_id = @person_id) as last_session";

                return dbManager.Select(query,
                    new Dictionary<string, object>
                    {
                { "@person_id", personId }
                    });
            }
            public DataTable GetPersonById(int id)
            {
                string query = @"
            SELECT 
                p.*,
                a.login as account_login,
                a.created_at as account_created_at
            FROM persons p
            LEFT JOIN accounts a ON p.person_id = a.person_id
            WHERE p.person_id = @id";

                return dbManager.Select(query,
                    new Dictionary<string, object>
                    {
                { "@id", id }
                    });
            }
            #endregion
            #region СЕССИИ

            public DataTable GetSessionById(int id)
            {
                string query = @"
            SELECT 
                s.*,
                p.full_name as person_name,
                t.table_number,
                t.zone
            FROM sessions s
            LEFT JOIN persons p ON s.person_id = p.person_id
            LEFT JOIN tables t ON s.table_id = t.table_id
            WHERE s.session_id = @id";

                return dbManager.Select(query,
                    new Dictionary<string, object>
                    {
                { "@id", id }
                    });
            }

            public void InsertSession(Dictionary<string, object> parameters)
            {
                string query = @"
            INSERT INTO sessions
            (person_id, table_id, started_at, ended_at, cost, paid, payment_method, created_by, notes)
            VALUES
            (@person_id, @table_id, @started_at, @ended_at, @cost, @paid, @payment_method, @created_by, @notes);
            SELECT LAST_INSERT_ID();";

                dbManager.Scalar(query, parameters);
            }

            public void UpdateSession(Dictionary<string, object> parameters)
            {
                string query = @"
            UPDATE sessions SET
                person_id = @person_id,
                table_id = @table_id,
                started_at = @started_at,
                ended_at = @ended_at,
                cost = @cost,
                paid = @paid,
                payment_method = @payment_method,
                notes = @notes
            WHERE session_id = @session_id";

                dbManager.NonQuery(query, parameters);
            }

            public DataTable GetActiveSessions()
            {
                string query = @"
            SELECT
                s.session_id,
                p.full_name AS organizer_name,
                t.table_number,
                s.started_at,
                s.cost,
                s.notes
            FROM sessions s
            LEFT JOIN persons p ON s.person_id = p.person_id
            LEFT JOIN tables t ON s.table_id = t.table_id
            WHERE s.ended_at IS NULL
            ORDER BY s.started_at DESC";

                return dbManager.Select(query);
            }

            public DataTable GetSessionsByDateRange(DateTime startDate, DateTime endDate)
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
                s.payment_method
            FROM sessions s
            LEFT JOIN persons p ON s.person_id = p.person_id
            LEFT JOIN tables t ON s.table_id = t.table_id
            WHERE DATE(s.started_at) BETWEEN @start_date AND @end_date
            ORDER BY s.started_at DESC";

                return dbManager.Select(query,
                    new Dictionary<string, object>
                    {
                { "@start_date", startDate.ToString("yyyy-MM-dd") },
                { "@end_date", endDate.ToString("yyyy-MM-dd") }
                    });
            }
        #region КАТЕГОРИИ

        public DataTable GetCategoryById(int id)
        {
            string query = @"
        SELECT
            category_id,
            name,
            description
        FROM categories
        WHERE category_id = @id";

            return dbManager.Select(query,
                new Dictionary<string, object>
                {
            { "@id", id }
                });
        }

        public void InsertCategory(Dictionary<string, object> parameters)
        {
            string query = @"
        INSERT INTO categories
        (name, description)
        VALUES
        (@name, @description)";

            dbManager.NonQuery(query, parameters);
        }

        public void UpdateCategory(Dictionary<string, object> parameters)
        {
            string query = @"
        UPDATE categories SET
            name = @name,
            description = @description
        WHERE category_id = @category_id";

            dbManager.NonQuery(query, parameters);
        }

        // Для GameCopyEditWindow
        public DataTable GetGameCopyById(int id)
        {
            string query = @"SELECT * FROM game_copies WHERE copy_id = @id";
            return dbManager.Select(query, new Dictionary<string, object> { { "@id", id } });
        }

        public void InsertGameCopy(Dictionary<string, object> parameters)
        {
            string query = @"INSERT INTO game_copies 
        (game_id, inventory_number, acquired_date, location, is_available, Condition, notes)
        VALUES 
        (@game_id, @inventory_number, @acquired_date, @location, @is_available, @condition, @notes)";

            dbManager.NonQuery(query, parameters);
        }

        public void UpdateGameCopy(Dictionary<string, object> parameters)
        {
            string query = @"UPDATE game_copies SET
        game_id = @game_id,
        inventory_number = @inventory_number,
        acquired_date = @acquired_date,
        location = @location,
        is_available = @is_available,
        Condition = @condition,
        notes = @notes
        WHERE copy_id = @copy_id";

            dbManager.NonQuery(query, parameters);
        }

        // Для TableEditWindow
        public DataTable GetTableById(int id)
        {
            string query = @"SELECT * FROM tables WHERE table_id = @id";
            return dbManager.Select(query, new Dictionary<string, object> { { "@id", id } });
        }

        public void InsertTable(Dictionary<string, object> parameters)
        {
            string query = @"INSERT INTO tables 
        (table_number, capacity, zone, is_available, notes)
        VALUES 
        (@table_number, @capacity, @zone, @is_available, @notes)";

            dbManager.NonQuery(query, parameters);
        }

        public void UpdateTable(Dictionary<string, object> parameters)
        {
            string query = @"UPDATE tables SET
        table_number = @table_number,
        capacity = @capacity,
        zone = @zone,
        is_available = @is_available,
        notes = @notes
        WHERE table_id = @table_id";

            dbManager.NonQuery(query, parameters);
        }

        // Для AccountEditWindow
        public DataTable GetAccountById(int id)
        {
            string query = @"SELECT * FROM accounts WHERE account_id = @id";
            return dbManager.Select(query, new Dictionary<string, object> { { "@id", id } });
        }

        public void InsertAccount(Dictionary<string, object> parameters)
        {
            string query = @"INSERT INTO accounts 
        (person_id, login, password, created_at)
        VALUES 
        (@person_id, @login, @password, NOW())";

            dbManager.NonQuery(query, parameters);
        }

        public void UpdateAccount(Dictionary<string, object> parameters)
        {
            string query = @"UPDATE accounts SET
        person_id = @person_id,
        login = @login
        WHERE account_id = @account_id";

            dbManager.NonQuery(query, parameters);
        }

        // Для RoleEditWindow
        public DataTable GetRoleById(int id)
        {
            string query = @"SELECT * FROM roles WHERE role_id = @id";
            return dbManager.Select(query, new Dictionary<string, object> { { "@id", id } });
        }

        public void InsertRole(Dictionary<string, object> parameters)
        {
            string query = @"INSERT INTO roles 
        (code, name, description)
        VALUES 
        (@code, @name, @description)";

            dbManager.NonQuery(query, parameters);
        }

        public void UpdateRole(Dictionary<string, object> parameters)
        {
            string query = @"UPDATE roles SET
        code = @code,
        name = @name,
        description = @description
        WHERE role_id = @role_id";

            dbManager.NonQuery(query, parameters);
        }
        #endregion
        #endregion
    }
}