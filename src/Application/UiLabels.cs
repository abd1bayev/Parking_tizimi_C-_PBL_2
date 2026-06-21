using Domain.Enums;

namespace Application;

/// <summary>UI va xabar matnlarida enum qiymatlarini o'zbek tiliga o'girish.</summary>
public static class UiLabels
{
    public static string Role(UserRole role) => role switch
    {
        UserRole.Admin => "Ma'mur",
        UserRole.Operator => "Operator",
        UserRole.User => "Foydalanuvchi",
        _ => role.ToString()
    };

    public static string FormatSlotStatus(SlotStatus status) => status switch
    {
        SlotStatus.Available => "Bo'sh",
        SlotStatus.Reserved => "Bron qilingan",
        SlotStatus.Occupied => "Band",
        SlotStatus.OutOfService => "Ta'mirda",
        _ => status.ToString()
    };

    public static string FormatReservationStatus(ReservationStatus status) => status switch
    {
        ReservationStatus.Active => "Faol",
        ReservationStatus.Completed => "Yakunlangan",
        ReservationStatus.Cancelled => "Bekor qilingan",
        _ => status.ToString()
    };

    public static string FormatPaymentStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Paid => "To'langan",
        PaymentStatus.Pending => "Kutilmoqda",
        PaymentStatus.Failed => "Muvaffaqiyatsiz",
        _ => status.ToString()
    };

    public static string FormatProblemStatus(ProblemStatus status) => status switch
    {
        ProblemStatus.Open => "Ochiq",
        ProblemStatus.InProgress => "Jarayonda",
        ProblemStatus.Resolved => "Hal qilingan",
        _ => status.ToString()
    };
}
