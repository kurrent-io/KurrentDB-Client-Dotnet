namespace HomeAutomation.Contracts;

// Core wire format - flat composition-based design
public readonly record struct TelemetryMessage(
    string DeviceId,
    byte DeviceType,
    byte SmartDeviceType,
    string ZoneId,
    byte ZoneType,
    long UnixTimestamp,
    byte MessageType,
    ReadOnlyMemory<byte> Payload
);

// Message types for type discrimination
public static class TelemetryMessageType {
    public const byte StateChange   = 1;
    public const byte SensorReading = 2;
    public const byte Event         = 3;
    public const byte Usage         = 4;
    public const byte Heartbeat     = 5;
}

// Device registry contract - separate from telemetry stream
public readonly record struct DeviceRegistryContract(
    string DeviceId,
    byte DeviceType,
    byte SmartDeviceType,
    string Name,
    string Brand,
    string Model,
    string ZoneId,
    byte ZoneType,
    byte Status,
    bool IsControllable,
    string[] SupportedCommands,
    string[] MeasurementTypes,
    long RegisteredAt
);

// Zone mapping contract - for device-zone associations
public readonly record struct ZoneMappingContract(
    string ZoneId,
    string ZoneName,
    byte ZoneType,
    string? Floor,
    string? ParentZoneId,
    string[] DeviceIds
);

// Optimized telemetry payload structures - binary serializable
public readonly record struct StateChangePayload(
    string Property,
    float PreviousValueFloat,
    float NewValueFloat,
    string? PreviousValueString,
    string? NewValueString,
    byte ValueType // 1=float, 2=string, 3=bool
);

public readonly record struct SensorReadingPayload(
    float Value,
    byte Unit,   // Enum: 1=Celsius, 2=Fahrenheit, 3=Percent, 4=Watts, etc.
    byte Quality // 1=Excellent, 2=Good, 3=Fair, 4=Poor, 5=Critical
);

public readonly record struct EventPayload(
    byte EventType,         // Enum for known event types
    string? EventData,      // JSON string for complex data if needed
    float[]? NumericValues, // Array for numeric context
    byte Priority           // 1=Critical, 2=High, 3=Medium, 4=Low
);

public readonly record struct UsagePayload(
    float EnergyConsumed, // kWh
    int DurationSeconds,
    float PeakPower,   // Watts
    float AveragePower // Watts
);

public readonly record struct HeartbeatPayload(
    byte SignalStrength, // 0-100
    byte BatteryLevel,   // 0-100, 255 if N/A
    int UptimeSeconds,
    byte ErrorCount // Rolling error count
);

// High-throughput batch contract
public readonly record struct TelemetryBatch(
    string HomeId,
    long BatchTimestamp,
    TelemetryMessage[] Messages,
    byte CompressionType, // 0=None, 1=Gzip, 2=LZ4
    ReadOnlyMemory<byte>? CompressedPayload
);

// Device type enums matching domain models
public static class DeviceTypeWire {
    public const byte Lighting          = 1;
    public const byte Switch            = 2;
    public const byte SmartPlug         = 3;
    public const byte MotionSensor      = 4;
    public const byte TemperatureSensor = 5;
    public const byte SmartTV           = 6;
    public const byte SecurityCamera    = 7;
    public const byte Doorbell          = 8;
    public const byte GameConsole       = 9;
    public const byte SoundSystem       = 10;
    public const byte HumiditySensor    = 11;
    public const byte LightSensor       = 12;
    public const byte MotionDetector    = 13;
    public const byte PressureSensor    = 14;
}

public static class SmartDeviceTypeWire {
    public const byte ThermostatSensor = 1;
    public const byte SecurityCamera   = 2;
    public const byte DoorLock         = 3;
    public const byte LightSwitch      = 4;
    public const byte MotionDetector   = 5;
    public const byte SmokeDetector    = 6;
    public const byte WaterLeakSensor  = 7;
    public const byte EnergyMeter      = 8;
    public const byte WeatherStation   = 9;
    public const byte AirQualitySensor = 10;
    public const byte WindowSensor     = 11;
    public const byte SmartPlug        = 12;
    public const byte GarageDoorOpener = 13;
    public const byte Irrigation       = 14;
    public const byte HomeAssistant    = 15;
    public const byte GameConsole      = 16;
    public const byte SoundSystem      = 17;
    public const byte SmartTV          = 18;
    public const byte SmartSpeaker     = 19;
}

public static class ZoneTypeWire {
    public const byte Kitchen    = 1;
    public const byte LivingRoom = 2;
    public const byte Bedroom    = 3;
    public const byte Bathroom   = 4;
    public const byte Garage     = 5;
    public const byte Office     = 6;
    public const byte DiningRoom = 7;
    public const byte Basement   = 8;
    public const byte Attic      = 9;
    public const byte Laundry    = 10;
    public const byte Outdoor    = 11;
}

public static class DeviceStatusWire {
    public const byte Online      = 1;
    public const byte Offline     = 2;
    public const byte Maintenance = 3;
    public const byte LowBattery  = 4;
}

public static class SensorUnitWire {
    public const byte Celsius    = 1;
    public const byte Fahrenheit = 2;
    public const byte Percent    = 3;
    public const byte Watts      = 4;
    public const byte Volts      = 5;
    public const byte Amperes    = 6;
    public const byte Lumens     = 7;
    public const byte DeciBels   = 8;
    public const byte Pascal     = 9;
    public const byte PPM        = 10; // Parts per million
}

public static class EventTypeWire {
    public const byte DeviceStartup        = 1;
    public const byte DeviceShutdown       = 2;
    public const byte MotionDetected       = 3;
    public const byte MotionCleared        = 4;
    public const byte DoorOpened           = 5;
    public const byte DoorClosed           = 6;
    public const byte AlarmTriggered       = 7;
    public const byte AlarmCleared         = 8;
    public const byte ButtonPressed        = 9;
    public const byte ButtonReleased       = 10;
    public const byte BatteryLow           = 11;
    public const byte ConnectionLost       = 12;
    public const byte ConnectionRestored   = 13;
    public const byte FirmwareUpdate       = 14;
    public const byte ConfigurationChanged = 15;
}

// Performance optimization utilities
public static class TelemetryMessageFactory {
    public static TelemetryMessage CreateStateChange(
        string deviceId, byte deviceType, byte smartDeviceType, string zoneId,
        byte zoneType,
        long unixTimestamp, StateChangePayload payload
    ) {
        var payloadBytes = SerializeStateChangePayload(payload);
        return new TelemetryMessage(
            deviceId, deviceType, smartDeviceType,
            zoneId, zoneType,
            unixTimestamp, TelemetryMessageType.StateChange, payloadBytes
        );
    }

    public static TelemetryMessage CreateSensorReading(
        string deviceId, byte deviceType, byte smartDeviceType, string zoneId,
        byte zoneType,
        long unixTimestamp, SensorReadingPayload payload
    ) {
        var payloadBytes = SerializeSensorReadingPayload(payload);
        return new TelemetryMessage(
            deviceId, deviceType, smartDeviceType,
            zoneId, zoneType,
            unixTimestamp, TelemetryMessageType.SensorReading, payloadBytes
        );
    }

    public static TelemetryMessage CreateEvent(
        string deviceId, byte deviceType, byte smartDeviceType, string zoneId,
        byte zoneType,
        long unixTimestamp, EventPayload payload
    ) {
        var payloadBytes = SerializeEventPayload(payload);
        return new TelemetryMessage(
            deviceId, deviceType, smartDeviceType,
            zoneId, zoneType,
            unixTimestamp, TelemetryMessageType.Event, payloadBytes
        );
    }

    public static TelemetryMessage CreateUsage(
        string deviceId, byte deviceType, byte smartDeviceType, string zoneId,
        byte zoneType,
        long unixTimestamp, UsagePayload payload
    ) {
        var payloadBytes = SerializeUsagePayload(payload);
        return new TelemetryMessage(
            deviceId, deviceType, smartDeviceType,
            zoneId, zoneType,
            unixTimestamp, TelemetryMessageType.Usage, payloadBytes
        );
    }

    public static TelemetryMessage CreateHeartbeat(
        string deviceId, byte deviceType, byte smartDeviceType, string zoneId,
        byte zoneType,
        long unixTimestamp, HeartbeatPayload payload
    ) {
        var payloadBytes = SerializeHeartbeatPayload(payload);
        return new TelemetryMessage(
            deviceId, deviceType, smartDeviceType,
            zoneId, zoneType,
            unixTimestamp, TelemetryMessageType.Heartbeat, payloadBytes
        );
    }

    // Binary serialization methods - optimized for minimal allocations
    static ReadOnlyMemory<byte> SerializeStateChangePayload(StateChangePayload payload) {
        // Implementation would use high-performance binary serialization
        // For now, placeholder that would be replaced with actual binary serialization
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    static ReadOnlyMemory<byte> SerializeSensorReadingPayload(SensorReadingPayload payload) {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    static ReadOnlyMemory<byte> SerializeEventPayload(EventPayload payload) {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    static ReadOnlyMemory<byte> SerializeUsagePayload(UsagePayload payload) {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    static ReadOnlyMemory<byte> SerializeHeartbeatPayload(HeartbeatPayload payload) {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
}

// Domain to Contract mapping - high-performance transformation
public static class TelemetryContractMapper {
    public static TelemetryMessage ToContract(this DeviceTelemetry telemetry) =>
        telemetry switch {
            StateChangeTelemetry state => TelemetryMessageFactory.CreateStateChange(
                state.DeviceId.ToString(),
                MapDeviceType(state.DeviceType),
                MapSmartDeviceType(state.DeviceType),
                state.ZoneId,
                MapZoneType(state.ZoneType),
                TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds(),
                new StateChangePayload(
                    state.Property,
                    ExtractFloatValue(state.PreviousValue),
                    ExtractFloatValue(state.NewValue),
                    ExtractStringValue(state.PreviousValue),
                    ExtractStringValue(state.NewValue),
                    DetermineValueType(state.NewValue)
                )
            ),

            SensorReadingTelemetry sensor => TelemetryMessageFactory.CreateSensorReading(
                sensor.DeviceId.ToString(),
                MapDeviceType(sensor.DeviceType),
                MapSmartDeviceType(sensor.DeviceType),
                sensor.ZoneId,
                MapZoneType(sensor.ZoneType),
                TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds(),
                new SensorReadingPayload(
                    (float)sensor.Value,
                    MapSensorUnit(sensor.Unit),
                    MapReadingQuality(sensor.Quality)
                )
            ),

            EventTelemetry evt => TelemetryMessageFactory.CreateEvent(
                evt.DeviceId.ToString(),
                MapDeviceType(evt.DeviceType),
                MapSmartDeviceType(evt.DeviceType),
                evt.ZoneId,
                MapZoneType(evt.ZoneType),
                TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds(),
                new EventPayload(
                    MapEventType(evt.EventType),
                    evt.EventData != null ? System.Text.Json.JsonSerializer.Serialize(evt.EventData) : null,
                    null, // NumericValues - could be extracted from EventData if needed
                    3     // Medium priority by default
                )
            ),

            UsageTelemetry usage => TelemetryMessageFactory.CreateUsage(
                usage.DeviceId.ToString(),
                MapDeviceType(usage.DeviceType),
                MapSmartDeviceType(usage.DeviceType),
                usage.ZoneId,
                MapZoneType(usage.ZoneType),
                TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds(),
                new UsagePayload(
                    (float)usage.EnergyConsumed,
                    (int)usage.Duration.TotalSeconds,
                    (float)usage.EnergyConsumed,                                   // PeakPower - would need to be added to domain model
                    (float)usage.EnergyConsumed / (float)usage.Duration.TotalHours // AveragePower calculation
                )
            ),

            _ => throw new NotSupportedException($"Unknown telemetry type: {telemetry.GetType()}")
        };

    public static DeviceRegistryContract ToRegistryContract(this SmartDevice device, string zoneId, ZoneType zoneType) =>
        new(
            device.Id.ToString(),
            MapDeviceType(device.DeviceType),
            MapSmartDeviceType(device.DeviceType),
            device.Name,
            device.Brand,
            device.Model,
            zoneId,
            MapZoneType(zoneType),
            MapDeviceStatus(device.Status),
            device.IsControllable,
            device.SupportedCommands,
            device.MeasurementTypes,
            TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds()
        );

    public static ZoneMappingContract ToZoneMappingContract(this HomeZone zone, string[] deviceIds) =>
        new(
            zone.ZoneId,
            zone.ZoneName,
            MapZoneType(zone.Type),
            zone.Floor,
            zone.ParentZoneId,
            deviceIds
        );

    // Mapping helper methods
    static byte MapDeviceType(DeviceType deviceType) =>
        deviceType switch {
            DeviceType.Lighting          => DeviceTypeWire.Lighting,
            DeviceType.Switch            => DeviceTypeWire.Switch,
            DeviceType.SmartPlug         => DeviceTypeWire.SmartPlug,
            DeviceType.MotionSensor      => DeviceTypeWire.MotionSensor,
            DeviceType.TemperatureSensor => DeviceTypeWire.TemperatureSensor,
            DeviceType.SmartTV           => DeviceTypeWire.SmartTV,
            DeviceType.SecurityCamera    => DeviceTypeWire.SecurityCamera,
            DeviceType.Doorbell          => DeviceTypeWire.Doorbell,
            DeviceType.GameConsole       => DeviceTypeWire.GameConsole,
            DeviceType.SoundSystem       => DeviceTypeWire.SoundSystem,
            DeviceType.HumiditySensor    => DeviceTypeWire.HumiditySensor,
            DeviceType.LightSensor       => DeviceTypeWire.LightSensor,
            DeviceType.MotionDetector    => DeviceTypeWire.MotionDetector,
            DeviceType.PressureSensor    => DeviceTypeWire.PressureSensor,
            _                            => 0
        };

    static byte MapSmartDeviceType(DeviceType deviceType) =>
        deviceType switch {
            DeviceType.Lighting          => SmartDeviceTypeWire.LightSwitch,
            DeviceType.SmartPlug         => SmartDeviceTypeWire.SmartPlug,
            DeviceType.MotionSensor      => SmartDeviceTypeWire.MotionDetector,
            DeviceType.TemperatureSensor => SmartDeviceTypeWire.ThermostatSensor,
            DeviceType.SecurityCamera    => SmartDeviceTypeWire.SecurityCamera,
            DeviceType.SmartTV           => SmartDeviceTypeWire.SmartTV,
            DeviceType.GameConsole       => SmartDeviceTypeWire.GameConsole,
            DeviceType.SoundSystem       => SmartDeviceTypeWire.SoundSystem,
            _                            => SmartDeviceTypeWire.HomeAssistant
        };

    static byte MapZoneType(ZoneType zoneType) =>
        zoneType switch {
            ZoneType.Kitchen    => ZoneTypeWire.Kitchen,
            ZoneType.LivingRoom => ZoneTypeWire.LivingRoom,
            ZoneType.Bedroom    => ZoneTypeWire.Bedroom,
            ZoneType.Bathroom   => ZoneTypeWire.Bathroom,
            ZoneType.Garage     => ZoneTypeWire.Garage,
            ZoneType.Office     => ZoneTypeWire.Office,
            ZoneType.DiningRoom => ZoneTypeWire.DiningRoom,
            ZoneType.Basement   => ZoneTypeWire.Basement,
            ZoneType.Attic      => ZoneTypeWire.Attic,
            ZoneType.Laundry    => ZoneTypeWire.Laundry,
            ZoneType.Outdoor    => ZoneTypeWire.Outdoor,
            _                   => ZoneTypeWire.LivingRoom
        };

    static byte MapDeviceStatus(DeviceStatus status) =>
        status switch {
            DeviceStatus.Online      => DeviceStatusWire.Online,
            DeviceStatus.Offline     => DeviceStatusWire.Offline,
            DeviceStatus.Maintenance => DeviceStatusWire.Maintenance,
            DeviceStatus.LowBattery  => DeviceStatusWire.LowBattery,
            _                        => DeviceStatusWire.Online
        };

    static byte MapSensorUnit(string unit) =>
        unit.ToLowerInvariant() switch {
            "°c" or "celsius"    => SensorUnitWire.Celsius,
            "°f" or "fahrenheit" => SensorUnitWire.Fahrenheit,
            "%" or "percent"     => SensorUnitWire.Percent,
            "w" or "watts"       => SensorUnitWire.Watts,
            "v" or "volts"       => SensorUnitWire.Volts,
            "a" or "amperes"     => SensorUnitWire.Amperes,
            "lm" or "lumens"     => SensorUnitWire.Lumens,
            "db" or "decibels"   => SensorUnitWire.DeciBels,
            "pa" or "pascal"     => SensorUnitWire.Pascal,
            "ppm"                => SensorUnitWire.PPM,
            _                    => SensorUnitWire.Percent
        };

    static byte MapReadingQuality(ReadingQuality quality) =>
        quality switch {
            ReadingQuality.Excellent => 1,
            ReadingQuality.Good      => 2,
            ReadingQuality.Fair      => 3,
            ReadingQuality.Poor      => 4,
            ReadingQuality.Critical  => 5,
            _                        => 2
        };

    static byte MapEventType(string eventType) =>
        eventType.ToLowerInvariant() switch {
            "device_startup"        => EventTypeWire.DeviceStartup,
            "device_shutdown"       => EventTypeWire.DeviceShutdown,
            "motion_detected"       => EventTypeWire.MotionDetected,
            "motion_cleared"        => EventTypeWire.MotionCleared,
            "door_opened"           => EventTypeWire.DoorOpened,
            "door_closed"           => EventTypeWire.DoorClosed,
            "alarm_triggered"       => EventTypeWire.AlarmTriggered,
            "alarm_cleared"         => EventTypeWire.AlarmCleared,
            "button_pressed"        => EventTypeWire.ButtonPressed,
            "button_released"       => EventTypeWire.ButtonReleased,
            "battery_low"           => EventTypeWire.BatteryLow,
            "connection_lost"       => EventTypeWire.ConnectionLost,
            "connection_restored"   => EventTypeWire.ConnectionRestored,
            "firmware_update"       => EventTypeWire.FirmwareUpdate,
            "configuration_changed" => EventTypeWire.ConfigurationChanged,
            _                       => EventTypeWire.ConfigurationChanged
        };

    // Value extraction helpers for StateChange payloads
    static float ExtractFloatValue(object value) =>
        value switch {
            float f  => f,
            double d => (float)d,
            int i    => i,
            bool b   => b ? 1.0f : 0.0f,
            _        => 0.0f
        };

    static string? ExtractStringValue(object value) =>
        value switch {
            string s => s,
            bool b   => b.ToString(),
            _        => value?.ToString()
        };

    static byte DetermineValueType(object value) =>
        value switch {
            float or double or int => 1, // float
            string                 => 2, // string
            bool                   => 3, // bool
            _                      => 2  // default to string
        };
}
