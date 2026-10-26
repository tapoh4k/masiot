using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;

namespace Backend.Controllers;

[ApiController]
[Route("api/file-uploads")]
public class FileUploadsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FileUploadsController> _logger;

    public FileUploadsController(ApplicationDbContext context, ILogger<FileUploadsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/file-uploads
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FileUploadDto>>> GetAllFileUploads()
    {
        try
        {
            var fileUploads = await _context.FileUploads
                .OrderByDescending(f => f.UploadedAt)
                .Select(f => new FileUploadDto
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    UploadedAt = f.UploadedAt,
                    Status = f.Status,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(fileUploads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file uploads");
            return StatusCode(500, new { message = "Error retrieving file uploads" });
        }
    }

    // POST: api/file-uploads
    [HttpPost]
    public async Task<ActionResult<FileUploadDto>> CreateFileUpload([FromBody] CreateFileUploadDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var fileUpload = new FileUpload
            {
                FileName = createDto.FileName,
                Status = createDto.Status,
                UploadedAt = createDto.UploadedAt ?? DateTime.UtcNow
            };

            _context.FileUploads.Add(fileUpload);
            await _context.SaveChangesAsync();

            var dto = new FileUploadDto
            {
                Id = fileUpload.Id,
                FileName = fileUpload.FileName,
                UploadedAt = fileUpload.UploadedAt,
                Status = fileUpload.Status,
                CreatedAt = fileUpload.CreatedAt
            };

            return CreatedAtAction(nameof(GetAllFileUploads), new { id = fileUpload.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file upload record");
            return StatusCode(500, new { message = "Error creating file upload record" });
        }
    }
}
