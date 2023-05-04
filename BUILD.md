# Build the macOS `.app` and `.pkg`

1. Run release build of the `Importer.csproj`.
   ```
   dotnet build -f:net7.0-maccatalyst -c:Release
   ```
   
   Results (`.app` and `.pkg`) will be in the `./Importer/bin/Release/net7.0-maccatalyst/` directory.

# Notarize a macOS app

1. Use the included `notarizer.sh` script to notarize your application.
   ```
   ./notarizer.sh --notarize -a "./Importer/bin/Release/net7.0-maccatalyst/Bitwarden Importer.app" -b com.bitwarden.importer -u $APPLE_ID_USERNAME -p $APPLE_ID_PASSWORD -v LTZ2PFU5D6
   ```

2. Check the status of the notarization process with Apple by running the check command with `notarizer.sh`. The RequestUUID is available from the response in running the previous command.
   ```
   ./notarizer.sh --check -u $APPLE_ID_USERNAME -p $APPLE_ID_PASSWORD -k <REQUEST_UUID>
   ```

3. Once notarization is successful, staple the notarized application.
   ```
   ./notarizer.sh --staple --file "./Importer/bin/Release/net7.0-maccatalyst/Bitwarden Importer.app"
   ```

# Build the macOS `.zip` artifact

1. Follow steps for building the macOS `.app`.

2. Notarize the `Bitwarden Importer.app` by following the steps for notarizing a macOS app.

3. Zip up the `Bitwarden Importer.app` file for publishing.

# Build the macOS `.pkg` artifact

1. Follow steps for building the macOS `.pkg`.

2. Notarize the `.pkg` by following the steps for notarizing a macOS app.

# Build the Windows `.msix` artifact

1. Run release build of the `Importer.csproj`.
   ```
   dotnet publish -f net7.0-windows10.0.19041.0 -c Release /p:RuntimeIdentifierOverride=win10-x64
   ```

2. Sign the created `.msix` with `azuresigntool`
   ```
   azuresigntool.exe sign -v -kvu <URL> -kvi <ID> -kvt <TENANT> -kvs <SECRET> -kvc code-signing-certificate-3 -tr http://timestamp.digicert.com .\Importer_1.0.0.0_x64.msix
   ```
