    using System;
    using System.Collections.Generic;

    namespace HwGarage.Core.Orm.Models
    {
        public class User
        {
            [Column("id")]
            public int Id { get; set; }

            [Column("username")]
            public string Username { get; set; }

            [Column("password_hash")]
            public string PasswordHash { get; set; }

            [Column("email")]
            public string Email { get; set; }

            [Column("first_name")]
            public string? FirstName { get; set; }

            [Column("last_name")]
            public string? LastName { get; set; }

            [Column("balance")]
            public decimal Balance { get; set; }

            [Column("created_at")]
            public DateTime CreatedAt { get; set; }

            // НЕ мапится в БД, только для удобства
            public List<string> Roles { get; set; } = new();
        }
    }