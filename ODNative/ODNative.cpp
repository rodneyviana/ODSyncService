// ODNative.cpp : Returns true or false for the Icon related to the action on OneDrive
//

#include "stdafx.h"
#include <atlbase.h>
#include <Shlobj.h>
#include <shellapi.h>
#include <string>


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

HWND GetHandle(wstring OneDriveType)
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
	auto handle = GetHandle(OneDriveType);
	auto result = GetStatusByName(handle, OneDriveType);
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

