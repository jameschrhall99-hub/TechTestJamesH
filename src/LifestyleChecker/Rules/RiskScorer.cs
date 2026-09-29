namespace src.LifestyleChecker.Rules;

/// <summary>
/// Calculates a risk score using age and answers
/// </summary>
public static class RiskScorer
{

    //setup age boundaries so can be changed later if needed
    private const int YoungMinAge = 16;
    private const int YoungMaxAge = 21;

    private const int AdultMinAge = 22;
    private const int AdultMaxAge = 40;

    private const int MiddleMinAge = 41;
    private const int MiddleMaxAge = 65;

    private const int SeniorMinAge = 66;

    //create rules outside of method so only need to be created once
    private static readonly RiskRule[] Rules =
    {
        new RiskRule(YoungMinAge, YoungMaxAge, 1, 2, 1),
        new RiskRule(AdultMinAge, AdultMaxAge, 2, 2, 3),
        new RiskRule(MiddleMinAge, MiddleMaxAge, 3, 2, 2),
        new RiskRule(SeniorMinAge, int.MaxValue, 3, 3, 1)
    };


    /// <summary>
    /// Calculates the score for the answers using the rule for the person's age.
    /// </summary>
    /// <param name="age">The person's age in years. Supported ages are 16 and older.</param>
    /// <param name="q1Yes"><see langword="true"/> if the answer to question 1 is yes.</param>
    /// <param name="q2Yes"><see langword="true"/> if the answer to question 2 is yes.</param>
    /// <param name="q3Yes"><see langword="true"/> if the answer to question 3 is yes.</param>
    /// <returns>total points assigned to the answers for the person's age.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="age"/> is below 16.</exception>
    public static int CalculateRisk(int age, bool q1Yes, bool q2Yes, bool q3Yes)
    {

        //work out which rule to use
        var rule = Rules.FirstOrDefault(r => age >= r.MinAge && age <= r.MaxAge);

        //age didn't match any rule
        if(rule is null)
        {
            throw new ArgumentOutOfRangeException(nameof(age));
        }

        int score = 0;

        //add scores based on answers
        if(q1Yes)
        {
            score += rule.Q1YesPoints;
        }
        if(q2Yes)
        {
            score += rule.Q2YesPoints;     
        }
        //not q3Yes, as "no" adds points here
        if(!q3Yes)
        {
            score += rule.Q3NoPoints;
        }
        
        return score;
    }
}
