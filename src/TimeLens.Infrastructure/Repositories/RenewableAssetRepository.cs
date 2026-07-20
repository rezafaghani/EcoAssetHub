using System.Data;
using TimeLens.Domain.Models;
using Npgsql;

namespace TimeLens.Infrastructure.Repositories;

public class RenewableAssetRepository(TimeLensContext context) : IRenewableAssetRepository
{
    public async Task<List<RenewableAssetDto>> GetAllAsync()
    {
        await using var command = context.Postgres.CreateCommand("""
            SELECT id, type, capacity, meter_point_id, hub_height, rotor_diameter, compass_orientation
            FROM renewable_assets
            ORDER BY meter_point_id
            """);
        await using var reader = await command.ExecuteReaderAsync();

        var result = new List<RenewableAssetDto>();
        while (await reader.ReadAsync())
        {
            result.Add(ToDto(reader));
        }

        return result;
    }

    public async Task<RenewableAsset?> GetByMeterPointIdAsync(long id)
    {
        return await GetAssetAsync("meter_point_id = @meter_point_id", ("meter_point_id", id));
    }

    public async Task<string> CreateAsync(RenewableAsset newObj, CancellationToken cancellationToken = default)
    {
        try
        {
            await InsertAsync(newObj, cancellationToken);
            return newObj.Id;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateNameException($"MeterPointId: {newObj.MeterPointId} is duplicated.", ex);
        }
    }

    public async Task RemoveAsync(string id)
    {
        await using var command = context.Postgres.CreateCommand("DELETE FROM renewable_assets WHERE id = @id");
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<RenewableAsset?> GetAsync(string id)
    {
        return await GetAssetAsync("id = @id", ("id", id));
    }

    public async Task<List<CurveDto>> SearchCurvesAsync(string? search, CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT id, type, name, capacity, meter_point_id
            FROM renewable_assets
            """;

        await using var command = context.Postgres.CreateCommand();
        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += " WHERE name ILIKE @search";
            command.Parameters.AddWithValue("search", $"%{search.Trim()}%");
            if (long.TryParse(search.Trim(), out var meterPointId))
            {
                sql += " OR meter_point_id = @meter_point_id";
                command.Parameters.AddWithValue("meter_point_id", meterPointId);
            }
        }

        command.CommandText = $"{sql} ORDER BY meter_point_id LIMIT 25";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<CurveDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CurveDto
            {
                Id = reader.GetString(0),
                Type = (RenewableAssetType)reader.GetInt32(1),
                Name = reader.GetString(2),
                Capacity = reader.GetDecimal(3),
                MeterPointId = reader.GetInt64(4)
            });
        }

        return result;
    }

    internal async Task InsertAsync(RenewableAsset asset, CancellationToken cancellationToken = default)
    {
        await using var command = context.Postgres.CreateCommand("""
            INSERT INTO renewable_assets (
                id, type, name, capacity, meter_point_id, hub_height, rotor_diameter, compass_orientation)
            VALUES (
                @id, @type, @name, @capacity, @meter_point_id, @hub_height, @rotor_diameter, @compass_orientation)
            """);
        AddAssetParameters(command, asset);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task UpdateAsync(RenewableAsset asset, CancellationToken cancellationToken = default)
    {
        await using var command = context.Postgres.CreateCommand("""
            UPDATE renewable_assets
            SET name = @name,
                capacity = @capacity,
                meter_point_id = @meter_point_id,
                hub_height = @hub_height,
                rotor_diameter = @rotor_diameter,
                compass_orientation = @compass_orientation
            WHERE id = @id
            """);
        AddAssetParameters(command, asset);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task<List<T>> GetByTypeAsync<T>(RenewableAssetType type, Func<NpgsqlDataReader, T> map)
    {
        await using var command = context.Postgres.CreateCommand("""
            SELECT id, type, name, capacity, meter_point_id, hub_height, rotor_diameter, compass_orientation
            FROM renewable_assets
            WHERE type = @type
            ORDER BY meter_point_id
            """);
        command.Parameters.AddWithValue("type", (int)type);
        await using var reader = await command.ExecuteReaderAsync();

        var result = new List<T>();
        while (await reader.ReadAsync())
        {
            result.Add(map(reader));
        }

        return result;
    }

    private async Task<RenewableAsset?> GetAssetAsync(string where, params (string Name, object Value)[] parameters)
    {
        await using var command = context.Postgres.CreateCommand($"""
            SELECT id, type, name, capacity, meter_point_id, hub_height, rotor_diameter, compass_orientation
            FROM renewable_assets
            WHERE {where}
            LIMIT 1
            """);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ToAsset(reader) : null;
    }

    private static void AddAssetParameters(NpgsqlCommand command, RenewableAsset asset)
    {
        command.Parameters.AddWithValue("id", asset.Id);
        command.Parameters.AddWithValue("type", (int)asset.Type);
        command.Parameters.AddWithValue("name", asset.Name);
        command.Parameters.AddWithValue("capacity", asset.Capacity);
        command.Parameters.AddWithValue("meter_point_id", asset.MeterPointId);
        command.Parameters.AddWithValue("hub_height", DbValue.From((asset as WindTurbine)?.HubHeight));
        command.Parameters.AddWithValue("rotor_diameter", DbValue.From((asset as WindTurbine)?.RotorDiameter));
        command.Parameters.AddWithValue("compass_orientation", DbValue.From((asset as SolarPanel)?.CompassOrientation));
    }

    private static RenewableAssetDto ToDto(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetString(0),
        Type = (RenewableAssetType)reader.GetInt32(1),
        Capacity = reader.GetDecimal(2),
        MeterPointId = reader.GetInt64(3),
        HubHeight = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
        RotorDiameter = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
        CompassOrientation = reader.IsDBNull(6) ? null : reader.GetString(6)
    };

    private static RenewableAsset ToAsset(NpgsqlDataReader reader)
    {
        var type = (RenewableAssetType)reader.GetInt32(1);
        var asset = type switch
        {
            RenewableAssetType.WindTurbine => new WindTurbine(reader.GetDecimal(3), reader.GetInt64(4), reader.GetDecimal(5), reader.GetDecimal(6)),
            RenewableAssetType.SolarPanel => new SolarPanel(reader.GetDecimal(3), reader.GetInt64(4), reader.GetString(7)),
            _ => new RenewableAsset(type, reader.GetDecimal(3), reader.GetInt64(4))
        };

        asset.Id = reader.GetString(0);
        asset.Name = reader.GetString(2);
        return asset;
    }
}
