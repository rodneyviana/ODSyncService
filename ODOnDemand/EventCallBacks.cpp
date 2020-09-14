#include "EventCallBacks.h"



std::string formathex(UINT64 Number)
{
	char str[30];
	sprintf_s(str, 30, "%I64x", Number);
	return str;
}

std::wstring ReadStringW(ULONG64 Offset)
{
	HRESULT hr = S_OK;

	std::wstring str(RESTART_MAX_CMD_LINE, ' ');
	ULONG size = str.size();
	hr = g_Data->ReadUnicodeStringVirtualWide(Offset, str.size() * sizeof(TCHAR), const_cast<wchar_t*>(str.c_str()), size, &size);
	if (hr == S_OK && size > 0)
	{
		str.resize(size - 1);
	}
	else
	{
		str = L"<ERROR-READING-MEMORY>";
	}
	return str;
}



#pragma region BreakPointClass
extern ComPtr<IDebugClient> g_Client;
extern ComPtr<IDebugControl5> g_Control;
extern ComPtr<IDebugSymbols> g_Symbols;
BreakpointClass::BreakpointClass(std::string BPExpression, std::string Tag, BPCallBack Method, ULONG64 BPOffset)
{
	this->tag = Tag;
	this->method = Method;
	hr = g_Control->AddBreakpoint(DEBUG_BREAKPOINT_CODE, DEBUG_ANY_ID, &breakPoint);
	if (IsValid())
	{
		ULONG flags = 0;
		breakPoint->GetFlags(&flags);
		if ((flags & DEBUG_BREAKPOINT_DEFERRED) == 0)
		{
			breakPoint->AddFlags(DEBUG_BREAKPOINT_ADDER_ONLY | DEBUG_BREAKPOINT_ENABLED);
			if (BPOffset)
			{
				hr = breakPoint->SetOffset(BPOffset);
				isOffset = true;
			}
			else
			{
				isOffset = false;
				hr = breakPoint->SetOffsetExpression(BPExpression.c_str());
			}
		}
		else
		{
			RemoveBreakPoint();
			hr = E_NOT_SET;
		}
	}
	else
	{
		hr = E_NOT_SET;
	}
}


void BreakpointClass::RemoveBreakPoint()
{
	if (!IsValid())
		return;

	if (breakPoint)
	{
		hr = g_Control->RemoveBreakpoint(breakPoint.Get());

		hr = E_NOT_SET;
	}

}

HRESULT BreakpointClass::InvokeCallBack()
{
	if (NULL == method)
	{
		//this->RemoveBreakPoint();
		return DEBUG_STATUS_IGNORE_EVENT;
	}
	return method(this->breakPoint.Get());
}

#pragma endregion

#pragma region EventCallBackClass

ComPtr<EventCallBacks> EventCallBacks::singleTon;

EventCallBacks::EventCallBacks()
{
	g_Client->SetEventCallbacks(this);
}


EventCallBacks::~EventCallBacks()
{
}




STDMETHODIMP_(ULONG)
EventCallBacks::AddRef(
	THIS
)
{
	// This class is designed to be static so
	// there's no true refcount.
	return 1;
}

STDMETHODIMP_(ULONG)
EventCallBacks::Release(
	THIS
)
{
	// This class is designed to be static so
	// there's no true refcount.
	return 0;
}

STDMETHODIMP
EventCallBacks::GetInterestMask(
	THIS_
	_Out_ PULONG Mask
)
{
#if _DEBUG
	*Mask =
		DEBUG_EVENT_BREAKPOINT | DEBUG_EVENT_LOAD_MODULE | DEBUG_EVENT_SESSION_STATUS | DEBUG_EVENT_EXCEPTION | DEBUG_EVENT_CHANGE_DEBUGGEE_STATE;
#else
	* Mask = DEBUG_EVENT_BREAKPOINT | DEBUG_EVENT_LOAD_MODULE | DEBUG_EVENT_SESSION_STATUS;
#endif
	/*|
	DEBUG_EVENT_EXCEPTION |
	DEBUG_EVENT_CREATE_PROCESS |
	DEBUG_EVENT_LOAD_MODULE |
	DEBUG_EVENT_SESSION_STATUS; */
	return S_OK;
}

STDMETHODIMP
EventCallBacks::Breakpoint(
	THIS_
	_In_ PDEBUG_BREAKPOINT Bp
)
{
	ULONG64 offset = 0;

	std::string Expression(MAX_PATH, ' ');

	ULONG size = 0;



	HRESULT hr = Bp->GetOffsetExpression(const_cast<char*>(Expression.c_str()), MAX_PATH, &size);

#if _DEBUG
	printf("Expression: %s\n", Expression.c_str());
#endif
	if (hr == S_OK && size > 0)
	{
		Expression.resize(size - 1);
		if (bps.find(Expression) != bps.end())
		{
			return bps[Expression].InvokeCallBack();
		}
	}
#if _DEBUG
	printf("BreakPoint could not be found\n");
#endif
	return DEBUG_STATUS_NO_CHANGE;
}

STDMETHODIMP
EventCallBacks::Exception(
	THIS_
	_In_ PEXCEPTION_RECORD64 Exception,
	_In_ ULONG FirstChance
)
{
#if _DEBUG
	printf("Exception: 0x%x\n", Exception->ExceptionCode);
#endif
	if (Exception->ExceptionCode == STATUS_BREAKPOINT)
	{
		return DEBUG_STATUS_BREAK;
	}
	return DEBUG_STATUS_GO;

}

STDMETHODIMP
EventCallBacks::CreateProcess(
	THIS_
	_In_ ULONG64 ImageFileHandle,
	_In_ ULONG64 Handle,
	_In_ ULONG64 BaseOffset,
	_In_ ULONG ModuleSize,
	_In_ PCSTR ModuleName,
	_In_ PCSTR ImageName,
	_In_ ULONG CheckSum,
	_In_ ULONG TimeDateStamp,
	_In_ ULONG64 InitialThreadHandle,
	_In_ ULONG64 ThreadDataOffset,
	_In_ ULONG64 StartOffset
)
{

	UNREFERENCED_PARAMETER(ImageFileHandle);
	UNREFERENCED_PARAMETER(Handle);
	UNREFERENCED_PARAMETER(ModuleSize);
	UNREFERENCED_PARAMETER(ModuleName);
	UNREFERENCED_PARAMETER(CheckSum);
	UNREFERENCED_PARAMETER(TimeDateStamp);
	UNREFERENCED_PARAMETER(InitialThreadHandle);
	UNREFERENCED_PARAMETER(ThreadDataOffset);
	UNREFERENCED_PARAMETER(StartOffset);

#if _DEBUG
	printf("Create Process: %s\n", ModuleName);
#endif
	return DEBUG_STATUS_GO;

}

STDMETHODIMP
EventCallBacks::LoadModule(
	THIS_
	_In_ ULONG64 ImageFileHandle,
	_In_ ULONG64 BaseOffset,
	_In_ ULONG ModuleSize,
	_In_ PCSTR ModuleName,
	_In_ PCSTR ImageName,
	_In_ ULONG CheckSum,
	_In_ ULONG TimeDateStamp
)
{

	UNREFERENCED_PARAMETER(ImageFileHandle);
	UNREFERENCED_PARAMETER(ModuleSize);
	UNREFERENCED_PARAMETER(ModuleName);
	UNREFERENCED_PARAMETER(CheckSum);
	UNREFERENCED_PARAMETER(TimeDateStamp);


#if _DEBUG
	printf("Loaded: %s\n", ModuleName);
#endif
	if (_stricmp(ModuleName, "shell32") == 0)
	{
		//int i = EventCallBacks::GetInstance()->AddBreakPoint("iisfreb!HandleGlTraceEvent", "", FrebEventCallBack);
#if _DEBUG
		//printf("BP Id = %i\n", i);
#endif
		return DEBUG_STATUS_BREAK;
	}

	return DEBUG_STATUS_GO;
}

STDMETHODIMP
EventCallBacks::SessionStatus(
	THIS_
	_In_ ULONG SessionStatus
)
{
#if _DEBUG
	printf("Session Status: 0x%x\n", SessionStatus);

#endif

	if (SessionStatus == DEBUG_SESSION_END)
	{
#if _DEBUG
		printf("Application ended\n");
#endif
		Log(L"Application Ended");
		exit(0);

		//s_isStop = true;
	}

}
#pragma endregion