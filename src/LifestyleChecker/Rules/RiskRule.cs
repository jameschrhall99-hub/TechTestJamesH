namespace src.LifestyleChecker.Rules;

/// <summary>
/// calculates points assigned to answers for each age
/// </summary>
/// <param name="MinAge">The lowest age in the range.</param>
/// <param name="MaxAge">The highest age in the range.</param>
/// <param name="Q1YesPoints">Points added when the answer to question 1 is yes.</param>
/// <param name="Q2YesPoints">Points added when the answer to question 2 is yes.</param>
/// <param name="Q3NoPoints">Points added when the answer to question 3 is no.</param>
public record RiskRule(int MinAge, int MaxAge, int Q1YesPoints, int Q2YesPoints, int Q3NoPoints);