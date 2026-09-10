using LamaWMS.Domain.Entities;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LamaWMS.WebUI.Controllers;

public class FabricsController : Controller
{
    private readonly ApplicationDbContext _context;

    public FabricsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var fabrics = await _context.Fabrics
        .Include(x => x.Rolls)
        .OrderBy(x => x.Name)
        .ThenBy(x => x.Color)
        .ToListAsync();

        return View(fabrics);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
            return NotFound();

        var fabric = await _context.Fabrics
            .Include(x => x.Rolls)
                .ThenInclude(x => x.Cell)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (fabric == null)
            return NotFound();

        return View(fabric);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Fabric fabric)
    {
        if (!ModelState.IsValid)
            return View(fabric);

        fabric.Id = Guid.NewGuid();

        _context.Add(fabric);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
            return NotFound();

        var fabric = await _context.Fabrics.FindAsync(id);

        if (fabric == null)
            return NotFound();

        return View(fabric);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Fabric fabric)
    {
        if (id != fabric.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(fabric);

        try
        {
            _context.Update(fabric);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Fabrics.Any(x => x.Id == fabric.Id))
                return NotFound();

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
            return NotFound();

        var fabric = await _context.Fabrics
            .FirstOrDefaultAsync(x => x.Id == id);

        if (fabric == null)
            return NotFound();

        return View(fabric);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var fabric = await _context.Fabrics.FindAsync(id);

        if (fabric != null)
            _context.Fabrics.Remove(fabric);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}