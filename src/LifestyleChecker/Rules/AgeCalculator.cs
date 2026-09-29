namespace src.LifestyleChecker.Rules;

/// <summary>
/// Calculates a person's age from their date of birth and a reference date.
/// </summary>
public static class AgeCalculator
{
    /// <summary>
    /// Calculates age on given date. 29 February birthday
    /// is 1 March in non-leap years
    /// </summary>
    /// <param name="dateOfBirth">The person's date of birth.</param>
    /// <param name="today">The date on which to calculate the person's age.</param>
    /// <returns>The person's age in years.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dateOfBirth"/> is later than <paramref name="today"/>.</exception>
    public static int GetAge(DateOnly dateOfBirth, DateOnly today)
    {

        //error if birth date is in the future
        if(today < dateOfBirth)
        {
            throw new ArgumentOutOfRangeException(nameof(dateOfBirth));
        }

        //initial age assumption
        int age = today.Year - dateOfBirth.Year;

        DateOnly birthdayThisYear;

        //if born on Feb 29th and this year is leap year, they turn new age on 29th Feb, non leap years turn new age on 1st March
        if(dateOfBirth.Month == 2 && dateOfBirth.Day == 29 && DateTime.IsLeapYear(today.Year))
        {  
            birthdayThisYear = new DateOnly(today.Year, 2, 29);
        }
        else if(dateOfBirth.Month == 2 && dateOfBirth.Day == 29)
        {
            birthdayThisYear = new DateOnly(today.Year, 3, 1);
        }
        else
        {
            birthdayThisYear = new DateOnly(today.Year, dateOfBirth.Month, dateOfBirth.Day);
        }


        //if birthday hasn't happened yet age doesn't go up this year yet
        if(today < birthdayThisYear)
        {
            age--;
        }

        return age;

    }
}
