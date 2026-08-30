# Image de l'API TechMaint.
#
# Construction en deux etapes : le SDK compile, l'image finale ne porte que le
# runtime ASP.NET (~110 Mo contre ~800 Mo pour le SDK). Le VPS heberge sept
# projets sur 4 Go de RAM et 80 Go de disque : la difference compte.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Le csproj est copie seul dans un premier temps : tant que les dependances ne
# changent pas, Docker reutilise le cache de `restore` et une modification de
# code ne relance pas le telechargement des paquets.
COPY GestionInterventionApi.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl sert au healthcheck de la pile compose ; l'image aspnet ne l'a pas.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

# Utilisateur non privilegie : rien ici n'a besoin de root.
RUN useradd --create-home --shell /usr/sbin/nologin techmaint
COPY --from=build --chown=techmaint:techmaint /app .

# Serilog ecrit dans Logs/ a cote de l'application (voir appsettings.json).
RUN mkdir -p /app/Logs && chown techmaint:techmaint /app/Logs

USER techmaint
EXPOSE 8080

# Kestrel derriere Caddy : HTTP clair sur le reseau interne, TLS termine en
# amont. ASPNETCORE_URLS est fixe ici pour ne pas dependre de launchSettings.json,
# qui n'est pas embarque dans l'image de publication.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0

ENTRYPOINT ["dotnet", "GestionInterventionApi.dll"]
