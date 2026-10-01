using LamaWMS.Domain.Entities;
using LamaWMS.Domain.Enums;
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

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Rolls
            .Include(x => x.Fabric)
            .Include(x => x.Cell)
            .Include(x => x.Reservations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Number.Contains(search) ||
                x.Fabric.Name.Contains(search) ||
                (x.Cell != null &&
                 x.Cell.Code.Contains(search)));
        }

        var rolls = await query
            .OrderBy(x => x.Number)
            .ToListAsync();

        ViewBag.Search = search;

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
                .ThenInclude(x => x.Product)
            .Include(x => x.Reservations)
                .ThenInclude(x => x.ProductFabric)
                    .ThenInclude(x => x.Product)
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

        // При создании рулона текущий остаток
        // равен его начальной длине.
        roll.CurrentLength = roll.InitialLength;

        _context.Rolls.Add(roll);

        // Первичное поступление рулона также является
        // физическим движением ткани и фиксируется в истории.
        _context.RollTransactions.Add(new RollTransaction
        {
            Id = Guid.NewGuid(),
            RollId = roll.Id,
            Type = RollTransactionType.Receipt,
            Length = roll.InitialLength,
            CreatedAt = DateTime.UtcNow,
            Comment = "Первичный приход рулона"
        });

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
    public async Task<IActionResult> Edit(
    Guid id,
    string number,
    Guid fabricId,
    Guid? cellId)
    {
        var roll = await _context.Rolls
            .FirstOrDefaultAsync(x => x.Id == id);

        if (roll == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(number))
        {
            ModelState.AddModelError(
                nameof(Roll.Number),
                "Укажите номер рулона.");
        }

        var fabricExists = await _context.Fabrics
            .AnyAsync(x => x.Id == fabricId);

        if (!fabricExists)
        {
            ModelState.AddModelError(
                nameof(Roll.FabricId),
                "Выбранная ткань не найдена.");
        }

        if (cellId.HasValue)
        {
            var cellExists = await _context.Cells
                .AnyAsync(x => x.Id == cellId.Value);

            if (!cellExists)
            {
                ModelState.AddModelError(
                    nameof(Roll.CellId),
                    "Выбранная ячейка не найдена.");
            }
        }

        if (!ModelState.IsValid)
        {
            // Возвращаем введённые пользователем значения,
            // но учётные остатки берём только из базы данных.
            roll.Number = number;
            roll.FabricId = fabricId;
            roll.CellId = cellId;

            await LoadSelectListsAsync(
                fabricId,
                cellId);

            return View(roll);
        }

        // Через обычное редактирование разрешено менять
        // только реквизиты рулона.
        roll.Number = number.Trim();
        roll.FabricId = fabricId;
        roll.CellId = cellId;

        // InitialLength и CurrentLength здесь намеренно
        // не изменяются. Остаток меняется только
        // складскими операциями через RollTransaction.

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Details),
            new { id = roll.Id });
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
            .Select(x => new
            {
                x.Id,
                DisplayName = x.Name
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