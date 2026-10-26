using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly IPasswordHashService _passwordHashService;

    public UsersController(
        ApplicationDbContext context,
        ILogger<UsersController> logger,
        IEncryptionService encryptionService,
        IPasswordHashService passwordHashService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
        _passwordHashService = passwordHashService;
    }


    // POST: api/users/login
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null)
            {
                return Unauthorized(new { message = "Неверный логин или пароль" });
            }

            // Проверяем хеш пароля для аутентификации
            if (!_passwordHashService.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Неверный логин или пароль" });
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "Ошибка входа" });
        }
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        try
        {
            // Проверка прав доступа: только admin может видеть список пользователей
            var requestRole = Request.Headers["X-User-Role"].FirstOrDefault();
            if (requestRole != "admin")
            {
                return StatusCode(403, new { message = "Доступ запрещён. Требуется роль администратора." });
            }

            var users = await _context.Users.ToListAsync();

            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                FullName = u.FullName,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            }).ToList();

            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new { message = "Ошибка получения пользователей" });
        }
    }

    // Проверка роли администратора
    private bool IsAdmin() => Request.Headers["X-User-Role"].FirstOrDefault() == "admin";

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"Пользователь с id {id} не найден" });
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {Id}", id);
            return StatusCode(500, new { message = "Ошибка получения пользователя" });
        }
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createDto)
    {
        try
        {
            if (!IsAdmin())
            {
                return StatusCode(403, new { message = "Доступ запрещён. Требуется роль администратора." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == createDto.Username);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Пользователь с таким логином уже существует" });
            }

            var user = new User
            {
                Username = createDto.Username,
                Password = _encryptionService.Encrypt(createDto.Password), // Шифруем для просмотра
                PasswordHash = _passwordHashService.HashPassword(createDto.Password), // Хешируем для аутентификации
                Role = createDto.Role,
                FullName = createDto.FullName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Ошибка создания пользователя" });
        }
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateDto)
    {
        try
        {
            if (!IsAdmin())
            {
                return StatusCode(403, new { message = "Доступ запрещён. Требуется роль администратора." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"Пользователь с id {id} не найден" });
            }

            if (!string.IsNullOrEmpty(updateDto.Password))
            {
                user.Password = _encryptionService.Encrypt(updateDto.Password);
                user.PasswordHash = _passwordHashService.HashPassword(updateDto.Password);
            }

            if (!string.IsNullOrEmpty(updateDto.Role))
            {
                user.Role = updateDto.Role;
            }

            if (updateDto.FullName != null)
            {
                user.FullName = updateDto.FullName;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Password = !string.IsNullOrEmpty(updateDto.Password) ? updateDto.Password : _encryptionService.Decrypt(user.Password),
                Role = user.Role,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Id}", id);
            return StatusCode(500, new { message = "Ошибка обновления пользователя" });
        }
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        try
        {
            if (!IsAdmin())
            {
                return StatusCode(403, new { message = "Доступ запрещён. Требуется роль администратора." });
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"Пользователь с id {id} не найден" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Пользователь удален успешно" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {Id}", id);
            return StatusCode(500, new { message = "Ошибка удаления пользователя" });
        }
    }
}
