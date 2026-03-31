namespace POSOpen.Application.Abstractions.Services;

public static class DeviceDiagnosticCode
{
	public const string ScannerUnavailable = "SCANNER_UNAVAILABLE";
	public const string ScannerTimeout = "SCANNER_TIMEOUT";
	public const string ScannerUnresolvedToken = "SCANNER_UNRESOLVED_TOKEN";
	public const string CardReaderUnavailable = "CARD_READER_UNAVAILABLE";
	public const string CardAuthorizationDeclined = "CARD_AUTH_DECLINED";
	public const string CardAuthorizationTimeout = "CARD_AUTH_TIMEOUT";
	public const string CardAuthorizationFaulted = "CARD_AUTH_FAULTED";
	public const string PrinterUnavailable = "PRINTER_UNAVAILABLE";
	public const string PrinterOffline = "PRINTER_OFFLINE";
	public const string PrinterPaperOut = "PRINTER_PAPER_OUT";
	public const string PrinterFaulted = "PRINTER_FAULTED";
}