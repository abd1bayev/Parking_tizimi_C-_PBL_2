# Parking Tizimi - 3-Tier Role System (C# Edition)

Oddiy, ammo kengaytirishga tayyor konsolga yo'naltirilgan parking tizimi. Ushbu repo oldingi Python versiyasidagi yechimni to'liq va sifatli ravishda C# ekotizimiga ko'chirish uchun tayyorlanmoqda.

Loyiha markazida 3 bosqichli rol tizimi bor:

- Admin - tizimni boshqarish va operatorlarni yaratish
- Operator - parking jarayonlarini boshqarish
- User - ro'yxatdan o'tish, login, bron qilish va to'lovlarni ko'rish

Bu README loyihaning C# versiyasi uchun asosiy yo'nalish, arxitektura, ishga tushirish tartibi va ishlab chiqish rejasini belgilaydi.

## Maqsad

Python asosidagi Parking Tizimi funksionalligini C# da qayta qurish, kod bazani qatlamli arxitektura asosida yozish va keyinchalik kengaytirish uchun mustahkam poydevor yaratish.

Asosiy maqsadlar:

- Python versiyadagi biznes mantiqni C# ga ko'chirish
- Rollarga asoslangan boshqaruvni saqlab qolish
- JSON yoki keyingi bosqichda DB bilan ishlashga tayyor struktura yaratish
- Test yozish va kodni modul ko'rinishida ajratish
- CLI tajribasini tartibli va tushunarli qilish

## Rejalashtirilgan Texnologiyalar

- Language: C#
- Platforma: .NET 8
- App turi: Console Application
- Data storage: boshlanishida JSON, keyinchalik SQLite yoki SQL Server ga o'tkazish mumkin
- Testing: xUnit yoki NUnit
- Serialization: System.Text.Json
- Password hashing: SHA-256 yoki undan yaxshiroq variant sifatida PBKDF2

## Rejalashtirilgan Asosiy Xususiyatlar

- 3-Tier Role System
- Admin yaratish va himoyalangan admin flow
- Operator va user autentifikatsiyasi
- Telefon raqam validatsiyasi (+998 format)
- Parking slotlarni boshqarish
- Avtomobil kirishi va chiqishini qayd etish
- Bron qilish va bronni bekor qilish
- To'lovni vaqt bo'yicha hisoblash
- JSON orqali saqlash
- Jadval ko'rinishidagi CLI chiqishlari
- Unit testlar

## Role Model

### 1. Admin

Admin odatda alohida boshqaruv oqimi orqali yaratiladi. Keyinchalik quyidagi imkoniyatlarga ega bo'ladi:

- operator yaratish
- tizim konfiguratsiyasini ko'rish va yangilash
- foydalanuvchilar va operatorlarni ko'rish
- hisobotlarni olish

### 2. Operator

Operator parkingning kundalik ishini boshqaradi:

- mashina kirishini qayd etish
- mashina chiqishini yakunlash
- bo'sh va band joylarni ko'rish
- bronlar bilan ishlash
- to'lov summasini hisoblash

### 3. User

Oddiy foydalanuvchi quyidagilarni bajara oladi:

- ro'yxatdan o'tish
- login qilish
- o'z mashinasini biriktirish
- bron qilish
- bronni bekor qilish
- o'z tarixini ko'rish

## Tavsiya Etiladigan Arxitektura

Loyihani qatlamli tarzda yozish tavsiya etiladi:

```text
ParkingTizimi/
├── src/
│   ├── ParkingTizimi.App/          # CLI entry point
│   ├── ParkingTizimi.Core/         # Biznes qoidalar, servislar, interfeyslar
│   ├── ParkingTizimi.Domain/       # Entity, enum, value object
│   ├── ParkingTizimi.Infrastructure/ # JSON storage, repository, persistence
│   └── ParkingTizimi.Shared/       # Umumiy helper va constantlar
├── tests/
│   ├── ParkingTizimi.Core.Tests/
│   └── ParkingTizimi.Integration.Tests/
├── docs/
├── README.md
└── LICENSE
```

## Domain Model Taklifi

Quyidagi asosiy obyektlar kutiladi:

- User
- Operator
- Admin
- Vehicle
- ParkingSlot
- Reservation
- ParkingSession
- Payment

Qo'shimcha enumlar:

- UserRole
- SlotStatus
- ReservationStatus
- PaymentStatus

## Tavsiya Etiladigan Folder Darajasidagi Vazifalar

### ParkingTizimi.Domain

- entity classlar
- enumlar
- value objectlar
- domain qoidalari

### ParkingTizimi.Core

- auth service
- parking service
- reservation service
- payment service
- validatorlar
- service interfeyslari

### ParkingTizimi.Infrastructure

- JSON repository implementatsiyasi
- fayl bilan ishlash
- seed ma'lumotlar
- config o'qish va yozish

### ParkingTizimi.App

- menyular
- foydalanuvchi input handling
- CLI navigation
- jadval va formatlangan output

## Rejalashtirilgan Menyular

### Guest Menu

- Register
- Login
- Exit

### Admin Menu

- Operator yaratish
- Barcha foydalanuvchilarni ko'rish
- Barcha parking slotlarni ko'rish
- Hisobotlarni ko'rish
- Logout

### Operator Menu

- Vehicle check-in
- Vehicle check-out
- Reservation list
- Slot holatini ko'rish
- Logout

### User Menu

- Profilni ko'rish
- Vehicle qo'shish
- Reservation yaratish
- Reservation bekor qilish
- To'lov tarixini ko'rish
- Logout

## Data Storage Bosqichlari

### 1-bosqich

JSON fayl orqali saqlash:

- users.json
- slots.json
- reservations.json
- sessions.json
- payments.json

### 2-bosqich

Yagona data papka va generic repository.

### 3-bosqich

SQLite yoki SQL Server ga migratsiya qilish.

## Xavfsizlik Talablari

- Parollar plain text ko'rinishida saqlanmasligi kerak
- Telefon formatlari validatsiya qilinishi kerak
- Role-based access qat'iy tekshirilishi kerak
- Admin yaratish oqimi alohida nazorat qilinishi kerak
- File corruption holati uchun error handling bo'lishi kerak

## Validation Qoidalari

- Telefon raqam faqat +998 formatda bo'lishi kerak
- Username bo'sh bo'lmasligi kerak
- Parol minimal uzunlikka ega bo'lishi kerak
- Slot band bo'lsa qayta check-in qilinmasligi kerak
- Reservation va parking session bir-biriga zid bo'lmasligi kerak

## Ishga Tushirish Rejasi

Quyidagi buyruqlar loyiha yaratilgandan keyin ishlatiladi:

```bash
dotnet new sln -n ParkingTizimi
dotnet new console -n ParkingTizimi.App -o src/ParkingTizimi.App
dotnet new classlib -n ParkingTizimi.Core -o src/ParkingTizimi.Core
dotnet new classlib -n ParkingTizimi.Domain -o src/ParkingTizimi.Domain
dotnet new classlib -n ParkingTizimi.Infrastructure -o src/ParkingTizimi.Infrastructure
dotnet new classlib -n ParkingTizimi.Shared -o src/ParkingTizimi.Shared
dotnet new xunit -n ParkingTizimi.Core.Tests -o tests/ParkingTizimi.Core.Tests
# Parking Tizimi - 3-Tier Role System (C#)

C# va .NET 8 asosida yozilgan cross-platform console desktop parking tizimi. Loyiha 3 ta rol bilan ishlaydi:

- Admin
- Operator
- User

Tizim terminal orqali boshqariladi, ma'lumotlar esa JSON faylga saqlanadi.

## Xususiyatlar

- 3-tier role system: `Admin`, `Operator`, `User`
- Admin yaratish
- User ro'yxatdan o'tishi va login
- Operator yaratish
- Avtomobil qo'shish
- Parking slotlarni ko'rish
- Bron yaratish va bekor qilish
- Vehicle check-in va check-out
- To'lovni soatbay hisoblash
- `+998XXXXXXXXX` telefon validatsiyasi
- Parolni `PBKDF2 SHA-256` bilan hash qilish
- JSON persistence
- xUnit testlar

## Texnologiyalar

- C#
- .NET 8
- Console Application
- System.Text.Json
- xUnit

## Loyiha Tuzilishi

```text
Parking_tizimi_C-_PBL_2/
├── src/
│   ├── ParkingTizimi.App/             # CLI entry point va menyular
│   ├── ParkingTizimi.Core/            # Business logic va service layer
│   ├── ParkingTizimi.Domain/          # Entity va enumlar
│   ├── ParkingTizimi.Infrastructure/  # JSON repository
│   └── ParkingTizimi.Shared/          # Common helpers, validation, security
├── tests/
│   └── ParkingTizimi.Core.Tests/      # Unit testlar
├── docs/
├── ParkingTizimi.sln
├── README.md
└── LICENSE
```

## Rollar

### Admin

- tizimdagi barcha userlarni ko'radi
- operator yaratadi
- slotlar, bronlar va active sessionlarni ko'radi

### Operator

- foydalanuvchi transportini parkingga kiritadi
- check-out qiladi
- active sessionlarni va slotlarni ko'radi

### User

- ro'yxatdan o'tadi va login qiladi
- o'z avtomobilini qo'shadi
- bron qiladi va bekor qiladi
- o'z to'lov tarixini ko'radi

## Asosiy Domain Obyektlar

- `User`
- `Vehicle`
- `ParkingSlot`
- `Reservation`
- `ParkingSession`
- `Payment`

## Ishga Tushirish

### 1. .NET 8 SDK

Global `dotnet` o'rnatilgan bo'lishi kerak.

Agar lokal user install ishlatilsa:

```bash
export PATH="$HOME/.dotnet:$PATH"
```

### 2. Build

```bash
dotnet build ParkingTizimi.sln
```

### 3. Test

```bash
dotnet test ParkingTizimi.sln
```

### 4. Dasturni ishga tushirish

```bash
dotnet run --project src/ParkingTizimi.App
```

## Boshlang'ich Foydalanish

1. Dastur ochilgach `Create Admin` ni tanlang.
2. Admin yarating.
3. Admin bilan login qiling va operator yarating.
4. User sifatida ro'yxatdan o'ting.
5. User menyusidan avtomobil qo'shing va bron qiling.
6. Operator menyusidan check-in va check-out qiling.

## Menyular

### Guest Menu

- Register
- Login
- Create Admin
- Exit

### Admin Menu

- Create Operator
- View Users
- View Slots
- View Reservations
- View Active Sessions
- Logout

### Operator Menu

- Check In Vehicle
- Check Out Vehicle
- View Slots
- View Active Sessions
- Logout

### User Menu

- Add Vehicle
- View My Vehicles
- Create Reservation
- Cancel Reservation
- View My Reservations
- View My Payments
- Logout

## Data Saqlash

Tizim ishga tushganda loyiha rootida `data/parking-data.json` yaratiladi.

Saqlanadigan ma'lumotlar:

- users
- vehicles
- slots
- reservations
- sessions
- payments

## Xavfsizlik

- Parollar ochiq matnda saqlanmaydi
- `PBKDF2 SHA-256` hashing ishlatiladi
- `+998` format telefon validatsiyasi mavjud
- Role-based access qo'llangan

## Test Holati

Joriy testlar:

- admin bootstrap
- invalid phone validation
- reservation flow
- checkout va payment flow

Natija:

```bash
Passed: 4
Failed: 0
```

## Muhim Eslatma

Bu versiya `console desktop app` hisoblanadi. Ya'ni u GUI emas, terminal ichida ishlaydi. Keyingi bosqichda xohlasangiz shu business layer ustiga `WPF`, `WinForms` yoki `Avalonia UI` bilan haqiqiy oynali desktop interfeys ham qurish mumkin.

## License

Loyiha [MIT License](LICENSE) asosida tarqatiladi.