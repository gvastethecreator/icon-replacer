#include <windows.h>
#include <shlobj_core.h>
#include <shobjidl_core.h>

#include <chrono>
#include <iostream>
#include <string>

namespace
{
constexpr GUID CLSID_ChangeIcon =
{
    0xb8f1a86d,
    0x4c52,
    0x4c53,
    { 0xbf, 0x72, 0x30, 0xb5, 0x9f, 0x7f, 0x0f, 0x7d }
};

constexpr GUID CLSID_Collections =
{
    0xb8f1a86d,
    0x4c52,
    0x4c53,
    { 0xbf, 0x72, 0x30, 0xb5, 0x9f, 0x7f, 0x0f, 0x7e }
};

constexpr GUID CLSID_ClassicContextMenu =
{
    0xb8f1a86d,
    0x4c52,
    0x4c53,
    { 0xbf, 0x72, 0x30, 0xb5, 0x9f, 0x7f, 0x0f, 0x7f }
};

constexpr WORD ClassicMaxExpectedCommandCount = 242;
constexpr auto ClassicQueryBudget = std::chrono::milliseconds(250);

using DllGetClassObjectFunction = HRESULT(__stdcall*)(REFCLSID, REFIID, void**);

int Fail(const wchar_t* message, HRESULT result = E_FAIL)
{
    std::wcerr << L"FAIL: " << message << L" (0x" << std::hex << result << L")\n";
    return 1;
}

HRESULT CreateCommand(
    DllGetClassObjectFunction getClassObject,
    const GUID& classId,
    IExplorerCommand** command)
{
    *command = nullptr;
    IClassFactory* factory = nullptr;
    auto result = getClassObject(classId, IID_PPV_ARGS(&factory));
    if (FAILED(result))
    {
        return result;
    }

    result = factory->CreateInstance(nullptr, IID_PPV_ARGS(command));
    factory->Release();
    return result;
}

HRESULT ReadTitle(IExplorerCommand* command, std::wstring& title)
{
    PWSTR value = nullptr;
    const auto result = command->GetTitle(nullptr, &value);
    if (SUCCEEDED(result) && value != nullptr)
    {
        title = value;
    }

    CoTaskMemFree(value);
    return result;
}

HRESULT ReadIcon(IExplorerCommand* command, std::wstring& icon)
{
    PWSTR value = nullptr;
    const auto result = command->GetIcon(nullptr, &value);
    if (SUCCEEDED(result) && value != nullptr)
    {
        icon = value;
    }

    CoTaskMemFree(value);
    return result;
}

HRESULT VerifyClassicInvocationIsSuppressed(
    DllGetClassObjectFunction getClassObject,
    const GUID& classId,
    IShellItemArray* items)
{
    IExplorerCommand* command = nullptr;
    auto result = CreateCommand(getClassObject, classId, &command);
    if (FAILED(result))
    {
        return result;
    }

    EXPCMDSTATE state = ECS_DISABLED;
    result = command->GetState(items, FALSE, &state);
    if (FAILED(result) || state != ECS_ENABLED)
    {
        command->Release();
        return E_UNEXPECTED;
    }

    IObjectWithSelection* selectionAware = nullptr;
    result = command->QueryInterface(IID_PPV_ARGS(&selectionAware));
    if (SUCCEEDED(result))
    {
        result = selectionAware->SetSelection(items);
        selectionAware->Release();
    }

    state = ECS_ENABLED;
    if (SUCCEEDED(result))
    {
        result = command->GetState(items, FALSE, &state);
    }

    command->Release();
    return SUCCEEDED(result) && state == ECS_HIDDEN ? S_OK : E_UNEXPECTED;
}

HRESULT CreateSelectionDataObject(const std::wstring& path, IDataObject** dataObject)
{
    *dataObject = nullptr;
    PIDLIST_ABSOLUTE absoluteItem = nullptr;
    auto result = SHParseDisplayName(path.c_str(), nullptr, &absoluteItem, 0, nullptr);
    if (FAILED(result))
    {
        return result;
    }

    const auto parent = ILCloneFull(absoluteItem);
    if (parent == nullptr)
    {
        ILFree(absoluteItem);
        return E_OUTOFMEMORY;
    }

    const PCUITEMID_CHILD child = ILFindLastID(absoluteItem);
    if (!ILRemoveLastID(parent))
    {
        ILFree(parent);
        ILFree(absoluteItem);
        return HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME);
    }

    LPCITEMIDLIST children[] = { child };
    result = SHCreateDataObject(
        parent,
        1,
        children,
        nullptr,
        IID_PPV_ARGS(dataObject));
    ILFree(parent);
    ILFree(absoluteItem);
    return result;
}

bool ReadMenuItem(HMENU menu, UINT position, MENUITEMINFOW& item, std::wstring& title)
{
    wchar_t buffer[256]{};
    item = {};
    item.cbSize = sizeof(item);
    item.fMask = MIIM_FTYPE | MIIM_STRING | MIIM_BITMAP | MIIM_SUBMENU | MIIM_STATE | MIIM_DATA;
    item.dwTypeData = buffer;
    item.cch = static_cast<UINT>(std::size(buffer));
    if (!GetMenuItemInfoW(menu, position, TRUE, &item))
    {
        return false;
    }

    title = buffer;
    return true;
}

bool IsRenderableMenuBitmap(HBITMAP bitmap)
{
    BITMAP details{};
    if (bitmap == nullptr ||
        GetObjectW(bitmap, sizeof(details), &details) != sizeof(details) ||
        details.bmWidth <= 0 || details.bmHeight <= 0 ||
        details.bmBitsPixel != 32 || details.bmBits == nullptr)
    {
        return false;
    }

    const auto pixelCount = static_cast<size_t>(details.bmWidth) * details.bmHeight;
    const auto* pixels = static_cast<const DWORD*>(details.bmBits);
    for (size_t index = 0; index < pixelCount; ++index)
    {
        if ((pixels[index] & 0xff000000) != 0)
        {
            return true;
        }
    }

    return false;
}

HBITMAP ResolveMenuItemBitmap(const MENUITEMINFOW& item)
{
    return item.hbmpItem == HBMMENU_CALLBACK
        ? reinterpret_cast<HBITMAP>(item.dwItemData)
        : item.hbmpItem;
}

bool HasRenderableMenuPreview(const MENUITEMINFOW& item)
{
    return IsRenderableMenuBitmap(ResolveMenuItemBitmap(item));
}

bool RendersCallbackPreview(IContextMenu3* menu, const MENUITEMINFOW& item)
{
    if (menu == nullptr || item.hbmpItem != HBMMENU_CALLBACK || item.dwItemData == 0)
    {
        return false;
    }

    MEASUREITEMSTRUCT measure{};
    measure.CtlType = ODT_MENU;
    measure.itemID = item.wID;
    measure.itemData = item.dwItemData;
    LRESULT callbackResult = 0;
    if (FAILED(menu->HandleMenuMsg2(
            WM_MEASUREITEM,
            0,
            reinterpret_cast<LPARAM>(&measure),
            &callbackResult)) ||
        callbackResult == 0 || measure.itemWidth == 0 || measure.itemHeight == 0)
    {
        return false;
    }

    const auto screen = GetDC(nullptr);
    const auto target = screen == nullptr ? nullptr : CreateCompatibleDC(screen);
    const auto canvas = screen == nullptr
        ? nullptr
        : CreateCompatibleBitmap(screen, measure.itemWidth, measure.itemHeight);
    if (screen != nullptr)
    {
        ReleaseDC(nullptr, screen);
    }
    if (target == nullptr || canvas == nullptr)
    {
        if (canvas != nullptr)
        {
            DeleteObject(canvas);
        }
        if (target != nullptr)
        {
            DeleteDC(target);
        }
        return false;
    }

    const auto previous = SelectObject(target, canvas);
    PatBlt(target, 0, 0, measure.itemWidth, measure.itemHeight, WHITENESS);
    DRAWITEMSTRUCT draw{};
    draw.CtlType = ODT_MENU;
    draw.itemID = item.wID;
    draw.hDC = target;
    draw.rcItem = {
        0,
        0,
        static_cast<LONG>(measure.itemWidth),
        static_cast<LONG>(measure.itemHeight)
    };
    draw.itemData = item.dwItemData;
    callbackResult = 0;
    const auto handled = SUCCEEDED(menu->HandleMenuMsg2(
        WM_DRAWITEM,
        0,
        reinterpret_cast<LPARAM>(&draw),
        &callbackResult)) && callbackResult != 0;

    bool changedPixel = false;
    for (UINT y = 0; handled && !changedPixel && y < measure.itemHeight; ++y)
    {
        for (UINT x = 0; x < measure.itemWidth; ++x)
        {
            if (GetPixel(target, x, y) != RGB(255, 255, 255))
            {
                changedPixel = true;
                break;
            }
        }
    }

    SelectObject(target, previous);
    DeleteObject(canvas);
    DeleteDC(target);
    return handled && changedPixel;
}

bool UsesBitmapMenuLayout(HMENU menu)
{
    MENUINFO info{};
    info.cbSize = sizeof(info);
    info.fMask = MIM_STYLE;
    return menu != nullptr &&
        GetMenuInfo(menu, &info) != FALSE &&
        (info.dwStyle & MNS_CHECKORBMP) != 0;
}

bool AllMenuItemsHavePreviews(HMENU menu, bool requireSubmenu)
{
    const auto count = GetMenuItemCount(menu);
    if (count <= 0)
    {
        return false;
    }

    for (int position = 0; position < count; ++position)
    {
        MENUITEMINFOW item{};
        std::wstring title;
        if (!ReadMenuItem(menu, static_cast<UINT>(position), item, title))
        {
            return false;
        }

        if ((item.fState & (MFS_DISABLED | MFS_GRAYED)) != 0)
        {
            continue;
        }

        if (
            title.empty() ||
            !HasRenderableMenuPreview(item) ||
            (requireSubmenu && item.hSubMenu == nullptr))
        {
            return false;
        }
    }

    return true;
}

bool AllCollectionIconMenusHavePreviews(HMENU collectionsMenu)
{
    const auto count = GetMenuItemCount(collectionsMenu);
    for (int position = 0; position < count; ++position)
    {
        MENUITEMINFOW item{};
        std::wstring title;
        if (!ReadMenuItem(collectionsMenu, static_cast<UINT>(position), item, title))
        {
            return false;
        }

        if ((item.fState & (MFS_DISABLED | MFS_GRAYED)) != 0)
        {
            continue;
        }

        if (item.hSubMenu == nullptr || !AllMenuItemsHavePreviews(item.hSubMenu, false))
        {
            return false;
        }
    }

    return true;
}

bool MenuCommandIdsWithinRange(HMENU menu, UINT firstCommandId, UINT lastCommandId)
{
    const auto count = GetMenuItemCount(menu);
    for (int position = 0; position < count; ++position)
    {
        MENUITEMINFOW item{};
        item.cbSize = sizeof(item);
        item.fMask = MIIM_FTYPE | MIIM_ID | MIIM_STATE | MIIM_SUBMENU;
        if (!GetMenuItemInfoW(menu, static_cast<UINT>(position), TRUE, &item))
        {
            return false;
        }

        const auto isSeparator = (item.fType & MFT_SEPARATOR) != 0;
        const auto isDisabled = (item.fState & (MFS_DISABLED | MFS_GRAYED)) != 0;
        if (!isSeparator && !isDisabled && item.hSubMenu == nullptr &&
            (item.wID < firstCommandId || item.wID > lastCommandId))
        {
            return false;
        }

        if (item.hSubMenu != nullptr &&
            !MenuCommandIdsWithinRange(item.hSubMenu, firstCommandId, lastCommandId))
        {
            return false;
        }
    }

    return true;
}

class SmokeLibraryFixture
{
public:
    ~SmokeLibraryFixture()
    {
        if (!iconPath_.empty())
        {
            DeleteFileW(iconPath_.c_str());
        }
        if (!collectionPath_.empty())
        {
            RemoveDirectoryW(collectionPath_.c_str());
        }
        if (createdLibraryRoot_ && !libraryRoot_.empty())
        {
            RemoveDirectoryW(libraryRoot_.c_str());
        }
    }

    HRESULT Create(const std::wstring& sourceIcon)
    {
        PWSTR profilePath = nullptr;
        auto result = SHGetKnownFolderPath(FOLDERID_Profile, KF_FLAG_DEFAULT, nullptr, &profilePath);
        if (FAILED(result))
        {
            return result;
        }

        libraryRoot_ = std::wstring(profilePath) + L"\\.icons";
        CoTaskMemFree(profilePath);
        if (!CreateDirectoryW(libraryRoot_.c_str(), nullptr))
        {
            const auto error = GetLastError();
            if (error != ERROR_ALREADY_EXISTS)
            {
                return HRESULT_FROM_WIN32(error);
            }
        }
        else
        {
            createdLibraryRoot_ = true;
        }

        collectionPath_ = libraryRoot_ + L"\\.icon-replacer-smoke-" +
            std::to_wstring(GetCurrentProcessId());
        if (!CreateDirectoryW(collectionPath_.c_str(), nullptr) &&
            GetLastError() != ERROR_ALREADY_EXISTS)
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }

        iconPath_ = collectionPath_ + L"\\Smoke.ico";
        if (!CopyFileW(sourceIcon.c_str(), iconPath_.c_str(), FALSE))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }

        return S_OK;
    }

    const std::wstring& LibraryRoot() const
    {
        return libraryRoot_;
    }

private:
    std::wstring libraryRoot_;
    std::wstring collectionPath_;
    std::wstring iconPath_;
    bool createdLibraryRoot_ = false;
};

HRESULT CreateSmokeShortcut(
    const std::wstring& targetPath,
    std::wstring& shortcutPath,
    std::wstring& smokeDirectory)
{
    wchar_t temporaryPath[MAX_PATH]{};
    const auto length = GetTempPathW(static_cast<DWORD>(std::size(temporaryPath)), temporaryPath);
    if (length == 0 || length >= std::size(temporaryPath))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    smokeDirectory = std::wstring(temporaryPath) + L"IconReplacer.ShellExtension.Smoke";
    if (!CreateDirectoryW(smokeDirectory.c_str(), nullptr) && GetLastError() != ERROR_ALREADY_EXISTS)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    shortcutPath = smokeDirectory + L"\\ClassicTarget.lnk";
    DeleteFileW(shortcutPath.c_str());

    IShellLinkW* shortcut = nullptr;
    auto result = CoCreateInstance(
        CLSID_ShellLink,
        nullptr,
        CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(&shortcut));
    if (FAILED(result))
    {
        return result;
    }

    result = shortcut->SetPath(targetPath.c_str());
    IPersistFile* persistFile = nullptr;
    if (SUCCEEDED(result))
    {
        result = shortcut->QueryInterface(IID_PPV_ARGS(&persistFile));
    }
    if (SUCCEEDED(result))
    {
        result = persistFile->Save(shortcutPath.c_str(), TRUE);
    }

    if (persistFile != nullptr)
    {
        persistFile->Release();
    }
    shortcut->Release();
    return result;
}
}

int wmain(int argumentCount, wchar_t** arguments)
{
    if (argumentCount != 2)
    {
        return Fail(L"Usage: ShellExtensionSmoke <IconReplacer.ShellExtension.dll>");
    }

    const auto coInitializeResult = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (FAILED(coInitializeResult))
    {
        return Fail(L"COM could not be initialized", coInitializeResult);
    }

    const auto library = LoadLibraryW(arguments[1]);
    if (library == nullptr)
    {
        return Fail(L"Could not load the shell extension", HRESULT_FROM_WIN32(GetLastError()));
    }

    const auto getClassObject = reinterpret_cast<DllGetClassObjectFunction>(
        GetProcAddress(library, "DllGetClassObject"));
    if (getClassObject == nullptr)
    {
        FreeLibrary(library);
        return Fail(L"DllGetClassObject was not exported");
    }

    IExplorerCommand* direct = nullptr;
    auto result = CreateCommand(getClassObject, CLSID_ChangeIcon, &direct);
    if (FAILED(result))
    {
        FreeLibrary(library);
        return Fail(L"Direct command could not be created", result);
    }

    std::wstring directTitle;
    std::wstring directIcon;
    EXPCMDFLAGS directFlags{};
    result = ReadTitle(direct, directTitle);
    if (FAILED(result) || directTitle != L"Change icon..." ||
        FAILED(ReadIcon(direct, directIcon)) ||
        directIcon.find(L"AppIcon.ico") == std::wstring::npos ||
        GetFileAttributesW(directIcon.c_str()) == INVALID_FILE_ATTRIBUTES ||
        FAILED(direct->GetFlags(&directFlags)) || directFlags != ECF_DEFAULT)
    {
        direct->Release();
        FreeLibrary(library);
        return Fail(L"Direct command contract is invalid", result);
    }
    direct->Release();

    SmokeLibraryFixture libraryFixture;
    result = libraryFixture.Create(directIcon);
    if (FAILED(result))
    {
        FreeLibrary(library);
        return Fail(L"Isolated icon-library fixture could not be created", result);
    }

    IExplorerCommand* root = nullptr;
    result = CreateCommand(getClassObject, CLSID_Collections, &root);
    if (FAILED(result))
    {
        FreeLibrary(library);
        return Fail(L"Collections command could not be created", result);
    }

    std::wstring rootTitle;
    EXPCMDFLAGS rootFlags{};
    result = ReadTitle(root, rootTitle);
    if (FAILED(result) || rootTitle != L"Icon collections" ||
        FAILED(root->GetFlags(&rootFlags)) || rootFlags != ECF_HASSUBCOMMANDS)
    {
        root->Release();
        FreeLibrary(library);
        return Fail(L"Collections root contract is invalid", result);
    }

    IEnumExplorerCommand* collections = nullptr;
    result = root->EnumSubCommands(&collections);
    root->Release();
    if (FAILED(result) || collections == nullptr)
    {
        FreeLibrary(library);
        return Fail(L"Collections could not be enumerated", result);
    }

    IExplorerCommand* collection = nullptr;
    ULONG fetched = 0;
    result = collections->Next(1, &collection, &fetched);
    collections->Release();
    if (result != S_OK || fetched != 1 || collection == nullptr)
    {
        FreeLibrary(library);
        return Fail(L"No collection command was returned", result);
    }

    std::wstring collectionTitle;
    std::wstring collectionIcon;
    EXPCMDFLAGS collectionFlags{};
    if (FAILED(ReadTitle(collection, collectionTitle)) || collectionTitle.empty() ||
        FAILED(ReadIcon(collection, collectionIcon)) || collectionIcon.empty() ||
        FAILED(collection->GetFlags(&collectionFlags)) || collectionFlags != ECF_HASSUBCOMMANDS)
    {
        collection->Release();
        FreeLibrary(library);
        return Fail(L"Collection command is missing its title, preview, or submenu flag");
    }

    IEnumExplorerCommand* icons = nullptr;
    result = collection->EnumSubCommands(&icons);
    collection->Release();
    if (FAILED(result) || icons == nullptr)
    {
        FreeLibrary(library);
        return Fail(L"Icons could not be enumerated", result);
    }

    IExplorerCommand* iconCommand = nullptr;
    fetched = 0;
    result = icons->Next(1, &iconCommand, &fetched);
    icons->Release();
    if (result != S_OK || fetched != 1 || iconCommand == nullptr)
    {
        FreeLibrary(library);
        return Fail(L"No icon command was returned", result);
    }

    std::wstring iconTitle;
    std::wstring iconPath;
    EXPCMDFLAGS iconFlags{};
    const auto iconValid = SUCCEEDED(ReadTitle(iconCommand, iconTitle)) && !iconTitle.empty() &&
        SUCCEEDED(ReadIcon(iconCommand, iconPath)) && !iconPath.empty() &&
        SUCCEEDED(iconCommand->GetFlags(&iconFlags)) && iconFlags == ECF_DEFAULT;
    iconCommand->Release();
    if (!iconValid)
    {
        return Fail(L"Icon command is missing its title, preview, or invocation flag");
    }

    IClassFactory* classicFactory = nullptr;
    result = getClassObject(CLSID_ClassicContextMenu, IID_PPV_ARGS(&classicFactory));
    if (FAILED(result) || classicFactory == nullptr)
    {
        return Fail(L"Classic context-menu factory could not be created", result);
    }

    IShellExtInit* classicInitializer = nullptr;
    result = classicFactory->CreateInstance(nullptr, IID_PPV_ARGS(&classicInitializer));
    classicFactory->Release();
    if (FAILED(result) || classicInitializer == nullptr)
    {
        return Fail(L"Classic context-menu handler could not be created", result);
    }

    IContextMenu* classicMenu = nullptr;
    result = classicInitializer->QueryInterface(IID_PPV_ARGS(&classicMenu));
    if (FAILED(result) || classicMenu == nullptr)
    {
        classicInitializer->Release();
        return Fail(L"Classic handler does not expose IContextMenu", result);
    }

    const auto& libraryPath = libraryFixture.LibraryRoot();
    IDataObject* selection = nullptr;
    result = CreateSelectionDataObject(libraryPath, &selection);
    if (FAILED(result) || selection == nullptr)
    {
        classicMenu->Release();
        classicInitializer->Release();
        return Fail(L"Classic smoke selection could not be created", result);
    }

    IShellItemArray* selectedItems = nullptr;
    result = SHCreateShellItemArrayFromDataObject(selection, IID_PPV_ARGS(&selectedItems));
    const auto directSuppressionResult = SUCCEEDED(result) && selectedItems != nullptr
        ? VerifyClassicInvocationIsSuppressed(getClassObject, CLSID_ChangeIcon, selectedItems)
        : FAILED(result) ? result : E_UNEXPECTED;
    const auto collectionsSuppressionResult = SUCCEEDED(directSuppressionResult)
        ? VerifyClassicInvocationIsSuppressed(getClassObject, CLSID_Collections, selectedItems)
        : directSuppressionResult;
    if (FAILED(result) || selectedItems == nullptr || FAILED(collectionsSuppressionResult))
    {
        if (selectedItems != nullptr)
        {
            selectedItems->Release();
        }
        selection->Release();
        classicMenu->Release();
        classicInitializer->Release();
        return Fail(
            L"Modern commands were not suppressed for a classic-menu invocation",
            collectionsSuppressionResult);
    }
    selectedItems->Release();

    result = classicInitializer->Initialize(nullptr, selection, nullptr);
    selection->Release();
    if (FAILED(result))
    {
        classicMenu->Release();
        classicInitializer->Release();
        return Fail(L"Classic handler could not initialize the folder selection", result);
    }

    const auto menu = CreatePopupMenu();
    if (menu == nullptr)
    {
        classicMenu->Release();
        classicInitializer->Release();
        return Fail(L"Classic smoke menu could not be created", HRESULT_FROM_WIN32(GetLastError()));
    }

    const auto queryStarted = std::chrono::steady_clock::now();
    result = classicMenu->QueryContextMenu(menu, 0, 1, 0x7fff, CMF_NORMAL);
    const auto queryDuration = std::chrono::steady_clock::now() - queryStarted;
    const auto classicCommandCount = LOWORD(result);
    if (FAILED(result) ||
        GetMenuItemCount(menu) != 3 ||
        classicCommandCount < 3 ||
        classicCommandCount > ClassicMaxExpectedCommandCount ||
        queryDuration > ClassicQueryBudget)
    {
        DestroyMenu(menu);
        classicMenu->Release();
        classicInitializer->Release();
        return Fail(L"Classic handler did not create the expected three-item group", result);
    }

    MENUITEMINFOW separatorInfo{};
    MENUITEMINFOW changeInfo{};
    MENUITEMINFOW collectionsInfo{};
    std::wstring separatorTitle;
    std::wstring changeTitle;
    std::wstring collectionsTitle;
    const auto menuShapeValid =
        ReadMenuItem(menu, 0, separatorInfo, separatorTitle) &&
        (separatorInfo.fType & MFT_SEPARATOR) != 0 &&
        ReadMenuItem(menu, 1, changeInfo, changeTitle) &&
        changeTitle == L"Change icon..." &&
        IsRenderableMenuBitmap(changeInfo.hbmpItem) &&
        ReadMenuItem(menu, 2, collectionsInfo, collectionsTitle) &&
        collectionsTitle == L"Icon collections" &&
        IsRenderableMenuBitmap(collectionsInfo.hbmpItem) &&
        collectionsInfo.hSubMenu != nullptr &&
        UsesBitmapMenuLayout(collectionsInfo.hSubMenu) &&
        GetMenuItemCount(collectionsInfo.hSubMenu) > 0;
    if (!menuShapeValid)
    {
        DestroyMenu(menu);
        classicMenu->Release();
        classicInitializer->Release();
        return Fail(L"Classic menu is missing its separator, app icons, or collection submenu");
    }

    MENUITEMINFOW firstCollectionInfo{};
    std::wstring firstCollectionTitle;
    const auto collectionShapeValid =
        AllMenuItemsHavePreviews(collectionsInfo.hSubMenu, true) &&
        ReadMenuItem(collectionsInfo.hSubMenu, 0, firstCollectionInfo, firstCollectionTitle) &&
        !firstCollectionTitle.empty() &&
        firstCollectionInfo.hbmpItem == HBMMENU_CALLBACK &&
        HasRenderableMenuPreview(firstCollectionInfo) &&
        firstCollectionInfo.hSubMenu != nullptr &&
        UsesBitmapMenuLayout(firstCollectionInfo.hSubMenu) &&
        GetMenuItemCount(firstCollectionInfo.hSubMenu) > 0;

    MENUITEMINFOW firstIconInfo{};
    std::wstring firstIconTitle;
    IContextMenu3* callbackMenu = nullptr;
    const auto callbackInterfaceValid = SUCCEEDED(
        classicMenu->QueryInterface(IID_PPV_ARGS(&callbackMenu))) && callbackMenu != nullptr;
    const auto iconPreviewValid = collectionShapeValid && callbackInterfaceValid &&
        AllCollectionIconMenusHavePreviews(collectionsInfo.hSubMenu) &&
        AllMenuItemsHavePreviews(firstCollectionInfo.hSubMenu, false) &&
        ReadMenuItem(firstCollectionInfo.hSubMenu, 0, firstIconInfo, firstIconTitle) &&
        !firstIconTitle.empty() &&
        firstIconInfo.hbmpItem == HBMMENU_CALLBACK &&
        HasRenderableMenuPreview(firstIconInfo) &&
        RendersCallbackPreview(callbackMenu, firstCollectionInfo) &&
        RendersCallbackPreview(callbackMenu, firstIconInfo);
    if (callbackMenu != nullptr)
    {
        callbackMenu->Release();
    }

    constexpr UINT budgetFirstCommandId = 4000;
    constexpr UINT budgetLastCommandId = 4007;
    const auto budgetMenu = CreatePopupMenu();
    const auto budgetResult = budgetMenu == nullptr
        ? HRESULT_FROM_WIN32(GetLastError())
        : classicMenu->QueryContextMenu(
            budgetMenu,
            0,
            budgetFirstCommandId,
            budgetLastCommandId,
            CMF_NORMAL);
    const auto budgetCommandCount = LOWORD(budgetResult);
    const auto commandBudgetValid = SUCCEEDED(budgetResult) &&
        budgetCommandCount >= 2 &&
        budgetCommandCount <= (budgetLastCommandId - budgetFirstCommandId + 1) &&
        MenuCommandIdsWithinRange(budgetMenu, budgetFirstCommandId, budgetLastCommandId);
    if (budgetMenu != nullptr)
    {
        DestroyMenu(budgetMenu);
    }

    DestroyMenu(menu);
    classicMenu->Release();
    classicInitializer->Release();
    if (!iconPreviewValid)
    {
        return Fail(L"Classic collection submenu does not expose collection and icon previews");
    }
    if (!commandBudgetValid)
    {
        return Fail(L"Classic handler exceeded Explorer's command-id budget", budgetResult);
    }

    std::wstring shortcutPath;
    std::wstring shortcutDirectory;
    result = CreateSmokeShortcut(libraryPath, shortcutPath, shortcutDirectory);
    if (FAILED(result))
    {
        return Fail(L"Classic shortcut smoke target could not be created", result);
    }

    IClassFactory* shortcutFactory = nullptr;
    IShellExtInit* shortcutInitializer = nullptr;
    IContextMenu* shortcutMenu = nullptr;
    IDataObject* shortcutSelection = nullptr;
    HMENU shortcutPopup = nullptr;
    bool shortcutMenuValid = false;

    result = getClassObject(CLSID_ClassicContextMenu, IID_PPV_ARGS(&shortcutFactory));
    if (SUCCEEDED(result))
    {
        result = shortcutFactory->CreateInstance(nullptr, IID_PPV_ARGS(&shortcutInitializer));
    }
    if (SUCCEEDED(result))
    {
        result = shortcutInitializer->QueryInterface(IID_PPV_ARGS(&shortcutMenu));
    }
    if (SUCCEEDED(result))
    {
        result = CreateSelectionDataObject(shortcutPath, &shortcutSelection);
    }
    if (SUCCEEDED(result))
    {
        result = shortcutInitializer->Initialize(nullptr, shortcutSelection, nullptr);
    }
    if (SUCCEEDED(result))
    {
        shortcutPopup = CreatePopupMenu();
        result = shortcutPopup == nullptr
            ? HRESULT_FROM_WIN32(GetLastError())
            : shortcutMenu->QueryContextMenu(shortcutPopup, 0, 1, 0x7fff, CMF_NORMAL);
    }
    if (SUCCEEDED(result) && GetMenuItemCount(shortcutPopup) == 3)
    {
        MENUITEMINFOW shortcutChangeInfo{};
        MENUITEMINFOW shortcutCollectionsInfo{};
        std::wstring shortcutChangeTitle;
        std::wstring shortcutCollectionsTitle;
        const auto shortcutShapeValid =
            ReadMenuItem(shortcutPopup, 1, shortcutChangeInfo, shortcutChangeTitle) &&
            shortcutChangeTitle == L"Change icon..." &&
            IsRenderableMenuBitmap(shortcutChangeInfo.hbmpItem) &&
            ReadMenuItem(shortcutPopup, 2, shortcutCollectionsInfo, shortcutCollectionsTitle) &&
            shortcutCollectionsTitle == L"Icon collections" &&
            IsRenderableMenuBitmap(shortcutCollectionsInfo.hbmpItem) &&
            shortcutCollectionsInfo.hSubMenu != nullptr;

        MENUITEMINFOW shortcutFirstCollection{};
        std::wstring shortcutFirstCollectionTitle;
        const auto firstCollectionValid = shortcutShapeValid &&
            AllMenuItemsHavePreviews(shortcutCollectionsInfo.hSubMenu, true) &&
            ReadMenuItem(
                shortcutCollectionsInfo.hSubMenu,
                0,
                shortcutFirstCollection,
                shortcutFirstCollectionTitle) &&
            shortcutFirstCollection.hSubMenu != nullptr;

        shortcutMenuValid = firstCollectionValid &&
            AllCollectionIconMenusHavePreviews(shortcutCollectionsInfo.hSubMenu) &&
            AllMenuItemsHavePreviews(shortcutFirstCollection.hSubMenu, false);
    }

    if (shortcutPopup != nullptr)
    {
        DestroyMenu(shortcutPopup);
    }
    if (shortcutSelection != nullptr)
    {
        shortcutSelection->Release();
    }
    if (shortcutMenu != nullptr)
    {
        shortcutMenu->Release();
    }
    if (shortcutInitializer != nullptr)
    {
        shortcutInitializer->Release();
    }
    if (shortcutFactory != nullptr)
    {
        shortcutFactory->Release();
    }
    DeleteFileW(shortcutPath.c_str());
    RemoveDirectoryW(shortcutDirectory.c_str());
    if (!shortcutMenuValid)
    {
        return Fail(L"Classic handler did not expose complete previews for a .lnk selection", result);
    }

    std::wcout << L"PASS: direct='" << directTitle
        << L"', collection='" << collectionTitle
        << L"', icon='" << iconTitle
        << L"', preview='" << iconPath
        << L"', classic='folder + shortcut, bounded coexistence + collection/icon previews'"
        << L", commands='" << classicCommandCount
        << L", constrained=" << budgetCommandCount << L"'"
        << L", timing-ms='query="
        << std::chrono::duration<double, std::milli>(queryDuration).count()
        << L"'\n";
    FreeLibrary(library);
    CoUninitialize();
    return 0;
}
