using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;
using NLog;
using Microsoft.Extensions.Configuration;

namespace Mstart.Crawler.Console
{
    internal class Crawler : ICrawler
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private IConfiguration konfiguracija;

        public Crawler(IConfiguration konfiguracija)
        {
            this.konfiguracija = konfiguracija;
        }

        /// <summary>
        /// Metoda  koja čita šifre proizvoda iz datoteke i za svaki proizvod pokreće preuzimanje slike 
        /// </summary>
        /// <param name="putanjaProizvoda"> Putanja datoteke s popisom šifri proizvoda</param>
        /// <param name="urlStranice"> Putanja do web stranice</param>
        /// <param name="putanjaPohrane"> Putanja do direktorija u kojemu će se spremati slike</param>
        public async Task CitajPodatke(string putanjaProizvoda, string urlStranice, string putanjaPohrane)
        {
            logger.Info("Preuzimanje slika...");
            using (var citac = new StreamReader(putanjaProizvoda))
            {
                while (true)
                {
                    var linija = citac.ReadLine();
                    if (linija == null)
                    {
                        break;
                    }
                    await PreuzmiSliku(linija, urlStranice, putanjaPohrane);
                }
            }
            logger.Info("Završeno preuzimanje slika!");
        }

        /// <summary>
        /// Metoda koja pristupa web stranici proizvoda, dohvaća sve slike i pohranjuje prvu u direktorij na računalu 
        /// </summary>
        /// <param name="sifraProizvoda"> Šifra proizvoda </param>
        /// <param name="urlStranice"> Putanja do web stranice </param>
        /// <param name="putanjaPohrane"> Putanja do direktorija u kojemu će se spremati slike </param>
        public async Task PreuzmiSliku(string sifraProizvoda, string urlStranice, string putanjaPohrane)
        {

            HtmlWeb web = new HtmlWeb();
            HtmlDocument document = web.Load(urlStranice + sifraProizvoda);
            HtmlNodeCollection kolekcijaSlika = null;
            
            string tag = konfiguracija["imgTag"];
            if (!await ProvjeriPostavku(tag, "imgTag")) return;

            kolekcijaSlika = document.DocumentNode.SelectNodes(tag);
            if (!await ProvjeriKolekcijuSlika(kolekcijaSlika, sifraProizvoda)) return;

            var prvaSlikaProizvoda = DohvatiPrvuSlikuProizvoda(kolekcijaSlika);
            if (prvaSlikaProizvoda == null)
            {
                logger.Warn("Ne postoji slika proizvoda sa šifrom: " + sifraProizvoda);
                return;
            }

            string slikaTag = konfiguracija["slika"];
            if (!await ProvjeriPostavku(slikaTag, "slika")) return;

            var url = await DohvatiUrlSlike(await prvaSlikaProizvoda, urlStranice, slikaTag);
            if (!await ProvjeriPostavku(slikaTag, "urlStranice")) return;

            string ekstenzija = konfiguracija["ekstenzijaPohrane"];
            if (!await ProvjeriPostavku(ekstenzija, "ekstenzijaPohrane")) return;

            var imeSlike = sifraProizvoda + ekstenzija;
            var putanjaSlike = Path.Combine(putanjaPohrane, imeSlike);

            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile(url, putanjaSlike);
                    logger.Info("Preuzeta slika: " + imeSlike);
                }
                catch (Exception ex)
                {
                    logger.Error("Neuspjelo preuzimanje slike: " + imeSlike + " Razlog: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Metoda koja osigurava da postoji direktorij za pohranu slika 
        /// </summary>
        /// <param name="putanjaPohrane"> Putanja do direktorija u kojemu će se spremati slike </param>
        public async Task StvoriFilePohrane(string putanjaPohrane)
        {
            if (!await DatotekaPostoji(putanjaPohrane))
            {
                try
                {
                    Directory.CreateDirectory(putanjaPohrane);
                }
                catch (Exception ex)
                {
                    logger.Error($"Greška prilikom kreiranja direktorija: "+ ex.Message);
                }
            }
        }

        /// <summary>
        /// Metoda za dohvat prve slike proizvoda 
        /// </summary>
        /// <param name="kolekcijaSlika"> kolekcija slika na stranici proizvoda</param>
        /// <returns> Prvu sliku u kolekciji </returns>
        private async Task<HtmlNode> DohvatiPrvuSlikuProizvoda(HtmlNodeCollection kolekcijaSlika)
        {
            return kolekcijaSlika.FirstOrDefault();
        }

        /// <summary>
        /// Metoda za dohvat potpune putanje do slike za preuzimanje 
        /// </summary>
        /// <param name="slika"> Slika koju želimo preuzeti </param>
        /// <param name="urlStranice"> Putanja do web stranice </param>
        /// <returns></returns>
        private async Task<string> DohvatiUrlSlike(HtmlNode slika, string urlStranice, string slikaTag)
        {
            var url = slika.GetAttributeValue(slikaTag, "");
            if (!string.IsNullOrEmpty(url) && !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                url = new Uri(new Uri(urlStranice), url).AbsoluteUri;
            }
            return url;
        }

        /// <summary>
        /// Metoda za provjeru postojanja datoteke s popisom šifri proizvoda
        /// </summary>
        /// <param name="putanja"> Predstavlja putanju datoteke s popisom šifri proizvoda </param>
        /// <returns></returns>
        private async Task<bool> DatotekaPostoji(string putanja)
        {
            try
            {
                return File.Exists(putanja);
            }
            catch (Exception ex)
            {
                logger.Error("Greška prilikom provjere postojanja datoteke. " + ex.Message);
                return false;
            }
        }
        /// <summary>
        /// Provjerava je li postavka dohvaćena iz konfiguracijske datoteke null
        /// </summary>
        /// <param name="postavka"> vrijednost postavke </param>
        /// <param name="nazivPostavke"> Ime postavke koje će se ispisati na ekranu </param>
        /// <returns></returns>
        private async Task<bool> ProvjeriPostavku(string postavka, string nazivPostavke)
        {
            if (postavka == null || postavka == "")
            {
                logger.Error("Nema postavke " + nazivPostavke);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Metoda provjerava postoji li kolekecija slika ili je kolekcija prazna 
        /// </summary>
        /// <param name="kolekcijaSlika"> kolekcija slika na stranici proizvoda </param>
        /// <param name="sifraProizvoda"> šifra željenog proizvoda </param>
        /// <returns></returns>
        private async Task<bool> ProvjeriKolekcijuSlika(HtmlNodeCollection kolekcijaSlika, string sifraProizvoda)
        {
            if (kolekcijaSlika == null || kolekcijaSlika.Count == 0)
            {
                logger.Info("Nema proizvoda sa šifrom: " + sifraProizvoda);
                return false;
            }
            return true;
        }
    }
}