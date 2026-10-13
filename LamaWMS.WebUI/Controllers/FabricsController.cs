using Microsoft.AspNetCore.Authorization;
using LamaWMS.Domain.Entities;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LamaWMS.WebUI.Controllers;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class FabricsController : Controller
{
    private readonly ApplicationDbContext _context;

    public FabricsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Fabrics
            .Include(x => x.Rolls)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Name.Contains(search));
        }

        var fabrics = await query
            .OrderBy(x => x.Name)
            .ToListAsync();

        ViewBag.Search = search;

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

    [Authorize(Roles = "Admin,Operator")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Create(Fabric fabric)
    {
        if (!ModelState.IsValid)
            return View(fabric);

        fabric.Id = Guid.NewGuid();

        _context.Add(fabric);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Operator")]
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
    [Authorize(Roles = "Admin,Operator")]
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

    [Authorize(Roles = "Admin,Operator")]
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
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var fabric = await _context.Fabrics.FindAsync(id);

        if (fabric != null)
            _context.Fabrics.Remove(fabric);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}