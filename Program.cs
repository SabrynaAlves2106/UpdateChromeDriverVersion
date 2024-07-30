using OpenQA.Selenium.Chrome;
using UpdateChromeDriverVersion;

ChromeDriverUpdate chromeDriverUpdate = new ChromeDriverUpdate();
string pathChromeDriver = await chromeDriverUpdate.ExecuteAsync();

ChromeDriver chromeDriver = new ChromeDriver(pathChromeDriver);