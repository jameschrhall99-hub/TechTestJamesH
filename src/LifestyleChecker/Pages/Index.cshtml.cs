using System.ComponentModel.DataAnnotations;
using System.Globalization;
using LifestyleChecker.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using src.LifestyleChecker.Rules;
using LifestyleChecker.Api.Interfaces;

namespace LifestyleChecker.Pages;

public class IndexModel(IPatientApiClient patientApiClient) : PageModel
{
    private const string NotFoundMessage = "Your details could not be found";
    private const string UnderAgeMessage = "You are not eligble for this service";
    private const string UnavailableMessage = "The patient service is currently unavailable. Please try again later.";

    [BindProperty]
    [Required(ErrorMessage = "Enter your NHS number.")]
    [RegularExpression("^[0-9]+$", ErrorMessage = "Enter an NHS number using digits only.")]
    public string NhsNumber { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Enter your surname.")]
    public string Surname { get; set; } = string.Empty;

    //keep date as text so its yyyy-MM-dd form value can be parsed explicitly
    [BindProperty]
    [Required(ErrorMessage = "Enter your date of birth.")]
    public string DateOfBirth { get; set; } = string.Empty;

    public string? OutcomeMessage { get; private set; }

    public void OnGet()
    {
        //reset session
        HttpContext.Session.Remove("PartOnePassed");
        HttpContext.Session.Remove("VerifiedAge");
        HttpContext.Session.Remove("ResultCategory");       
    }

    
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        //reset session
        HttpContext.Session.Remove("PartOnePassed");
        HttpContext.Session.Remove("VerifiedAge");
        HttpContext.Session.Remove("ResultCategory");

        //set today
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);


        //try parsing dob, error message if invalid and return to page
        DateOnly enteredDateOfBirth = new DateOnly();
        try
        {
            enteredDateOfBirth = DateOnly.ParseExact(DateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None);
        }
        catch(Exception ex) when(ex is FormatException or ArgumentNullException)
        {
            ModelState.AddModelError(nameof(DateOfBirth), "Enter a valid date of birth.");
        }

        //dob cant be in the future
        if(enteredDateOfBirth > today)
        {
            ModelState.AddModelError(nameof(DateOfBirth), "Date of birth cannot be in the future.");
        }
        
        //return to page
        if (!ModelState.IsValid)
        {
            return Page();
        }

        //lookup patient
        PatientLookupResult lookup = await patientApiClient.GetPatientAsync(NhsNumber, cancellationToken);

        //details not found, return to page
        if(lookup.Status == PatientLookupStatus.NotFound)
        {
            OutcomeMessage = NotFoundMessage;
            return Page();
        }

        //missing patient record or service is unavailable
        if(lookup.Status == PatientLookupStatus.Unavailable || lookup.Patient == null)
        {
            OutcomeMessage = UnavailableMessage;
            return Page();
        }

        //do results match?
        Models.PatientInfo patient = lookup.Patient;
        PatientMatcher matcher = new PatientMatcher();
        bool detailsMatch = matcher.IsMatch(NhsNumber, Surname, enteredDateOfBirth, patient.NhsNumber, patient.Name,
            patient.Born);

        //details dont match
        if(!detailsMatch)
        {
            OutcomeMessage = NotFoundMessage;
            return Page();
        }

        //calculate age based on nhs record dob, not entered dob
        int age = AgeCalculator.GetAge(patient.Born, today);

        if(age < 16)
        {
            OutcomeMessage = UnderAgeMessage;
            return Page();
        }

        //store age and if part one passed
        //dont store patient details
        HttpContext.Session.SetString("PartOnePassed", "true");
        HttpContext.Session.SetInt32("VerifiedAge", age);

        //send to next page if all details match and are valid
        return RedirectToPage("/PartTwo");
    }

}
