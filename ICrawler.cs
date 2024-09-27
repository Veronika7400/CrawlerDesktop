using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mstart.Crawler.Console
{
    internal interface ICrawler
    {
        Task StvoriFilePohrane(string putanjaPohrane);
        Task CitajPodatke(string putanjaTagova, string urlStranice, string putanjaPohrane);
        Task PreuzmiSliku(string tag, string urlStranice, string putanjaPohrane); 
    }
}
