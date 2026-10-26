using Backend.Models;

namespace Backend.Repositories;

public interface IWFListRepository
{
    Task<WFList?> GetByIdAsync(int id);
    Task<List<WFList>> GetAllAsync();
    Task<List<WFList>> GetByAgentIdAsync(string agentId);
    Task<List<WFList>> GetByStatusAsync(int status);
    Task<int> AddAsync(WFList wfList);
    Task UpdateAsync(WFList wfList);
    Task UpdateStepAsync(int id, int step);
    Task UpdateParametersAsync(int id, string parameters);
    Task UpdateStatusAsync(int id, int status);
    Task DeleteAsync(int id);
    Task<int> GetLastIdAsync();
}
