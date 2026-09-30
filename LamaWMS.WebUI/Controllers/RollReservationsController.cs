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
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.RollReservations
            .Include(x => x.Roll)
                .ThenInclude(x => x.Cell)
            .Include(x => x.ProductFabric)
                .ThenInclude(x => x.Product)
            .Include(x => x.ProductFabric)
                .ThenInclude(x => x.Fabric)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.RequestNumber.Contains(search) ||
                x.ProductFabric.Product.Name.Contains(search) ||
                x.ProductFabric.Fabric.Name.Contains(search) ||
                x.Roll.Number.Contains(search) ||
                (x.Roll.Cell != null &&
                 x.Roll.Cell.Code.Contains(search)));
        }

        var reservations = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        ViewBag.Search = search;

        return View(reservations);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
    Guid productId,
    string? requestNumber)
    {
        var product = await _context.Products
            .Include(x => x.ProductFabrics)
                .ThenInclude(x => x.Fabric)
            .Include(x => x.ProductFabrics)
                .ThenInclude(x => x.RollReservations)
            .FirstOrDefaultAsync(x => x.Id == productId);

        if (product == null)
            return NotFound();

        var fabricIds = product.ProductFabrics
            .Select(x => x.FabricId)
            .Distinct()
            .ToList();

        var rolls = await _context.Rolls
            .Include(x => x.Cell)
            .Include(x => x.Reservations)
            .Where(x => fabricIds.Contains(x.FabricId))
            .OrderBy(x => x.Number)
            .ToListAsync();

        ViewBag.Product = product;
        ViewBag.Rolls = rolls;
        ViewBag.RequestNumber = requestNumber?.Trim();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    Guid productId,
    string requestNumber,
    Dictionary<Guid, decimal> reservations)
    {
        requestNumber =
            requestNumber?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(requestNumber))
        {
            TempData["Error"] =
                "Необходимо указать номер заявки.";

            return RedirectToAction(
                nameof(Create),
                new { productId });
        }

        if (requestNumber.Length > 100)
        {
            TempData["Error"] =
                "Номер заявки не может быть длиннее 100 символов.";

            return RedirectToAction(
                nameof(Create),
                new { productId });
        }

        // Оставляем только реально введённые значения.
        var selectedReservations = reservations
            .Where(x => x.Value > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value);

        if (!selectedReservations.Any())
        {
            TempData["Error"] =
                "Необходимо указать хотя бы один резерв.";

            return RedirectToAction(
                nameof(Create),
                new { productId });
        }

        // Загружаем изделие и его потребности.
        var product = await _context.Products
            .Include(x => x.ProductFabrics)
                .ThenInclude(x => x.Fabric)
            .Include(x => x.ProductFabrics)
                .ThenInclude(x => x.RollReservations)
            .FirstOrDefaultAsync(x => x.Id == productId);

        if (product == null)
            return NotFound();

        // Загружаем все выбранные рулоны.
        var rollIds =
            selectedReservations.Keys.ToList();

        var rolls = await _context.Rolls
            .Include(x => x.Reservations)
            .Where(x => rollIds.Contains(x.Id))
            .ToListAsync();

        if (rolls.Count != rollIds.Count)
        {
            TempData["Error"] =
                "Один или несколько выбранных рулонов не найдены.";

            return RedirectToAction(
                nameof(Create),
                new { productId });
        }

        var newReservations =
            new List<RollReservation>();

        /*
         * Один рулон принадлежит одной ткани.
         *
         * Поэтому по FabricId рулона мы определяем,
         * какую потребность ProductFabric он закрывает.
         */
        foreach (var roll in rolls)
        {
            var reservedLength =
                selectedReservations[roll.Id];

            if (reservedLength <= 0)
                continue;

            var productFabric =
                product.ProductFabrics
                    .FirstOrDefault(x =>
                        x.FabricId == roll.FabricId);

            if (productFabric == null)
            {
                TempData["Error"] =
                    $"Рулон {roll.Number} содержит ткань, " +
                    "которая не используется в этом изделии.";

                return RedirectToAction(
                    nameof(Create),
                    new { productId });
            }

            // Сколько уже занято на этом рулоне.
            var reservedOnRoll =
                roll.Reservations
                    .Sum(x => x.ReservedLength);

            var availableLength =
                roll.CurrentLength - reservedOnRoll;

            if (reservedLength > availableLength)
            {
                TempData["Error"] =
                    $"В рулоне {roll.Number} доступно только " +
                    $"{availableLength:0.00} м.";

                return RedirectToAction(
                    nameof(Create),
                    new { productId });
            }

            newReservations.Add(
                new RollReservation
                {
                    Id = Guid.NewGuid(),
                    RollId = roll.Id,
                    ProductFabricId = productFabric.Id,
                    RequestNumber = requestNumber,
                    ReservedLength = reservedLength,
                    CreatedAt = DateTime.UtcNow
                });
        }

        /*
         * Теперь проверяем суммарное количество.
         *
         * Важно делать это после построения всего списка,
         * потому что одна ткань может резервироваться
         * сразу с нескольких рулонов.
         */
        foreach (var productFabric in product.ProductFabrics)
        {
            var alreadyReservedForRequest =
                productFabric.RollReservations
                .Where(x => x.RequestNumber == requestNumber)
                .Sum(x => x.ReservedLength);

            var remainingNeed =
                productFabric.RequiredLength -
                alreadyReservedForRequest;

            var newReserved =
                newReservations
                    .Where(x =>
                        x.ProductFabricId == productFabric.Id)
                    .Sum(x => x.ReservedLength);

            if (newReserved > remainingNeed)
            {
                TempData["Error"] =
                    $"Для ткани \"{productFabric.Fabric.Name}\" " +
                    $"по заявке №{requestNumber} " +
                    $"осталось зарезервировать только " +
                    $"{remainingNeed:0.00} м.";

                return RedirectToAction(
                    nameof(Create),
                    new { productId });
            }
        }

        if (!newReservations.Any())
        {
            TempData["Error"] =
                "Необходимо указать хотя бы один резерв.";

            return RedirectToAction(
                nameof(Create),
                new { productId });
        }

        _context.RollReservations.AddRange(
            newReservations);

        await _context.SaveChangesAsync();

        return RedirectToAction(
            "Details",
            "Products",
            new { id = productId });
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

        return RedirectToAction(nameof(Index));
    }
}