# Bitwarden Importer

The Bitwarden Importer utility can be used to migrate individual vaults from another password management service, such as LastPass, without having to deal with the typical export process requiring CSV files. Just enter your credentails from Bitwarden and the old password management service and you're done!

![Bitwarden Importer Screenshot](https://user-images.githubusercontent.com/1190944/236015514-76f2c282-73c3-442a-95a4-698c929e6ad5.png)

## Command line args

You can use command line arguments to pre-populate any of the fields with default values:

- `bitwardenServerUrl=https://bitwarden.company.com`
- `bitwardenEmail=john.doe@company.com`
- `bitwardenApiKey=1` (1 = checked)
- `bitwardenApiKeyClientId=user.guid`
- `bitwardenApiKeySecret=myApiKeySecret`
- `bitwardenMasterPassword=my-bitwarden-master-password`
- `bitwardenKeyConnector=1` (1 = checked)
- `lastpassEmail=john.doe@company.com`
- `lastpassMasterPassword=my-lastpass-master-password`
- `lastpassSkipShared=1` (1 = checked)

## Special thanks

A special thank you to the [Password Manager Access](https://github.com/detunized/password-manager-access) library that powers this application.
