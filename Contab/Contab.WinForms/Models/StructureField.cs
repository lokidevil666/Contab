namespace Contab.WinForms.Models;

public enum StructureSection
{
    In,
    Out,
    Header
}

public sealed record StructureField(
    string Name,
    int Start,
    int Length,
    StructureSection Section,
    string DefaultValue,
    int Order
);
