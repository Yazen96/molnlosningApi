using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Text.Json;

namespace molnlosningApi
{
    public class CreateTask
    {
        private readonly ILogger _logger;

        public CreateTask(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CreateTask>();
        }

        /// <summary>
        /// CreateTask Function (POST)
        /// This function creates a new task in the Azure SQL database.
        ///
        /// 🔗 Endpoint: POST /api/tasks
        /// 🔐 Requires Function Key: Yes  (you find the key in the Azure portal site: https://portal.azure.com/#view/WebsitesExtension/FunctionTabMenuBlade/~/functionKeys/resourceId/%2Fsubscriptions%2Fc4b7ee56-bbc7-4664-bf23-a55d20a9086f%2FresourceGroups%2FRG-yazanalnsierat%2Fproviders%2FMicrosoft.Web%2Fsites%2FmolnlosningApi%2Ffunctions%2FCreateTask)
        /// 
        /// 📥 URL Example:
        /// https://molnlosningapi.azurewebsites.net/api/tasks?code=put_the_function_key_here
        /// 
        /// 📥 Request Body (JSON):
        /// {
        ///     "title": "Buy groceries",
        ///     "description": "Milk, eggs, bread",
        ///     "isCompleted": false
        /// }
        ///
        /// 📤 Response:
        /// - 201 Created with the full task object
        /// - 400 Bad Request if JSON is invalid
        /// - 500 Internal Server Error if DB insert fails
        /// </summary>
        [Function("CreateTask")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks")] HttpRequestData req)
        {
            _logger.LogInformation("CreateTask function triggered");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"Raw request body: {requestBody}");

            TaskItem task;

            try
            {
                task = JsonSerializer.Deserialize<TaskItem>(requestBody);
                if (task == null)
                {
                    _logger.LogError("Deserialized task is null.");
                    var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badReq.WriteStringAsync("Invalid task format in JSON.");
                    return badReq;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"JSON deserialization failed: {ex.Message}");
                var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badReq.WriteStringAsync("Malformed JSON.");
                return badReq;
            }

            task.Id = Guid.NewGuid();

            string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    _logger.LogInformation("Connected to SQL Server.");

                    var cmd = new SqlCommand(
                        "INSERT INTO Tasks (Id, Title, Description, IsCompleted) VALUES (@Id, @Title, @Description, @IsCompleted)",
                        conn);

                    cmd.Parameters.AddWithValue("@Id", task.Id);
                    cmd.Parameters.AddWithValue("@Title", task.Title ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Description", task.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("@IsCompleted", task.IsCompleted);

                    await cmd.ExecuteNonQueryAsync();
                }

                var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
                await response.WriteAsJsonAsync(task);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database insert failed: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error saving task to database.");
                return errorResponse;
            }
        }
    }
}
