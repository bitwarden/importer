# Bitwarden Importer

The Bitwarden Importer utility can be used to migrate individual vaults from another password management service, such as LastPass, without having to deal with the typical export process requiring CSV files. Just enter your credentails from Bitwarden and the old password management service and you're done!

![image](https://user-images.githubusercontent.com/1190944/220473849-3bb51806-144e-4996-808c-c2c036980afd.png)

## Command line args

You can use command line arguments to pre-populate any of the fields with default values:

- `bitwardenServerUrl=https://bitwarden.company.com`
- `bitwardenApiKeyClientId=user.guid`
- `bitwardenApiKeySecret=myApiKeySecret`
- `bitwardenMasterPassword=my-bitwarden-master-password`
- `bitwardenKeyConnector=1` (1 = checked)
- `lastpassEmail=john.doe@company.com`
- `lastpassMasterPassword=my-lastpass-master-password`
