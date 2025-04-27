using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KTU_forum.Data;
using KTU_forum.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KTU_forum.Pages
{
    public class RoomsModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;
        public void OnGet()
        {
        }
    }
}
