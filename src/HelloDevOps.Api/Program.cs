using HelloDevOps.Api;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello, DevOps!");

// Used by the container smoke test in CI (and later by any hosting platform).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/add/{a:int}/{b:int}", (int a, int b) =>
    Results.Ok(new { a, b, result = Calculator.Add(a, b) }));

app.MapGet("/subtract/{a:int}/{b:int}", (int a, int b) =>
    Results.Ok(new { a, b, result = Calculator.Subtract(a, b) }));

app.MapGet("/multiply/{a:int}/{b:int}", (int a, int b) =>
    Results.Ok(new { a, b, result = Calculator.Multiply(a, b) }));

app.Run();
