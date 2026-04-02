using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Final_Backend_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiseaseController : ControllerBase
    {
        private readonly string _connStr;

        public DiseaseController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection");
        }

        // GET: api/disease/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserDiseases(int userId)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT Id, DiseaseName, DiagnosedDate, Notes, IsActive
                    FROM UserDiseases
                    WHERE UserId = @UserId AND IsActive = 1
                    ORDER BY DiseaseName", conn);

                cmd.Parameters.AddWithValue("@UserId", userId);

                var reader = await cmd.ExecuteReaderAsync();
                var diseases = new List<object>();

                while (await reader.ReadAsync())
                {
                    diseases.Add(new
                    {
                        id = reader["Id"],
                        diseaseName = reader["DiseaseName"],
                        diagnosedDate = reader["DiagnosedDate"] == DBNull.Value
                            ? null : reader["DiagnosedDate"].ToString(),
                        notes = reader["Notes"] == DBNull.Value
                            ? "" : reader["Notes"].ToString(),
                        isActive = reader["IsActive"]
                    });
                }

                return Ok(diseases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }

        // POST: api/disease/add
        [HttpPost("add")]
        public async Task<IActionResult> AddDisease([FromBody] DiseaseModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    INSERT INTO UserDiseases (UserId, DiseaseName, DiagnosedDate, Notes, IsActive)
                    VALUES (@UserId, @DiseaseName, @DiagnosedDate, @Notes, 1);
                    SELECT SCOPE_IDENTITY();", conn);

                cmd.Parameters.AddWithValue("@UserId", model.UserId);
                cmd.Parameters.AddWithValue("@DiseaseName", model.DiseaseName);
                cmd.Parameters.AddWithValue("@DiagnosedDate",
                    string.IsNullOrEmpty(model.DiagnosedDate) ? DBNull.Value : model.DiagnosedDate);
                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");

                int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return Ok(new { id = newId, message = "✅ Disease added!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }

        // DELETE: api/disease/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDisease(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new SqlCommand(
                    "UPDATE UserDiseases SET IsActive = 0 WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "✅ Disease removed!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }
    }

    public class DiseaseModel
    {
        public int UserId { get; set; }
        public string DiseaseName { get; set; } = "";
        public string? DiagnosedDate { get; set; }
        public string? Notes { get; set; }
    }
}