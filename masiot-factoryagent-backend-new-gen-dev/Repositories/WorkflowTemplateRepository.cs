using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class WorkflowTemplateRepository : IWorkflowTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public WorkflowTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkflowTemplate>> GetAllAsync()
    {
        return await _context.WorkflowTemplates
            .OrderByDescending(wt => wt.CreatedAt)
            .ToListAsync();
    }

    public async Task<WorkflowTemplate?> GetByIdAsync(int id)
    {
        return await _context.WorkflowTemplates.FindAsync(id);
    }

    public async Task<WorkflowTemplate?> GetByNameAsync(string name)
    {
        return await _context.WorkflowTemplates
            .FirstOrDefaultAsync(wt => wt.Name == name);
    }

    public async Task<WorkflowTemplate> CreateAsync(WorkflowTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        _context.WorkflowTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<WorkflowTemplate> UpdateAsync(WorkflowTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;

        _context.WorkflowTemplates.Update(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var template = await _context.WorkflowTemplates.FindAsync(id);
        if (template == null)
            return false;

        _context.WorkflowTemplates.Remove(template);
        await _context.SaveChangesAsync();
        return true;
    }
}
