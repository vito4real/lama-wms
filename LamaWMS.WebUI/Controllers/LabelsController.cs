using System.Security.Claims;
using LamaWMS.Domain.Entities;
using LamaWMS.Infrastructure;
using LamaWMS.WebUI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LamaWMS.WebUI.Controllers;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class LabelsController : Controller
{
    private readonly ApplicationDbContext _context;
    public LabelsController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.FurnitureLabels.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x => x.RequestNumber.Contains(search) ||
                x.Name.Contains(search) || x.Complectation.Contains(search) ||
                x.Designation.Contains(search));
        }
        var labels = await query.OrderByDescending(x => x.CreatedAt).Take(300).ToListAsync();
        var ids = labels.Where(x => x.CreatedByUserId != null).Select(x => x.CreatedByUserId!).Distinct().ToList();
        ViewBag.Users = await _context.Users.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FullName);
        ViewBag.Search = search;
        return View(labels);
    }

    [HttpGet, Authorize(Roles = "Admin,Operator")]
    public IActionResult Create(Guid? copyFrom = null)
    {
        if (copyFrom == null) return View(new FurnitureLabelInput());
        var label = _context.FurnitureLabels.AsNoTracking().FirstOrDefault(x => x.Id == copyFrom.Value);
        if (label == null) return NotFound();
        return View(new FurnitureLabelInput
        {
            Name = label.Name, Complectation = label.Complectation,
            Designation = label.Designation, FunctionalPurpose = label.FunctionalPurpose,
            DeclarationNumber = label.DeclarationNumber,
            DeclarationValidFrom = label.DeclarationValidFrom,
            DeclarationValidTo = label.DeclarationValidTo,
            RequestNumber = label.RequestNumber, FabricGroup = label.FabricGroup,
            ManufactureDate = DateOnly.FromDateTime(DateTime.Today), Copies = 1
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Create(FurnitureLabelInput input)
    {
        if (input.DeclarationValidFrom == default || input.DeclarationValidTo == default ||
            input.DeclarationValidFrom > input.DeclarationValidTo)
            ModelState.AddModelError(nameof(input.DeclarationValidTo), "Проверьте даты действия декларации.");
        if (input.ManufactureDate == default)
            ModelState.AddModelError(nameof(input.ManufactureDate), "Укажите дату изготовления.");
        if (!ModelState.IsValid) return View(input);

        var label = new FurnitureLabel
        {
            Id = Guid.NewGuid(), Name = input.Name.Trim(),
            Complectation = input.Complectation.Trim(), Designation = input.Designation.Trim(),
            FunctionalPurpose = input.FunctionalPurpose.Trim(),
            DeclarationNumber = input.DeclarationNumber.Trim(),
            DeclarationValidFrom = input.DeclarationValidFrom,
            DeclarationValidTo = input.DeclarationValidTo,
            RequestNumber = input.RequestNumber.Trim(), FabricGroup = input.FabricGroup.Trim(),
            ManufactureDate = input.ManufactureDate, Copies = input.Copies,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };
        _context.FurnitureLabels.Add(label);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Этикетка сохранена. Для печати нажмите «Печать».";
        return RedirectToAction(nameof(Details), new { id = label.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var label = await _context.FurnitureLabels.AsNoTracking()
            .Include(x => x.PrintEvents).FirstOrDefaultAsync(x => x.Id == id);
        if (label == null) return NotFound();
        var ids = label.PrintEvents.Where(x => x.RequestedByUserId != null)
            .Select(x => x.RequestedByUserId!).Distinct().ToList();
        ViewBag.Users = await _context.Users.Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName);
        return View(label);
    }

    [HttpGet]
    public async Task<IActionResult> Preview(Guid id)
    {
        var label = await _context.FurnitureLabels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (label == null) return NotFound();
        return View("Print", label);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Print(Guid id, int copies = 1)
    {
        var label = await _context.FurnitureLabels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (label == null) return NotFound();
        if (copies is < 1 or > 100) return BadRequest("Количество копий: от 1 до 100.");

        _context.FurnitureLabelPrintEvents.Add(new FurnitureLabelPrintEvent
        {
            Id = Guid.NewGuid(), FurnitureLabelId = id, Copies = copies,
            RequestedAt = DateTime.UtcNow,
            RequestedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        });
        await _context.SaveChangesAsync();
        ViewBag.Copies = copies;
        return View("Print", label);
    }
}
