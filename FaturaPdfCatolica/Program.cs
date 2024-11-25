using Application.Services;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;

string usuarioCatolica = string.Empty;
string senhaCatolica = string.Empty;
string pastaTemp = string.Empty;

#if RELEASE
    usuarioCatolica = args[0];
    senhaCatolica = args[1];
    pastaTemp = args[2];
#else
    usuarioCatolica = "";
    senhaCatolica = "";
    pastaTemp = $@"";
#endif

Directory.CreateDirectory(pastaTemp);
string msgExecucao = "INÍCIO";
StreamWriter log = new(pastaTemp + @"\log.txt");
StealthPlugin stealth = new();
var puppeteer = new PuppeteerExtra();
puppeteer.Use(stealth);
await new BrowserFetcher().DownloadAsync();

var browser = await puppeteer.LaunchAsync(new LaunchOptions()
{
    DumpIO = true,
    Headless = false,
    Args = ["--start-maximized"]
});

var page = await browser.NewPageAsync();

await page.SetViewportAsync(new ViewPortOptions()
{
    Width = 1024,
    Height = 768,
});

try
{
    await log.WriteLineAsync(msgExecucao);
    await log.WriteLineAsync("------------------------------------------------------------");
    await new CatolicaService(log, usuarioCatolica, senhaCatolica, browser, pastaTemp).SiteCatolica(page);
    msgExecucao = "FIM";
}
catch (Exception ex)
{
    msgExecucao = $"Erro na execução: {ex.Message}";
}
finally
{
    await log.WriteLineAsync("------------------------------------------------------------");
    await log.WriteLineAsync(msgExecucao);
    await log.DisposeAsync();
    await browser.CloseAsync();
}
