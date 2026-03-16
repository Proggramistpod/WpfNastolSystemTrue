using System;
using System.Collections.Generic;
using System.Text;

namespace WpfNastolSystem.Moduls.CurrentUser
{
    internal static class DataCurrentUser
    {
        public static string RoleCode { get; private set; }   // "admin", "cashier", "gamemaster", "sklad", "visitor"

        public static void SetUser(string roleCode)
        {
            RoleCode = roleCode?.ToLowerInvariant() ?? "visitor";
        }

        public static void Clear()
        {
            RoleCode = null;
        }

        // Удобные свойства-проверки
        public static bool IsAdmin => RoleCode == "admin";
        public static bool IsCashier => RoleCode == "cashier";
        public static bool IsGameMaster => RoleCode == "gamemaster";
        public static bool IsSklad => RoleCode == "sklad";
        public static bool IsVisitor => RoleCode == "visitor" || string.IsNullOrEmpty(RoleCode);
    }
}
