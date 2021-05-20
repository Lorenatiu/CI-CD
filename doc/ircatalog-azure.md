# irCatalog Service with CI/CD Features - Azure Deployment

irCatalog® is a business rule management tool that provides centralized management of rules to ensure the integrity of business rules, keep everyone working on the latest version of rules, and promote sharing of common rules across customers, processes or applications.

The CI/CD solution requires a number of binaries and configuration parameters to be deployed to the Azure® irCatalog service instance. There are two options for deploying these components:
* Create and configure a new instance of irCatalog app service.
* Update an existing instance of irCatalog app service.

#### Deploying a new instance:

* [Database Deployment](#database-deployment)
* [irCatalog Web App Deployment](#web-app-deployment)
* [Configure CI/CD Catalog Service](#configure-catalog-service-with-cicd)

#### Deploying to an existing instance:

* This option applies if you first [deployed the standard Azure irCatalog App Service](https://github.com/InRule/AzureAppServices).
* [Add CI/CD Artifacts](#add-cicd-artifacts-to-an-existing-catalog-service)
* [Configure CI/CD Catalog Service](#configure-catalog-service-with-cicd)


If you have not done so already, please read the [prerequisites](deployment.md#prerequisites) before you get started.

## Database Deployment

irCatalog supports both Microsoft® SQL Server (which includes Microsoft Azure SQL Databases) and Oracle Database. This section explains how to provision a new Microsoft Azure SQL Database for irCatalog. If you have an existing database, you may skip to the the [Web App Deployment](#web-app-deployment) section.

### Sign in to Microsoft Azure
First, [open a PowerShell prompt](https://docs.microsoft.com/en-us/powershell/scripting/setup/starting-windows-powershell) and use the Azure CLI to [sign in](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) to your Microsoft Azure subscription:
```powershell
az login
```

### Set active subscription
If your Microsoft Azure account has access to multiple subscriptions, you will need to [set your active subscription](https://docs.microsoft.com/en-us/cli/azure/account#az-account-set) to where you create your Azure resources:
```powershell
# Example: az account set --subscription "Contoso Subscription 1"
az account set --subscription SUBSCRIPTION_NAME
```

### Create resource group
Create the resource group (one resource group per environment is typical) that will contain the InRule®-related Azure resources with the [az group create](https://docs.microsoft.com/en-us/cli/azure/group#az-group-create) command:
```powershell
# Example: az group create --name inrule-prod-rg --location eastus
az group create --name RESOURCE_GROUP_NAME --location LOCATION
```

### Create Database Server
Create the [Azure SQL Server](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-logical-servers) with the [az sql server create](https://docs.microsoft.com/en-us/cli/azure/sql/server?view=azure-cli-latest#az-sql-server-create) command:
```powershell
# Example: az sql server create --name contoso-catalog-prod-sql --resource-group inrule-prod-rg --location eastus --admin-user cicdadmin --admin-password password
az sql server create --name SERVER_NAME --resource-group RESOURCE_GROUP_NAME --location LOCATION --admin-user ADMIN_USER_NAME --admin-password ADMIN_USER_PASSWORD
```

### Create Database
Create the [Azure SQL Server Database](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-single-databases-manage) with the [az sql db create](https://docs.microsoft.com/en-us/cli/azure/sql/db?view=azure-cli-latest#az-sql-db-create) command:
```powershell
# Example: az sql db create --name catalog-prod-db --server contoso-catalog-prod-sql --resource-group inrule-prod-rg
az sql db create --name DATABASE_NAME --server SERVER_NAME --resource-group RESOURCE_GROUP_NAME
```

### Allow irCatalog Server Access via Firewall Rule
In order to allow the irCatalog Server access to the database, a firewall rule must be added to allow Azure services access to the Azure SQL Server.

Create a rule in the firewall to allow you to access the newly created database with the [az sql server firewall-rule create](https://docs.microsoft.com/en-us/cli/azure/sql/server/firewall-rule?view=azure-cli-latest#az-sql-server-firewall-rule-create) command:
```powershell
# Example: az sql server firewall-rule create --name AllowAllWindowsAzureIps --server contoso-catalog-prod-sql --resource-group inrule-prod-rg --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
az sql server firewall-rule create --name AllowAllWindowsAzureIps --server SERVER_NAME --resource-group RESOURCE_GROUP_NAME --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```
### Allow Your Local Machine Access via Firewall Rule
In order to run the catalog database install/upgrade application, a firewall rule must be added to allow your local machine access to the Azure SQL Server. One way to find your external IP address would be to use [Google](https://www.google.com/search?q=what+is+my+ip).

Create a rule in the firewall to allow you to access the newly created database with the [az sql server firewall-rule create](https://docs.microsoft.com/en-us/cli/azure/sql/server/firewall-rule?view=azure-cli-latest#az-sql-server-firewall-rule-create) command:
```powershell
# Example: az sql server firewall-rule create --name myLocalMachine --server contoso-catalog-prod-sql --resource-group inrule-prod-rg --start-ip-address 1.2.3.4 --end-ip-address 1.2.3.4
az sql server firewall-rule create --name FIREWALL_RULE_NAME --server SERVER_NAME --resource-group RESOURCE_GROUP_NAME --start-ip-address MY_EXTERNAL_IP --end-ip-address MY_EXTERNAL_IP
```

### Deploy the irCatalog Database
First, [download](https://github.com/InRule/AzureAppServices/releases/latest) the latest irCatalog Database package (`InRule.Catalog.Service.Database.zip`) from GitHub, and unzip into a directory of your choosing.

Update the `appsettings.json` found in the newly unzipped directory with the connection string for your database. Be sure to set a valid user name and password. You can retrieve the connection string with the [az sql db show-connection-string](https://docs.microsoft.com/en-us/cli/azure/sql/db?view=azure-cli-latest#az-sql-db-show-connection-string) command:
```powershell
# Example: az sql db show-connection-string --server contoso-catalog-prod-sql --name catalog-prod-db --client ado.net
az sql db show-connection-string --server SERVER_NAME --name DATABASE_NAME --client ado.net
```

Then run the included executable to deploy the initial irCatalog database schema:
```powershell
.\InRule.Catalog.Service.Database.exe
```

### (Optional) Remove Local Machine Firewall Rule
While not required, the local machine firewall rule that was added earlier may be removed with the [az sql server firewall-rule delete](https://docs.microsoft.com/en-us/cli/azure/sql/server/firewall-rule?view=azure-cli-latest#az-sql-server-firewall-rule-delete) command:
```powershell
# Example: az sql server firewall-rule delete --name myLocalMachine --server contoso-catalog-prod-sql --resource-group inrule-prod-rg
az sql server firewall-rule delete --name FIREWALL_RULE_NAME --server SERVER_NAME --resource-group RESOURCE_GROUP_NAME
```

## Web App Deployment

### Sign in to Azure
First, [open a PowerShell prompt](https://docs.microsoft.com/en-us/powershell/scripting/setup/starting-windows-powershell) and use the Azure CLI to [sign in](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli) to your Azure subscription:
```powershell
az login
```

### Set active subscription
If your Azure account has access to multiple subscriptions, you will need to [set your active subscription](https://docs.microsoft.com/en-us/cli/azure/account#az-account-set) to where you create your Azure resources:
```powershell
# Example: az account set --subscription "Contoso Subscription 1"
az account set --subscription SUBSCRIPTION_NAME
```

### Create resource group
Create the resource group (one resource group per environment is typical) that will contain the InRule-related Azure resources with the [az group create](https://docs.microsoft.com/en-us/cli/azure/group#az-group-create) command:
```powershell
# Example: az group create --name inrule-prod-rg --location eastus
az group create --name RESOURCE_GROUP_NAME --location LOCATION
```

### Create App Service plan
Create the [App Service plan](https://docs.microsoft.com/en-us/azure/app-service/azure-web-sites-web-hosting-plans-in-depth-overview) that will host the InRule-related web apps with the [az appservice plan create](https://docs.microsoft.com/en-us/cli/azure/appservice/plan#az-appservice-plan-create) command:
```powershell
# Example: az appservice plan create --name inrule-prod-sp --resource-group inrule-prod-rg --location eastus
az appservice plan create --name APP_SERVICE_PLAN_NAME --resource-group RESOURCE_GROUP_NAME --location LOCATION
```

### Create Web App
Create the [Azure App Service Web App](https://docs.microsoft.com/en-us/azure/app-service/app-service-web-overview) for the Catalog Service with the [az webapp create](https://docs.microsoft.com/en-us/cli/azure/webapp#az-webapp-create) command:
```powershell
# Example: az webapp create --name contoso-catalog-prod-wa --plan inrule-prod-sp --resource-group inrule-prod-rg
az webapp create --name WEB_APP_NAME --plan APP_SERVICE_PLAN_NAME --resource-group RESOURCE_GROUP_NAME
```

### Deploy package
First, [download](../releases/InRule.Catalog.Service_CICD.zip) the latest irCatalog CI/CD package (`InRule.Catalog.Service_CICD.zip`) from GitHub. Then [deploy the zip file](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-zip) package to the Web App with the [az webapp deployment source](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment/source#az-webapp-deployment-source-config-zip) command:
```powershell
# Example: az webapp deployment source config-zip --name contoso-catalog-prod-wa --resource-group inrule-prod-rg --src InRule.Catalog.Service.zip
az webapp deployment source config-zip --name WEB_APP_NAME --resource-group RESOURCE_GROUP_NAME --src FILE_PATH
```

### Upload valid license file
In order for the irCatalog service to properly function, a valid license file must be uploaded to the web app. The simplest way to upload the license file is via FTP.

First, retrieve the FTP deployment profile (url and credentials) with the [az webapp deployment list-publishing-profiles](https://docs.microsoft.com/en-us/cli/azure/webapp/deployment#az-webapp-deployment-list-publishing-profiles) command and put the values into a variable:
```powershell
# Example: az webapp deployment list-publishing-profiles --name contoso-catalog-prod-wa --resource-group inrule-prod-rg --query "[?contains(publishMethod, 'FTP')].{publishUrl:publishUrl,userName:userName,userPWD:userPWD}[0]" | ConvertFrom-Json -OutVariable creds | Out-Null
az webapp deployment list-publishing-profiles --name WEB_APP_NAME --resource-group RESOURCE_GROUP_NAME --query "[?contains(publishMethod, 'FTP')].{publishUrl:publishUrl,userName:userName,userPWD:userPWD}[0]" | ConvertFrom-Json -OutVariable creds | Out-Null
```

Then, upload the license file using those retrieved values:
```powershell
# Example: $client = New-Object System.Net.WebClient;$client.Credentials = New-Object System.Net.NetworkCredential($creds.userName,$creds.userPWD);$uri = New-Object System.Uri($creds.publishUrl + "/InRuleLicense.xml");$client.UploadFile($uri, "$pwd\InRuleLicense.xml");
$client = New-Object System.Net.WebClient;$client.Credentials = New-Object System.Net.NetworkCredential($creds.userName,$creds.userPWD);$uri = New-Object System.Uri($creds.publishUrl + "/InRuleLicense.xml");$client.UploadFile($uri, "LICENSE_FILE_ABSOLUTE_PATH")
```

### Change the connection string
The irCatalog application now needs to be configured to point to your irCatalog database.
```powershell
# Example: az webapp config appsettings set --name contoso-catalog-prod-wa --resource-group inrule-prod-rg --settings inrule:repository:service:connectionString="Server=tcp:contoso-catalog-prod-sql.database.windows.net,1433;Initial Catalog=catalog-prod-db;Persist Security Info=False;User ID=admin;Password=%14TVpB*g$4b;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";
az webapp config appsettings set --name WEB_APP_NAME --resource-group RESOURCE_GROUP_NAME --settings inrule:repository:service:connectionString="Server=tcp:SERVER_NAME.database.windows.net,1433;Initial Catalog=DATABASE_NAME;Persist Security Info=False;User ID=USER_NAME;Password=USER_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";
```

## Add CICD Artifacts to an Existing Catalog Service

This section applies when deploying only the CI/CD add-on to an existing instance of the irCatalog App Service. The steps to configure the Azure app service with the CI/CD features are:

* Download [InRule.Catalog.Service_CICD.zip](../releases/InRule.Catalog.Service_CICD.zip) and unzip in a folder on the local file system.
* Copy the content of the bin folder to the existing bin folder in App Service Editor. Accept to overwrite files, if prompted.

## Configure Catalog Service with CICD
* Download the starter configuration file [InRule.Catalog.Service_CICD.config.json](../config/InRule.Catalog.Service_CICD.config.json) and save it to the local file system. Edit the values for *AesEncryptDecryptKey* and *ApiKeyAuthentication.ApiKey* to match the values set on the InRule CI/CD service.
* In Azure portal, navigate to the App Service Editor:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn1.png)
* Open the bulk configuration editor, by clicking Advanced Edit, and merge the items in the file downloaded and edited before, then click Save and agree with the action that restarts the app service:
    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn2.png)

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn3.png)
* Restart the app service and confirm that the irCatalog service works properly: browse to the URL in browser, open a rule application in irAuthor.

### Verify using irAuthor®
Using irAuthor you should now be able to connect to your catalog using the url [https://WEB_APP_NAME.azurewebsites.net/service.svc](https://WEB_APP_NAME.azurewebsites.net/service.svc).