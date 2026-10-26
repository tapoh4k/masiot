using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class WFListRepository : IWFListRepository
{
    private readonly ApplicationDbContext _context;

    public WFListRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WFList?> GetByIdAsync(int id)
    {
        return await _context.WFLists.FindAsync(id);
    }

    public async Task<List<WFList>> GetAllAsync()
    {
        return await _context.WFLists.ToListAsync();
    }

    public async Task<List<WFList>> GetByAgentIdAsync(string agentId)
    {
        return await _context.WFLists
            .Where(w => w.AgentId == agentId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<WFList>> GetByStatusAsync(int status)
    {
        return await _context.WFLists
            .Where(w => w.Status == status)
            .ToListAsync();
    }

    public async Task<int> AddAsync(WFList wfList)
    {
        _context.WFLists.Add(wfList);
        await _context.SaveChangesAsync();
        return wfList.Id;
    }

    public async Task UpdateAsync(WFList wfList)
    {
        _context.WFLists.Update(wfList);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStepAsync(int id, int step)
    {
        var wfList = await GetByIdAsync(id);
        if (wfList != null)
        {
            wfList.Step = step;
            wfList.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateParametersAsync(int id, string parameters)
    {
        var wfList = await GetByIdAsync(id);
        if (wfList != null)
        {
            wfList.Parameters = parameters;
            wfList.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateStatusAsync(int id, int status)
    {
        var wfList = await GetByIdAsync(id);
        if (wfList != null)
        {
            wfList.Status = status;
            wfList.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var wfList = await GetByIdAsync(id);
        if (wfList != null)
        {
            _context.WFLists.Remove(wfList);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetLastIdAsync()
    {
        var lastWfList = await _context.WFLists
            .OrderByDescending(w => w.Id)
            .FirstOrDefaultAsync();
        return lastWfList?.Id ?? 0;
    }
}
