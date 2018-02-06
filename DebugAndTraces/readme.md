This version is for tracing purposes to verify whether the process is running in elevated privileges mode (Is Elevated: True)

When running, it will create a log file at %temp%\OneDriveLib-yyyy-mm-dd.log

This log will detail the result of each inquiry.

In the example below we see that the process is running in elevated privileges, thus it will not work as per OneDrive design requirement.
When it is running in elevated mode, you will also notice that the icon inquiry returns with error 0x80040154
```
2018-02-06T19:59:07	Information	Is Interactive: True, Is UAC Enabled: True, Is Elevated: True
2018-02-06T19:59:07	Information	Testing Type: Native.IIconError, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T19:59:07	Information	Testing CLSID: {bbacc218-34ea-4666-9d7a-c78f2274a524}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconUpToDate, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T19:59:07	Information	Testing CLSID: {f241c880-6982-4ce5-8cf7-7085ba96da5a}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconReadOnly, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T19:59:07	Information	Testing CLSID: {9aa2f32d-362a-42d9-9328-24a483e2ccc3}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconShared, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T19:59:07	Information	Testing CLSID: {5ab7172c-9c11-405c-8dd5-af20f3606282}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconSharedSync, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T19:59:07	Information	Testing CLSID: {a78ed123-ab77-406b-9962-2a5d9d2f7f30}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconSync, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T19:59:07	Information	Testing CLSID: {a0396a93-dc06-4aef-bee9-95ffccaef20e}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconGrooveUpToDate, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T19:59:07	Information	Testing CLSID: {e768cd3b-bddc-436d-9c13-e1b39ca257b1}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x0
2018-02-06T19:59:07	Information	Testing Type: Native.IIconGrooveSync, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T19:59:07	Information	Testing CLSID: {cd55129a-b1a1-438e-a425-cebc7dc684ee}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x0
2018-02-06T19:59:07	Information	Testing Type: Native.IIconGrooveError, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T19:59:07	Information	Testing CLSID: {8ba85c75-763b-4103-94eb-9470f12fe0f7}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x0
2018-02-06T19:59:07	Information	Testing Type: Native.IIconError, Path: C:\Users\rviana\OneDrive
2018-02-06T19:59:07	Information	Testing CLSID: {bbacc218-34ea-4666-9d7a-c78f2274a524}, Path: C:\Users\rviana\OneDrive, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconUpToDate, Path: C:\Users\rviana\OneDrive
2018-02-06T19:59:07	Information	Testing CLSID: {f241c880-6982-4ce5-8cf7-7085ba96da5a}, Path: C:\Users\rviana\OneDrive, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconReadOnly, Path: C:\Users\rviana\OneDrive
2018-02-06T19:59:07	Information	Testing CLSID: {9aa2f32d-362a-42d9-9328-24a483e2ccc3}, Path: C:\Users\rviana\OneDrive, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconShared, Path: C:\Users\rviana\OneDrive
2018-02-06T19:59:07	Information	Testing CLSID: {5ab7172c-9c11-405c-8dd5-af20f3606282}, Path: C:\Users\rviana\OneDrive, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconSharedSync, Path: C:\Users\rviana\OneDrive
2018-02-06T19:59:07	Information	Testing CLSID: {a78ed123-ab77-406b-9962-2a5d9d2f7f30}, Path: C:\Users\rviana\OneDrive, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconSync, Path: C:\Users\rviana\OneDrive
2018-02-06T19:59:07	Information	Testing CLSID: {a0396a93-dc06-4aef-bee9-95ffccaef20e}, Path: C:\Users\rviana\OneDrive, HR=0x80040154
2018-02-06T19:59:07	Information	Testing Type: Native.IIconGrooveUpToDate, Path: C:\Users\rviana\OneDrive
2018-02-06T19:59:07	Information	Testing CLSID: {e768cd3b-bddc-436d-9c13-e1b39ca257b1}, Path: C:\Users\rviana\OneDrive, HR=0x0
2018-02-06T19:59:07	Information	Testing Type: Native.IIconGrooveSync, Path: C:\Users\rviana\OneDrive
2018-02-06T19:59:07	Information	Testing CLSID: {cd55129a-b1a1-438e-a425-cebc7dc684ee}, Path: C:\Users\rviana\OneDrive, HR=0x0
2018-02-06T19:59:07	Information	Testing Type: Native.IIconGrooveError, Path: C:\Users\rviana\OneDrive
2018-02-06T19:59:07	Information	Testing CLSID: {8ba85c75-763b-4103-94eb-9470f12fe0f7}, Path: C:\Users\rviana\OneDrive, HR=0x0
```

A working scenario looks like this:

```
2018-02-06T20:17:22	Information	Is Interactive: True, Is UAC Enabled: True, Is Elevated: False
2018-02-06T20:17:22	Information	Testing Type: Native.IIconError, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T20:17:22	Information	Testing CLSID: {bbacc218-34ea-4666-9d7a-c78f2274a524}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x0
2018-02-06T20:17:22	Information	Testing Type: Native.IIconUpToDate, Path: C:\Users\rviana\OneDrive - Contoso
2018-02-06T20:17:23	Information	Testing CLSID: {f241c880-6982-4ce5-8cf7-7085ba96da5a}, Path: C:\Users\rviana\OneDrive - Contoso, HR=0x0
2018-02-06T20:17:23	Information	Testing Type: Native.IIconError, Path: C:\Users\rviana\OneDrive
2018-02-06T20:17:23	Information	Testing CLSID: {bbacc218-34ea-4666-9d7a-c78f2274a524}, Path: C:\Users\rviana\OneDrive, HR=0x0
2018-02-06T20:17:23	Information	Testing Type: Native.IIconUpToDate, Path: C:\Users\rviana\OneDrive
2018-02-06T20:17:23	Information	Testing CLSID: {f241c880-6982-4ce5-8cf7-7085ba96da5a}, Path: C:\Users\rviana\OneDrive, HR=0x0

```
