namespace HomeAutomation;

// Core Domain Models
public readonly record struct HomeId(Guid Value) {
    public static   HomeId New()      => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N")[..8].ToUpperInvariant();
}

public readonly record struct RoomId(Guid Value) {
    public static   RoomId New()      => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N")[..8].ToUpperInvariant();
}

public readonly record struct DeviceId(Guid Value) {
    public static   DeviceId New()      => new(Guid.NewGuid());
    public override string   ToString() => Value.ToString("N")[..12].ToUpperInvariant();
}

public record HomeAddress(
    string Street,
    string City,
    string State,
    string ZipCode,
    double Latitude,
    double Longitude
) {
    public override string ToString() => $"{Street}, {City}, {State} {ZipCode}";
}

public record HomeZone(
    string   ZoneId,
    string   ZoneName,
    ZoneType Type,
    string?  Floor,
    string?  ParentZoneId = null
);

public record DeviceZoneMapping(
    string DeviceId,
    string ZoneId
);

public record Home(
    HomeId                Id,
    HomeAddress           Address,
    HomeZone[]            Zones,
    SmartDevice[]         Devices,
    DeviceZoneMapping[]   DeviceMappings
) {
    public IEnumerable<SmartDevice> GetAllDevices() => Devices;
    
    public SmartDevice? GetDevice(DeviceId deviceId) {
        var deviceIdString = deviceId.ToString();
        foreach (var device in Devices) {
            if (device.Id.ToString() == deviceIdString)
                return device;
        }
        return null;
    }
    
    public IEnumerable<SmartDevice> GetDevicesByType(DeviceType type) {
        foreach (var device in Devices) {
            if (device.DeviceType == type)
                yield return device;
        }
    }
    
    public IEnumerable<SmartDevice> GetDevicesInZone(string zoneId) {
        var deviceIdsInZone = new HashSet<string>();
        foreach (var mapping in DeviceMappings) {
            if (mapping.ZoneId == zoneId)
                deviceIdsInZone.Add(mapping.DeviceId);
        }
        
        foreach (var device in Devices) {
            if (deviceIdsInZone.Contains(device.Id.ToString()))
                yield return device;
        }
    }
    
    public HomeZone? GetZone(string zoneId) {
        foreach (var zone in Zones) {
            if (zone.ZoneId == zoneId)
                return zone;
        }
        return null;
    }
    
    public string? GetDeviceZone(DeviceId deviceId) {
        var deviceIdString = deviceId.ToString();
        foreach (var mapping in DeviceMappings) {
            if (mapping.DeviceId == deviceIdString)
                return mapping.ZoneId;
        }
        return null;
    }
}

// Keep Room for backward compatibility with templates
public record Room(
    RoomId            Id,
    RoomType          Type,
    string            Name,
    SmartDevice[]     Devices
);

public enum RoomType : byte {
    Kitchen    = 1,
    LivingRoom = 2,
    Bedroom    = 3,
    Bathroom   = 4,
    Garage     = 5,
    Office     = 6,
    DiningRoom = 7,
    Basement   = 8,
    Attic      = 9,
    Laundry    = 10
}

public enum DeviceType : byte {
    Lighting          = 1,
    Switch            = 2,
    SmartPlug         = 3,
    MotionSensor      = 4,
    TemperatureSensor = 5,
    SmartTV           = 6,
    SecurityCamera    = 7,
    Doorbell          = 8,
    GameConsole       = 9,
    SoundSystem       = 10,
    HumiditySensor    = 11,
    LightSensor       = 12,
    MotionDetector    = 13,
    PressureSensor    = 14
}

public enum SmartHomeDeviceType : byte {
    ThermostatSensor  = 1,
    SecurityCamera    = 2,
    DoorLock          = 3,
    LightSwitch       = 4,
    MotionDetector    = 5,
    SmokeDetector     = 6,
    WaterLeakSensor   = 7,
    EnergyMeter       = 8,
    WeatherStation    = 9,
    AirQualitySensor  = 10,
    WindowSensor      = 11,
    SmartPlug         = 12,
    GarageDoorOpener  = 13,
    Irrigation        = 14,
    HomeAssistant     = 15,
    GameConsole       = 16,
    SoundSystem       = 17,
    SmartTV           = 18,
    SmartSpeaker      = 19
}

public enum ZoneType : byte {
    Kitchen    = 1,
    LivingRoom = 2,
    Bedroom    = 3,
    Bathroom   = 4,
    Garage     = 5,
    Office     = 6,
    DiningRoom = 7,
    Basement   = 8,
    Attic      = 9,
    Laundry    = 10,
    Outdoor    = 11
}

// Smart Device Hierarchy - Rich domain objects
public abstract record SmartDevice(
    DeviceId     Id,
    string       Name,
    string       Brand,
    string       Model,
    DeviceStatus Status
) {
    public abstract DeviceType             DeviceType         { get; }
    public virtual  SmartHomeDeviceType    SmartDeviceType    => MapToSmartDeviceType(DeviceType);
    public virtual  bool                   IsControllable     => true;
    public virtual  string[]               SupportedCommands  => [];
    public virtual  string[]               MeasurementTypes   => [];

    static SmartHomeDeviceType MapToSmartDeviceType(DeviceType deviceType) => deviceType switch {
        DeviceType.Lighting       => SmartHomeDeviceType.LightSwitch,
        DeviceType.SmartPlug      => SmartHomeDeviceType.SmartPlug,
        DeviceType.MotionSensor   => SmartHomeDeviceType.MotionDetector,
        DeviceType.SecurityCamera => SmartHomeDeviceType.SecurityCamera,
        DeviceType.SmartTV        => SmartHomeDeviceType.SmartTV,
        DeviceType.GameConsole    => SmartHomeDeviceType.GameConsole,
        DeviceType.SoundSystem    => SmartHomeDeviceType.SoundSystem,
        _                         => SmartHomeDeviceType.HomeAssistant
    };
}

public record LightingDevice(
    DeviceId     Id,
    string       Name,
    string       Brand,
    string       Model,
    DeviceStatus Status,
    bool         IsOn,
    int          Brightness,
    string?      Color = null
) : SmartDevice(Id, Name, Brand, Model, Status) {
    public override DeviceType DeviceType         => DeviceType.Lighting;
    public override string[]   SupportedCommands => ["turn_on", "turn_off", "set_brightness", "set_color"];
    public override string[]   MeasurementTypes  => ["state", "brightness"];
}

public record SwitchDevice(
    DeviceId     Id,
    string       Name,
    string       Brand,
    string       Model,
    DeviceStatus Status,
    bool         IsOn,
    double       PowerDraw
) : SmartDevice(Id, Name, Brand, Model, Status) {
    public override DeviceType DeviceType         => DeviceType.Switch;
    public override string[]   SupportedCommands => ["turn_on", "turn_off"];
    public override string[]   MeasurementTypes  => ["state", "power"];
}

public record SmartPlugDevice(
    DeviceId     Id,
    string       Name,
    string       Brand,
    string       Model,
    DeviceStatus Status,
    bool         IsOn,
    double       PowerDraw,
    string?      ConnectedAppliance = null
) : SmartDevice(Id, Name, Brand, Model, Status) {
    public override DeviceType DeviceType         => DeviceType.SmartPlug;
    public override string[]   SupportedCommands => ["turn_on", "turn_off"];
    public override string[]   MeasurementTypes  => ["state", "power", "energy"];
}

public record MotionSensorDevice(
    DeviceId      Id,
    string        Name,
    string        Brand,
    string        Model,
    DeviceStatus  Status,
    bool          MotionDetected,
    DateTime      LastMotion,
    BatteryLevel? Battery = null
) : SmartDevice(Id, Name, Brand, Model, Status) {
    public override DeviceType DeviceType        => DeviceType.MotionSensor;
    public override bool       IsControllable    => false;
    public override string[]   MeasurementTypes => ["motion", "battery"];
}

public record TemperatureSensorDevice(
    DeviceId      Id,
    string        Name,
    string        Brand,
    string        Model,
    DeviceStatus  Status,
    double        Temperature,
    BatteryLevel? Battery = null
) : SmartDevice(Id, Name, Brand, Model, Status) {
    public override DeviceType          DeviceType        => DeviceType.TemperatureSensor;
    public override SmartHomeDeviceType SmartDeviceType   => SmartHomeDeviceType.ThermostatSensor;
    public override bool                IsControllable    => false;
    public override string[]            MeasurementTypes  => ["temperature", "battery"];
}

public record SmartTVDevice(
    DeviceId     Id,
    string       Name,
    string       Brand,
    string       Model,
    DeviceStatus Status,
    bool         IsOn,
    string?      CurrentChannel = null,
    int?         Volume = null
) : SmartDevice(Id, Name, Brand, Model, Status) {
    public override DeviceType DeviceType         => DeviceType.SmartTV;
    public override string[]   SupportedCommands => ["turn_on", "turn_off", "set_volume", "change_channel", "mute"];
    public override string[]   MeasurementTypes  => ["state", "volume", "channel"];
}

public record SecurityCameraDevice(
    DeviceId Id, string Name, string Brand, string Model,
    DeviceStatus Status, bool IsRecording, bool MotionDetected
) : SmartDevice(
    Id, Name, Brand,
    Model, Status
) {
    public override DeviceType DeviceType => DeviceType.SecurityCamera;
    public override string[] SupportedCommands => ["start_recording", "stop_recording", "take_snapshot", "pan", "tilt"];
    public override string[] MeasurementTypes => ["recording_state", "motion", "video_quality"];
}

public record DoorbellDevice(
    DeviceId Id, string Name, string Brand, string Model,
    DeviceStatus Status, bool IsPressed, bool HasCamera, DateTime? LastPressed = null
) : SmartDevice(
    Id, Name, Brand,
    Model, Status
) {
    public override DeviceType DeviceType => DeviceType.Doorbell;
    public override bool IsControllable => false;
    public override string[] MeasurementTypes => ["button_press", "motion", "video_feed"];
}

public record GameConsoleDevice(
    DeviceId Id, string Name, string Brand, string Model,
    DeviceStatus Status, bool IsOn, string? CurrentGame = null
) : SmartDevice(
    Id, Name, Brand,
    Model, Status
) {
    public override DeviceType DeviceType => DeviceType.GameConsole;
    public override string[] SupportedCommands => ["turn_on", "turn_off", "launch_game", "suspend"];
    public override string[] MeasurementTypes => ["state", "active_game", "usage_time"];
}

public record SoundSystemDevice(
    DeviceId Id, string Name, string Brand, string Model,
    DeviceStatus Status, bool IsOn, int? Volume = null, string? CurrentSource = null
) : SmartDevice(
    Id, Name, Brand,
    Model, Status
) {
    public override DeviceType DeviceType => DeviceType.SoundSystem;
    public override string[] SupportedCommands => ["turn_on", "turn_off", "set_volume", "change_source", "mute"];
    public override string[] MeasurementTypes => ["state", "volume", "source", "audio_level"];
}

// Domain Telemetry Events (rich domain objects)
public abstract record DeviceTelemetry(
    DeviceId DeviceId, DeviceType DeviceType, string Brand, string Model, string ZoneId,
    ZoneType ZoneType, DateTime Timestamp
);

public record StateChangeTelemetry(
    DeviceId DeviceId, DeviceType DeviceType, string Brand, string Model, string ZoneId,
    ZoneType ZoneType, DateTime Timestamp, string Property, object PreviousValue, object NewValue
) : DeviceTelemetry(
    DeviceId, DeviceType, Brand,
    Model, ZoneId, ZoneType,
    Timestamp
);

public record SensorReadingTelemetry(
    DeviceId DeviceId, DeviceType DeviceType, string Brand, string Model, string ZoneId,
    ZoneType ZoneType, DateTime Timestamp, double Value, string Unit, ReadingQuality Quality = ReadingQuality.Good
) : DeviceTelemetry(
    DeviceId, DeviceType, Brand,
    Model, ZoneId, ZoneType,
    Timestamp
);

public record EventTelemetry(
    DeviceId DeviceId, DeviceType DeviceType, string Brand, string Model, string ZoneId,
    ZoneType ZoneType, DateTime Timestamp, string EventType, Dictionary<string, object>? EventData = null
) : DeviceTelemetry(
    DeviceId, DeviceType, Brand,
    Model, ZoneId, ZoneType,
    Timestamp
);

public record UsageTelemetry(
    DeviceId DeviceId, DeviceType DeviceType, string Brand, string Model, string ZoneId,
    ZoneType ZoneType, DateTime Timestamp, double EnergyConsumed, TimeSpan Duration
) : DeviceTelemetry(
    DeviceId, DeviceType, Brand,
    Model, ZoneId, ZoneType,
    Timestamp
);

public enum ReadingQuality : byte {
    Excellent = 1,
    Good      = 2,
    Fair      = 3,
    Poor      = 4,
    Critical  = 5
}

public readonly record struct BatteryLevel(byte Level) {
    public BatteryLevel(double level) : this((byte)Math.Clamp(level, 0, 100)) { }

    public bool IsLow      => Level < 20;
    public bool IsCritical => Level < 5;

    public BatteryStatus Status =>
        Level switch {
            < 5  => BatteryStatus.Critical,
            < 20 => BatteryStatus.Low,
            < 80 => BatteryStatus.Normal,
            _    => BatteryStatus.High
        };

    public static implicit operator double(BatteryLevel battery) => battery.Level;
    public static implicit operator BatteryLevel(double level)   => new(level);
}

public enum BatteryStatus : byte {
    Critical = 1,
    Low      = 2,
    Normal   = 3,
    High     = 4
}

public enum DeviceStatus : byte {
    Online      = 1,
    Offline     = 2,
    Maintenance = 3,
    LowBattery  = 4
}

// Home Templates
public record HomeTemplate(string Name, RoomTemplate[] RoomTemplates, ResidentProfile ResidentProfile);

public record RoomTemplate(RoomType Type, string Name, DeviceTemplate[] DefaultDevices);

public record DeviceTemplate(DeviceType DeviceType, string Name, string Brand, string Model, Dictionary<string, object>? DefaultProperties = null);

public record ResidentProfile(string Type, int AdultCount, int ChildCount, Dictionary<string, object>? Properties = null) {
    public static ResidentProfile Family(int adults = 2, int children = 2) => new("Family", adults, children);

    public static ResidentProfile Single(int age = 30) =>
        new(
            "Single", 1, 0,
            new() { ["age"] = age }
        );

    public static ResidentProfile Office(int employees = 5) => new("Office", employees, 0);

    public static ResidentProfile Elderly() =>
        new(
            "Elderly", 2, 0,
            new() { ["elderlyCouple"] = true }
        );
}
