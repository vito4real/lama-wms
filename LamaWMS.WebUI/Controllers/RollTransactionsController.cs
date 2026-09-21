using LamaWMS.Domain.Entities;
using LamaWMS.Domain.Enums;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LamaWMS.WebUI.Controllers;

public class RollTransactionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RollTransactionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ------------------------------------------------------------
    // ПРИХОД
    // Дополнительное поступление ткани на существующий рулон.
    // ------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Receipt(Guid rollId)
    {
        var roll = await GetRollAsync(rollId);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receipt(
        Guid rollId,
        decimal length,
        string? comment)
    {
        var roll = await _context.Rolls
            .FirstOrDefaultAsync(x => x.Id == rollId);

        if (roll == null)
            return NotFound();

        if (length <= 0)
        {
            TempData["Error"] =
                "Количество прихода должно быть больше 0.";

            return RedirectToAction(
                nameof(Receipt),
                new { rollId });
        }

        roll.CurrentLength += length;

        AddTransaction(
            roll,
            RollTransactionType.Receipt,
            length,
            comment ?? "Приход ткани на склад");

        await _context.SaveChangesAsync();

        return RedirectToRoll(roll.Id);
    }

    // ------------------------------------------------------------
    // ВОЗВРАТ
    // Ткань была выдана/списана, но фактически вернулась на склад.
    // ------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Return(Guid rollId)
    {
        var roll = await GetRollAsync(rollId);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(
        Guid rollId,
        decimal length,
        string? comment)
    {
        var roll = await _context.Rolls
            .FirstOrDefaultAsync(x => x.Id == rollId);

        if (roll == null)
            return NotFound();

        if (length <= 0)
        {
            TempData["Error"] =
                "Количество возврата должно быть больше 0.";

            return RedirectToAction(
                nameof(Return),
                new { rollId });
        }

        roll.CurrentLength += length;

        AddTransaction(
            roll,
            RollTransactionType.Return,
            length,
            comment ?? "Возврат ткани на склад");

        await _context.SaveChangesAsync();

        return RedirectToRoll(roll.Id);
    }

    // ------------------------------------------------------------
    // СПИСАНИЕ
    // Брак, повреждение, потеря и другие складские потери.
    // ------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> WriteOff(Guid rollId)
    {
        var roll = await GetRollAsync(rollId);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WriteOff(
        Guid rollId,
        decimal length,
        string? comment)
    {
        var roll = await _context.Rolls
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.Id == rollId);

        if (roll == null)
            return NotFound();

        if (length <= 0)
        {
            TempData["Error"] =
                "Количество для списания должно быть больше 0.";

            return RedirectToAction(
                nameof(WriteOff),
                new { rollId });
        }

        var reservedLength =
            roll.Reservations.Sum(x => x.ReservedLength);

        var availableLength =
            roll.CurrentLength - reservedLength;

        // Нельзя списать ткань, которая уже обещана изделиям.
        if (length > availableLength)
        {
            TempData["Error"] =
                $"Для свободного списания доступно только {availableLength:0.00} м. " +
                $"Остальная ткань зарезервирована.";

            return RedirectToAction(
                nameof(WriteOff),
                new { rollId });
        }

        roll.CurrentLength -= length;

        AddTransaction(
            roll,
            RollTransactionType.WriteOff,
            length,
            comment ?? "Списание ткани");

        await _context.SaveChangesAsync();

        return RedirectToRoll(roll.Id);
    }

    // ------------------------------------------------------------
    // КОРРЕКТИРОВКА
    // Устанавливает фактический остаток после пересчёта/инвентаризации.
    // ------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Adjustment(Guid rollId)
    {
        var roll = await GetRollAsync(rollId);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjustment(
        Guid rollId,
        decimal actualLength,
        string? comment)
    {
        var roll = await _context.Rolls
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.Id == rollId);

        if (roll == null)
            return NotFound();

        if (actualLength < 0)
        {
            TempData["Error"] =
                "Фактический остаток не может быть отрицательным.";

            return RedirectToAction(
                nameof(Adjustment),
                new { rollId });
        }

        var reservedLength =
            roll.Reservations.Sum(x => x.ReservedLength);

        // После корректировки физического остатка должно хватать
        // на уже существующие резервы.
        if (actualLength < reservedLength)
        {
            TempData["Error"] =
                $"Нельзя установить остаток {actualLength:0.00} м, " +
                $"поскольку уже зарезервировано {reservedLength:0.00} м.";

            return RedirectToAction(
                nameof(Adjustment),
                new { rollId });
        }

        var difference =
            actualLength - roll.CurrentLength;

        if (difference == 0)
        {
            TempData["Error"] =
                "Фактический остаток совпадает с текущим.";

            return RedirectToAction(
                nameof(Adjustment),
                new { rollId });
        }

        roll.CurrentLength = actualLength;

        AddTransaction(
            roll,
            RollTransactionType.Adjustment,
            difference,
            comment ?? "Корректировка остатка");

        await _context.SaveChangesAsync();

        return RedirectToRoll(roll.Id);
    }

    private async Task<Roll?> GetRollAsync(Guid rollId)
    {
        return await _context.Rolls
            .Include(x => x.Fabric)
            .Include(x => x.Cell)
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.Id == rollId);
    }

    private void AddTransaction(
        Roll roll,
        RollTransactionType type,
        decimal length,
        string comment)
    {
        _context.RollTransactions.Add(
            new RollTransaction
            {
                Id = Guid.NewGuid(),
                RollId = roll.Id,
                Type = type,
                Length = length,
                CreatedAt = DateTime.UtcNow,
                Comment = comment
            });
    }

    private IActionResult RedirectToRoll(Guid rollId)
    {
        return RedirectToAction(
            "Details",
            "Rolls",
            new { id = rollId });
    }
}