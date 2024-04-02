// Build a DLL: cl DnsQueryWin.cpp /LD /W3 /O2 /Zi
#ifndef UNICODE
#define UNICODE
#endif

#define WIN32_LEAN_AND_MEAN

#include <string>

#include <windows.h>
#include <windns.h>
#include <ws2tcpip.h>

#pragma comment(lib, "dnsapi.lib")
#pragma comment(lib, "ws2_32.lib")

extern "C"
{
    // Callback provided by the client, which will be invoked when the query is finished.
    typedef void (__stdcall *OnQueryFinished)(int, const wchar_t*, void*);

    __declspec(dllexport) int __stdcall QueryDns(
        _In_ const wchar_t* hostName,
        _In_ OnQueryFinished callback,
        _In_ void* context)
    {
        std::wstring str(L"abc: ");

        str += hostName;

        callback(10, str.c_str(), context);

        return 0;
    }
}
