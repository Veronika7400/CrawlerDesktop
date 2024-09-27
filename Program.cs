using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Mstart.Crawler.Console;
using NLog;

class Program 
{
    private static readonly IConfiguration konfiguracija = UčitajKonfiguraciju();
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Glavna funkcija koja ako postoji datoteka s popisom šifri proizvoda
    /// ispisuje sadržaj datoteke i pokreće preuzimanje slika 
    /// </summary>
    static async Task Main( string[] args)
    {
        string putanjaProizvoda = Path.GetFullPath(konfiguracija["datotekaProizvoda"]);
        string putanjaPohrane = Path.GetFullPath(konfiguracija["datotekaPohrane"]);
        string urlStranice = konfiguracija["urlStranice"]; 
        ICrawler crawler = new Crawler(konfiguracija);
        foreach (string s in args)
        {
            Console.WriteLine(s);
        }
        try
        {
            if (PostojeStranice(putanjaProizvoda, putanjaPohrane, urlStranice) && DatotekaPostoji(putanjaProizvoda))
            {
                IspisiSadrzajDatoteke(putanjaProizvoda);
                await crawler.StvoriFilePohrane(putanjaPohrane);
                await crawler.CitajPodatke(putanjaProizvoda, urlStranice, putanjaPohrane);
            }
            else
            {
                logger.Warn("Datoteka ne postoji ili u konfiguracijskoj datoteci nisu ispravne sve postavke.");
            }
        }
        catch (Exception ex)
        {
            logger.Error("Pogreška: " + ex.Message);
        }
        Console.ReadKey(); 
    }

    /// <summary>
    /// Učitavanje konfiguracije iz AppSettings.json datoteke 
    /// </summary>
    /// <returns></returns>
    private static IConfiguration UčitajKonfiguraciju()
    {
        string putanjaDoAppSettings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "AppSettings.json");

        IConfigurationRoot konfiguracija = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile(putanjaDoAppSettings, optional: false, reloadOnChange: true)
            .Build();

        return konfiguracija;
    }

    /// <summary>
    /// Metoda za ispis sadržaja datoteke s popisom šifri proizvoda
    /// </summary>
    /// <param name="putanja"> Predstavlja putanju datoteke s popisom šifri proizvoda </param>
    private static void IspisiSadrzajDatoteke(string putanja)
    {
        using (var citac = new StreamReader(putanja))
        {
            logger.Info("-------------------------");
            logger.Info("Sadržaj datoteke: ");
            while (true)
            {
                var linija = citac.ReadLine();
                if (linija == null)
                {
                    break;
                }
                logger.Info(linija);
            }
            logger.Info("-------------------------");
        }
    }

    /// <summary>
    /// Metoda za provjeru postojanja datoteke s popisom šifri proizvoda
    /// </summary>
    /// <param name="putanja"> Predstavlja putanju datoteke s popisom šifri proizvoda </param>
    /// <returns></returns>
    private static bool DatotekaPostoji(string putanja)
    {
        try
        {
            return File.Exists(putanja);
        }
        catch (Exception ex)
        {
           logger.Error("Greška prilikom provjere postojanja datoteke. "+ ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Metoda koja provjerava jesu li putanja za pohranu, putanja liste proizvoda i url web stranice validni 
    /// </summary>
    /// <param name="putanjaProizvoda"> Putanja do datoteke koja sadrži listu šifri proizvoda</param>
    /// <param name="putanjaPohrane"> Putanja do datoteke u koju će se pohraniti preuzete slike </param>
    /// <param name="urlStranice"> Link na web stranicu trgovine </param>
    /// <returns></returns>
    private static bool PostojeStranice(string putanjaProizvoda, string putanjaPohrane, string urlStranice)
    {
        return putanjaProizvoda != null && putanjaPohrane != null && urlStranice != null; 
    }
}