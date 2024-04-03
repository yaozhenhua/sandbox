// Build a DLL: cl DnsQueryWin.cpp /EHsc /LD /W3 /O2 /Zi
#ifndef UNICODE
#define UNICODE
#endif

#define WIN32_LEAN_AND_MEAN

#include <iostream>
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

    // Information saved in the query start, and will be used in the callback.
    struct QueryContext
    {
        OnQueryFinished     Callback;
        void*               Context;
        DNS_QUERY_REQUEST   QueryRequest;
        DNS_QUERY_RESULT    QueryResult;
    };

    // Callback to DnsQueryEx internally.
    void CALLBACK InternalQueryCompletionCallback(
        _In_ PVOID pQueryContext,
        _Inout_ PDNS_QUERY_RESULT pQueryResults)
    {
        DNS_STATUS status = pQueryResults->QueryStatus;

        QueryContext* queryContext = static_cast<QueryContext*>(pQueryContext);

        // Query result contains all resolved addresses and TTL, separated by "=". Different records are delimited
        // by semicolon.
        std::wstring resolvedAddresses;
        
        if (status == ERROR_SUCCESS)
        {
            // Preserve the buffer to avoid excessive reallocation.
            resolvedAddresses.reserve(1024);

            PDNS_RECORD pDnsRecord = pQueryResults->pQueryRecords;
            while (pDnsRecord)
            {
                // Maximum length of IPv6 string.
                WCHAR buffer[65] = L"\0";

                if (resolvedAddresses.size())
                {
                    resolvedAddresses += L";";
                }

                switch (pDnsRecord->wType)
                {
                case DNS_TYPE_A:
                    InetNtop(AF_INET, (PVOID)&pDnsRecord->Data.A.IpAddress, buffer, INET_ADDRSTRLEN);
                    // std::wcout << L"A Record: " << buffer << std::endl;
                    resolvedAddresses += buffer;

                    break;

                case DNS_TYPE_AAAA:
                    InetNtop(AF_INET6, (PVOID)&pDnsRecord->Data.AAAA.Ip6Address, buffer, INET6_ADDRSTRLEN);
                    // std::wcout << L"AAAA Record: " << buffer << std::endl;
                    resolvedAddresses += buffer;

                    break;

                case DNS_TYPE_CNAME:
                    // std::wcout << L"CNAME: " << pDnsRecord->Data.Cname.pNameHost << std::endl;
                    resolvedAddresses += pDnsRecord->Data.Cname.pNameHost;

                    break;

                case DNS_TYPE_SRV:
                    // std::wcout << L"SRV Record: " << pDnsRecord->Data.SRV.pNameTarget
                    //     << L" Priority: " << pDnsRecord->Data.SRV.wPriority
                    //     << L" Weight: " << pDnsRecord->Data.SRV.wWeight
                    //     << L" Port: " << pDnsRecord->Data.SRV.wPort << std::endl;
                    resolvedAddresses += pDnsRecord->Data.SRV.pNameTarget;

                    break;

                default:
                    // std::wcout << L"Other Record Type: " << pDnsRecord->wType << std::endl;
                    break;
                }

                // std::wcout << L"TTL: " << pDnsRecord->dwTtl << L" seconds" << std::endl;
                resolvedAddresses += L"=";
                resolvedAddresses += std::to_wstring(pDnsRecord->dwTtl);

                pDnsRecord = pDnsRecord->pNext;
            }
        }
        // else
        // {
        //     std::wcerr << L"DNS query failed with status: " << status << std::endl;
        // }

        // Send the query result and status to the managed code.
        queryContext->Callback(status, resolvedAddresses.c_str(), queryContext->Context);

        if (pQueryResults->pQueryRecords)
        {
            DnsRecordListFree(pQueryResults->pQueryRecords, DnsFreeRecordList);
        }

        delete queryContext;

        // std::wcout << L"returned " << resolvedAddresses << L"." << std::endl;
    }

    __declspec(dllexport) int __stdcall QueryDns(
        _In_ PCWSTR hostName,
        _In_ WORD wType,
        _In_ OnQueryFinished callback,
        _In_ void* context)
    {
        DNS_STATUS dnsStatus = 0;
        QueryContext* queryContext = new QueryContext();
        ZeroMemory(queryContext, sizeof(QueryContext));

        queryContext->Callback = callback;
        queryContext->Context = context;

        // Initialize the DNS_QUERY_REQUEST structure
        queryContext->QueryRequest.Version = DNS_QUERY_REQUEST_VERSION1;
        queryContext->QueryRequest.QueryName = hostName;
        queryContext->QueryRequest.QueryType = wType;
        queryContext->QueryRequest.QueryOptions = DNS_QUERY_STANDARD;
        queryContext->QueryRequest.pDnsServerList = NULL;
        queryContext->QueryRequest.InterfaceIndex = 0;
        queryContext->QueryRequest.pQueryCompletionCallback = InternalQueryCompletionCallback;
        queryContext->QueryRequest.pQueryContext = queryContext;

        // Initialize the DNS_QUERY_RESULT structure
        queryContext->QueryResult.Version = DNS_QUERY_REQUEST_VERSION1;

        dnsStatus = DnsQueryEx(&(queryContext->QueryRequest), &(queryContext->QueryResult), NULL);
        if (dnsStatus != DNS_REQUEST_PENDING)
        {
            InternalQueryCompletionCallback(queryContext, &(queryContext->QueryResult));
        }

        return 0;
    }
}
