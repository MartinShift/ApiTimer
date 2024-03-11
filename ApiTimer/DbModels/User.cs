using Microsoft.AspNetCore.Identity;

namespace ApiTimer.DbModels
{
    public class User : IdentityUser<int>
    {
        public virtual ImageFile? Logo { get; set; }
        public string VisibleName { get; set; }
    }
}
