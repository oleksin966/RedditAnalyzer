FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Installing browsers at the build stage (where SDK and pwsh are present)
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN dotnet tool install --global Microsoft.Playwright.CLI && \
    /root/.dotnet/tools/playwright install chromium --with-deps


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
# Copying already downloaded browsers
COPY --from=build /ms-playwright /ms-playwright

RUN apt-get update && apt-get install -y \
    libnss3 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    libasound2t64 \
    libx11-6 \
    libxcb1 \
    libxext6 \
    libexpat1 \
    libfontconfig1 \
    ca-certificates \
    fonts-liberation \
    wget \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

EXPOSE 8080

ENTRYPOINT ["dotnet", "RedditAnalyzer.dll"]