using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            //Last 7 days transactions
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(y => y.Category)
                .Where(x => x.Date >= StartDate && x.Date <= EndDate)
                .ToListAsync();

            //Total Income
            int TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(x => x.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");   // C0 -> Currency with 0 decimal precision (no decimal values)

            //Total Expense
            int TotalExpense = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(x => x.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            //Balance Amount
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;   //To avoid seeing negative balance (enclosed in brackets)
            ViewBag.Balance = string.Format(culture, "{0:C0}", Balance);

            //Doughnut Chart  - Expense by category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(x => x.Category.Type == "Expense")
                .GroupBy(y => y.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0")                     
                })
                .ToList();

            return View();
        }
    }
}
