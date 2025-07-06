# Antennascraper
Antennascraper is a dockerized C# application designed to scrape and download antenna data from the Dutch antenne register.
It also does some pre-processing on the data to get it ready for analysis.
An instance of the application is hosted [here](https://antenna.gerretzen.eu/openapi.json), you can download the SQLite dump directly from [here](https://antenna.gerretzen.eu/dump).
It might take a while to generate the dump so please be patient.

## Features
- Scrape base station and antenna data from the Dutch antenne register.
- Pre-processes the data for easier analysis, including extracting the frequency from the messy string provided by the register as well as linking infrastructure to specific MNOs.
- Exports the cleaned data to a SQLite database for further analysis.

## Development
To build and run the application locally, ensure you have Docker installed and run the following commands:
```bash
cp .env.example .env # Copy the example environment file to .env. 

# Start the database and migrator containers
docker compose up -d db 
docker compose up -d --build migrator # Always run this using the --build flag after changing migration files
```

Now you can run the API project from your command line or IDE of choice, the application will connect to the database container started earlier.
If it does not, check if the information in the connection string in `appsettings.Development.json` matches your configuation in `.env` and ensure environment variable `ASPNETCORE_ENVIRONMENT` is set to `Development`.