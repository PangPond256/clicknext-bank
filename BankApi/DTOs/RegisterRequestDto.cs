namespace BankApi.DTOs
{
    public class RegisterRequestDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public decimal InitialBalance { get; set; }
    }
}