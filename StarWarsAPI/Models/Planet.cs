using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarWarsAPI.Models
{
    public class Planet
    {
        [Key]
        [JsonPropertyName("name")]
        public string Name { get; set;}
        [JsonPropertyName("diameter")]
        public string Diameter { get; set; }
        [JsonPropertyName("rotation_period")]
        public string Rotation_period { get; set; }
        [JsonPropertyName("orbital_period")]
        public string Orbital_period { get; set; }
        [JsonPropertyName("gravity")]
        public string Gravity { get; set; }
        [JsonPropertyName("population")]
        public string Population { get; set; }
        [JsonPropertyName("climate")]
        public string Climate { get; set; }
        [JsonPropertyName("terrain")]
        public string Terrain { get; set; }
        [JsonPropertyName("surface_water")]
        public string Surface_water { get; set; }
        [JsonPropertyName("residents")]
        [NotMapped] // not sure how to handle in in-memory database
        public string[] Residents { get; set; }
        [JsonPropertyName("films")]
        [NotMapped] // not sure how to handle in in-memory database
        public string[] Films { get; set; }
        [JsonPropertyName("created")]
        public string Created { get; set; }
        [JsonPropertyName("edited")]
        public string Edited { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }

        public static Planet FromJson(string json) => JsonSerializer.Deserialize<Planet>(json);

        public static string ToJson(Planet self) => JsonSerializer.Serialize(self);

        public Planet() { }

        public Planet(
            string name, 
            string diameter, 
            string rotationPeriod, 
            string orbitalPeriod, 
            string gravity, 
            string population, 
            string climate, 
            string terrain, 
            string surfaceWater, 
            string[] residents, 
            string[] films, 
            string created, 
            string edited, 
            string url)
        {
            Name = name;
            Diameter = diameter;
            Rotation_period = rotationPeriod;
            Orbital_period = orbitalPeriod;
            Gravity = gravity;
            Population = population;
            Climate = climate;
            Terrain = terrain;
            Surface_water = surfaceWater;
            Residents = residents;
            Films = films;
            Created = created;
            Edited = edited;
            Url = url;
        }
    }
}
