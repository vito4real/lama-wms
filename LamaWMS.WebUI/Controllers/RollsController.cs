using LamaWMS.Domain.Entities;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LamaWMS.WebUI.Controllers;

public class RollsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RollsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var rolls = await _context.Rolls
            .Include(x => x.Fabric)
            .Include(x => x.Cell)
            .OrderBy(x => x.Number)
            .ToListAsync();

        return View(rolls);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
            return NotFound();

        var roll = await _context.Rolls
            .Include(x => x.Fabric)
            .Include(x => x.Cell)
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    public async Task<IActionResult> Create()
    {
        await LoadSelectListsAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Roll roll)
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(
                roll.FabricId,
                roll.CellId);

            return View(roll);
        }

        roll.Id = Guid.NewGuid();

        // При создании рулона текущий остаток равен начальному.
        roll.CurrentLength = roll.InitialLength;

        _context.Rolls.Add(roll);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
            return NotFound();

        var roll = await _context.Rolls.FindAsync(id);

        if (roll == null)
            return NotFound();

        await LoadSelectListsAsync(
            roll.FabricId,
            roll.CellId);

        return View(roll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Roll roll)
    {
        if (id != roll.Id)
            return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(
                roll.FabricId,
                roll.CellId);

            return View(roll);
        }

        try
        {
            _context.Update(roll);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Rolls.Any(x => x.Id == roll.Id))
                return NotFound();

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
            return NotFound();

        var roll = await _context.Rolls
            .Include(x => x.Fabric)
            .Include(x => x.Cell)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var roll = await _context.Rolls.FindAsync(id);

        if (roll != null)
        {
            _context.Rolls.Remove(roll);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadSelectListsAsync(
    Guid? fabricId = null,
    Guid? cellId = null)
    {
        var fabrics = await _context.Fabrics
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Color)
            .Select(x => new
            {
                x.Id,
                DisplayName = x.Name + " — " + x.Color
            })
            .ToListAsync();

        var cells = await _context.Cells
            .OrderBy(x => x.Code)
            .ToListAsync();

        ViewBag.FabricId = new SelectList(
            fabrics,
            "Id",
            "DisplayName",
            fabricId);

        ViewBag.CellId = new SelectList(
            cells,
            "Id",
            "Code",
            cellId);
    }
}