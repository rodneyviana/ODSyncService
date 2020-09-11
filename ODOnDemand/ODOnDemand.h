#pragma once
#ifndef ONDEMAND_H
#define ONDEMAND_H

// TODO: add headers that you want to pre-compile here
#include <wrl\client.h>
#include <DbgEng.h>
#pragma comment(lib, "dbgeng.lib")
#include <string>
#include <vector>
#include <map>
#include <string>


#pragma region Helper

using namespace Microsoft::WRL;

extern ComPtr<IDebugClient> g_Client;
extern ComPtr<IDebugControl5> g_Control;
extern ComPtr<IDebugSymbols> g_Symbols;
extern ComPtr<IDebugDataSpaces4> g_Data;
extern ComPtr<IDebugSystemObjects3> g_System;

const LPCWSTR DEBUG_REGISTRY_KEY = L"Software\\Microsoft\\Windows NT\\"
L"CurrentVersion\\Image File Execution Options\\";
#ifdef _M_AMD64
const LPCWSTR DEBUG_REGISTRY_KEY_WOW64 = L"Software\\Wow6432Node\\Microsoft\\Windows NT\\"
L"CurrentVersion\\Image File Execution Options\\";
#endif

using namespace std;

extern wstring s_appPool;
extern bool s_attach;
extern bool s_isLaunchMode;
extern bool s_isClear;
extern bool s_isStop;
extern ULONG s_AttachId;
extern wstring cmdLine;
extern wstring myApp;


bool IsInteractive();

void CreateInterfaces();

void PrintUsage();

void PrintUsageAndExit(int Error, wstring Message = L"");

void PrintErrorAndExit(HRESULT Error, wstring Message = L"");

void ParseCommand(int argc, wchar_t* argv[]);

HRESULT SetDebugKey(const wstring& ApplicationName);

LONG DeleteDebugKey(const wstring& ApplicationName);

HRESULT ODEventCallBack(IDebugBreakpoint* BP);

std::string ws2s(const std::wstring& wstr);

void AttachTo(ULONG PID);

void printHR(const std::string& Message, const HRESULT& hr);

wstring GetTimeAnsi();
wstring GetFileName();

ULONG CreateLocalProcess();
ULONG CreateDebuggingProcess();

wstring format(const wchar_t* formatStr, ...);

enum sev
{
	Verbose,
	Information,
	Warning,
	Error
};

wstring GetLogFolder();

void Log(wstring Message, wstring source = L"General", sev severity = sev::Information);
#pragma endregion // Helper

#endif //ONDEMAND_H