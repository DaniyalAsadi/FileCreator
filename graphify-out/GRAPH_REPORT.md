# Graph Report - .  (2026-08-30)

## Corpus Check
- Corpus is ~31,904 words - fits in a single context window. You may not need a graph.

## Summary
- 782 nodes · 1480 edges · 63 communities (43 shown, 20 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 11 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_gRPC Host Coordination|gRPC Host Coordination]]
- [[_COMMUNITY_Endpoint Analysis|Endpoint Analysis]]
- [[_COMMUNITY_Generator Tests|Generator Tests]]
- [[_COMMUNITY_WPF Main UI|WPF Main UI]]
- [[_COMMUNITY_gRPC Models and DI|gRPC Models and DI]]
- [[_COMMUNITY_.NET Project Dependencies|.NET Project Dependencies]]
- [[_COMMUNITY_Roslyn Rewriters|Roslyn Rewriters]]
- [[_COMMUNITY_Generator Documentation|Generator Documentation]]
- [[_COMMUNITY_Legacy Code Generators|Legacy Code Generators]]
- [[_COMMUNITY_Workspace and Preview|Workspace and Preview]]
- [[_COMMUNITY_Endpoint Test Models|Endpoint Test Models]]
- [[_COMMUNITY_gRPC Contract Generation|gRPC Contract Generation]]
- [[_COMMUNITY_Service Model Factories|Service Model Factories]]
- [[_COMMUNITY_Application Startup and Paths|Application Startup and Paths]]
- [[_COMMUNITY_Preview Editor UI|Preview Editor UI]]
- [[_COMMUNITY_Roslyn Syntax Helpers|Roslyn Syntax Helpers]]
- [[_COMMUNITY_Request Model Factories|Request Model Factories]]
- [[_COMMUNITY_Legacy Endpoint Generation|Legacy Endpoint Generation]]
- [[_COMMUNITY_Scriban Generation Pipeline|Scriban Generation Pipeline]]
- [[_COMMUNITY_gRPC Template Engine|gRPC Template Engine]]
- [[_COMMUNITY_Template Sources|Template Sources]]
- [[_COMMUNITY_gRPC Roslyn Context|gRPC Roslyn Context]]
- [[_COMMUNITY_Generator Resolution|Generator Resolution]]
- [[_COMMUNITY_Dependency Injection|Dependency Injection]]
- [[_COMMUNITY_Core Integration Guide|Core Integration Guide]]
- [[_COMMUNITY_Validator Templates|Validator Templates]]
- [[_COMMUNITY_Filter Templates|Filter Templates]]
- [[_COMMUNITY_Response Templates|Response Templates]]
- [[_COMMUNITY_Handler Model Analysis|Handler Model Analysis]]
- [[_COMMUNITY_Scriban Endpoint Generator|Scriban Endpoint Generator]]
- [[_COMMUNITY_Scriban Request Generator|Scriban Request Generator]]
- [[_COMMUNITY_API Description Parsing|API Description Parsing]]
- [[_COMMUNITY_Legacy Request Generation|Legacy Request Generation]]
- [[_COMMUNITY_Endpoint Visibility|Endpoint Visibility]]
- [[_COMMUNITY_Generated Region Merging|Generated Region Merging]]
- [[_COMMUNITY_Legacy Handler Tests|Legacy Handler Tests]]
- [[_COMMUNITY_WPF Settings|WPF Settings]]
- [[_COMMUNITY_Handler Test Models|Handler Test Models]]
- [[_COMMUNITY_Endpoint Test Templates|Endpoint Test Templates]]
- [[_COMMUNITY_Mediator Request Templates|Mediator Request Templates]]
- [[_COMMUNITY_Handler Templates|Handler Templates]]
- [[_COMMUNITY_Handler Test Templates|Handler Test Templates]]
- [[_COMMUNITY_Service Templates|Service Templates]]
- [[_COMMUNITY_Service Implementation Templates|Service Implementation Templates]]
- [[_COMMUNITY_Specification Templates|Specification Templates]]
- [[_COMMUNITY_Endpoint Model Factory|Endpoint Model Factory]]
- [[_COMMUNITY_Project File Updates|Project File Updates]]
- [[_COMMUNITY_Roslyn Formatting|Roslyn Formatting]]
- [[_COMMUNITY_Legacy Handler Generation|Legacy Handler Generation]]
- [[_COMMUNITY_Global Imports|Global Imports]]
- [[_COMMUNITY_Mediator Property Model|Mediator Property Model]]
- [[_COMMUNITY_Property Template Model|Property Template Model]]

## God Nodes (most connected - your core abstractions)
1. `FileCreatorForm` - 26 edges
2. `MappingNullHandlingTests` - 24 edges
3. `PreviewWorkspace` - 24 edges
4. `GrpcGenerationForm` - 19 edges
5. `ProtoGenerator` - 19 edges
6. `MapNullableValueTests` - 16 edges
7. `ScribanCodeGenerator` - 15 edges
8. `GroupName` - 14 edges
9. `IGeneratorModel` - 13 edges
10. `MappingExpressionBuilder` - 13 edges

## Surprising Connections (you probably didn't know these)
- `MapperGenerator` --semantically_similar_to--> `MappingGenerator`  [INFERRED] [semantically similar]
  FileCreator.Core/Readme.md → grpc-mapping-null-review.md
- `ProtoGenerator` --semantically_similar_to--> `ProtoGenerator`  [INFERRED] [semantically similar]
  FileCreator.Core/Readme.md → grpc-mapping-null-review.md
- `ScribanCodeGenerator` --implements--> `ICodeGenerator`  [EXTRACTED]
  FileCreator.Core/Generators/ScribanCodeGenerator.cs → FileCreator.Core/Generators/ICodeGenerator.cs
- `ScribanFileCreator` --references--> `ICodeGeneratorResolver`  [EXTRACTED]
  FileCreator.Core/Generators/ScribanFileCreator.cs → FileCreator.Core/Generators/ICodeGeneratorResolver.cs
- `EndpointRequestGenerator` --inherits--> `ScribanCodeGenerator`  [EXTRACTED]
  FileCreator.Core/Templates/Generators/EndpointRequestGenerator.cs → FileCreator.Core/Generators/ScribanCodeGenerator.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Extensible Scriban Generator Family** — filecreator_core_readme_responsegenerator, filecreator_core_readme_mappergenerator, filecreator_core_readme_validatorgenerator, filecreator_core_readme_handlergenerator, filecreator_core_readme_protogenerator [EXTRACTED 1.00]
- **Complete gRPC Mapping Null-Handling Remediation** — grpc_mapping_null_review_string_backed_nullable_conversion, grpc_mapping_null_review_reference_type_nullability_propagation, grpc_mapping_null_review_nested_message_presence_guard, grpc_mapping_null_review_repeated_collection_null_guard, grpc_mapping_null_review_unset_to_null_consistency, grpc_mapping_null_review_proto3_optional_presence [EXTRACTED 1.00]

## Communities (63 total, 20 thin omitted)

### Community 0 - "gRPC Host Coordination"
Cohesion: 0.06
Nodes (22): GrpcGenerationCoordinator, EndpointFilter, EndpointDiscoveryService, IEndpointDiscoveryService, EndpointModel, RoslynFileCreator, GeneratedFile, ClientMappingGenerator (+14 more)

### Community 1 - "Endpoint Analysis"
Cohesion: 0.06
Nodes (21): ApiDescriptionResolver, EndpointAnalyzer, MediatorSendResolver, ApiDescriptionInfo, CancellationToken, Compilation, NamingConventions, ProtoTypeConversion (+13 more)

### Community 2 - "Generator Tests"
Cohesion: 0.07
Nodes (7): Fact, EndpointGeneratorTests, EndpointRequestGeneratorTests, GrpcClientGenerationTests, MapNullableValueTests, MappingNullHandlingTests, MetadataReference

### Community 3 - "WPF Main UI"
Cohesion: 0.06
Nodes (17): ComboBox, DependencyPropertyChangedEventArgs, EndpointSelectionItem, FileCreatorForm, GenerationContext, GrpcGenerationForm, SettingsForm, GenerationManifest (+9 more)

### Community 4 - "gRPC Models and DI"
Cohesion: 0.10
Nodes (17): Action, ConstructorInfo, ConstructorParameterInfo, ContractInfo, Dictionary, Expression, DiRegistrationGenerator, MappingExpressionBuilder (+9 more)

### Community 5 - ".NET Project Dependencies"
Cohesion: 0.05
Nodes (40): net10.0, Humanizer (3.0.1), Humanizer.Core (3.0.1), Microsoft.Build.Framework (17.11.48), Microsoft.CodeAnalysis.Common (5.0.0), Microsoft.CodeAnalysis.CSharp (5.0.0), Microsoft.CodeAnalysis.CSharp.Workspaces (5.0.0), Microsoft.CodeAnalysis.Workspaces.Common (5.0.0) (+32 more)

### Community 6 - "Roslyn Rewriters"
Cohesion: 0.09
Nodes (15): ClassDeclarationSyntax, CSharpSyntaxRewriter, CSharpSyntaxWalker, EnumDeclarationSyntax, EnumMemberDeclarationSyntax, FieldDeclarationSyntax, HashSet, MemberDeclarationSyntax (+7 more)

### Community 7 - "Generator Documentation"
Cohesion: 0.09
Nodes (28): HandlerGenerator, IScribanTemplateRenderer, MapperGenerator, ProtoGenerator, ResponseGenerator, ScribanCodeGenerator<ResponseTemplateModel>, ServiceCollectionExtensions, Three-Step Generator Pattern (+20 more)

### Community 8 - "Legacy Code Generators"
Cohesion: 0.09
Nodes (8): CompilationUnitSyntax, EndpointRequestValidatorGenerator, MediatorRequestFiltersGenerator, MediatorRequestGenerator, MediatorRequestResponseGenerator, MediatorRequestServiceGenerator, MediatorRequestServiceImplementationGenerator, MediatorRequestSpecificationGenerator

### Community 9 - "Workspace and Preview"
Cohesion: 0.13
Nodes (9): bool, PreviewWorkspace, IWorkspaceCache, WorkspaceCache, WorkspaceCacheService, IDisposable, MSBuildWorkspace, Project (+1 more)

### Community 10 - "Endpoint Test Models"
Cohesion: 0.22
Nodes (4): EndpointTestTemplateModelFactory, GroupName, HttpVerb, EndpointTestGenerator

### Community 11 - "gRPC Contract Generation"
Cohesion: 0.23
Nodes (4): Content, FileName, ContractGenerator, NullableAnnotation

### Community 12 - "Service Model Factories"
Cohesion: 0.25
Nodes (4): MediatorRequestServiceImplementationTemplateModelFactory, MediatorRequestServiceTemplateModelFactory, MethodParameterTemplateModel, ResponseType

### Community 13 - "Application Startup and Paths"
Cohesion: 0.15
Nodes (8): Application, ExitEventArgs, App, IServiceProvider, ProjectPaths, IProjectPathsProvider, ProjectPathsProvider, StartupEventArgs

### Community 14 - "Preview Editor UI"
Cohesion: 0.19
Nodes (6): ClassifiedSpan, Color, PreviewForm, FontFamily, RichTextBox, Run

### Community 15 - "Roslyn Syntax Helpers"
Cohesion: 0.20
Nodes (4): ExpressionSyntax, Chain, RoslynHelpers, StatementSyntax

### Community 16 - "Request Model Factories"
Cohesion: 0.23
Nodes (4): EndpointRequestTemplateModelFactory, MediatorRequestSpecificationTemplateModelFactory, MediatorRequestTemplateModelFactory, RequestType

### Community 17 - "Legacy Endpoint Generation"
Cohesion: 0.26
Nodes (4): AttributeListSyntax, ExpressionStatementSyntax, MethodDeclarationSyntax, EndpointGenerator

### Community 18 - "Scriban Generation Pipeline"
Cohesion: 0.18
Nodes (5): ConcurrentDictionary, ScribanFileCreator, IScribanTemplateRenderer, ScribanTemplateRenderer, TModel

### Community 19 - "gRPC Template Engine"
Cohesion: 0.20
Nodes (6): Func, DictionaryExtensions, TemplateEngine, Template, TKey, TValue

### Community 20 - "Template Sources"
Cohesion: 0.25
Nodes (4): Assembly, EmbeddedResourceTemplateSource, FileSystemTemplateSource, IScribanTemplateSource

### Community 21 - "gRPC Roslyn Context"
Cohesion: 0.31
Nodes (3): GrpcAnalysisContextFactory, ProjectId, Task

### Community 22 - "Generator Resolution"
Cohesion: 0.31
Nodes (4): ICodeGenerator, CodeGeneratorResolver, ICodeGeneratorResolver, Type

### Community 23 - "Dependency Injection"
Cohesion: 0.32
Nodes (3): GrpcServiceCollectionExtensions, ServiceCollectionExtensions, IServiceCollection

### Community 24 - "Core Integration Guide"
Cohesion: 0.25
Nodes (8): EndpointGenerator, EndpointTemplateModelFactory, FileCreator.Core Integration Guide, GeneratedFile, RoslynCodeFormatter, RoslynFileCreator, Scriban Render and Roslyn Format Generation Pipeline, SyntaxFactory

### Community 25 - "Validator Templates"
Cohesion: 0.29
Nodes (3): EndpointRequestValidatorTemplateModelFactory, EndpointRequestValidatorGenerator, EndpointRequestValidatorTemplateModel

### Community 26 - "Filter Templates"
Cohesion: 0.29
Nodes (3): MediatorRequestFiltersTemplateModelFactory, MediatorRequestFiltersGenerator, MediatorRequestFiltersTemplateModel

### Community 27 - "Response Templates"
Cohesion: 0.29
Nodes (3): MediatorRequestResponseTemplateModelFactory, MediatorRequestResponseGenerator, MediatorRequestResponseTemplateModel

### Community 29 - "Scriban Endpoint Generator"
Cohesion: 0.33
Nodes (3): EndpointGenerator, ScribanCodeGenerator, EndpointTemplateModel

### Community 30 - "Scriban Request Generator"
Cohesion: 0.33
Nodes (3): EndpointRequestGenerator, EndpointRequestTemplateModel, IGeneratorModel

### Community 33 - "Endpoint Visibility"
Cohesion: 0.40
Nodes (3): VisibilityResolver, EndpointVisibility, RouteInfo

### Community 36 - "WPF Settings"
Cohesion: 0.67
Nodes (3): ApplicationSettingsBase, FileCreator.Properties, Settings

## Knowledge Gaps
- **58 isolated node(s):** `net10.0`, `Humanizer (3.0.1)`, `Humanizer.Core (3.0.1)`, `Microsoft.CodeAnalysis.Common (5.0.0)`, `Microsoft.CodeAnalysis.CSharp (5.0.0)` (+53 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **20 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `PreviewWorkspace` connect `Workspace and Preview` to `WPF Main UI`, `gRPC Models and DI`, `gRPC Roslyn Context`, `Preview Editor UI`?**
  _High betweenness centrality (0.097) - this node is a cross-community bridge._
- **Why does `FileCreatorForm` connect `WPF Main UI` to `Workspace and Preview`, `Application Startup and Paths`?**
  _High betweenness centrality (0.082) - this node is a cross-community bridge._
- **Why does `ScribanCodeGenerator` connect `Scriban Endpoint Generator` to `Endpoint Test Templates`, `Mediator Request Templates`, `Handler Templates`, `Handler Test Templates`, `Service Templates`, `Service Implementation Templates`, `Specification Templates`, `Scriban Generation Pipeline`, `Generator Resolution`, `Validator Templates`, `Filter Templates`, `Response Templates`, `Scriban Request Generator`?**
  _High betweenness centrality (0.048) - this node is a cross-community bridge._
- **What connects `net10.0`, `Humanizer (3.0.1)`, `Humanizer.Core (3.0.1)` to the rest of the system?**
  _59 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `gRPC Host Coordination` be split into smaller, more focused modules?**
  _Cohesion score 0.059237319511292116 - nodes in this community are weakly interconnected._
- **Should `Endpoint Analysis` be split into smaller, more focused modules?**
  _Cohesion score 0.062111801242236024 - nodes in this community are weakly interconnected._
- **Should `Generator Tests` be split into smaller, more focused modules?**
  _Cohesion score 0.06573426573426573 - nodes in this community are weakly interconnected._