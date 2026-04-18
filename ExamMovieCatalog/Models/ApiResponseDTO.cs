
using System.Text.Json.Serialization;


namespace ExamMovieCatalog.Models
{
    public class ApiResponseDto
    {
        public string Msg { get; set; }
        public MovieDto Movie { get; set; }
    }
}