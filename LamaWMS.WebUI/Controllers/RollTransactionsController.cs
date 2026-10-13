using Microsoft.AspNetCore.Authorization;
using LamaWMS.Domain.Entities;
using LamaWMS.Domain.Enums;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Security.Claims;

namespace LamaWMS.WebUI.Controllers;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class RollTransactionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RollTransactionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.RollTransactions
            .Include(x => x.Roll)
                .ThenInclude(x => x.Fabric)
            .Include(x => x.Roll)
                .ThenInclude(x => x.Cell)
            .Include(x => x.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Roll.Number.Contains(search) ||
                x.Roll.Fabric.Name.Contains(search) ||
                (x.Product != null && x.Product.Name.Contains(search)) ||
                (x.RequestNumber != null && x.RequestNumber.Contains(search)) ||
                (x.Comment != null && x.Comment.Contains(search)));
        }

        var transactions = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var userIds = transactions
            .Where(x => x.UserId != null)
            .Select(x => x.UserId!)
            .Distinct()
            .ToList();

        ViewBag.Users = await _context.Users
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(
                x => x.Id,
                x => x.FullName);

        ViewBag.Search = search;

        return View(transactions);
    }

    // ------------------------------------------------------------
    // ПРИХОД
    // Дополнительное поступление ткани на существующий рулон.
    // ------------------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Receipt(Guid rollId)
    {
        var roll = await GetRollAsync(rollId);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Operator")]
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
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Return(Guid rollId)
    {
        var roll = await GetRollAsync(rollId);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Operator")]
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
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> WriteOff(Guid rollId)
    {
        var roll = await GetRollAsync(rollId);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Operator")]
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
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Adjustment(Guid rollId)
    {
        var roll = await GetRollAsync(rollId);

        if (roll == null)
            return NotFound();

        return View(roll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Operator")]
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
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
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

    //------------------------------------------------------------
    // ЭКСПОРТ В EXCEL
    //------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? search)
    {
        var query = _context.RollTransactions
            .Include(x => x.Roll)
                .ThenInclude(x => x.Fabric)
            .Include(x => x.Roll)
                .ThenInclude(x => x.Cell)
            .Include(x => x.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Roll.Number.Contains(search) ||
                x.Roll.Fabric.Name.Contains(search) ||
                (x.Product != null &&
                 x.Product.Name.Contains(search)) ||
                (x.RequestNumber != null &&
                 x.RequestNumber.Contains(search)) ||
                (x.Comment != null &&
                 x.Comment.Contains(search)));
        }

        var transactions = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        // Получаем ФИО пользователей, выполнивших операции.
        var userIds = transactions
            .Where(x => x.UserId != null)
            .Select(x => x.UserId!)
            .Distinct()
            .ToList();

        var users = await _context.Users
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName);

        using var workbook = new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("История операций");

        // Заголовки
        worksheet.Cell(1, 1).Value = "Дата";
        worksheet.Cell(1, 2).Value = "Операция";
        worksheet.Cell(1, 3).Value = "Рулон";
        worksheet.Cell(1, 4).Value = "Ткань";
        worksheet.Cell(1, 5).Value = "Количество, м";
        worksheet.Cell(1, 6).Value = "№ заявки";
        worksheet.Cell(1, 7).Value = "Изделие";
        worksheet.Cell(1, 8).Value = "Комментарий";
        worksheet.Cell(1, 9).Value = "Пользователь";

        var row = 2;

        foreach (var transaction in transactions)
        {
            worksheet.Cell(row, 1).Value =
                transaction.CreatedAt.ToLocalTime();

            worksheet.Cell(row, 2).Value =
                GetTransactionName(transaction.Type);

            worksheet.Cell(row, 3).Value =
                transaction.Roll.Number;

            worksheet.Cell(row, 4).Value =
                transaction.Roll.Fabric.Name;

            worksheet.Cell(row, 5).Value =
                GetSignedLength(
                    transaction.Type,
                    transaction.Length);

            worksheet.Cell(row, 6).Value =
                transaction.RequestNumber ?? string.Empty;

            worksheet.Cell(row, 7).Value =
                transaction.Product?.Name ?? string.Empty;

            worksheet.Cell(row, 8).Value =
                transaction.Comment ?? string.Empty;

            worksheet.Cell(row, 9).Value =
                transaction.UserId != null &&
                users.TryGetValue(
                    transaction.UserId, out var fullName)
                    ? fullName
                    : "Не указан";

            row++;
        }

        // Заголовок таблицы
        var header = worksheet.Range(1, 1, 1, 9);

        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Формат даты
        worksheet.Column(1)
            .Style.DateFormat.Format = "dd.MM.yyyy HH:mm";

        // Формат количества
        worksheet.Column(5)
            .Style.NumberFormat.Format = "+0.00;-0.00;0.00";

        // Автоматическая ширина колонок
        worksheet.Columns().AdjustToContents();

        // Ограничиваем ширину комментария
        if (worksheet.Column(8).Width > 50)
            worksheet.Column(8).Width = 50;

        // Фиксируем заголовок
        worksheet.SheetView.FreezeRows(1);

        // Автофильтр
        if (row > 2)
        {
            worksheet.Range(1, 1, row - 1, 9)
                .SetAutoFilter();
        }

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        var fileName =
            $"transactions_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    //---------------------------------------------------------------------------------
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ GetTransactionName и GetSignedLength для экспорта в Excel
    //---------------------------------------------------------------------------------
    private static string GetTransactionName(
    RollTransactionType type)
    {
        return type switch
        {
            RollTransactionType.Receipt =>
                "Приход",

            RollTransactionType.Consumption =>
                "В производство",

            RollTransactionType.Return =>
                "Возврат",

            RollTransactionType.WriteOff =>
                "Списание",

            RollTransactionType.Adjustment =>
                "Корректировка",

            _ => type.ToString()
        };
    }

    private static decimal GetSignedLength(
        RollTransactionType type,
        decimal length)
    {
        return type switch
        {
            RollTransactionType.Receipt =>
                Math.Abs(length),

            RollTransactionType.Return =>
                Math.Abs(length),

            RollTransactionType.Consumption =>
                -Math.Abs(length),

            RollTransactionType.WriteOff =>
                -Math.Abs(length),

            // У Adjustment Length уже является разницей:
            // может быть как положительным, так и отрицательным.
            RollTransactionType.Adjustment =>
                length,

            _ => length
        };
    }
}