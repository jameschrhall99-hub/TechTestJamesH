using System.Globalization;
using System.Net;
using System.Text.Json;
using LifestyleChecker.Models;
using LifestyleChecker.Api.Interfaces;

namespace LifestyleChecker.Api;

//possible outcomes of looking up a patient.
public enum PatientLookupStatus
{
    Success,

    //HTTP 404 no patient was found for this number
    NotFound,

    //could not provide a usable response
    Unavailable
}

/// <summary>
/// Represents the outcome of a patient lookup, including the patient details when found.
/// </summary>
/// <param name="Status">Indicates whether the lookup succeeded, the patient was not found, or the service was unavailable.</param>
/// <param name="Patient">The patient details when the lookup succeeds; otherwise, <see langword="null"/>.</param>
public record PatientLookupResult(

    //lookup succeeded, found no patient, or failed
    PatientLookupStatus Status,

    // contains patient details for success, null for other statuses
    PatientInfo? Patient = null);

public class PatientApiClient(HttpClient httpClient) : IPatientApiClient
{

    /// <summary>
    /// Retrieves a patient record using the supplied NHS number and validates the API response.
    /// </summary>
    /// <param name="nhsNumber">The patient's NHS number, containing digits only.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>
    /// A result with status <see cref="PatientLookupStatus.Success"/> and patient details when a valid
    /// record is returned; <see cref="PatientLookupStatus.NotFound"/> when the API returns HTTP 404;
    /// otherwise, <see cref="PatientLookupStatus.Unavailable"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">The request was canceled using <paramref name="cancellationToken"/>.</exception>
    public async Task<PatientLookupResult> GetPatientAsync(string nhsNumber, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(nhsNumber) || nhsNumber.Length > 10)
        {
            return new PatientLookupResult(PatientLookupStatus.Unavailable);
        }

        //check if nhsNumber has any non number characters
        bool hasNonNumberDigit = nhsNumber.Any(c => c < '0' || c > '9');

        //reject empty or malformed nhs number
        if(hasNonNumberDigit)
        {
            return new PatientLookupResult(PatientLookupStatus.Unavailable);
        }

        try
        {
            //send get request, "using" erases response after try block
            using var response = await httpClient.GetAsync($"tech-test/t2/patients/{nhsNumber}", cancellationToken);

            if(response.StatusCode == HttpStatusCode.NotFound)
            {
                return new PatientLookupResult(PatientLookupStatus.NotFound);
            }

            //Success status codes start with 2
            if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
            {
                return new PatientLookupResult(PatientLookupStatus.Unavailable);
            }

            //read response body
            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            //"using" again clears after try block
            using JsonDocument document = JsonDocument.Parse(json);
            //
            JsonElement root = document.RootElement;
            
            //check json is formed as expected
            //eg as
            /*{
            "nhsNumber": "123456789",
            "name": "DOE, John",
            "born": "25-12-1990"
            }*/
            if(root.ValueKind != JsonValueKind.Object)
            {
                return new PatientLookupResult(PatientLookupStatus.Unavailable);
            }

            //chech each property exists and assign to element if it does, return unavailable if not
            //nhsNumber
            if(!root.TryGetProperty("nhsNumber", out JsonElement nhsNumberProperty))
            {
                return new PatientLookupResult(PatientLookupStatus.Unavailable);
            }

            //name
            if(!root.TryGetProperty("name", out JsonElement nameProperty))
            {
                return new PatientLookupResult(PatientLookupStatus.Unavailable);
            }

            //born
            if(!root.TryGetProperty("born", out JsonElement bornProperty))
            {
                return new PatientLookupResult(PatientLookupStatus.Unavailable);
            }
 
            //check each property is correct kind
            if(nhsNumberProperty.ValueKind != JsonValueKind.String ||
                nameProperty.ValueKind != JsonValueKind.String ||
                bornProperty.ValueKind != JsonValueKind.String)
            {
                return new PatientLookupResult(PatientLookupStatus.Unavailable);
            }

            //assign strings so can check non empty
            string? returnedNhsNumber = nhsNumberProperty.GetString();
            string? name = nameProperty.GetString();
            string? born = bornProperty.GetString();

            //check not empty
            if(string.IsNullOrWhiteSpace(returnedNhsNumber) ||
                string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(born))
            {
                return new PatientLookupResult(PatientLookupStatus.Unavailable);
            }

            //declare date
            DateOnly date;

            //try parse to correct form, unavailable if fails
            try
            {
                date = DateOnly.ParseExact(born, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            }
            catch(FormatException)
            {
                return new PatientLookupResult(PatientLookupStatus.Unavailable);
            }

            //valid response
            PatientInfo patient = new PatientInfo(returnedNhsNumber, name, date);

            return new PatientLookupResult(PatientLookupStatus.Success, patient);

        }

        //http timeout
        catch(OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PatientLookupResult(PatientLookupStatus.Unavailable);
        }

        //network failed
        catch(HttpRequestException)
        {
            return new PatientLookupResult(PatientLookupStatus.Unavailable);
        }

        //invalid json
        catch(JsonException)
        {
            return new PatientLookupResult(PatientLookupStatus.Unavailable);
        }
        
    }
    
}
