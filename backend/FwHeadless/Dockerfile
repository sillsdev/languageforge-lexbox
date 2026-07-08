# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore "FwHeadless/FwHeadless.csproj"

ARG APP_VERSION
LABEL version=$APP_VERSION

RUN --mount=type=cache,target=/root/.nuget/packages dotnet build /p:InformationalVersion=$APP_VERSION "FwHeadless/FwHeadless.csproj" -c Release -o /app/build

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish /p:InformationalVersion=$APP_VERSION "FwHeadless/FwHeadless.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS dotnet8

FROM base AS final
RUN mkdir -p /var/lib/fw-headless /var/www/.local/share && chown -R www-data:www-data /var/lib/fw-headless /var/www/.local/share
RUN apt-get update \
	&& apt-get install --yes --no-install-recommends tini iputils-ping python3 \
	&& rm -rf /var/lib/apt/lists/*
WORKDIR /app
# dotnet 8 runtime is for ChorusMerge
COPY --from=dotnet8 /usr/share/dotnet/host/fxr/ /usr/share/dotnet/host/fxr/
COPY --from=dotnet8 /usr/share/dotnet/shared/Microsoft.NETCore.App/ /usr/share/dotnet/shared/Microsoft.NETCore.App/
COPY --from=publish /app/publish .
# Ensure Chorus and Mercurial exec bits were not stripped by dotnet CLI tools
RUN chmod +x chorusmerge && chmod +x Mercurial/hg && chmod +x Mercurial/chg 2>/dev/null || true
# Fix up mercurial.ini path to fixutf8
RUN sed -i -e 's/fixutf8 = \/FwHeadless/fixutf8 = \/app/' Mercurial/mercurial.ini
COPY Testing/test-template-repo.zip /tmp/test-template-repo.zip
# Smoke-test FixFwData at image build time: FwHeadless runs under dotnet (AspNetCore shared
# framework) but FixFwData is a standalone apphost (Core only). Web publish does not copy the
# extra assemblies FixFwData needs (e.g. Logging.Abstractions); the csproj PublishFixFwData
# target must publish it into the same output. Running it here catches a broken publish early.
RUN test -f FixFwData \
    && chmod +x FixFwData \
    && python3 -c "import zipfile; open('/tmp/smoke.fwdata','wb').write(zipfile.ZipFile('/tmp/test-template-repo.zip').read('kevin-test-01.fwdata'))" \
    && ./FixFwData /tmp/smoke.fwdata; ec=$?; rm -f /tmp/test-template-repo.zip /tmp/smoke.fwdata; \
    if [ $ec -gt 1 ]; then echo "FixFwData smoke test failed with exit $ec"; exit 1; fi
USER www-data:www-data
ENV XDG_DATA_HOME=/var/www/.local/share
ENTRYPOINT ["tini", "--"]
CMD ["dotnet", "FwHeadless.dll"]
