using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class TimetableRepository : ITimetableRepository
{
    private readonly ApplicationDbContext _context;

    public TimetableRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Timetable?> GetByIdAsync(int id)
    {
        return await _context.Timetables
            .Include(t => t.WFList)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Timetable>> GetAllAsync()
    {
        return await _context.Timetables
            .Include(t => t.WFList)
            .OrderBy(t => t.TimeStart)
            .ToListAsync();
    }

    public async Task<List<Timetable>> GetByAgentIdAsync(string agentId)
    {
        return await _context.Timetables
            .Where(t => t.WFList != null && t.WFList.AgentId == agentId)
            .Include(t => t.WFList)
            .OrderBy(t => t.TimeStart)
            .ToListAsync();
    }

    public async Task<List<Timetable>> GetByStatusAsync(int status)
    {
        return await _context.Timetables
            .Where(t => t.Status == status)
            .Include(t => t.WFList)
            .OrderBy(t => t.TimeStart)
            .ToListAsync();
    }

    public async Task<List<Timetable>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.Timetables
            .Where(t => t.TimeStart >= start && t.TimeEnd <= end)
            .Include(t => t.WFList)
            .OrderBy(t => t.TimeStart)
            .ToListAsync();
    }

    public async Task<int> AddAsync(Timetable timetable)
    {
        _context.Timetables.Add(timetable);
        await _context.SaveChangesAsync();
        return timetable.Id;
    }

    public async Task UpdateAsync(Timetable timetable)
    {
        _context.Timetables.Update(timetable);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var timetable = await GetByIdAsync(id);
        if (timetable != null)
        {
            _context.Timetables.Remove(timetable);
            await _context.SaveChangesAsync();
        }
    }
}
