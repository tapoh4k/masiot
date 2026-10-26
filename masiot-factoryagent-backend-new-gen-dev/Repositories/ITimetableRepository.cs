using Backend.Models;

namespace Backend.Repositories;

public interface ITimetableRepository
{
    Task<Timetable?> GetByIdAsync(int id);
    Task<List<Timetable>> GetAllAsync();
    Task<List<Timetable>> GetByAgentIdAsync(string agentId);
    Task<List<Timetable>> GetByStatusAsync(int status);
    Task<List<Timetable>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<int> AddAsync(Timetable timetable);
    Task UpdateAsync(Timetable timetable);
    Task DeleteAsync(int id);
}
