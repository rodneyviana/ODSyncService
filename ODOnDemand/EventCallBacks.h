#pragma once

#include "ODOnDemand.h"



using namespace std;
using namespace Microsoft::WRL;

extern ComPtr<IDebugClient> g_Client;
extern ComPtr<IDebugDataSpaces4> g_Data;

template<class T>
HRESULT ReadTargetMemory(ULONG64 Offset, T* Buffer)
{
	ULONG bytes = 0;
	return g_Data->ReadVirtual(Offset, &Buffer, sizeof(T), &bytes);
};

std::wstring ReadStringW(ULONG64 Offset);

std::string formathex(UINT64 Number);
#pragma region BreakPointClass

typedef HRESULT(*BPCallBack)(IDebugBreakpoint* BP);

struct BreakpointClass
{
public:

	BreakpointClass() {};
	BreakpointClass(std::string BPExpression, std::string Tag, BPCallBack Method, ULONG64 BPOffset = 0);


	HRESULT InvokeCallBack();
	void RemoveBreakPoint();
	bool IsValid()
	{
		return hr == S_OK;
	}

	bool IsOffset()
	{
		return isOffset;
	}

	ComPtr<IDebugBreakpoint> breakPoint;
protected:
	bool isOffset = false;
	std::string tag;
	BPCallBack method;
	HRESULT hr = 0;
};
#pragma endregion

#pragma region EventCallBacks

class EventCallBacks :
	public DebugBaseEventCallbacks
{
public:
	EventCallBacks();
	~EventCallBacks();
	static EventCallBacks* GetInstance()
	{
		if (NULL != EventCallBacks::singleTon.Get())
		{
#if _DEBUG
			printf("[CREATE] EventCallBack is Not NULL\n");
#endif
			return singleTon.Get();
		}
#if _DEBUG
		printf("[CREATE] EventCallBack is NULL\n");
#endif

		singleTon = ComPtr<EventCallBacks>(new EventCallBacks());
		return singleTon.Get();
	}

	static void DeleteInstance()
	{
		if (NULL == EventCallBacks::singleTon.Get())
			return;
#if _DEBUG
		printf("[DELETE] EventCallBack is Not NULL\n");
#endif

		if (true /* g_ExtInstancePtr->m_Control4.IsSet() */)
		{
			for (auto i = GetInstance()->bps.begin(); i != GetInstance()->bps.end(); i++)
			{
#if _DEBUG
				UINT64 IP = 0;
				i->second.breakPoint->GetOffset(&IP);
				printf("Deleting BP: %p IsValid: %s\n", IP, i->second.IsValid() ? "true" : "false");
#endif

				i->second.RemoveBreakPoint();
			}
		}
		GetInstance()->bps.clear();
		PDEBUG_EVENT_CALLBACKS callBack = NULL;
		if (true /* g_ExtInstancePtr->m_Client4.IsSet() */)
		{
			g_Client->GetEventCallbacks(&callBack);
			if (NULL != callBack)
			{
				g_Client->SetEventCallbacks(NULL);

			}
		}
		singleTon.Reset();

	}

	int AddBreakPoint(const std::string& BPExpr, std::string Tag, BPCallBack Method)
	{
#if _DEBUG
		printf("Add BP: %s ", Tag.c_str());
		printf("[%s]\n", BPExpr.c_str());
#endif
		//std::string BPStr = formathex(BPExpr);
		if (bps.find(BPExpr) == bps.end())
		{
			bps.emplace(BPExpr, BreakpointClass(BPExpr, Tag, Method));
			if (bps.find(BPExpr)->second.IsValid())
				return static_cast<int>(bps.size());
			bps.erase(BPExpr);
		}
#if _DEBUG
		printf("*** Adding BP %s failed\n", BPExpr.c_str());
#endif


		return -1;
	}

	// IUnknown.
	STDMETHOD_(ULONG, AddRef)(
		THIS
		);
	STDMETHOD_(ULONG, Release)(
		THIS
		);

	// IDebugEventCallbacks.
	STDMETHOD(GetInterestMask)(
		THIS_
		_Out_ PULONG Mask
		);

	STDMETHOD(Breakpoint)(
		THIS_
		_In_ PDEBUG_BREAKPOINT Bp
		);
	STDMETHOD(Exception)(
		THIS_
		_In_ PEXCEPTION_RECORD64 Exception,
		_In_ ULONG FirstChance
		);
	STDMETHOD(CreateProcess)(
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
		);
	STDMETHOD(LoadModule)(
		THIS_
		_In_ ULONG64 ImageFileHandle,
		_In_ ULONG64 BaseOffset,
		_In_ ULONG ModuleSize,
		_In_ PCSTR ModuleName,
		_In_ PCSTR ImageName,
		_In_ ULONG CheckSum,
		_In_ ULONG TimeDateStamp
		);
	STDMETHOD(SessionStatus)(
		THIS_
		_In_ ULONG Status
		);
protected:
	std::map<std::string, BreakpointClass> bps;
	static ComPtr<EventCallBacks> singleTon;
};

#pragma endregion
