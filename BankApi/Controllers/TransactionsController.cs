using System.Security.Claims;
using BankApi.Data;
using BankApi.DTOs;
using BankApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return null;

            if (!int.TryParse(userId, out var parsedUserId))
                return null;

            return parsedUserId;
        }

        private async Task<decimal> CalculateBalanceAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                throw new Exception("ไม่พบผู้ใช้ในระบบ");

            var transactions = await _context.Transactions
                .Where(x => x.UserId == userId)
                .ToListAsync();

            var deposit = transactions
                .Where(x => x.Type == "deposit")
                .Sum(x => x.Amount);

            var withdraw = transactions
                .Where(x => x.Type == "withdraw")
                .Sum(x => x.Amount);

            return user.InitialBalance + deposit - withdraw;
        }

        private async Task<decimal> CalculateBalanceWithoutTransactionAsync(int userId, int transactionId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                throw new Exception("ไม่พบผู้ใช้ในระบบ");

            var transactions = await _context.Transactions
                .Where(x => x.UserId == userId && x.Id != transactionId)
                .ToListAsync();

            var deposit = transactions
                .Where(x => x.Type == "deposit")
                .Sum(x => x.Amount);

            var withdraw = transactions
                .Where(x => x.Type == "withdraw")
                .Sum(x => x.Amount);

            return user.InitialBalance + deposit - withdraw;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "ไม่พบข้อมูลผู้ใช้" });

            var items = await _context.Transactions
                .Where(x => x.UserId == userId.Value)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.Type,
                    x.Amount,
                    x.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                message = "ดึงรายการสำเร็จ",
                transactions = items
            });
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "ไม่พบข้อมูลผู้ใช้" });

            var balance = await CalculateBalanceAsync(userId.Value);

            return Ok(new
            {
                message = "ดึงยอดเงินสำเร็จ",
                balance
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransactionCreateDto dto)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "ไม่พบข้อมูลผู้ใช้" });

            if (dto == null)
                return BadRequest(new { message = "ข้อมูลไม่ถูกต้อง" });

            var type = dto.Type?.Trim().ToLower();

            if (dto.Amount <= 0 || dto.Amount > 100000)
                return BadRequest(new { message = "จำนวนเงินต้องอยู่ระหว่าง 1 - 100000" });

            if (type != "deposit" && type != "withdraw")
                return BadRequest(new { message = "ประเภทรายการต้องเป็น deposit หรือ withdraw" });

            var currentBalance = await CalculateBalanceAsync(userId.Value);

            if (type == "withdraw" && dto.Amount > currentBalance)
                return BadRequest(new { message = "ยอดเงินไม่เพียงพอ" });

            var item = new Transaction
            {
                UserId = userId.Value,
                Type = type,
                Amount = dto.Amount,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(item);
            await _context.SaveChangesAsync();

            var newBalance = await CalculateBalanceAsync(userId.Value);

            return Ok(new
            {
                message = "สร้างรายการสำเร็จ",
                transaction = new
                {
                    item.Id,
                    item.UserId,
                    item.Type,
                    item.Amount,
                    item.CreatedAt
                },
                balance = newBalance
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, [FromBody] TransactionUpdateDto dto)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "ไม่พบข้อมูลผู้ใช้" });

            if (dto == null)
                return BadRequest(new { message = "ข้อมูลไม่ถูกต้อง" });

            if (dto.Amount <= 0 || dto.Amount > 100000)
                return BadRequest(new { message = "จำนวนเงินต้องอยู่ระหว่าง 1 - 100000" });

            var item = await _context.Transactions
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value);

            if (item == null)
                return NotFound(new { message = "ไม่พบรายการ" });

            var balanceWithoutCurrentItem = await CalculateBalanceWithoutTransactionAsync(userId.Value, id);

            decimal newBalance;

            if (item.Type == "deposit")
            {
                newBalance = balanceWithoutCurrentItem + dto.Amount;
            }
            else if (item.Type == "withdraw")
            {
                newBalance = balanceWithoutCurrentItem - dto.Amount;
            }
            else
            {
                return BadRequest(new { message = "ประเภทรายการไม่ถูกต้อง" });
            }

            if (newBalance < 0)
                return BadRequest(new { message = "ไม่สามารถแก้ไขรายการได้ เนื่องจากยอดเงินคงเหลือจะติดลบ" });

            item.Amount = dto.Amount;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "แก้ไขรายการสำเร็จ",
                transaction = new
                {
                    item.Id,
                    item.UserId,
                    item.Type,
                    item.Amount,
                    item.CreatedAt
                },
                balance = newBalance
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "ไม่พบข้อมูลผู้ใช้" });

            var item = await _context.Transactions
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value);

            if (item == null)
                return NotFound(new { message = "ไม่พบรายการ" });

            var balanceWithoutCurrentItem = await CalculateBalanceWithoutTransactionAsync(userId.Value, id);

            if (balanceWithoutCurrentItem < 0)
                return BadRequest(new { message = "ไม่สามารถลบรายการได้ เนื่องจากยอดเงินคงเหลือจะติดลบ" });

            _context.Transactions.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "ลบรายการสำเร็จ",
                balance = balanceWithoutCurrentItem
            });
        }
    }
}