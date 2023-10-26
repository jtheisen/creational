public static class RazorHelpers
{
    public static String ClassNames(params String[] classes) => String.Join(' ', classes);

    public static T If<T>(this T source, Boolean predicate) => predicate ? source : default;
}
