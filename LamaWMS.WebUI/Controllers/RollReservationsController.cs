using LamaWMS.Domain.Entities;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LamaWMS.Domain.Enums;

namespace LamaWMS.WebUI.Controllers;

public class RollReservationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RollReservationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid productFabricId)
    {
        var productFabric = await _context.ProductFabrics
            .Include(x => x.Product)
            .Include(x => x.Fabric)
            .Include(x => x.RollReservations)
            .FirstOrDefaultAsync(x => x.Id == productFabricId);

        if (productFabric == null)
            return NotFound();

        var rolls = await _context.Rolls
            .Include(x => x.Cell)
            .Include(x => x.Reservations)
            .Where(x => x.FabricId == productFabric.FabricId)
            .OrderBy(x => x.Number)
            .ToListAsync();

        ViewBag.ProductFabric = productFabric;
        ViewBag.Rolls = rolls;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Guid productFabricId,
        Guid rollId,
        decimal reservedLength,
        string requestNumber)
    {
        requestNumber = requestNumber?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(requestNumber))
        {
            TempData["Error"] =
                "Необходимо указать номер заявки.";

            return RedirectToAction(
                nameof(Create),
                new { productFabricId });
        }

        if (requestNumber.Length > 100)
        {
            TempData["Error"] =
                "Номер заявки не может быть длиннее 100 символов.";

            return RedirectToAction(
                nameof(Create),
                new { productFabricId });
        }

        if (reservedLength <= 0)
        {
            TempData["Error"] = "Количество для резервирования должно быть больше 0.";
            return RedirectToAction(nameof(Create), new { productFabricId });
        }

        var productFabric = await _context.ProductFabrics
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == productFabricId);

        if (productFabric == null)
            return NotFound();

        var roll = await _context.Rolls
            .FirstOrDefaultAsync(x => x.Id == rollId);

        if (roll == null)
            return NotFound();

        // Нельзя резервировать рулон другой ткани.
        if (roll.FabricId != productFabric.FabricId)
        {
            TempData["Error"] =
                "Выбранный рулон относится к другой ткани.";

            return RedirectToAction(
                nameof(Create),
                new { productFabricId });
        }

        // Сколько всего уже зарезервировано из этого рулона.
        var reservedOnRoll = await _context.RollReservations
            .Where(x => x.RollId == rollId)
            .SumAsync(x => (decimal?)x.ReservedLength) ?? 0m;

        var availableLength =
            roll.CurrentLength - reservedOnRoll;

        if (reservedLength > availableLength)
        {
            TempData["Error"] =
                $"В рулоне доступно только {availableLength:0.00} м.";

            return RedirectToAction(
                nameof(Create),
                new { productFabricId });
        }

        // Сколько уже зарезервировано для этой потребности изделия.
        var alreadyReserved = await _context.RollReservations
            .Where(x => x.ProductFabricId == productFabricId)
            .SumAsync(x => (decimal?)x.ReservedLength) ?? 0m;

        var remainingNeed =
            productFabric.RequiredLength - alreadyReserved;

        if (reservedLength > remainingNeed)
        {
            TempData["Error"] =
                $"Для изделия осталось зарезервировать только {remainingNeed:0.00} м.";

            return RedirectToAction(
                nameof(Create),
                new { productFabricId });
        }

        var reservation = new RollReservation
        {
            Id = Guid.NewGuid(),
            RollId = rollId,
            ProductFabricId = productFabricId,
            RequestNumber = requestNumber,
            ReservedLength = reservedLength,
            CreatedAt = DateTime.UtcNow
        };

        _context.RollReservations.Add(reservation);
        await _context.SaveChangesAsync();

        return RedirectToAction(
            "Details",
            "Products",
            new { id = productFabric.ProductId });
    }

    [HttpGet]
    public async Task<IActionResult> Consume(Guid id)
    {
        var reservation = await _context.RollReservations
            .Include(x => x.Roll)
                .ThenInclude(x => x.Fabric)
            .Include(x => x.ProductFabric)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (reservation == null)
            return NotFound();

        return View(reservation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Consume(Guid id, decimal consumedLength)
    {
        var reservation = await _context.RollReservations
            .Include(x => x.Roll)
            .Include(x => x.ProductFabric)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (reservation == null)
            return NotFound();

        if (consumedLength <= 0)
        {
            TempData["Error"] =
                "Количество для списания должно быть больше 0.";

            return RedirectToAction(
                nameof(Consume),
                new { id });
        }

        if (consumedLength > reservation.ReservedLength)
        {
            TempData["Error"] =
                $"Зарезервировано только {reservation.ReservedLength:0.00} м.";

            return RedirectToAction(
                nameof(Consume),
                new { id });
        }

        if (consumedLength > reservation.Roll.CurrentLength)
        {
            TempData["Error"] =
                $"Физический остаток рулона составляет только {reservation.Roll.CurrentLength:0.00} м.";

            return RedirectToAction(
                nameof(Consume),
                new { id });
        }

        // Физически уменьшаем остаток ткани на рулоне.
        reservation.Roll.CurrentLength -= consumedLength;

        // Уменьшаем резерв на фактически использованное количество.
        reservation.ReservedLength -= consumedLength;

        // Фиксируем физическое движение ткани.
        var transaction = new RollTransaction
        {
            Id = Guid.NewGuid(),
            RollId = reservation.RollId,
            ProductId = reservation.ProductFabric.ProductId,
            RequestNumber = reservation.RequestNumber,
            Type = RollTransactionType.Consumption,
            Length = consumedLength,
            CreatedAt = DateTime.UtcNow,
            Comment =
                $"Списание в производство по заявке №{reservation.RequestNumber}, " +
                $"изделие \"{reservation.ProductFabric.Product.Name}\""
        };

        _context.RollTransactions.Add(transaction);

        // Если весь резерв израсходован — резерв больше не нужен.
        if (reservation.ReservedLength == 0)
        {
            _context.RollReservations.Remove(reservation);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(
            "Details",
            "Rolls",
            new { id = reservation.RollId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var reservation = await _context.RollReservations
            .Include(x => x.ProductFabric)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (reservation == null)
            return NotFound();

        var productId =
            reservation.ProductFabric.ProductId;

        _context.RollReservations.Remove(reservation);
        await _context.SaveChangesAsync();

        return RedirectToAction(
            "Details",
            "Products",
            new { id = productId });
    }
}