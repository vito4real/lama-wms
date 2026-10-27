using Microsoft.AspNetCore.Authorization;
using LamaWMS.Domain.Entities;
using LamaWMS.Domain.Enums;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LamaWMS.WebUI.Controllers;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class RollsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RollsController(ApplicationDbContext context)
    {
        _context = context;
    }


    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        decimal? minLength,
        decimal? maxLength)
    {
        var query = _context.Rolls
            .Include(x => x.Fabric)
            .Include(x => x.Cell)
            .Include(x => x.Reservations)
            .AsQueryable();

        // Поиск по номеру рулона, ткани или ячейке.
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Number.Contains(search) ||
                x.Fabric.Name.Contains(search) ||
                (x.Cell != null &&
                 x.Cell.Code.Contains(search)));
        }

        // Проверка диапазона.
        if (minLength.HasValue &&
            maxLength.HasValue &&
            minLength.Value > maxLength.Value)
        {
            ModelState.AddModelError(
                string.Empty,
                "Минимальный метраж не может превышать максимальный.");
        }
        else
        {
            // Доступно = физический остаток - сумма резервов.
            if (minLength.HasValue)
            {
                query = query.Where(x =>
                    x.CurrentLength -
                    (x.Reservations.Sum(r =>
                        (decimal?)r.ReservedLength) ?? 0m)
                    >= minLength.Value);
            }

            if (maxLength.HasValue)
            {
                query = query.Where(x =>
                    x.CurrentLength -
                    (x.Reservations.Sum(r =>
                        (decimal?)r.ReservedLength) ?? 0m)
                    <= maxLength.Value);
            }
        }

        var rolls = (await query.ToListAsync())
            .OrderBy(x => x.Fabric.Name)
            .ThenBy(x => int.TryParse(x.Number, out var number)
                ? number
                : int.MaxValue)
            .ToList();

        ViewBag.Search = search;
        ViewBag.MinLength = minLength;
        ViewBag.MaxLength = maxLength;

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

    [HttpGet]
    public async Task<IActionResult> GetNextNumber(Guid fabricId)
    {
        var numbers = await _context.Rolls
            .Where(x => x.FabricId == fabricId)
            .Select(x => x.Number)
            .ToListAsync();

        var maxNumber = numbers
            .Select(x => int.TryParse(x, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        return Json(new
        {
            number = (maxNumber + 1).ToString()
        });
    }

    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Create()
    {
        await LoadSelectListsAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Create(Roll roll)
    {
        // Номер рулона назначается системой,
        // поэтому значение из формы не валидируем.
        ModelState.Remove(nameof(Roll.Number));

        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(
                roll.FabricId,
                roll.CellId);

            return View(roll);
        }

        var numbers = await _context.Rolls
            .Where(x => x.FabricId == roll.FabricId)
            .Select(x => x.Number)
            .ToListAsync();

        var maxNumber = numbers
            .Select(x => int.TryParse(x, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        roll.Number = (maxNumber + 1).ToString();

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
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Comment = "Первичный приход рулона"
        });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Operator")]
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
    [Authorize(Roles = "Admin,Operator")]
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

    [Authorize(Roles = "Admin,Operator")]
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
    [Authorize(Roles = "Admin,Operator")]
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