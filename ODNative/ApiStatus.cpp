#include "stdafx.h"
#include "ApiStatus.h"

GUID CLSID_FileCoAuth_StorageProviderStatusUISourceFactory = { /* 0827D883-485C-4D62-BA2C-A332DBF3D4B0 */  0x0827D883, 0x485C,  0x4D62, {0xBA, 0x2C, 0xA3, 0x32, 0xDB, 0xF3, 0xD4, 0xB0} };

void safeStringCopy(void* dest, int dest_size, void* source, int source_size) {
    int realSize = (min(source_size, dest_size)) * (sizeof TCHAR);
    ZeroMemory(dest, dest_size * (sizeof TCHAR));
    if (nullptr != source)
    {
        std::memcpy(dest, source, realSize);
    }
}

HRESULT printStatusUI(ABI::Windows::Storage::Provider::IStorageProviderStatusUISource* pSource, OneDriveState& currentState)
{
    HRESULT getHR = S_OK;

    ABI::Windows::Storage::Provider::IStorageProviderStatusUI* status = nullptr;
    HRESULT hr = pSource->GetStatusUI(&status);

    if (SUCCEEDED(hr))
    {
        ABI::Windows::Storage::Provider::StorageProviderState state = {};
        getHR = status->get_ProviderState(&state);
        //std::cout << "////////// Latest status UI is ////////////" << std::endl;
        //std::cout << "Current State is: " << state << " . HRESULT from get_ProviderState is: " << std::hex << getHR << std::endl;
        currentState.CurrentState = state;
        // Test ProviderStateLabel
        HSTRING stateLable = nullptr;
        getHR = status->get_ProviderStateLabel(&stateLable);
        UINT32 stateLableLength = 0;
        auto rawLabel = WindowsGetStringRawBuffer(stateLable, &stateLableLength);
        safeStringCopy(&(currentState.Label), MAX_STATE_LABEL, (void*)rawLabel, stateLableLength);
        //std::wstring stateLableString = WindowsGetStringRawBuffer(stateLable, &stateLableLength);
        //std::wcout << L"Current ProviderStateLabel is: " << stateLableString << " size: " << stateLableLength << " . HRESULT from get_ProviderState is: " << std::hex << getHR << std::endl;


        // Test ProviderStateIcon
        ABI::Windows::Foundation::IUriRuntimeClass* uri = nullptr;
        getHR = status->get_ProviderStateIcon(&uri);

        if (SUCCEEDED(getHR))
        {
            HSTRING stateIcon = nullptr;
            UINT32 stateIconLength = 0;
            getHR = uri->get_AbsoluteUri(&stateIcon);
            auto rawIcon = WindowsGetStringRawBuffer(stateIcon, &stateIconLength);
            safeStringCopy(&(currentState.IconUri), MAX_ICON_URI, (void*)rawIcon, stateIconLength);
            //std::wstring stateIconString = WindowsGetStringRawBuffer(stateIcon, &stateIconLength);
            //std::wcout << L"Current ProviderStateIcon is: " << stateIconString << " . HRESULT from get_AbsoluteUri is: " << std::hex << getHR << std::endl;
        }
        else
        {
            return getHR;
            //std::cout << "Error: HRESULT from get_ProviderStateIcon is: " << std::hex << getHR << std::endl;
        }

        // Test StorageProviderQuotaUI
        ABI::Windows::Storage::Provider::IStorageProviderQuotaUI* quotaUI = nullptr;
        getHR = status->get_QuotaUI(&quotaUI);

        if (SUCCEEDED(getHR))
        {
            HSTRING quotaUsedLabel = nullptr;
            UINT32 labelLength = 0;
            HRESULT getQuotaHR = quotaUI->get_QuotaUsedLabel(&quotaUsedLabel);
            if (!SUCCEEDED(getQuotaHR))
            {
                currentState.isQuotaAvailable = FALSE;
                return getHR; // quota may not be available, but it is ok
            }
            auto rawQuotaUsed = WindowsGetStringRawBuffer(quotaUsedLabel, &labelLength);
            safeStringCopy(&(currentState.QuotaLabel), MAX_QUOTA_LABEL, (void*)rawQuotaUsed, labelLength);
            //std::wstring quotaUsedLabelString = WindowsGetStringRawBuffer(quotaUsedLabel, &labelLength);
            //std::wcout << L"Current QuotaUsedLabel is: " << quotaUsedLabelString << " . HRESULT from get_QuotaUsedLabel is: " << std::hex << getHR << std::endl;

            uint64_t bytesUsed = 0;
            uint64_t bytesTotal = 0;
            HRESULT getUsedQuota = quotaUI->get_QuotaUsedInBytes(&bytesUsed);
            HRESULT getTotalQuota = quotaUI->get_QuotaTotalInBytes(&bytesTotal);
            currentState.UsedQuota = bytesUsed;
            currentState.TotalQuota = bytesTotal;
            //std::cout << "Current total Quota in bytes is: " << std::dec << bytesTotal << " . HRESULT from get_QuotaTotalInBytes is: " << std::hex << getTotalQuota << std::endl;
            //std::cout << "Current used Quota in bytes is: " << std::dec << bytesUsed << " . HRESULT from get_QuotaUsedInBytes is: " << std::hex << getUsedQuota << std::endl;

            ABI::Windows::Foundation::IReference<struct ABI::Windows::UI::Color>* pColorStruct = nullptr;
            HRESULT getColorHR = quotaUI->get_QuotaUsedColor(&pColorStruct);

            if (SUCCEEDED(getColorHR))
            {
                ABI::Windows::UI::Color quotaColor;
                getColorHR = pColorStruct->get_Value(&quotaColor);
                currentState.IconColorA = quotaColor.A;
                currentState.IconColorB = quotaColor.B;
                currentState.IconColorG = quotaColor.G;
                currentState.IconColorR = quotaColor.R;
                //std::wcout << L"Current used Quota color is: A:" << quotaColor.A << " R:" << quotaColor.R << " G:" << quotaColor.G << " B:" << quotaColor.B << " . HRESULT from quotaUsedColor struct is: " << std::hex << getUsedQuota << std::endl;
            }
        }
    }
    else
    {
        return hr;
    }
    return getHR;

}

HRESULT getInstanceStatus(const std::wstring& syncrootId, OneDriveState& currentState)
{
    ABI::Windows::Storage::Provider::IStorageProviderStatusUISourceFactory* pFactory = nullptr;
    IUnknown* pUnk = nullptr;
    ABI::Windows::Storage::Provider::IStorageProviderStatusUISource* pSource = nullptr;

    HRESULT hr = CoCreateInstance(CLSID_FileCoAuth_StorageProviderStatusUISourceFactory, NULL, CLSCTX_LOCAL_SERVER, __uuidof(IUnknown), (void**)&pUnk);

    //std::cout << "CoCreateInstance: " << std::hex << hr << std::endl;

    if (SUCCEEDED(hr))
    {
        GUID guidIID = __uuidof(ABI::Windows::Storage::Provider::IStorageProviderStatusUISourceFactory);
        hr = pUnk->QueryInterface(guidIID, (void**)&pFactory);
        //std::cout << "QueryInterface: " << std::hex << hr << std::endl;
        if (SUCCEEDED(hr))
        {
            HSTRING hstrProviderId = nullptr;
            WindowsCreateString(syncrootId.c_str(), static_cast<UINT>(syncrootId.size()), &hstrProviderId);

            hr = pFactory->GetStatusUISource(hstrProviderId, &pSource);
            //std::cout << "Result of invoking GetStatusUISource: " << std::hex << hr << std::endl;
        }
    }

    ABI::Windows::Storage::Provider::IStorageProviderStatusUI* status = nullptr;

    if (SUCCEEDED(hr))
    {
        hr = pSource->GetStatusUI(&status);

        //std::cout << "Result from GetStatusUI: " << std::hex << hr << std::endl;
    }

    if (SUCCEEDED(hr))
    {
        // Print default StatusUI
        printStatusUI(pSource, currentState);
    }

    return hr;

}