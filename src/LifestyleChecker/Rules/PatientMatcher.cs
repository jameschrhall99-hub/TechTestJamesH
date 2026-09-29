/*

assuming data will always come from api in the form of the example given

{
  "nhsNumber": "123456789",
  "name": "DOE, John",
  "born": "25-12-1990"
}

*/

namespace src.LifestyleChecker.Rules;

/// <summary>
/// Checks whether patient details entered by a user match details returned by the patient service
/// </summary>
public class PatientMatcher
{
    /// <summary>
    /// Returns if the NHS number, surname, and date of birth match. API name is expected
    /// to contain a surname followed by a comma and the rest of the name
    /// </summary>
    /// <param name="enteredNHSNumber">NHS number entered by the user.</param>
    /// <param name="enteredSurname">surname entered by the user.</param>
    /// <param name="enteredDOB">Tdate of birth entered by the user.</param>
    /// <param name="apiNHSNumber">NHS number returned by the patient service.</param>
    /// <param name="apiName">name returned by the patient service, with surname before a comma.</param>
    /// <param name="apiDOB">date of birth returned by the patient service.</param>
    /// <returns><see langword="true"/> when all three patient details match; otherwise, <see langword="false"/>.</returns>
    public bool IsMatch(string enteredNHSNumber, string enteredSurname, DateOnly enteredDOB, string apiNHSNumber, string apiName, DateOnly apiDOB)
    {

        //check nhs numbers are populated
        if(string.IsNullOrWhiteSpace(enteredNHSNumber) || string.IsNullOrWhiteSpace(apiNHSNumber))
        {
            return false;
        }

        //immediately return false if no matching NHS number
        if(enteredNHSNumber != apiNHSNumber)
        {
            return false;
        }

        //check user has entered a surname
        if(string.IsNullOrWhiteSpace(enteredSurname))
        {
            return false;
        }

        //ensure case insensitive and remove trailing or leading whitespace
        string compareEnteredSurname = enteredSurname.Trim();

        //check api has a name
        if(string.IsNullOrWhiteSpace(apiName))
        {
            return false;
        }

        //check comma in api name
        if(!apiName.Contains(','))
        {
            return false;
        }

        //split name into surname and first name, assuming no commas in name
        string[] nameParts = apiName.Split(',');

        //apiSurname is surname given from api
        string compareApiSurname = nameParts[0].Trim();


        //check api name has a surname
        if(string.IsNullOrWhiteSpace(compareApiSurname))
        {
            return false;
        }


        //if names aren't equal return false
        if(!string.Equals(compareApiSurname, compareEnteredSurname, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if(enteredDOB == apiDOB)
        {
            return true;
        }

        return false;


    }
}
