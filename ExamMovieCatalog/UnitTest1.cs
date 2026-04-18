using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using ExamMovieCatalog.Models;

namespace ExamMovieCatalog
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;

        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI3MDcxY2VjMy1lOGU3LTRmMDUtYjRjMi0wYzcxN2M1ZmRiMWUiLCJpYXQiOiIwNC8xOC8yMDI2IDA3OjExOjE1IiwiVXNlcklkIjoiMTNmZjBkYzAtZjRjYy00NGUzLTYxYjQtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJtaW1pMTIzQHRlc3QuYmciLCJVc2VyTmFtZSI6Im1pbWl0ZXJ6aWV2YSIsImV4cCI6MTc3NjUxNzg3NSwiaXNzIjoiTW92aWVDYXRhbG9nX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiTW92aWVDYXRhbG9nX1dlYkFQSV9Tb2Z0VW5pIn0.Bmn9ipsBO6QWwT1P_HZeuCC-45KdRhPpABqcGLOwbDw";
        private const string LoginEmail = "mimi123@test.bg";
        private const string LoginPassword = "123123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;
            if (!string.IsNullOrEmpty(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token is null or empty.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}");
            }
        }

        private static string createdMovieId;

        [Test, Order(1)]
        public void CreateMovie_ShouldWork()
        {
            var request = new RestRequest("/api/Movie/Create", Method.Post);

            request.AddJsonBody(new
            {
                title = "Test Movie",
                description = "Test Description"
            });

            var response = client.Execute(request);

            Assert.AreEqual(200, (int)response.StatusCode);

            var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.IsNotNull(data.Movie);
            Assert.IsFalse(string.IsNullOrEmpty(data.Movie.Id));
            Assert.AreEqual("Movie created successfully!", data.Msg);

            createdMovieId = data.Movie.Id;
        }

        [Test, Order(2)]
        public void EditMovie_ShouldWork()
        {
            var request = new RestRequest($"/api/Movie/Edit?movieId={createdMovieId}", Method.Put);

            request.AddJsonBody(new
            {
                title = "Edited Movie",
                description = "Edited Description"
            });

            var response = client.Execute(request);

            var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.AreEqual(200, (int)response.StatusCode);
            Assert.AreEqual("Movie edited successfully!", data.Msg);
        }

        [Test, Order(3)]
        public void GetAllMovies_ShouldReturnData()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);

            var response = client.Execute(request);

            Assert.AreEqual(200, (int)response.StatusCode);

            var json = JsonDocument.Parse(response.Content);

            Assert.IsTrue(json.RootElement.GetArrayLength() > 0);
        }

        [Test, Order(4)]
        public void DeleteMovie_ShouldWork()
        {
            var request = new RestRequest($"/api/Movie/Delete?movieId={createdMovieId}", Method.Delete);

            var response = client.Execute(request);

            var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.AreEqual(200, (int)response.StatusCode);
            Assert.AreEqual("Movie deleted successfully!", data.Msg);
        }

        [Test, Order(5)]
        public void CreateMovie_WithoutRequiredFields_ShouldFail()
        {
            var request = new RestRequest("/api/Movie/Create", Method.Post);

            request.AddJsonBody(new { });

            var response = client.Execute(request);

            Assert.AreEqual(400, (int)response.StatusCode);
        }

        [Test, Order(6)]
        public void Edit_NonExistingMovie_ShouldFail()
        {
            var request = new RestRequest("/api/Movie/Edit?movieId=invalid", Method.Put);

            request.AddJsonBody(new
            {
                title = "Test",
                description = "Test"
            });

            var response = client.Execute(request);

            var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.AreEqual(400, (int)response.StatusCode);
            Assert.AreEqual("Unable to edit the movie! Check the movieId parameter or user verification!", data.Msg);
        }

        [Test, Order(7)]
        public void Delete_NonExistingMovie_ShouldFail()
        {
            var request = new RestRequest("/api/Movie/Delete?movieId=invalid", Method.Delete);

            var response = client.Execute(request);

            var data = JsonSerializer.Deserialize<ApiResponseDto>(response.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.AreEqual(400, (int)response.StatusCode);
            Assert.AreEqual("Unable to delete the movie! Check the movieId parameter or user verification!", data.Msg);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}