namespace WebArsip.Core.DTOs
{
    public class RoleReadDto
    {
        public int RoleId { get; set; }          // map ke Role.Id
        public string RoleName { get; set; } = string.Empty; // map ke Role.Name
    }
}