# FileCreator.Core integration

`FileCreator.Core` is the generic generation layer. It contains:

- immutable template models and naming/model factories;
- embedded Scriban templates;
- `ScribanCodeGenerator<TModel>` and generator resolution;
- Roslyn syntax validation and deterministic formatting;
- `GeneratedFile`, structured diagnostics, and `GeneratedFileWriter`.

Register the canonical pipeline once at the application boundary:

```csharp
services.AddScribanCodeGeneration();
services.AddSingleton<GeneratedFileWriter>();
```

The WPF host creates a normalized `FileCreatorGenerationRequest`, asks
`FileCreatorGenerator` for an in-memory, path-sorted collection of `GeneratedFile`
artifacts, previews them through the Roslyn workspace, and only then calls the
output writer. Generators never create directories or write files.

## Add a generator

1. Add an immutable model implementing `IGeneratorModel` and a focused model factory.
2. Add an embedded `.sbn` template beginning with the FileCreator generated header.
3. Derive a concrete generator from `ScribanCodeGenerator<TModel>`.
4. Add the artifact to orchestration and cover its text, path, and two-run determinism.

Concrete `ICodeGenerator` implementations are discovered by
`AddScribanCodeGeneration`; no manual registration line or second resolver is needed.
Do not reintroduce the removed syntax-factory `Generators/V1` pipeline.

The full architecture and output policies are documented in the ECommerce mdBook under
`Documents/src/file-creator/`.
