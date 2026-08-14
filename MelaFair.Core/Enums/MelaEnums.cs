namespace MelaFair.Core.Enums;

/// <summary>
/// Categories of stalls available for lease at the fair
/// </summary>
public enum StallCategory
{
    General = 0,
    FoodAndBeverage = 1,
    Handicrafts = 2,
    ClothingAndTextiles = 3,
    GamesAndEntertainment = 4,
    ElectronicsAndGadgets = 5,
    TraditionalArt = 6
}

/// <summary>
/// Physical size classification of fair stalls
/// </summary>
public enum StallSize
{
    Small = 0,
    Medium = 1,
    Large = 2,
    PremiumCorner = 3
}

/// <summary>
/// Status of stall rental transactions
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Refunded = 2,
    Failed = 3
}

/// <summary>
/// Admission ticket validation status
/// </summary>
public enum TicketStatus
{
    Valid = 0,
    Used = 1,
    Cancelled = 2
}

/// <summary>
/// Status of an employee recruitment application
/// </summary>
public enum ApplicationStatus
{
    Pending = 0,
    Shortlisted = 1,
    Accepted = 2,
    Rejected = 3
}
