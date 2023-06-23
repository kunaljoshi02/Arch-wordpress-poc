---
title: Guide: Support Scaling of App Service and MySQL, along with pipeline for video conversion, for an intranet based on WordPress.
author: GitHub - Steve Craft, Kunal Joshi
ms.author: stcraft; fsi-csu; 
ms.service: #Required; service per approved list. slug assigned by ACOM.
ms.topic: how-to #Required; leave this attribute/value as-is.
ms.date: #Required; 06/30/2022 format.
ms.custom: template-how-to #Required; leave this attribute/value as-is.
---
 

# Guide: Support Scaling of App Service and MySQL, along with pipeline for video conversion, for an intranet based on WordPress.

 

A WordPress intranet environment is deployed with app/data tiers that will optimally serve content and video, and a pipeline subsystem is established to convert video into view-ready formats on the intranet.




<!-- 3. Requirements 
Optional. If you need Requirements, make them your first H2 in a how-to guide. 
Use clear and unambiguous language and use a list format.
-->


## Azure Service Requirements 

- <!-- Requirement 1 --> Existing Azure Tenant and Subscription.
- <!-- Requirement 2 --> Resource Group to contain all WordPress "Primary Environment" components.
     - <!-- Requirement 2a --> Deployed using Azure Marketplace image with scripts from "core infra" section of this repo.
- <!-- Requirement 3 --> Resource Group to contain all WordPress "Replica Environment" components.
     - <!-- Requirement 3a --> Deployed using Azure Marketplace image with scripts from "core infra" section of this repo.
- <!-- Requirement 4 --> Resource Group to contain Scaling Control, Media Pipeline, and configuration components.
     - <!-- Requirement 4a --> App Configuration
     - <!-- Requirement 4b --> App Service Plan
     - <!-- Requirement 4c --> Cosmos DB
     - <!-- Requirement 4d --> Batch Account
     - <!-- Requirement 4e --> Event Grid System Topic
     - <!-- Requirement 4f --> Function App (4)
     - <!-- Requirement 4g --> Log Analytics Workspace
     - <!-- Requirement 4h --> Service Bus Namespace
     - <!-- Requirement 4i --> Five Storage v2 Accounts (blob, not ADLSv2)
- <!-- Requirement 5 --> Application identity for Azure RBAC IAM.

Note: this sample may be modified to use network privacy to keep traffic between the services within a customer-defined boundary.

 

## Technical Requirements

- <!-- Requirement 6 --> Visual Studio 2022 Community Edition or higher *or* dotnet command line tools.


## Steps
 
 1. Deploy each of the Azure components listed above, ensuring that each has Diagnostic logging for all events and metrics delivered to the Log Analytics workspace. Any component that has a Storage Accounbt requirement (eg Functions, Batch) should use the fifth account.
 1. Enter values into the Azure App Configuration service, using the Configuration Explorer in the Azure Portal, or the az cli command using the table of values below.
 1. Create a Service Bus Topic with three subscribers
     - "Activity" with SQL Filter "securityAnalysisState='100' and activityOperationState='0'"
     - "Security" with SQL Filter "securityAnalysisState='0'"
     - "Status" with SQL Filter "taskCompletionStatus > 0.5"
 1. Add an Event Subscription on the Blob Storage Account that will take uploading, publishing to the Service Bus Topic previously created.
     - Filter "Blob Created", EventGridSchema, Subject filtering "/blobservices/default/containers/upload" (if upload is the name of the Container that will receive files from clients).  
     - Delivery Properties
       - workflowCorrelationId: Dynamic, id
       - securityAnalysisState: Static, 0
       - activityOperationState: Static, 0
       - consumerReadyState: Static, 0
 1. Update the Function code for "SecurityAnalysisActor" to point to the Service Bus Trigger for "Security" for Topic Name and Subscription Name.
 1. Update the Function code for "TaskActor" to point to the Service Bus Trigger for "Activity" for Topic Name and Subscription Name.
  1. Update the Function code for "TaskActorStatus" to point to the Service Bus Trigger for "Status" for Topic Name and Subscription Name.
 1. Publish Function SasUriActor code to first Function
1. Publish Function SecurityAnalysis code to second Function
1. Publish Function TaskActor code to third Function
1. Publish Function UtilityWorker code to fourth Function
 1. Enable UtilityWorker Function with created App Id
 1. Enable IAM for all App Service Plans and MySQL Flexible Servers that will be managed with UtilityWorker Function
 
 
 ## Usage
 
 1. Allow a client to make an HTTP GET call to the first Function, which will return a write-only SAS URI for the upload
 1. When the upload completes, SecurityAnalysisWorker Function will receive a message from Service Bus to act, sourced by the Event from the completed upload.
 1. SecurityAnalysisWorker will copy the file from Upload Container to Security Analysis Container, and stub/make an API call to a service for scanning.
 1. SecurityAnalysisWorker will place a new message on the Service Bus Topic with success.
 1. TaskActor will obtain a message/pointer to the safely scanned file, initiate an Azure Batch Pool/Job/Task to perform conversion.
 1. Azure Batch Task will use ffmpeg to make the conversion to a web-delivery-ready format, and write it to the web location.
 1. WordPress developer can connect to the first Azure Function to obtain a list of all web-ready files for linking and styling.
 1. Cosmos DB can be queried ordering by date and grouped by the Workflow Correlation Id, which represents all operations performed on a completed upload.
 
 
 ## Azure Application Configuration Key-Values to Enter
 
 
| Key                       | Value                                                                                                                              |
|-----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|
Activity:Batch:AccountKey                 | Azure Batch Account Key used for secure connection from Azure Function controller.                                   | 
Activity:Batch:AccountName           | Azure Batch Account name used for secure connection from Azure Function controller.                           |
Activity:Batch:AccountUri           | Azure Batch Account endpoint (https://) used for secure connection from Azure Function controller.             |
Activity:Storage:Connection    | Blob Storage Account used for the operational activity, assumed "safe".|
Activity:Storage:ContainerName    |  Blob Storage Container used for the operational activity, assumed "safe".                                                     |
SecurityAnalysis:Storage:Connection   |  Blob Storage Account used for analyzing content that was uploaded.|
| SecurityAnalysis:Storage:ContainerName |  Blob Storage Container used for analyzing content that was uploaded.|
Upload:Storage:Connection | Blob Storage Account that contains the upload from the client.|
| Upload:Storage:ContainerName| Blob Storage Container that contains the upload from the client.
| Util:Scaling:Auth:AppId| Application Identity used by Functions to control scaling on web and data services.
| Util:Scaling:Auth:Key| Application Identity authorizatino key used by Functions to control scaling on web and data services.                                               |
| Util:Scaling:Auth:TenantId| Application Identity Azure Tenant (GUID) used by Functions to control scaling on web and data services.                                               |
| Util:Scaling:Database:Level:1 | Defined level of data tier for normal use (eg "southcentralus/Standard_D4ds_v4/GeneralPurpose")|                                               
| Util:Scaling:Database:Level:2| Defined level of data tier for passive/minimal runtime (eg "southcentralus/Standard_B2ms/Burstable")|
| Util:Scaling:Primary:Database:ResourcePath| Azure Resource Path pointing to the database server (eg "/subscriptions/(guid)/resourceGroups/(RG name))/providers/Microsoft.DBforMySQL/flexibleServers/(server name)"|
| Util:Scaling:Primary:Web:ResourcePath| Azure Resource Path pointing to the App Service Plan (eg "/resource/subscriptions/(guid)/resourceGroups/c12a4/providers/Microsoft.Web/serverFarms/(ASP name)/webHostingPlan") |
| Util:Scaling:Web:Level:1| Defined level of ASP tier for normal use (eg "southcentralus/P1v2/PremiumV2/P1v2/P/1")|
| Util:Scaling:Web:Level:2| Defined level of ASP tier for reduced perf use (eg "southcentralus/B2/Basic/B2/B/1") |
| WebView:Storage:Connection| Storage Account Key to perform read and write operations from Azure Batch Tasks. |
| WebView:Ux:StaticWebsitePrimaryEndpoint| Static website of WebView Storage account.|

 
 
 
 
