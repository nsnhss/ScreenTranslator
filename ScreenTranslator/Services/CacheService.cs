using Microsoft.Data.Sqlite;
using System.IO;

namespace ScreenTranslator.Services;

/// <summary>
/// SQLite 翻译缓存：key = (原文, 引擎, 源语言, 目标语言)，命中即 0ms 返回且不消耗 API 额度。
/// </summary>
public class CacheService
{
    private readonly string _connStr;
    private readonly string _dbPath;

    public CacheService()
    {
        _dbPath = Path.Combine(ConfigService.DataDirectory, "cache.db");
        Directory.CreateDirectory(ConfigService.DataDirectory);
        _connStr = $"Data Source={_dbPath}";
        Init();
    }

    private void Init()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS cache (
                source    TEXT NOT NULL,
                engine    TEXT NOT NULL DEFAULT '',
                from_lang TEXT NOT NULL DEFAULT '',
                to_lang   TEXT NOT NULL DEFAULT '',
                target    TEXT NOT NULL,
                hits      INTEGER NOT NULL DEFAULT 0,
                created   TEXT NOT NULL,
                last_used TEXT NOT NULL,
                PRIMARY KEY (source, engine, from_lang, to_lang)
            );
            """;
        cmd.ExecuteNonQuery();

        // 旧版本表结构没有 engine 列，缓存数据可丢弃，直接重建表
        using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM pragma_table_info('cache') WHERE name='engine';";
            if (Convert.ToInt32(check.ExecuteScalar()) == 0)
            {
                using var drop = conn.CreateCommand();
                drop.CommandText = "DROP TABLE cache;";
                drop.ExecuteNonQuery();
                Init();
            }
        }
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connStr);
        conn.Open();
        return conn;
    }

    public string? TryGet(string source, string engine, string from, string to)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE cache SET hits = hits + 1, last_used = $now
            WHERE source = $s AND engine = $e AND from_lang = $f AND to_lang = $t
            RETURNING target;
            """;
        cmd.Parameters.AddWithValue("$now", DateTime.Now.ToString("s"));
        cmd.Parameters.AddWithValue("$s", source);
        cmd.Parameters.AddWithValue("$e", engine);
        cmd.Parameters.AddWithValue("$f", from);
        cmd.Parameters.AddWithValue("$t", to);
        return cmd.ExecuteScalar() as string;
    }

    public void Put(string source, string target, string engine, string from, string to)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO cache (source, engine, from_lang, to_lang, target, hits, created, last_used)
            VALUES ($s, $e, $f, $t, $r, 0, $now, $now)
            ON CONFLICT (source, engine, from_lang, to_lang)
            DO UPDATE SET target = $r, last_used = $now;
            """;
        cmd.Parameters.AddWithValue("$s", source);
        cmd.Parameters.AddWithValue("$e", engine);
        cmd.Parameters.AddWithValue("$f", from);
        cmd.Parameters.AddWithValue("$t", to);
        cmd.Parameters.AddWithValue("$r", target);
        cmd.Parameters.AddWithValue("$now", DateTime.Now.ToString("s"));
        cmd.ExecuteNonQuery();
    }

    public (int Entries, long SizeBytes) Stats()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM cache;";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        long size = File.Exists(_dbPath) ? new FileInfo(_dbPath).Length : 0;
        return (count, size);
    }

    /// <summary>清理超过指定天数未使用的缓存</summary>
    public int Cleanup(int olderThanDays = 180)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM cache WHERE last_used < $cutoff;";
        cmd.Parameters.AddWithValue("$cutoff", DateTime.Now.AddDays(-olderThanDays).ToString("s"));
        return cmd.ExecuteNonQuery();
    }
}
