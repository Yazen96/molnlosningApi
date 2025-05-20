using System;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace molnlosningApi
{
    public class DeleteTask
    {
        private readonly ILogger _logger;

        public DeleteTask(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DeleteTask>();
        }

        /// <summary>
        /// DeleteTask Function (DELETE)
        /// This function deletes a task by its ID from the Azure SQL database.
        ///
        /// 🔗 Endpoint: DELETE /api/tasks/{id}
        /// 🔐 Requires Function Key: Yes  (you find the key in the Azure portal site: https://portal.azure.com/#view/WebsitesExtension/FunctionTabMenuBlade/~/functionKeys/resourceId/%2Fsubscriptions%2Fc4b7ee56-bbc7-4664-bf23-a55d20a9086f%2FresourceGroups%2FRG-yazanalnsierat%2Fproviders%2FMicrosoft.Web%2Fsites%2FmolnlosningApi%2Ffunctions%2FDeleteTask)
        ///
        /// 📥 URL Example:
        /// https://molnlosningapi.azurewebsites.net/api/tasks?code=put_the_function_key_here
        ///
        /// 📤 Response:
        /// - 204 No Content (if deleted successfully)
        /// - 404 Not Found (if task with specified ID does not exist)
        /// - 500 Internal Server Error (if a DB error occurs)
        /// </summary>
        [Function("DeleteTask")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tasks/{id:guid}")] HttpRequestData req,
            Guid id)
        {
            _logger.LogInformation($"DeleteTask triggered with id: {id}");

            string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    var cmd = new SqlCommand("DELETE FROM Tasks WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", id);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("Task not found.");
                        var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                        await notFound.WriteStringAsync("Task not found.");
                        return notFound;
                    }

                    _logger.LogInformation("Task deleted successfully.");
                    var response = req.CreateResponse(HttpStatusCode.NoContent); // 204
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting task: {ex.Message}");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("Error deleting task.");
                return error;
            }
        }
    }
}
