# Arch-wordpress-poc
This repo contains Github code to deploy the infrastructure needed to run Wordpress on Azure managed offering from Microsoft using the Storage Account as a CDN. Both of these are running behind an Azure Application Gateway WAF V2 SKU and deployed in US East2 region.

Following are the main resources are included in the code repo:
1. Word Press on Azure App Service - Premium SKU
     a. App Service Plan P1V3
     b. My SQL Database - GP
3. Azure Storage Account - LRS
4. A New VNET and related Subnets
5. Azure Application Gateway WAF V2 SKU
6. Public IP address for the Frontend IP for the App Gateway
7. Listeners, Backend targets and routing rules to route traffic
