using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ASPdemo.Entities;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System.Configuration;
using ASPdemo.Database;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ASPdemo.Pages;

public class AdministrationModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public string SearchTerm {  get; set; }

    [FromQuery]
    public int PageId { get; set; }

    [FromQuery]
    public int MaxId { get; set; }

    [BindProperty]
    public List<Role> roles { get; set; }

    public List<User> users { get; set; }

    [BindProperty]
    public Search? Search {  get; set; }

    public List<Role>? filteredRoles { get; set; }
    public class InputModel
    {
        public string? newUserName { get; set; }
    }
    public string? userName { get; set; }
    public string? userEmail { get; set; }
    

    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private ApplicationDbContext dbContext;

    public AdministrationModel(UserManager<User> userManager, ILogger<IndexModel> logger, SignInManager<User> signInManager, RoleManager<Role> roleManager)
    {
       _userManager = userManager;
       _roleManager = roleManager;
       _signInManager = signInManager;
       dbContext = new ApplicationDbContext();
       _logger = logger;
    }

    public async Task OnGet()
    {
        roles = new List<Role>(); 

        HttpClient client = new HttpClient();
        //await dbContext.Database.MigrateAsync();
        users = await dbContext.Users.ToListAsync();
        Console.WriteLine(users.Count);
        foreach (User user in users)
        {
             Console.WriteLine("User in users: ");
        }
        //users = (List<User>)await _userManager.GetUsersInRoleAsync("Admin");
        if (MaxId == 0)
        {
            MaxId = 10;
        }
        else
        {
            MaxId = MaxId + 10;
        }

        if (PageId == 0)
        {
            PageId = 1;
        }
        else
        {
            if (PageId == 1)
            {
                PageId = 0;
            }
            PageId = PageId + 10;
        }

        ViewData["MaxId"] = MaxId;
        ViewData["PageId"] = PageId;

        var url = new UriBuilder("http://127.0.0.1:5220/roles/" + MaxId + "/" + PageId);
        ViewData["Test"] = url;
        string tokens = null;
        try
        {
            try
            {
                tokens = await client.GetStringAsync(url.ToString()); 
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("404 ERROR");
            }
            
            if (tokens != null)
            {
                dynamic results = JsonConvert.DeserializeObject<dynamic>(tokens);
                foreach (dynamic result in results)
                {
                    var role = new Role();

                    var id = result.id;
                    var name = result.name;
                    var users = result.users;

                    role.Id = id;
                    role.Name = name;
                    role.Users = users;

                    roles.Add(role);
                }
            }
            else
            {
                Console.WriteLine("NO TOKENS TO DISPLAY");
            }
        }
        catch (Microsoft.Data.Sqlite.SqliteException) //catches if table is crapped
        {
            Console.WriteLine("TABLE DOES NOT EXIST");
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("HTTP REQUEST EXCEPTION ON CATEGORIES POST");
        }
        catch (WebException)
        {
            Console.WriteLine("WEB EXCEPTION ON CATEGORIES POST");
        }

        
    }
    public async Task<IActionResult> OnPost()
    {
        try
        {  
            //https://learn.microsoft.com/en-us/aspnet/core/razor-pages/?view=aspnetcore-8.0&tabs=visual-studio
            
            if (!ModelState.IsValid)
            {
                return Page(); 
            }

            if (Search != null)
            {
                ViewData["SearchTerm"] = Search.SearchTerm; 

                await OnGet(); 

                filteredRoles = new List<Role>();
                
                foreach (Role role in roles)
                {
                    if (role.Name.ToLower().Contains(Search.SearchTerm.ToLower())) //This should probably be done a lot better but this works for now
                    {
                        filteredRoles.Add(role); //Add to the filtered list
                    }
                }
                return Page(); 
            }
        }
        catch (Microsoft.Data.Sqlite.SqliteException) //catches if the users table does not exist yet
        {
            Console.WriteLine("SQLITE EXCEPTION");
        }
        return RedirectToPage("./Administration"); 
    }
    public async Task<Entities.User?> GetCurrentUser(ApplicationDbContext dbContext)
    {
        try
        { 
            User? currentUser = await _userManager.GetUserAsync(User);
            return currentUser;
        }
        catch (Microsoft.Data.Sqlite.SqliteException) //catches if the users table does not exist yet
        {
            Console.WriteLine("NO USERS TABLE YET! CRAAAAAAAAAAAAAP!");
            return null;
        }
    }
}
