using System.Text.Json;
using Shouldly;

namespace HomeAutomation;

public static class SensorReadingAssertions {
    public static void ShouldBeValidSensorReading(this SensorReading reading) {
        reading.DeviceId.ShouldNotBeNullOrWhiteSpace();
        reading.SensorType.ShouldBeOneOf(Enum.GetValues<SensorType>());
        reading.LocationId.ShouldNotBeNullOrWhiteSpace();
        reading.Unit.ShouldBeOneOf(Enum.GetValues<MeasurementUnit>());
        reading.Timestamp.ShouldBeGreaterThan(DateTime.MinValue);
        reading.Quality.ShouldBeOneOf(Enum.GetValues<ReadingQuality>());
    }

    public static void ShouldHaveRealisticTemperatureValue(this SensorReading reading) {
        reading.SensorType.ShouldBe(SensorType.Temperature);
        reading.Value.ShouldBeInRange(-10, 40);
        reading.Unit.ShouldBe(MeasurementUnit.Celsius);
    }

    public static void ShouldHaveValidBatteryLevel(this SensorReading reading) {
        if (reading.BatteryLevel.HasValue) reading.BatteryLevel.Value.ShouldBeInRange(0, 100);
    }

    public static void ShouldHaveMetadata(this SensorReading reading, string key, object expectedValue) {
        reading.Metadata.ShouldNotBeNull();
        reading.Metadata.Keys.ShouldContain(key);
        reading.Metadata[key].ShouldBe(expectedValue);
    }

    public static void ShouldBeMotionDetection(this SensorReading reading, bool isMotionDetected) {
        reading.SensorType.ShouldBe(SensorType.Motion);
        reading.Value.ShouldBe(isMotionDetected ? 1 : 0);
        reading.Unit.ShouldBe(MeasurementUnit.Boolean);
    }
}

public static class ProcessResultAssertions {
    public static void ShouldBeSuccess(this ProcessResult result, string? expectedMessage = null) {
        result.IsSuccess.ShouldBeTrue();
        if (expectedMessage != null) result.Message.ShouldBe(expectedMessage);
    }

    public static void ShouldBeFailure(this ProcessResult result, string? expectedErrorCode = null) {
        result.IsSuccess.ShouldBeFalse();
        if (expectedErrorCode != null) result.ErrorCode.ShouldBe(expectedErrorCode);
    }
}

public class SensorDeviceBuilder {
    double     _batteryLevel      = 100.0;
    string     _deviceId          = "TEST_DEVICE_001";
    DeviceType _deviceType        = DeviceType.TemperatureSensor;
    bool       _isOnline          = true;
    DateTime   _lastSeen          = DateTime.UtcNow;
    string     _locationId        = "LOC_TEST_001";
    TimeSpan   _reportingInterval = TimeSpan.FromMinutes(5);

    public SensorDeviceBuilder WithDeviceId(string deviceId) {
        _deviceId = deviceId;
        return this;
    }

    public SensorDeviceBuilder WithDeviceType(DeviceType deviceType) {
        _deviceType = deviceType;
        return this;
    }

    public SensorDeviceBuilder WithLocationId(string locationId) {
        _locationId = locationId;
        return this;
    }

    public SensorDeviceBuilder WithBatteryLevel(double batteryLevel) {
        _batteryLevel = batteryLevel;
        return this;
    }

    public SensorDeviceBuilder IsOffline() {
        _isOnline     = false;
        _batteryLevel = 0;
        return this;
    }

    public SensorDeviceBuilder WithReportingInterval(TimeSpan interval) {
        _reportingInterval = interval;
        return this;
    }

    public SensorDevice Build() =>
        new(
            _deviceId,
            _deviceType,
            _locationId,
            _lastSeen,
            _batteryLevel,
            _isOnline,
            _reportingInterval
        );
}

public class SimulatorBuilder {
    int _deviceCount = 10;

    public SimulatorBuilder WithDeviceCount(int deviceCount) {
        _deviceCount = deviceCount;
        return this;
    }

    public HomeAutomationSimulator BuildAndInitialize() {
        var simulator = new HomeAutomationSimulator();
        simulator.InitializeDevices(_deviceCount);
        return simulator;
    }
}

public abstract class SimulatorTestFixture {
    protected HomeAutomationSimulator Simulator    { get; private set; } = null!;
    protected List<SensorReading>     TestReadings { get; set; }         = [];

    [Before(Test)]
    public virtual void SetupFixture() {
        Simulator = new SimulatorBuilder()
            .WithDeviceCount(10)
            .BuildAndInitialize();
    }

    [After(Test)]
    public virtual void TeardownFixture() {
        Simulator?.Reset();
        TestReadings.Clear();
    }

    protected void GenerateTestData(int count = 100) {
        TestReadings = Simulator.GenerateBatch(count);
    }

    protected SensorReading GetRandomReading(string? sensorType = null) {
        if (TestReadings.Count == 0) GenerateTestData();

        var readings = sensorType == null
            ? TestReadings
            : TestReadings.Where(r => r.SensorType.ToString() == sensorType).ToList();

        readings.ShouldNotBeEmpty($"No readings found for sensor type: {sensorType}");
        return readings[Random.Shared.Next(readings.Count)];
    }
}

[Category("Unit")]
public class HomeAutomationSimulatorTests : SimulatorTestFixture {
    [Test]
    public void constructor_initializes_simulator() {
        var simulator = new HomeAutomationSimulator();

        simulator.ShouldNotBeNull();
        simulator.DeviceCount.ShouldBe(0);
    }

    [Test]
    public void initialize_devices_creates_specified_number_of_devices() {
        const int expectedDeviceCount = 25;

        Simulator.InitializeDevices(expectedDeviceCount);

        Simulator.DeviceCount.ShouldBe(expectedDeviceCount);
        Simulator.Devices.ShouldAllBe(d => !string.IsNullOrWhiteSpace(d.DeviceId));
        Simulator.Devices.ShouldAllBe(d => Enum.IsDefined(d.DeviceType));
    }

    [Test]
    public void initialize_devices_throws_argument_out_of_range_exception_when_device_count_is_zero() {
        Should.Throw<ArgumentOutOfRangeException>(() => Simulator.InitializeDevices(0));
    }

    [Test]
    public void initialize_devices_throws_argument_out_of_range_exception_when_device_count_is_negative() {
        Should.Throw<ArgumentOutOfRangeException>(() => Simulator.InitializeDevices(-5));
    }

    [Test]
    public void generate_batch_returns_specified_number_of_readings() {
        const int expectedCount = 50;

        var readings = Simulator.GenerateBatch(expectedCount);

        readings.Count.ShouldBe(expectedCount);
        foreach (var reading in readings) reading.ShouldBeValidSensorReading();
    }

    [Test]
    public void generate_batch_throws_argument_out_of_range_exception_when_count_is_zero() {
        Should.Throw<ArgumentOutOfRangeException>(() => Simulator.GenerateBatch(0));
    }

    [Test]
    public void generate_batch_throws_invalid_operation_exception_when_no_devices_initialized() {
        var emptySimulator = new HomeAutomationSimulator();

        Should.Throw<InvalidOperationException>(() => emptySimulator.GenerateBatch(10))
            .Message.ShouldContain("No devices initialized");
    }

    [Test]
    public void generate_time_series_produces_readings_at_specified_intervals() {
        var device        = Simulator.Devices[0];
        var duration      = TimeSpan.FromHours(2);
        var interval      = TimeSpan.FromMinutes(30);
        var expectedCount = (int)(duration.TotalMinutes / interval.TotalMinutes);

        var timeSeries = Simulator.GenerateTimeSeries(device.DeviceId, duration, interval).ToList();

        timeSeries.Count.ShouldBe(expectedCount);
        timeSeries.ShouldAllBe(r => r.DeviceId == device.DeviceId);

        for (var i = 1; i < timeSeries.Count; i++) {
            var timeDiff = timeSeries[i].Timestamp - timeSeries[i - 1].Timestamp;
            timeDiff.ShouldBe(interval);
        }
    }

    [Test]
    public void generate_time_series_throws_argument_exception_when_device_id_is_empty() {
        Should.Throw<ArgumentException>(() =>
            Simulator.GenerateTimeSeries("", TimeSpan.FromHours(1), TimeSpan.FromMinutes(5)).ToList()
        );
    }

    [Test]
    public void generate_time_series_throws_argument_exception_when_device_not_found() {
        Should.Throw<ArgumentException>(() =>
            Simulator.GenerateTimeSeries("NONEXISTENT_DEVICE", TimeSpan.FromHours(1), TimeSpan.FromMinutes(5)).ToList()
        );
    }

    [Test]
    public void simulate_device_failure_marks_device_as_offline() {
        var device           = Simulator.Devices.First(d => d.IsOnline);
        var originalDeviceId = device.DeviceId;

        var result = Simulator.SimulateDeviceFailure(originalDeviceId);

        result.ShouldBeSuccess();
        var updatedDevice = Simulator.Devices.First(d => d.DeviceId == originalDeviceId);
        updatedDevice.IsOnline.ShouldBeFalse();
        updatedDevice.BatteryLevel.ShouldBe(0);
    }

    [Test]
    public void simulate_device_failure_returns_failure_when_device_not_found() {
        var result = Simulator.SimulateDeviceFailure("NONEXISTENT_DEVICE");

        result.ShouldBeFailure("INVALID_INPUT");
    }

    [Test]
    public void restore_device_marks_device_as_online() {
        var device = Simulator.Devices[0];
        Simulator.SimulateDeviceFailure(device.DeviceId);

        var result = Simulator.RestoreDevice(device.DeviceId);

        result.ShouldBeSuccess();
        var restoredDevice = Simulator.Devices.First(d => d.DeviceId == device.DeviceId);
        restoredDevice.IsOnline.ShouldBeTrue();
        restoredDevice.BatteryLevel.ShouldBe(100);
        restoredDevice.LastSeen.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Test]
    public void reset_clears_all_devices_and_state() {
        Simulator.InitializeDevices(20);
        var originalDeviceCount = Simulator.DeviceCount;
        originalDeviceCount.ShouldBeGreaterThan(0);

        Simulator.Reset();

        Simulator.DeviceCount.ShouldBe(0);
        Simulator.Devices.ShouldBeEmpty();
    }
}

[Category("Unit")]
public class SensorReadingGenerationTests : SimulatorTestFixture {
    [Test]
    public void generates_realistic_temperature_readings_with_daily_patterns() {
        GenerateTestData(200);

        var temperatureReadings = TestReadings.Where(r => r.SensorType == SensorType.Temperature).ToList();

        temperatureReadings.ShouldNotBeEmpty();
        foreach (var reading in temperatureReadings) reading.ShouldHaveRealisticTemperatureValue();

        var distinctValues = temperatureReadings.Select(r => Math.Round(r.Value, 1)).Distinct().Count();
        distinctValues.ShouldBeGreaterThan(5);
    }

    [Test]
    public void generates_motion_readings_as_boolean_values() {
        GenerateTestData();

        var motionReadings = TestReadings.Where(r => r.SensorType == SensorType.Motion).ToList();

        motionReadings.ShouldNotBeEmpty();
        motionReadings.ShouldAllBe(r => r.Value == 0 || r.Value == 1);
        motionReadings.ShouldAllBe(r => r.Unit == MeasurementUnit.Boolean);
    }

    [Test]
    public void generates_readings_with_metadata_for_all_sensor_types() {
        GenerateTestData(50);

        var readingsWithMetadata = TestReadings.Where(r => r.Metadata != null && r.Metadata.Count > 0).ToList();
        readingsWithMetadata.ShouldNotBeEmpty();

        readingsWithMetadata.ShouldAllBe(r => r.Metadata!.ContainsKey("firmwareVersion"));
        readingsWithMetadata.ShouldAllBe(r => r.Metadata!.ContainsKey("signalStrength"));
    }

    [Test]
    public void generates_readings_with_valid_quality_values() {
        GenerateTestData();

        var qualityValues = TestReadings.Select(r => r.Quality).Distinct().ToList();

        var validQualities = Enum.GetValues<ReadingQuality>();
        qualityValues.ShouldAllBe(q => validQualities.Contains(q));
        qualityValues.ShouldContain(ReadingQuality.Good);
    }

    [Test]
    public void generates_readings_with_battery_levels_in_valid_range() {
        GenerateTestData();

        var readingsWithBattery = TestReadings.Where(r => r.BatteryLevel.HasValue).ToList();

        readingsWithBattery.ShouldNotBeEmpty();
        foreach (var reading in readingsWithBattery) reading.ShouldHaveValidBatteryLevel();
    }
}

[Category("Integration")]
public class AsyncGenerationTests : SimulatorTestFixture {
    [Test]
    [Property("Timeout", "10000")]
    public async Task generates_readings_asynchronously_with_cancellation() {
        var       receivedReadings   = new List<SensorReading>();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        try {
            await foreach (var reading in Simulator.GenerateReadingsAsync(
                               TimeSpan.FromMilliseconds(100),
                               cancellationSource.Token
                           )) {
                receivedReadings.Add(reading);

                if (receivedReadings.Count >= 10) break;
            }
        }
        catch (OperationCanceledException) { }

        receivedReadings.ShouldNotBeEmpty();
        foreach (var reading in receivedReadings) reading.ShouldBeValidSensorReading();
    }

    [Test]
    public async Task throws_invalid_operation_exception_when_no_devices_for_async_generation() {
        var       emptySimulator     = new HomeAutomationSimulator();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        await Should.ThrowAsync<InvalidOperationException>(async () => {
                await foreach (var reading in emptySimulator.GenerateReadingsAsync(TimeSpan.FromMilliseconds(100), cancellationSource.Token)) { }
            }
        );
    }
}

[Category("Unit")]
public class ExtensionMethodTests : SimulatorTestFixture {
    [Test]
    public void to_json_produces_valid_json_output() {
        var reading = GetRandomReading();

        var json = reading.ToJson();

        json.ShouldNotBeNullOrWhiteSpace();

        var parsed = JsonSerializer.Deserialize<SensorReading>(
            json, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        );

        parsed.DeviceId.ShouldBe(reading.DeviceId);
        parsed.SensorType.ShouldBe(reading.SensorType);
    }

    [Test]
    public void to_mqtt_payload_contains_required_mqtt_fields() {
        var reading = GetRandomReading();

        var mqttPayload = reading.ToMqttPayload();

        mqttPayload.ShouldNotBeNullOrWhiteSpace();
        mqttPayload.ShouldContain("deviceId");
        mqttPayload.ShouldContain("timestamp");
        mqttPayload.ShouldContain("measurements");
        mqttPayload.ShouldContain("battery");
        mqttPayload.ShouldContain("location");
    }

    [Test]
    public void to_telemetry_message_contains_message_id_and_event_type() {
        var reading = GetRandomReading();

        var telemetryMessage = reading.ToTelemetryMessage();

        telemetryMessage.ShouldNotBeNullOrWhiteSpace();
        telemetryMessage.ShouldContain("messageId");
        telemetryMessage.ShouldContain("eventType");
        telemetryMessage.ShouldContain("SensorReading");
    }
}

[Category("Unit")]
public class SensorDeviceRecordTests {
    [Test]
    public void sensor_device_update_battery_creates_new_instance_with_updated_values() {
        var originalDevice = new SensorDeviceBuilder()
            .WithDeviceId("TEST_001")
            .WithBatteryLevel(50.0)
            .Build();

        var updatedDevice = originalDevice.UpdateBattery(75.0);

        updatedDevice.BatteryLevel.ShouldBe(75.0);
        updatedDevice.DeviceId.ShouldBe(originalDevice.DeviceId);
        updatedDevice.LastSeen.ShouldBeGreaterThan(originalDevice.LastSeen);

        originalDevice.BatteryLevel.ShouldBe(50.0);
    }

    [Test]
    public void sensor_device_set_offline_updates_state_correctly() {
        var onlineDevice = new SensorDeviceBuilder()
            .WithDeviceId("TEST_002")
            .WithBatteryLevel(60.0)
            .Build();

        var offlineDevice = onlineDevice.SetOffline();

        offlineDevice.IsOnline.ShouldBeFalse();
        offlineDevice.BatteryLevel.ShouldBe(0);
        offlineDevice.LastSeen.ShouldBeGreaterThan(onlineDevice.LastSeen);

        onlineDevice.IsOnline.ShouldBeTrue();
        onlineDevice.BatteryLevel.ShouldBe(60.0);
    }

    [Test]
    public void sensor_device_set_online_restores_device_state() {
        var offlineDevice = new SensorDeviceBuilder()
            .WithDeviceId("TEST_003")
            .IsOffline()
            .Build();

        var onlineDevice = offlineDevice.SetOnline();

        onlineDevice.IsOnline.ShouldBeTrue();
        onlineDevice.BatteryLevel.ShouldBe(100);
        onlineDevice.LastSeen.ShouldBeGreaterThan(offlineDevice.LastSeen);

        offlineDevice.IsOnline.ShouldBeFalse();
        offlineDevice.BatteryLevel.ShouldBe(0);
    }
}

[Category("EdgeCases")]
public class EdgeCaseTests : SimulatorTestFixture {
    [Test]
    public void handles_time_series_with_very_small_intervals() {
        var device   = Simulator.Devices[0];
        var duration = TimeSpan.FromMinutes(1);
        var interval = TimeSpan.FromMilliseconds(100);

        var readings = Simulator.GenerateTimeSeries(device.DeviceId, duration, interval).ToList();

        readings.ShouldNotBeEmpty();
        readings.ShouldAllBe(r => r.DeviceId == device.DeviceId);

        readings.Count.ShouldBeGreaterThan(500);
    }

    [Test]
    public void handles_all_devices_offline_scenario() {
        var deviceIds = Simulator.Devices.Select(d => d.DeviceId).ToList();
        foreach (var deviceId in deviceIds) Simulator.SimulateDeviceFailure(deviceId);

        var readings = Simulator.GenerateBatch(10);

        readings.Count.ShouldBe(10);
        foreach (var reading in readings) reading.ShouldBeValidSensorReading();
    }

    [Test]
    public void generates_valid_results_for_multiple_simulators() {
        var simulator1 = new HomeAutomationSimulator();
        var simulator2 = new HomeAutomationSimulator();

        simulator1.InitializeDevices(10);
        simulator2.InitializeDevices(10);

        var readings1 = simulator1.GenerateBatch(20);
        var readings2 = simulator2.GenerateBatch(20);

        readings1.Count.ShouldBe(readings2.Count);

        // Note: Since we're using Random.Shared, results won't be deterministic anymore
        // This test verifies that both simulators can generate the same number of readings
        foreach (var reading in readings1) reading.ShouldBeValidSensorReading();
        foreach (var reading in readings2) reading.ShouldBeValidSensorReading();
    }
}
