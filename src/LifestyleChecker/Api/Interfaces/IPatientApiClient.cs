namespace LifestyleChecker.Api.Interfaces;

public interface IPatientApiClient
{
    Task<PatientLookupResult> GetPatientAsync(string nhsNumber, CancellationToken cancellationToken = default);
}