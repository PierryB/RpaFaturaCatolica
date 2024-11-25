using Moq;
using PuppeteerSharp;

namespace Application.Services.Tests;
public class CatolicaServiceTests
{
    private readonly Mock<IPage> _mockPage;
    private readonly Mock<IBrowser> _mockBrowser;
    private readonly StreamWriter _logWriter;
    private readonly MemoryStream _logStream;
    private readonly string _usuarioCatolica;
    private readonly string _senhaCatolica;
    private readonly string _diretorioTemp;

    public CatolicaServiceTests()
    {
        _mockPage = new Mock<IPage>();
        _mockBrowser = new Mock<IBrowser>();
        _logStream = new MemoryStream();
        _logWriter = new StreamWriter(_logStream);
        _usuarioCatolica = "Teste";
        _senhaCatolica = "123";
        _diretorioTemp = $@"C:\temp\PdfCatolicaTeste\{Guid.NewGuid()}";
    }

    [Fact]
    public async Task SiteCatolica_ShouldThrowException_WhenUserIsEmpty()
    {
        var service = new CatolicaService(_logWriter, string.Empty, _senhaCatolica, _mockBrowser.Object, _diretorioTemp);
        var exception = await Assert.ThrowsAsync<SiteNavigationException>(() => service.SiteCatolica(_mockPage.Object));
        Assert.Equal("O parâmetro 'usuarioCatolica' está vazio", exception.Message);
    }

    [Fact]
    public async Task SiteCatolica_ShouldThrowException_WhenPasswordIsEmpty()
    {
        var service = new CatolicaService(_logWriter, _usuarioCatolica, string.Empty, _mockBrowser.Object, _diretorioTemp);
        var exception = await Assert.ThrowsAsync<SiteNavigationException>(() => service.SiteCatolica(_mockPage.Object));
        Assert.Equal("O parâmetro 'senhaCatolica' está vazio", exception.Message);
    }
}
