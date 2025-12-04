namespace WebArsip.Mvc.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        public int UserId { get; set; }
        public string NewPassword { get; set; } = "";
    }
}