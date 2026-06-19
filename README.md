# Parking Tizimi

Professional darajadagi parking boshqaruv tizimi. Loyiha C# va .NET 8 asosida yozilgan bo'lib, asosiy ishchi mijoz sifatida zamonaviy `Avalonia UI` ish stoli dasturidan foydalanadi. Qo'shimcha ravishda terminal asosidagi konsol varianti ham saqlangan.

## Loyiha Maqsadi

Ushbu loyiha parking jarayonlarini tartibli, xavfsiz va kengaytirishga tayyor tarzda boshqarish uchun ishlab chiqilgan. Tizim quyidagi asosiy yo'nalishlarni qamrab oladi:

- rollarga asoslangan boshqaruv
- foydalanuvchi va operator oqimlarini ajratish
- bron, kirim va chiqim jarayonlarini nazorat qilish
- to'lovlarni hisoblash va saqlash
- ish stoli interfeysi orqali qulay boshqaruv

## Asosiy Imkoniyatlar

- 3 rol: `Ma'mur`, `Operator`, `Foydalanuvchi`
- birinchi ma'murni yaratish
- foydalanuvchini ro'yxatdan o'tkazish va tizimga kiritish
- operator yaratish
- avtomobil qo'shish
- parking joylari holatini ko'rish
- bron yaratish va bekor qilish
- avtomobilni joylashtirish va chiqarish
- to'lovni vaqt asosida hisoblash
- JSON faylga saqlash
- telefon raqamini `+998` formatida tekshirish
- parolni `PBKDF2 SHA-256` asosida himoyalash

## Texnologiyalar

- `C#`
- `.NET 8`
- `Avalonia UI`
- `System.Text.Json`
- `xUnit`

## Loyiha Tuzilishi

```text
Parking_tizimi_C-_PBL_2/
├── src/
│   ├── ParkingTizimi.App/             # Konsol mijoz
│   ├── ParkingTizimi.Desktop/         # Ish stoli mijoz
│   ├── ParkingTizimi.Core/            # Biznes mantiq va servislar
│   ├── ParkingTizimi.Domain/          # Entity va enumlar
│   ├── ParkingTizimi.Infrastructure/  # JSON repository va persistence
│   └── ParkingTizimi.Shared/          # Umumiy yordamchi kodlar
├── tests/
│   └── ParkingTizimi.Core.Tests/      # Unit testlar
├── docs/
├── ParkingTizimi.sln
├── README.md
└── LICENSE
```

## Rollar

### Ma'mur

- operator yaratadi
- foydalanuvchilarni ko'radi
- joylar, bronlar va faol sessiyalarni kuzatadi
- umumiy holatni boshqaradi

### Operator

- foydalanuvchi avtomobilini joylashtiradi
- avtomobilni chiqaradi
- joylar holatini ko'radi
- faol sessiyalarni boshqaradi

### Foydalanuvchi

- ro'yxatdan o'tadi
- tizimga kiradi
- avtomobil qo'shadi
- bron yaratadi va bekor qiladi
- to'lovlar va bronlar tarixini ko'radi

## Asosiy Obyektlar

- `User`
- `Vehicle`
- `ParkingSlot`
- `Reservation`
- `ParkingSession`
- `Payment`

## Ishga Tushirish

### 1. .NET 8 ni tayyorlash

Agar tizimda global `dotnet` bo'lmasa va lokal o'rnatish ishlatilsa:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

### 2. Loyihani build qilish

```bash
dotnet build ParkingTizimi.sln
```

### 3. Testlarni ishga tushirish

```bash
dotnet test ParkingTizimi.sln
```

### 4. Ish stoli dasturini ishga tushirish

```bash
chmod +x run-desktop.sh
./run-desktop.sh
```

Yoki to'g'ridan-to'g'ri:

```bash
dotnet run --project src/ParkingTizimi.Desktop
```

### 5. Konsol dasturini ishga tushirish

```bash
chmod +x run-console.sh
./run-console.sh
```

Yoki:

```bash
dotnet run --project src/ParkingTizimi.App
```

## Boshlang'ich Ishlatish Tartibi

1. Ish stoli dasturini oching.
2. Birinchi ma'murni yarating.
3. Ma'mur sifatida tizimga kiring.
4. Operator yarating.
5. Foydalanuvchini ro'yxatdan o'tkazing.
6. Foydalanuvchi avtomobil qo'shib bron yaratsin.
7. Operator avtomobilni joylashtirish va chiqarish amallarini bajarsin.

## Interfeys Bo'limlari

### Kirish bo'limi

- ma'mur yaratish
- foydalanuvchini ro'yxatdan o'tkazish
- tizimga kirish

### Umumiy holat

- foydalanuvchilar soni
- joylar soni
- bronlar soni
- faol sessiyalar soni
- so'nggi harakatlar

### Ma'mur bo'limi

- operator yaratish
- foydalanuvchilar ro'yxati
- joylar ro'yxati
- bronlar ro'yxati
- faol sessiyalar ro'yxati

### Operator bo'limi

- avtomobilni joylashtirish
- avtomobilni chiqarish
- joylar jadvali
- faol oqim

### Foydalanuvchi bo'limi

- avtomobil qo'shish
- bron yaratish
- bronni bekor qilish
- mening avtomobillarim
- mening bronlarim
- mening to'lovlarim

## Ma'lumotlar Saqlanishi

Tizim ishga tushganda loyiha ildizida `data/parking-data.json` fayli yaratiladi.

Saqlanadigan asosiy ma'lumotlar:

- foydalanuvchilar
- avtomobillar
- joylar
- bronlar
- sessiyalar
- to'lovlar

## Xavfsizlik

- parollar ochiq matnda saqlanmaydi
- `PBKDF2 SHA-256` ishlatiladi
- telefon raqami `+998` formatida tekshiriladi
- rolga asoslangan ruxsat nazorati mavjud

## Test Holati

Hozirgi asosiy test yo'nalishlari:

- ma'mur yaratish oqimi
- noto'g'ri telefon validatsiyasi
- bron oqimi
- chiqarish va to'lov oqimi
- bo'sh maydon bilan avtomobil qo'shishdagi xatolik nazorati

## Muhim Eslatma

Asosiy tavsiya etilgan mijoz bu `ParkingTizimi.Desktop` loyihasi hisoblanadi. `ParkingTizimi.App` esa qo'shimcha konsol varianti sifatida saqlangan.

`run-desktop.sh` skripti har safar loyihaning joriy holatini ishga tushirish uchun qulay yo'l hisoblanadi. Lokal `.NET` ishlatilayotgan bo'lsa, `DOTNET_ROOT` va `PATH` to'g'ri sozlangan bo'lishi kerak.

## Loyiha Yanada Mukammal Bo'lishi Uchun Variantlar

Quyidagi yo'nalishlar loyihani ishlab chiqarish darajasiga yaqinlashtiradi:

Batafsil amaliy yo'l xaritasi bu yerda berilgan: [docs/RIVOJLANTIRISH-YOL-XARITASI.md](/home/abd1bayev/Projects/Parking_tizimi_C-_PBL_2/docs/RIVOJLANTIRISH-YOL-XARITASI.md)

### 1. Vizual uslubni premium darajaga olib chiqish

- maxsus ikonalar va belgilar to'plamini qo'shish
- kartalar va statistik bloklar uchun kuchliroq vizual ierarxiya yaratish
- kirish va bo'limlar almashinuvida yumshoq animatsiyalar qo'shish
- professional rang tizimi va yagona dizayn tokenlaridan foydalanish

### 2. To'liq mahalliylashtirish va formatlash

- sana va vaqt maydonlarini o'zbekcha foydalanish uslubiga moslash
- son, pul va vaqt ko'rinishlarini yagona formatga o'tkazish
- xatolik va muvaffaqiyat xabarlarini bir xil uslubda standartlashtirish

### 3. Operator ish jarayonini tezlashtirish

- tezkor qidiruv maydoni qo'shish
- foydalanuvchi va avtomobilni filtrlash imkonini berish
- faol sessiyalar bo'yicha tezkor amallar panelini yaratish
- operator uchun bir bosqichli joylashtirish oqimini soddalashtirish

### 4. Ma'mur uchun hisobot va tahlil bo'limi

- kunlik tushum statistikasi
- band va bo'sh joylar dinamikasi
- eng faol foydalanuvchilar va eng ko'p ishlatilgan joylar
- bronlar va real joylashtirishlar o'rtasidagi tahlil

### 5. Ma'lumotlar bazasiga o'tish

- JSON saqlashdan `SQLite` ga o'tish
- keyingi bosqichda `PostgreSQL` yoki `SQL Server` qo'llash
- ko'p foydalanuvchili ishlashga tayyor poydevor yaratish

### 6. Xavfsizlikni kuchaytirish

- rol bo'yicha amallar tarixini yuritish
- audit jurnalini qo'shish
- noto'g'ri kirish urinishlarini nazorat qilish
- muhim amallar uchun qo'shimcha tasdiqlash kiritish

### 7. Sifat nazoratini kengaytirish

- ko'proq unit testlar
- xizmatlararo integratsion testlar
- ish stoli interfeysi uchun avtomatlashtirilgan UI testlar
- ma'lumotlar saqlanishi va tiklanishi bo'yicha regressiya testlari

### 8. Professional funksional kengaytmalar

- abonement yoki tarif rejalari
- oldindan bronni tasdiqlash mexanizmi
- QR yoki chipta asosidagi kirim-chiqim oqimi
- to'lov kvitansiyasi yoki eksport funksiyasi
- mijoz va operator faoliyati bo'yicha hisobot eksporti

## Tavsiya Etiladigan Keyingi Bosqichlar

Agar loyiha keyingi iteratsiyada yanada kuchaytirilsa, quyidagi ketma-ketlik eng foydali bo'ladi:

1. `SQLite` asosidagi saqlash qatlamiga o'tish.
2. Ma'mur uchun hisobot va tahlil bo'limini qo'shish.
3. Operator va foydalanuvchi oqimini tezkor qidiruv va filtrlar bilan boyitish.
4. Ish stoli interfeysi uchun chuqurroq animatsiya va vizual yakunlash ishlarini bajarish.
5. UI va integratsion testlarni ko'paytirish.

## License

Loyiha [MIT License](LICENSE) asosida tarqatiladi.