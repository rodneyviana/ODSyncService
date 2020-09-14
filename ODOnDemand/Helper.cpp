#include "ODOnDemand.h"
#include "EventCallBacks.h"
#include <fstream>
#include <locale>
#include <codecvt>
#include <wil/resource.h>

wstring s_appPool;
wstring cmdLine;
bool s_attach = false;
bool s_isLaunchMode = false;
bool s_isClear = false;
bool s_isStop = false;
ULONG s_AttachId = 0;
wstring myApp;

ComPtr<IDebugClient> g_Client;
ComPtr<IDebugControl5> g_Control;
ComPtr<IDebugSymbols> g_Symbols;
ComPtr<IDebugDataSpaces4> g_Data;
ComPtr<IDebugSystemObjects3> g_System;

wstring logFolder;

// below from https://stackoverflow.com/questions/4804298/how-to-convert-wstring-into-string
std::string ws2s(const std::wstring& wstr)
{
	using convert_typeX = std::codecvt_utf8<wchar_t>;
	std::wstring_convert<convert_typeX, wchar_t> converterX;

	return converterX.to_bytes(wstr);
}

wstring GetLogFolder()
{
	if (logFolder.size() > 0)
		return logFolder;
	logFolder = wstring(MAX_PATH, '\0');
	DWORD count = MAX_PATH;

	count = ::ExpandEnvironmentStrings(L"%LOCALAPPDATA%\\OneDriveMonitor", const_cast<wchar_t*>(logFolder.c_str()), count);
	logFolder.resize(count-1);
	BOOL created = ::CreateDirectory(logFolder.c_str(), NULL);
	HRESULT hr = ::GetLastError();
	if (FALSE == created && hr != ERROR_ALREADY_EXISTS)
	{
		PrintErrorAndExit(hr, L"Unable to create folder " + logFolder);
	}

	logFolder.append(L"\\Logs");
	created = ::CreateDirectory(logFolder.c_str(), NULL);
	hr = ::GetLastError();
	if (FALSE == created && hr != ERROR_ALREADY_EXISTS)
	{
		PrintErrorAndExit(hr, L"Unable to create folder " + logFolder);
	}
	logFolder.append(L"\\");
	return logFolder;

}

wstring format(const wchar_t* formatStr, ...)
{
	va_list args;
	va_start(args, formatStr);
	auto len = _vscwprintf(formatStr, args);
	va_end(args);
	std::wstring result((size_t)len, L'\0');

	va_start(args, formatStr);
	auto count = _vsnwprintf_s(const_cast<wchar_t*>(result.c_str()), len+1, len+1, formatStr, args);
	va_end(args);
	return result;
}

wstring GetTimeAnsi()
{
	SYSTEMTIME st;
	::GetLocalTime(&st);
	auto timeStr = format(L"%04d-%02d-%02d %02d:%02d:%02d.%03d", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
	return timeStr;
}
wstring GetFileName()
{
	SYSTEMTIME st;
	::GetLocalTime(&st);
	auto timeStr = format(L"OneDriveMonitor_%04d-%02d-%02d.log", st.wYear, st.wMonth, st.wDay);
	return GetLogFolder() + timeStr;

}

void Log(wstring Message, wstring source, sev severity)
{
	wstring sevStr;
	switch (severity)
	{
		case sev::Error:
			sevStr = L"Error      ";
			break;
		case sev::Information:
			sevStr = L"Information";
			break;
		case sev::Verbose:
			sevStr = L"Verbose    ";
			break;
		case sev::Warning:
			sevStr = L"Warning    ";
			break;
	}

	//if (s_AttachId == 0)
	//{
	//	auto hr = g_System->GetCurrentProcessSystemId(&s_AttachId);
	//	wprintf(L"PID=%d,hr=%d\n", s_AttachId, hr);

	//}

	wstring logLine = format(L"%s\t%s\t%s\t%s\t%d",
		GetTimeAnsi().c_str(),
		sevStr.c_str(),
		source.c_str(),
		Message.c_str(),
		s_AttachId
		);
	wofstream file;
	file.open(GetFileName(), ios_base::app | ios::out);
	file << logLine << endl;
	file.close();
}

bool IsInteractive()
{
	HANDLE hProcessToken = NULL;
	DWORD groupLength = 50;

	PTOKEN_GROUPS groupInfo = (PTOKEN_GROUPS)LocalAlloc(0,
		groupLength);

	SID_IDENTIFIER_AUTHORITY siaNt = SECURITY_NT_AUTHORITY;
	PSID InteractiveSid = NULL;
	PSID ServiceSid = NULL;
	DWORD i;

	// Start with assumption that process is not a Service.
	bool fExe = true;


	if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY,
		&hProcessToken))
		goto ret;

	if (groupInfo == NULL)
		goto ret;

	if (!GetTokenInformation(hProcessToken, TokenGroups, groupInfo,
		groupLength, &groupLength))
	{
		if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
			goto ret;

		LocalFree(groupInfo);
		groupInfo = NULL;

		groupInfo = (PTOKEN_GROUPS)LocalAlloc(0, groupLength);

		if (groupInfo == NULL)
			goto ret;

		if (!GetTokenInformation(hProcessToken, TokenGroups, groupInfo,
			groupLength, &groupLength))
		{
			goto ret;
		}
	}

	//
	//  Is the interactive group active in the token? If so, we know that
	//    this is an interactive process.
	//
	//  We also look for the "service" SID, and if it's present, we know we're a service.
	//
	//  The service SID will be present iff the service is running in a
	//  user account (and was invoked by the service controller).
	//
	if (!AllocateAndInitializeSid(&siaNt, 1, SECURITY_INTERACTIVE_RID, 0,
		0,
		0, 0, 0, 0, 0, &InteractiveSid))
	{
		goto ret;
	}

	if (!AllocateAndInitializeSid(&siaNt, 1, SECURITY_SERVICE_RID, 0, 0, 0,
		0, 0, 0, 0, &ServiceSid))
	{
		goto ret;
	}

	for (i = 0; i < groupInfo->GroupCount; i += 1)
	{
		SID_AND_ATTRIBUTES sanda = groupInfo->Groups[i];
		PSID Sid = sanda.Sid;

		//
		//  Check to see if the group we're looking at is one of
		//  the 2 groups we're interested in.
		//

		if (EqualSid(Sid, InteractiveSid))
		{
			//
			//  This process has the Interactive SID in its
			//  token.  This means that the process is running as
			//  an EXE.
			//
			goto ret;
		}
		else if (EqualSid(Sid, ServiceSid))
		{
			//
			//  This process has the Service SID in its
			//  token.  This means that the process is running as
			//  a service running in a user account.
			//
			fExe = FALSE;
			goto ret;
		}
	}

	//
	//  Neither Interactive or Service was present in the current users token,
	//  This implies that the process is running as a service, most likely
	//  running as LocalSystem.
	//
	fExe = FALSE;

ret:

	if (InteractiveSid)
		FreeSid(InteractiveSid);

	if (ServiceSid)
		FreeSid(ServiceSid);

	if (groupInfo)
		LocalFree(groupInfo);

	if (hProcessToken)
		CloseHandle(hProcessToken);

	return(fExe);
}


void CreateInterfaces()
{
	HRESULT Status;

	// Start things off by getting an initial interface from
	// the engine.  This can be any engine interface but is
	// generally IDebugClient as the client interface is
	// where sessions are started.
	if ((Status = DebugCreate(__uuidof(IDebugClient),
		(void**)&g_Client)) != S_OK)
	{
		exit(1);
		//Exit(1, "DebugCreate failed, 0x%X\n", Status);
	}

	// Query for some other interfaces that we'll need.
	if ((Status = g_Client->QueryInterface(__uuidof(IDebugControl5),
		(void**)&g_Control)) != S_OK ||
		(Status = g_Client->QueryInterface(__uuidof(IDebugSymbols),
			(void**)&g_Symbols)) != S_OK ||
		(Status = g_Client->QueryInterface(__uuidof(IDebugDataSpaces4),
			(void**)&g_Data)) != S_OK ||
		(Status = g_Client->QueryInterface(__uuidof(IDebugSystemObjects3),
			(void**)&g_System)) != S_OK)

	{
		PrintErrorAndExit(Status, L"QueryInterface failed");
		//Exit(1, "QueryInterface failed, 0x%X\n", Status);
	}
}

void PrintUsage()
{
	printf("OneDrive OnDemand monitoring tool 1.0");
	printf("\n");
	printf("Syntax:                                          \n");
	printf("ODOnDemand.exe [-help] [-clear] [-onLaunch] [-stop] [-launch <cmd-line> | -attach <PID>]\n");
	printf("Where:\n");
	printf("\t-help              - show this help\n");
	printf("\t-attach <PID>      - attach OneDrive instance to PID (e.g. -attach 19024(\n");
	printf("\t-onLaunch          - register as debugger and monitor every new launch nut not any current\n");
	printf("\t-clear             - unregister as debugger nut will not detach current monitoring\n");
	printf("\t-launch <cmd-line> - launch OnedDrive with cmd-line (used only internally)\n");
	printf("\t-stop              - stop any current monitoring [NOT IMPLEMENTED!!!]\n");

}

void PrintUsageAndExit(int Error, wstring Message)
{
	wstring str;
	if (Message.size() != 0)
	{
		str.append(format(L"Invalid Command: %s.\n", Message.c_str()));
	}

	if (IsInteractive())
	{
		wprintf(str.c_str());
		PrintUsage();
	}
	else
	{
		OutputDebugString(str.c_str());
	}

	exit(Error);
}

void PrintErrorAndExit(HRESULT Error, wstring Message)
{
	wstring str;
	if (Error != 0)
	{
		str.append(format(L" Error: 0x%x. ", Error));
	}
	if (Message.size() != 0)
	{
		str.append(format(Message.c_str()));
	}
	if (Error != 0)
	{
		str.append(format(L" Error: 0x%x", Error));
	}
	str.append(L"\n");
	
	if (IsInteractive())
	{
		wprintf(str.c_str());
	}
	Log(str.c_str(), L"General", sev::Error);
	exit(Error);
}

void AttachTo(ULONG PID)
{
	HRESULT hr = g_Client->AttachProcess(NULL, PID, DEBUG_ATTACH_DEFAULT);
	if (hr != S_OK)
	{
		PrintErrorAndExit(hr);
	}
	//hr = g_Control->WaitForEvent(DEBUG_WAIT_DEFAULT, INFINITE);
	if (hr != S_OK)
		PrintErrorAndExit(hr);
}

LONG SetDebugKey(const wstring& ApplicationName)
{
	wstring key(DEBUG_REGISTRY_KEY);
	key.append(ApplicationName);
	HKEY debKey = NULL;
	LONG status = ::RegCreateKeyEx(HKEY_LOCAL_MACHINE, key.c_str(), 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &debKey, NULL);
	if (status != ERROR_SUCCESS)
		return status;
	//RegCloseKey(debKey)
#ifdef _M_AMD64
	key = DEBUG_REGISTRY_KEY_WOW64;
	key.append(ApplicationName);
	HKEY debKeyWoW = NULL;
	status = ::RegCreateKeyEx(HKEY_LOCAL_MACHINE, key.c_str(), 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &debKeyWoW, NULL);

#endif

	status = RegSetValueEx(debKey, L"Debugger", 0, REG_SZ, (BYTE*)const_cast<wchar_t*>(myApp.c_str()), (DWORD)(2 * (myApp.size() + 1)));
	return status;
}

LONG DeleteDebugKey(const wstring& ApplicationName)
{
	wstring key(DEBUG_REGISTRY_KEY);
	key.append(ApplicationName);
	HKEY debKey = NULL;
	LONG status = ::RegCreateKeyEx(HKEY_LOCAL_MACHINE, key.c_str(), 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &debKey, NULL);
	if (status != ERROR_SUCCESS)
		return status;
	//RegCloseKey(debKey)
#ifdef _M_AMD64
	key = DEBUG_REGISTRY_KEY_WOW64;
	key.append(ApplicationName);
	HKEY debKeyWoW = NULL;
	status = ::RegCreateKeyEx(HKEY_LOCAL_MACHINE, key.c_str(), 0, NULL, REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &debKeyWoW, NULL);

#endif

	status = RegDeleteValue(debKey, L"Debugger");
	return status;
}

static wstring lastStatus;

HRESULT ODEventCallBack(IDebugBreakpoint* BP)
{
	HRESULT hr = S_OK;
	DEBUG_STACK_FRAME_EX frame{ 0 };
	ULONG frames = 0;
	g_Control->GetStackTraceEx(NULL, NULL, NULL, &frame, 1, &frames);

	if (frames == 1 && frame.Params[1] != NULL)
	{
;
		ULONG bytes = 0;
		DEBUG_VALUE vl = { 0 };
		wstring evaluation = format(L"poi(0x%p) & 0xffff", frame.Params[1] + offsetof(NOTIFYICONDATA, uID));
		hr = g_Control->EvaluateWide(evaluation.c_str(), DEBUG_VALUE_INT64, &vl, NULL);

		if (hr != S_OK)
		{
#if _DEBUG
			printf("error: %x\n", hr);
#endif
			Log(format(L"Notify Icon error 0x%x", hr), L"Breakpoint", sev::Error);
			return DEBUG_STATUS_GO_HANDLED;
		}
		wstring tipStr(128, '\0');
		bytes = 128 * sizeof(wchar_t);
		hr = g_Data->ReadVirtual(frame.Params[1] + offsetof(NOTIFYICONDATA, szTip), const_cast<wchar_t*>(tipStr.c_str()), bytes, &bytes);
		if (hr != S_OK)
		{
#if _DEBUG
			printf("error: %x\n", hr);
#endif
			Log(format(L"Notify Icon message error 0x%x", hr), L"Breakpoint", sev::Error);
			return DEBUG_STATUS_GO_HANDLED;
		}

		auto zero = tipStr.find(L'\0');
		if (zero != wstring::npos)
		{
			tipStr.resize(zero);
		}

		zero = tipStr.find(L"\x0a");
		if (zero != wstring::npos)
		{
			tipStr[zero] = L'.';
		}
		tipStr = L",tootip='" + tipStr + L"'";
		UINT uID = vl.I32;

		if (lastStatus != tipStr)
		{
#if _DEBUG
			wprintf(L"Status changed: %s\n", tipStr.c_str());
#endif
			Log(format(L"uID=%d,Action=%d", uID, frame.Params[0]) + tipStr, L"IconChange");
			lastStatus = tipStr;
		}
#if _DEBUG
		else
			wprintf(L"No logging because status is the same");
#endif

	}

	return DEBUG_STATUS_GO_HANDLED;

}

void ParseCommand(int argc, wchar_t* argv[])
{
	for (int i = 1; i < argc; ++i)
	{
		if (argv[i][0] == L'/')
		{
			argv[i][0] = L'-';
		}
		if ((_wcsicmp(argv[i], L"-help") == 0) || (_wcsicmp(argv[i], L"-?") == 0))
		{
			PrintUsage();
			exit(0);
		}

		if (_wcsicmp(argv[i], L"-appPool") == 0)
		{
			if (++i >= argc)
			{
				PrintUsageAndExit(1, argv[i-1]);
			}
			// Assume the next argument is the command line filter string.
			s_appPool.assign(argv[i]);
		}
		if (_wcsicmp(argv[i], L"-attach") == 0)
		{
			if (true == s_isLaunchMode)
			{
				PrintUsageAndExit(1, L"-attach cannot be combined with -onLaunch");
			}
			s_isLaunchMode = false;
			if (++i >= argc)
			{
				PrintUsageAndExit(1, argv[0]);
			}
			if (swscanf_s(argv[i], L"%u", &s_AttachId) != 1)
			{
				PrintUsageAndExit(1, argv[0]);
			}
		}


		if (_wcsicmp(argv[i], L"-clear") == 0)
		{
			if (i + 1 < argc)
			{
				PrintUsageAndExit(1, L"-clear cannot be combined with any other command");
			}
			s_isClear = true;
		}

		if (_wcsicmp(argv[i], L"-onLaunch") == 0)
		{
			if (0 != s_AttachId)
			{
				PrintUsageAndExit(1, L"-attach cannot be combined with -onLaunch");
			}


			s_isLaunchMode = true;
		}

		if (_wcsicmp(argv[i], L"-launch") == 0)
		{
			
			while (++i < argc)
			{
				bool first = cmdLine.size() == 0;
				if (!first)
					cmdLine.append(L" ");
				else
					cmdLine.append(L"\"");
				
				cmdLine.append(argv[i]);
				if (first)
					cmdLine.append(L"\"");
			}
		}

	}


	return;
}

ULONG CreateLocalProcess()
{
	if (cmdLine.size() == 0)
		return 0;
	PROCESS_INFORMATION pi = { 0 };
	STARTUPINFO si = { 0 };

	DWORD retValue = 0;
	DWORD exitCode = 0;
	BOOL result = 0;
	DWORD rc = 0;
	BOOL isDebugModeError;

	BOOL procStarted = CreateProcess(NULL, &cmdLine.front(), NULL, NULL, FALSE,
		CREATE_SUSPENDED | DEBUG_ONLY_THIS_PROCESS, NULL, NULL, &si, &pi);
	wil::unique_handle procHandle(pi.hProcess);
	wil::unique_handle threadHandle(pi.hThread);
	if (!procStarted)
	{
		retValue = ::GetLastError();
		Log(format(L"Could launch guest process. Error: %x\n", retValue), L"Launch", sev::Error);
		return retValue;
	}

	isDebugModeError = DebugActiveProcessStop(pi.dwProcessId);
	rc = ResumeThread(threadHandle.get());
	if (rc < 1)
	{
		retValue = GetLastError();
		Log(format(L"Error: Could not resume the created process (%s)\nError: %d",
			cmdLine.c_str(), retValue), L"Launch", sev::Error);
		if (!TerminateProcess(pi.hProcess, 1))
		{
			retValue = GetLastError();
			Log(format(L"Error: Could not terminate the created process:%s\n\tError: %d",
				cmdLine.c_str(), retValue), L"Launch", sev::Error);
		}
	}
	WaitForSingleObject(procHandle.get(), INFINITE);

	// Get the exit code.
	exitCode = 0;
	result = GetExitCodeProcess(procHandle.get(), &exitCode);
	if (result)
		retValue = exitCode;
	return retValue;

}

ULONG CreateDebuggingProcess()
{
	if (cmdLine.size() == 0)
		return 0;
	PROCESS_INFORMATION pi = { 0 };
	STARTUPINFO si = { 0 };

	DWORD retValue = 0;
	BOOL procStarted = CreateProcess(NULL, &cmdLine.front(), NULL, NULL, FALSE,
		CREATE_SUSPENDED, NULL, NULL, &si, &pi);
	wil::unique_handle procHandle(pi.hProcess);
	wil::unique_handle threadHandle(pi.hThread);
	DWORD rc = 0;
	if (!procStarted)
	{
		retValue = ::GetLastError();
		Log(format(L"Could launch guest process. Error: %x\n", retValue), L"Launch", sev::Error);
		return retValue;
	}

	AttachTo(pi.dwProcessId);
	rc = ResumeThread(threadHandle.get());
	if (rc < 1)
	{
		retValue = GetLastError();
		Log(format(L"Error: Could not resume the created process (%s)\nError: %d",
			cmdLine.c_str(), retValue), L"Launch", sev::Error);
		if (!TerminateProcess(procHandle.get(), 1))
		{
			retValue = GetLastError();
			Log(format(L"Error: Could not terminate the created process:%s\n\tError: %d",
				cmdLine.c_str(), retValue), L"Launch", sev::Error);
		}
		return retValue;
	}

	s_AttachId = pi.dwProcessId;
	Log(format(L"Successfully started process (id=%d, cmdLine='%s')", s_AttachId, cmdLine.c_str()));

	return retValue;
}

void printHR(const std::string& Message, const HRESULT& hr)
{
	printf("%s - ", Message.c_str());
	printf("HR: %x\n", hr);
}




