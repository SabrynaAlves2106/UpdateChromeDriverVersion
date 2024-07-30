using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using UpdateChromeDriverVersion.Enum;
using UpdateChromeDriverVersion.Exception;

namespace UpdateChromeDriverVersion;

public class ChromeDriverUpdate
{
    public async Task<string> ExecuteAsync(string ChromeLocalPath = null)
    {        
        //  Passo 1: Captura a versão do chrome instalado na maquina
        ESystem oS = default;
        if (ChromeLocalPath is null)
        {
            oS = GetCurrentOS();
            if (oS == ESystem.Unknown)
            {
                Console.WriteLine("Não foi possivel identificar o sistema Operacional");
                throw new UpdateChromeDriverException("Não foi possivel identificar o sistema Operacional");
            }
        }
        string chromeVersion = GetChromeVersion(ChromeLocalPath,oS);
        Console.WriteLine($"Versão do Google Chrome: {chromeVersion}");

        //  Passo 2: Valida se o arquivo já existe
        string chromeDriverPath = Path.Combine(AppContext.BaseDirectory, chromeVersion.Split('.')[0]);

        if (!Directory.Exists(chromeDriverPath) || Directory.EnumerateFiles(chromeDriverPath) is null)
        {
            // Passo 3: Consultar a versão correspondente do ChromeDriver
            string chromeDriverVersion = await GetChromeDriverVersion(chromeVersion);
            Console.WriteLine($"Versão do ChromeDriver: {chromeDriverVersion}");

            // Passo 4: Baixar o ChromeDriver
            string zipFile = await DownloadChromeDriver(chromeDriverVersion);
            Console.WriteLine("ChromeDriver baixado com sucesso!");

            // Passo 5: Descompactar o arquivo zip e criar pasta
            GeneteteFolder(zipFile, chromeDriverPath);
        }
        else
        {
            Console.WriteLine("Chrome driver já esta instalado em sua versão mais recente");
        }
        // Passo 6: Retorna o caminho da pasta onde se localiza o ChromeDriver
        return chromeDriverPath;
       
    }


    private static string GetChromeVersion(string ChromeLocalPath, ESystem eSystem = default)
    {
        string path = ChromeLocalPath;
        string chromeVersion = "";
        
        if(path is null)
            path = GetDefaultLocalPath(eSystem);

        if (File.Exists(path))
        {
            chromeVersion = FileVersionInfo.GetVersionInfo(path).FileVersion;
        }
        return chromeVersion;
    }
    private static string GetDefaultLocalPath(ESystem eSystem)
    {
        return eSystem switch
        {
            ESystem.Windows => GetWindowsChromePath(),
            ESystem.Mac => "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
            ESystem.Linux => GetLinuxChromePath(),
            _ =>throw new UpdateChromeDriverException("Sistema operacional não suportado")
        };
    }
    static string GetWindowsChromePath()
    {
        string path64 = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        string path32 = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        return File.Exists(path64) ? path64 : File.Exists(path32) ? path32 : throw new UpdateChromeDriverException("Chrome não encontrado");
    }
    static string GetLinuxChromePath()
    {
        string path1 = @"/usr/bin/google-chrome";
        string path2 = @"/opt/google/chrome/google-chrome";
        return File.Exists(path1) ? path1 : File.Exists(path2) ? path2 : throw new UpdateChromeDriverException("Chrome não encontrado");
    }
    static ESystem GetCurrentOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ESystem.Windows;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return ESystem.Mac;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return ESystem.Linux;
        else
            return ESystem.Unknown;
    }
    private static async Task<string> GetChromeDriverVersion(string chromeVersion)
    {
        string majorVersion = chromeVersion.Split('.')[0];
        string url = $"https://googlechromelabs.github.io/chrome-for-testing/LATEST_RELEASE_{majorVersion}";

        using (HttpClient client = new HttpClient())
        {
            return await client.GetStringAsync(url);
        }
    }

    private static async Task<string> DownloadChromeDriver(string version)
    {
        string url = $"https://storage.googleapis.com/chrome-for-testing-public/{version}/win32/chromedriver-win32.zip";
        string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chromedriver.zip");

        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        using (HttpClient client = new HttpClient())
        {
            byte[] data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(destinationPath, data);
            return destinationPath;
        }
    }

    private static void GeneteteFolder(string zipFile, string chromeDriverPath)
    {
        //Extrai todos os itens do Zip
        ZipFile.ExtractToDirectory(zipFile, chromeDriverPath, true);

        // Captura nos diretorios e SubDiretorios todos os arquivos que tenha a extenção .exe
        var files = Directory.GetFiles(chromeDriverPath, "*.exe", SearchOption.AllDirectories);

        // Move o chromedriver para a pasta principal
        foreach (var file in files)
        {
            FileInfo fileInfo = new FileInfo(file);
            File.Move(file, Path.Combine(chromeDriverPath, fileInfo.Name));
        }

        // Exclui pasta e arquivo que não serão utilizados
        foreach (var directory in Directory.GetDirectories(chromeDriverPath))
            Directory.Delete(directory, true);

        // Apaga o arquivo zip que foi baixado
        File.Delete(zipFile);
    }

}
