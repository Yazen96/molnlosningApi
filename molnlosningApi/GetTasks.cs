﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Data.SqlClient;

namespace molnlosningApi
{
    public class GetTasks
    {
        private readonly ILogger _logger;

        public GetTasks(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetTasks>();
        }

        /// <summary>
        /// GetTasks Function (GET)
        /// This function retrieves all tasks from the Azure SQL database.
        ///
        /// 🔗 Endpoint: GET /api/tasks
        /// 🔐 Requires Function Key: Yes  (you find the key in the Azure portal site: https://portal.azure.com/#view/WebsitesExtension/FunctionTabMenuBlade/~/functionKeys/resourceId/%2Fsubscriptions%2Fc4b7ee56-bbc7-4664-bf23-a55d20a9086f%2FresourceGroups%2FRG-yazanalnsierat%2Fproviders%2FMicrosoft.Web%2Fsites%2FmolnlosningApi%2Ffunctions%2FGetTasks)
        /// 
        /// 📥 URL Example:
        /// https://molnlosningapi.azurewebsites.net/api/tasks?code=put_the_function_key_here
        /// 
        /// 📤 Response:
        /// - 200 OK with a JSON array of tasks
        /// [
        ///     {
        ///         "id": "guid",
        ///         "title": "Example title",
        ///         "description": "Example description",
        ///         "isCompleted": false
        ///     }
        /// ]
        /// - 500 Internal Server Error if DB query fails
        /// </summary>
        [Function("GetTasks")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks")] HttpRequestData req)
        {
            _logger.LogInformation("GetTasks function triggered.");

            var tasks = new List<TaskItem>();
            string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string is missing!");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Database connection string is not set.");
                return errorResponse;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    _logger.LogInformation("Opening SQL connection...");
                    await conn.OpenAsync();
                    _logger.LogInformation("SQL connection opened.");

                    var command = new SqlCommand("SELECT Id, Title, Description, IsCompleted FROM Tasks", conn);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tasks.Add(new TaskItem
                            {
                                Id = reader.GetGuid(0),
                                Title = reader.GetString(1),
                                Description = reader.GetString(2),
                                IsCompleted = reader.GetBoolean(3)
                            });
                        }
                    }

                    _logger.LogInformation($"Fetched {tasks.Count} tasks from the database.");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(tasks);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching tasks: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error fetching tasks from the database.");
                return errorResponse;
            }
        }
    }
}
