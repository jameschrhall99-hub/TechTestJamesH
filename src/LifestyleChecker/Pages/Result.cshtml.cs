using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LifestyleChecker.Pages;

/// <summary>
/// Loads the result message associated with the result category stored in the session.
/// </summary>
public class ResultModel : PageModel
{
    /// <summary>
    /// Gets the message displayed for the user's result category.
    /// </summary>
    public string OutcomeMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Displays the high or low result message, or redirects to the start page if no recognized
    /// result category is stored in the session.
    /// </summary>
    /// <returns>The result page, or a redirect to the index page.</returns>
    public IActionResult OnGet()
    {
        string? category = HttpContext.Session.GetString("ResultCategory");

        if(category == "High")
        {
            //changed to "your quality of life" from "you"
            OutcomeMessage = "We think there are some simple things you could do to improve your quality of life, please phone to book an appointment";
            return Page();
        }
        else if(category == "Low")
        {
            OutcomeMessage = "Thank you for answering our questions, we don't need to see you at this time. Keep up the good work!";
            return Page();
        }

        return RedirectToPage("/Index"); 
    }

}
