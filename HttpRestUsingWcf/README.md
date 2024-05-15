# Demonostration of REST Service Using WCF

This is a simple demonstration of REST service using WCF. The service is hosted in a console application.

To generate a self-signed cert, run the following in PowerShell as administrator:

     New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\\LocalMachine\\My"

Then configure the cert using (using the real thumbprint, empty GUID is intentional):

     netsh http add sslcert ipport=0.0.0.0:14080 certhash=E923DC2ECF932B6E341BC43AA281A17AE5479294 certstorename=MY appid={00000000-0000-0000-0000-000000000000}

To cleanup the cert run:

     netsh http delete sslcert ipport=0.0.0.0:14080

using curl to test:

     curl -kv https://localhost:14080/WcfRest/hello/Yao
