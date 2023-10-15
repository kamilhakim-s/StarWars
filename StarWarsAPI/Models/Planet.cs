using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarWarsAPI.Models
{
    public class Planet
    {
        [Key]
        [Required]
        [MaxLength(100)]
        [JsonPropertyName("name")]
        public string Name { get; set;} = default!;
        [JsonPropertyName("diameter")]
        public string Diameter { get; set; }
        [JsonPropertyName("rotation_period")]
        public string RotationPeriod { get; set; }
        [JsonPropertyName("orbital_period")]
        public string OrbitalPeriod { get; set; }
        [JsonPropertyName("gravity")]
        public string Gravity { get; set; }
        [JsonPropertyName("population")]
        public string Population { get; set; }
        [JsonPropertyName("climate")]
        public string Climate { get; set; }
        [JsonPropertyName("terrain")]
        public string Terrain { get; set; }
        [JsonPropertyName("surface_water")]
        public string SurfaceWater { get; set; }
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
        public Planet() 
        {
            Diameter = "unknown";
            RotationPeriod = "unknown";
            OrbitalPeriod = "unknown";
            Gravity = "unknown";
            Population = "unknown";
            Climate = "unknown";
            Terrain = "unknown";
            SurfaceWater = "unknown";
            Residents = new string[] {};
            Films = new string[] {};
            Created = DateTime.Now.ToString();
            Edited = DateTime.Now.ToString();
            Url = "unknown";
        }

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
            RotationPeriod = rotationPeriod;
            OrbitalPeriod = orbitalPeriod;
            Gravity = gravity;
            Population = population;
            Climate = climate;
            Terrain = terrain;
            SurfaceWater = surfaceWater;
            Residents = residents;
            Films = films;
            Created = created;
            Edited = edited;
            Url = url;
        }
    }
}
