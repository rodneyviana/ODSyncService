// ODNative.cpp : Returns true or false for the Icon related to the action on OneDrive
//

#include "stdafx.h"
#include <atlbase.h>
#include <Shlobj.h>
#include <shellapi.h>

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
	CLSIDFromString(GuidString, &CLSID_Interface);
	hr = spShellIcon.CoCreateInstance(CLSID_Interface);

	*IsTrue = 0;
	if (hr != S_OK)
		return hr;

	int Priority = -1;
	SHFILEINFO fi = {0};
	::SHGetFileInfo(Path, FILE_ATTRIBUTE_DIRECTORY, &fi, sizeof(fi), SHGFI_ADDOVERLAYS | SHGFI_ICONLOCATION | SHGFI_OVERLAYINDEX | SHGFI_ICON | SHGFI_TYPENAME);

	*IsTrue = spShellIcon->IsMemberOf(Path, 0) == S_OK;
	

	return S_OK; //(HRESULT)fi.hIcon; // S_OK;


}