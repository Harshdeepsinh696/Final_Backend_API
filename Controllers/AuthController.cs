using Final_Backend_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Final_Backend_API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly string _connStr = string.Empty;

        public AuthController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")
                       ?? string.Empty;
        }

        // ══════════════════════════════════════
        // 1. SIGNUP
        // POST: api/auth/signup
        // ══════════════════════════════════════
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid data." });

            // Check passwords match
            if (model.Password != model.Confirm)
                return BadRequest(new { message = "Passwords don't match." });

            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                // Check email already exists
                var checkCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM Users WHERE Email = @Email", conn);
                checkCmd.Parameters.AddWithValue("@Email", model.Email);
                int exists = Convert.ToInt32(
                    await checkCmd.ExecuteScalarAsync()
                );

                if (exists > 0)
                    return Conflict(new { message = "❌ Email already registered." });

                // Hash password
                string passwordHash = HashPassword(model.Password);

                // Insert new user
                var insertCmd = new SqlCommand(@"
                    INSERT INTO Users
                        (FullName, Email, Phone, Dob, Gender, PasswordHash, IsActive)
                    VALUES
                        (@FullName, @Email, @Phone, @Dob, @Gender, @PasswordHash, 1);
                    SELECT SCOPE_IDENTITY();", conn);

                insertCmd.Parameters.AddWithValue("@FullName", model.FullName);
                insertCmd.Parameters.AddWithValue("@Email", model.Email);
                insertCmd.Parameters.AddWithValue("@Phone", model.Phone);
                insertCmd.Parameters.AddWithValue("@Dob", model.Dob);
                insertCmd.Parameters.AddWithValue("@Gender", model.Gender);
                insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                int newUserId = Convert.ToInt32(
                    await insertCmd.ExecuteScalarAsync()
                );

                return Ok(new
                {
                    message = "✅ Account created successfully!",
                    userId = newUserId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Server error.",
                    error = ex.Message
                });
            }
        }

        // ══════════════════════════════════════
        // 2. LOGIN
        // POST: api/auth/login
        // ══════════════════════════════════════
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                // 🔥 IMPORTANT: hash password
                string passwordHash = HashPassword(model.Password);

                var cmd = new SqlCommand(@"
                    SELECT Id, FullName, Email, Phone, Dob, Gender, IsActive, PasswordHash
                    FROM Users
                    WHERE Email = @Email", conn);

                cmd.Parameters.AddWithValue("@Email", model.Email);

                var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return Unauthorized(new { message = "Invalid email or password" });

                // compare password manually
                string dbPassword = reader["PasswordHash"].ToString();

                if (dbPassword != passwordHash)
                    return Unauthorized(new { message = "Invalid email or password" });

                return Ok(new
                {
                    message = "Login successful",
                    user = new
                    {
                        Id = reader["Id"],
                        FullName = reader["FullName"],
                        Email = reader["Email"],
                        Phone = reader["Phone"],
                        Dob = reader["Dob"],
                        Gender = reader["Gender"]
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Server error",
                    error = ex.Message
                });
            }
        }

        // ══════════════════════════════════════
        // 3. TEST CONNECTION
        // GET: api/auth/test
        // ══════════════════════════════════════
        [HttpGet("test")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();
                return Ok(new { message = "✅ Auth connected!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Connection failed!",
                    error = ex.Message
                });
            }
        }

        // ══════════════════════════════════════
        // 4. GET USER BY ID
        // GET: api/auth/user/{id}
        // ══════════════════════════════════════
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT Id, FullName, Email, Phone, Dob, Gender, IsActive, PhotoUrl
                    FROM Users
                    WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", id);

                var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return NotFound(new { message = "❌ User not found." });

                return Ok(new
                {
                    userId = reader["Id"],
                    fullName = reader["FullName"],
                    email = reader["Email"],
                    phone = reader["Phone"],
                    dob = reader["Dob"],
                    gender = reader["Gender"],
                    photo = reader["PhotoUrl"],

                    isActive = reader["IsActive"]
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Server error.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("upload-photo/{id}")]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // ✅ Don't hardcode http or https — build URL from current Request
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            string imageUrl = $"{baseUrl}/uploads/{fileName}";

            using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new SqlCommand(
                "UPDATE Users SET PhotoUrl = @Photo WHERE Id = @Id", conn);

            cmd.Parameters.AddWithValue("@Photo", imageUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", id);

            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return BadRequest("User not found");

            return Ok(new { photo = imageUrl });
        }

        // ══════════════════════════════════════
        // REMOVE PHOTO
        // DELETE: api/auth/remove-photo/{id}
        // ══════════════════════════════════════
        [HttpDelete("remove-photo/{id}")]
        public async Task<IActionResult> RemovePhoto(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                // Get current photo URL first
                var getCmd = new SqlCommand("SELECT PhotoUrl FROM Users WHERE Id = @Id", conn);
                getCmd.Parameters.AddWithValue("@Id", id);
                var photoUrl = (await getCmd.ExecuteScalarAsync())?.ToString();

                // Delete file from server if exists
                if (!string.IsNullOrEmpty(photoUrl))
                {
                    var fileName = Path.GetFileName(photoUrl);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                // Set PhotoUrl to NULL in DB
                var cmd = new SqlCommand("UPDATE Users SET PhotoUrl = NULL WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "Photo removed successfully ✅" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Server error.", error = ex.Message });
            }
        }

        // ══════════════════════════════════════
        // 5. UPDATE USER
        // PUT: api/auth/update/{id}
        // ══════════════════════════════════════
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] SignupModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid data." });

            try
            {
                using var conn = new SqlConnection(_connStr);
                await conn.OpenAsync();

                // Check user exists
                var checkCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM Users WHERE Id = @Id", conn);
                checkCmd.Parameters.AddWithValue("@Id", id);

                int exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                if (exists == 0)
                    return NotFound(new { message = "❌ User not found." });

                // 🔥 Update query
                var updateCmd = new SqlCommand(@"
                    UPDATE Users
                    SET 
                        FullName = @FullName,
                        Email = @Email,
                        Phone = @Phone,
                        Dob = @Dob,
                        Gender = @Gender
                    WHERE Id = @Id", conn);

                updateCmd.Parameters.AddWithValue("@FullName", model.FullName);
                updateCmd.Parameters.AddWithValue("@Email", model.Email);
                updateCmd.Parameters.AddWithValue("@Phone", model.Phone);
                updateCmd.Parameters.AddWithValue("@Dob", model.Dob);
                updateCmd.Parameters.AddWithValue("@Gender", model.Gender);
                updateCmd.Parameters.AddWithValue("@Id", id);

                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new { message = "✅ Profile updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Server error.",
                    error = ex.Message
                });
            }
        }



        // ══════════════════════════════════════
        // Password Hash Helper
        // ══════════════════════════════════════
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(
                Encoding.UTF8.GetBytes(password)
            );
            return Convert.ToHexString(bytes);
        }
    }
}