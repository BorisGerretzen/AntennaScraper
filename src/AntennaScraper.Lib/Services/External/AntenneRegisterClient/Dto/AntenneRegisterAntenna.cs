namespace AntennaScraper.Lib.Services.External.AntenneRegisterClient.Dto;

public record AntenneRegisterAntenna(long Id, string SatCode, bool IsDirectional, decimal Height, decimal Direction, decimal TransmissionPower, string Frequency, DateOnly? DateOfCommissioning, DateOnly? DateLastChanged);