namespace Kurrent.Client.Testing.TUnit;

public static class TestTimeouts {
    public class OneSecondAttribute() : TimeoutAttribute(1000);

    public class FiveSecondsAttribute() : TimeoutAttribute(5000);

    public class TenSecondsAttribute() : TimeoutAttribute(10000);
}
