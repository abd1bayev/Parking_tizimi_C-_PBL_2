using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Application.Models;

namespace Infrastructure.Persistence;

public sealed class JsonParkingRepository : IParkingRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonParkingRepository(string rootPath)
    {
        _filePath = Path.Combine(rootPath, "data", "parking-data.json");
    }

    public async Task<ParkingState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new ParkingState();
        }

        await using var stream = File.OpenRead(_filePath);
        if (stream.Length == 0)
        {
            return new ParkingState();
        }

        try
        {
            var state = await JsonSerializer
                .DeserializeAsync<ParkingState>(stream, _serializerOptions, cancellationToken)
                .ConfigureAwait(false);
            return state ?? new ParkingState();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("JSON data fayli buzilgan yoki noto'g'ri formatda.", exception);
        }
    }

    public async Task SaveAsync(ParkingState state, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath)
            ?? throw new InvalidOperationException("Data direktoriyasi aniqlanmadi.");
        Directory.CreateDirectory(directory);

        var tempFilePath = $"{_filePath}.tmp";
        await using (var stream = File.Create(tempFilePath))
        {
            await JsonSerializer
                .SerializeAsync(stream, state, _serializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Move(tempFilePath, _filePath, true);
    }
}
