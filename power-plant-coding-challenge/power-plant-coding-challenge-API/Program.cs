using FluentValidation;
using MediatR;
using power_plant_coding_challenge_API.Middlewares;
using power_plant_coding_challenge_core.Features.CalculateProductionPlan;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
{
    cfg.LicenseKey = builder.Configuration["MediatR:LicenseKey"];
    cfg.RegisterServicesFromAssemblies(typeof(CalculateProductionPlan.Handler).Assembly);
});

builder.Services.AddValidatorsFromAssemblyContaining<CalculateProductionPlan.CommandValidator>();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

app.UseMiddleware<CustomExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
