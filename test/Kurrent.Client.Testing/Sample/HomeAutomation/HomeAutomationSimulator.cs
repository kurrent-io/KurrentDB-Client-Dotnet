using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Bogus;

namespace HomeAutomation;

public enum SensorType {
    Temperature,
    Humidity,
    Light,
    Motion,
    Pressure
}

public enum MeasurementUnit {
    Celsius,
    Percent,
    Lux,
    Boolean,
    Hectopascal
}

public enum ReadingQuality {
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

public enum DeviceType {
    TemperatureSensor,
    HumiditySensor,
    LightSensor,
    MotionDetector,
    PressureSensor
}

public enum HomeLocation {
    LivingRoom,
    Kitchen,
    Bedroom,
    Bathroom,
    Garage,
    Garden
}

public static class IdGenerator {
    public static string NewDeviceId(SensorType sensorType) {
        var prefix = sensorType switch {
            SensorType.Temperature => "TEMP",
            SensorType.Humidity    => "HUM",
            SensorType.Light       => "LIGHT",
            SensorType.Motion      => "MOTION",
            SensorType.Pressure    => "PRESS"
        };

        return $"{prefix}_{Guid.NewGuid():N}"[..16].ToUpperInvariant();
    }

    public static string NewLocationId(HomeLocation location) => $"LOC_{location:G}_{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    public static string NewLocationId() => $"LOC_{Guid.NewGuid():N}"[..16].ToUpperInvariant();
}

public static class LocationHelper {
    static readonly Dictionary<string, HomeLocation> LocationMappings = new();

    public static string GetLocationName(HomeLocation location) =>
        location switch {
            HomeLocation.LivingRoom => "Living Room",
            HomeLocation.Kitchen    => "Kitchen",
            HomeLocation.Bedroom    => "Bedroom",
            HomeLocation.Bathroom   => "Bathroom",
            HomeLocation.Garage     => "Garage",
            HomeLocation.Garden     => "Garden",
            _                       => location.ToString()
        };

    public static string RegisterLocation(HomeLocation location) {
        var locationId = IdGenerator.NewLocationId(location);
        LocationMappings[locationId] = location;
        return locationId;
    }

    public static HomeLocation? GetLocation(string locationId) => LocationMappings.TryGetValue(locationId, out var location) ? location : null;
}

public record SensorReading {
    public required string                      DeviceId     { get; init; }
    public required SensorType                  SensorType   { get; init; }
    public required string                      LocationId   { get; init; }
    public required DateTime                    Timestamp    { get; init; }
    public required double                      Value        { get; init; }
    public required MeasurementUnit             Unit         { get; init; }
    public          double?                     BatteryLevel { get; init; }
    public required ReadingQuality              Quality      { get; init; }
    public          Dictionary<string, object>? Metadata     { get; init; }
}

public record SensorDevice(
    string DeviceId,
    DeviceType DeviceType,
    string LocationId,
    DateTime LastSeen,
    double BatteryLevel,
    bool IsOnline,
    TimeSpan ReportingInterval
) {
    public SensorDevice UpdateBattery(double newLevel) => this with { BatteryLevel = newLevel, LastSeen = DateTime.UtcNow };
    public SensorDevice SetOffline()                   => this with { IsOnline = false, BatteryLevel = 0, LastSeen = DateTime.UtcNow };
    public SensorDevice SetOnline()                    => this with { IsOnline = true, BatteryLevel = 100, LastSeen = DateTime.UtcNow };
    public SensorDevice UpdateLastSeen()               => this with { LastSeen = DateTime.UtcNow };
}

public readonly record struct ProcessResult(bool IsSuccess, string Message, string? ErrorCode = null) {
    public static ProcessResult Success(string message = "Success")               => new(true, message);
    public static ProcessResult Failure(string message, string? errorCode = null) => new(false, message, errorCode);
    public static ProcessResult InvalidInput(string message)                      => new(false, message, "INVALID_INPUT");
    public static ProcessResult Timeout(string message)                           => new(false, message, "TIMEOUT");
    public static ProcessResult ConnectionFailure(string message)                 => new(false, message, "CONNECTION_FAILURE");
    public static ProcessResult UnexpectedError(string message)                   => new(false, message, "UNEXPECTED_ERROR");
}

public readonly record struct SensorTypeConfig(
    SensorType Type,
    MeasurementUnit Unit,
    double MinValue,
    double MaxValue,
    bool HasDailyPattern,
    double BaseValue,
    double PatternAmplitude,
    double NoiseLevel
);

public static class SensorTypes {
    public static readonly SensorTypeConfig Humidity = new(
        SensorType.Humidity,
        MeasurementUnit.Percent,
        20,
        90,
        true,
        50,
        15,
        2.0
    );

    public static readonly SensorTypeConfig Light = new(
        SensorType.Light,
        MeasurementUnit.Lux,
        0,
        1000,
        true,
        300,
        400,
        20
    );

    public static readonly SensorTypeConfig Motion = new(
        SensorType.Motion,
        MeasurementUnit.Boolean,
        0,
        1,
        false,
        0,
        0,
        0
    );

    public static readonly SensorTypeConfig Pressure = new(
        SensorType.Pressure,
        MeasurementUnit.Hectopascal,
        980,
        1050,
        false,
        1013,
        10,
        1.0
    );

    public static readonly SensorTypeConfig Temperature = new(
        SensorType.Temperature,
        MeasurementUnit.Celsius,
        -10,
        40,
        true,
        22,
        8,
        0.5
    );

    public static readonly SensorTypeConfig[] AllTypes = [
        Temperature, Humidity, Light, Motion, Pressure
    ];

    public static SensorTypeConfig GetConfig(SensorType sensorType) =>
        sensorType switch {
            SensorType.Temperature => Temperature,
            SensorType.Humidity    => Humidity,
            SensorType.Light       => Light,
            SensorType.Motion      => Motion,
            SensorType.Pressure    => Pressure,
            _                      => throw new ArgumentException($"Unknown sensor type: {sensorType}", nameof(sensorType))
        };
}

public class SimulationException(string message) : Exception(message);

public class HomeAutomationSimulator {
    readonly List<SensorDevice>                     _devices          = [];
    readonly ConcurrentDictionary<string, DateTime> _lastReadingTimes = new();
    readonly Dictionary<string, HomeLocation>       _locationMappings = new();

    Faker<SensorReading> _readingFaker = null!;

    public HomeAutomationSimulator() {
        InitializeFakers();
    }

    public TimeSpan DefaultReportInterval { get; init; } = TimeSpan.FromSeconds(5);

    public int                         DeviceCount       => _devices.Count;
    public int                         OnlineDeviceCount => _devices.Count(d => d.IsOnline);
    public IReadOnlyList<SensorDevice> Devices           => _devices.AsReadOnly();

    public void InitializeDevices(int deviceCount = 50) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(deviceCount);

        _devices.Clear();
        _lastReadingTimes.Clear();
        _locationMappings.Clear();

        var locations = Enum.GetValues<HomeLocation>();

        for (var i = 0; i < deviceCount; i++) {
            var sensorTypeConfig = SensorTypes.AllTypes[Random.Shared.Next(SensorTypes.AllTypes.Length)];
            var homeLocation     = locations[Random.Shared.Next(locations.Length)];
            var locationId       = RegisterLocation(homeLocation);

            var deviceType = HomeAutomationSimulator.GetDeviceType(sensorTypeConfig.Type);

            var device = new SensorDevice(
                IdGenerator.NewDeviceId(sensorTypeConfig.Type),
                deviceType,
                locationId,
                DateTime.UtcNow.AddMinutes(-Random.Shared.Next(0, 60)),
                Random.Shared.NextDouble() * 80 + 20,
                Random.Shared.NextSingle() > 0.05f,
                TimeSpan.FromSeconds(Random.Shared.Next(30, 600))
            );

            _devices.Add(device);
            _lastReadingTimes[device.DeviceId] = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(0, 60));
        }
    }

    public async IAsyncEnumerable<SensorReading> GenerateReadingsAsync(
        TimeSpan? interval = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var reportInterval = interval ?? DefaultReportInterval;

        if (_devices.Count == 0) throw new InvalidOperationException("No devices initialized. Call InitializeDevices() first.");

        while (!cancellationToken.IsCancellationRequested) {
            var currentTime       = DateTime.UtcNow;
            var readingsGenerated = false;

            for (var i = 0; i < _devices.Count; i++) {
                var device = _devices[i];
                if (!device.IsOnline) continue;

                if (ShouldGenerateReading(device, currentTime)) {
                    var reading = GenerateReading(device, currentTime);
                    _lastReadingTimes[device.DeviceId] = currentTime;

                    var updatedDevice = HomeAutomationSimulator.UpdateDeviceState(device);
                    _devices[i]       = updatedDevice;
                    readingsGenerated = true;

                    yield return reading;
                }
            }

            if (readingsGenerated)
                await Task.Delay(reportInterval, cancellationToken);
            else
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }
    }

    public List<SensorReading> GenerateBatch(int count, DateTime? startTime = null) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        if (_devices.Count == 0) throw new InvalidOperationException("No devices initialized. Call InitializeDevices() first.");

        var readings    = new List<SensorReading>(count);
        var currentTime = startTime ?? DateTime.UtcNow;

        for (var i = 0; i < count; i++) {
            var device  = _devices[Random.Shared.Next(_devices.Count)];
            var reading = GenerateReading(device, currentTime.AddSeconds(i * 10));
            readings.Add(reading);
        }

        return readings;
    }

    public IEnumerable<SensorReading> GenerateTimeSeries(
        string deviceId,
        TimeSpan duration,
        TimeSpan interval,
        DateTime? startTime = null
    ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration.Ticks);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(interval.Ticks);

        var device = _devices.Find(d => d.DeviceId == deviceId)
                  ?? throw new ArgumentException($"Device '{deviceId}' not found", nameof(deviceId));

        var current = startTime ?? DateTime.UtcNow.Subtract(duration);
        var end     = current.Add(duration);

        while (current < end) {
            yield return GenerateReading(device, current);

            current = current.Add(interval);
        }
    }

    public ProcessResult SimulateDeviceFailure(string deviceId) {
        try {
            var deviceIndex = FindDeviceIndex(deviceId);
            _devices[deviceIndex] = _devices[deviceIndex].SetOffline();
            return ProcessResult.Success($"Device '{deviceId}' marked as offline");
        }
        catch (ArgumentException ex) {
            return ProcessResult.InvalidInput(ex.Message);
        }
    }

    public ProcessResult RestoreDevice(string deviceId) {
        try {
            var deviceIndex = FindDeviceIndex(deviceId);
            _devices[deviceIndex] = _devices[deviceIndex].SetOnline();
            return ProcessResult.Success($"Device '{deviceId}' restored to online");
        }
        catch (ArgumentException ex) {
            return ProcessResult.InvalidInput(ex.Message);
        }
    }

    public void Reset() {
        _devices.Clear();
        _lastReadingTimes.Clear();
    }

    void InitializeFakers() {
        _readingFaker = new Faker<SensorReading>()
            .RuleFor(r => r.DeviceId, f => f.Random.AlphaNumeric(12))
            .RuleFor(r => r.SensorType, f => f.PickRandom<SensorType>())
            .RuleFor(r => r.LocationId, f => f.Random.AlphaNumeric(16))
            .RuleFor(r => r.Timestamp, f => f.Date.Recent())
            .RuleFor(r => r.Value, f => f.Random.Double(0, 100))
            .RuleFor(r => r.Unit, f => f.PickRandom<MeasurementUnit>())
            .RuleFor(r => r.BatteryLevel, f => f.Random.Double(0, 100))
            .RuleFor(r => r.Quality, f => f.Random.WeightedRandom([ReadingQuality.Good, ReadingQuality.Fair, ReadingQuality.Poor], [0.85f, 0.12f, 0.03f]))
            .RuleFor(r => r.Metadata, f => new Dictionary<string, object>());
    }

    string RegisterLocation(HomeLocation homeLocation) {
        var locationId = IdGenerator.NewLocationId(homeLocation);
        _locationMappings[locationId] = homeLocation;
        return locationId;
    }

	static DeviceType GetDeviceType(SensorType sensorType) =>
        sensorType switch {
            SensorType.Temperature => DeviceType.TemperatureSensor,
            SensorType.Humidity    => DeviceType.HumiditySensor,
            SensorType.Light       => DeviceType.LightSensor,
            SensorType.Motion      => DeviceType.MotionDetector,
            SensorType.Pressure    => DeviceType.PressureSensor,
            _                      => throw new ArgumentException($"Unknown sensor type: {sensorType}", nameof(sensorType))
        };

    SensorReading GenerateReading(SensorDevice device, DateTime timestamp) {
        var sensorType   = HomeAutomationSimulator.GetSensorType(device.DeviceType);
        var sensorConfig = SensorTypes.GetConfig(sensorType);
        var value        = GenerateRealisticValue(sensorConfig, timestamp);
        var metadata     = HomeAutomationSimulator.GenerateMetadata(device, sensorConfig);

        return _readingFaker.Generate() with {
            DeviceId = device.DeviceId,
            SensorType = sensorType,
            LocationId = device.LocationId,
            Timestamp = timestamp,
            Value = value,
            Unit = sensorConfig.Unit,
            BatteryLevel = device.BatteryLevel,
            Metadata = metadata
        };
    }

    double GenerateRealisticValue(SensorTypeConfig config, DateTime timestamp) {
        var value = config.BaseValue;

        if (config.HasDailyPattern) value += HomeAutomationSimulator.CalculateDailyPattern(config, timestamp);

        value += (Random.Shared.NextDouble() - 0.5) * config.NoiseLevel * 2;

        if (config.Type == SensorType.Motion) {
            var activityProbability = HomeAutomationSimulator.CalculateActivityProbability(timestamp.Hour);
            return Random.Shared.NextSingle() < activityProbability ? 1 : 0;
        }

        return Math.Clamp(value, config.MinValue, config.MaxValue);
    }

	static double CalculateDailyPattern(SensorTypeConfig config, DateTime timestamp) {
        var hourOfDay = timestamp.Hour + timestamp.Minute / 60.0;

        return config.Type switch {
            SensorType.Temperature                            => config.PatternAmplitude * Math.Sin((hourOfDay - 6) * Math.PI / 12),
            SensorType.Light when hourOfDay is >= 6 and <= 20 => config.PatternAmplitude * Math.Sin((hourOfDay - 6) * Math.PI / 14),
            SensorType.Light                                  => Random.Shared.NextDouble() * 50,
            SensorType.Humidity                               => -config.PatternAmplitude * 0.3 * Math.Sin((hourOfDay - 6) * Math.PI / 12),
            _                                                 => 0
        };
    }

	static double CalculateActivityProbability(int hour) => hour is >= 7 and <= 22 ? 0.1 : 0.02;

	static Dictionary<string, object> GenerateMetadata(SensorDevice device, SensorTypeConfig config) {
        var metadata = new Dictionary<string, object>();

        switch (config.Type) {
            case SensorType.Motion:
                metadata["detectionConfidence"] = Random.Shared.NextSingle();
                break;

            case SensorType.Temperature:
                metadata["calibrationOffset"] = Random.Shared.NextSingle() * 0.5 - 0.25;
                break;

            case SensorType.Light:
                metadata["sensorType"] = Random.Shared.NextSingle() > 0.5 ? "photodiode" : "photoresistor";
                break;
        }

        metadata["firmwareVersion"] = $"v{Random.Shared.Next(1, 5)}.{Random.Shared.Next(0, 10)}.{Random.Shared.Next(0, 10)}";
        metadata["signalStrength"]  = Random.Shared.Next(-80, -30);

        return metadata;
    }

    bool ShouldGenerateReading(SensorDevice device, DateTime currentTime) {
        if (!_lastReadingTimes.TryGetValue(device.DeviceId, out var lastReading)) return true;

        return currentTime - lastReading >= device.ReportingInterval;
    }

	static SensorDevice UpdateDeviceState(SensorDevice device) {
        var newBatteryLevel = Math.Max(0, device.BatteryLevel - Random.Shared.NextDouble() * 0.01);

        if (newBatteryLevel < 5 && Random.Shared.NextSingle() < 0.1) return device.SetOffline();

        return device.UpdateBattery(newBatteryLevel).UpdateLastSeen();
    }

    int FindDeviceIndex(string deviceId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var index = _devices.FindIndex(d => d.DeviceId == deviceId);
        return index >= 0 ? index : throw new ArgumentException($"Device '{deviceId}' not found", nameof(deviceId));
    }

	static SensorType GetSensorType(DeviceType deviceType) =>
        deviceType switch {
            DeviceType.TemperatureSensor => SensorType.Temperature,
            DeviceType.HumiditySensor    => SensorType.Humidity,
            DeviceType.LightSensor       => SensorType.Light,
            DeviceType.MotionDetector    => SensorType.Motion,
            DeviceType.PressureSensor    => SensorType.Pressure,
            _                            => throw new ArgumentException($"Unknown device type: {deviceType}", nameof(deviceType))
        };

    public record SimulationConfiguration {
        public int                             DefaultDeviceCount    { get; init; } = 50;
        public TimeSpan                        DefaultReportInterval { get; init; } = TimeSpan.FromSeconds(5);
        public double                          DeviceFailureRate     { get; init; } = 0.05;
        public IReadOnlyList<SensorTypeConfig> EnabledSensorTypes    { get; init; } = SensorTypes.AllTypes;

        public IReadOnlyList<string> AvailableLocations { get; init; } = [
            "Living Room", "Kitchen", "Bedroom", "Bathroom", "Garage", "Garden"
        ];
    }
}

public static class SensorReadingExtensions {
    public static string ToJson(this SensorReading reading, bool indented = true) {
        var options = new JsonSerializerOptions {
            WriteIndented        = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(reading, options);
    }

    public static string ToMqttPayload(this SensorReading reading) {
        var payload = new {
            deviceId  = reading.DeviceId,
            timestamp = reading.Timestamp.ToString("O"),
            measurements = new[] {
                new {
                    type    = reading.SensorType.ToString().ToLowerInvariant(),
                    value   = reading.Value,
                    unit    = GetUnitSymbol(reading.Unit),
                    quality = reading.Quality.ToString().ToLowerInvariant()
                }
            },
            battery  = reading.BatteryLevel,
            location = reading.LocationId,
            metadata = reading.Metadata
        };

        return JsonSerializer.Serialize(
            payload, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        );
    }

    public static string ToTelemetryMessage(this SensorReading reading) {
        var telemetry = new {
            MessageId = Guid.NewGuid(),
            reading.DeviceId,
            EventType = "SensorReading",
            reading.Timestamp,
            Data = new {
                SensorType = reading.SensorType.ToString(),
                reading.Value,
                Unit     = GetUnitSymbol(reading.Unit),
                Location = reading.LocationId,
                Quality  = reading.Quality.ToString(),
                reading.BatteryLevel
            },
            reading.Metadata
        };

        return JsonSerializer.Serialize(
            telemetry, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented        = false
            }
        );
    }

    static string GetUnitSymbol(MeasurementUnit unit) =>
        unit switch {
            MeasurementUnit.Celsius     => "°C",
            MeasurementUnit.Percent     => "%",
            MeasurementUnit.Lux         => "lux",
            MeasurementUnit.Boolean     => "boolean",
            MeasurementUnit.Hectopascal => "hPa",
            _                           => unit.ToString()
        };
}
