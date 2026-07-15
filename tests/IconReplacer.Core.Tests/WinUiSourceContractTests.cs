using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace IconReplacer.Core.Tests;

public sealed class WinUiSourceContractTests
{
    private static readonly HashSet<string> InteractiveControlNames =
    [
        "AutoSuggestBox",
        "Button",
        "GridView",
        "HyperlinkButton",
        "ListView",
        "NavigationView",
        "NavigationViewItem",
        "RadioButtons",
        "Slider",
        "Thumb"
    ];

    [Fact]
    public void MainPageInteractiveControlsHaveStableAutomationIds()
    {
        var document = XDocument.Load(SourcePath("src", "IconReplacer.App", "MainPage.xaml"));
        var controls = document
            .Descendants()
            .Where(element => InteractiveControlNames.Contains(element.Name.LocalName))
            .ToArray();

        Assert.NotEmpty(controls);
        var missing = controls
            .Where(element => string.IsNullOrWhiteSpace(Attribute(element, "AutomationProperties.AutomationId")))
            .Select(Describe)
            .ToArray();

        Assert.True(missing.Length == 0, $"Interactive controls without AutomationId: {string.Join(", ", missing)}");
    }

    [Fact]
    public void MainPageHighContrastResourcesAliasSystemBrushes()
    {
        var document = XDocument.Load(SourcePath("src", "IconReplacer.App", "MainPage.xaml"));
        var highContrast = Assert.Single(
            document.Descendants(),
            element =>
                element.Name.LocalName == "ResourceDictionary" &&
                Attribute(element, "Key") == "HighContrast");
        var resources = highContrast.Elements().ToArray();
        var brushResources = resources
            .Where(resource => resource.Name.LocalName == "StaticResource")
            .ToArray();

        Assert.Equal(7, brushResources.Length);
        Assert.All(brushResources, resource =>
        {
            var resourceKey = Attribute(resource, "ResourceKey");
            Assert.StartsWith("SystemColor", resourceKey, StringComparison.Ordinal);
            Assert.EndsWith("Brush", resourceKey, StringComparison.Ordinal);
        });
        Assert.Single(
            document.Descendants(),
            resource =>
                resource.Name.LocalName == "String" &&
                Attribute(resource, "Key") == "AboutAppIconPath" &&
                resource.Value.Contains("AppIcon.png", StringComparison.Ordinal));
    }

    [Fact]
    public void ManagementAppDoesNotConsumeExplorerActivations()
    {
        var appSource = File.ReadAllText(SourcePath("src", "IconReplacer.App", "App.xaml.cs"));
        var viewModelSource = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "ViewModels",
            "MainPageViewModel.cs"));
        var commandHostSource = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.CommandHost",
            "Program.cs"));

        Assert.DoesNotContain("pending-activation", appSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("args.Arguments", appSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AppActivationService", viewModelSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ActivatedIconChangeService", viewModelSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ActivatedMenuApplyService", viewModelSource, StringComparison.Ordinal);

        Assert.Contains("AppActivationService", commandHostSource, StringComparison.Ordinal);
        Assert.Contains("ActivatedIconChangeService", commandHostSource, StringComparison.Ordinal);
        Assert.Contains("ActivatedMenuApplyService", commandHostSource, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowActivatesBeforeConstructingTheGallery()
    {
        var appSource = File.ReadAllText(SourcePath("src", "IconReplacer.App", "App.xaml.cs"));
        var windowSource = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "MainWindow.xaml.cs"));
        var windowXaml = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "MainWindow.xaml"));

        var assignWindow = appSource.IndexOf("Window = window;", StringComparison.Ordinal);
        var activateWindow = appSource.IndexOf("window.Activate();", StringComparison.Ordinal);
        var enqueueGallery = appSource.IndexOf("DispatcherQueue.TryEnqueue(", StringComparison.Ordinal);
        Assert.True(
            assignWindow >= 0 && activateWindow > assignWindow && enqueueGallery > activateWindow,
            "The lightweight window must activate before the gallery is queued for construction.");
        Assert.Contains(
            "Microsoft.UI.Dispatching.DispatcherQueuePriority.Low",
            appSource,
            StringComparison.Ordinal);
        Assert.Contains("window.ShowMainPage();", appSource, StringComparison.Ordinal);

        var constructor = windowSource.IndexOf("public MainWindow()", StringComparison.Ordinal);
        var showMainPage = windowSource.IndexOf("internal void ShowMainPage()", StringComparison.Ordinal);
        Assert.True(constructor >= 0 && showMainPage > constructor);
        Assert.DoesNotContain(
            "RootFrame.Navigate",
            windowSource[constructor..showMainPage],
            StringComparison.Ordinal);
        Assert.Contains(
            "RootFrame.Navigate(typeof(MainPage))",
            windowSource[showMainPage..],
            StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StartupProgress\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("IsActive=\"True\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("StartupProgress.IsActive = false;", windowSource, StringComparison.Ordinal);
        Assert.Contains(
            "StartupProgress.Visibility = Visibility.Collapsed;",
            windowSource,
            StringComparison.Ordinal);
        Assert.Contains("AppContext.BaseDirectory", windowSource, StringComparison.Ordinal);
        Assert.Contains("File.Exists(appIconPath)", windowSource, StringComparison.Ordinal);
        Assert.Contains("AppWindow.SetIcon(appIconPath);", windowSource, StringComparison.Ordinal);
        Assert.Contains("AppIcon.ico", windowSource, StringComparison.Ordinal);
        Assert.Contains("AppIcon.png", windowSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AppIcon.Light", windowSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AppIcon.Dark", windowSource, StringComparison.Ordinal);
        Assert.Contains("AppTitleBar.IconSource = new ImageIconSource", windowSource, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "AppWindow.SetIcon(\"Assets/AppIcon.ico\")",
            windowSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void OfficialIconIsGeneratedPackagedAndUsedByExplorer()
    {
        var iconSource = SourcePath("assets", "icon.png");
        Assert.Equal(
            "B09AABDB6B370985C6463F2A93CBDB08AF47919AB9D4715FBC7550283B331A8F",
            Sha256(iconSource));

        var project = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "IconReplacer.App.csproj"));
        var shellSource = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.ShellExtension",
            "IconReplacer.ShellExtension.cpp"));
        var assetScript = File.ReadAllText(SourcePath("scripts", "Build-AppIconAssets.py"));
        var packageScript = File.ReadAllText(SourcePath("scripts", "Build-MsixPackage.ps1"));

        Assert.Contains("Assets\\AppIcon.ico", project, StringComparison.Ordinal);
        Assert.Contains("Assets\\AppIcon.png", project, StringComparison.Ordinal);
        Assert.DoesNotContain("AppIcon.Light", project, StringComparison.Ordinal);
        Assert.DoesNotContain("AppIcon.Dark", project, StringComparison.Ordinal);
        Assert.Contains("AppIcon.ico", shellSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AppsUseLightTheme", shellSource, StringComparison.Ordinal);
        Assert.Contains("icon.png", assetScript, StringComparison.Ordinal);
        Assert.Contains("AppIcon.png", assetScript, StringComparison.Ordinal);
        Assert.DoesNotContain("icon-light.png", assetScript, StringComparison.Ordinal);
        Assert.DoesNotContain("icon-dark.png", assetScript, StringComparison.Ordinal);
        Assert.Contains("AppIcon.ico", packageScript, StringComparison.Ordinal);
        Assert.Contains("AppIcon.png", packageScript, StringComparison.Ordinal);

        var generatedIcon = File.ReadAllBytes(SourcePath(
            "src", "IconReplacer.App", "Assets", "AppIcon.ico"));
        Assert.NotEmpty(generatedIcon);
        Assert.False(File.Exists(SourcePath(
            "src", "IconReplacer.App", "Assets", "AppIcon.Light.ico")));
        Assert.False(File.Exists(SourcePath(
            "src", "IconReplacer.App", "Assets", "AppIcon.Dark.ico")));
        Assert.False(File.Exists(SourcePath(
            "src", "IconReplacer.App", "Assets", "AppIcon.Light.png")));
        Assert.False(File.Exists(SourcePath(
            "src", "IconReplacer.App", "Assets", "AppIcon.Dark.png")));
    }

    [Fact]
    public void PackageManifestRegistersBothExplorerSurfaces()
    {
        var document = XDocument.Load(SourcePath(
            "src",
            "IconReplacer.App",
            "Package.appxmanifest"));
        var extensionCategories = document
            .Descendants()
            .Where(element => element.Name.LocalName == "Extension")
            .Select(element => Attribute(element, "Category"))
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .ToArray();

        Assert.Contains("windows.comServer", extensionCategories);
        Assert.Contains("windows.fileExplorerContextMenus", extensionCategories);
        Assert.Contains("windows.fileExplorerClassicContextMenuHandler", extensionCategories);

        var modernItemTypes = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ItemType")
            .Select(element => Attribute(element, "Type"))
            .ToArray();
        var classicItemTypes = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ExtensionHandler")
            .Select(element => Attribute(element, "Type"))
            .ToArray();

        Assert.Contains("Directory", modernItemTypes);
        Assert.Contains(".lnk", modernItemTypes);
        Assert.Contains("Directory", classicItemTypes);
        Assert.Contains(".lnk", classicItemTypes);
    }

    [Fact]
    public void PackageScriptsDeriveCandidateNameFromManifestIdentity()
    {
        var packageScript = File.ReadAllText(SourcePath("scripts", "Build-MsixPackage.ps1"));
        var lifecycleScript = File.ReadAllText(SourcePath("scripts", "Test-MsixLifecycle.ps1"));

        Assert.Contains("$sourceIdentity = $sourceManifestXml.Package.Identity", packageScript, StringComparison.Ordinal);
        Assert.Contains("${packageName}_${packageVersion}_$Platform.msix", packageScript, StringComparison.Ordinal);
        Assert.Contains("$sourceIdentity = $sourceManifestXml.Package.Identity", lifecycleScript, StringComparison.Ordinal);
        Assert.Contains("$($sourceIdentity.Name)_$($sourceIdentity.Version)_x64.msix", lifecycleScript, StringComparison.Ordinal);
        Assert.DoesNotContain("IconReplacer_1.0.0.0_x64.msix", packageScript, StringComparison.Ordinal);
        Assert.DoesNotContain("IconReplacer_1.0.0.0_x64.msix", lifecycleScript, StringComparison.Ordinal);
    }

    [Fact]
    public void LifecycleScriptRequiresExplicitApprovalBeforePackageMutation()
    {
        var script = File.ReadAllText(SourcePath("scripts", "Test-MsixLifecycle.ps1"));
        var approvalGate = script.IndexOf(
            "if (!$SnapshotOnly -and !$ApproveExplorerRegistration)",
            StringComparison.Ordinal);
        var cleanBaselineGate = script.IndexOf(
            "if ($installedPackagesBefore.Count -gt 0)",
            StringComparison.Ordinal);
        var recoveryMutationStart = script.IndexOf(
            "$packageMutationStarted = $true",
            StringComparison.Ordinal);
        var normalMutationStart = script.LastIndexOf(
            "$packageMutationStarted = $true",
            StringComparison.Ordinal);

        Assert.True(approvalGate >= 0, "The lifecycle script is missing its explicit Explorer approval gate.");
        Assert.True(cleanBaselineGate >= 0, "The lifecycle script must reject a preinstalled package.");
        Assert.True(
            recoveryMutationStart > approvalGate,
            "The approval gate must run before interrupted-proof recovery mutation starts.");
        Assert.True(
            normalMutationStart > approvalGate && normalMutationStart > cleanBaselineGate,
            "Approval and clean-baseline gates must run before a new proof mutates the package.");
        Assert.Contains(
            "Refusing to modify Explorer registration without -ApproveExplorerRegistration.",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "Refusing to replace an existing Icon Replacer package.",
            script,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SnapshotOnlyLifecycleCapturesVersionedDeploymentReadiness()
    {
        var script = File.ReadAllText(SourcePath("scripts", "Test-MsixLifecycle.ps1"));

        Assert.Contains("function Get-MsixPackageSnapshot", script, StringComparison.Ordinal);
        Assert.Contains("function Get-PackageDeploymentReadiness", script, StringComparison.Ordinal);
        Assert.Contains("candidatePackage = $candidatePackage", script, StringComparison.Ordinal);
        Assert.Contains("deploymentReadiness = $deploymentReadiness", script, StringComparison.Ordinal);
        Assert.Contains("Candidate $candidateVersion is a signed update over installed $installedVersion.", script, StringComparison.Ordinal);

        var snapshotBranch = script.IndexOf("if ($SnapshotOnly)", StringComparison.Ordinal);
        var mutationStart = script.LastIndexOf("$packageMutationStarted = $true", StringComparison.Ordinal);
        Assert.True(snapshotBranch >= 0 && mutationStart > snapshotBranch);
    }

    [Fact]
    public void GuardedUpgradeRequiresApprovalNewerCandidateAndKeepInstalled()
    {
        var script = File.ReadAllText(SourcePath("scripts", "Test-MsixLifecycle.ps1"));

        Assert.Contains("[switch]$UpgradeInstalledPackage", script, StringComparison.Ordinal);
        Assert.Contains("UpgradeInstalledPackage requires KeepInstalled", script, StringComparison.Ordinal);
        Assert.Contains("UpgradeInstalledPackage requires ApproveExplorerRestart", script, StringComparison.Ordinal);
        Assert.Contains("if (!$UpgradeInstalledPackage)", script, StringComparison.Ordinal);
        Assert.Contains("$upgradeReadiness.mode -ne \"upgrade\"", script, StringComparison.Ordinal);
        Assert.Contains("$deploymentMode = \"upgrade\"", script, StringComparison.Ordinal);
        Assert.Contains("Add-AppxPackage -Path $PackagePath -ErrorAction Stop", script, StringComparison.Ordinal);
        Assert.Contains("deploymentMode = $deploymentMode", script, StringComparison.Ordinal);
        Assert.Contains("iconLibraryPreserved = $libraryPreservedAfterInstall", script, StringComparison.Ordinal);
        Assert.Contains("restoreStatePreserved = $restoreStatePreservedAfterInstall", script, StringComparison.Ordinal);

        var approvalGate = script.IndexOf(
            "if (!$SnapshotOnly -and !$ApproveExplorerRegistration)",
            StringComparison.Ordinal);
        var upgradeGate = script.IndexOf(
            "if (!$UpgradeInstalledPackage)",
            StringComparison.Ordinal);
        var mutationStart = script.LastIndexOf(
            "$packageMutationStarted = $true",
            StringComparison.Ordinal);
        Assert.True(approvalGate >= 0 && upgradeGate > approvalGate && mutationStart > upgradeGate);
    }

    [Fact]
    public void GuardedUpgradeRestartsOnlyTheExplorerShellAndVerifiesRecovery()
    {
        var script = File.ReadAllText(SourcePath("scripts", "Test-MsixLifecycle.ps1"));

        Assert.Contains("function Stop-ExplorerShell", script, StringComparison.Ordinal);
        Assert.Contains("function Start-ExplorerShell", script, StringComparison.Ordinal);
        Assert.Contains("function Assert-ExplorerRestartedHealthy", script, StringComparison.Ordinal);
        Assert.Contains("Stop-Process -Id $shellProcessId -Force", script, StringComparison.Ordinal);
        Assert.Contains("Start-Process -FilePath (Join-Path $env:WINDIR \"explorer.exe\")", script, StringComparison.Ordinal);
        Assert.Contains("Get-ExplorerModuleIdentities", script, StringComparison.Ordinal);
        Assert.Contains("function Get-IconReplacerPackageModuleHolders", script, StringComparison.Ordinal);
        Assert.Contains("function Stop-IconReplacerPackageProcesses", script, StringComparison.Ordinal);
        Assert.Contains("Add-AppxPackage -Path $PackagePath -ForceApplicationShutdown", script, StringComparison.Ordinal);
        Assert.Contains("explorerRestartApproved = [bool]$ApproveExplorerRestart", script, StringComparison.Ordinal);
        Assert.Contains("explorerRestarted = $explorerRestarted", script, StringComparison.Ordinal);
    }

    [Fact]
    public void InteractiveExplorerProofOwnsCleanup()
    {
        var script = File.ReadAllText(SourcePath("scripts", "Test-MsixLifecycle.ps1"));
        var cleanupStatus = script.IndexOf(
            "registration-removed-awaiting-evidence-review",
            StringComparison.Ordinal);
        var lifecycleEvidence = script.IndexOf("$evidence = [ordered]@{", StringComparison.Ordinal);

        Assert.Contains("[switch]$InteractiveProof", script, StringComparison.Ordinal);
        Assert.Contains("if ($KeepInstalled -and $InteractiveProof)", script, StringComparison.Ordinal);
        Assert.Contains("if (!$KeepInstalled)", script, StringComparison.Ordinal);
        Assert.True(cleanupStatus >= 0, "Interactive proof cleanup status is missing.");
        Assert.True(
            lifecycleEvidence > cleanupStatus,
            "Final interactive cleanup status must be included in lifecycle evidence.");
        Assert.Contains("restoreStateBaselineCaptured = $true", script, StringComparison.Ordinal);
        Assert.Contains("Restore-FileContent", script, StringComparison.Ordinal);
        Assert.Contains("restoreStateRestored = $true", script, StringComparison.Ordinal);
        Assert.Contains("Assert-ContextMenuStatePreserved", script, StringComparison.Ordinal);
        Assert.Contains("Assert-ExplorerSessionPreserved", script, StringComparison.Ordinal);
    }

    [Fact]
    public void InterruptedExplorerProofHasProtectedRecoveryPath()
    {
        var script = File.ReadAllText(SourcePath("scripts", "Test-MsixLifecycle.ps1"));
        var protectedBackupWrite = script.IndexOf(
            "WriteAllBytes($restoreStateRecoveryPath, $protectedRestoreState)",
            StringComparison.Ordinal);
        var normalMutationStart = script.LastIndexOf(
            "$packageMutationStarted = $true",
            StringComparison.Ordinal);

        Assert.Contains("[switch]$RecoverInterruptedProof", script, StringComparison.Ordinal);
        Assert.Contains("[string]$RecoverySessionPath", script, StringComparison.Ordinal);
        Assert.Contains("ProtectedData]::Protect", script, StringComparison.Ordinal);
        Assert.Contains("ProtectedData]::Unprotect", script, StringComparison.Ordinal);
        Assert.Contains("schemaVersion = 1", script, StringComparison.Ordinal);
        Assert.Contains("contextMenuBefore = $contextMenuBefore", script, StringComparison.Ordinal);
        Assert.Contains("explorerBefore = $explorerBefore", script, StringComparison.Ordinal);
        Assert.Contains("interrupted-proof-recovered", script, StringComparison.Ordinal);
        Assert.True(
            protectedBackupWrite >= 0 && normalMutationStart > protectedBackupWrite,
            "The protected recovery baseline must be persisted before normal package mutation.");
    }

    [Fact]
    public void ManagementOperationsAreSerializedAndKeepActionableFeedback()
    {
        var viewModel = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "ViewModels",
            "MainPageViewModel.cs"));
        var models = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "ViewModels",
            "MainPageModels.cs"));
        var xaml = File.ReadAllText(SourcePath("src", "IconReplacer.App", "MainPage.xaml"));

        Assert.Equal(
            3,
            Regex.Matches(
                viewModel,
                @"\[RelayCommand\(CanExecute = nameof\(CanRunLibraryOperation\)\)\]").Count);
        Assert.Contains(
            "private bool CanRunLibraryOperation() => _paths is not null && !IsBusy;",
            viewModel,
            StringComparison.Ordinal);
        Assert.Contains("item.NotifyCanExecuteChanged();", viewModel, StringComparison.Ordinal);
        Assert.Contains("The icons could not be imported.", viewModel, StringComparison.Ordinal);
        Assert.Contains("The original icon could not be restored.", viewModel, StringComparison.Ordinal);

        var feedback = viewModel.IndexOf(
            "var feedback = _feedbackService.FromBatchImport(import.Value);",
            StringComparison.Ordinal);
        var refresh = viewModel.IndexOf(
            "if (await RefreshCoreAsync(clearStatus: true))",
            feedback,
            StringComparison.Ordinal);
        var showFeedback = viewModel.IndexOf("ShowFeedback(feedback);", refresh, StringComparison.Ordinal);
        Assert.True(
            feedback >= 0 && refresh > feedback && showFeedback > refresh,
            "Import feedback must be shown after the refreshed catalog succeeds.");

        Assert.Contains("Func<bool>? canRunRestore = null", models, StringComparison.Ordinal);
        Assert.Contains("NotifyCanExecuteChanged()", models, StringComparison.Ordinal);
        Assert.DoesNotContain("IsEnabled=\"{x:Bind CanRestore}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void GalleryFirstAccessibilityNamesAndLiveRegionsStayActionable()
    {
        var models = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "ViewModels",
            "MainPageModels.cs"));
        var xaml = File.ReadAllText(SourcePath("src", "IconReplacer.App", "MainPage.xaml"));
        var accessibilityScript = File.ReadAllText(SourcePath(
            "tests",
            "ui",
            "accessibility-ui.ps1"));

        Assert.Contains("public string AccessibilityName =>", models, StringComparison.Ordinal);
        Assert.Contains("public string RestoreAutomationName =>", models, StringComparison.Ordinal);
        Assert.Contains(
            "AutomationProperties.Name=\"{x:Bind AccessibilityName}\"",
            xaml,
            StringComparison.Ordinal);
        Assert.Equal(
            2,
            Regex.Matches(
                xaml,
                "AutomationProperties.Name=\"{x:Bind RestoreAutomationName}\"").Count);
        Assert.Contains(
            "AutomationProperties.LocalizedControlType=\"splitter\"",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains(
            "AutomationProperties.AutomationId=\"EmptyLibraryState\"",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains(
            "AutomationProperties.AutomationId=\"SettingsLibraryPath\"",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains(
            "AutomationProperties.Name=\"Open Icon Library\"",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"AboutSection\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ProjectGitHubLink\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"CheckForUpdatesButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"UpdateStatusPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ToolbarCompact\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ToolbarNarrow\"", xaml, StringComparison.Ordinal);
        Assert.Contains(
            "Target=\"LibraryActionsPanel.(Grid.Row)\" Value=\"1\"",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains(
            "Target=\"SearchBox.(Grid.ColumnSpan)\" Value=\"3\"",
            xaml,
            StringComparison.Ordinal);
        Assert.True(
            Regex.Matches(xaml, "AutomationProperties.LiveSetting=\"Polite\"").Count >= 4,
            "Search results, empty state, operation status, and update status must be live regions.");
        Assert.Contains("IconSearchBox", accessibilityScript, StringComparison.Ordinal);
        Assert.Contains("CollectionsSplitter", accessibilityScript, StringComparison.Ordinal);
        Assert.Contains("Restore-", accessibilityScript, StringComparison.Ordinal);
        Assert.Contains("AboutVersionText", accessibilityScript, StringComparison.Ordinal);
        Assert.Contains("CheckForUpdatesButton", accessibilityScript, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdatedInfoBarContentReopensForScreenReaders()
    {
        var viewModel = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "ViewModels",
            "MainPageViewModel.cs"));
        var method = viewModel.IndexOf("private void OpenStatus(", StringComparison.Ordinal);
        var close = viewModel.IndexOf("IsStatusOpen = false;", method, StringComparison.Ordinal);
        var title = viewModel.IndexOf("StatusTitle = title;", close, StringComparison.Ordinal);
        var message = viewModel.IndexOf("StatusMessage = message;", title, StringComparison.Ordinal);
        var open = viewModel.IndexOf("IsStatusOpen = true;", message, StringComparison.Ordinal);

        Assert.True(method >= 0, "The shared status-opening method is missing.");
        Assert.True(
            close > method && title > close && message > title && open > message,
            "InfoBar feedback must close, update its content, and reopen in that order.");
        Assert.Equal(4, Regex.Matches(viewModel, @"OpenStatus\(").Count);
    }

    [Fact]
    public void GalleryRefreshPreservesUnchangedThumbnailCacheEntries()
    {
        var pageSource = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "MainPage.xaml.cs"));
        var cacheSource = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "IconThumbnailCache.cs"));

        var catalogChanged = pageSource.IndexOf(
            "private void ViewModel_CatalogChanged",
            StringComparison.Ordinal);
        var nextMethod = pageSource.IndexOf(
            "private void IconGrid_SizeChanged",
            catalogChanged,
            StringComparison.Ordinal);
        Assert.True(catalogChanged >= 0 && nextMethod > catalogChanged);
        Assert.DoesNotContain(
            "_thumbnailCache.Clear()",
            pageSource[catalogChanged..nextMethod],
            StringComparison.Ordinal);

        Assert.Contains("file.LastWriteTimeUtc.Ticks", cacheSource, StringComparison.Ordinal);
        Assert.Contains("file.Length", cacheSource, StringComparison.Ordinal);
        Assert.Contains("cached.Version == version", cacheSource, StringComparison.Ordinal);
        Assert.Contains("currentVersion != version", cacheSource, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals(current.Task, loading)", cacheSource, StringComparison.Ordinal);
    }

    [Fact]
    public void IconPreviewsDecodeAtTheRenderedDpiWithoutDowngradingCachedImages()
    {
        var pageSource = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "MainPage.xaml.cs"));
        var pageXaml = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "MainPage.xaml"));
        var cacheSource = File.ReadAllText(SourcePath(
            "src",
            "IconReplacer.App",
            "IconThumbnailCache.cs"));

        Assert.Contains("XamlRoot?.RasterizationScale", pageSource, StringComparison.Ordinal);
        Assert.Contains("GetRequiredDecodePixelWidth(image)", pageSource, StringComparison.Ordinal);
        Assert.Contains(
            "currentThumbnail.DecodePixelWidth >= decodePixelWidth",
            pageSource,
            StringComparison.Ordinal);
        Assert.Contains("SizeChanged=\"IconPreview_SizeChanged\"", pageXaml, StringComparison.Ordinal);
        Assert.Contains("NormalizeDecodePixelWidth", cacheSource, StringComparison.Ordinal);
        Assert.Contains("<= 64 => 64", cacheSource, StringComparison.Ordinal);
        Assert.Contains("<= 128 => 128", cacheSource, StringComparison.Ordinal);
        Assert.Contains("_ => 256", cacheSource, StringComparison.Ordinal);
        Assert.Contains("new CacheKey(path", cacheSource, StringComparison.Ordinal);
    }

    private static string Sha256(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static string SourcePath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "IconReplacer.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. segments]);
    }

    private static string? Attribute(XElement element, string localName) =>
        element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == localName)?.Value;

    private static string Describe(XElement element)
    {
        var name = Attribute(element, "Name");
        var content = Attribute(element, "Content");
        return string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(content)
            ? element.Name.LocalName
            : $"{element.Name.LocalName}({name ?? content})";
    }
}
