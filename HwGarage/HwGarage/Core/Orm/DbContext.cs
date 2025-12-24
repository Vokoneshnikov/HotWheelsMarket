using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HwGarage.Core.Orm.Models;

namespace HwGarage.Core.Orm
{
    public class DbContext : IDisposable
    {
        private readonly string _connectionString;
        private NpgsqlConnection _connection;

        public DbContext(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new NpgsqlConnection(_connectionString);
            _connection.Open();
        }

        public QueryBuilder<T> Table<T>(string tableName) where T : new()
            => new QueryBuilder<T>(_connection, tableName);

        public QueryBuilder<User> Users => Table<User>("users");
        public QueryBuilder<Car> Cars => Table<Car>("cars");
        public QueryBuilder<Auction> Auctions => Table<Auction>("auctions");
        public QueryBuilder<Bid> Bids => Table<Bid>("bids");
        public QueryBuilder<Transaction> Transactions => Table<Transaction>("transactions");
        public QueryBuilder<MarketListing> Listings => Table<MarketListing>("market_listings");
        public QueryBuilder<CarPhoto> CarPhotos => Table<CarPhoto>("car_photos");
        public QueryBuilder<Role> Roles => Table<Role>("roles");
        public QueryBuilder<UserRole> UserRoles => Table<UserRole>("user_roles");
        
        
        // public async Task AddUserRoleAsync(int userId, int roleId)
        // {
        //     const string sqlCmd = @"
        //     INSERT INTO user_roles (user_id, role_id)
        //     VALUES (@user_id, @role_id)
        //     ON CONFLICT DO NOTHING;";
        //
        //     await using var cmd = new NpgsqlCommand(sqlCmd, _connection);
        //     cmd.Parameters.AddWithValue("user_id", userId);
        //     cmd.Parameters.AddWithValue("role_id", roleId);
        //     await cmd.ExecuteNonQueryAsync();
        // }
        public async Task<NpgsqlTransaction> BeginTransactionAsync()
        {
            return await _connection.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}