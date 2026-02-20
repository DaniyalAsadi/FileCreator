using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Core.Helpers;

public sealed class ImportedEndpoint
{
    public string GroupName { get; set; } = default!;
    public string Name { get; init; } = default!;
    public string Route { get; init; } = default!;
    public HttpVerb Verb { get; init; }

    // user-editable
    public bool Selected { get; set; }
    public RequestType RequestType { get; set; } = RequestType.Command;
    public bool HasRequest { get; set; } = true;
    public bool HasResponse { get; set; }
    public ResponseType ResponseType { get; set; } = ResponseType.Single;
}
