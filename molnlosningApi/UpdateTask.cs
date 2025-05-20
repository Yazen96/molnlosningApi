using System;
using System.IO;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net;

namespace molnlosningApi
{
    public class UpdateTask
    {
        private readonly ILogger _logger;

        public UpdateTask(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdateTask>();
        }

        /// <summary>
        /// UpdateTask Function (PUT)
        /// This function updates an existing task in the Azure SQL database by ID.
        ///
        /// 🔗 Endpoint: PUT /api/tasks/{id}
        /// 🔐 Requires Function Key: Yes  (you find the key in the Azure portal site:https://portal.azure.com/#view/WebsitesExtension/FunctionTabMenuBlade/~/functionKeys/resourceId/%2Fsubscriptions%2Fc4b7ee56-bbc7-4664-bf23-a55d20a9086f%2FresourceGroups%2FRG-yazanalnsierat%2Fproviders%2FMicrosoft.Web%2Fsites%2FmolnlosningApi%2Ffunctions%2FUpdateTask)
        /// 
        /// 📥 URL Example:
        /// https://molnlosningapi.azurewebsites.net/api/tasks?code=put_the_function_key_here
        /// 
        /// 📥 Request Body (JSON):
        /// {
        ///     "title": "Updated task title",
        ///     "description": "Updated description",
        ///     "isCompleted": true
        /// }
        ///
        /// 📤 Response:
        /// - 200 OK with the updated task
        /// - 400 Bad Request if JSON is invalid
        /// - 404 Not Found if the task ID doesn't exist
        /// - 500 Internal Server Error if DB update fails
        /// </summary>
        [Function("UpdateTask")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tasks/{id:guid}")] HttpRequestData req,
            Guid id)
        {
            _logger.LogInformation($"UpdateTask triggered with ID: {id}");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"Received body: {requestBody}");

            TaskItem updatedTask;
            try
            {
                updatedTask = JsonSerializer.Deserialize<TaskItem>(requestBody);
                if (updatedTask == null)
                {
                    _logger.LogError("Invalid JSON: null task");
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid JSON.");
                    return bad;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Deserialization error: {ex.Message}");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Malformed JSON.");
                return bad;
            }

            updatedTask.Id = id;

            try
            {
                string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    _logger.LogInformation("Connected to SQL Server.");

                    var cmd = new SqlCommand(
                        "UPDATE Tasks SET Title = @Title, Description = @Description, IsCompleted = @IsCompleted WHERE Id = @Id", conn);

                    cmd.Parameters.AddWithValue("@Id", updatedTask.Id);
                    cmd.Parameters.AddWithValue("@Title", updatedTask.Title ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Description", updatedTask.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("@IsCompleted", updatedTask.IsCompleted);

                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows == 0)
                    {
                        _logger.LogWarning("Task not found.");
                        var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                        await notFound.WriteStringAsync("Task not found.");
                        return notFound;
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(updatedTask);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update error: {ex.Message}");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("Error updating task.");
                return error;
            }
        }
    }
}
