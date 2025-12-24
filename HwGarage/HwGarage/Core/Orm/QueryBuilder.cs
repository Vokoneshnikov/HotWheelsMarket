using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HwGarage.Core.Orm
{
    public class QueryBuilder<T> where T : new()
    {
        private readonly NpgsqlConnection _connection;
        private readonly NpgsqlTransaction? _transaction;
        private readonly string _tableName;
        private readonly List<string> _whereClauses = new();
        private readonly List<NpgsqlParameter> _parameters = new();
        private int? _limit;

        public QueryBuilder(NpgsqlConnection connection, string tableName, NpgsqlTransaction? transaction = null)
        {
            _connection = connection;
            _tableName = tableName;
            _transaction = transaction;
        }


        public QueryBuilder<T> Where(string column, object value)
        {
            string paramName = $"@p{_parameters.Count}";
            _whereClauses.Add($"{column} = {paramName}");
            _parameters.Add(new NpgsqlParameter(paramName, value ?? DBNull.Value));
            return this;
        }

        public QueryBuilder<T> Limit(int count)
        {
            _limit = count;
            return this;
        }

        public async Task<List<T>> ToListAsync()
        {
            var result = new List<T>();
            var query = BuildSelectQuery();

            await using var cmd = new NpgsqlCommand(query, _connection, _transaction);            cmd.Parameters.AddRange(_parameters.ToArray());
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                result.Add(MapReaderToEntity(reader));

            return result;
        }

        public async Task<T?> FirstOrDefaultAsync()
        {
            _limit = 1;
            var list = await ToListAsync();
            return list.Count > 0 ? list[0] : default;
        }

        public async Task<List<T>> GetAllAsync()
        {
            var query = $"SELECT * FROM {_tableName}";
            var result = new List<T>();

            await using var cmd = new NpgsqlCommand(query, _connection, _transaction);            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(MapReaderToEntity(reader));

            return result;
        }

        public async Task<T?> FindAsync(int id)
        {
            var query = $"SELECT * FROM {_tableName} WHERE id = @id LIMIT 1;";
            await using var cmd = new NpgsqlCommand(query, _connection, _transaction);            cmd.Parameters.AddWithValue("id", id);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return MapReaderToEntity(reader);

            return default;
        }
        
        public async Task<int> InsertAsync(T entity)
        {
            
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columns = new List<string>();
            var values = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            foreach (var prop in props)
            {
                if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    continue;

                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr == null)
                    continue;

                var colName = columnAttr.Name;
                var paramName = "@" + colName;

                columns.Add(colName);
                values.Add(paramName);
                parameters.Add(new NpgsqlParameter(paramName, prop.GetValue(entity) ?? DBNull.Value));
            }

            bool hasIdColumn =
                typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Any(p =>
                    {
                        var col = p.GetCustomAttribute<ColumnAttribute>();
                        return col != null &&
                               col.Name.Equals("id", StringComparison.OrdinalIgnoreCase);
                    });

            string sql = $"INSERT INTO {_tableName} ({string.Join(",", columns)}) " +
                         $"VALUES ({string.Join(",", values)})";

            if (hasIdColumn)
                sql += " RETURNING id";

            await using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddRange(parameters.ToArray());

            if (hasIdColumn)
            {
                var idObj = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(idObj);
            }
            else
            {
                await cmd.ExecuteNonQueryAsync();
                return 0; // для таблиц без id
            }
        }




        public async Task UpdateAsync(int id, T entity)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var updates = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            foreach (var prop in props)
            {
                if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    continue;

                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr == null)
                    continue;

                var colName = columnAttr.Name;
                var paramName = "@" + colName;

                updates.Add($"{colName} = {paramName}");
                parameters.Add(new NpgsqlParameter(paramName, prop.GetValue(entity) ?? DBNull.Value));
            }

            var query = $"UPDATE {_tableName} SET {string.Join(",", updates)} WHERE id = @id";
            await using var cmd = new NpgsqlCommand(query, _connection, _transaction);            cmd.Parameters.AddRange(parameters.ToArray());
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }



        public async Task DeleteAsync(int id)
        {
            var query = $"DELETE FROM {_tableName} WHERE id = @id";
            await using var cmd = new NpgsqlCommand(query, _connection, _transaction);            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private string BuildSelectQuery()
        {
            var sb = new StringBuilder($"SELECT * FROM {_tableName}");
            if (_whereClauses.Count > 0)
                sb.Append(" WHERE " + string.Join(" AND ", _whereClauses));
            if (_limit.HasValue)
                sb.Append(" LIMIT " + _limit.Value);
            sb.Append(";");
            return sb.ToString();
        }

        private static T MapReaderToEntity(IDataReader reader)
        {
            var entity = new T();
            foreach (var prop in typeof(T).GetProperties())
            {
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var colName = columnAttr?.Name ?? prop.Name.ToLower();
                if (!reader.HasColumn(colName)) 
                    continue;

                var value = reader[colName];
                if (value == DBNull.Value)
                    continue;

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                if (targetType.IsInstanceOfType(value))
                {
                    prop.SetValue(entity, value);
                }
                else
                {
                    var converted = Convert.ChangeType(value, targetType);
                    prop.SetValue(entity, converted);
                }
            }
            return entity;
        }
        public QueryBuilder<T> UseTransaction(NpgsqlTransaction tx)
        {
            return new QueryBuilder<T>(_connection, _tableName, tx);
        }


    }

    public static class DataReaderExtensions
    {
        public static bool HasColumn(this IDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
