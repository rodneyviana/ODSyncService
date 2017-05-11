// ShellShim.h

#pragma once
#include <atlbase.h>
#include <Shlobj.h>

using namespace System;

namespace ShellShim {
	const MIDL_INTERFACE("F241C880-6982-4CE5-8CF7-7085BA96DA5A") IguidUptoDate;

	const MIDL_INTERFACE("8BA85C75-763B-4103-94EB-9470F12FE0F7") ISkydriverPro;
	CLSID CLSID_UoToDate;
	public ref class Class1
	{
		::CLSIDFromString(L"F241C880-6982-4CE5-8CF7-7085BA96DA5A", &CLSID_UoToDate);
		// TODO: Add your methods for this class here.
	};

#pragma unmanaged


}
