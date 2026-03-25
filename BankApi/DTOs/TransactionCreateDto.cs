namespace BankApi.DTOs
{
    public class TransactionCreateDto
    {
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
    }
}