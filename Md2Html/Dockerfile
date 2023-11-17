# Versions of the Docker base images
ARG SDK_VERSION=6.0.413
ARG ALPINE_VERSION=alpine3.18
ARG ASPNET_VERSION=6.0.21

# Project name
ARG PROJECT_NAME=Md2HtmlAPI

# -------------------------------------------------
# Build developement image, named 'build-env'
# -------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION}-${ALPINE_VERSION} AS build-env
# To use an arg in multiple stages, each stage must include the ARG instruction
ARG PROJECT_NAME

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY Directory.Build.props ./

# Copy csproj and restore as distinct layers
COPY *.csproj ./


RUN dotnet restore ./${PROJECT_NAME}.csproj
    
# Copy everything else
COPY . ./
# Delete the NuGet.config file, since online will take default Nuget source
RUN rm NuGet.config
# and build
RUN dotnet publish ./${PROJECT_NAME}.csproj -c Release -o out

# -------------------------------------------------
# Build runtime image using result of 'build-env'
# -------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${ASPNET_VERSION}-${ALPINE_VERSION}
# To use an arg in multiple stages, each stage must include the ARG instruction
ARG PROJECT_NAME
ENV PROJECT_NAME_ENV=${PROJECT_NAME}
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

############################

# Create the work dir
# Set the permissions as 'root'
RUN mkdir /home/app/net
# Set permissions to 'app:app' user only
RUN chown -R app:app /home/app/net
WORKDIR /home/app/net

COPY --from=build-env --chown=app:app /app/out .

# Switch to the already existing user (since .Net 8)
USER app

# Expose only port 8080 (since not "root", not allowed to expose 80)
EXPOSE 8080

# Start the application
CMD ["sh", "-c", "dotnet ${PROJECT_NAME_ENV}.dll"]

# For debug only: output and error go respectively to 'output.txt' and 'error.txt'
#CMD dotnet ${PROJECT_NAME}.dll > ./output.txt 2>./error.txt
