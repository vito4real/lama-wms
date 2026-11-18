
using Microsoft.Playwright;

namespace LamaWMS.WebUI.Services;

public class FurnitureLabelExportService
{
    private readonly IWebHostEnvironment _environment;

    public FurnitureLabelExportService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<byte[]> GeneratePdfAsync(string html)
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true });

        var page = await browser.NewPageAsync();

        await ConfigurePageAsync(page, html);

        await page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            Media = Media.Print
        });

        return await page.PdfAsync(new PagePdfOptions
        {
            PreferCSSPageSize = true,
            PrintBackground = true,
            DisplayHeaderFooter = false,
            Margin = new Margin
            {
                Top = "0",
                Right = "0",
                Bottom = "0",
                Left = "0"
            }
        });
    }

    public async Task<byte[]> GeneratePngAsync(string html)
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true });

        var context = await browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = 1200,
                    Height = 1400
                },
                DeviceScaleFactor = 300f / 96f
            });

        var page = await context.NewPageAsync();

        await ConfigurePageAsync(page, html);

        await page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = """
                .sheet {
                    zoom: 1 !important;
                    margin: 0 !important;
                    box-shadow: none !important;
                }
                .controls { display: none !important; }
                body { background: white !important; }
                """
        });

        var label = page.Locator(".sheet").First;

        return await label.ScreenshotAsync(
            new LocatorScreenshotOptions
            {
                Type = ScreenshotType.Png,
                Animations = ScreenshotAnimations.Disabled
            });
    }

    private async Task ConfigurePageAsync(IPage page, string html)
    {
        var logoPath = Path.Combine(
            _environment.WebRootPath,
            "img",
            "eac.svg");

        if (!File.Exists(logoPath))
            throw new FileNotFoundException(
                "Не найден логотип EAC.", logoPath);

        // Читаем локальный SVG и встраиваем его в HTML.
        var svgBytes = await File.ReadAllBytesAsync(logoPath);
        var svgBase64 = Convert.ToBase64String(svgBytes);

        var logoDataUri = $"data:image/svg+xml;base64,{svgBase64}";

        // Заменяем путь к логотипу на встроенное изображение.
        html = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"(<img\b[^>]*\bsrc\s*=\s*[""'])[^""']*/img/eac\.svg([""'])",
            m => m.Groups[1].Value + logoDataUri + m.Groups[2].Value,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Запрещаем сетевые запросы при генерации.
        await page.RouteAsync("**/*", async route =>
        {
            await route.AbortAsync();
        });

        // Не ждём загрузки всех внешних ресурсов.
        await page.SetContentAsync(
            html,
            new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 10000
            });

        // Проверяем, что логотип действительно загрузился.
        await page.Locator(".eac img").EvaluateAsync(
            @"async img => {
            if (!img.complete) {
                await new Promise((resolve, reject) => {
                    img.addEventListener('load', resolve, {once:true});
                    img.addEventListener('error', reject, {once:true});
                });
            }

            if (!img.naturalWidth) {
                throw new Error('Не удалось загрузить EAC');
            }
        }",
            null,
            new LocatorEvaluateOptions
            {
                Timeout = 10000
            });

        // Дожидаемся готовности шрифтов.
        await page.EvaluateAsync(
            "() => document.fonts.ready");
    }
}
