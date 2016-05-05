using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game
{
    /// <summary>
    /// Všeobecně použitelné utilitky pro okno konzole.
    /// </summary>
    static class ConsoleStuffs
    {
        /// <summary>
        /// Nastavení konzolového okna,  musí být volána  před jakýmkoliv výpisem na obrazovku.
        /// </summary>
        public static void SetUpConsole()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.SetWindowSize(90, 30);
            Console.BufferHeight = 30;
            Console.BufferWidth = 90;
            Console.Title = "Linux story";
        }

        /// <summary>
        /// Funce pro čtení klávesy od uživatele. Čeká na stisk.
        /// </summary>
        /// <returns>Stisknutá klávesa datového typu ConsoleKeyInfo.</returns>
        public static ConsoleKeyInfo ReadKey()
        {
            Console.SetCursorPosition(0, 0);
            return Console.ReadKey(true);
        }

        /// <summary>
        /// Vykreslí rámeček okolo celého okna. :)
        /// </summary>
        public static void DrawFrame()
        {
            Console.SetCursorPosition(1, 0);
            for (int i = 1; i < 89; i++)
            {
                Console.Write("#");
            }
            Console.SetCursorPosition(1, 29);
            for (int i = 1; i < 89; i++)
            {
                Console.Write("#");
            }
            for (int i = 1; i < 29; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("#");
            }
            for (int i = 1; i < 29; i++)
            {
                Console.SetCursorPosition(89, i);
                Console.Write("#");
            }
        }

        /// <summary>
        /// Tisk textu do konzole na přesně zadanou pozici.
        /// </summary>
        /// <param name="TextForPrint">Text který bude tisknut. Musí se do konzole vejít.</param>
        /// <param name="PositionY">Sloupec na který se umístí první písmeno.</param>
        /// <param name="PositionX">Řádek na který se umístí první písmeno.</param>
        public static void TextPrint(string TextForPrint, int PositionY, int PositionX)
        {
            //ošetření proti tisku ničeho
            if (TextForPrint != null)
            {
                //ošetření proti tisku mimo povolenou oblast
                if ((TextForPrint.Length + PositionX < 90) && (PositionY < 30) && (PositionX > 0) && (PositionY > 0))
                {
                    Console.SetCursorPosition(PositionX, PositionY);
                    Console.Write(TextForPrint);
                }
            }
        }

        /// <summary>
        /// Tisk textu do konzole na přesně zadanou pozici vybranou barvou.
        /// </summary>
        /// <param name="TextForPrint">Text který bude tisknut. Musí se do konzole vejít.</param>
        /// <param name="PositionY">Sloupec na který se umístí první písmeno.</param>
        /// <param name="PositionX">Řádek na který se umístí první písmeno.</param>
        public static void TextPrint(string TextForPrint, int PositionY, int PositionX, ConsoleColor Color)
        {
            //ošetření proti tisku ničeho
            if (TextForPrint != null)
            {
                //ošetření proti tisku mimo povolenou oblast
                if ((TextForPrint.Length + PositionX < 90) && (PositionY < 30) && (PositionX > 0) && (PositionY > 0))
                {
                    Console.ForegroundColor = Color;
                    Console.SetCursorPosition(PositionX, PositionY);
                    Console.Write(TextForPrint);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }

        /// <summary>
        /// Tisk textu do konzole na přesně zadanou pozici.
        /// </summary>
        /// <param name="TextForPrint">Text který bude tisknut. Musí se do konzole vejít.</param>
        /// <param name="column">Sloupec na který se umístí první písmeno.</param>
        /// <param name="PositionY">Řádek na který se umístí první písmeno.</param>
        public static void TextPrint(char TextForPrint, int PositionY, int PositionX)
        {
            //ošetření proti tisku mimo povolenou oblast
            if ((1 + PositionX < 90) && (PositionY < 30) && (PositionX > 0) && (PositionY > 0))
            {
                Console.SetCursorPosition(PositionX, PositionY);
                Console.Write(TextForPrint);
            }
        }

        /// <summary>
        /// Tisk textu do konzole na přesně zadanou pozici vybranou barvou.
        /// </summary>
        /// <param name="TextForPrint">Text který bude tisknut. Musí se do konzole vejít.</param>
        /// <param name="column">Sloupec na který se umístí první písmeno.</param>
        /// <param name="PositionY">Řádek na který se umístí první písmeno.</param>
        /// <param name="PositionX">Sloupec na který se umístí text.</param>
        /// <param name="Color">Barva kterou se text vytiskne.</param>
        public static void TextPrint(char TextForPrint, int PositionY, int PositionX, ConsoleColor Color)
        {
            //ošetření proti tisku mimo povolenou oblast
            if ((1 + PositionX < 90) && (PositionY < 30) && (PositionX > 0) && (PositionY > 0))
            {
                Console.ForegroundColor = Color;
                Console.SetCursorPosition(PositionX, PositionY);
                Console.Write(TextForPrint);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /// <summary>
        /// Metoda vytiskne krátké rozloučení na ukončovací obrazovku.
        /// </summary>
        public static void DrawEndScreen()
        {
            Console.Clear();
            DrawFrame();
            TextPrint("So long...", 5, 20);
            TextPrint("and thank's for all the fish!", 6, 30);
            TextPrint("Pro ukončení stiskni enter...", 28, 2);
            Console.SetCursorPosition(0, 0);
            Console.ReadLine();
        }

        /// <summary>
        /// Tahle věc je tady pro generování náhodných čísel.
        /// </summary>
        private static Random generator = new Random();

        /// <summary>
        /// Metoda vrací pseudo náhodné číslo v zadaném rozmezí.
        /// </summary>
        /// <param name="min">Minimální generované číslo.</param>
        /// <param name="max">Maximální generované číslo.</param>
        /// <returns>Psoudonáhodné číslo.</returns>
        public static int GetRandomNumber(int min, int max)
        {
            return generator.Next(min, max + 1);
        }

        /// <summary>
        /// Vypíše parte hráče.
        /// </summary>
        /// <param name="player">Objekt hráče ze kterého si vezme důležité věci.</param>
        public static void DrawDiedScreen(Player player)
        {
            Console.Clear();
            DrawFrame();
            TextPrint("Ve věku nedožitého " + player.Level + " levelu,", 5, 5);
            TextPrint("zesnul náš drahocený " + player.Name + ".", 6, 5);

            TextPrint("Kéž je mu země lehká...", 8, 5);

            TextPrint("Pro ukončení stiskni enter...", 28, 2);
            Console.SetCursorPosition(0, 0);
            Console.ReadLine();
        }

        /// <summary>
        /// Vypíše výherní obrazovku.
        /// </summary>
        /// <param name="player">Objekt hráče ze kterého si vezme důležité věci.</param>
        public static void DrawWinScreen(Player player)
        {
            Console.Clear();
            DrawFrame();
            TextPrint("Gratuluji " + player.Name + " !", 5, 5);

            TextPrint("Dokázal jsi o čem mnozí pochybovali!", 8, 5);
            TextPrint("Zachránil jsi svět před zlem jménem Windows,", 9, 5);
            TextPrint("jsi proto náš veliký hrdina!", 10, 5);
            TextPrint("Jsi požehnán magickou silou GNU!", 12, 5);
            TextPrint("Teď jsi spasiteli světů, a spas i svůj, reálný svět!", 14, 5);

            TextPrint("Pro ukončení stiskni enter...", 28, 2);
            Console.SetCursorPosition(0, 0);
            Console.ReadLine();
        }
    }

    /// <summary>
    /// Kecající okénko v okně hry.
    /// </summary>
    static class Messenger
    {

        /// <summary>
        /// Udržuje aktuálně zobrazený řádek
        /// </summary>
        static private string displayedText = "";

        /// <summary>
        /// Vypíše rámeček na obrazovku. Rámeček pod okénkem.
        /// </summary>
        static private void DrawFrame()
        {
            for (int i = 1; i < 89; i++)
            {
                ConsoleStuffs.TextPrint('#', 2, i);
            }
        }

        /// <summary>
        /// Aktualizuje text zobrazovaný messengerem. 
        /// </summary>
        /// <param name="Text">Zobrazovaný text, musí být kratší než 95 znaků.</param>
        static public void Update(string Text)
        {
            if (Text.Length <= 95) displayedText = Text;
        }

        /// <summary>
        /// Aktualizace výstupu messengera.
        /// </summary>
        static public void Refresh()
        {
            DrawFrame();
            ConsoleStuffs.TextPrint("                                                                                       ", 1, 2);
            ConsoleStuffs.TextPrint(displayedText, 1, 2);
        }

        /// <summary>
        /// Jen  malé zpřehlednění kódu. Smaže uloženou zprávu.
        /// </summary>
        static public void Clean()
        {
            displayedText = "";
        }

        /// <summary>
        /// Přidání textu do zásobníku, přidá zadaný text k už existujícímu.
        /// </summary>
        /// <param name="Text">String který se přidá na konec řetězce.</param>
        static public void Append(string Text)
        {
            displayedText += " " + Text;
        }

        /// <summary>
        /// Vrátí aktuálně držený text.
        /// </summary>
        /// <returns>Text který je zrovna v messengeru uložen.</returns>
        static public string GetActual()
        {
            return displayedText;
        }
    }

    /// <summary>
    /// Reprezentuje jednu položku v tabulce skóre.
    /// </summary>
    public class ScoreItem
    {
        /// <summary>
        /// Počet expů nasbíraných hráčem.
        /// </summary>
        public int Exp;

        /// <summary>
        /// Dosažený level.
        /// </summary>
        public int Lvl;

        /// <summary>
        /// Jméno hráče.
        /// </summary>
        public string Name;

        /// <summary>
        /// Síla tuxe.
        /// </summary>
        public int TuxPower;

        /// <summary>
        /// Nainstalováno serverů.
        /// </summary>
        public int InstalledServers;

        /// <summary>
        /// Datum vytvoření.
        /// </summary>
        public DateTime Date;

    }

}
