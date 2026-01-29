# Antennascraper
Antennascraper is a dockerized C# application designed to scrape and download antenna data from the Dutch antenne register.
It also does some pre-processing on the data to get it ready for analysis.
An instance of the application is hosted [here](https://antenna.gerretzen.eu), you can download the SQLite dump directly from [here](https://antenna.gerretzen.eu/dump).
It might take a while to generate the dump so please be patient.

## Features
- Scrape base station and antenna data from the Dutch antenne register.
- Pre-processes the data for easier analysis, including extracting the frequency from the messy string provided by the register as well as linking infrastructure to specific MNOs.
- Exports the cleaned data to a SQLite database for further analysis.

## Maintenance
Most of the data is automatically scraped and processed by the application,
however the spectrum allocation to MNOs is manually maintained as there is no nice API that provides this information.
I copied it from [AntenneKaart](https://antennekaart.nl/page/frequencies), if you find any mistakes please open an issue or a PR to correct it.
- The list of frequency bands can be found in `src/AntennaScraper.Lib/Data/BandData.cs`.
- How each band is allocated to MNOs can be found in `src/AntennaScraper.Lib/Data/CarrierData.cs`.

## Running locally
If you want to run the application locally without doing any development, you can use Docker Compose to set up the entire environment.
Make sure you have Docker installed on your machine, then run the following commands:
```bash
cp .env.example .env # Copy the example environment file to .env. 
docker compose up -d # This will start the application and all required services.
```

Now visit `http://localhost:8080` to access the API. The Postgres database is also available on port `5432`.

### Development
To build and run the application locally, in addition to docker you need the .NET SDK ([Windows / macOS](https://dotnet.microsoft.com/en-us/download), [Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux)).
After it is installed, you can run the following commands to set up the database and migrator containers:
```bash
cp .env.example .env # Copy the example environment file to .env. 

# Start the database and migrator containers
docker compose up -d db 
docker compose up -d --build migrator # Always run this using the --build flag after changing migration files
```

Now you can run the API project from your command line or IDE of choice, the application will connect to the database container started earlier.
If it does not, check if the information in the connection string in `appsettings.Development.json` matches your configuation in `.env` and ensure environment variable `ASPNETCORE_ENVIRONMENT` is set to `Development`.