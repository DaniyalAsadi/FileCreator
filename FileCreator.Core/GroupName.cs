using Humanizer;

namespace FileCreator.Core;

public sealed class GroupName
{
    public string Feature { get; }
    public string Resource { get; }

    private GroupName(string feature, string resource)
    {
        Feature = feature;
        Resource = resource;
    }

    public static GroupName Create(string raw)
    {

        return new GroupName(
            feature: ToFeatureName(raw),
            resource: ToResourceName(raw)
        );
    }
    public static string ToFeatureName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Group name is required.");

        // تبدیل به PascalCase استاندارد
        return $"The{raw.Pascalize().Singularize(false)}";
    }

    public static string ToResourceName(string raw)
    {
        // REST همیشه جمع است
        return raw.Pluralize(false);
    }
    public static bool IsPlural(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var pascal = input.Pascalize();

        // اگر Singularize تغییری ایجاد کند یعنی ورودی Plural بوده
        var singular = pascal.Singularize(false);

        return !string.Equals(pascal, singular, StringComparison.Ordinal);
    }
}
