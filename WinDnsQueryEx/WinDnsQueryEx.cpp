#include <iostream>
#include <string>

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef UNICODE
#define UNICODE
#endif

#include <ws2tcpip.h>
#include <windns.h>

#pragma comment(lib, "dnsapi.lib")
#pragma comment(lib, "ws2_32.lib")

// Callback function
VOID CALLBACK QueryCompletionCallback(
    _In_ PVOID pQueryContext,
    _Inout_ PDNS_QUERY_RESULT pQueryResults)
{
    DNS_STATUS status = pQueryResults->QueryStatus;
    
    if (status == ERROR_SUCCESS)
    {
        PDNS_RECORD pDnsRecord = pQueryResults->pQueryRecords;
        while (pDnsRecord)
        {
            WCHAR buffer[65] = L"\0";

            switch (pDnsRecord->wType)
            {
            case DNS_TYPE_A:
                InetNtop(AF_INET, (PVOID)&pDnsRecord->Data.A.IpAddress, buffer, INET_ADDRSTRLEN);
                std::wcout << L"A Record: " << buffer << std::endl;
                break;

            case DNS_TYPE_AAAA:
                InetNtop(AF_INET, (PVOID)&pDnsRecord->Data.AAAA.Ip6Address, buffer, INET6_ADDRSTRLEN);
                std::wcout << L"AAAA Record: " << buffer << std::endl;
                break;

            case DNS_TYPE_CNAME:
                std::wcout << L"CNAME: " << pDnsRecord->Data.Cname.pNameHost << std::endl;
                break;

            case DNS_TYPE_SRV:
                std::wcout << L"SRV Record: " << pDnsRecord->Data.SRV.pNameTarget
                    << L" Priority: " << pDnsRecord->Data.SRV.wPriority
                    << L" Weight: " << pDnsRecord->Data.SRV.wWeight
                    << L" Port: " << pDnsRecord->Data.SRV.wPort << std::endl;
                break;

            default:
                std::wcout << L"Other Record Type: " << pDnsRecord->wType << std::endl;
                break;
            }

            std::wcout << L"TTL: " << pDnsRecord->dwTtl << L" seconds" << std::endl;
            pDnsRecord = pDnsRecord->pNext;
        }
    }
    else
    {
        std::wcerr << L"DNS query failed with status: " << status << std::endl;
    }

    if (pQueryResults->pQueryRecords)
    {
        DnsRecordListFree(pQueryResults->pQueryRecords, DnsFreeRecordList);
    }

    // Signal the event to indicate completion
    HANDLE hEvent = static_cast<HANDLE>(pQueryContext);
    SetEvent(hEvent);
}

int wmain()
{
    std::wstring hostname = L"_pubsub._any.uswest.pubsub.core.windows.net"; // Replace with the hostname you want to resolve
    DNS_QUERY_REQUEST dnsQueryRequest = {};
    DNS_QUERY_RESULT dnsQueryResult = {};
    DNS_STATUS dnsStatus = 0;
    HANDLE hEvent = NULL;

    // Initialize the DNS_QUERY_REQUEST structure
    dnsQueryRequest.Version = DNS_QUERY_REQUEST_VERSION1;
    dnsQueryRequest.QueryName = hostname.c_str();
    dnsQueryRequest.QueryType = DNS_TYPE_SRV;
    dnsQueryRequest.QueryOptions = DNS_QUERY_STANDARD;
    dnsQueryRequest.pDnsServerList = NULL;
    dnsQueryRequest.InterfaceIndex = 0;
    dnsQueryRequest.pQueryCompletionCallback = QueryCompletionCallback;

    // Create an event for notification
    hEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
    if (!hEvent)
    {
        std::cerr << "Failed to create event." << std::endl;
        return 1;
    }
    dnsQueryRequest.pQueryContext = hEvent;

    // Initialize the DNS_QUERY_RESULT structure
    dnsQueryResult.Version = DNS_QUERY_REQUEST_VERSION1;

    dnsStatus = DnsQueryEx(&dnsQueryRequest, &dnsQueryResult, NULL);
    if (dnsStatus != DNS_REQUEST_PENDING)
    {
        QueryCompletionCallback(hEvent, &dnsQueryResult);
    }

    // Wait for the callback to be called
    WaitForSingleObject(hEvent, INFINITE);
    CloseHandle(hEvent);

    return 0;
}