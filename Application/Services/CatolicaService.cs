using PuppeteerSharp;
using System.Globalization;
using WindowsInput;
using WindowsInput.Native;

namespace Application.Services;

public class CatolicaService(StreamWriter logFile, string usuarioCatolica, string senhaCatolica, IBrowser browser, string diretorioTemp)
{
    public StreamWriter LogFile { get; set; } = logFile;
    public string UsuarioCatolica { get; set; } = usuarioCatolica.Trim();
    public string SenhaCatolica { get; set; } = senhaCatolica.Trim();
    public IBrowser Browser { get; set; } = browser;
    public string DiretorioTemp { get; set; } = diretorioTemp;

    public async Task SiteCatolica(IPage page)
    {
        if (String.IsNullOrEmpty(UsuarioCatolica)) throw new SiteNavigationException("O parâmetro 'usuarioCatolica' está vazio");
        else if (String.IsNullOrEmpty(SenhaCatolica)) throw new SiteNavigationException("O parâmetro 'senhaCatolica' está vazio");
        
        StreamWriter log = LogFile;
        int countAbreNav = 0;
        bool isAbriuSite = false;
        while (countAbreNav < 5)
        {
            await page.GoToAsync("https://portal.catolicasc.org.br/FrameHTML/web/app/edu/PortalEducacional/login/", 20000,
            [
                WaitUntilNavigation.Load,
                WaitUntilNavigation.DOMContentLoaded
            ]);

            try
            {
                Thread.Sleep(5000);
                var isAbriuNavegador = await page.XPathAsync("/html/body/div[2]/div[3]/form/div[4]/input");
                if (isAbriuNavegador.Length > 0)
                {
                    isAbriuSite = true;
                    break;
                }
            }
            catch
            {
                Thread.Sleep(5000);
                countAbreNav++;
                continue;
            }
        }
        if (!isAbriuSite) throw new SiteNavigationException("Erro ao abrir o site da Católica SC.");
        
        await log.WriteLineAsync("Abriu o site da Católica SC com sucesso.");
        await page.DeleteCookieAsync();
        await page.SetCacheEnabledAsync(false);

        await page.TypeAsync("#User", UsuarioCatolica);
        await page.TypeAsync("#Pass", SenhaCatolica);

        await page.ClickAsync("body > div.container > div.login-box.animated.fadeInDown > form > div:nth-child(4) > input[type=submit]");

        Thread.Sleep(10000);
        int countErroLogin = 0;
        try
        {
            var erroLogin = await page.XPathAsync("/html/body/table/tbody/tr/td/table/tbody/tr[2]/td[2]/table/tbody/tr[1]/td/table/tbody/tr/td[2]/div/table/tbody/tr/td/span");
            countErroLogin = erroLogin.Length;
        }
        catch{}

        if (countErroLogin > 0) throw new SiteNavigationException("Usuário ou Senha inválidos!");

        Thread.Sleep(15000);
        try
        {
            await page.WaitForSelectorAsync("#edu-widget > span > div.widgetPageContent.page-content.ng-scope > div > div:nth-child(1)", new WaitForSelectorOptions { Timeout = 5000 });
        }
        catch (Exception ex)
        {
            throw new TimeoutException("Não foi possível realizar login no site da Católica.", ex);
        }

        Thread.Sleep(5000);
        await page.ClickAsync("#sidebar-min > ul > li:nth-child(10)");
        Thread.Sleep(10000);

        try
        {
            await page.WaitForSelectorAsync("#btnBoletoMenu", new WaitForSelectorOptions { Timeout = 5000 });
        }
        catch (Exception ex)
        {
            throw new TimeoutException($"Não há nenhum boleto em aberto para o mês de {DateTime.Now.ToString("MMMM", new CultureInfo("pt-BR"))}.", ex);
        }

        await page.ClickAsync("#btnBoletoMenu");
        Thread.Sleep(2000);
        await page.ClickAsync("#edu-widget > span > div.widgetPageContent.page-content.ng-scope > div > div > div > div > div > div.panel-body.ng-scope > div > div.panel-footer.ng-scope > div.btn-group.pull-right.open > ul > li:nth-child(1) > a");
        Thread.Sleep(10000);
        await page.ClickAsync("body > div.modal.fade.ng-isolate-scope.in > div > div > div.modal-body.page-content.ng-scope > div.row.modal-pgto-boletos.ng-scope > div > div:nth-child(2) > button");
        Thread.Sleep(30000);

        var newPageTask = Browser.WaitForTargetAsync(t => t.Type == TargetType.Page);
        await page.ClickAsync("body > div.modal.fade.ng-isolate-scope.in > div > div > div.modal-footer.ng-scope > button:nth-child(2)");
        Thread.Sleep(10000);
        var newTarget = await newPageTask;
        var newPage = await newTarget.PageAsync();
        Thread.Sleep(10000);

        var input = new InputSimulator();

        for (int i = 0; i < 8; i++)
        {
            input.Keyboard.KeyPress(VirtualKeyCode.TAB);
            Thread.Sleep(500);
        }
        input.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        Thread.Sleep(5000);

        input.Keyboard.TextEntry(DiretorioTemp + @$"\FaturaCatolica {DateTime.Now:ddMMyyyy}");
        Thread.Sleep(2000);

        input.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        Thread.Sleep(5000);

        int timeout = 60;
        string caminhoArquivoPdf = await BuscarDownloadPdf(DiretorioTemp, timeout);

        if (File.Exists(caminhoArquivoPdf))
        {
            await log.WriteLineAsync("Arquivo pdf baixado com sucesso! -> " + caminhoArquivoPdf);
            Console.WriteLine(caminhoArquivoPdf);
        }
    }

    public static async Task<string> BuscarDownloadPdf(string diretorioDownload, int timeoutInSeconds)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds));

        try
        {
            string? pdfPath = await WaitForPdfFileAsync(diretorioDownload, cancellationTokenSource.Token);

            if (File.Exists(pdfPath))
                return pdfPath;

            throw new TimeoutException("O download do arquivo PDF não foi concluído dentro do tempo esperado.");
        }
        catch (TaskCanceledException ex)
        {
            throw new TaskCanceledException("O download do arquivo PDF não foi concluído dentro do tempo esperado.", ex);
        }
    }
    private static async Task<string?> WaitForPdfFileAsync(string diretorioDownload, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? pdfPath = GetReadyPdfFile(diretorioDownload);

            if (!string.IsNullOrEmpty(pdfPath))
                return pdfPath;

            await Task.Delay(1000, cancellationToken);
        }

        return null;
    }
    private static string? GetReadyPdfFile(string diretorioDownload)
    {
        var pdfFiles = Directory.GetFiles(diretorioDownload, "*.pdf");

        foreach (var file in pdfFiles)
        {
            if (IsFileReady(file))
                return file;
        }

        return null;
    }
    private static bool IsFileReady(string filePath)
    {
        try
        {
            using FileStream inputStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return inputStream.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}