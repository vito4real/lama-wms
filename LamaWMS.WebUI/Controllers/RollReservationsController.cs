using LamaWMS.Domain.Entities;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        decimal reservedLength)
    {
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