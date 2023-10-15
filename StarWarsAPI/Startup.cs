using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StarWarsAPI;
using StarWarsAPI.Models;

public class Startup
{
    public IConfiguration Configuration { get; }
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<StarWarsContext>(opt => opt.UseInMemoryDatabase("StarWars"));
        services.AddSingleton<ISet<string>>(provider => new HashSet<string>()); // for storing attempted planet calls in random endpoint
        services.AddHttpClient("swapi", c =>
        {
            c.BaseAddress = new Uri("https://swapi.dev/api/");
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseExceptionHandler(a => a.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature.Error;

            var logger = context.RequestServices.GetService<ILogger<Startup>>();
            logger.LogError(exception, "An error occurred while processing the request.");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = exception.Message }));
        }));

        app.UseEndpoints(endpoints => 
        {
            endpoints.MapGet("api/planets", async context =>
                {
                    var HttpClientFactory = context.RequestServices.GetService<IHttpClientFactory>();
                    var httpClient = HttpClientFactory.CreateClient("swapi");
                    var logger = context.RequestServices.GetService<ILogger<Startup>>();

                    var pageQuery = context.Request.Query["page"];
                    var requestUri = string.IsNullOrEmpty(pageQuery) ? "planets" : $"planets?page={pageQuery}";

                    try
                    {
                        var response = await httpClient.GetAsync(requestUri);

                        if (!response.IsSuccessStatusCode)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                            await context.Response.WriteAsJsonAsync(new { error = $"Error fetching data: {response.ReasonPhrase}" });
                            return;
                        }

                        var content = await response.Content.ReadAsStringAsync();

                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(content);

                    }
                    catch (HttpRequestException e)
                    {
                        logger.LogError(e, "An error occurred while fetching data from SWAPI.");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred. Please try again later." });
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "An error occurred while processing the request.");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred. Please try again later." });
                    }
                }).WithName("GetPlanets");

            endpoints.MapPost("api/favourite", async context =>
                {
                    var dbContext = context.RequestServices.GetRequiredService<StarWarsContext>();
                    var logger = context.RequestServices.GetRequiredService<ILogger<Startup>>();

                    try
                    {
                        var planet = await JsonSerializer.DeserializeAsync<Planet>(context.Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (planet == null || string.IsNullOrWhiteSpace(planet.Name))
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            await context.Response.WriteAsJsonAsync(new { error = "Invalid planet name provided" });
                            return;
                        }

                        // check if planet already exists in database (case insensitive)
                        var existingPlanet = await dbContext.Planets.FirstOrDefaultAsync(p => p.Name.ToLower() == planet.Name.ToLower());
                        if (existingPlanet != null)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                            await context.Response.WriteAsJsonAsync(new { error = $"Planet {planet.Name} already exists in database" });
                            return;
                        }

                        await dbContext.Planets.AddAsync(planet);
                        await dbContext.SaveChangesAsync();

                        context.Response.StatusCode = (int)HttpStatusCode.Created;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(planet);
                        
                    }
                    catch (JsonException e)
                    {
                        logger.LogError(e, "An error occurred while deserializing planet data.");
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid planet data provided" });
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "An error occurred while processing the request.");
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred. Please try again later." });
                    }
                }).WithName("AddFavouritePlanet");

                endpoints.MapGet("api/favourite", async context =>
                {
                    
                    var dbContext = context.RequestServices.GetService<StarWarsContext>();
                    var logger = context.RequestServices.GetService<ILogger<Startup>>();

                    try
                    {
                        var planets = await dbContext.Planets.ToListAsync();
                        if (planets == null || planets.Count == 0)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            await context.Response.WriteAsJsonAsync(new { error = "No planets found in database" });
                            return;
                        }

                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(planets);

                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "An error occurred while processing the request.");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred. Please try again later." });
                    }
                }).WithName("GetFavouritePlanets");

                endpoints.MapDelete("api/favourite/{name}", async context =>
                {

                    var dbContext = context.RequestServices.GetService<StarWarsContext>();
                    var logger = context.RequestServices.GetService<ILogger<Startup>>();
                    var planetName = context.Request.RouteValues["name"] as string;
                    // change to uppercase to match database
                    if (string.IsNullOrWhiteSpace(planetName))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid planet name provided" });
                        return;
                    }
                    planetName =  planetName.Substring(0, 1).ToUpper() + planetName.Substring(1).ToLower(); // change to capitalised to match database

                    try
                    {
                        var planet = await dbContext.Planets.FindAsync(planetName); // find planet by name (case insensitive)
                        if (planet == null)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            await context.Response.WriteAsJsonAsync(new { error = $"Planet {planetName} not found in database" });
                            return;
                        }

                        // remove planet from database
                        dbContext.Planets.Remove(planet);
                        await dbContext.SaveChangesAsync();
                        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "An error occurred while processing the request.");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred. Please try again later." });
                    }
                }).WithName("RemoveFavouritePlanet");

                endpoints.MapGet("api/random", async context =>
                {
                    var HttpClientFactory = context.RequestServices.GetService<IHttpClientFactory>();
                    var httpClient = HttpClientFactory.CreateClient("swapi");
                    var retrievedPlanets = context.RequestServices.GetService<ISet<string>>();
                    var dbContext = context.RequestServices.GetService<StarWarsContext>();
                    var logger = context.RequestServices.GetService<ILogger<Startup>>();
                    try
                    {
                        
                        // get count of planets from SWAPI
                        var countResponse = await httpClient.GetAsync("planets");
                        if(!countResponse.IsSuccessStatusCode)
                        {
                            logger.LogError($"Error fetching data: {countResponse.ReasonPhrase}");
                            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                            await context.Response.WriteAsJsonAsync(new { error = $"Error fetching data: {countResponse.ReasonPhrase}" });
                            return;
                        }

                        var countContent = await countResponse.Content.ReadAsStringAsync();
                        var planetCount = JsonSerializer.Deserialize<JsonElement>(countContent).GetProperty("count").GetInt32();
                        var random = new Random();
                        Planet newPlanet = null;
                        
                        // get random planet from SWAPI
                        for (int i = 0; i < planetCount; i++)
                        {
                            var randomId = random.Next(1, planetCount);
                            var response = await httpClient.GetAsync($"planets/{randomId}");

                            if (response.IsSuccessStatusCode)
                            {
                                var content = await response.Content.ReadAsStringAsync();
                                newPlanet = Planet.FromJson(content);
                                if (!retrievedPlanets.Contains(newPlanet.Name)) // check if planet has already been retrieved
                                {
                                    retrievedPlanets.Add(newPlanet.Name);
                                    break;
                                } else if (!dbContext.Planets.Any(p => p.Name == newPlanet.Name))
                                {
                                    break; // Exit the loop once we find a new planet
                                }

                            } 
                            else if (response.StatusCode != HttpStatusCode.NotFound)
                            {
                                logger.LogError($"Error fetching planet with ID {randomId} : {response.StatusCode}");
                                context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                                break;
                                
                            }
                        }

                        if (newPlanet == null)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            await context.Response.WriteAsJsonAsync(new { error = "No planets found" });
                            return;
                        }

                        // return planet with 200 OK
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(newPlanet);

                    }
                    catch (HttpRequestException e)
                    {
                        logger.LogError(e, "An error occurred while fetching data from SWAPI.");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsync($"Error fetching data: {e.Message}");
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "An error occurred while processing the request.");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred. Please try again later." });
                    }
                }).WithName("GetRandomPlanet");
        }
        );
    }
}