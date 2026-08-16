namespace IconReplacer.AppModel;

public sealed record NativeToolingSnapshot(
    bool IsChecked,
    bool CompilerAvailable,
    bool MsBuildAvailable,
    bool CMakeAvailable,
    string CompilerDetail,
    string MsBuildDetail,
    string CMakeDetail)
{
    public static NativeToolingSnapshot NotChecked { get; } = new(
        IsChecked: false,
        CompilerAvailable: false,
        MsBuildAvailable: false,
        CMakeAvailable: false,
        "cl.exe was not checked.",
        "Visual Studio MSBuild was not checked.",
        "CMake was not checked.");

    public bool CanBuildNativeExtension => CompilerAvailable && MsBuildAvailable && CMakeAvailable;

    public string Summary => IsChecked
        ? $"cl.exe: {Format(CompilerAvailable)}; MSBuild: {Format(MsBuildAvailable)}; CMake: {Format(CMakeAvailable)}."
        : "Native build tools were not checked.";

    private static string Format(bool available)
    {
        return available ? "available" : "missing";
    }
}
