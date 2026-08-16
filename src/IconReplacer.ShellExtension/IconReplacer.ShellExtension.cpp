#include <windows.h>
#include <knownfolders.h>
#include <shellapi.h>
#include <shlobj_core.h>
#include <shobjidl_core.h>
#include <wincodec.h>
#include <wrl/client.h>

#include <algorithm>
#include <cstring>
#include <cwctype>
#include <new>
#include <string>
#include <utility>
#include <vector>

#ifndef RETURN_IF_FAILED
#define RETURN_IF_FAILED(expression) \
    do \
    { \
        const HRESULT returnIfFailedResult = (expression); \
        if (FAILED(returnIfFailedResult)) \
        { \
            return returnIfFailedResult; \
        } \
    } while (false)
#endif

namespace
{
constexpr wchar_t ChangeIconTitle[] = L"Change icon...";
constexpr wchar_t CollectionsTitle[] = L"Icon collections";
constexpr wchar_t ChangeIconTooltip[] = L"Choose an .ico file and apply it immediately.";
constexpr wchar_t CollectionsTooltip[] = L"Apply an icon from your Icon Library.";
constexpr wchar_t CommandHostFileName[] = L"IconReplacer.CommandHost.exe";
constexpr wchar_t ClassicOverflowTitle[] = L"More icons are available in Icon Replacer";
constexpr size_t ClassicMaxVisibleCollections = 8;
constexpr size_t ClassicMaxIconItemsPerCollection = 30;
constexpr size_t ClassicMaxCommandCount = 2 +
    (ClassicMaxVisibleCollections * ClassicMaxIconItemsPerCollection);

// Keep in sync with ShellManifestContractService.
constexpr GUID CLSID_IconReplacerChangeIconCommand =
{
    0xb8f1a86d,
    0x4c52,
    0x4c53,
    { 0xbf, 0x72, 0x30, 0xb5, 0x9f, 0x7f, 0x0f, 0x7d }
};

constexpr GUID CLSID_IconReplacerCollectionsCommand =
{
    0xb8f1a86d,
    0x4c52,
    0x4c53,
    { 0xbf, 0x72, 0x30, 0xb5, 0x9f, 0x7f, 0x0f, 0x7e }
};

constexpr GUID CLSID_IconReplacerClassicContextMenu =
{
    0xb8f1a86d,
    0x4c52,
    0x4c53,
    { 0xbf, 0x72, 0x30, 0xb5, 0x9f, 0x7f, 0x0f, 0x7f }
};

HINSTANCE g_instance = nullptr;
long g_objectCount = 0;
long g_lockCount = 0;

enum class TargetKind
{
    Unknown,
    Folder,
    Shortcut
};

enum class CommandKind
{
    ChangeIcon,
    CollectionsRoot,
    Collection,
    ApplyIcon
};

enum class ClassKind
{
    ChangeIconCommand,
    CollectionsCommand,
    ClassicContextMenu
};

struct ShellTarget
{
    std::wstring path;
    TargetKind kind = TargetKind::Unknown;
};

void IncrementObjectCount()
{
    InterlockedIncrement(&g_objectCount);
}

void DecrementObjectCount()
{
    InterlockedDecrement(&g_objectCount);
}

HRESULT CopyCoTaskMemString(const wchar_t* value, PWSTR* output)
{
    if (output == nullptr)
    {
        return E_POINTER;
    }

    *output = nullptr;
    if (value == nullptr)
    {
        return E_INVALIDARG;
    }

    const auto characterCount = wcslen(value) + 1;
    const auto byteCount = characterCount * sizeof(wchar_t);
    auto* buffer = static_cast<PWSTR>(CoTaskMemAlloc(byteCount));
    if (buffer == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    memcpy(buffer, value, byteCount);
    *output = buffer;
    return S_OK;
}

std::wstring ToLowerInvariant(std::wstring value)
{
    std::transform(value.begin(), value.end(), value.begin(), [](wchar_t character)
    {
        return static_cast<wchar_t>(std::towlower(character));
    });
    return value;
}

bool CaseInsensitiveLess(const std::wstring& left, const std::wstring& right)
{
    return _wcsicmp(left.c_str(), right.c_str()) < 0;
}

bool IsUncPath(const std::wstring& path)
{
    return path.size() >= 2 && path[0] == L'\\' && path[1] == L'\\';
}

bool HasIcoExtension(const std::wstring& path)
{
    const auto lastSlash = path.find_last_of(L"\\/");
    const auto lastDot = path.find_last_of(L'.');
    if (lastDot == std::wstring::npos || (lastSlash != std::wstring::npos && lastDot < lastSlash))
    {
        return false;
    }

    return ToLowerInvariant(path.substr(lastDot)) == L".ico";
}

bool EndsWithShortcutExtension(const std::wstring& path)
{
    const auto lastSlash = path.find_last_of(L"\\/");
    const auto lastDot = path.find_last_of(L'.');
    if (lastDot == std::wstring::npos || (lastSlash != std::wstring::npos && lastDot < lastSlash))
    {
        return false;
    }

    return ToLowerInvariant(path.substr(lastDot)) == L".lnk";
}

std::wstring CombinePath(const std::wstring& directory, const std::wstring& name)
{
    if (directory.empty() || directory.back() == L'\\')
    {
        return directory + name;
    }

    return directory + L"\\" + name;
}

std::wstring FileName(const std::wstring& path)
{
    const auto slash = path.find_last_of(L"\\/");
    return path.substr(slash == std::wstring::npos ? 0 : slash + 1);
}

std::wstring FileNameWithoutExtension(const std::wstring& path)
{
    const auto slash = path.find_last_of(L"\\/");
    const auto start = slash == std::wstring::npos ? 0 : slash + 1;
    const auto dot = path.find_last_of(L'.');
    const auto length = dot == std::wstring::npos || dot < start
        ? std::wstring::npos
        : dot - start;
    return path.substr(start, length);
}

std::wstring HumanizeFileName(std::wstring value)
{
    bool capitalize = true;
    for (auto& character : value)
    {
        if (character == L'_' || character == L'-')
        {
            character = L' ';
            capitalize = true;
            continue;
        }

        if (capitalize && !iswspace(character))
        {
            character = static_cast<wchar_t>(towupper(character));
            capitalize = false;
        }
        else if (iswspace(character))
        {
            capitalize = true;
        }
    }

    return value;
}

TargetKind ResolveTargetKind(const std::wstring& path)
{
    if (path.empty() || IsUncPath(path))
    {
        return TargetKind::Unknown;
    }

    const auto attributes = GetFileAttributesW(path.c_str());
    if (attributes == INVALID_FILE_ATTRIBUTES)
    {
        return TargetKind::Unknown;
    }

    if ((attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY)
    {
        return TargetKind::Folder;
    }

    return EndsWithShortcutExtension(path) ? TargetKind::Shortcut : TargetKind::Unknown;
}

HRESULT TryGetSingleTarget(IShellItemArray* items, ShellTarget& target)
{
    target = {};
    if (items == nullptr)
    {
        return E_INVALIDARG;
    }

    DWORD count = 0;
    RETURN_IF_FAILED(items->GetCount(&count));
    if (count != 1)
    {
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    IShellItem* item = nullptr;
    RETURN_IF_FAILED(items->GetItemAt(0, &item));

    PWSTR fileSystemPath = nullptr;
    const auto pathResult = item->GetDisplayName(SIGDN_FILESYSPATH, &fileSystemPath);
    item->Release();
    if (FAILED(pathResult))
    {
        return pathResult;
    }

    target.path = fileSystemPath;
    CoTaskMemFree(fileSystemPath);
    target.kind = ResolveTargetKind(target.path);
    return target.kind == TargetKind::Unknown
        ? HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED)
        : S_OK;
}

std::wstring TargetKindArgument(TargetKind kind)
{
    return kind == TargetKind::Folder ? L"folder" : L"shortcut";
}

std::wstring QuoteArgument(const std::wstring& argument)
{
    return L"\"" + argument + L"\"";
}

std::wstring BuildChangeIconArguments(const ShellTarget& target)
{
    return L"change-icon --target " + QuoteArgument(target.path) +
        L" --target-kind " + TargetKindArgument(target.kind);
}

std::wstring BuildMenuApplyArguments(const ShellTarget& target, const std::wstring& iconPath)
{
    return L"menu-apply " + QuoteArgument(target.path) + L" " + QuoteArgument(iconPath);
}

HRESULT GetModuleDirectory(std::wstring& directory)
{
    std::vector<wchar_t> buffer(32768);
    const auto length = GetModuleFileNameW(g_instance, buffer.data(), static_cast<DWORD>(buffer.size()));
    if (length == 0 || length >= buffer.size())
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    directory.assign(buffer.data(), length);
    const auto slash = directory.find_last_of(L"\\/");
    if (slash == std::wstring::npos)
    {
        return HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME);
    }

    directory.resize(slash);
    return S_OK;
}

HRESULT GetIconLibraryRoot(std::wstring& libraryRoot)
{
    PWSTR profilePath = nullptr;
    RETURN_IF_FAILED(SHGetKnownFolderPath(FOLDERID_Profile, KF_FLAG_DEFAULT, nullptr, &profilePath));
    libraryRoot = CombinePath(profilePath, L".icons");
    CoTaskMemFree(profilePath);
    return S_OK;
}

std::vector<std::wstring> EnumerateIconFiles(const std::wstring& directory)
{
    std::vector<std::wstring> icons;
    WIN32_FIND_DATAW data{};
    const auto pattern = CombinePath(directory, L"*");
    const auto find = FindFirstFileW(pattern.c_str(), &data);
    if (find == INVALID_HANDLE_VALUE)
    {
        return icons;
    }

    do
    {
        if ((data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0 ||
            (data.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
        {
            continue;
        }

        const auto path = CombinePath(directory, data.cFileName);
        if (HasIcoExtension(path))
        {
            icons.push_back(path);
        }
    } while (FindNextFileW(find, &data));

    FindClose(find);
    std::sort(icons.begin(), icons.end(), CaseInsensitiveLess);
    return icons;
}

std::vector<std::wstring> EnumerateCollectionDirectories(const std::wstring& libraryRoot)
{
    std::vector<std::wstring> directories;
    WIN32_FIND_DATAW data{};
    const auto pattern = CombinePath(libraryRoot, L"*");
    const auto find = FindFirstFileW(pattern.c_str(), &data);
    if (find == INVALID_HANDLE_VALUE)
    {
        return directories;
    }

    do
    {
        if ((data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0 ||
            (data.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0 ||
            wcscmp(data.cFileName, L".") == 0 ||
            wcscmp(data.cFileName, L"..") == 0)
        {
            continue;
        }

        directories.push_back(CombinePath(libraryRoot, data.cFileName));
    } while (FindNextFileW(find, &data));

    FindClose(find);
    std::sort(directories.begin(), directories.end(), CaseInsensitiveLess);
    return directories;
}

std::wstring GetApplicationIconPath()
{
    std::wstring directory;
    if (FAILED(GetModuleDirectory(directory)))
    {
        return {};
    }

    const auto assetsDirectory = CombinePath(directory, L"Assets");
    const auto iconPath = CombinePath(assetsDirectory, L"AppIcon.ico");
    return GetFileAttributesW(iconPath.c_str()) == INVALID_FILE_ATTRIBUTES
        ? std::wstring{}
        : iconPath;
}

HBITMAP CreateMenuBitmapForIconFile(
    IWICImagingFactory* imagingFactory,
    const std::wstring& iconPath)
{
    if (imagingFactory == nullptr || iconPath.empty())
    {
        return nullptr;
    }

    using Microsoft::WRL::ComPtr;

    ComPtr<IWICBitmapDecoder> decoder;
    if (FAILED(imagingFactory->CreateDecoderFromFilename(
        iconPath.c_str(),
        nullptr,
        GENERIC_READ,
        WICDecodeMetadataCacheOnLoad,
        &decoder)))
    {
        return nullptr;
    }

    const auto targetWidth = static_cast<UINT>(GetSystemMetrics(SM_CXSMICON));
    const auto targetHeight = static_cast<UINT>(GetSystemMetrics(SM_CYSMICON));
    UINT frameCount = 0;
    if (targetWidth == 0 || targetHeight == 0 ||
        FAILED(decoder->GetFrameCount(&frameCount)) || frameCount == 0)
    {
        return nullptr;
    }

    ComPtr<IWICBitmapFrameDecode> selectedFrame;
    bool selectedFrameMeetsTarget = false;
    ULONGLONG selectedArea = 0;
    for (UINT index = 0; index < frameCount; ++index)
    {
        ComPtr<IWICBitmapFrameDecode> frame;
        UINT width = 0;
        UINT height = 0;
        if (FAILED(decoder->GetFrame(index, &frame)) ||
            FAILED(frame->GetSize(&width, &height)) || width == 0 || height == 0)
        {
            continue;
        }

        const auto meetsTarget = width >= targetWidth && height >= targetHeight;
        const auto area = static_cast<ULONGLONG>(width) * height;
        const auto isBetter = selectedFrame == nullptr ||
            (meetsTarget && !selectedFrameMeetsTarget) ||
            (meetsTarget == selectedFrameMeetsTarget &&
                ((meetsTarget && area < selectedArea) || (!meetsTarget && area > selectedArea)));
        if (isBetter)
        {
            selectedFrame = frame;
            selectedFrameMeetsTarget = meetsTarget;
            selectedArea = area;
        }
    }

    if (selectedFrame == nullptr)
    {
        return nullptr;
    }

    ComPtr<IWICBitmapScaler> scaler;
    if (FAILED(imagingFactory->CreateBitmapScaler(&scaler)) ||
        FAILED(scaler->Initialize(
            selectedFrame.Get(),
            targetWidth,
            targetHeight,
            WICBitmapInterpolationModeFant)))
    {
        return nullptr;
    }

    ComPtr<IWICFormatConverter> converter;
    if (FAILED(imagingFactory->CreateFormatConverter(&converter)) ||
        FAILED(converter->Initialize(
            scaler.Get(),
            GUID_WICPixelFormat32bppPBGRA,
            WICBitmapDitherTypeNone,
            nullptr,
            0,
            WICBitmapPaletteTypeCustom)))
    {
        return nullptr;
    }

    BITMAPINFO bitmapInfo{};
    bitmapInfo.bmiHeader.biSize = sizeof(bitmapInfo.bmiHeader);
    bitmapInfo.bmiHeader.biWidth = static_cast<LONG>(targetWidth);
    bitmapInfo.bmiHeader.biHeight = -static_cast<LONG>(targetHeight);
    bitmapInfo.bmiHeader.biPlanes = 1;
    bitmapInfo.bmiHeader.biBitCount = 32;
    bitmapInfo.bmiHeader.biCompression = BI_RGB;

    void* pixels = nullptr;
    const auto screen = GetDC(nullptr);
    const auto bitmap = CreateDIBSection(
        screen,
        &bitmapInfo,
        DIB_RGB_COLORS,
        &pixels,
        nullptr,
        0);
    if (screen != nullptr)
    {
        ReleaseDC(nullptr, screen);
    }

    if (bitmap == nullptr || pixels == nullptr)
    {
        if (bitmap != nullptr)
        {
            DeleteObject(bitmap);
        }
        return nullptr;
    }

    const auto stride = targetWidth * 4;
    const auto bufferSize = stride * targetHeight;
    if (FAILED(converter->CopyPixels(nullptr, stride, bufferSize, static_cast<BYTE*>(pixels))))
    {
        DeleteObject(bitmap);
        return nullptr;
    }

    return bitmap;
}

HBITMAP CreateApplicationMenuBitmap(IWICImagingFactory* imagingFactory)
{
    return CreateMenuBitmapForIconFile(imagingFactory, GetApplicationIconPath());
}

HRESULT LaunchCommandHost(const std::wstring& arguments)
{
    std::wstring directory;
    RETURN_IF_FAILED(GetModuleDirectory(directory));
    const auto hostPath = CombinePath(directory, CommandHostFileName);
    if (GetFileAttributesW(hostPath.c_str()) == INVALID_FILE_ATTRIBUTES)
    {
        return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    }

    const auto commandLineText = QuoteArgument(hostPath) + L" " + arguments;
    std::vector<wchar_t> commandLine(commandLineText.begin(), commandLineText.end());
    commandLine.push_back(L'\0');

    STARTUPINFOW startupInfo{};
    startupInfo.cb = sizeof(startupInfo);
    PROCESS_INFORMATION processInfo{};
    const auto created = CreateProcessW(
        hostPath.c_str(),
        commandLine.data(),
        nullptr,
        nullptr,
        FALSE,
        CREATE_NO_WINDOW | CREATE_UNICODE_ENVIRONMENT,
        nullptr,
        directory.c_str(),
        &startupInfo,
        &processInfo);
    if (!created)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    CloseHandle(processInfo.hThread);
    CloseHandle(processInfo.hProcess);
    return S_OK;
}

class EnumExplorerCommand final : public IEnumExplorerCommand
{
public:
    explicit EnumExplorerCommand(std::vector<IExplorerCommand*> commands, ULONG index = 0)
        : commands_(std::move(commands)), index_(index)
    {
        IncrementObjectCount();
    }

    ~EnumExplorerCommand()
    {
        for (auto* command : commands_)
        {
            command->Release();
        }

        DecrementObjectCount();
    }

    IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override
    {
        if (object == nullptr)
        {
            return E_POINTER;
        }

        *object = nullptr;
        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IEnumExplorerCommand))
        {
            *object = static_cast<IEnumExplorerCommand*>(this);
            AddRef();
            return S_OK;
        }

        return E_NOINTERFACE;
    }

    IFACEMETHODIMP_(ULONG) AddRef() override
    {
        return static_cast<ULONG>(InterlockedIncrement(&referenceCount_));
    }

    IFACEMETHODIMP_(ULONG) Release() override
    {
        const auto count = InterlockedDecrement(&referenceCount_);
        if (count == 0)
        {
            delete this;
        }

        return static_cast<ULONG>(count);
    }

    IFACEMETHODIMP Next(ULONG count, IExplorerCommand** commands, ULONG* fetched) override
    {
        if (commands == nullptr || (count > 1 && fetched == nullptr))
        {
            return E_POINTER;
        }

        ULONG copied = 0;
        while (copied < count && index_ < commands_.size())
        {
            commands[copied] = commands_[index_];
            commands[copied]->AddRef();
            copied++;
            index_++;
        }

        if (fetched != nullptr)
        {
            *fetched = copied;
        }

        return copied == count ? S_OK : S_FALSE;
    }

    IFACEMETHODIMP Skip(ULONG count) override
    {
        const auto remaining = static_cast<ULONG>(commands_.size() - index_);
        index_ += (std::min)(count, remaining);
        return count <= remaining ? S_OK : S_FALSE;
    }

    IFACEMETHODIMP Reset() override
    {
        index_ = 0;
        return S_OK;
    }

    IFACEMETHODIMP Clone(IEnumExplorerCommand** commands) override
    {
        if (commands == nullptr)
        {
            return E_POINTER;
        }

        *commands = nullptr;
        std::vector<IExplorerCommand*> cloneCommands = commands_;
        for (auto* command : cloneCommands)
        {
            command->AddRef();
        }

        auto* clone = new (std::nothrow) EnumExplorerCommand(std::move(cloneCommands), index_);
        if (clone == nullptr)
        {
            for (auto* command : cloneCommands)
            {
                command->Release();
            }

            return E_OUTOFMEMORY;
        }

        *commands = clone;
        return S_OK;
    }

private:
    long referenceCount_ = 1;
    std::vector<IExplorerCommand*> commands_;
    ULONG index_ = 0;
};

class ExplorerCommand final : public IExplorerCommand, public IObjectWithSelection
{
public:
    explicit ExplorerCommand(
        CommandKind kind,
        std::wstring title = {},
        std::wstring collectionPath = {},
        std::wstring iconPath = {})
        : kind_(kind),
          title_(std::move(title)),
          collectionPath_(std::move(collectionPath)),
          iconPath_(std::move(iconPath))
    {
        IncrementObjectCount();
    }

    ~ExplorerCommand()
    {
        if (selection_ != nullptr)
        {
            selection_->Release();
        }

        DecrementObjectCount();
    }

    IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override
    {
        if (object == nullptr)
        {
            return E_POINTER;
        }

        *object = nullptr;
        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IExplorerCommand))
        {
            *object = static_cast<IExplorerCommand*>(this);
        }
        else if (IsEqualIID(riid, IID_IObjectWithSelection))
        {
            *object = static_cast<IObjectWithSelection*>(this);
        }
        else
        {
            return E_NOINTERFACE;
        }

        AddRef();
        return S_OK;
    }

    IFACEMETHODIMP_(ULONG) AddRef() override
    {
        return static_cast<ULONG>(InterlockedIncrement(&referenceCount_));
    }

    IFACEMETHODIMP_(ULONG) Release() override
    {
        const auto count = InterlockedDecrement(&referenceCount_);
        if (count == 0)
        {
            delete this;
        }

        return static_cast<ULONG>(count);
    }

    IFACEMETHODIMP GetTitle(IShellItemArray*, PWSTR* name) override
    {
        const wchar_t* title = nullptr;
        switch (kind_)
        {
            case CommandKind::ChangeIcon:
                title = ChangeIconTitle;
                break;
            case CommandKind::CollectionsRoot:
                title = CollectionsTitle;
                break;
            default:
                title = title_.c_str();
                break;
        }

        return CopyCoTaskMemString(title, name);
    }

    IFACEMETHODIMP GetIcon(IShellItemArray*, PWSTR* icon) override
    {
        if (icon == nullptr)
        {
            return E_POINTER;
        }

        *icon = nullptr;
        const auto resolvedIcon = iconPath_.empty() ? GetApplicationIconPath() : iconPath_;
        return resolvedIcon.empty()
            ? E_NOTIMPL
            : CopyCoTaskMemString(resolvedIcon.c_str(), icon);
    }

    IFACEMETHODIMP GetToolTip(IShellItemArray*, PWSTR* tooltip) override
    {
        if (kind_ == CommandKind::ChangeIcon)
        {
            return CopyCoTaskMemString(ChangeIconTooltip, tooltip);
        }

        if (kind_ == CommandKind::CollectionsRoot)
        {
            return CopyCoTaskMemString(CollectionsTooltip, tooltip);
        }

        return E_NOTIMPL;
    }

    IFACEMETHODIMP GetCanonicalName(GUID* commandName) override
    {
        if (commandName == nullptr)
        {
            return E_POINTER;
        }

        *commandName = kind_ == CommandKind::ChangeIcon
            ? CLSID_IconReplacerChangeIconCommand
            : CLSID_IconReplacerCollectionsCommand;
        return S_OK;
    }

    IFACEMETHODIMP GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) override
    {
        if (state == nullptr)
        {
            return E_POINTER;
        }

        if (classicInvocation_)
        {
            *state = ECS_HIDDEN;
            return S_OK;
        }

        ShellTarget target;
        *state = SUCCEEDED(TryGetSingleTarget(items, target)) ? ECS_ENABLED : ECS_DISABLED;
        return S_OK;
    }

    IFACEMETHODIMP SetSelection(IShellItemArray* items) override
    {
        if (selection_ != nullptr)
        {
            selection_->Release();
        }

        selection_ = items;
        classicInvocation_ = selection_ != nullptr;
        if (selection_ != nullptr)
        {
            selection_->AddRef();
        }

        return S_OK;
    }

    IFACEMETHODIMP GetSelection(REFIID riid, void** object) override
    {
        if (object == nullptr)
        {
            return E_POINTER;
        }

        *object = nullptr;
        return selection_ == nullptr ? E_FAIL : selection_->QueryInterface(riid, object);
    }

    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) override
    {
        ShellTarget target;
        RETURN_IF_FAILED(TryGetSingleTarget(items, target));

        if (kind_ == CommandKind::ChangeIcon)
        {
            return LaunchCommandHost(BuildChangeIconArguments(target));
        }

        if (kind_ == CommandKind::ApplyIcon)
        {
            return LaunchCommandHost(BuildMenuApplyArguments(target, iconPath_));
        }

        return E_NOTIMPL;
    }

    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* flags) override
    {
        if (flags == nullptr)
        {
            return E_POINTER;
        }

        *flags = kind_ == CommandKind::CollectionsRoot || kind_ == CommandKind::Collection
            ? ECF_HASSUBCOMMANDS
            : ECF_DEFAULT;
        return S_OK;
    }

    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** commands) override
    {
        if (commands == nullptr)
        {
            return E_POINTER;
        }

        *commands = nullptr;
        std::vector<IExplorerCommand*> items;
        if (kind_ == CommandKind::CollectionsRoot)
        {
            BuildCollectionCommands(items);
        }
        else if (kind_ == CommandKind::Collection)
        {
            BuildIconCommands(items);
        }
        else
        {
            return E_NOTIMPL;
        }

        auto* enumerator = new (std::nothrow) EnumExplorerCommand(std::move(items));
        if (enumerator == nullptr)
        {
            for (auto* item : items)
            {
                item->Release();
            }

            return E_OUTOFMEMORY;
        }

        *commands = enumerator;
        return S_OK;
    }

private:
    void BuildCollectionCommands(std::vector<IExplorerCommand*>& commands)
    {
        std::wstring libraryRoot;
        if (FAILED(GetIconLibraryRoot(libraryRoot)))
        {
            return;
        }

        struct CollectionDescriptor
        {
            std::wstring title;
            std::wstring path;
            std::wstring icon;
        };

        std::vector<CollectionDescriptor> collections;
        const auto rootIcons = EnumerateIconFiles(libraryRoot);
        if (!rootIcons.empty())
        {
            collections.push_back({ L"Imported", libraryRoot, rootIcons.front() });
        }

        for (const auto& directory : EnumerateCollectionDirectories(libraryRoot))
        {
            const auto icons = EnumerateIconFiles(directory);
            if (!icons.empty())
            {
                collections.push_back({
                    HumanizeFileName(FileName(directory)),
                    directory,
                    icons.front()
                });
            }
        }

        std::sort(collections.begin(), collections.end(), [](const auto& left, const auto& right)
        {
            return CaseInsensitiveLess(left.title, right.title);
        });

        for (auto& collection : collections)
        {
            auto* command = new (std::nothrow) ExplorerCommand(
                CommandKind::Collection,
                std::move(collection.title),
                std::move(collection.path),
                std::move(collection.icon));
            if (command != nullptr)
            {
                commands.push_back(command);
            }
        }
    }

    void BuildIconCommands(std::vector<IExplorerCommand*>& commands)
    {
        for (const auto& iconPath : EnumerateIconFiles(collectionPath_))
        {
            auto* command = new (std::nothrow) ExplorerCommand(
                CommandKind::ApplyIcon,
                HumanizeFileName(FileNameWithoutExtension(iconPath)),
                std::wstring{},
                iconPath);
            if (command != nullptr)
            {
                commands.push_back(command);
            }
        }
    }

    long referenceCount_ = 1;
    IShellItemArray* selection_ = nullptr;
    bool classicInvocation_ = false;
    CommandKind kind_;
    std::wstring title_;
    std::wstring collectionPath_;
    std::wstring iconPath_;
};

enum class ClassicActionKind
{
    ChangeIcon,
    CollectionsRoot,
    ApplyIcon
};

struct ClassicMenuAction
{
    ClassicActionKind kind;
    std::wstring iconPath;
};

bool InsertSeparator(HMENU menu, UINT position)
{
    MENUITEMINFOW item{};
    item.cbSize = sizeof(item);
    item.fMask = MIIM_FTYPE;
    item.fType = MFT_SEPARATOR;
    return InsertMenuItemW(menu, position, TRUE, &item) != FALSE;
}

bool ConfigurePreviewMenu(HMENU menu)
{
    if (menu == nullptr)
    {
        return false;
    }

    MENUINFO info{};
    info.cbSize = sizeof(info);
    info.fMask = MIM_STYLE;
    if (!GetMenuInfo(menu, &info))
    {
        return false;
    }

    info.dwStyle |= MNS_CHECKORBMP;
    return SetMenuInfo(menu, &info) != FALSE;
}

bool MeasureMenuBitmap(HBITMAP bitmap, MEASUREITEMSTRUCT* measureItem)
{
    BITMAP details{};
    if (bitmap == nullptr || measureItem == nullptr ||
        GetObjectW(bitmap, sizeof(details), &details) != sizeof(details))
    {
        return false;
    }

    measureItem->itemWidth = static_cast<UINT>(details.bmWidth);
    measureItem->itemHeight = static_cast<UINT>(details.bmHeight);
    return true;
}

bool DrawMenuBitmap(HBITMAP bitmap, const DRAWITEMSTRUCT* drawItem)
{
    BITMAP details{};
    if (bitmap == nullptr || drawItem == nullptr || drawItem->hDC == nullptr ||
        GetObjectW(bitmap, sizeof(details), &details) != sizeof(details))
    {
        return false;
    }

    const auto availableWidth = drawItem->rcItem.right - drawItem->rcItem.left;
    const auto availableHeight = drawItem->rcItem.bottom - drawItem->rcItem.top;
    const auto width = (std::min)(details.bmWidth, availableWidth);
    const auto height = (std::min)(details.bmHeight, availableHeight);
    if (width <= 0 || height <= 0)
    {
        return false;
    }

    const auto source = CreateCompatibleDC(drawItem->hDC);
    if (source == nullptr)
    {
        return false;
    }

    const auto previous = SelectObject(source, bitmap);
    if (previous == nullptr || previous == HGDI_ERROR)
    {
        DeleteDC(source);
        return false;
    }

    const auto x = drawItem->rcItem.left + ((availableWidth - width) / 2);
    const auto y = drawItem->rcItem.top + ((availableHeight - height) / 2);
    BLENDFUNCTION blend{};
    blend.BlendOp = AC_SRC_OVER;
    blend.SourceConstantAlpha = 255;
    blend.AlphaFormat = AC_SRC_ALPHA;
    const auto drawn = AlphaBlend(
        drawItem->hDC,
        x,
        y,
        width,
        height,
        source,
        0,
        0,
        details.bmWidth,
        details.bmHeight,
        blend) != FALSE;

    SelectObject(source, previous);
    DeleteDC(source);
    return drawn;
}

bool InsertTextMenuItem(
    HMENU menu,
    UINT position,
    const std::wstring& title,
    UINT commandId,
    HMENU submenu = nullptr,
    HBITMAP bitmap = nullptr,
    bool enabled = true,
    bool hasCommandId = true,
    bool useBitmapCallback = false)
{
    MENUITEMINFOW item{};
    item.cbSize = sizeof(item);
    item.fMask = MIIM_FTYPE | MIIM_STRING | MIIM_STATE;
    item.fType = MFT_STRING;
    item.fState = enabled ? MFS_ENABLED : MFS_DISABLED;
    item.dwTypeData = const_cast<PWSTR>(title.c_str());
    if (hasCommandId)
    {
        item.fMask |= MIIM_ID;
        item.wID = commandId;
    }

    if (submenu != nullptr)
    {
        item.fMask |= MIIM_SUBMENU;
        item.hSubMenu = submenu;
    }

    if (bitmap != nullptr)
    {
        item.fMask |= MIIM_BITMAP;
        item.hbmpItem = useBitmapCallback ? HBMMENU_CALLBACK : bitmap;
        if (useBitmapCallback)
        {
            item.fMask |= MIIM_DATA;
            item.dwItemData = reinterpret_cast<ULONG_PTR>(bitmap);
        }
    }

    return InsertMenuItemW(menu, position, TRUE, &item) != FALSE;
}

class ClassicContextMenuHandler final : public IShellExtInit, public IContextMenu3
{
public:
    ClassicContextMenuHandler()
    {
        IncrementObjectCount();
    }

    ~ClassicContextMenuHandler()
    {
        for (const auto& preview : previewBitmaps_)
        {
            DeleteObject(preview.bitmap);
        }

        if (applicationBitmap_ != nullptr)
        {
            DeleteObject(applicationBitmap_);
        }

        if (imagingFactory_ != nullptr)
        {
            imagingFactory_->Release();
        }

        DecrementObjectCount();
    }

    IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override
    {
        if (object == nullptr)
        {
            return E_POINTER;
        }

        *object = nullptr;
        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IShellExtInit))
        {
            *object = static_cast<IShellExtInit*>(this);
        }
        else if (IsEqualIID(riid, IID_IContextMenu) ||
            IsEqualIID(riid, IID_IContextMenu2) ||
            IsEqualIID(riid, IID_IContextMenu3))
        {
            *object = static_cast<IContextMenu3*>(this);
        }
        else
        {
            return E_NOINTERFACE;
        }

        AddRef();
        return S_OK;
    }

    IFACEMETHODIMP_(ULONG) AddRef() override
    {
        return static_cast<ULONG>(InterlockedIncrement(&referenceCount_));
    }

    IFACEMETHODIMP_(ULONG) Release() override
    {
        const auto count = InterlockedDecrement(&referenceCount_);
        if (count == 0)
        {
            delete this;
        }

        return static_cast<ULONG>(count);
    }

    IFACEMETHODIMP Initialize(PCIDLIST_ABSOLUTE, IDataObject* dataObject, HKEY) override
    {
        target_ = {};
        if (dataObject == nullptr)
        {
            return E_INVALIDARG;
        }

        IShellItemArray* items = nullptr;
        RETURN_IF_FAILED(SHCreateShellItemArrayFromDataObject(dataObject, IID_PPV_ARGS(&items)));
        const auto result = TryGetSingleTarget(items, target_);
        items->Release();
        return result;
    }

    IFACEMETHODIMP QueryContextMenu(
        HMENU menu,
        UINT indexMenu,
        UINT commandIdFirst,
        UINT commandIdLast,
        UINT flags) override
    {
        actions_.clear();
        menuTruncated_ = false;
        if ((flags & CMF_DEFAULTONLY) != 0 || target_.kind == TargetKind::Unknown)
        {
            return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
        }

        if (menu == nullptr || commandIdFirst > commandIdLast)
        {
            return E_INVALIDARG;
        }

        RETURN_IF_FAILED(EnsureImagingFactory());
        if (applicationBitmap_ == nullptr)
        {
            applicationBitmap_ = CreateApplicationMenuBitmap(imagingFactory_);
        }
        if (applicationBitmap_ == nullptr)
        {
            return HRESULT_FROM_WIN32(ERROR_RESOURCE_DATA_NOT_FOUND);
        }

        UINT position = indexMenu;
        if (!InsertSeparator(menu, position++))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }

        actions_.push_back({ ClassicActionKind::ChangeIcon, {} });
        if (!InsertTextMenuItem(
                menu,
                position++,
                ChangeIconTitle,
                commandIdFirst,
                nullptr,
                applicationBitmap_))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }

        if (commandIdFirst == commandIdLast)
        {
            return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 1);
        }

        const auto collectionsMenu = CreatePopupMenu();
        if (collectionsMenu == nullptr || !ConfigurePreviewMenu(collectionsMenu))
        {
            const auto error = GetLastError();
            if (collectionsMenu != nullptr)
            {
                DestroyMenu(collectionsMenu);
            }
            return HRESULT_FROM_WIN32(
                error == ERROR_SUCCESS ? ERROR_INVALID_FUNCTION : error);
        }

        actions_.push_back({ ClassicActionKind::CollectionsRoot, {} });
        BuildCollectionsMenu(collectionsMenu, commandIdFirst, commandIdLast);
        if (!InsertTextMenuItem(
                menu,
                position,
                CollectionsTitle,
                commandIdFirst + 1,
                collectionsMenu,
                applicationBitmap_))
        {
            DestroyMenu(collectionsMenu);
            return HRESULT_FROM_WIN32(GetLastError());
        }

        return MAKE_HRESULT(
            SEVERITY_SUCCESS,
            FACILITY_NULL,
            static_cast<USHORT>(actions_.size()));
    }

    IFACEMETHODIMP InvokeCommand(CMINVOKECOMMANDINFO* commandInfo) override
    {
        if (commandInfo == nullptr)
        {
            return E_POINTER;
        }

        if (HIWORD(commandInfo->lpVerb) != 0)
        {
            return E_INVALIDARG;
        }

        const auto offset = LOWORD(commandInfo->lpVerb);
        if (offset >= actions_.size())
        {
            return E_INVALIDARG;
        }

        const auto& action = actions_[offset];
        if (action.kind == ClassicActionKind::ChangeIcon)
        {
            return LaunchCommandHost(BuildChangeIconArguments(target_));
        }

        if (action.kind == ClassicActionKind::ApplyIcon)
        {
            return LaunchCommandHost(BuildMenuApplyArguments(target_, action.iconPath));
        }

        return S_OK;
    }

    IFACEMETHODIMP GetCommandString(
        UINT_PTR commandOffset,
        UINT flags,
        UINT*,
        LPSTR name,
        UINT maximumCharacters) override
    {
        if (name == nullptr || maximumCharacters == 0 || commandOffset >= actions_.size())
        {
            return E_INVALIDARG;
        }

        const auto& action = actions_[commandOffset];
        const wchar_t* value = nullptr;
        switch (flags)
        {
            case GCS_HELPTEXTW:
            case GCS_HELPTEXTA:
                value = action.kind == ClassicActionKind::ChangeIcon
                    ? ChangeIconTooltip
                    : CollectionsTooltip;
                break;
            case GCS_VERBW:
            case GCS_VERBA:
                value = action.kind == ClassicActionKind::ChangeIcon
                    ? L"IconReplacer.ChangeIcon"
                    : L"IconReplacer.ApplyIcon";
                break;
            default:
                return E_NOTIMPL;
        }

        if (flags == GCS_HELPTEXTW || flags == GCS_VERBW)
        {
            return wcsncpy_s(
                reinterpret_cast<PWSTR>(name),
                maximumCharacters,
                value,
                _TRUNCATE) == 0
                ? S_OK
                : E_FAIL;
        }

        return WideCharToMultiByte(
            CP_ACP,
            0,
            value,
            -1,
            name,
            static_cast<int>(maximumCharacters),
            nullptr,
            nullptr) > 0
            ? S_OK
            : HRESULT_FROM_WIN32(GetLastError());
    }

    IFACEMETHODIMP HandleMenuMsg(UINT message, WPARAM wParam, LPARAM lParam) override
    {
        UNREFERENCED_PARAMETER(message);
        UNREFERENCED_PARAMETER(wParam);
        UNREFERENCED_PARAMETER(lParam);
        return S_FALSE;
    }

    IFACEMETHODIMP HandleMenuMsg2(
        UINT message,
        WPARAM wParam,
        LPARAM lParam,
        LRESULT* result) override
    {
        UNREFERENCED_PARAMETER(wParam);
        if (result != nullptr)
        {
            *result = 0;
        }

        if (message == WM_MEASUREITEM)
        {
            const auto measureItem = reinterpret_cast<MEASUREITEMSTRUCT*>(lParam);
            if (measureItem != nullptr && measureItem->CtlType == ODT_MENU &&
                MeasureMenuBitmap(
                    reinterpret_cast<HBITMAP>(measureItem->itemData),
                    measureItem))
            {
                if (result != nullptr)
                {
                    *result = TRUE;
                }
                return S_OK;
            }
        }
        else if (message == WM_DRAWITEM)
        {
            const auto drawItem = reinterpret_cast<DRAWITEMSTRUCT*>(lParam);
            if (drawItem != nullptr && drawItem->CtlType == ODT_MENU &&
                DrawMenuBitmap(
                    reinterpret_cast<HBITMAP>(drawItem->itemData),
                    drawItem))
            {
                if (result != nullptr)
                {
                    *result = TRUE;
                }
                return S_OK;
            }
        }

        return S_FALSE;
    }

private:
    HRESULT EnsureImagingFactory()
    {
        if (imagingFactory_ != nullptr)
        {
            return S_OK;
        }

        return CoCreateInstance(
            CLSID_WICImagingFactory,
            nullptr,
            CLSCTX_INPROC_SERVER,
            IID_PPV_ARGS(&imagingFactory_));
    }

    void BuildCollectionsMenu(HMENU collectionsMenu, UINT commandIdFirst, UINT commandIdLast)
    {
        std::wstring libraryRoot;
        if (FAILED(GetIconLibraryRoot(libraryRoot)))
        {
            InsertEmptyCollectionsItem(collectionsMenu);
            return;
        }

        size_t visibleCollectionCount = 0;
        if (AddCollectionMenu(collectionsMenu, L"Imported", libraryRoot, commandIdFirst, commandIdLast))
        {
            ++visibleCollectionCount;
        }

        const auto directories = EnumerateCollectionDirectories(libraryRoot);
        for (const auto& directory : directories)
        {
            if (visibleCollectionCount >= ClassicMaxVisibleCollections ||
                actions_.size() >= ClassicMaxCommandCount)
            {
                menuTruncated_ = true;
                break;
            }

            if (AddCollectionMenu(
                collectionsMenu,
                HumanizeFileName(FileName(directory)),
                directory,
                commandIdFirst,
                commandIdLast))
            {
                ++visibleCollectionCount;
            }
        }

        if (menuTruncated_)
        {
            InsertTextMenuItem(
                collectionsMenu,
                static_cast<UINT>(GetMenuItemCount(collectionsMenu)),
                ClassicOverflowTitle,
                0,
                nullptr,
                nullptr,
                false,
                false);
        }

        if (GetMenuItemCount(collectionsMenu) == 0)
        {
            InsertEmptyCollectionsItem(collectionsMenu);
        }
    }

    bool AddCollectionMenu(
        HMENU collectionsMenu,
        const std::wstring& title,
        const std::wstring& directory,
        UINT commandIdFirst,
        UINT commandIdLast)
    {
        const auto icons = EnumerateIconFiles(directory);
        if (icons.empty())
        {
            return false;
        }

        const auto iconsMenu = CreatePopupMenu();
        if (iconsMenu == nullptr || !ConfigurePreviewMenu(iconsMenu))
        {
            if (iconsMenu != nullptr)
            {
                DestroyMenu(iconsMenu);
            }
            return false;
        }

        size_t visibleIconCount = 0;
        HBITMAP collectionBitmap = nullptr;
        for (const auto& iconPath : icons)
        {
            const auto offset = static_cast<UINT>(actions_.size());
            if (visibleIconCount >= ClassicMaxIconItemsPerCollection ||
                actions_.size() >= ClassicMaxCommandCount ||
                commandIdFirst + offset > commandIdLast)
            {
                menuTruncated_ = true;
                break;
            }

            const auto decodedBitmap = GetOrCreatePreviewBitmap(iconPath);
            const auto iconBitmap = decodedBitmap != nullptr
                ? decodedBitmap
                : applicationBitmap_;

            actions_.push_back({ ClassicActionKind::ApplyIcon, iconPath });
            const auto iconPosition = static_cast<UINT>(GetMenuItemCount(iconsMenu));
            if (!InsertTextMenuItem(
                    iconsMenu,
                    iconPosition,
                    HumanizeFileName(FileNameWithoutExtension(iconPath)),
                    commandIdFirst + offset,
                    nullptr,
                    iconBitmap,
                    true,
                    true,
                    true))
            {
                actions_.pop_back();
                continue;
            }

            if (collectionBitmap == nullptr)
            {
                collectionBitmap = iconBitmap;
            }
            ++visibleIconCount;
        }

        if (visibleIconCount < icons.size())
        {
            menuTruncated_ = true;
        }

        const auto collectionPosition = static_cast<UINT>(GetMenuItemCount(collectionsMenu));
        if (GetMenuItemCount(iconsMenu) == 0 || !InsertTextMenuItem(
                collectionsMenu,
                collectionPosition,
                title,
                0,
                iconsMenu,
                collectionBitmap,
                true,
                false,
                true))
        {
            DestroyMenu(iconsMenu);
            return false;
        }
        return true;
    }

    HBITMAP GetOrCreatePreviewBitmap(const std::wstring& iconPath)
    {
        for (const auto& preview : previewBitmaps_)
        {
            if (CompareStringOrdinal(
                    preview.iconPath.c_str(),
                    -1,
                    iconPath.c_str(),
                    -1,
                    TRUE) == CSTR_EQUAL)
            {
                return preview.bitmap;
            }
        }

        const auto bitmap = CreateMenuBitmapForIconFile(imagingFactory_, iconPath);
        if (bitmap != nullptr)
        {
            previewBitmaps_.push_back({ iconPath, bitmap });
        }

        return bitmap;
    }

    static void InsertEmptyCollectionsItem(HMENU collectionsMenu)
    {
        InsertTextMenuItem(
            collectionsMenu,
            0,
            L"No icon collections",
            0,
            nullptr,
            nullptr,
            false,
            false);
    }

    long referenceCount_ = 1;
    ShellTarget target_;
    IWICImagingFactory* imagingFactory_ = nullptr;
    HBITMAP applicationBitmap_ = nullptr;
    struct PreviewBitmap
    {
        std::wstring iconPath;
        HBITMAP bitmap;
    };
    std::vector<PreviewBitmap> previewBitmaps_;
    std::vector<ClassicMenuAction> actions_;
    bool menuTruncated_ = false;
};

class ClassFactory final : public IClassFactory
{
public:
    explicit ClassFactory(ClassKind classKind)
        : classKind_(classKind)
    {
        IncrementObjectCount();
    }

    ~ClassFactory()
    {
        DecrementObjectCount();
    }

    IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override
    {
        if (object == nullptr)
        {
            return E_POINTER;
        }

        *object = nullptr;
        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IClassFactory))
        {
            *object = static_cast<IClassFactory*>(this);
            AddRef();
            return S_OK;
        }

        return E_NOINTERFACE;
    }

    IFACEMETHODIMP_(ULONG) AddRef() override
    {
        return static_cast<ULONG>(InterlockedIncrement(&referenceCount_));
    }

    IFACEMETHODIMP_(ULONG) Release() override
    {
        const auto count = InterlockedDecrement(&referenceCount_);
        if (count == 0)
        {
            delete this;
        }

        return static_cast<ULONG>(count);
    }

    IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID riid, void** object) override
    {
        if (object == nullptr)
        {
            return E_POINTER;
        }

        *object = nullptr;
        if (outer != nullptr)
        {
            return CLASS_E_NOAGGREGATION;
        }

        IUnknown* instance = nullptr;
        if (classKind_ == ClassKind::ClassicContextMenu)
        {
            instance = static_cast<IShellExtInit*>(new (std::nothrow) ClassicContextMenuHandler());
        }
        else
        {
            const auto rootKind = classKind_ == ClassKind::ChangeIconCommand
                ? CommandKind::ChangeIcon
                : CommandKind::CollectionsRoot;
            instance = static_cast<IExplorerCommand*>(new (std::nothrow) ExplorerCommand(rootKind));
        }

        if (instance == nullptr)
        {
            return E_OUTOFMEMORY;
        }

        const auto result = instance->QueryInterface(riid, object);
        instance->Release();
        return result;
    }

    IFACEMETHODIMP LockServer(BOOL lock) override
    {
        if (lock)
        {
            InterlockedIncrement(&g_lockCount);
        }
        else
        {
            InterlockedDecrement(&g_lockCount);
        }

        return S_OK;
    }

private:
    long referenceCount_ = 1;
    ClassKind classKind_;
};
}

extern "C" BOOL WINAPI DllMain(HINSTANCE instance, DWORD reason, void*)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        g_instance = instance;
        DisableThreadLibraryCalls(instance);
    }

    return TRUE;
}

extern "C" HRESULT __stdcall DllCanUnloadNow()
{
    return g_objectCount == 0 && g_lockCount == 0 ? S_OK : S_FALSE;
}

extern "C" HRESULT __stdcall DllGetClassObject(REFCLSID clsid, REFIID riid, void** object)
{
    ClassKind classKind;
    if (IsEqualCLSID(clsid, CLSID_IconReplacerChangeIconCommand))
    {
        classKind = ClassKind::ChangeIconCommand;
    }
    else if (IsEqualCLSID(clsid, CLSID_IconReplacerCollectionsCommand))
    {
        classKind = ClassKind::CollectionsCommand;
    }
    else if (IsEqualCLSID(clsid, CLSID_IconReplacerClassicContextMenu))
    {
        classKind = ClassKind::ClassicContextMenu;
    }
    else
    {
        return CLASS_E_CLASSNOTAVAILABLE;
    }

    auto* factory = new (std::nothrow) ClassFactory(classKind);
    if (factory == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    const auto result = factory->QueryInterface(riid, object);
    factory->Release();
    return result;
}
