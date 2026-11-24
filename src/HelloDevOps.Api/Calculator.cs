namespace HelloDevOps.Api;

/// <summary>
/// Tiny piece of "business logic" so we have something real to unit test.
/// </summary>
public static class Calculator
{
    public static int Add(int a, int b) => a + b;

    public static int Subtract(int a, int b) => a - b;

    public static int Multiply(int a, int b) => a * b;
}
