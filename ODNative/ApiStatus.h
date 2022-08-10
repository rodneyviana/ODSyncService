#pragma once


#include <windows.h>
#include <atlbase.h>
#include <atlcom.h>
#include <string>
#include <vector>
#include <hstring.h>
#include <shellapi.h>
#include <wrl.h>
#include <wrl/async.h>

const int MAX_STATE_LABEL = 255;
const int MAX_ICON_URI = 1024;
const int MAX_QUOTA_LABEL = 255;

struct  OneDriveState {
    int CurrentState;
    TCHAR Label[MAX_STATE_LABEL];
    TCHAR IconUri[MAX_ICON_URI];
    BOOL isQuotaAvailable;
    uint64_t TotalQuota;
    uint64_t UsedQuota;
    TCHAR QuotaLabel[MAX_QUOTA_LABEL];
    BYTE IconColorA;
    BYTE IconColorR;
    BYTE IconColorG;
    BYTE IconColorB;
};

HRESULT getInstanceStatus(const std::wstring& syncrootId, OneDriveState& currentState);