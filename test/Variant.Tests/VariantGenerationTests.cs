namespace Kurrent.Variant.Tests;

// Define a test type that implements IVariant directly
// The source generator will generate the full implementation.
public readonly partial record struct TestVariantResult : IVariant<string, int> {
    // User-defined part is empty or can contain other non-generated members.
}

public readonly partial record struct TestVariantComplexResult : IVariant<MyError, MySuccess, MyWarning> {
    // Generator will handle this
}

public record MyError(string Code, string Message);

public record MySuccess(Guid Id);

public record MyWarning(string Text);

public class VariantGenerationTests {
    [Test]
    public void is_string_should_be_true_when_value_is_string() {
        TestVariantResult result = "hello";
        result.IsString.ShouldBeTrue();
        result.IsInt.ShouldBeFalse();
    }

    [Test]
    public void as_string_should_return_string_value_when_value_is_string() {
        TestVariantResult result = "hello";
        result.AsString.ShouldBe("hello");
    }

    [Test]
    public void as_string_should_throw_when_value_is_not_string() {
        TestVariantResult result = 123; // int value
        Should.Throw<InvalidOperationException>(() => result.AsString);
    }

    [Test]
    public void is_int_should_be_true_when_value_is_int() {
        TestVariantResult result = 123;
        result.IsInt.ShouldBeTrue();
        result.IsString.ShouldBeFalse();
    }

    [Test]
    public void as_int_should_return_int_value_when_value_is_int() {
        TestVariantResult result = 123;
        result.AsInt.ShouldBe(123);
    }

    [Test]
    public void as_int_should_throw_when_value_is_not_int() {
        TestVariantResult result = "not an int";
        Should.Throw<InvalidOperationException>(() => result.AsInt);
    }

    [Test]
    public void value_property_should_return_correct_object() {
        TestVariantResult resultString = "test";
        resultString.Value.ShouldBe("test");
        resultString.Value.ShouldBeOfType<string>();

        TestVariantResult resultInt = 42;
        resultInt.Value.ShouldBe(42);
        resultInt.Value.ShouldBeOfType<int>();
    }

    [Test]
    public void index_property_should_return_correct_index() {
        TestVariantResult resultString = "test";
        resultString.Index.ShouldBe(0);

        TestVariantResult resultInt = 42;
        resultInt.Index.ShouldBe(1);
    }

    [Test]
    public void to_string_should_return_underlying_value_to_string() {
        TestVariantResult resultString = "test";
        resultString.ToString().ShouldBe("test");

        TestVariantResult resultInt = 42;
        resultInt.ToString().ShouldBe("42");
    }

    [Test]
    public void equals_should_return_true_for_same_value_and_type() {
        TestVariantResult result1A = "hello";
        TestVariantResult result1B = "hello";
        result1A.Equals(result1B).ShouldBeTrue();
    }

    [Test]
    public void equals_should_return_false_for_different_value_same_type() {
        TestVariantResult result1A = "hello";
        TestVariantResult result1B = "world";
        result1A.Equals(result1B).ShouldBeFalse();
    }

    [Test]
    public void equals_should_return_false_for_different_type() {
        TestVariantResult result1A = "hello";
        TestVariantResult result2  = 123;
        result1A.Equals(result2).ShouldBeFalse();
    }

    [Test]
    public void equals_operator_should_work_correctly() {
        TestVariantResult  result1A   = "hello";
        TestVariantResult  result1B   = "hello";
        TestVariantResult  result2    = "world";
        TestVariantResult  result3    = 123;
        TestVariantResult? nullResult = null;

        (result1A == result1B).ShouldBeTrue();
        (result1A == result2).ShouldBeFalse();
        (result1A == result3).ShouldBeFalse();
        (result1A == nullResult).ShouldBeFalse();
        (nullResult == result1A).ShouldBeFalse();

        TestVariantResult resultInt1 = 10;
        TestVariantResult resultInt2 = 10;
        (resultInt1 == resultInt2).ShouldBeTrue();
    }

    [Test]
    public void not_equals_operator_should_work_correctly() {
        TestVariantResult  result1A   = "hello";
        TestVariantResult  result1B   = "hello";
        TestVariantResult  result2    = "world";
        TestVariantResult? nullResult = null;

        (result1A != result1B).ShouldBeFalse();
        (result1A != result2).ShouldBeTrue();
        (result1A != nullResult).ShouldBeTrue();
        (nullResult != result1A).ShouldBeTrue();
        (nullResult != nullResult).ShouldBeFalse();
    }

    [Test]
    public void get_hash_code_should_be_consistent_for_equal_objects() {
        TestVariantResult result1A = "hello";
        TestVariantResult result1B = "hello";
        result1A.GetHashCode().ShouldBe(result1B.GetHashCode());

        TestVariantResult resultInt1 = 123;
        TestVariantResult resultInt2 = 123;
        resultInt1.GetHashCode().ShouldBe(resultInt2.GetHashCode());
    }

    [Test]
    public void get_hash_code_should_generally_be_different_for_unequal_objects() {
        TestVariantResult result1 = "hello";
        TestVariantResult result2 = "world";
        TestVariantResult result3 = 123;

        // Note: Hash code collisions are possible but should be rare for distinct small inputs.
        result1.GetHashCode().ShouldNotBe(result2.GetHashCode());
        result1.GetHashCode().ShouldNotBe(result3.GetHashCode());
    }

    [Test]
    public void implicit_conversion_from_string_should_work() {
        TestVariantResult result = "implicit";
        result.IsString.ShouldBeTrue();
        result.AsString.ShouldBe("implicit");
    }

    [Test]
    public void implicit_conversion_from_int_should_work() {
        TestVariantResult result = 789;
        result.IsInt.ShouldBeTrue();
        result.AsInt.ShouldBe(789);
    }


    [Test]
    public void switch_should_execute_correct_action_for_string() {
        TestVariantResult result             = "switch_me";
        var                stringActionCalled = false;
        var                intActionCalled    = false;

        result.Switch(
            _ => stringActionCalled = true,
            _ => intActionCalled    = true
        );

        stringActionCalled.ShouldBeTrue();
        intActionCalled.ShouldBeFalse();
    }

    [Test]
    public void switch_should_execute_correct_action_for_int() {
        TestVariantResult result             = 555;
        var                stringActionCalled = false;
        var                intActionCalled    = false;

        result.Switch(
            _ => stringActionCalled = true,
            _ => intActionCalled    = true
        );

        stringActionCalled.ShouldBeFalse();
        intActionCalled.ShouldBeTrue();
    }

    [Test]
    public void match_should_return_correct_value_for_string() {
        TestVariantResult result = "match_me";
        var outcome = result.Match(
            s => $"String: {s}",
            i => $"Int: {i}"
        );

        outcome.ShouldBe("String: match_me");
    }

    [Test]
    public void match_should_return_correct_value_for_int() {
        TestVariantResult result = 777;
        var outcome = result.Match(
            s => $"String: {s}",
            i => $"Int: {i}"
        );

        outcome.ShouldBe("Int: 777");
    }

    // [Test]
    // public void try_pick_string_should_work() {
    //     TestVariantResult result = "pick_string";
    //
    //     result.TryPickString(out var pickedString).ShouldBeTrue();
    //     pickedString.ShouldBe("pick_string");
    //
    //     TestVariantResult intResult = 123;
    //     intResult.TryPickString(out pickedString).ShouldBeFalse();
    //     pickedString.ShouldBeNull();
    // }
    //
    // [Test]
    // public void try_pick_int_should_work() {
    //     TestVariantResult result = 987;
    //
    //     result.TryPickInt(out var pickedInt).ShouldBeTrue();
    //     pickedInt.ShouldBe(987);
    //
    //     TestVariantResult stringResult = "not_an_int";
    //     stringResult.TryPickInt(out pickedInt).ShouldBeFalse();
    //     pickedInt.ShouldBe(0);
    // }

    [Test]
    public void complex_type_generation_is_my_error_should_be_true() {
        var                       error  = new MyError("E01", "Test Error");
        TestVariantComplexResult result = error;

        result.IsMyError.ShouldBeTrue();
        result.IsMySuccess.ShouldBeFalse();
        result.IsMyWarning.ShouldBeFalse();
        result.AsMyError.ShouldBe(error);
    }

    [Test]
    public void complex_type_generation_is_my_success_should_be_true() {
        var                       success = new MySuccess(Guid.NewGuid());
        TestVariantComplexResult result  = success;

        result.IsMyError.ShouldBeFalse();
        result.IsMySuccess.ShouldBeTrue();
        result.IsMyWarning.ShouldBeFalse();
        result.AsMySuccess.ShouldBe(success);
    }

    [Test]
    public void constructor_should_throw_argument_null_exception_for_null_reference_type() {
        Should.Throw<ArgumentNullException>(() => new TestVariantResult(null!));
    }

    [Test]
    public void constructor_should_not_throw_for_value_type_default() {
        // This test is tricky because int default is 0, which is valid.
        // The generator's null check is `if (parameterName == null)`, which doesn't apply to non-nullable value types.
        // So, this test effectively checks that the constructor for int doesn't throw.
        Should.NotThrow(() => new TestVariantResult(0));
        Should.NotThrow(() => new TestVariantResult(0));
    }

    // Case enum tests
    [Test]
    public void case_property_should_return_correct_enum_value_for_string() {
        TestVariantResult result = "test";
        result.Case.ShouldBe(TestVariantResult.TestVariantResultCase.String);
    }

    [Test]
    public void case_property_should_return_correct_enum_value_for_int() {
        TestVariantResult result = 123;
        result.Case.ShouldBe(TestVariantResult.TestVariantResultCase.Int);
    }

    // Async Switch tests
    [Test]
    public async Task switch_async_should_execute_correct_action_for_string() {
        TestVariantResult result = "async_test";
        var stringActionCalled = false;
        var intActionCalled = false;

        await result.SwitchAsync(
            async s => { stringActionCalled = true; await ValueTask.CompletedTask; },
            async i => { intActionCalled = true; await ValueTask.CompletedTask; }
        );

        stringActionCalled.ShouldBeTrue();
        intActionCalled.ShouldBeFalse();
    }

    // Async Match tests
    [Test]
    public async Task match_async_should_return_correct_value_for_string() {
        TestVariantResult result = "async_match";
        var outcome = await result.MatchAsync(
            async s => { await ValueTask.CompletedTask; return $"String: {s}"; },
            async i => { await ValueTask.CompletedTask; return $"Int: {i}"; }
        );

        outcome.ShouldBe("String: async_match");
    }

    // State parameter tests
    [Test]
    public void switch_with_state_should_pass_state_correctly() {
        TestVariantResult result = "state_test";
        var capturedState = "";
        var state = "test_state";

        result.Switch<string>(
            (s, st) => capturedState = st,
            (i, st) => capturedState = "wrong",
            state
        );

        capturedState.ShouldBe("test_state");
    }
}
