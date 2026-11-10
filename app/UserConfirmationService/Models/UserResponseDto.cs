namespace UserConfirmationService.Models
{
    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int RegisteredObjects { get; set; }
    }
}