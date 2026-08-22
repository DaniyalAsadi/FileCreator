# بررسی مدیریت `null` در Mapperهای gRPC (`GrpcScaffold.Core`)

> **وضعیت (۱۴۰۵/۰۵/۳۱):** هر ۶ مورد فیکس شد ✅ — باگهای ۱ تا ۴ در نوبت اول و گپهای ۵ و ۶
> در نوبت دوم. تغییرات در `ProtoTypeConversion.cs`، `MappingExpressionBuilder.cs`،
> `MappingGenerator.cs`، `ClientMappingGenerator.cs`، `Templates/mapping.sbn` و
> `Templates/client-mapping.sbn` اعمال و در
> `FileCreator.Test/MappingNullHandlingTests.cs` پوشش تستی کامل اضافه شد.

**نتیجهٔ کوتاه: نه — مقداردهی `null` به‌طور کامل درست هندل نشده است.** چند باگ واقعی (کرش در زمان اجرا) و چند گپ معنایی وجود دارد.

`MappingGenerator` (سرور: gRPC ⇄ Mediator) و `ClientMappingGenerator` (کلاینت: BFF ⇄ gRPC) هیچ‌کدام منطق تبدیل را خودشان پیاده نکرده‌اند؛ هر دو از کد مشترک `MappingExpressionBuilder` و `ProtoTypeConversion` استفاده می‌کنند. در نتیجه **باگ‌ها بین هر دو ژنراتور مشترک است**. تفاوت‌های رفتاری موجود:

| رفتار | `MappingGenerator` (سرور) | `ClientMappingGenerator` (کلاینت) |
|---|---|---|
| نگه‌داشت null برای repeated در سمت CLR | ❌ بدون guard | ✅ `if (request.X is not null)` |
| Expressionهای نامشخص (`/* TODO */`) | فقط کامنت TODO | `ThrowIfUnsupported` → Exception (fail-fast) |

---

## ✅ مواردی که درست هندل شده‌اند

1. **`Nullable<T>` در جهت CLR → proto** (`ProtoTypeConversion.ClrScalarToProto`):
   - اسکالرهای ساده (`int?`, `bool?`, ...) → `result.Page is null ? default : result.Page.Value` ✅
   - enum nullable → `result.Kind is null ? default : (Proto.UserKind)result.Kind.Value` ✅
   - `DateTime?`/`DateTimeOffset?` (پشتشان Timestamp است) → `result.X is null ? null : Timestamp.FromDateTime(...)` ✅ (چون پراپرتی message در C# تولیدی پروتو `null` می‌پذیرد)
2. **`DateTime?`/`DateTimeOffset?` در جهت proto → CLR** (`ProtoScalarToClr`): `request.X is null ? (DateTime?)null : request.X.ToDateTime()` ✅
3. **repeated در کلاینت (خروجی)**: `client-mapping.sbn` با `if (... is not null)` + `AddRange` ✅
4. **repeated در جهت proto → CLR**: `RepeatedField<T>` تولیدی پروتو هرگز null نیست، پس `request.Tags.Select(...).ToList()` امن است ✅

---

## 🐞 باگ‌ها

### باگ ۱ — بحرانی: `Guid?` / `DateOnly?` / `decimal?` مقدار `null` به پراپرتی `string` پروتو می‌فرستند → `ArgumentNullException`

📍 `GrpcScaffold.Core/Generation/ProtoTypeConversion.cs:220`

```csharp
return kind == ScalarKind.None
    ? $"{source} is null ? default : {accessor}"
    : $"{source} is null ? null : {Convert(accessor)}";   // ← این خط
```

کد تولیدشده (هر دو ژنراتور، جهت CLR → proto):

```csharp
Id = result.Id is null ? null : result.Id.Value.ToString(),
```

ولی setter پراپرتی `string` در C# تولیدی پروتو این است:

```csharp
public string Id { set { id_ = pb::ProtoPreconditions.CheckNotNull(value, "value"); } }
```

یعنی هر بار که مقدار nullable برابر `null` باشد، **در زمان اجرا `ArgumentNullException` پرتاب می‌شود**. کامنت بالای کد می‌گوید «both are reference types in generated C#, so `null` is a valid value to assign» — این برای `Timestamp` (message) درست است ولی برای `string` **غلط** است.

**فیکس:** برای نوع‌های string-backed باید `string.Empty` برگردد نه `null`:

```csharp
: $"{source} is null ? string.Empty : {Convert(accessor)}"  // برای Guid/DateOnly/Decimal
: $"{source} is null ? null : {Convert(accessor)}"          // فقط برای DateTime/DateTimeOffset (Timestamp)
```

### باگ ۲ — nullability انواع مرجع (reference types) اصلاً به Mapper نمی‌رسد

📍 `GrpcScaffold.Core/Generation/ProtoTypeMapper.cs:66-68` — `Reference.IsNullable` **فقط** برای `Nullable<T>` (value type) ست می‌شود. Annotation تایپ‌های مرجع (`string?`, `UserDetails?`) در `ProtoFieldInfo.IsNullable` (خط ۲۷) ذخیره می‌شود، ولی **هیچ‌جایی در مسیر Mapping مصرف نمی‌شود** — نه `MappingGenerator`، نه `ClientMappingGenerator`، نه `MappingExpressionBuilder`. فقط `ProtoGenerator` آن را برای چاپ کلمهٔ `optional` در فایل `.proto` می‌خواند.

پیامدها (جهت CLR → proto):
- `string? Name` → `Name = result.Name` بدون هیچ guard → null یعنی `ArgumentNullException`
- `UserDetails? Details` → `Details = new Proto.UserDetails { Email = result.Details.Email }` → null یعنی `NullReferenceException`

### باگ ۳ — پیام تو‌در‌تو (nested message) در جهت proto → CLR بدون null-guard → `NullReferenceException`

📍 `GrpcScaffold.Core/Generation/MappingExpressionBuilder.cs:232 و 238` (متد `BuildProtoToClrExpression`)

فیلدهای message در C# تولیدی پروتو همیشه nullable هستند؛ وقتی کلاینت فیلد را نفرستاده باشد، getter مقدار `null` برمی‌گرداند. ولی کد تولیدی بدون بررسی null روی آن dereference می‌کند:

```csharp
// سرور: MapToQuery
return new UserListQuery(new PermissionFilter(request.Filter.MinLevel), ...);
//                                 ^^^^^^^^^^^^^^ اگر null باشد → NRE

// کلاینت: MapToResponse
Details = new UserDetails(response.Details.Email),
//                        ^^^^^^^^^^^^^^^ اگر null باشد → NRE
```

عبارت درست: `request.Filter is null ? null : new PermissionFilter(request.Filter.MinLevel)` (با توجه به nullable بودن/نبودن مقصد). همین مشکل برای `google.protobuf.Struct` هم هست (`MappingExpressionBuilder.cs:191` → `request.Meta.Fields.ToDictionary(...)` وقتی unset باشد NRE می‌دهد).

### باگ ۴ — سرور `MapToResponse`: repeated بدون null-guard (ناسازگار با کلاینت)

📍 `GrpcScaffold.Core/Templates/mapping.sbn:62`

```csharp
Tags = { result.Tags.Select(x => x) },   // اگر result.Tags == null → کرش
```

تمپلیت کلاینت (`client-mapping.sbn:43-48`) guard دارد (`if (request.Tags is not null)`) ولی تمپلیت سرور نه. اگر سرویس دامین لیست را null برگرداند، Mapper سرور کرش می‌کند.

### گپ ۵ — ناهماهنگی در «unset → null» برای نوع‌های string-backed (proto → CLR)

📍 `ProtoTypeConversion.cs:148-166`

گارد `is null ? (T?)null : ...` فقط برای `DateTime`/`DateTimeOffset` (که پشتشان message است) اعمال می‌شود. برای `Guid?`/`DateOnly?`/`decimal?` تولید می‌کند:

```csharp
Guid.Parse(request.Id)   // فیلد unset ⇒ "" ⇒ FormatException
```

یعنی `DateTime?` وقتی مقدار نیاید `null` می‌گیرد، ولی `Guid?` به‌جای `null` با `FormatException` منفجر می‌شود — رفتار ناهماهنگ برای دو نوع nullable. (کامنت کد این را عمدی می‌داند، ولی سیاست باید یکدست باشد.)

### گپ ۶ — presence مربوط به proto3 `optional` در Mapper نادیده گرفته می‌شود

`ProtoGenerator` برای فیلدهای nullable در `.proto` کلمهٔ `optional` می‌گذارد (`service-proto.sbn:35`)، پس C# تولیدی `HasX`/`ClearX()` دارد. ولی Mapperها هرگز از آن‌ها استفاده نمی‌کنند:

- **proto → CLR**: `request.Page` (نوع `int` غیرnullable) مستقیم به پارامتر `int?` می‌رود ⇒ مقدار unset به‌جای `null` برابر `0` می‌شود؛ باید `request.HasPage ? request.Page : (int?)null` باشد.
- **CLR → proto**: `Page = result.Page is null ? default : result.Page.Value` — چون setter همیشه بیت presence را ست می‌کند، `null` تبدیل به «صفرِ دارای presence» روی سیم می‌شود؛ باید برای null اصلاً assign نشود (یا `ClearPage`).
- گارد enum در `ProtoScalarToClr:140` (`request.Kind is null ? ... : ...`) کد مرده است؛ پراپرتی enum تولیدی value type است و همیشه false. با `optional` باید `HasKind` بررسی می‌شد.
- `DateTime` غیرnullable از Timestampِ unset: `request.X.ToDateTime()` → NRE خام (احتمالاً سیاست «missing ⇒ fail» است، ولی بهتر است پیام واضح داشته باشد).

### نکات جزئی

- کلیدهای `is_nullable` در مدل تمپلیت (`CreateField`/`CreateParameter` در `MappingExpressionBuilder.cs:51,82`) ساخته می‌شوند ولی هیچ‌کدام از دو تمپلیت mapping ازشان استفاده نمی‌کنند — قلاب آماده برای پیاده‌سازی فیکس.
- `ProtoScalarToClr` برای enum nullable کاربرد عملی ندارد (ترکیب گپ ۶).

---

## جمع‌بندی شدت

| # | محل | جهت | شدت | علائم | وضعیت |
|---|---|---|---|---|---|
| ۱ | `ProtoTypeConversion.ClrScalarToProto` | CLR→proto | 🔴 کرش | `Guid?/DateOnly?/decimal?` = null ⇒ `ArgumentNullException` | ✅ فیکس شد — null به `string.Empty` تبدیل می‌شود |
| ۲ | `ProtoTypeMapper`/`MappingExpressionBuilder` | CLR→proto | 🔴 کرش | `string?`/message nullable ⇒ کرش هنگام null | ✅ فیکس شد — annotation از طریق `clrNullable` به builderها می‌رسد |
| ۳ | `MappingExpressionBuilder.BuildProtoToClrExpression` | proto→CLR | 🔴 کرش | فیلد message ارسال‌نشده ⇒ NRE | ✅ فیکس شد — guard حضور (`is null ? null :` / `is null ? new T() :`) |
| ۴ | `mapping.sbn` | CLR→proto (سرور) | 🟠 کرش | لیست null در response ⇒ کرش | ✅ فیکس شد — `if (src is not null) { ...AddRange(...) }` |
| ۵ | `ProtoTypeConversion.cs` | proto→CLR | 🟡 معنایی | `Guid?`/... به‌جای null خطای Parse | ✅ فیکس شد — با `HasX`: `request.HasId ? Guid.Parse(request.Id) : (Guid?)null` |
| ۶ | هر دو ژنراتور | دوطرفه | 🟡 معنایی | `int?` ⇄ `optional int32`: null با 0 قاطی می‌شود | ✅ فیکس شد — خواندن با `HasX`، نوشتن با guard مقدماتی `if (src is not null) grpc.X = ...;` |

## جزئیات فیکس‌های اعمال‌شده

- **باگ ۱** — در `ClrScalarToProto` خروجی nullable حالا بر اساس نوع مقصد تفکیک می‌شود:
  - `Guid?`/`DateOnly?`/`decimal?` (پشتشان proto `string` با setter حساس به null است) ⇒ `source is null ? string.Empty : ...`
  - `DateTime?`/`DateTimeOffset?` (پشتشان `Timestamp` است و null می‌پذیرد) ⇒ بدون تغییر: `? null :`
  - اسکالرهای ساده (`int?` و…) ⇒ بدون تغییر: `? default :`
- **باگ ۲** — پارامتر `clrNullable` به `BuildClrToProtoExpression`/`BuildProtoToClrExpression`/`ClrScalarToProto` اضافه شد و از annotation فیلد (`ProtoFieldInfo.IsNullable`) تغذیه می‌شود: `string?` ⇒ `source ?? string.Empty`؛ message/Struct nullable ⇒ `source is null ? null : ...` (پراپرتی‌های پیام/Struct پروتو null می‌پذیرند).
- **باگ ۳** — در جهت proto→CLR، guard حضور برای message و Struct اضافه شد: مقصد nullable ⇒ `null`؛ مقصد non-nullable با سازنده بدون پارامتر ⇒ نمونه خالی (`new T()` / `new Dictionary<string, object?>()`). مقصدهای ctor-based غیرnullable عمداً بدون guard مانده‌اند (رفتار fail-on-missing حفظ شد). در مسیر پارامتر کانستراکتور mediator، تصمیم بر اساس nullabilityِ **پارامتر مقصد** گرفته می‌شود.
- **باگ ۴** — تمپلیت سرور (`mapping.sbn`) به الگوی کلاینت مهاجرت کرد: فیلدهای non-repeated داخل object initializer، و فیلدهای repeated با `AddRange` — برای کالکشن‌های nullable با `if (src is not null)` guard می‌شوند.
- **گپ ۵** — در جهت proto→CLR، فیلدهای nullable که `optional` شدن حالا presence را می‌خوانند؛ `Guid?`/`decimal?`/`DateOnly?` موقع unset بودن فیلد به‌جای `FormatException` مقدار `null` می‌گیرند (هماهنگ با `DateTime?`).
- **گپ ۶** — پشتیبانی کامل از proto3 `optional` presence:
  - **خواندن:** `request.HasPage ? request.Page : (int?)null` برای اسکالر/enum/string — هم در سطح بالا هم در messageهای تو‌در‌تو (`request.Filter.HasMinLevel`).
  - **نوشتن:** assignment مستقیم حذف شد؛ به‌جای آن `if (result.Page is not null) grpc.Page = result.Page.Value;` تا بیت presence برای null ست نشود (در غیر این صورت setter همیشه بیت را ست می‌کند).
  - predicate مشترک `ProtoTypeConversion.HasProtoPresenceAccessor` با همان منطقی که `service-proto.sbn` برای چاپ `optional` دارد کار می‌کند — فیلدهای message-backed (Timestamp/Struct/nested/repeated/map) `HasX` ندارند و presence آن‌ها همان `null` است.
  - محدودیت آگاهانه: داخل messageهای تو‌در‌تو در جهت CLR→proto، اسکالرهای nullable همان fallback خطی باگ ۱ (`? string.Empty :`/`? default :`) را نگه می‌دارند چون guard مقدماتی فقط در سطح بالا ممکن است.
  - فیلدهای `DateTime?`/`DateTimeOffset?` (پشتشان `Timestamp` message است) بدون تغییر درست باقی ماندند: `? null :`.
- **تست** — `FileCreator.Test/MappingNullHandlingTests.cs`: ۱۷ تست رگرسیون برای هر دو ژنراتور (guardهای جدید + قفل‌کردن رفتارهای درست قبلی).
