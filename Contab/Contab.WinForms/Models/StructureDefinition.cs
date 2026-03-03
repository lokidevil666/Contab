namespace Contab.WinForms.Models;

public sealed class StructureDefinition
{
    public StructureDefinition(
        IReadOnlyList<StructureField> inputFields,
        IReadOnlyList<StructureField> outputFields,
        IReadOnlyList<StructureField> headerFields)
    {
        InputFields = inputFields;
        OutputFields = outputFields;
        HeaderFields = headerFields;
    }

    public IReadOnlyList<StructureField> InputFields { get; }

    public IReadOnlyList<StructureField> OutputFields { get; }

    public IReadOnlyList<StructureField> HeaderFields { get; }
}
