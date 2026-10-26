using Backend.Models;

namespace Backend.Repositories;

public interface IWorkflowTemplateRepository
{
    Task<IEnumerable<WorkflowTemplate>> GetAllAsync();
    Task<WorkflowTemplate?> GetByIdAsync(int id);
    Task<WorkflowTemplate?> GetByNameAsync(string name);
    Task<WorkflowTemplate> CreateAsync(WorkflowTemplate template);
    Task<WorkflowTemplate> UpdateAsync(WorkflowTemplate template);
    Task<bool> DeleteAsync(int id);
}
