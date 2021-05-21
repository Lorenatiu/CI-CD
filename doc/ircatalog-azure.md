# irCatalog Service with CI/CD Features - Azure Deployment

irCatalog® is a business rule management tool that provides centralized management of rules to ensure the integrity of business rules, keep everyone working on the latest version of rules, and promote sharing of common rules across customers, processes or applications.

The CI/CD solution requires a number of binaries and configuration parameters to be deployed to the Azure® irCatalog service instance. There are two options for deploying these components:

### Create and configure a new instance of irCatalog app service

* [Database Deployment](ircatalog-azure-db.md)
* [irCatalog Web App Deployment](ircatalog-azure-cicd.md)
* [Configure CI/CD Catalog Service](#configure-catalog-service-with-cicd)

### Update an existing instance of irCatalog app service

* This option applies if you first [deployed the standard Azure irCatalog App Service](https://github.com/InRule/AzureAppServices).
* [Add CI/CD Artifacts](#add-cicd-artifacts-to-an-existing-catalog-service)
* [Configure CI/CD Catalog Service](#configure-catalog-service-with-cicd)

---
## Add CICD Artifacts to an Existing Catalog Service

This section applies when deploying only the CI/CD add-on to an existing instance of the irCatalog App Service. The steps to configure the Azure app service with the CI/CD features are:

* Download [InRule.Catalog.Service_CICD.zip](../releases/InRule.Catalog.Service_CICD.zip) and unzip in a folder on the local file system.
* Copy the content of the bin folder to the existing bin folder in App Service Editor. Accept to overwrite files, if prompted.

---
## Configure Catalog Service with CICD

This section applies to both deployment options: new irCatalog service with CI/CD or existing irCatalog service.  Once either app service was created and the binaries deployed or updated, the configuration must be updated using [Azure portal](https://portal.azure.com): 
* Download the starter configuration file [InRule.Catalog.Service_CICD.config.json](../config/InRule.Catalog.Service_CICD.config.json) and save it to the local file system. Edit the values for *AesEncryptDecryptKey* and *ApiKeyAuthentication.ApiKey* to match the values set on the InRule CI/CD service.
* In Azure portal, navigate to the App Service Editor:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn1.png)
* Open the bulk configuration editor, by clicking Advanced Edit, and merge the items in the file downloaded and edited before.  You must maintain the validity of the JSON array content, following the format in the two files to merge only the new configuration entries:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn2.png)
* Click Save and agree with the action that restarts the app service:

    ![Azure App Service Editor](../images/InRuleCICD_AzureAddOn3.png)
* Restart the app service and confirm that the irCatalog service works properly: browse to the URL in browser, open a rule application in irAuthor.

---
### Verify using irAuthor®
Using irAuthor you should now be able to connect to your catalog using the url [https://WEB_APP_NAME.azurewebsites.net/service.svc](https://WEB_APP_NAME.azurewebsites.net/service.svc).