using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StarWarsAPI;
using StarWarsAPI.Models;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<StarWarsContext>(opt => opt.UseInMemoryDatabase("StarWars"));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseSwagger();

        app.UseEndpoints(endpoints => 
        {
            endpoints.MapGet("api/planets", async context =>
                {
                    // call SWAPI to get planets
                    using var httpClient = new HttpClient();
                    try
                    {
                        var response = await httpClient.GetStringAsync("https://swapi.dev/api/planets");
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(response);
                    }
                    catch (HttpRequestException e)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Error fetching data: {e.Message}");
                    }
                }

                ).WithName("GetPlanets");

            endpoints.MapPost("api/favourite", async context =>
                {
                    try 
                    {
                        // read planet details from request body
                        var requestBodyStream = new MemoryStream();
                        await context.Request.Body.CopyToAsync(requestBodyStream);
                        requestBodyStream.Seek(0, SeekOrigin.Begin);
                        var planet = await JsonSerializer.DeserializeAsync<Planet>(requestBodyStream);

                        // check if planet already exists in database
                        var dbContext = context.RequestServices.GetService<StarWarsContext>();
                        var existingPlanet = await dbContext.Planets.FindAsync(planet.Name);
                        if (existingPlanet != null)
                        {
                            context.Response.StatusCode = 409;
                            await context.Response.WriteAsync($"Planet {planet.Name} already exists in database");
                            return;
                        }


                        // add planet to database
                        await dbContext.Planets.AddAsync(planet);
                        await dbContext.SaveChangesAsync();
                        // return 201 Created with details of the planet
                        context.Response.StatusCode = 201;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync($"Planet {planet.Name} added to database" + JsonSerializer.Serialize(planet));

                        //await context.Response.WriteAsync($"Planet {planet.Name} added to database");
                        
                    }
                    catch (HttpRequestException e)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Error fetching data: {e.Message}");
                    }
                }

                ).WithName("AddFavouritePlanet");

                endpoints.MapGet("api/favourite", async context =>
                {
                    try
                    {
                        // get all planets from database
                        var dbContext = context.RequestServices.GetService<StarWarsContext>();
                        var planets = await dbContext.Planets.ToListAsync();
                        // return planets
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(planets));
                    }
                    catch (HttpRequestException e)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Error fetching data: {e.Message}");
                    }
                }

                ).WithName("GetFavouritePlanets");

                endpoints.MapDelete("api/favourite/{name}", async context =>
                {
                    try
                    {
                        // get planet from database
                        var dbContext = context.RequestServices.GetService<StarWarsContext>();
                        var planet = await dbContext.Planets.FindAsync(context.Request.RouteValues["name"]);
                        if (planet == null)
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync($"Planet {context.Request.RouteValues["name"]} not found in database");
                            return;
                        }
                        // remove planet from database
                        dbContext.Planets.Remove(planet);
                        await dbContext.SaveChangesAsync();
                        // return 204 No Content
                        context.Response.StatusCode = 204;
                        await context.Response.WriteAsync($"Planet {planet.Name} removed from database");
                    }
                    catch (HttpRequestException e)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Error fetching data: {e.Message}");
                    }
                }

                ).WithName("RemoveFavouritePlanet");

                endpoints.MapGet("api/random", async context =>
                {
                    try
                    {
                        var random = new Random();
                        Planet newPlanet = null;
                        var maxPlanetId = 60;

                        while (newPlanet == null)
                        {
                            var randomId = random.Next(1, maxPlanetId);

                            // call SWAPI to get planet with random id
                            using var httpClient = new HttpClient();
                            var response = await httpClient.GetStringAsync($"https://swapi.dev/api/planets/{randomId}");
                            if (response.Contains("Not found"))
                            {
                                continue;
                            }
                            var planet = JsonSerializer.Deserialize<Planet>(response);

                            // check if planet already exists in database
                            var dbContext = context.RequestServices.GetService<StarWarsContext>();
                            var existingPlanet = await dbContext.Planets.FindAsync(planet.Name);
                            if (existingPlanet == null)
                            {
                                newPlanet = planet;
                            }
                        }

                        // return planet with 200 OK
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(newPlanet));
                    }
                    catch (HttpRequestException e)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Error fetching data: {e.Message}");
                    }
                }

                ).WithName("GetRandomPlanet");
        }
        );
    }
}