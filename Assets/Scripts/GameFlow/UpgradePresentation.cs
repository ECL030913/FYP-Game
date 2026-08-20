public readonly struct UpgradePresentation
{
    public UpgradePresentation(string title, string description, string valueChange)
    {
        Title = title;
        Description = description;
        ValueChange = valueChange;
    }

    public string Title { get; }
    public string Description { get; }
    public string ValueChange { get; }
}
