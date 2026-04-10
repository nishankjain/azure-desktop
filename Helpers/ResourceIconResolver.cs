namespace AzureDesktop.Helpers;

/// <summary>
/// Maps Azure resource types to their corresponding icon SVG asset paths.
/// </summary>
public static class ResourceIconResolver
{
    private static readonly Dictionary<string, string> TypeToIcon = new(StringComparer.OrdinalIgnoreCase)
    {
        // Compute
        ["Microsoft.Compute/virtualMachines"] = "virtualMachines.svg",
        ["Microsoft.Compute/disks"] = "disks.svg",
        ["Microsoft.Compute/availabilitySets"] = "availabilitySets.svg",

        // Networking
        ["Microsoft.Network/applicationGateways"] = "applicationGateways.svg",
        ["Microsoft.Network/networkInterfaces"] = "networkInterfaces.svg",
        ["Microsoft.Network/publicIPAddresses"] = "publicIPAddresses.svg",
        ["Microsoft.Network/virtualNetworks"] = "virtualNetworks.svg",
        ["Microsoft.Network/networkSecurityGroups"] = "networkSecurityGroups.svg",
        ["Microsoft.Network/loadBalancers"] = "loadBalancers.svg",
        ["Microsoft.Network/azureFirewalls"] = "azureFirewalls.svg",
        ["Microsoft.Network/dnszones"] = "dnsZones.svg",
        ["Microsoft.Network/routeTables"] = "routeTables.svg",
        ["Microsoft.Network/natGateways"] = "natGateways.svg",
        ["Microsoft.Network/bastionHosts"] = "bastionHosts.svg",
        ["Microsoft.Network/expressRouteCircuits"] = "expressRouteCircuits.svg",
        ["Microsoft.Network/frontDoors"] = "frontDoors.svg",
        ["Microsoft.Network/virtualNetworkGateways"] = "virtualNetworkGateways.svg",
        ["Microsoft.Network/ApplicationGatewayWebApplicationFirewallPolicies"] = "wafPolicies.svg",
        ["Microsoft.Network/frontdoorWebApplicationFirewallPolicies"] = "wafPolicies.svg",

        // Web
        ["Microsoft.Web/sites"] = "sites.svg",
        ["Microsoft.Web/serverFarms"] = "serverFarms.svg",

        // Functions
        ["Microsoft.Web/sites/functions"] = "functionApps.svg",

        // Databases
        ["Microsoft.Sql/servers"] = "sqlServers.svg",
        ["Microsoft.Sql/servers/databases"] = "sqlDatabases.svg",
        ["Microsoft.DocumentDB/databaseAccounts"] = "databaseAccounts.svg",
        ["Microsoft.DBforMySQL/servers"] = "mysqlServers.svg",
        ["Microsoft.DBforMySQL/flexibleServers"] = "mysqlServers.svg",
        ["Microsoft.DBforPostgreSQL/servers"] = "postgresqlServers.svg",
        ["Microsoft.DBforPostgreSQL/flexibleServers"] = "postgresqlServers.svg",
        ["Microsoft.Cache/Redis"] = "redis.svg",
        ["Microsoft.Cache/redisEnterprise"] = "redis.svg",

        // Containers
        ["Microsoft.ContainerInstance/containerGroups"] = "containerGroups.svg",
        ["Microsoft.ContainerRegistry/registries"] = "registries.svg",

        // Messaging
        ["Microsoft.ServiceBus/namespaces"] = "serviceBusNamespaces.svg",
        ["Microsoft.EventHub/namespaces"] = "eventHubNamespaces.svg",

        // Integration
        ["Microsoft.Logic/workflows"] = "logicApps.svg",

        // Identity
        ["Microsoft.ManagedIdentity/userAssignedIdentities"] = "managedIdentities.svg",

        // Storage
        ["Microsoft.Storage/storageAccounts"] = "storageAccounts.svg",

        // Security
        ["Microsoft.KeyVault/vaults"] = "vaults.svg",

        // Monitoring
        ["Microsoft.OperationalInsights/workspaces"] = "logAnalyticsWorkspaces.svg",
        ["Microsoft.Insights/components"] = "monitor.svg",
    };

    /// <summary>
    /// Returns just the icon file name for the given resource type.
    /// </summary>
    public static string GetIconFileName(string resourceType)
    {
        return TypeToIcon.GetValueOrDefault(resourceType, "default.svg");
    }

    /// <summary>
    /// Returns the ms-appx:/// URI for the icon matching the given resource type,
    /// or the default icon if no specific match is found.
    /// </summary>
    public static string GetIconPath(string resourceType)
    {
        return $"ms-appx:///Assets/Icons/{GetIconFileName(resourceType)}";
    }
}
