# Lifestyle Checker

A .NET 10 ASP.NET Core Razor Pages application for the Lifestyle Checker interview task.

The user enters an NHS number, surname, and date of birth. The app looks up the patient through the supplied API, checks that the details match, and allows patients aged 16 or over to answer three lifestyle questions. The answers produce a low or high result.

## Requirements

- .NET 10 SDK
- A subscription key for the patient API to use the live lookup

## Configure the API key

From the repository root, store your key in .NET User Secrets:

```powershell
dotnet user-secrets set "PatientApi:SubscriptionKey" "YOUR_KEY" --project src/LifestyleChecker/LifestyleChecker.csproj
```

You can also set the `PatientApi__SubscriptionKey` environment variable. The app reads this configuration value and sends it in the `Ocp-Apim-Subscription-Key` header from the server.

## Run

From the repository root:

```powershell
dotnet run --project src/LifestyleChecker/LifestyleChecker.csproj
```

Open the local URL printed by the command.

## How it works

1. The start page validates the entered details. The patient API client handles missing records, unavailable responses, and malformed data. `PatientMatcher` compares the NHS number and date exactly and the surname without regard to case or surrounding spaces.
2. `AgeCalculator` calculates age from the API record. Patients under 16 cannot continue. Eligible patients answer three yes/no questions on the second page.
3. `RiskScorer` applies the age band's points. Scores of 3 or less show the low result; scores of 4 or more show the high result.

The session holds the Part One pass flag, verified age, and result category. It does not store patient details or questionnaire answers. The question and result pages redirect to the start page when the required session state is missing.

## Test

From the repository root:

```powershell
dotnet test LifestyleChecker.slnx
```

The automated tests use fake API responses and a dummy subscription key. They do not need the real key or access to the patient API.

## Assumptions

- The confirmed scoring age bands are 16–21, 22–40, 41–65, and 66+. Someone aged 65 is in the 41–65 band.
- A 29 February birthday is treated as occurring on 1 March in non-leap years so no one under the age of 16 can use the service.
- "High" message changed from "improve you quality of life" to "improve your quality of life"


## Extras
- Given the time, I would add DTOs and validation for form/user inputs
- Move the patient API URL and timeout into configuration, and validate the settings at startup
- Use an injectable clock so age and birthday boundary cases can be tested deterministically
- Add browser-side validation and accessibility checks for the questionnaire

## Part 3
I would achieve the optional addition by moving the values such as the age group boundaries or points per answer from code to a csv file that the applciation could read. 

eg
MinAge,MaxAge,Q1YesPoints,Q2YesPoints,Q3NoPoints,HighRiskMinScore
16,21,1,2,1,4
22,40,2,2,3,4
41,65,3,2,2,4
66,,3,3,1,4 //here blank upper limit means no upper limit

A ScoringRuleSet would hold a list of RiskRule records and one high-result threshold. The existing RiskRule record could be reused or adapted for an open-ended final band. An IScoringRulesProvider would expose the current, validated rule-set snapshot. A CsvScoringRulesProvider could load it from the file, an IRiskScorer would take the patient's age and three answers, obtain one snapshot from the provider, find the matching band, add its points, and return both the score and low/high category. PartTwoModel would call this service instead of the current static RiskScorer and its own hard-coded 3/4 threshold.

Option to "upgrade" to a database rather than csv later if code got more complicated / broarder scope.