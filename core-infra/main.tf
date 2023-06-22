resource "azurerm_resource_group" "res-0" {
  location = "eastus2"
  name     = "Arch-wordpress-poc"
}
resource "azurerm_mysql_flexible_server" "res-1" {
  delegated_subnet_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet/subnets/archintran-88801f72c2-dbsubnet"
  location            = "eastus2"
  name                = "archintran-d4379aa4b3df497fa017ae84b09f953d-dbserver"
  private_dns_zone_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Network/privateDnsZones/archintran-88801f72c2-privatelink.mysql.database.azure.com"
  resource_group_name = "Arch-wordpress-poc"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  zone = "1"
  depends_on = [
    azurerm_private_dns_zone.res-20,
  ]
}
resource "azurerm_mysql_flexible_database" "res-8" {
  charset             = "utf8mb3"
  collation           = "utf8mb3_general_ci"
  name                = "archintran_d4379aa4b3df497fa017ae84b09f953d_database"
  resource_group_name = "Arch-wordpress-poc"
  server_name         = "archintran-d4379aa4b3df497fa017ae84b09f953d-dbserver"
  depends_on = [
    azurerm_mysql_flexible_server.res-1,
  ]
}
resource "azurerm_mysql_flexible_database" "res-9" {
  charset             = "utf8mb3"
  collation           = "utf8mb3_general_ci"
  name                = "information_schema"
  resource_group_name = "Arch-wordpress-poc"
  server_name         = "archintran-d4379aa4b3df497fa017ae84b09f953d-dbserver"
  depends_on = [
    azurerm_mysql_flexible_server.res-1,
  ]
}
resource "azurerm_mysql_flexible_database" "res-10" {
  charset             = "utf8mb4"
  collation           = "utf8mb4_0900_ai_ci"
  name                = "mysql"
  resource_group_name = "Arch-wordpress-poc"
  server_name         = "archintran-d4379aa4b3df497fa017ae84b09f953d-dbserver"
  depends_on = [
    azurerm_mysql_flexible_server.res-1,
  ]
}
resource "azurerm_mysql_flexible_database" "res-11" {
  charset             = "utf8mb4"
  collation           = "utf8mb4_0900_ai_ci"
  name                = "performance_schema"
  resource_group_name = "Arch-wordpress-poc"
  server_name         = "archintran-d4379aa4b3df497fa017ae84b09f953d-dbserver"
  depends_on = [
    azurerm_mysql_flexible_server.res-1,
  ]
}
resource "azurerm_mysql_flexible_database" "res-12" {
  charset             = "utf8mb4"
  collation           = "utf8mb4_0900_ai_ci"
  name                = "sys"
  resource_group_name = "Arch-wordpress-poc"
  server_name         = "archintran-d4379aa4b3df497fa017ae84b09f953d-dbserver"
  depends_on = [
    azurerm_mysql_flexible_server.res-1,
  ]
}
resource "azurerm_key_vault" "res-14" {
  enabled_for_template_deployment = true
  location                        = "eastus2"
  name                            = "archpockeyv"
  resource_group_name             = "Arch-wordpress-poc"
  sku_name                        = "standard"
  tags = {
    Entity = "Arch"
    Env    = "POC"
  }
  tenant_id = "16b3c013-d300-468d-ac64-7eda0820b6d3"
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_key_vault_certificate" "res-15" {
  key_vault_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.KeyVault/vaults/archpockeyv"
  name         = "archwordpresscert"
  certificate_policy {
    issuer_parameters {
      name = "Self"
    }
    key_properties {
      exportable = true
      key_type   = "RSA"
      reuse_key  = false
    }
    lifetime_action {
      action {
        action_type = "AutoRenew"
      }
      trigger {
        lifetime_percentage = 80
      }
    }
    secret_properties {
      content_type = "application/x-pkcs12"
    }
  }
  depends_on = [
    azurerm_key_vault.res-14,
  ]
}
resource "azurerm_key_vault_certificate" "res-16" {
  key_vault_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.KeyVault/vaults/archpockeyv"
  name         = "xxxxxx-cert"
  certificate_policy {
    issuer_parameters {
      name = "Unknown"
    }
    key_properties {
      exportable = true
      key_type   = "RSA"
      reuse_key  = false
    }
    lifetime_action {
      action {
        action_type = "EmailContacts"
      }
      trigger {
        lifetime_percentage = 80
      }
    }
    secret_properties {
      content_type = "application/x-pkcs12"
    }
  }
  depends_on = [
    azurerm_key_vault.res-14,
  ]
}
resource "azurerm_key_vault_certificate" "res-17" {
  key_vault_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.KeyVault/vaults/archpockeyv"
  name         = "newjoskncert"
  certificate_policy {
    issuer_parameters {
      name = "Self"
    }
    key_properties {
      exportable = true
      key_type   = "RSA"
      reuse_key  = false
    }
    lifetime_action {
      action {
        action_type = "AutoRenew"
      }
      trigger {
        lifetime_percentage = 80
      }
    }
    secret_properties {
      content_type = "application/x-pkcs12"
    }
  }
  depends_on = [
    azurerm_key_vault.res-14,
  ]
}
resource "azurerm_web_application_firewall_policy" "res-18" {
  location            = "eastus2"
  name                = "archpocwafpolicy"
  resource_group_name = "Arch-wordpress-poc"
  managed_rules {
    managed_rule_set {
      version = "3.2"
    }
  }
  policy_settings {
    mode = "Detection"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_application_gateway" "res-19" {
  firewall_policy_id  = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Network/ApplicationGatewayWebApplicationFirewallPolicies/archpocwafpolicy"
  location            = "eastus2"
  name                = "archwordpressgw"
  resource_group_name = "Arch-wordpress-poc"
  tags = {
    Entity = "Arch"
    Env    = "POC"
  }
  autoscale_configuration {
    max_capacity = 10
    min_capacity = 0
  }
  backend_address_pool {
    fqdns = ["archintran229a63044e.blob.core.windows.net"]
    name  = "storagebe"
  }
  backend_address_pool {
    fqdns = ["archintranet.azurewebsites.net"]
    name  = "appservicebe"
  }
  backend_http_settings {
    affinity_cookie_name  = "ApplicationGatewayAffinity"
    cookie_based_affinity = "Enabled"
    name                  = "appservicebesett"
    port                  = 443
    probe_name            = "appservicehealthprobe"
    protocol              = "Https"
    request_timeout       = 20
  }
  backend_http_settings {
    affinity_cookie_name  = "ApplicationGatewayAffinity"
    cookie_based_affinity = "Enabled"
    host_name             = "archintran229a63044e.blob.core.windows.net"
    name                  = "storagebackendse"
    path                  = "/"
    port                  = 443
    probe_name            = "storagebecustom"
    protocol              = "Https"
    request_timeout       = 20
  }
  frontend_ip_configuration {
    name                 = "appGwPublicFrontendIpIPv4"
    public_ip_address_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Network/publicIPAddresses/archwordpresspip2"
  }
  frontend_ip_configuration {
    name                          = "appGwPrivateFrontendIpIPv4"
    private_ip_address_allocation = "Static"
    subnet_id                     = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet/subnets/Archwordpressgwsubnet"
  }
  frontend_port {
    name = "port_1048"
    port = 1048
  }
  frontend_port {
    name = "port_1049"
    port = 1049
  }
  frontend_port {
    name = "port_3443"
    port = 3443
  }
  frontend_port {
    name = "port_443"
    port = 443
  }
  frontend_port {
    name = "port_80"
    port = 80
  }
  gateway_ip_configuration {
    name      = "appGatewayIpConfig"
    subnet_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet/subnets/Archwordpressgwsubnet"
  }
  http_listener {
    frontend_ip_configuration_name = "appGwPublicFrontendIpIPv4"
    frontend_port_name             = "port_443"
    host_names                     = ["archintranet.joshikun.com"]
    name                           = "appservicelistener"
    protocol                       = "Https"
    require_sni                    = true
    ssl_certificate_name           = "xxxxxxx"
  }
  http_listener {
    frontend_ip_configuration_name = "appGwPublicFrontendIpIPv4"
    frontend_port_name             = "port_443"
    host_name                      = "archintranetblob.joshikun.com"
    name                           = "storagelistener"
    protocol                       = "Https"
    require_sni                    = true
    ssl_certificate_name           = "xxxxxxxx"
  }
  probe {
    interval                                  = 30
    name                                      = "storagebecustom"
    path                                      = "/"
    pick_host_name_from_backend_http_settings = true
    protocol                                  = "Https"
    timeout                                   = 30
    unhealthy_threshold                       = 3
    match {
      status_code = ["400"]
    }
  }
  probe {
    host                = "archintranet.joshikun.com"
    interval            = 30
    name                = "appservicehealthprobe"
    path                = "/"
    protocol            = "Https"
    timeout             = 30
    unhealthy_threshold = 3
    match {
      status_code = ["200-399"]
    }
  }
  request_routing_rule {
    backend_address_pool_name  = "appservicebe"
    backend_http_settings_name = "appservicebesett"
    http_listener_name         = "appservicelistener"
    name                       = "appservicerule"
    priority                   = 1
    rule_type                  = "Basic"
  }
  request_routing_rule {
    backend_address_pool_name  = "storagebe"
    backend_http_settings_name = "storagebackendse"
    http_listener_name         = "storagelistener"
    name                       = "storageberule"
    priority                   = 2
    rule_type                  = "Basic"
  }
  sku {
    name = "WAF_v2"
    tier = "WAF_v2"
  }
  ssl_certificate {
    name = "joshikunpfx"
  }
  depends_on = [
    azurerm_web_application_firewall_policy.res-18,
    azurerm_public_ip.res-28,
    # One of azurerm_subnet.res-185,azurerm_subnet_network_security_group_association.res-186 (can't auto-resolve as their ids are identical)
    # One of azurerm_subnet.res-185,azurerm_subnet_network_security_group_association.res-186 (can't auto-resolve as their ids are identical)
  ]
}
resource "azurerm_private_dns_zone" "res-20" {
  name                = "archintran-88801f72c2-privatelink.mysql.database.azure.com"
  resource_group_name = "Arch-wordpress-poc"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_private_dns_zone_virtual_network_link" "res-21" {
  name                  = "archintran-88801f72c2-privatelink.mysql.database.azure.com-vnetlink"
  private_dns_zone_name = "archintran-88801f72c2-privatelink.mysql.database.azure.com"
  resource_group_name   = "Arch-wordpress-poc"
  virtual_network_id    = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet"
  depends_on = [
    azurerm_private_dns_zone.res-20,
  ]
}
resource "azurerm_private_dns_zone" "res-22" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = "Arch-wordpress-poc"
  tags = {
    ENV    = "POC"
    Entity = "Arch"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_private_dns_zone_virtual_network_link" "res-23" {
  name                  = "ezyemrke6agus"
  private_dns_zone_name = "privatelink.blob.core.windows.net"
  resource_group_name   = "Arch-wordpress-poc"
  virtual_network_id    = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet"
  depends_on = [
    azurerm_private_dns_zone.res-22,
  ]
}
resource "azurerm_private_endpoint" "res-24" {
  custom_network_interface_name = "archwordpresspe-nic"
  location                      = "eastus2"
  name                          = "archwordpresspe"
  resource_group_name           = "Arch-wordpress-poc"
  subnet_id                     = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet/subnets/archintranetstoragepesubnet"
  tags = {
    ENV    = "POC"
    Entity = "Arch"
  }
  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = ["/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/privateDnsZones/privatelink.blob.core.windows.net"]
  }
  private_service_connection {
    is_manual_connection           = false
    name                           = "archwordpresspe"
    private_connection_resource_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Storage/storageAccounts/archintran229a63044e"
    subresource_names              = ["blob"]
  }
  depends_on = [
    azurerm_storage_account.res-29,
  ]
}
resource "azurerm_public_ip" "res-26" {
  allocation_method   = "Dynamic"
  location            = "eastus2"
  name                = "archwordpresspip"
  resource_group_name = "Arch-wordpress-poc"
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_public_ip" "res-27" {
  allocation_method   = "Dynamic"
  location            = "eastus2"
  name                = "archwordpresspip1"
  resource_group_name = "Arch-wordpress-poc"
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_public_ip" "res-28" {
  allocation_method   = "Static"
  location            = "eastus2"
  name                = "archwordpresspip2"
  resource_group_name = "Arch-wordpress-poc"
  sku                 = "Standard"
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_storage_account" "res-29" {
  account_replication_type = "LRS"
  account_tier             = "Standard"
  location                 = "eastus2"
  name                     = "archintran229a63044e"
  resource_group_name      = "Arch-wordpress-poc"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_storage_container" "res-31" {
  container_access_type = "blob"
  name                  = "blobarchintran229a63044e"
  storage_account_name  = "archintran229a63044e"
}
resource "azurerm_app_service_certificate" "res-35" {
  location            = "eastus2"
  name                = "joshikun.com-archintranet"
  resource_group_name = "Arch-wordpress-poc"
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_service_plan" "res-36" {
  location            = "eastus2"
  name                = "ASP-Archwordpresspoc-8b3b"
  os_type             = "Linux"
  resource_group_name = "Arch-wordpress-poc"
  sku_name            = "B1"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_linux_web_app" "res-37" {
  app_settings = {
    BLOB_CONTAINER_NAME                   = "blobarchintran229a63044e"
    BLOB_STORAGE_ENABLED                  = "true"
    BLOB_STORAGE_URL                      = "archintran229a63044e.blob.core.windows.net"
    DATABASE_HOST                         = "archintran-d4379aa4b3df497fa017ae84b09f953d-dbserver.mysql.database.azure.com"
    DATABASE_NAME                         = "archintran_d4379aa4b3df497fa017ae84b09f953d_database"
    DATABASE_PASSWORD                     = "3677IP2251M6Q664$"
    DATABASE_USERNAME                     = "nksajsjmkb"
    DOCKER_REGISTRY_SERVER_URL            = "https://mcr.microsoft.com"
    SETUP_PHPMYADMIN                      = "true"
    STORAGE_ACCOUNT_KEY                   = "LUuMzaE1p43q4C1LrtNOpwcqaV7qVZ8Igu/5aDb/20PexV8PStjsM38vtj51AIlcYYtaQxbD8p6W+AStRqKyQg=="
    STORAGE_ACCOUNT_NAME                  = "archintran229a63044e"
    WEBSITES_CONTAINER_START_TIME_LIMIT   = "1800"
    WEBSITES_ENABLE_APP_SERVICE_STORAGE   = "true"
    WORDPRESS_ADMIN_EMAIL                 = "janedo@something.com"
    WORDPRESS_ADMIN_PASSWORD              = "xxxxxx"
    WORDPRESS_ADMIN_USER                  = "xxxxxxxx"
    WORDPRESS_LOCALE_CODE                 = "en_US"
    WORDPRESS_LOCAL_STORAGE_CACHE_ENABLED = "true"
    WORDPRESS_TITLE                       = "WordPress On Azure"
  }
  location            = "eastus2"
  name                = "archintranet"
  resource_group_name = "Arch-wordpress-poc"
  service_plan_id     = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Web/serverfarms/ASP-Archwordpresspoc-8b3b"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  virtual_network_subnet_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/Arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet/subnets/archintran-88801f72c2-appsubnet"
  site_config {
    always_on              = false
    ftps_state             = "FtpsOnly"
    vnet_route_all_enabled = true
  }
  depends_on = [
    azurerm_service_plan.res-36,
  ]
}
resource "azurerm_app_service_custom_hostname_binding" "res-41" {
  app_service_name    = "archintranet"
  hostname            = "*.joshikun.com"
  resource_group_name = "Arch-wordpress-poc"
  depends_on = [
    azurerm_linux_web_app.res-37,
  ]
}
resource "azurerm_app_service_custom_hostname_binding" "res-42" {
  app_service_name    = "archintranet"
  hostname            = "archintranet.azurewebsites.net"
  resource_group_name = "Arch-wordpress-poc"
  depends_on = [
    azurerm_linux_web_app.res-37,
  ]
}
resource "azurerm_network_security_group" "res-166" {
  location            = "eastus2"
  name                = "archintran-f0f2d998eb-vnet-Archwordpressgwsubnet-nsg-eastus2"
  resource_group_name = "arch-wordpress-poc"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_network_security_rule" "res-167" {
  access                      = "Allow"
  description                 = "Allow GatewayManager"
  destination_address_prefix  = "*"
  destination_port_range      = "65200-65535"
  direction                   = "Inbound"
  name                        = "AllowGatewayManager"
  network_security_group_name = "archintran-f0f2d998eb-vnet-Archwordpressgwsubnet-nsg-eastus2"
  priority                    = 2702
  protocol                    = "*"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "GatewayManager"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-166,
  ]
}
resource "azurerm_network_security_rule" "res-168" {
  access                      = "Allow"
  destination_address_prefix  = "*"
  destination_port_range      = "3443"
  direction                   = "Inbound"
  name                        = "AllowTagCustom3443Inbound"
  network_security_group_name = "archintran-f0f2d998eb-vnet-Archwordpressgwsubnet-nsg-eastus2"
  priority                    = 2701
  protocol                    = "*"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "Internet"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-166,
  ]
}
resource "azurerm_network_security_rule" "res-169" {
  access                      = "Allow"
  destination_address_prefix  = "*"
  destination_port_range      = "443"
  direction                   = "Inbound"
  name                        = "AllowTagCustom8080Inbound"
  network_security_group_name = "archintran-f0f2d998eb-vnet-Archwordpressgwsubnet-nsg-eastus2"
  priority                    = 2700
  protocol                    = "Tcp"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "Internet"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-166,
  ]
}
resource "azurerm_network_security_rule" "res-170" {
  access                      = "Allow"
  destination_address_prefix  = "Storage"
  destination_port_range      = "8080"
  direction                   = "Outbound"
  name                        = "AllowTagCustom8080Outbound"
  network_security_group_name = "archintran-f0f2d998eb-vnet-Archwordpressgwsubnet-nsg-eastus2"
  priority                    = 2712
  protocol                    = "*"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "VirtualNetwork"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-166,
  ]
}
resource "azurerm_network_security_group" "res-171" {
  location            = "eastus2"
  name                = "archintran-f0f2d998eb-vnet-archintran-88801f72c2-appsubnet-nsg-eastus2"
  resource_group_name = "arch-wordpress-poc"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_network_security_rule" "res-172" {
  access                      = "Allow"
  description                 = "CSS Governance Security Rule.  Allow Corpnet inbound.  https://aka.ms/casg"
  destination_address_prefix  = "*"
  destination_port_range      = "443"
  direction                   = "Inbound"
  name                        = "AllowCorpnet"
  network_security_group_name = "archintran-f0f2d998eb-vnet-archintran-88801f72c2-appsubnet-nsg-eastus2"
  priority                    = 2700
  protocol                    = "Tcp"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "*"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-171,
  ]
}
resource "azurerm_network_security_rule" "res-173" {
  access                      = "Allow"
  description                 = "CSS Governance Security Rule.  Allow SAW inbound.  https://aka.ms/casg"
  destination_address_prefix  = "*"
  destination_port_range      = "*"
  direction                   = "Inbound"
  name                        = "AllowSAW"
  network_security_group_name = "archintran-f0f2d998eb-vnet-archintran-88801f72c2-appsubnet-nsg-eastus2"
  priority                    = 2701
  protocol                    = "*"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "CorpNetSaw"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-171,
  ]
}
resource "azurerm_network_security_rule" "res-174" {
  access                      = "Allow"
  destination_address_prefix  = "Storage"
  destination_port_range      = "8080"
  direction                   = "Outbound"
  name                        = "AllowTagCustom8080Outbound"
  network_security_group_name = "archintran-f0f2d998eb-vnet-archintran-88801f72c2-appsubnet-nsg-eastus2"
  priority                    = 2711
  protocol                    = "*"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "VirtualNetwork"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-171,
  ]
}
resource "azurerm_network_security_group" "res-175" {
  location            = "eastus2"
  name                = "archintran-f0f2d998eb-vnet-archintran-88801f72c2-dbsubnet-nsg-eastus2"
  resource_group_name = "arch-wordpress-poc"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_network_security_rule" "res-176" {
  access                      = "Allow"
  description                 = "CSS Governance Security Rule.  Allow Corpnet inbound.  https://aka.ms/casg"
  destination_address_prefix  = "*"
  destination_port_range      = "*"
  direction                   = "Inbound"
  name                        = "AllowCorpnet"
  network_security_group_name = "archintran-f0f2d998eb-vnet-archintran-88801f72c2-dbsubnet-nsg-eastus2"
  priority                    = 2700
  protocol                    = "*"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "CorpNetPublic"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-175,
  ]
}
resource "azurerm_network_security_rule" "res-177" {
  access                      = "Allow"
  description                 = "CSS Governance Security Rule.  Allow SAW inbound.  https://aka.ms/casg"
  destination_address_prefix  = "*"
  destination_port_range      = "*"
  direction                   = "Inbound"
  name                        = "AllowSAW"
  network_security_group_name = "archintran-f0f2d998eb-vnet-archintran-88801f72c2-dbsubnet-nsg-eastus2"
  priority                    = 2701
  protocol                    = "*"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "CorpNetSaw"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-175,
  ]
}
resource "azurerm_network_security_group" "res-178" {
  location            = "eastus2"
  name                = "archintran-f0f2d998eb-vnet-archintranetstoragepesubnet-nsg-eastus2"
  resource_group_name = "arch-wordpress-poc"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_network_security_rule" "res-179" {
  access                      = "Allow"
  description                 = "CSS Governance Security Rule.  Allow Corpnet inbound.  https://aka.ms/casg"
  destination_address_prefix  = "*"
  destination_port_range      = "*"
  direction                   = "Inbound"
  name                        = "AllowCorpnet"
  network_security_group_name = "archintran-f0f2d998eb-vnet-archintranetstoragepesubnet-nsg-eastus2"
  priority                    = 2700
  protocol                    = "*"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "CorpNetPublic"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-178,
  ]
}
resource "azurerm_network_security_rule" "res-180" {
  access                      = "Allow"
  description                 = "CSS Governance Security Rule.  Allow SAW inbound.  https://aka.ms/casg"
  destination_address_prefix  = "*"
  destination_port_range      = "*"
  direction                   = "Inbound"
  name                        = "AllowSAW"
  network_security_group_name = "archintran-f0f2d998eb-vnet-archintranetstoragepesubnet-nsg-eastus2"
  priority                    = 2701
  protocol                    = "*"
  resource_group_name         = "arch-wordpress-poc"
  source_address_prefix       = "CorpNetSaw"
  source_port_range           = "*"
  depends_on = [
    azurerm_network_security_group.res-178,
  ]
}
resource "azurerm_private_dns_a_record" "res-181" {
  name                = "archintran-d4379aa4b3df497fa017ae84b09f953d-dbserver"
  records             = ["10.0.1.4"]
  resource_group_name = "arch-wordpress-poc"
  ttl                 = 30
  zone_name           = "archintran-88801f72c2-privatelink.mysql.database.azure.com"
  depends_on = [
    azurerm_private_dns_zone.res-20,
  ]
}
resource "azurerm_virtual_network" "res-184" {
  address_space       = ["10.0.0.0/16"]
  location            = "eastus2"
  name                = "archintran-f0f2d998eb-vnet"
  resource_group_name = "arch-wordpress-poc"
  tags = {
    AppProfile = "Wordpress"
    ENV        = "POC"
    Entity     = "Arch"
  }
  depends_on = [
    azurerm_resource_group.res-0,
  ]
}
resource "azurerm_subnet" "res-185" {
  address_prefixes     = ["10.0.2.0/24"]
  name                 = "Archwordpressgwsubnet"
  resource_group_name  = "arch-wordpress-poc"
  service_endpoints    = ["Microsoft.Storage"]
  virtual_network_name = "archintran-f0f2d998eb-vnet"
  depends_on = [
    azurerm_virtual_network.res-184,
  ]
}
resource "azurerm_subnet_network_security_group_association" "res-186" {
  network_security_group_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/networkSecurityGroups/archintran-f0f2d998eb-vnet-Archwordpressgwsubnet-nsg-eastus2"
  subnet_id                 = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet/subnets/Archwordpressgwsubnet"
  depends_on = [
    azurerm_network_security_group.res-166,
    azurerm_subnet.res-185,
  ]
}
resource "azurerm_subnet" "res-187" {
  address_prefixes     = ["10.0.0.0/24"]
  name                 = "archintran-88801f72c2-appsubnet"
  resource_group_name  = "arch-wordpress-poc"
  service_endpoints    = ["Microsoft.Storage"]
  virtual_network_name = "archintran-f0f2d998eb-vnet"
  delegation {
    name = "dlg-appService"
    service_delegation {
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
      name    = "Microsoft.Web/serverFarms"
    }
  }
  depends_on = [
    azurerm_virtual_network.res-184,
  ]
}
resource "azurerm_subnet_network_security_group_association" "res-188" {
  network_security_group_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/networkSecurityGroups/archintran-f0f2d998eb-vnet-archintran-88801f72c2-appsubnet-nsg-eastus2"
  subnet_id                 = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet/subnets/archintran-88801f72c2-appsubnet"
  depends_on = [
    azurerm_network_security_group.res-171,
    azurerm_subnet.res-187,
  ]
}
resource "azurerm_subnet" "res-189" {
  address_prefixes     = ["10.0.1.0/24"]
  name                 = "archintran-88801f72c2-dbsubnet"
  resource_group_name  = "arch-wordpress-poc"
  virtual_network_name = "archintran-f0f2d998eb-vnet"
  delegation {
    name = "dlg-appService"
    service_delegation {
      actions = ["Microsoft.Network/virtualNetworks/subnets/join/action"]
      name    = "Microsoft.DBforMySQL/flexibleServers"
    }
  }
  depends_on = [
    azurerm_virtual_network.res-184,
  ]
}
resource "azurerm_subnet_network_security_group_association" "res-190" {
  network_security_group_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/networkSecurityGroups/archintran-f0f2d998eb-vnet-archintran-88801f72c2-dbsubnet-nsg-eastus2"
  subnet_id                 = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet/subnets/archintran-88801f72c2-dbsubnet"
  depends_on = [
    azurerm_network_security_group.res-175,
    azurerm_subnet.res-189,
  ]
}
resource "azurerm_subnet" "res-191" {
  address_prefixes     = ["10.0.3.0/24"]
  name                 = "archintranetstoragepesubnet"
  resource_group_name  = "arch-wordpress-poc"
  virtual_network_name = "archintran-f0f2d998eb-vnet"
  depends_on = [
    azurerm_virtual_network.res-184,
  ]
}
resource "azurerm_subnet_network_security_group_association" "res-192" {
  network_security_group_id = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/networkSecurityGroups/archintran-f0f2d998eb-vnet-archintranetstoragepesubnet-nsg-eastus2"
  subnet_id                 = "/subscriptions/c3df6b60-25b3-4b7c-a9a5-ccf154f68963/resourceGroups/arch-wordpress-poc/providers/Microsoft.Network/virtualNetworks/archintran-f0f2d998eb-vnet/subnets/archintranetstoragepesubnet"
  depends_on = [
    azurerm_network_security_group.res-178,
    azurerm_subnet.res-191,
  ]
}
