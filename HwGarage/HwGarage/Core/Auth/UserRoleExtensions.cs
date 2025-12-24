using System;
using System.Linq;
using HwGarage.Core.Orm.Models;

namespace HwGarage.Core.Auth
{
    public static class UserRoleExtensions
    {
        public static bool HasRole(this User user, string roleName)
        {
            if (user.Roles == null || user.Roles.Count == 0)
                return false;

            return user.Roles.Any(r =>
                string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
        }
    }
}