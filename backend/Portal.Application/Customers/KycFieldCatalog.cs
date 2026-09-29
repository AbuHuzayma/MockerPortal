namespace Portal.Application.Customers;

/// <summary>
/// The authoritative, code-reviewed whitelist of KYC fields — see master spec
/// §10 / docs/02-requirements.md. <see cref="ColumnName"/> is the exact
/// T_PRT_CUSTOMER column (docs/05-database-design.md §3); <see cref="Key"/> is
/// the camelCase key used in JSON/ScreenField.IntegrationKey/FormValues.
/// Adding a row here (and nowhere else) is what grants write capability for a
/// field — a ScreenField row alone does not (docs/07 §3).
///
/// Modeled as an untyped key→value bag (Portal.Domain.Customers.KycRecord)
/// rather than ~68 hand-written POCO properties: every field here is a plain
/// pass-through value with no per-field business logic or distinct validation,
/// so a generic bag matches the actual shape of the problem — see
/// docs/01-architecture.md's "don't over-engineer" principle. IsBoolean fields
/// are the exception, carrying a real bool instead of a string.
/// </summary>
public sealed record KycFieldDefinition(string Key, string ColumnName, string Label, bool IsBoolean = false);

public static class KycFieldCatalog
{
    public static IReadOnlyList<KycFieldDefinition> Fields { get; } =
    [
        new("mobileNo", "MOBILE_NO", "Mobile Number"),
        new("nationality", "NATIONALITY", "Nationality"),
        new("languageCode", "LANGUAGE_CODE", "Language Code"),
        new("arabicFirstName", "ARABIC_FIRST_NAME", "Arabic First Name"),
        new("arabicSecondName", "ARABIC_SECOND_NAME", "Arabic Second Name"),
        new("arabicThirdName", "ARABIC_THIRD_NAME", "Arabic Third Name"),
        new("arabicLastName", "ARABIC_LAST_NAME", "Arabic Last Name"),
        new("firstName", "FIRST_NAME", "First Name"),
        new("secondName", "SECOND_NAME", "Second Name"),
        new("thirdName", "THIRD_NAME", "Third Name"),
        new("lastName", "LAST_NAME", "Last Name"),
        new("placeOfBirth", "PLACE_OF_BIRTH", "Place of Birth"),
        new("email", "EMAIL", "Email"),
        new("lifeStatus", "LIFE_STATUS", "Life Status"),
        new("blacklistStatus", "BLACKLIST_STATUS", "Blacklist Status"),
        new("nationalityId", "NATIONALITY_ID", "Nationality ID"),
        new("idOccupation", "ID_OCCUPATION", "Occupation ID"),
        new("primaryIncome", "PRIMARY_INCOME", "Primary Income"),
        new("primaryIncomeRangeId", "PRIMARY_INCOME_RANGE_ID", "Primary Income Range ID"),
        new("primaryIncomeRange", "PRIMARY_INCOME_RANGE", "Primary Income Range"),
        new("primaryIncomeSourceId", "PRIMARY_INCOME_SOURCE_ID", "Primary Income Source ID"),
        new("secondaryIncome", "SECONDARY_INCOME", "Secondary Income"),
        new("secondaryIncomeRangeId", "SECONDARY_INCOME_RANGE_ID", "Secondary Income Range ID"),
        new("secondaryIncomeRange", "SECONDARY_INCOME_RANGE", "Secondary Income Range"),
        new("secondaryIncomeSourceId", "SECONDARY_INCOME_SOURCE_ID", "Secondary Income Source ID"),
        new("employerId", "EMPLOYER_ID", "Employer ID"),
        new("employerName", "EMPLOYER_NAME", "Employer Name"),
        new("jobTitle", "JOB_TITLE", "Job Title"),
        new("professionId", "PROFESSION_ID", "Profession ID"),
        new("kycLevelId", "KYC_LEVEL_ID", "KYC Level ID"),
        new("agreementStatusId", "AGREEMENT_STATUS_ID", "Agreement Status ID"),
        new("zipcode", "ZIPCODE", "Zip Code"),
        new("additionalNo", "ADDITIONAL_NO", "Additional Number"),
        new("unitNo", "UNIT_NO", "Unit Number"),
        new("buildingNo", "BUILDING_NO", "Building Number"),
        new("streetName", "STREET_NAME", "Street Name"),
        new("district", "DISTRICT", "District"),
        new("city", "CITY", "City"),
        new("cityId", "CITY_ID", "City ID"),
        new("districtId", "DISTRICT_ID", "District ID"),
        new("employmentStatus", "EMPLOYMENT_STATUS", "Employment Status"),
        new("sanctionStatus", "SANCTION_STATUS", "Sanction Status"),
        new("blockedReasonCode", "BLOCKED_REASON_CODE", "Blocked Reason Code"),
        new("t24CustomerId", "T24_CUSTOMER_ID", "T24 Customer ID"),
        new("purposeOfAccountId", "PURPOSE_OF_ACCOUNT_ID", "Purpose of Account ID"),
        new("primaryIncomeSource", "PRIMARY_INCOME_SOURCE", "Primary Income Source"),
        new("secondaryIncomeStatus", "SECONDARY_INCOME_STATUS", "Secondary Income Status"),
        new("secondaryIncomeSource", "SECONDARY_INCOME_SOURCE", "Secondary Income Source"),
        new("levelOfEducationId", "LEVEL_OF_EDUCATION_ID", "Level of Education ID"),
        new("levelOfEducation", "LEVEL_OF_EDUCATION", "Level of Education"),
        new("expectedMonthlyCashInCountId", "EXPECTED_MONTHLY_CASH_IN_COUNT_ID", "Expected Monthly Cash-In Count ID"),
        new("expectedMonthlyCashInCount", "EXPECTED_MONTHLY_CASH_IN_COUNT", "Expected Monthly Cash-In Count"),
        new("expectedMonthlyCashInAmountId", "EXPECTED_MONTHLY_CASH_IN_AMOUNT_ID", "Expected Monthly Cash-In Amount ID"),
        new("expectedMonthlyCashInAmount", "EXPECTED_MONTHLY_CASH_IN_AMOUNT", "Expected Monthly Cash-In Amount"),
        new("expectedMonthlyCashOutCountId", "EXPECTED_MONTHLY_CASH_OUT_COUNT_ID", "Expected Monthly Cash-Out Count ID"),
        new("expectedMonthlyCashOutCount", "EXPECTED_MONTHLY_CASH_OUT_COUNT", "Expected Monthly Cash-Out Count"),
        new("expectedMonthlyCashOutAmountId", "EXPECTED_MONTHLY_CASH_OUT_AMOUNT_ID", "Expected Monthly Cash-Out Amount ID"),
        new("expectedMonthlyCashOutAmount", "EXPECTED_MONTHLY_CASH_OUT_AMOUNT", "Expected Monthly Cash-Out Amount"),
        new("minorStatus", "MINOR_STATUS", "Minor Status"),
        new("legalGuardian", "LEGAL_GUARDIAN", "Legal Guardian"),
        new("homePhoneNumber", "HOME_PHONE_NUMBER", "Home Phone Number"),
        new("custClassificationId", "CUST_CLASSIFICATION_ID", "Customer Classification ID"),
        new("limitIndicator", "LIMIT_INDICATOR", "Limit Indicator"),
        new("custDigitalIdStatus", "CUST_DIGITAL_ID_STATUS", "Digital ID Status"),

        // PEP mapping (master spec §10): PEP itself is derived, not stored — see
        // KycRecord.ComputePep. The four contributing flags are real booleans.
        new("pepByScreening", "PEP_BY_SCREENING", "PEP by Screening", IsBoolean: true),
        new("pepByCustomer", "PEP_BY_CUSTOMER", "PEP by Customer", IsBoolean: true),
        new("pepByProfession", "PEP_BY_PROFESSION", "PEP by Profession", IsBoolean: true),
        new("pepByRelationship", "PEP_BY_RELATIONSHIP", "PEP by Relationship", IsBoolean: true),
    ];

    public static readonly IReadOnlySet<string> ValidKeys = Fields.Select(f => f.Key).ToHashSet();

    /// <summary>Masked in audit records per docs/10-audit.md §4 (last 4 digits shown).</summary>
    public const string MobileNumberKey = "mobileNo";
}
