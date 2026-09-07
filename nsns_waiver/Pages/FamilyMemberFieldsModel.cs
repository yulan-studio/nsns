namespace nsns_waiver.Pages;

/// <summary>
/// Supplies a stable collection index and values to the family-member partial.
/// </summary>
public sealed record FamilyMemberFieldsModel(
    int Index,
    IndexModel.FamilyMemberInput Member);
