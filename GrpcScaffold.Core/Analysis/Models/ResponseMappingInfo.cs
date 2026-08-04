using System;
using System.Collections.Generic;
using System.Text;

namespace GrpcScaffold.Core.Analysis.Models;

public sealed record MappingField
{
    public required string Destination { get; init; }

    public required bool IsRepeated { get; init; }

    public required string AssignmentExpression { get; init; }

    public string? CollectionExpression { get; init; }
}
