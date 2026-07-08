using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class ReleaseReadinessService
{
    public const string IntegrationPath = "Modern MSIX + IExplorerCommand";

    private readonly PackagingPlanService _packagingPlanService;
    private readonly AccessibilityPlanService _accessibilityPlanService;
    private readonly AppDiagnosticsService _diagnosticsService;

    public ReleaseReadinessService(
        PackagingPlanService? packagingPlanService = null,
        AccessibilityPlanService? accessibilityPlanService = null,
        AppDiagnosticsService? diagnosticsService = null)
    {
        _packagingPlanService = packagingPlanService ?? new PackagingPlanService();
        _accessibilityPlanService = accessibilityPlanService ?? new AccessibilityPlanService();
        _diagnosticsService = diagnosticsService ?? new AppDiagnosticsService();
    }

    public OperationResult<ReleaseReadinessSnapshot> GetReadiness(
        IconLibraryPaths paths,
        ReleaseReadinessInputs? inputs = null)
    {
        var currentInputs = inputs ?? ReleaseReadinessInputs.NotChecked;
        var packagingPlan = _packagingPlanService.GetPlan(currentInputs.PackagingInputs);
        var accessibilityPlan = _accessibilityPlanService.GetPlan();
        var diagnostics = _diagnosticsService.GetDiagnostics(
            paths,
            currentInputs.PackagingInputs.WinUiTooling,
            currentInputs.PackagingInputs.NativeTooling);
        if (!diagnostics.Succeeded || diagnostics.Value is null)
        {
            return OperationResult<ReleaseReadinessSnapshot>.Failure(diagnostics.Error);
        }

        var items = new List<ReleaseReadinessItem>
        {
            ProofGate(
                "build-proof",
                "Build proof captured",
                currentInputs.BuildPassed,
                "The solution build passed.",
                "Run and record a clean solution build before release.",
                "dotnet build IconReplacer.slnx"),
            ProofGate(
                "test-proof",
                "Test proof captured",
                currentInputs.TestsPassed,
                "The automated test suite passed.",
                "Run and record the automated test suite before release.",
                "dotnet test IconReplacer.slnx --no-build"),
            ProofGate(
                "cli-proof",
                "CLI proof captured",
                currentInputs.CliProofCaptured,
                "Representative CLI proof has been captured.",
                "Capture the CLI proof matrix from docs/qa/TEST-PLAN.md.",
                "dotnet run --no-build --project src\\IconReplacer.Cli -- release-readiness"),
            DiagnosticsGate(diagnostics.Value),
            PackagingGate(packagingPlan),
            AccessibilityGate(accessibilityPlan, currentInputs.AccessibilityProofCaptured),
            ProofGate(
                "manual-explorer-proof",
                "Manual Explorer proof captured",
                currentInputs.ManualExplorerProofCaptured,
                "Explorer context menu, picker, apply, restore, and error screenshots are captured.",
                "Capture the manual Explorer proof checklist before release.",
                "docs\\verification\\MANUAL_EXPLORER_PROOF.md"),
            ProofGate(
                "release-evidence-packet",
                "Release evidence packet captured",
                currentInputs.ReleaseEvidenceCaptured,
                "The release evidence packet is filled in.",
                "Fill the release evidence template with command, manual, install, uninstall, and known-limit evidence.",
                "docs\\verification\\RELEASE_EVIDENCE_TEMPLATE.md")
        };

        return OperationResult<ReleaseReadinessSnapshot>.Success(new ReleaseReadinessSnapshot(
            IntegrationPath,
            items,
            packagingPlan,
            accessibilityPlan,
            diagnostics.Value,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<ReleaseReadinessSnapshot> GetReadinessFromEnvironment(
        ReleaseReadinessInputs? inputs = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<ReleaseReadinessSnapshot>.Failure(paths.Error);
        }

        return GetReadiness(paths.Value, inputs);
    }

    private static ReleaseReadinessItem DiagnosticsGate(AppDiagnosticsSnapshot diagnostics)
    {
        if (diagnostics.HasBlockingIssues)
        {
            return Blocking(
                "diagnostics",
                "Diagnostics have blockers",
                $"{diagnostics.BlockingCount} diagnostic blockers must be resolved before release.",
                "dotnet run --no-build --project src\\IconReplacer.Cli -- diagnostics");
        }

        if (diagnostics.WarningCount > 0)
        {
            return Warning(
                "diagnostics",
                "Diagnostics have warnings",
                $"{diagnostics.WarningCount} diagnostic warnings need release owner review.",
                "dotnet run --no-build --project src\\IconReplacer.Cli -- diagnostics");
        }

        return Pass(
            "diagnostics",
            "Diagnostics are release-ready",
            "Diagnostics have no blockers or warnings.",
            "dotnet run --no-build --project src\\IconReplacer.Cli -- diagnostics");
    }

    private static ReleaseReadinessItem PackagingGate(PackagingPlanSnapshot packagingPlan)
    {
        if (packagingPlan.HasBlockingIssues)
        {
            return Blocking(
                "package-plan",
                "Package plan has blockers",
                $"{packagingPlan.BlockingCount} package blockers must be resolved before release.",
                "dotnet run --no-build --project src\\IconReplacer.Cli -- package-plan");
        }

        if (packagingPlan.WarningCount > 0)
        {
            return Warning(
                "package-plan",
                "Package proof has warnings",
                $"{packagingPlan.WarningCount} package proof items remain before release.",
                "dotnet run --no-build --project src\\IconReplacer.Cli -- package-plan");
        }

        return Pass(
            "package-plan",
            "Package plan is release-ready",
            "Package, install, uninstall, and preservation gates are satisfied.",
            "dotnet run --no-build --project src\\IconReplacer.Cli -- package-plan");
    }

    private static ReleaseReadinessItem AccessibilityGate(
        AccessibilityPlanSnapshot accessibilityPlan,
        bool accessibilityProofCaptured)
    {
        if (accessibilityPlan.RequiresManualProof && !accessibilityProofCaptured)
        {
            return Blocking(
                "accessibility-proof",
                "Accessibility proof missing",
                $"{accessibilityPlan.ManualProofCount} manual accessibility proof items remain.",
                "dotnet run --no-build --project src\\IconReplacer.Cli -- accessibility-plan");
        }

        return Pass(
            "accessibility-proof",
            "Accessibility proof captured",
            "Accessibility requirements and manual proof are captured.",
            "dotnet run --no-build --project src\\IconReplacer.Cli -- accessibility-plan");
    }

    private static ReleaseReadinessItem ProofGate(
        string id,
        string title,
        bool isCaptured,
        string capturedDetail,
        string missingDetail,
        string evidenceCommand)
    {
        return isCaptured
            ? Pass(id, title, capturedDetail, evidenceCommand)
            : Blocking(id, title, missingDetail, evidenceCommand);
    }

    private static ReleaseReadinessItem Pass(
        string id,
        string title,
        string detail,
        string evidenceCommand)
    {
        return new ReleaseReadinessItem(
            id,
            AppDiagnosticStatus.Pass,
            title,
            detail,
            RequiredForRelease: true,
            evidenceCommand);
    }

    private static ReleaseReadinessItem Warning(
        string id,
        string title,
        string detail,
        string evidenceCommand)
    {
        return new ReleaseReadinessItem(
            id,
            AppDiagnosticStatus.Warning,
            title,
            detail,
            RequiredForRelease: true,
            evidenceCommand);
    }

    private static ReleaseReadinessItem Blocking(
        string id,
        string title,
        string detail,
        string evidenceCommand)
    {
        return new ReleaseReadinessItem(
            id,
            AppDiagnosticStatus.Blocking,
            title,
            detail,
            RequiredForRelease: true,
            evidenceCommand);
    }
}
