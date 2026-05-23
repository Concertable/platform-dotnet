using Concertable.DataAccess.Application;
using Dapper;
using System.Data;

namespace Concertable.DataAccess.Infrastructure.Repositories;

public class DapperRepository : IDapperRepository
{
    private readonly IDbConnection connection;

    public DapperRepository(IDbConnection connection)
    {
        this.connection = connection;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        return await connection.QueryAsync<T>(sql, parameters);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<T> QuerySingleAsync<T>(string sql, object? parameters = null)
    {
        return await connection.QuerySingleAsync<T>(sql, parameters);
    }

    public async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, object? parameters = null, string splitOn = "Id")
    {
        return await connection.QueryAsync(sql, map, parameters, splitOn: splitOn);
    }

    public async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, object? parameters = null, string splitOn = "Id")
    {
        return await connection.QueryAsync(sql, map, parameters, splitOn: splitOn);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        return await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        return await connection.ExecuteScalarAsync<T>(sql, parameters);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters, IDbTransaction transaction)
    {
        return await connection.ExecuteAsync(sql, parameters, transaction);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters, IDbTransaction transaction)
    {
        return await connection.QueryAsync<T>(sql, parameters, transaction);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters, IDbTransaction transaction)
    {
        return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters, transaction);
    }
}
