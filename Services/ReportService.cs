using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Factors.Web.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DashboardViewModel GetDashboardData()
    {
        var factors = _context.Factors
            .Include(f => f.Person)
            .Include(f => f.FactorItems)
            .OrderByDescending(f => f.CreateDate)
            .ToList();

        var dashboard = new DashboardViewModel
        {
            TotalFactors = factors.Count,
            TotalPersons = _context.Persons.Count(),
            TotalProducts = _context.Products.Count(),
            TotalCategories = _context.ProductCategories.Count(),
            TotalPacks = _context.ProductPacks.Count(),
            TotalUsers = _context.Users.Count(),
            TotalSalesAmount = factors.Sum(f => (f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)),
            RecentFactors = factors.Take(10).Select(f => new FactorViewModel
            {
                Id = f.Id,
                PersonId = f.PersonId,
                PersonName = f.Person?.PersonName ?? "",
                PersianCreateDate = PersianDateService.ToPersian(f.CreateDate),
                TotalAmount = f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0,
                TotalItems = f.FactorItems?.Count ?? 0
            }).ToList()
        };

        // Top Products (materialize first because SQLite cannot aggregate decimal)
        var factorItemsData = _context.FactorItems
            .Where(fi => fi.SalableId != null)
            .Include(fi => fi.Product)
            .ToList();

        var topProducts = factorItemsData
            .Where(fi => fi.Product != null)
            .GroupBy(fi => new { fi.SalableId, fi.Product!.Name })
            .Select(g => new TopProductViewModel
            {
                ProductName = g.Key.Name,
                TotalQty = g.Sum(x => x.Qty),
                TotalAmount = g.Sum(x => x.Price * x.Qty)
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(5)
            .ToList();

        dashboard.TopProducts = topProducts;

        // Monthly Sales (last 6 months)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var monthlySales = factors
            .Where(f => f.CreateDate >= sixMonthsAgo)
            .GroupBy(f => new { f.CreateDate.Year, f.CreateDate.Month })
            .Select(g => new MonthlySalesViewModel
            {
                Month = $"{g.Key.Year}/{g.Key.Month:00}",
                FactorCount = g.Count(),
                TotalAmount = g.Sum(f => f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0)
            })
            .OrderBy(x => x.Month)
            .ToList();

        dashboard.MonthlySales = monthlySales;

        return dashboard;
    }

    public byte[] GenerateFactorReportPdf(FactorViewModel factor)
    {
        factor ??= new FactorViewModel();
        factor.Items ??= new List<FactorItemViewModel>();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(compose => ComposeHeader(compose, $"فاکتور شماره {factor.Id}"));

                page.Content().Element(compose => ComposeFactorContent(compose, factor));

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحه ").FontSize(9);
                    x.CurrentPageNumber().FontSize(9);
                    x.Span(" از ").FontSize(9);
                    x.TotalPages().FontSize(9);
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateSalesReportPdf(ReportFilterViewModel filter, List<FactorViewModel> factors)
    {
        filter ??= new ReportFilterViewModel();
        factors ??= new List<FactorViewModel>();

        var fromDateStr = filter.FromDate.HasValue ? PersianDateService.ToPersian(filter.FromDate.Value) : "نامحدود";
        var toDateStr = filter.ToDate.HasValue ? PersianDateService.ToPersian(filter.ToDate.Value) : "نامحدود";
        var totalAmount = factors.Sum(f => f.TotalAmount);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(compose => ComposeHeader(compose, "گزارش فروش"));

                page.Content().Element(compose =>
                {
                    compose.Column(column =>
                    {
                        column.Spacing(10);

                        // Filter info
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"از تاریخ: {fromDateStr}");
                            row.RelativeItem().Text($"تا تاریخ: {toDateStr}");
                            row.RelativeItem().Text($"تعداد فاکتورها: {factors.Count}");
                        });

                        column.Item().LineHorizontal(1);

                        // Summary
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"مبلغ کل فروش: {totalAmount:N0} ریال");
                        });

                        column.Item().LineHorizontal(1);

                        if (factors.Count > 0)
                        {
                            // Factors table
                            column.Item().Element(compose2 => ComposeFactorsTable(compose2, factors));
                        }
                        else
                        {
                            column.Item().Text("داده‌ای برای نمایش وجود ندارد").FontSize(12).FontColor(Colors.Grey.Darken1);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحه ").FontSize(9);
                    x.CurrentPageNumber().FontSize(9);
                    x.Span(" از ").FontSize(9);
                    x.TotalPages().FontSize(9);
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateProductReportPdf(ReportFilterViewModel filter, List<ProductViewModel> products)
    {
        products ??= new List<ProductViewModel>();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(compose => ComposeHeader(compose, "گزارش محصولات"));

                page.Content().Element(compose =>
                {
                    compose.Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Text($"تعداد محصولات: {products.Count}");
                        column.Item().LineHorizontal(1);

                        if (products.Count > 0)
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(80);
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("#");
                                    header.Cell().Element(CellStyle).Text("نام محصول");
                                    header.Cell().Element(CellStyle).Text("کد");
                                    header.Cell().Element(CellStyle).Text("دسته‌بندی");
                                });

                                for (int i = 0; i < products.Count; i++)
                                {
                                    var p = products[i];
                                    table.Cell().Element(CellStyle).Text((i + 1).ToString());
                                    table.Cell().Element(CellStyle).Text(p.Name ?? "");
                                    table.Cell().Element(CellStyle).Text(p.Code ?? "");
                                    table.Cell().Element(CellStyle).Text(p.CategoryName ?? "");
                                }
                            });
                        }
                        else
                        {
                            column.Item().Text("داده‌ای برای نمایش وجود ندارد").FontSize(12).FontColor(Colors.Grey.Darken1);
                        }
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, string title)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(title ?? "").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"تاریخ: {PersianDateService.Now}").FontSize(9);
                column.Item().LineHorizontal(2);
            });
        });
    }

    private void ComposeFactorContent(IContainer container, FactorViewModel factor)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            // Factor info
            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"شماره فاکتور: {factor.Id}");
                row.RelativeItem().Text($"تاریخ: {factor.PersianCreateDate ?? "-"}");
                row.RelativeItem().Text($"مشتری: {factor.PersonName ?? "-"}");
            });

            column.Item().LineHorizontal(1);

            // Items table
            if (factor.Items != null && factor.Items.Count > 0)
            {
                column.Item().Element(compose => ComposeFactorItemsTable(compose, factor.Items));
            }
            else
            {
                column.Item().Text("آیتمی وجود ندارد").FontSize(10).FontColor(Colors.Grey.Darken1);
            }

            column.Item().LineHorizontal(1);

            // Total
            column.Item().AlignLeft().Text($"جمع کل: {factor.TotalAmount:N0} ریال").FontSize(14).Bold();
        });
    }

    private void ComposeFactorItemsTable(IContainer container, List<FactorItemViewModel> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);
                columns.RelativeColumn();
                columns.ConstantColumn(60);
                columns.ConstantColumn(100);
                columns.ConstantColumn(120);
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("#");
                header.Cell().Element(CellStyle).Text("نام کالا");
                header.Cell().Element(CellStyle).Text("تعداد");
                header.Cell().Element(CellStyle).Text("قیمت واحد");
                header.Cell().Element(CellStyle).Text("جمع");
            });

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                table.Cell().Element(CellStyle).Text((i + 1).ToString());
                table.Cell().Element(CellStyle).Text(item.ProductName ?? "");
                table.Cell().Element(CellStyle).Text(item.Qty.ToString("N0"));
                table.Cell().Element(CellStyle).Text(item.Price.ToString("N0"));
                table.Cell().Element(CellStyle).Text(item.TotalPrice.ToString("N0"));
            }
        });
    }

    private void ComposeFactorsTable(IContainer container, List<FactorViewModel> factors)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);
                columns.ConstantColumn(60);
                columns.RelativeColumn();
                columns.ConstantColumn(100);
                columns.ConstantColumn(60);
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("#");
                header.Cell().Element(CellStyle).Text("شماره");
                header.Cell().Element(CellStyle).Text("مشتری");
                header.Cell().Element(CellStyle).Text("مبلغ کل");
                header.Cell().Element(CellStyle).Text("تاریخ");
            });

            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                table.Cell().Element(CellStyle).Text((i + 1).ToString());
                table.Cell().Element(CellStyle).Text(f.Id.ToString());
                table.Cell().Element(CellStyle).Text(f.PersonName ?? "");
                table.Cell().Element(CellStyle).Text(f.TotalAmount.ToString("N0"));
                table.Cell().Element(CellStyle).Text(f.PersianCreateDate ?? "");
            }
        });
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.DefaultTextStyle(x => x.FontSize(10)).BorderVertical(1).BorderHorizontal(1).PaddingVertical(5).PaddingHorizontal(5);
    }
}
