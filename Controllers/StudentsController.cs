using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApi.Data;
using StudentApi.Models;


namespace StudentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController(StudentDbContext db) : ControllerBase
    {
    private IQueryable<StudentRow> StudentRows() =>
        from s in db.Students.AsNoTracking()
        join a in db.AcadLevels on s.AcadlevelId equals a.Id into levels
        from a in levels.DefaultIfEmpty()
        join t in db.Sections on s.SectionId equals t.Id into sections
        from t in sections.DefaultIfEmpty()
        select new StudentRow
        {
            Id = s.Id, Lastname = s.Lastname, Firstname = s.Firstname, Middlename = s.Middlename,
            AcadlevelId = s.AcadlevelId, AcadlevelName = a == null ? null : a.Name,
            SectionId = s.SectionId, SectionName = t == null ? null : t.Name,
            Gender = s.Gender, Email = s.Email
        };
    [HttpGet]
    public async Task<ActionResult> GetAll() => Ok(await StudentRows().ToListAsync());
    
  [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        // Filter before projecting for efficient database query.
        var student = await db.Students.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id, s.Lastname, s.Firstname, s.Middlename,
                s.AcadlevelId,
                AcadlevelName = db.AcadLevels.Where(a => a.Id == s.AcadlevelId)
                    .Select(a => a.Name).FirstOrDefault(),
                s.SectionId,
                SectionName = db.Sections.Where(t => t.Id == s.SectionId)
                    .Select(t => t.Name).FirstOrDefault(),
                s.Gender, s.Email
            }).FirstOrDefaultAsync();
        return student is null ? NotFound() : Ok(student);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return Ok(await StudentRows().ToListAsync());
        var term = $"%{search.Trim()}%";
        var ids = db.Students.Where(s =>
            EF.Functions.ILike(s.Firstname, term) ||
            EF.Functions.ILike(s.Lastname, term) ||
            EF.Functions.ILike(s.Email, term)).Select(s => s.Id);
        return Ok(await StudentRows().Where(s => ids.Contains(s.Id)).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] Student student)
    {
        student.Id = 0;
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, new { id = student.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Student input)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return NotFound();
        student.Lastname = input.Lastname;
        student.Firstname = input.Firstname;
        student.Middlename = input.Middlename;
        student.AcadlevelId = input.AcadlevelId;
        student.SectionId = input.SectionId;
        student.Gender = input.Gender;
        student.Email = input.Email;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return NotFound();
        db.Students.Remove(student);
        await db.SaveChangesAsync();
        return NoContent();
    }
    }
}