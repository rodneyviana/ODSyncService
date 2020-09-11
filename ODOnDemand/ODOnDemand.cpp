// ODOnDemand.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include "ODOnDemand.h"
#include "EventCallBacks.h"

std::string statusStr[] = {

	"DEBUG_STATUS_NO_CHANGE",
	"DEBUG_STATUS_GO",
	"DEBUG_STATUS_GO_HANDLED",
	"DEBUG_STATUS_GO_NOT_HANDLED",
	"DEBUG_STATUS_STEP_OVER",
	"DEBUG_STATUS_STEP_INTO",
	"DEBUG_STATUS_BREAK",
	"DEBUG_STATUS_NO_DEBUGGEE",
	"DEBUG_STATUS_STEP_BRANCH",
	"DEBUG_STATUS_IGNORE_EVENT",
	"DEBUG_STATUS_RESTART_REQUESTED",
	"DEBUG_STATUS_REVERSE_GO",
	"DEBUG_STATUS_REVERSE_STEP_BRANCH",
	"DEBUG_STATUS_REVERSE_STEP_OVER",
	"DEBUG_STATUS_REVERSE_STEP_INTO",
	"DEBUG_STATUS_OUT_OF_SYNC",
	"DEBUG_STATUS_WAIT_INPUT",
	"DEBUG_STATUS_TIMEOUT"
};

int wmain(int argc, wchar_t* argv[])
{
	myApp = L"\"";
	myApp.append(argv[0]);
	myApp.append(L"\" -launch ");
	HRESULT hr{ 0 };
	hr = ::CoInitialize(NULL);
	wprintf(L"log: %s\n", GetFileName().c_str());
	wprintf(L"Ansi Time: %s\n", GetTimeAnsi().c_str());
	Log(L"Initiating App");
    ParseCommand(argc, argv);
	if (s_isLaunchMode)
	{
		Log(format(L"Setting auto launch key. Use 'ODOnDemand.exe -clear' to remove the monitoring", myApp.c_str()));
		wprintf(L"Setting auto launch key. Use 'ODOnDemand.exe -clear' to remove the monitoring\n", myApp.c_str());
		hr = SetDebugKey(L"OneDrive.exe");
		if (hr == ERROR_ACCESS_DENIED)
		{
			Log(L"Access Denied. Please run ODOnDemand.exe in administrative mode");
			wprintf(L"Access Denied. Please run ODOnDemand.exe in administrative mode\n");
			exit(hr);
		}
		if (hr != ERROR_SUCCESS)
		{
			Log(format(L"Failed to set auto launch key for the monitoring. Error: %x", hr));
			wprintf(L"Failed to set auto launch key for the monitoring. Error: %x\n", hr);
			exit(hr);
		}
		exit(0);
	}

	if (s_isClear)
	{
		Log(format(L"Deleting auto launch key. Use 'ODOnDemand.exe -lauch' to restart monitoring", myApp.c_str()));
		wprintf(L"Deleting auto launch key. Use 'ODOnDemand.exe -lauch' to restart monitoring\n", myApp.c_str());
		hr = DeleteDebugKey(L"OneDrive.exe");
		if (hr == ERROR_ACCESS_DENIED)
		{
			Log(L"Access Denied. Please run ODOnDemand.exe in administrative mode");
			wprintf(L"Access Denied. Please run ODOnDemand.exe in administrative mode\n");
			exit(hr);
		}
		if (hr != ERROR_SUCCESS)
		{
			Log(format(L"Failed to remove launch key for the monitoring. Error: %x", hr));
			wprintf(L"Failed to remove launch key for the monitoring. Error: %x\n", hr);
			exit(hr);
		}
		exit(0);
	}
	
	if (0 == s_AttachId && cmdLine.size() == 0)
		return 0;
    CreateInterfaces();

	if (0 != s_AttachId)
		AttachTo(s_AttachId);
	else
	{
		if (cmdLine.find(L"/") == wstring::npos)
		{
			hr = CreateLocalProcess();
			Log(format(L"Created standalone process not monitored. HR=%d", hr));
			wprintf(L"Created standalone process not monitored. HR=%d\n", hr);
			exit(hr);
		}
		string cmdLineS = ws2s(cmdLine);
		hr = g_Client->CreateProcessW(NULL, const_cast<char*>(cmdLineS.c_str()), DEBUG_PROCESS);

		//hr = g_Control->WaitForEvent(DEBUG_WAIT_DEFAULT, INFINITE);
		
		Log(format(L"Successfully started process (id=%d, cmdLine='%s')", s_AttachId, cmdLine.c_str()));
		//hr = CreateDebuggingProcess();
		//AttachTo(s_AttachId);

	}
	EventCallBacks* callbacks = EventCallBacks::GetInstance();
	ULONG status{ 0 };

	s_isStop = false;
	bool once = false;
	while (true)
	{


		hr = g_Control->WaitForEvent(DEBUG_WAIT_DEFAULT, INFINITE);

		
		if (hr != 0)
			continue;
		printHR("WaitForEvent", hr);
		hr = g_Control->GetExecutionStatus(&status);
		printHR("GetStatus", hr);
		printf("%s\n", statusStr[status].c_str());

		switch (status)
		{

		case DEBUG_STATUS_BREAK:
			if (!once)
			{
				int i = callbacks->AddBreakPoint("SHELL32!Shell_NotifyIconW", "", ODEventCallBack);
				printf("BP %i\n", i);

				hr = g_Control->SetExecutionStatus(DEBUG_STATUS_GO);
				printHR("SetExecutionStatus to GO", hr);

				auto hr = g_System->GetCurrentProcessSystemId(&s_AttachId);
				wprintf(L"PID=%d,hr=%d\n", s_AttachId, hr);
				//hr = g_Control->WaitForEvent(DEBUG_WAIT_DEFAULT, 10000);
				//printHR("WaitForEvent", hr);

				once = i >= 0;
			}
			else
			{
				//hr = g_Control->SetExecutionStatus(DEBUG_STATUS_GO_NOT_HANDLED);
				//printHR("SetExecutionStatus to GO", hr);
				hr = g_Control->SetExecutionStatus(DEBUG_STATUS_GO);
				printHR("SetExecutionStatus to GO", hr);
				once = true;


			}
			
			break;

		case DEBUG_STATUS_NO_DEBUGGEE:
			printf("Application ended\n");
			Log(L"Application Ended");
			exit(0);
			break;
		default:
			printf("%s\n", statusStr[status].c_str());
			break;
		}



	}




	g_Control->SetExecutionStatus(DEBUG_STATUS_GO);
	hr = g_Control->WaitForEvent(0, INFINITE);

	if (hr != S_OK)
	{
		printf("Error: 0x%x\n", hr);
		return hr;
	}
	EventCallBacks::DeleteInstance();



	return 0;

}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
