    namespace WpfNastolSystem.Moduls.CurrentUser
    {
        internal static class DataCurrentUser
        {
            public static string RoleCode { get; private set; }   // "admin", "cashier", "gamemaster", "sklad", "visitor"

            public static int? PersonId { get; private set; }

            public static void SetUser(string roleCode, int? personId = null)
            {
                RoleCode = roleCode;
                PersonId = personId;
            }

            public static void Clear()
            {
                RoleCode = null;
                PersonId = null;
            }
            public static bool IsAdmin => RoleCode == "admin";
            public static bool IsCashier => RoleCode == "cashier";
            public static bool IsGameMaster => RoleCode == "gamemaster";
            public static bool IsSklad => RoleCode == "sklad";
            public static bool IsVisitor => RoleCode == "visitor" || string.IsNullOrEmpty(RoleCode);
        }
    }
