namespace LifestyleChecker.Models;

/// <summary>
/// Holds the patient details returned by the patient lookup service.
/// </summary>
/// <param name="NhsNumber">The patient's NHS number.</param>
/// <param name="Name">The patient's name as supplied by the service.</param>
/// <param name="Born">The patient's date of birth.</param>
public record PatientInfo(string NhsNumber, string Name, DateOnly Born);
