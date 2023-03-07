# Build the macOS `.app`

1. Run release build of the `Importer.csproj`. Build will fail on the codesign verify step, however, the `Bitwarden Importer.app` will still be built and signed properly.
   ```
   dotnet build -f:net7.0-maccatalyst -c:Release
   ```

# Notarize a macOS app

1. Use the included `notarizer.sh` script to notarize your application.
   ```
   ./notarizer.sh --notarize -a "Bitwarden Importer.app" -b com.bitwarden.importer -u <APPLE_ID> -p <APPLE_PASSWORD>
   ```

2. Check the status of the notarization process with Apple by running the check command with `notarizer.sh`. The RequestUUID is available from the response in running the previous command.
   ```
   ./notarizer.sh --check -u <APPLE_ID> -p <APPLE_PASSWORD> -k <REQUEST_UUID>
   ```

3. Once notarization is successful, staple the notarized application.
   ```
   ./notarizer.sh --staple --file "Bitwarden Importer.app"
   ```

# Build the macOS `.zip` artifact

1. Follow steps for building the macOS `.app`.

2. Notarize the `Bitwarden Importer.app` by following the steps for notarizing a macOS app.

3. Zip up the `Bitwarden Importer.app` file for publishing.

# Build the macOS `.pkg` artifact

1. Clean your bin folder and remove any files created from building and notarizing the `.app` previously.

2. Follow steps for building the macOS `.app`.

3. Create a `.pkg` by using the `productbuild` command.
   ```
   productbuild --sign "Developer ID Installer: Bitwarden Inc (LTZ2PFU5D6)" --component "./Bitwarden Importer.app" /Applications "./Bitwarden Importer.pkg"
   ```

4. Notarize the `.pkg` by following the steps for notarizing a macOS app.

# Build the Windows `.msix` artifact

1. Run release build of the `Importer.csproj`.
   ```
   dotnet publish -f net7.0-windows10.0.19041.0 -c Release /p:RuntimeIdentifierOverride=win10-x64
   ```

2. Sign the created `.msix` with `azuresigntool`
   ```
   azuresigntool.exe sign -v -kvu <URL> -kvi <ID> -kvt <TENANT> -kvs <SECRET> -kvc code-signing-certificate-3 -tr http://timestamp.digicert.com .\Importer_1.0.0.0_x64.msix
   ```
