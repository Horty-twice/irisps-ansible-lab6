namespace UserConfirmationService.Models
{
    public class CreateUserDto
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}