using System.Runtime.CompilerServices;
using Bogus;

namespace HomeAutomation;

public static class HomeTemplates {
    public static readonly HomeTemplate BigMansion = new(
        "Big Mansion",
        [
            new RoomTemplate(
                RoomType.Kitchen, "Main Kitchen", [
                    new DeviceTemplate(
                        DeviceType.MotionSensor, "Motion Sensor", "Aqara",
                        "Motion Sensor P1"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Lights", "Philips",
                        "Hue White and Color"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Island Lights", "Philips",
                        "Hue White and Color"
                    ),
                    new DeviceTemplate(
                        DeviceType.SmartPlug, "Coffee Station", "TP-Link",
                        "Kasa Smart Plug"
                    ),
                    new DeviceTemplate(
                        DeviceType.TemperatureSensor, "Temperature Sensor", "Xiaomi",
                        "Mi Temperature Sensor"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.LivingRoom, "Great Room", [
                    new DeviceTemplate(
                        DeviceType.SmartTV, "85-inch TV", "Samsung",
                        "85-inch QLED"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Chandelier", "Philips",
                        "Hue White and Color"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Accent Lights", "Philips",
                        "Hue Lightstrip"
                    ),
                    new DeviceTemplate(
                        DeviceType.MotionSensor, "Motion Sensor", "Aqara",
                        "Motion Sensor P1"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.Bedroom, "Master Suite", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "Philips",
                        "Hue White and Color"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Bedside Lamps", "Philips",
                        "Hue White"
                    ),
                    new DeviceTemplate(
                        DeviceType.SmartTV, "Bedroom TV", "Samsung",
                        "55-inch QLED"
                    )
                ]
            )
        ],
        ResidentProfile.Family(2, 3)
    );

    public static readonly HomeTemplate FamilyHouse = new(
        "Family House",
        [
            new RoomTemplate(
                RoomType.Kitchen, "Kitchen", [
                    new DeviceTemplate(
                        DeviceType.MotionSensor, "Motion Sensor", "Aqara",
                        "Motion Sensor P1"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "Philips",
                        "Hue White"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Under Cabinet Light", "Philips",
                        "Hue Lightstrip"
                    ),
                    new DeviceTemplate(
                        DeviceType.SmartPlug, "Coffee Maker Plug", "TP-Link",
                        "Kasa Smart Plug"
                    ),
                    new DeviceTemplate(
                        DeviceType.SmartPlug, "Microwave Plug", "TP-Link",
                        "Kasa Smart Plug"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.LivingRoom, "Living Room", [
                    new DeviceTemplate(
                        DeviceType.SmartTV, "Living Room TV", "Samsung",
                        "55-inch QLED"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "Philips",
                        "Hue White and Color"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Table Lamp", "IKEA",
                        "Tradfri Bulb"
                    ),
                    new DeviceTemplate(
                        DeviceType.MotionSensor, "Motion Sensor", "Aqara",
                        "Motion Sensor P1"
                    ),
                    new DeviceTemplate(
                        DeviceType.TemperatureSensor, "Temperature Sensor", "Xiaomi",
                        "Mi Temperature Sensor"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.Bedroom, "Master Bedroom", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "Philips",
                        "Hue White"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Side Table Lamp 1", "IKEA",
                        "Tradfri Bulb"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Side Table Lamp 2", "IKEA",
                        "Tradfri Bulb"
                    ),
                    new DeviceTemplate(
                        DeviceType.MotionSensor, "Motion Sensor", "Aqara",
                        "Motion Sensor P1"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.Bedroom, "Child Bedroom", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "Philips",
                        "Hue White"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Desk Lamp", "IKEA",
                        "Tradfri Bulb"
                    ),
                    new DeviceTemplate(
                        DeviceType.MotionSensor, "Motion Sensor", "Aqara",
                        "Motion Sensor P1"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.Bathroom, "Bathroom", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Main Light", "Philips",
                        "Hue White"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Mirror Light", "Philips",
                        "Hue White"
                    ),
                    new DeviceTemplate(
                        DeviceType.MotionSensor, "Motion Sensor", "Aqara",
                        "Motion Sensor P1"
                    ),
                    new DeviceTemplate(
                        DeviceType.TemperatureSensor, "Temperature Sensor", "Xiaomi",
                        "Mi Temperature Sensor"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.Garage, "Garage", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Garage Light", "Philips",
                        "Hue White"
                    ),
                    new DeviceTemplate(
                        DeviceType.MotionSensor, "Motion Sensor", "Aqara",
                        "Motion Sensor P1"
                    ),
                    new DeviceTemplate(
                        DeviceType.SecurityCamera, "Security Camera", "Ring",
                        "Indoor Cam"
                    )
                ]
            )
        ],
        ResidentProfile.Family()
    );

    public static readonly HomeTemplate SmallApartment = new(
        "Small Apartment",
        [
            new RoomTemplate(
                RoomType.Kitchen, "Kitchen", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "IKEA",
                        "Tradfri Bulb"
                    ),
                    new DeviceTemplate(
                        DeviceType.SmartPlug, "Coffee Maker", "TP-Link",
                        "Kasa Smart Plug"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.LivingRoom, "Living Room", [
                    new DeviceTemplate(
                        DeviceType.SmartTV, "TV", "Samsung",
                        "43-inch Smart TV"
                    ),
                    new DeviceTemplate(
                        DeviceType.Lighting, "Floor Lamp", "IKEA",
                        "Tradfri Bulb"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.Bedroom, "Bedroom", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "IKEA",
                        "Tradfri Bulb"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.Bathroom, "Bathroom", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Main Light", "IKEA",
                        "Tradfri Bulb"
                    )
                ]
            )
        ],
        ResidentProfile.Single()
    );

    public static readonly HomeTemplate SmallOffice = new(
        "Small Office",
        [
            new RoomTemplate(
                RoomType.Office, "Main Office", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "Philips",
                        "Hue White"
                    ),
                    new DeviceTemplate(
                        DeviceType.MotionSensor, "Motion Sensor", "Aqara",
                        "Motion Sensor P1"
                    ),
                    new DeviceTemplate(
                        DeviceType.TemperatureSensor, "Temperature Sensor", "Xiaomi",
                        "Mi Temperature Sensor"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.Office, "Meeting Room", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "Philips",
                        "Hue White"
                    ),
                    new DeviceTemplate(
                        DeviceType.SmartTV, "Conference TV", "Samsung",
                        "65-inch QLED"
                    )
                ]
            ),
            new RoomTemplate(
                RoomType.Kitchen, "Break Room", [
                    new DeviceTemplate(
                        DeviceType.Lighting, "Ceiling Light", "IKEA",
                        "Tradfri Bulb"
                    ),
                    new DeviceTemplate(
                        DeviceType.SmartPlug, "Coffee Machine", "TP-Link",
                        "Kasa Smart Plug"
                    )
                ]
            )
        ],
        ResidentProfile.Office()
    );
}

// V2 Smart Home Simulator with Builder Pattern
public class SmartHomeSimulator {
    readonly TimeSpan        _compressionRatio;
    readonly Home            _home;
    readonly Random          _random = Random.Shared;
    readonly ResidentProfile _residentProfile;

    internal SmartHomeSimulator(Home home, TimeSpan compressionRatio, ResidentProfile residentProfile) {
        _home             = home;
        _compressionRatio = compressionRatio;
        _residentProfile  = residentProfile;
    }

    public Home            Home             => _home;
    public TimeSpan        CompressionRatio => _compressionRatio;
    public ResidentProfile ResidentProfile  => _residentProfile;

    public static SmartHomeSimulatorBuilder Create() => new();

    public async IAsyncEnumerable<DeviceTelemetry> GenerateTelemetryAsync(
        int? days = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        var dayCount        = 0;
        var simulationStart = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested && (days == null || dayCount < days)) {
            var dayEvents = GenerateDailyEvents(simulationStart.AddDays(dayCount));

            foreach (var telemetry in dayEvents) {
                yield return telemetry;

                // Calculate compressed delay
                var realTimeDelay = TimeSpan.FromMilliseconds(_compressionRatio.TotalMilliseconds / 1440); // 1440 minutes per day
                if (realTimeDelay > TimeSpan.Zero) await Task.Delay(realTimeDelay, cancellationToken);
            }

            dayCount++;
        }
    }

    public IEnumerable<DeviceTelemetry> GenerateBatch(TimeSpan duration) {
        var events    = new List<DeviceTelemetry>();
        var totalDays = (int)Math.Ceiling(duration.TotalDays);
        var startTime = DateTime.UtcNow;

        for (var day = 0; day < totalDays; day++) events.AddRange(GenerateDailyEvents(startTime.AddDays(day)));

        return events.OrderBy(e => e.Timestamp);
    }

    IEnumerable<DeviceTelemetry> GenerateDailyEvents(DateTime date) {
        var events = new List<DeviceTelemetry>();

        // Generate events based on resident profile
        switch (_residentProfile.Type) {
            case "Family":
                events.AddRange(GenerateFamilyDayEvents(date));
                break;

            case "Single":
                events.AddRange(GenerateSingleDayEvents(date));
                break;

            case "Office":
                events.AddRange(GenerateOfficeDayEvents(date));
                break;

            default:
                events.AddRange(GenerateBasicDayEvents(date));
                break;
        }

        return events.OrderBy(e => e.Timestamp);
    }

    IEnumerable<DeviceTelemetry> GenerateFamilyDayEvents(DateTime date) {
        var events = new List<DeviceTelemetry>();

        // Morning routine (6:30 - 8:00)
        events.AddRange(GenerateTimeSlotEvents(date.AddHours(6.5), TimeSpan.FromHours(1.5), 0.7));

        // Day time activity (8:00 - 17:00) - lower activity
        events.AddRange(GenerateTimeSlotEvents(date.AddHours(8), TimeSpan.FromHours(9), 0.2));

        // Evening routine (17:00 - 22:00) - high activity
        events.AddRange(GenerateTimeSlotEvents(date.AddHours(17), TimeSpan.FromHours(5), 0.8));

        // Night time (22:00 - 6:30) - minimal activity
        events.AddRange(GenerateTimeSlotEvents(date.AddHours(22), TimeSpan.FromHours(8.5), 0.1));

        return events;
    }

    IEnumerable<DeviceTelemetry> GenerateSingleDayEvents(DateTime date) {
        var events = new List<DeviceTelemetry>();

        // Later morning routine (8:00 - 9:00)
        events.AddRange(GenerateTimeSlotEvents(date.AddHours(8), TimeSpan.FromHours(1), 0.6));

        // Work from home activity (9:00 - 17:00)
        events.AddRange(GenerateTimeSlotEvents(date.AddHours(9), TimeSpan.FromHours(8), 0.3));

        // Evening activity (17:00 - 23:00)
        events.AddRange(GenerateTimeSlotEvents(date.AddHours(17), TimeSpan.FromHours(6), 0.6));

        // Night time (23:00 - 8:00)
        events.AddRange(GenerateTimeSlotEvents(date.AddHours(23), TimeSpan.FromHours(9), 0.05));

        return events;
    }

    IEnumerable<DeviceTelemetry> GenerateOfficeDayEvents(DateTime date) {
        // Skip weekends for office
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) return [];

        var events = new List<DeviceTelemetry>();

        // Office hours (8:00 - 18:00)
        events.AddRange(GenerateTimeSlotEvents(date.AddHours(8), TimeSpan.FromHours(10), 0.5));

        return events;
    }

    IEnumerable<DeviceTelemetry> GenerateBasicDayEvents(DateTime date) => GenerateTimeSlotEvents(date, TimeSpan.FromDays(1), 0.3);

    IEnumerable<DeviceTelemetry> GenerateTimeSlotEvents(DateTime startTime, TimeSpan duration, double activityLevel) {
        var devices    = _home.GetAllDevices().ToArray(); // Convert to array for performance
        var eventCount = (int)(devices.Length * activityLevel * (duration.TotalHours / 24));
        var events     = new List<DeviceTelemetry>(eventCount); // Pre-allocate capacity

        for (var i = 0; i < eventCount; i++) {
            var device    = devices[_random.Next(devices.Length)];
            var eventTime = startTime.Add(TimeSpan.FromTicks((long)(_random.NextDouble() * duration.Ticks)));
            var zoneId    = _home.GetDeviceZone(device.Id) ?? "UNKNOWN";
            var zone      = _home.GetZone(zoneId);
            var zoneType  = zone?.Type ?? ZoneType.LivingRoom;

            DeviceTelemetry telemetry = device switch {
                LightingDevice light => new StateChangeTelemetry(
                    light.Id, light.DeviceType, light.Brand,
                    light.Model,
                    zoneId, zoneType, eventTime,
                    "IsOn", light.IsOn, !light.IsOn
                ),

                MotionSensorDevice motion => new EventTelemetry(
                    motion.Id, motion.DeviceType, motion.Brand,
                    motion.Model,
                    zoneId, zoneType, eventTime,
                    "MotionDetected", new Dictionary<string, object> { ["detected"] = true }
                ),

                TemperatureSensorDevice temp => new SensorReadingTelemetry(
                    temp.Id, temp.DeviceType, temp.Brand,
                    temp.Model,
                    zoneId, zoneType, eventTime,
                    _random.NextDouble() * 10 + 20, "°C"
                ), // 20-30°C

                SmartTVDevice tv => new StateChangeTelemetry(
                    tv.Id, tv.DeviceType, tv.Brand,
                    tv.Model,
                    zoneId, zoneType, eventTime,
                    "IsOn", tv.IsOn, !tv.IsOn
                ),

                SmartPlugDevice plug => new UsageTelemetry(
                    plug.Id, plug.DeviceType, plug.Brand,
                    plug.Model,
                    zoneId, zoneType, eventTime,
                    _random.NextDouble() * 2, TimeSpan.FromHours(1)
                ),

                _ => new EventTelemetry(
                    device.Id, device.DeviceType, device.Brand,
                    device.Model,
                    zoneId, zoneType, eventTime,
                    "DeviceUpdate"
                )
            };

            events.Add(telemetry);
        }

        return events;
    }
}

// Builder Pattern Implementation
public class SmartHomeSimulatorBuilder {
    readonly Dictionary<RoomType, List<DeviceTemplate>> _roomOverrides    = new();
    TimeSpan                                            _compressionRatio = TimeSpan.FromMinutes(1); // 1 day per minute default
    HomeAddress?                                        _customAddress;
    ResidentProfile?                                    _customResidents;
    HomeTemplate                                        _homeTemplate = HomeTemplates.FamilyHouse;

    public SmartHomeSimulatorBuilder WithFamilyHouse() {
        _homeTemplate = HomeTemplates.FamilyHouse;
        return this;
    }

    public SmartHomeSimulatorBuilder WithSmallApartment() {
        _homeTemplate = HomeTemplates.SmallApartment;
        return this;
    }

    public SmartHomeSimulatorBuilder WithSmallOffice() {
        _homeTemplate = HomeTemplates.SmallOffice;
        return this;
    }

    public SmartHomeSimulatorBuilder WithBigMansion() {
        _homeTemplate = HomeTemplates.BigMansion;
        return this;
    }

    public SmartHomeSimulatorBuilder WithHomeTemplate(HomeTemplate template) {
        _homeTemplate = template;
        return this;
    }

    public SmartHomeSimulatorBuilder WithAddress(string street, string city, string state, string zipCode = "00000") {
        _customAddress = new HomeAddress(
            street, city, state,
            zipCode, 0, 0
        );

        return this;
    }

    public SmartHomeSimulatorBuilder WithCompressionRatio(TimeSpan ratio) {
        _compressionRatio = ratio;
        return this;
    }

    public SmartHomeSimulatorBuilder WithResidents(ResidentProfile profile) {
        _customResidents = profile;
        return this;
    }

    public SmartHomeSimulatorBuilder WithCustomRoom(RoomType roomType, Action<RoomBuilder> configure) {
        var roomBuilder = new RoomBuilder();
        configure(roomBuilder);
        _roomOverrides[roomType] = roomBuilder.GetDeviceTemplates();
        return this;
    }

    public SmartHomeSimulatorBuilder OverrideRoom(RoomType roomType, Action<RoomBuilder> configure) {
        var existingTemplate = _homeTemplate.RoomTemplates.FirstOrDefault(r => r.Type == roomType);
        var roomBuilder      = new RoomBuilder(existingTemplate?.DefaultDevices ?? []);
        configure(roomBuilder);
        _roomOverrides[roomType] = roomBuilder.GetDeviceTemplates();
        return this;
    }

    public SmartHomeSimulator Build() {
        var address   = _customAddress ?? GenerateRandomAddress();
        var home      = BuildHome(address);
        var residents = _customResidents ?? _homeTemplate.ResidentProfile;

        return new SmartHomeSimulator(home, _compressionRatio, residents);
    }

    Home BuildHome(HomeAddress address) {
        var homeId = HomeId.New();
        var rooms  = new List<Room>();

        foreach (var roomTemplate in _homeTemplate.RoomTemplates) {
            var roomId = RoomId.New();
            var deviceTemplates = _roomOverrides.TryGetValue(roomTemplate.Type, out var overrides)
                ? overrides
                : roomTemplate.DefaultDevices.ToList();

            var devices = deviceTemplates.Select(dt => CreateDevice(dt)).ToArray();
            rooms.Add(
                new Room(
                    roomId, roomTemplate.Type, roomTemplate.Name,
                    devices
                )
            );
        }

        // Convert rooms to zones and devices - using arrays for performance
        var zoneList    = new List<HomeZone>(rooms.Count);
        var deviceList  = new List<SmartDevice>(rooms.Count * 5); // Estimate 5 devices per room
        var mappingList = new List<DeviceZoneMapping>(rooms.Count * 5);

        foreach (var room in rooms) {
            var zoneId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var zone = new HomeZone(
                zoneId, room.Name, (ZoneType)(int)room.Type,
                "Ground Floor"
            );

            zoneList.Add(zone);

            foreach (var device in room.Devices) {
                deviceList.Add(device);
                mappingList.Add(new DeviceZoneMapping(device.Id.ToString(), zoneId));
            }
        }

        return new Home(
            homeId, address, zoneList.ToArray(),
            deviceList.ToArray(), mappingList.ToArray()
        );
    }

    SmartDevice CreateDevice(DeviceTemplate template) {
        var deviceId = DeviceId.New();
        var status   = DeviceStatus.Online;

        return template.DeviceType switch {
            DeviceType.Lighting => new LightingDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, false,
                100
            ),
            DeviceType.Switch => new SwitchDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, false,
                0.0
            ),
            DeviceType.SmartPlug => new SmartPlugDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, false,
                0.0
            ),
            DeviceType.MotionSensor => new MotionSensorDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, false,
                DateTime.UtcNow.AddDays(-1), new BatteryLevel(Random.Shared.Next(70, 100))
            ),
            DeviceType.TemperatureSensor => new TemperatureSensorDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, 22.0,
                new BatteryLevel(Random.Shared.Next(70, 100))
            ),
            DeviceType.SmartTV => new SmartTVDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, false
            ),
            DeviceType.SecurityCamera => new SecurityCameraDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, false,
                false
            ),
            DeviceType.Doorbell => new DoorbellDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, false,
                true
            ),
            DeviceType.GameConsole => new GameConsoleDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, false
            ),
            DeviceType.SoundSystem => new SoundSystemDevice(
                deviceId, template.Name, template.Brand,
                template.Model, status, false
            ),
            _ => throw new ArgumentException($"Unknown device type: {template.DeviceType}")
        };
    }

    HomeAddress GenerateRandomAddress() {
        var faker = new Faker();
        return new HomeAddress(
            faker.Address.StreetAddress(),
            faker.Address.City(),
            faker.Address.StateAbbr(),
            faker.Address.ZipCode(),
            faker.Address.Latitude(),
            faker.Address.Longitude()
        );
    }
}

public class RoomBuilder {
    readonly List<DeviceTemplate> _devices = new();

    public RoomBuilder(DeviceTemplate[]? existingDevices = null) {
        if (existingDevices != null) _devices.AddRange(existingDevices);
    }

    public RoomBuilder AddDevice(DeviceType deviceType, string name, string brand, string model) {
        _devices.Add(
            new DeviceTemplate(
                deviceType, name, brand,
                model
            )
        );

        return this;
    }

    public RoomBuilder AddLighting(string name, string brand = "Philips", string model = "Hue White") =>
        AddDevice(
            DeviceType.Lighting, name, brand,
            model
        );

    public RoomBuilder AddMotionSensor(string name = "Motion Sensor", string brand = "Aqara", string model = "Motion Sensor P1") =>
        AddDevice(
            DeviceType.MotionSensor, name, brand,
            model
        );

    public RoomBuilder AddSmartTV(string name, string brand = "Samsung", string model = "Smart TV") =>
        AddDevice(
            DeviceType.SmartTV, name, brand,
            model
        );

    public RoomBuilder AddSmartPlug(string name, string brand = "TP-Link", string model = "Kasa Smart Plug") =>
        AddDevice(
            DeviceType.SmartPlug, name, brand,
            model
        );

    public RoomBuilder RemoveDevice<T>() where T : SmartDevice {
        var deviceType = typeof(T).Name.Replace("Device", "");
        if (Enum.TryParse<DeviceType>(deviceType, out var enumValue)) _devices.RemoveAll(d => d.DeviceType == enumValue);
        return this;
    }

    public RoomBuilder RemoveDeviceByName(string name) {
        _devices.RemoveAll(d => d.Name == name);
        return this;
    }

    internal List<DeviceTemplate> GetDeviceTemplates() => new(_devices);
}

// V2 Extension Methods
public static class SmartHomeExtensions {
    public static IEnumerable<DeviceTelemetry> FilterByDeviceType(this IEnumerable<DeviceTelemetry> telemetry, DeviceType deviceType) {
        return telemetry.Where(t => t.DeviceType == deviceType);
    }

    public static IEnumerable<DeviceTelemetry> FilterByZone(this IEnumerable<DeviceTelemetry> telemetry, ZoneType zoneType) {
        return telemetry.Where(t => t.ZoneType == zoneType);
    }

    public static IEnumerable<DeviceTelemetry> FilterByBrand(this IEnumerable<DeviceTelemetry> telemetry, string brand) {
        return telemetry.Where(t => t.Brand == brand);
    }
}
