using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WpfNastolSystem.Moduls.DB
{
    public class DatabaseConfig
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NastolClub", "dbconfig.json");

        public string Server { get; set; } = "localhost";
        public string Database { get; set; } = "nastolclub";
        public string UserId { get; set; } = "root";
        public string Password { get; set; } = "";
        public string CharSet { get; set; } = "utf8mb4";

        public string GetConnectionString()
        {
            return $"Server={Server};Database={Database};Uid={UserId};Pwd={Password};CharSet={CharSet};";
        }

        public void Save()
        {
            var encrypted = new DatabaseConfig
            {
                Server = this.Server,
                Database = this.Database,
                UserId = this.UserId,
                Password = Encrypt(this.Password),
                CharSet = this.CharSet
            };
            string json = JsonSerializer.Serialize(encrypted);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            File.WriteAllText(ConfigPath, json);
        }

        public static DatabaseConfig Load()
        {
            if (!File.Exists(ConfigPath))
                return null;

            string json = File.ReadAllText(ConfigPath);
            var encrypted = JsonSerializer.Deserialize<DatabaseConfig>(json);
            if (encrypted != null)
            {
                encrypted.Password = Decrypt(encrypted.Password);
            }
            return encrypted;
        }
        private static string Encrypt(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        private static string Decrypt(string encryptedBase64)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}