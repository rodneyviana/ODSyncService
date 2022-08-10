// ODNative.cpp : Returns true or false for the Icon related to the action on OneDrive
//

#include "stdafx.h"
#include <atlbase.h>
#include <Shlobj.h>
#include <shellapi.h>
#include <string>
#include "psapi.h"
#include "ApiStatus.h"

#pragma region TrayArea

using namespace std;

wstring GetButtonText(HWND Toolbar, int Order)
{
	DWORD processId = 0;
	::GetWindowThreadProcessId(Toolbar, &processId);
	if (!processId)
		return L"<ERROR>No process id";
	auto hProc = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
	if (!hProc)
		return L"<ERROR>Unable to open desktop process";
	TBBUTTON* tbButton = (TBBUTTON*)VirtualAllocEx(hProc, NULL, sizeof(TBBUTTON), MEM_COMMIT, PAGE_READWRITE);
	if (!tbButton)
		return L"<ERROR>Unable to alocate desktop process memory (1)";
	auto response = ::SendMessage(Toolbar, TB_GETBUTTON, static_cast<WPARAM>(Order), (LPARAM)tbButton);
	if (!response)
	{
		VirtualFreeEx(hProc, tbButton, 0, MEM_RELEASE);
		CloseHandle(hProc);
		return L"<ERROR>Unable to get OneDrive tooltip text";

	}
	TBBUTTON button = { 0 };
	SIZE_T size;
	auto read = ReadProcessMemory(hProc, tbButton, &button, sizeof(TBBUTTON), &size);
	if (!read)
	{
		VirtualFreeEx(hProc, tbButton, 0, MEM_RELEASE);
		CloseHandle(hProc);
		return L"<ERROR>Unable to read tooltip information";
	}
	size = SendMessage(Toolbar, TB_GETBUTTONTEXT, static_cast<WPARAM>(button.idCommand), NULL);
	if (size == 0)
	{
		VirtualFreeEx(hProc, tbButton, 0, MEM_RELEASE);
		CloseHandle(hProc);
		return L"";
	}
	wchar_t* text = (wchar_t*)VirtualAllocEx(hProc, NULL, (size + 1) * sizeof(TCHAR), MEM_COMMIT, PAGE_READWRITE);
	if (!text)
	{
		VirtualFreeEx(hProc, tbButton, 0, MEM_RELEASE);
		CloseHandle(hProc);
		return L"<ERROR>Unable to alocate desktop process memory (1)";
	}
	size = SendMessage(Toolbar, TB_GETBUTTONTEXT, static_cast<WPARAM>(button.idCommand), (LPARAM)text);
	if (!size)
	{
		VirtualFreeEx(hProc, tbButton, 0, MEM_RELEASE);
		VirtualFreeEx(hProc, text, 0, MEM_RELEASE);
		CloseHandle(hProc);
		return L"<ERROR>Notification message is empty";
	}
	wstring strText(size, '\0');
	read = ReadProcessMemory(hProc, text, const_cast<wchar_t*>(strText.c_str()), (size) * sizeof(TCHAR), &size);
	VirtualFreeEx(hProc, tbButton, 0, MEM_RELEASE);
	if (read)
		VirtualFreeEx(hProc, text, 0, MEM_RELEASE);
	CloseHandle(hProc);
	if (!read)
	{
		return L"<ERROR>Unable to alocate desktop process memory (2)";
	}
	return strText;

}

/// <summary>
/// Get Status by Name of All Buttons in Toolbar area
/// </summary>
/// <param name="Handle">Handle of window</param>
/// <param name="OneDriveType">Display Name of OneDrive instance</param>
/// <returns></returns>
wstring GetStatusByName(HWND Handle, wstring OneDriveType)
{
	if (!Handle)
		return L"<ERROR>Handle not provided";
	auto count = ::SendMessage(Handle, TB_BUTTONCOUNT, NULL, NULL);
	if (!count)
		return L"<ERROR>No status text found";
	for (int i = 0; i < count; i++)
	{
		auto strStatus = GetButtonText(Handle, i);
		if (strStatus.find(OneDriveType) == 0)
		{
			return strStatus;
		}
	}
	return L"<ERROR>Status not found for type [" + OneDriveType + L"]";

}

wstring nameToCompare;
wstring currentStatus;

BOOL CALLBACK enumWindowCallback(HWND hWnd, LPARAM lparam) {
	if (currentStatus.size() > nameToCompare.size() && currentStatus.find(nameToCompare) == 0)
		return TRUE;
	const int maxSize = 2048;
	wstring processPath(maxSize, L'\0');
	wstring className(maxSize, L'\0');

	DWORD processId = NULL;
	::GetWindowThreadProcessId(hWnd, &processId);

	auto procHwnd = ::OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, processId);
	auto length = ::GetProcessImageFileName(procHwnd, const_cast<wchar_t*>(processPath.c_str()), maxSize);
	CloseHandle(procHwnd);
	if (length)
		processPath.resize(length);
	else
		processPath = L"<<UNKNOWN>>";
	auto processName = processPath.substr(processPath.find_last_of(L'\\') + 1);
	if (processName != L"explorer.exe")
		return true;
	length = ::GetClassName(hWnd, const_cast<wchar_t*>(className.c_str()), maxSize);
	if (length)
		className.resize(length);
	else
		className = L"<<UNKNOWN>>";

	// List visible windows with a non-empty title
	if (className == L"ToolbarWindow32") {
		currentStatus = GetStatusByName(hWnd, nameToCompare);
		if (currentStatus.find(L"<ERROR>") != 0)
			return TRUE;
	}
	auto parent = (LPARAM)&hWnd;
	::EnumChildWindows(hWnd, enumWindowCallback, parent);
	return TRUE;
}

/// <summary>
/// Get Status of OneDrive by Instance Name
/// </summary>
/// <param name="OneDriveType">Display Name of OneDrive instance (e.g OneDrive - Personal)</param>
/// <returns>Status or Error string</returns>
wstring GetStatusByName(wstring OneDriveType)
{
	nameToCompare = OneDriveType;
	currentStatus = L"";
	::EnumChildWindows(GetDesktopWindow(), enumWindowCallback, NULL);
	return currentStatus;
}



/// <summary>
/// Get Handle of ToolbarWindow32 (Deprecated)
/// </summary>
/// <returns>One Drive Window Handle in Taskbar</returns>
HWND GetHandle()
{
	auto hDesktop = GetDesktopWindow();
	auto hTray = FindWindowEx(hDesktop, 0, L"Shell_TrayWnd", NULL);
	auto hNotify = FindWindowEx(hTray, 0, L"TrayNotifyWnd", NULL);
	auto hSys = FindWindowEx(hNotify, 0, L"SysPager", NULL);
	auto hToolbar = FindWindowEx(hSys, 0, L"ToolbarWindow32", NULL);
	return hToolbar;
}

#pragma endregion TrayArea

__declspec(dllexport) HRESULT GetShellInterfaceFromGuid(BOOL* IsTrue , LPWSTR GuidString, LPWSTR Path)
{
	HRESULT hr = 0;
	hr = ::CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
	if (hr != S_OK)
	{
		CoUninitialize();
		hr = ::CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
	}
	CComPtr<IShellIconOverlayIdentifier> spShellIcon;
	CLSID CLSID_Interface;
#pragma warning (disable: 6031)
	CLSIDFromString(GuidString, &CLSID_Interface);
	hr = spShellIcon.CoCreateInstance(CLSID_Interface);

	*IsTrue = 0;
	if (hr != S_OK)
		return hr;

	*IsTrue = spShellIcon->IsMemberOf(Path, 0) == S_OK;
	
	::CoUninitialize();

	return S_OK; //(HRESULT)fi.hIcon; // S_OK;


}

__declspec(dllexport) HRESULT GetStatusByType(LPWSTR OneDriveType, LPWSTR Status, int Size, PINT ActualSize)
{
	const wstring error = L"<ERROR>Buffer size is insufficient";
	auto result = GetStatusByName(OneDriveType);
	int bytesSize = static_cast<int>((result.size() + 1) * 2);
	*ActualSize = bytesSize;
	if (bytesSize > Size)
	{
		std::memcpy(Status, error.c_str(), (error.size() + 1) * 2);
		return E_NOT_SUFFICIENT_BUFFER;
	}
	std::memcpy(Status, result.c_str(), bytesSize);

	return S_OK;
}

__declspec(dllexport) HRESULT GetStatusByTypeApi(LPWSTR SyncRootId, OneDriveState* State)
{
	OneDriveState stateData = {0};
	HRESULT hr = getInstanceStatus(SyncRootId, stateData);
	std::memcpy(State, &stateData, sizeof(OneDriveState));
	return hr;

}

