#include <windows.h>
#include <appmodel.h>
#include <knownfolders.h>
#include <shlobj_core.h>
#include <shobjidl_core.h>

#include <algorithm>
#include <cstring>
#include <cwctype>
#include <new>
#include <string>
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
constexpr wchar_t AppId[] = L"IconReplacer.App";
constexpr wchar_t ChangeIconTitle[] = L"Change icon...";
constexpr wchar_t ChangeIconTooltip[] = L"Choose a .ico file and apply it to this folder or shortcut.";

// Keep in sync with ShellManifestContractService.ExplorerCommandClsid.
constexpr GUID CLSID_IconReplacerExplorerCommand =
{
    0xb8f1a86d,
    0x4c52,
    0x4c53,
    { 0xbf, 0x72, 0x30, 0xb5, 0x9f, 0x7f, 0x0f, 0x7d }
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

bool IsUncPath(const std::wstring& path)
{
    return path.size() >= 2 && path[0] == L'\\' && path[1] == L'\\';
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
    return target.kind == TargetKind::Unknown ? HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED) : S_OK;
}

std::wstring TargetKindArgument(TargetKind kind)
{
    return kind == TargetKind::Folder ? L"folder" : L"shortcut";
}

std::wstring QuoteActivationArgument(const std::wstring& argument)
{
    std::wstring quoted = L"\"";
    for (const auto character : argument)
    {
        if (character == L'"')
        {
            quoted.push_back(L'\\');
        }

        quoted.push_back(character);
    }

    quoted.push_back(L'"');
    return quoted;
}

std::wstring BuildChangeIconArguments(const ShellTarget& target)
{
    return L"change-icon --target " + QuoteActivationArgument(target.path) +
        L" --target-kind " + TargetKindArgument(target.kind);
}

HRESULT GetPendingActivationPath(std::wstring& pendingPath)
{
    PWSTR roamingAppData = nullptr;
    RETURN_IF_FAILED(SHGetKnownFolderPath(FOLDERID_RoamingAppData, KF_FLAG_DEFAULT, nullptr, &roamingAppData));

    std::wstring appRoot = roamingAppData;
    CoTaskMemFree(roamingAppData);
    appRoot += L"\\Icon Replacer";
    if (!CreateDirectoryW(appRoot.c_str(), nullptr))
    {
        const auto error = GetLastError();
        if (error != ERROR_ALREADY_EXISTS)
        {
            return HRESULT_FROM_WIN32(error);
        }
    }

    pendingPath = appRoot + L"\\pending-activation.args";
    return S_OK;
}

HRESULT ConvertToUtf8(const std::wstring& value, std::string& utf8)
{
    const auto byteCount = WideCharToMultiByte(
        CP_UTF8,
        0,
        value.c_str(),
        static_cast<int>(value.size()),
        nullptr,
        0,
        nullptr,
        nullptr);
    if (byteCount <= 0)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    utf8.resize(byteCount);
    const auto written = WideCharToMultiByte(
        CP_UTF8,
        0,
        value.c_str(),
        static_cast<int>(value.size()),
        utf8.data(),
        byteCount,
        nullptr,
        nullptr);
    return written == byteCount ? S_OK : HRESULT_FROM_WIN32(GetLastError());
}

HRESULT WritePendingActivation(const std::wstring& arguments)
{
    std::wstring pendingPath;
    RETURN_IF_FAILED(GetPendingActivationPath(pendingPath));

    std::string utf8;
    RETURN_IF_FAILED(ConvertToUtf8(arguments, utf8));

    HANDLE file = CreateFileW(
        pendingPath.c_str(),
        GENERIC_WRITE,
        0,
        nullptr,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        nullptr);
    if (file == INVALID_HANDLE_VALUE)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    DWORD written = 0;
    const auto ok = WriteFile(
        file,
        utf8.data(),
        static_cast<DWORD>(utf8.size()),
        &written,
        nullptr);
    const auto closeOk = CloseHandle(file);

    if (!ok)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    if (!closeOk)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    return written == utf8.size() ? S_OK : HRESULT_FROM_WIN32(ERROR_WRITE_FAULT);
}

HRESULT GetCurrentPackageFamily(std::wstring& familyName)
{
    UINT32 length = 0;
    auto result = GetCurrentPackageFamilyName(&length, nullptr);
    if (result != ERROR_INSUFFICIENT_BUFFER)
    {
        return HRESULT_FROM_WIN32(result);
    }

    std::vector<wchar_t> buffer(length);
    result = GetCurrentPackageFamilyName(&length, buffer.data());
    if (result != ERROR_SUCCESS)
    {
        return HRESULT_FROM_WIN32(result);
    }

    familyName.assign(buffer.data());
    return S_OK;
}

HRESULT BuildApplicationUserModelId(std::wstring& appUserModelId)
{
    std::wstring packageFamilyName;
    RETURN_IF_FAILED(GetCurrentPackageFamily(packageFamilyName));

    appUserModelId = packageFamilyName + L"!" + AppId;
    return S_OK;
}

HRESULT ActivatePackagedApp(const std::wstring& arguments)
{
    std::wstring appUserModelId;
    RETURN_IF_FAILED(BuildApplicationUserModelId(appUserModelId));

    IApplicationActivationManager* activationManager = nullptr;
    RETURN_IF_FAILED(CoCreateInstance(
        CLSID_ApplicationActivationManager,
        nullptr,
        CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(&activationManager)));

    DWORD processId = 0;
    const auto result = activationManager->ActivateApplication(
        appUserModelId.c_str(),
        arguments.c_str(),
        AO_NONE,
        &processId);
    activationManager->Release();
    return result;
}

class ExplorerCommand final : public IExplorerCommand
{
public:
    ExplorerCommand()
    {
        IncrementObjectCount();
    }

    ~ExplorerCommand()
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
        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IExplorerCommand))
        {
            *object = static_cast<IExplorerCommand*>(this);
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

    IFACEMETHODIMP GetTitle(IShellItemArray*, PWSTR* name) override
    {
        return CopyCoTaskMemString(ChangeIconTitle, name);
    }

    IFACEMETHODIMP GetIcon(IShellItemArray*, PWSTR* icon) override
    {
        if (icon == nullptr)
        {
            return E_POINTER;
        }

        *icon = nullptr;
        return E_NOTIMPL;
    }

    IFACEMETHODIMP GetToolTip(IShellItemArray*, PWSTR* tooltip) override
    {
        return CopyCoTaskMemString(ChangeIconTooltip, tooltip);
    }

    IFACEMETHODIMP GetCanonicalName(GUID* commandName) override
    {
        if (commandName == nullptr)
        {
            return E_POINTER;
        }

        *commandName = CLSID_IconReplacerExplorerCommand;
        return S_OK;
    }

    IFACEMETHODIMP GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) override
    {
        if (state == nullptr)
        {
            return E_POINTER;
        }

        ShellTarget target;
        *state = SUCCEEDED(TryGetSingleTarget(items, target)) ? ECS_ENABLED : ECS_DISABLED;
        return S_OK;
    }

    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) override
    {
        ShellTarget target;
        RETURN_IF_FAILED(TryGetSingleTarget(items, target));
        RETURN_IF_FAILED(WritePendingActivation(BuildChangeIconArguments(target)));
        return ActivatePackagedApp(L"");
    }

    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* flags) override
    {
        if (flags == nullptr)
        {
            return E_POINTER;
        }

        *flags = ECF_DEFAULT;
        return S_OK;
    }

    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** commands) override
    {
        if (commands == nullptr)
        {
            return E_POINTER;
        }

        *commands = nullptr;
        return E_NOTIMPL;
    }

private:
    long referenceCount_ = 1;
};

class ClassFactory final : public IClassFactory
{
public:
    ClassFactory()
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

        auto* command = new (std::nothrow) ExplorerCommand();
        if (command == nullptr)
        {
            return E_OUTOFMEMORY;
        }

        const auto result = command->QueryInterface(riid, object);
        command->Release();
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
    if (!IsEqualCLSID(clsid, CLSID_IconReplacerExplorerCommand))
    {
        return CLASS_E_CLASSNOTAVAILABLE;
    }

    auto* factory = new (std::nothrow) ClassFactory();
    if (factory == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    const auto result = factory->QueryInterface(riid, object);
    factory->Release();
    return result;
}
