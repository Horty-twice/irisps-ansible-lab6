FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NumismaticClub.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

#SSH
RUN apt-get update && apt-get install -y openssh-server iproute2 && rm -rf /var/lib/apt/lists/*
RUN mkdir -p /var/run/sshd
RUN useradd -m -s /bin/bash ansible && echo "ansible ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers

#Порты
EXPOSE 8080
EXPOSE 22

#Приложение
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS="http://+:8080"

#Запуск: sshd + приложение
ENTRYPOINT ["sh", "-c", "/usr/sbin/sshd && exec dotnet NumismaticClub.dll"]