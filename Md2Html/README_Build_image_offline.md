

# Build image offline

## ASP.Net Core

When we try to build the image offline, there is a problem with retrieving the Nuget packages (that cannot be retrieved from `nuget.org`).

### Retrieve the Nuget packages from the development machine (connected to Internet)

> WARNING: remove the following folders, since they may cause problem at build (even if not explicity referenced...)

Execute the following code on the developmenet machine

```bash
FOLDER_OFFLINE_PACKAGES=../packages_for_offline
PACKAGES_ZIP_NAME=packages.zip
# The global-packages folder is where NuGet installs any downloaded package. https://learn.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders
cd ~/.nuget/packages
# Create a copy
mkdir ${FOLDER_OFFLINE_PACKAGES}
cp -R * ${FOLDER_OFFLINE_PACKAGES}
cd ${FOLDER_OFFLINE_PACKAGES}
# Remove those packages that are not used
rm -rf microsoft.visualstudio*
rm -rf microsoft.codeanalysis.razor*
# Create a ZIP file with the package, minus those that may cause problems
zip -r ${PACKAGES_ZIP_NAME} *
echo "Your ZIP file is ready at ${PWD}/${PACKAGES_ZIP_NAME}
```

### Copy the ZIP file to the offline machine

### Modify slightly the Dockerfile

Just after the line `COPY *.csproj ./`, insert the following:

```Dockerfile
# Copy NugetPackages folder since it is referenced by NuGet.config
COPY NuGet.config ./
COPY ./NugetPackages ./NugetPackages
RUN ls -al /app
RUN dotnet restore ./${PROJECT_NAME}.csproj --configfile ./NuGet.config
```