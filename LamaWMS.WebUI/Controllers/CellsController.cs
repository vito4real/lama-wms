using LamaWMS.Domain.Entities;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LamaWMS.WebUI.Controllers;

public class CellsController : Controller
{
    private readonly ApplicationDbContext _context;

    public CellsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Cells
            .Include(x => x.Rolls)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Code.Contains(search));
        }

        var cells = await query
            .OrderBy(x => x.Code)
            .ToListAsync();

        ViewBag.Search = search;

        return View(cells);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
            return NotFound();

        var cell = await _context.Cells
            .Include(x => x.Rolls)
                .ThenInclude(x => x.Fabric)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cell == null)
            return NotFound();

        return View(cell);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Cell cell)
    {
        if (!ModelState.IsValid)
            return View(cell);

        cell.Id = Guid.NewGuid();

        _context.Cells.Add(cell);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
            return NotFound();

        var cell = await _context.Cells.FindAsync(id);

        if (cell == null)
            return NotFound();

        return View(cell);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Cell cell)
    {
        if (id != cell.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(cell);

        try
        {
            _context.Cells.Update(cell);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Cells.Any(x => x.Id == cell.Id))
                return NotFound();

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
            return NotFound();

        var cell = await _context.Cells
            .Include(x => x.Rolls)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cell == null)
            return NotFound();

        return View(cell);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var cell = await _context.Cells
            .Include(x => x.Rolls)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cell == null)
            return RedirectToAction(nameof(Index));

        if (cell.Rolls.Any())
        {
            ModelState.AddModelError(
                string.Empty,
                "Нельзя удалить ячейку, пока в ней находятся рулоны.");

            return View("Delete", cell);
        }

        _context.Cells.Remove(cell);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}