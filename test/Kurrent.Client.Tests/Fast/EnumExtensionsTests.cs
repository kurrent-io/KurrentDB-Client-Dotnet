using System.ComponentModel;

namespace Kurrent.Client.Tests;

public class EnumExtensionsTests {
    [Test]
    public void returns_description_when_attribute_exists() {
        TestEnum.First.Description().ShouldBe("First Value");
        TestEnum.Third.Description().ShouldBe("Third Value");
    }

    [Test]
    public void returns_enum_name_when_description_attribute_missing() {
        TestEnum.Second.Description().ShouldBe("Second");
    }

    [Test]
    public void returns_cached_result_for_repeated_calls() {
        var firstCall  = TestEnum.First.Description();
        var secondCall = TestEnum.First.Description();

        firstCall.ShouldBe(secondCall);
        firstCall.ShouldBe("First Value");
    }

    [Test]
    public void handles_different_enum_types_correctly() {
        TestEnum.First.Description().ShouldBe("First Value");
        KurrentConnectionScheme.Direct.Description().ShouldBe("kurrent");
    }

    [Test]
    public void returns_correct_descriptions_for_connection_scheme_type() {
        KurrentConnectionScheme.Direct.Description().ShouldBe("kurrent");
        KurrentConnectionScheme.Discover.Description().ShouldBe("kurrent+discover");
    }

    // Test enums for verification
    enum TestEnum {
        [Description("First Value")] First,

        // No description attribute
        Second,
        [Description("Third Value")] Third
    }

}
