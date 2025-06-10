// For Guid, InvalidOperationException

namespace Kurrent.Whatever.Tests;

// Define a test type that implements IWhatever directly
// The source generator will generate the full implementation.
public partial class TestWhateverResult : IWhatever<string, int> {
    // User-defined part is empty or can contain other non-generated members.
}

public partial class TestWhateverComplexResult : IWhatever<MyError, MySuccess, MyWarning> {
    // Generator will handle this
}

public record MyError(string Code, string Message);

public record MySuccess(Guid Id);

public record MyWarning(string Text);

public class WhateverGenerationTests {
    [Test]
    public void is_string_should_be_true_when_value_is_string() {
        TestWhateverResult result = "hello";
        result.IsString.ShouldBeTrue();
        result.IsInt.ShouldBeFalse();
    }

    [Test]
    public void as_string_should_return_string_value_when_value_is_string() {
        TestWhateverResult result = "hello";
        result.AsString.ShouldBe("hello");
    }

    [Test]
    public void as_string_should_throw_when_value_is_not_string() {
        TestWhateverResult result = 123; // int value
        Should.Throw<InvalidOperationException>(() => result.AsString);
    }

    [Test]
    public void is_int_should_be_true_when_value_is_int() {
        TestWhateverResult result = 123;
        result.IsInt.ShouldBeTrue();
        result.IsString.ShouldBeFalse();
    }

    [Test]
    public void as_int_should_return_int_value_when_value_is_int() {
        TestWhateverResult result = 123;
        result.AsInt.ShouldBe(123);
    }

    [Test]
    public void as_int_should_throw_when_value_is_not_int() {
        TestWhateverResult result = "not an int";
        Should.Throw<InvalidOperationException>(() => result.AsInt);
    }

    [Test]
    public void value_property_should_return_correct_object() {
        TestWhateverResult resultString = "test";
        resultString.Value.ShouldBe("test");
        resultString.Value.ShouldBeOfType<string>();

        TestWhateverResult resultInt = 42;
        resultInt.Value.ShouldBe(42);
        resultInt.Value.ShouldBeOfType<int>();
    }

    [Test]
    public void index_property_should_return_correct_index() {
        TestWhateverResult resultString = "test";
        resultString.Index.ShouldBe(0);

        TestWhateverResult resultInt = 42;
        resultInt.Index.ShouldBe(1);
    }

    [Test]
    public void to_string_should_return_underlying_value_to_string() {
        TestWhateverResult resultString = "test";
        resultString.ToString().ShouldBe("test");

        TestWhateverResult resultInt = 42;
        resultInt.ToString().ShouldBe("42");
    }

    [Test]
    public void equals_should_return_true_for_same_value_and_type() {
        TestWhateverResult result1A = "hello";
        TestWhateverResult result1B = "hello";
        result1A.Equals(result1B).ShouldBeTrue();
    }

    [Test]
    public void equals_should_return_false_for_different_value_same_type() {
        TestWhateverResult result1A = "hello";
        TestWhateverResult result1B = "world";
        result1A.Equals(result1B).ShouldBeFalse();
    }

    [Test]
    public void equals_should_return_false_for_different_type() {
        TestWhateverResult result1A = "hello";
        TestWhateverResult result2  = 123;
        result1A.Equals(result2).ShouldBeFalse();
    }

    [Test]
    public void equals_should_return_false_for_null() {
        TestWhateverResult result1A = "hello";
        result1A.Equals(null).ShouldBeFalse();
    }

    [Test]
    public void equals_operator_should_work_correctly() {
        TestWhateverResult  result1A   = "hello";
        TestWhateverResult  result1B   = "hello";
        TestWhateverResult  result2    = "world";
        TestWhateverResult  result3    = 123;
        TestWhateverResult? nullResult = null;

        (result1A == result1B).ShouldBeTrue();
        (result1A == result2).ShouldBeFalse();
        (result1A == result3).ShouldBeFalse();
        (result1A == nullResult).ShouldBeFalse();
        (nullResult == result1A).ShouldBeFalse();
        (nullResult == nullResult).ShouldBeTrue();

        TestWhateverResult resultInt1 = 10;
        TestWhateverResult resultInt2 = 10;
        (resultInt1 == resultInt2).ShouldBeTrue();
    }

    [Test]
    public void not_equals_operator_should_work_correctly() {
        TestWhateverResult  result1A   = "hello";
        TestWhateverResult  result1B   = "hello";
        TestWhateverResult  result2    = "world";
        TestWhateverResult? nullResult = null;

        (result1A != result1B).ShouldBeFalse();
        (result1A != result2).ShouldBeTrue();
        (result1A != nullResult).ShouldBeTrue();
        (nullResult != result1A).ShouldBeTrue();
        (nullResult != nullResult).ShouldBeFalse();
    }

    [Test]
    public void get_hash_code_should_be_consistent_for_equal_objects() {
        TestWhateverResult result1A = "hello";
        TestWhateverResult result1B = "hello";
        result1A.GetHashCode().ShouldBe(result1B.GetHashCode());

        TestWhateverResult resultInt1 = 123;
        TestWhateverResult resultInt2 = 123;
        resultInt1.GetHashCode().ShouldBe(resultInt2.GetHashCode());
    }

    [Test]
    public void get_hash_code_should_generally_be_different_for_unequal_objects() {
        TestWhateverResult result1 = "hello";
        TestWhateverResult result2 = "world";
        TestWhateverResult result3 = 123;

        // Note: Hash code collisions are possible but should be rare for distinct small inputs.
        result1.GetHashCode().ShouldNotBe(result2.GetHashCode());
        result1.GetHashCode().ShouldNotBe(result3.GetHashCode());
    }

    [Test]
    public void implicit_conversion_from_string_should_work() {
        TestWhateverResult result = "implicit";
        result.IsString.ShouldBeTrue();
        result.AsString.ShouldBe("implicit");
    }

    [Test]
    public void implicit_conversion_from_int_should_work() {
        TestWhateverResult result = 789;
        result.IsInt.ShouldBeTrue();
        result.AsInt.ShouldBe(789);
    }

    [Test]
    public void explicit_conversion_to_string_should_work() {
        TestWhateverResult result   = "explicit_cast";
        var                strValue = (string?)result;
        strValue.ShouldBe("explicit_cast");

        TestWhateverResult intResult    = 100;
        var                nullStrValue = (string?)intResult;
        nullStrValue.ShouldBeNull();
    }

    [Test]
    public void explicit_conversion_to_int_should_work() {
        TestWhateverResult result   = 99;
        var                intValue = (int?)result;
        intValue.ShouldBe(99);

        TestWhateverResult stringResult = "not_an_int";
        var                nullIntValue = (int?)stringResult;
        nullIntValue.ShouldBeNull();
    }

    [Test]
    public void switch_should_execute_correct_action_for_string() {
        TestWhateverResult result             = "switch_me";
        var                stringActionCalled = false;
        var                intActionCalled    = false;

        result.Switch(
            s => stringActionCalled = true,
            i => intActionCalled    = true
        );

        stringActionCalled.ShouldBeTrue();
        intActionCalled.ShouldBeFalse();
    }

    [Test]
    public void switch_should_execute_correct_action_for_int() {
        TestWhateverResult result             = 555;
        var                stringActionCalled = false;
        var                intActionCalled    = false;

        result.Switch(
            s => stringActionCalled = true,
            i => intActionCalled    = true
        );

        stringActionCalled.ShouldBeFalse();
        intActionCalled.ShouldBeTrue();
    }

    [Test]
    public void match_should_return_correct_value_for_string() {
        TestWhateverResult result = "match_me";
        var outcome = result.Match(
            s => $"String: {s}",
            i => $"Int: {i}"
        );

        outcome.ShouldBe("String: match_me");
    }

    [Test]
    public void match_should_return_correct_value_for_int() {
        TestWhateverResult result = 777;
        var outcome = result.Match(
            s => $"String: {s}",
            i => $"Int: {i}"
        );

        outcome.ShouldBe("Int: 777");
    }

    [Test]
    public void try_pick_string_should_work() {
        TestWhateverResult result = "pick_string";

        result.TryPickString(out var pickedString).ShouldBeTrue();
        pickedString.ShouldBe("pick_string");

        TestWhateverResult intResult = 123;
        intResult.TryPickString(out pickedString).ShouldBeFalse();
        pickedString.ShouldBeNull();
    }

    [Test]
    public void try_pick_int_should_work() {
        TestWhateverResult result = 987;

        result.TryPickInt(out var pickedInt).ShouldBeTrue();
        pickedInt.ShouldBe(987);

        TestWhateverResult stringResult = "not_an_int";
        stringResult.TryPickInt(out pickedInt).ShouldBeFalse();
        pickedInt.ShouldBe(default(int));
    }

    [Test]
    public void complex_type_generation_is_my_error_should_be_true() {
        var                       error  = new MyError("E01", "Test Error");
        TestWhateverComplexResult result = error;

        result.IsMyError.ShouldBeTrue();
        result.IsMySuccess.ShouldBeFalse();
        result.IsMyWarning.ShouldBeFalse();
        result.AsMyError.ShouldBe(error);
    }

    [Test]
    public void complex_type_generation_is_my_success_should_be_true() {
        var                       success = new MySuccess(Guid.NewGuid());
        TestWhateverComplexResult result  = success;

        result.IsMyError.ShouldBeFalse();
        result.IsMySuccess.ShouldBeTrue();
        result.IsMyWarning.ShouldBeFalse();
        result.AsMySuccess.ShouldBe(success);
    }

    [Test]
    public void constructor_should_throw_argument_null_exception_for_null_reference_type() {
        Should.Throw<ArgumentNullException>(() => new TestWhateverResult((string)null!));
    }

    [Test]
    public void constructor_should_not_throw_for_value_type_default() {
        // This test is tricky because int default is 0, which is valid.
        // The generator's null check is `if (parameterName == null)`, which doesn't apply to non-nullable value types.
        // So, this test effectively checks that the constructor for int doesn't throw.
        Should.NotThrow(() => new TestWhateverResult(0));
        Should.NotThrow(() => new TestWhateverResult(default(int)));
    }
}
