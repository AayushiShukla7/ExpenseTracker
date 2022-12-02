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
                .OrderByDescending(l => l.amount)
                .ToList();

            /* Spline Chart  - Income Vs Expense */
            //Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(x => x.Category.Type == "Income")
                .GroupBy(y => y.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(x => x.Category.Type == "Expense")
                .GroupBy(y => y.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            //Combine Income and Expense
            string[] Last7Days = Enumerable.Range(0, 7).Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = (from day in Last7Days
                                      join income in IncomeSummary on day equals income.day
                                      into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into dayExpenseJoined
                                      from expense in dayExpenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      })
                                      .ToList();


            return View();
        }
    }

    public class SplineChartData
    {
        public string day { get; set; }
        public int income { get; set; }
        public int expense { get; set; }
    }
}
