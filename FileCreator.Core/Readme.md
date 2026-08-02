# نحوه‌ی ادغام در FileCreator.Core

## 1) csproj

به `FileCreator.Core.csproj` این‌ها را اضافه کنید:

```xml
<ItemGroup>
  <PackageReference Include="Scriban" Version="7.2.5" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
</ItemGroup>

<ItemGroup>
  <EmbeddedResource Include="Templates\*.sbn" />
</ItemGroup>
```

(`Microsoft.CodeAnalysis.CSharp` و `Microsoft.CodeAnalysis.CSharp.Workspaces` را از قبل دارید —
همان‌ها برای `RoslynCodeFormatter` کافی‌اند؛ دیگر نیازی به `SyntaxFactory` نیست.)

## 2) استفاده (مثال جایگزین EndpointGenerator قدیمی)

```csharp
var services = new ServiceCollection().AddScribanCodeGeneration().BuildServiceProvider();
var generator = services.GetRequiredService<EndpointGenerator>();

var model = EndpointTemplateModelFactory.Create(
    projectName: "AuthorizationManager",
    useCaseNamespace: usecaseNamespace,
    webNamespace: webNamespace,
    group: groupName.Resource,
    useCaseName: "CreateUser",
    requestType: RequestType.Command,
    httpVerb: HttpVerb.POST,
    hasRequest: true,
    hasResponse: true,
    responseType: ResponseType.Single);

string endpointCs = await generator.GenerateAsync(model);
// endpointCs جایگزین خروجی EndpointGenerator.Generate(...).NormalizeWhitespace().ToFullString() قدیمی می‌شود
```

`RoslynFileCreator` تغییری در بقیه‌ی pipeline نیاز ندارد — همان `GeneratedFile(path, content)` را دریافت
می‌کند، فقط `content` حالا حاصل Scriban Render + Roslyn Format است، نه AST.

## 3) افزودن Generator جدید (مثلاً ResponseGenerator)

سه قدم، همیشه همین سه قدم:

1. `Models/ResponseTemplateModel.cs` + `ResponseTemplateModelFactory` (فقط منطق نام‌گذاری)
2. `Templates/response.sbn`
3.
   ```csharp
   public sealed class ResponseGenerator(IScribanTemplateRenderer renderer)
       : ScribanCodeGenerator<ResponseTemplateModel>(renderer)
   {
       protected override string TemplateName => "response.sbn";
   }
   ```
   و یک خط در `ServiceCollectionExtensions`.

همین الگو دقیقاً برای `MapperGenerator`, `ValidatorGenerator`, `HandlerGenerator`,
`ProtoGenerator` (که `GrpcScaffold.Core` از قبل به شکل مشابه دارد) و بقیه صدق می‌کند —
هیچ‌کدام دیگر نیازی به `SyntaxFactory` ندارند.