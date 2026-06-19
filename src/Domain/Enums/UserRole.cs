namespace Domain.Enums;

/// <summary>
/// Tizim rollari — ierarxiya: Admin → Operator → User.
/// Har bir rol alohida vazifalarga ega; rollar o'zaro almashtirilmaydi.
/// </summary>
public enum UserRole
{
    /// <summary>Ma'mur: tizimni boshlaydi, operatorlarni yaratadi, nazorat qiladi.</summary>
    Admin = 1,

    /// <summary>Operator: check-in / check-out operatsiyalarini bajaradi.</summary>
    Operator = 2,

    /// <summary>Foydalanuvchi: ro'yxatdan o'tadi, avtomobil va bron boshqaradi.</summary>
    User = 3
}
