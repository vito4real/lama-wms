using LamaWMS.Domain.Entities;
using LamaWMS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LamaWMS.WebUI.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products
            .Include(x => x.ProductFabrics)
                .ThenInclude(x => x.Fabric)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return View(products);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products
            .Include(x => x.ProductFabrics)
                .ThenInclude(x => x.Fabric)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
            return View(product);

        product.Id = Guid.NewGuid();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Product product)
    {
        if (id != product.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(product);

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
            return NotFound();

        var product = await _context.Products
            .Include(x => x.ProductFabrics)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> AddFabric(Guid productId)
    {
        var product = await _context.Products.FindAsync(productId);

        if (product == null)
            return NotFound();

        await LoadFabricsAsync();

        ViewBag.ProductName = product.Name;

        return View(new ProductFabric
        {
            ProductId = productId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFabric(ProductFabric productFabric)
    {
        if (!ModelState.IsValid)
        {
            await LoadFabricsAsync();

            var product = await _context.Products.FindAsync(productFabric.ProductId);
            ViewBag.ProductName = product?.Name;

            return View(productFabric);
        }

        var exists = await _context.ProductFabrics.AnyAsync(x =>
            x.ProductId == productFabric.ProductId &&
            x.FabricId == productFabric.FabricId);

        if (exists)
        {
            ModelState.AddModelError(
                string.Empty,
                "Эта ткань уже добавлена к изделию.");

            await LoadFabricsAsync();

            var product = await _context.Products.FindAsync(productFabric.ProductId);
            ViewBag.ProductName = product?.Name;

            return View(productFabric);
        }

        productFabric.Id = Guid.NewGuid();

        _context.ProductFabrics.Add(productFabric);
        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Details),
            new { id = productFabric.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFabric(Guid id)
    {
        var productFabric = await _context.ProductFabrics
            .FirstOrDefaultAsync(x => x.Id == id);

        if (productFabric == null)
            return NotFound();

        var productId = productFabric.ProductId;

        _context.ProductFabrics.Remove(productFabric);
        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Details),
            new { id = productId });
    }

    private async Task LoadFabricsAsync(Guid? selectedId = null)
    {
        var fabrics = await _context.Fabrics
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                DisplayName = x.Name
            })
            .ToListAsync();

        ViewBag.FabricId = new SelectList(
            fabrics,
            "Id",
            "DisplayName",
            selectedId);
    }
}