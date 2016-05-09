/*
    Linux Story. Simple RPG game.
    Copyright(C) 2016  Vladislav Mlejnecký

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.If not, see<http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace game
{
    /// <summary>
    /// Výčet stavů které vrací metoda hlavního menu.
    /// </summary>
    enum MenuChoice { New, Load, Score, Story, Info, Help, Quit, Debug };

    /// <summary>
    /// Statická třída pro globální proměnné.
    /// </summary>
    static class Global
    {
        static public bool HaveToSave = false;
        static public bool Monochrome = false;
        static public ConsoleStuffs.TuxChoise TuxArgumentFromCommandLine = ConsoleStuffs.TuxChoise.Tux;
#if (EN_COLOR_REPAIR)
        static public bool RepairColors = false;
#endif
    }

    /// <summary>
    /// Hlavní třída celé hry, zde je umístěn Main().
    /// </summary>
    class Program
    {
        /// <summary>
        /// Instance hry.
        /// </summary>
        static public Game game;

        /// <summary>
        /// Vstupní metoda programu.
        /// </summary>
        /// <param name="args">Nejsou třeba.</param>
        static int Main(string[] args)
        {
            //budu postupně procházet všechny argumenty (když žádné nebudou tak se tohle neprovede)
            for (int i = 0; i < args.Length; i++)
            {
                // u každého se rozhodnu co s ním
                switch (args[i].ToString())
                {
                    case "cow":
                        Global.TuxArgumentFromCommandLine = ConsoleStuffs.TuxChoise.Cow;
                        break;
                    case "head-in":
                        Global.TuxArgumentFromCommandLine = ConsoleStuffs.TuxChoise.HeadIn;
                        break;
                    case "monochrome":
                        Global.Monochrome = true;
                        break;
#if (EN_COLOR_REPAIR)
                    case "repair-colors":
                        Global.RepairColors = true;
                        break;
#endif
                    case "help":
                        Console.WriteLine("Tahová RPG hra o záchraně světa.\n");
                        Console.WriteLine("Typické použití je bez parametrů.\n\nParametry:");
                        Console.WriteLine("help          - vypíše tuto nápovědu");
                        Console.WriteLine("monochrome    - hra bude pouze černobílá");
#if (EN_COLOR_REPAIR)
                        Console.WriteLine("repair-colors - při načítání staré uložené hry opraví barvy");
#endif
#if (EN_EASTEREGG_HELP)
                        Console.WriteLine("cow           - vykreslí místo tuxe kravičku (easter egg)");
                        Console.WriteLine("head-in       - vykreslí místo tuxe jinou kravičku (easter egg)");
#endif
                        Console.WriteLine("\n\nChyby v programu můžete hlásit na:\nv.mlejnecky@seznam.cz\n\n");

                        Console.WriteLine("Linux Story. Copyright(C) 2016  Vladislav Mlejnecký");
                        Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY.");
                        Console.WriteLine("This is free software, and you are welcome to redistribute it");
                        Console.WriteLine("under certain conditions; type `show c' for details.");

                        return -1;
                    default:
                        Console.WriteLine("Špatné použití, pro nápovědu zadej parametr help.");
                        return -1;
                }
            }
            
            //nastavení konzole
            ConsoleStuffs.SetUpConsole();

            //načtení dat pro skóre
            Score.LoadScoreData();

            //proměnná držící informaci o tom jestli je hra zapnutá nebo ne
            bool GameRunning = true;

            // zde vykreslím menu, pokud si uživatel vybere tak si skočím do jiných 
            // metod kam jest třeba, tam ho pak držím dokud třeba hraje apod.
            do
            {
                switch (MakeMenu())
                {
                    case MenuChoice.New:
                        game = new Game();
                        CreateAndPlayNewGame();

                        //pokud mám uložit tak uložím
                        if (Global.HaveToSave == true) Saver.Save(game);
                        Global.HaveToSave = false;

                        game = null;
                        break;
                    case MenuChoice.Load:
                        //vyberu uloženou hru
                        string ChosenGame;
                        bool Chosen = DrawLoadMenu(out ChosenGame);
                        //pokud pokud jsem vybral, jinak se vracím do menu
                        if (Chosen == true)
                        {
                            Saver.Load(ChosenGame);

                            //zde se bude hrát
                            game.MakeLoadGame();

                            //pokud mám uložit tak uložím
                            if (Global.HaveToSave == true) Saver.Save(game);
                            Global.HaveToSave = false;

                            game = null;
                        }
                        break;
                    case MenuChoice.Score:
                        DrawScore();
                        break;
                    case MenuChoice.Story:
                        TextMenuWindow storyWindow = new TextMenuWindow(@"story.txt");
                        storyWindow.MakeWindow();
                        storyWindow = null;
                        break;
                    case MenuChoice.Info:
                        TextMenuWindow infoWindow = new TextMenuWindow(@"info.txt");
                        infoWindow.MakeWindow();
                        infoWindow = null;
                        break;
                    case MenuChoice.Help:
                        TextMenuWindow helpWindow = new TextMenuWindow(@"help.txt");
                        helpWindow.MakeWindow();
                        helpWindow = null;
                        break;
#if (DEBUG)
                    case MenuChoice.Debug:
                        Debug.MakeDebug();
                        break;
#endif
                    case MenuChoice.Quit:
                        GameRunning = false;
                        break;
                }

            } while (GameRunning);

            //uložení skóre
            Score.SaveScoreData();
            
            //krátké rozloučení na závěr
            ConsoleStuffs.DrawEndScreen();

            //uvolnění konzole
            ConsoleStuffs.FreeConsole();

            return 1;
        }

        /// <summary>
        /// Tato metoda vytvoří hru, je to takvé to okno před zapnutím hry kde se píše jméno hráče.
        /// </summary>
        private static void CreateAndPlayNewGame()
        {
            //vykreslím úvodní info na obrazovku
            Console.Clear();
            ConsoleStuffs.DrawFrame();
            ConsoleStuffs.TextPrint("Nová hra", 3, 3);
            ConsoleStuffs.TextPrint("Zadej své jméno udatný bojovníku: ", 6, 3);

            //sem uložím jméno hráče
            string PlayerName;

            //dokavaď se uživateli nepovede dobře zadat jméno tak ho tu držím, ven se dostanu pomocí break
            while (true)
            {
                //načtu jméno
                PlayerName = Console.ReadLine();

                //čekuju jméno jestli není již použito
                if (Saver.CheckName(PlayerName))
                {
                    //jméno taky musí být max. 15 znaků dlouhé
                    if (PlayerName.Length > 15)
                    {
                        //tohle se vypíše když bude jméno krátké
                        ConsoleStuffs.TextPrint("Bojovníkovo jméno musí být krátké a výstižné!", 10, 3);
                        ConsoleStuffs.TextPrint("Pokračuj stiskem klávesy enter...", 11, 3);
                        Console.ReadLine();
                        ConsoleStuffs.TextPrint("                                             ", 10, 3);
                        ConsoleStuffs.TextPrint("                                 ", 11, 3);
                        ConsoleStuffs.TextPrint("                                   ", 6, 37);
                        Console.SetCursorPosition(37, 6);

                    }
                    //když se dostanu až sem je vše jak má být a můžu pokračovat za while
                    else break;
                }
                //vypsání informace pokud je jméno již použito jiným  uživatelem
                else
                {
                    //toto se vypíše pokud je jméno již použito
                    ConsoleStuffs.TextPrint("Toto jméno nelze použít. Již jej nese jiný hrdina!", 10, 3);
                    ConsoleStuffs.TextPrint("Pokračuj stiskem klávesy enter...", 11, 3);
                    Console.ReadLine();
                    ConsoleStuffs.TextPrint("                                                  ", 10, 3);
                    ConsoleStuffs.TextPrint("                                 ", 11, 3);
                    ConsoleStuffs.TextPrint("                                   ", 6, 37);
                    Console.SetCursorPosition(37, 6);
                }
             
            }
            //nějaké další povídání pokud jsem už úspěšně vytvořil jméno
            ConsoleStuffs.TextPrint("Nyní můžeš začít hrát! Pokračuj stiskem \"S\" Hodně štěstí! ", 10, 3);
            ConsoleStuffs.TextPrint("Pro návrat stiskni \"q\" pro pokračování \"s\" ...", 28, 2);

            //tady držím hráče dokud bude hrát a nebo dokud nezmáčkne Q aby utekl z volby hráče
            while (true)
            {
                //tuten se čte klávesa
                switch (ConsoleStuffs.ReadKey().Key)
                {
                    //pokud zmáčknu Q vrátím se do menu
                    case ConsoleKey.Q:
                        return;
                    //pokud zmáčknu S tak vytvořím novou hru
                    case ConsoleKey.S:
                        //v této metodě je hráč držen tak dlouho dokud nedohraje
                        game.MakeNewGame(PlayerName);
                        return;
                    default: break;
                }
            }
        }

        /// <summary>
        /// Vytvoří obrazovku s hlavním menu a nechá uživatele vybrat co chce dělat. 
        /// </summary>
        /// <returns>Mód který chce uživatel spustit.</returns>
        private static MenuChoice MakeMenu()
        {
            //do tohoto se uloží stisknutá klávesa
            ConsoleKeyInfo choice;

            //vykreslení celé nabídky
            Console.Clear();
            ConsoleStuffs.DrawFrame();
            ConsoleStuffs.TextPrint("Linux story", 3, 3);
            ConsoleStuffs.TextPrint("n - nová hra", 8, 8);
            ConsoleStuffs.TextPrint("l - načíst hru", 9, 8);
            ConsoleStuffs.TextPrint("s - skóre", 11, 8);
            ConsoleStuffs.TextPrint("p - příběh", 13, 8);
            ConsoleStuffs.TextPrint("i - info", 14, 8);
            ConsoleStuffs.TextPrint("h - nápověda", 15, 8);
            ConsoleStuffs.TextPrint("q - ukončit hru", 17, 8);

#if (DEBUG)
            ConsoleStuffs.TextPrint("d - debug", 19, 8);
#endif
            //Vykreslení tučňáčka :)
            ConsoleStuffs.DrawTux(50, 15, Global.TuxArgumentFromCommandLine);
            
            //opakuju do nekonečna, ukončí se příkazem return
            while (true)
            {
                //načtení volby od uživatele
                choice = ConsoleStuffs.ReadKey();

                //rozhodnutí co se vrátí - pokud nic nanejdeme (uživatel stikl blbost) tak opakujeme
                switch (choice.Key)
                {
                    case ConsoleKey.N: return MenuChoice.New;    //nová hra
                    case ConsoleKey.L: return MenuChoice.Load;    //načíst hru
                    case ConsoleKey.S: return MenuChoice.Score;    //skóre
                    case ConsoleKey.P: return MenuChoice.Story;    //příběh
                    case ConsoleKey.I: return MenuChoice.Info;    //informace
                    case ConsoleKey.H: return MenuChoice.Help;    //nápověda
                    case ConsoleKey.Q: return MenuChoice.Quit;    //vypnout
#if (DEBUG)
                    case ConsoleKey.D: return MenuChoice.Debug;    //debug
#endif
                    default: break;
                }
            }
        }

        /// <summary>
        /// Metoda pro vykreslení celé obrazovky se skóre
        /// </summary>
        private static void DrawScore()
        {
            Console.Clear();
            ConsoleStuffs.DrawFrame();
            ConsoleStuffs.TextPrint("Skóre", 3, 3);

            //ConsoleStuffs.TextPrint("Skóre prozatím není implementováno!", 6, 3);
            int length = Score.ScoreData.Count();
#if (DEBUG)
            for (int i = 0; i < length; i++)
            {
                string name = Score.ScoreData[i].Name;
                if (name == null) name = "Empty name";

                ConsoleStuffs.TextPrint(name, i + 8, 3);
                ConsoleStuffs.TextPrint(Score.ScoreData[i].Lvl.ToString(), i + 8, 20);
                ConsoleStuffs.TextPrint(Score.ScoreData[i].Exp.ToString(), i + 8, 28);
                ConsoleStuffs.TextPrint(Score.ScoreData[i].TuxPower.ToString(), i + 8, 36);
                ConsoleStuffs.TextPrint(Score.ScoreData[i].InstalledServers.ToString(), i + 8, 44);
                ConsoleStuffs.TextPrint(Score.ScoreData[i].Date.ToString(), i + 8, 56);
            }

            ConsoleStuffs.TextPrint("Jméno", 6, 3);
            ConsoleStuffs.TextPrint("Level", 6, 20);
            ConsoleStuffs.TextPrint("Exp", 6, 28);
            ConsoleStuffs.TextPrint("ST", 6, 36);
            ConsoleStuffs.TextPrint("Servery", 6, 44);
            ConsoleStuffs.TextPrint("Datum", 6, 56);
#else
            int CountPrinted = 0;
            for (int i = 0; i < length; i++)
            {
                //nevypíšu záznamy které jsou nulové, lvl je už od začátku 1 a proto když je ve skóre nula tak položka ještě není na světě
                if (Score.ScoreData[i].Lvl != 0)
                {
                    ConsoleStuffs.TextPrint(Score.ScoreData[i].Name, i + 8, 3);
                    ConsoleStuffs.TextPrint(Score.ScoreData[i].Lvl.ToString(), i + 8, 20);
                    ConsoleStuffs.TextPrint(Score.ScoreData[i].Exp.ToString(), i + 8, 28);
                    ConsoleStuffs.TextPrint(Score.ScoreData[i].TuxPower.ToString(), i + 8, 36);
                    ConsoleStuffs.TextPrint(Score.ScoreData[i].InstalledServers.ToString(), i + 8, 44);
                    ConsoleStuffs.TextPrint(Score.ScoreData[i].Date.ToString(), i + 8, 56);
                
                    CountPrinted++;
                }
            }
            //nějaké krátké info pokud nic nevytisknu = skore není žádné
            if (CountPrinted == 0)
            {
                ConsoleStuffs.TextPrint("Nebyl nalezen žádný záznam!", 6, 3);
            }
            //pokud jsem  něco vytiskl tak tam přidám hlavičku 
            else
            {
                ConsoleStuffs.TextPrint("Jméno", 6, 3);
                ConsoleStuffs.TextPrint("Level", 6, 20);
                ConsoleStuffs.TextPrint("Exp", 6, 28);
                ConsoleStuffs.TextPrint("ST", 6, 36);
                ConsoleStuffs.TextPrint("Servery", 6, 44);
                ConsoleStuffs.TextPrint("Datum", 6, 56);
            }
#endif
            //toto je pro návrat do menu, bude se tu čekat tak dlouho dokud se nestikne Q
            ConsoleStuffs.TextPrint("Pro návrat stiskni \"q\" ...", 28, 2);
            while (ConsoleStuffs.ReadKey().Key != ConsoleKey.Q) ;

        }

        /// <summary>
        /// Vytvoí okno s nabídkou uloženýc her a vezme volbu od uživatele kterou nahrát.
        /// </summary>
        /// <returns>Název souboru s uloženou hrou k nahrání.</returns>
        private static bool DrawLoadMenu(out string SaveForLoad)
        {
            Console.Clear();
            ConsoleStuffs.DrawFrame();
            ConsoleStuffs.TextPrint("Načíst hru", 3, 3);

            //vytvoření listu se jmény uložených her
            List<string> SavedGames = new List<string>();

            //pokud složka neexistuje tak ji vytvořím
            if (!Directory.Exists(@"saves")) Directory.CreateDirectory(@"saves");
            
            //Procházení složky s uloženými hrami, hry se uloží do listu SavedGames
            var SavedFiles = Directory.EnumerateFiles(@"saves/");
            foreach (string SavedFile in SavedFiles)
            {
                //useknutí "saves/" na začátku a ".xml" na konci názvu
                string FileName = SavedFile.Remove(0, 6);
                FileName = FileName.Remove(FileName.Length - 4);
                SavedGames.Add(FileName);
            }
            
            //vytisknu si uložené hry
            int i = 0;
            foreach (string SavedGame in SavedGames)
            {
                //tisk do tří sloupků
                if (i >= 0 && i < 20) ConsoleStuffs.TextPrint(SavedGame, i + 5, 3);
                else if (i >= 20 && i < 40) ConsoleStuffs.TextPrint(SavedGame, i - 15, 30);
                else if (i >= 40 && i < 60) ConsoleStuffs.TextPrint(SavedGame, i - 35, 60);
                else break;
                i++;
            }
            if (SavedGames.Count == 0)
            {
                ConsoleStuffs.TextPrint("Nebyla nalezena žádná hra.", 5, 3);
            }


            ConsoleStuffs.TextPrint("Pro zadání hry k načtení stiskni \"L\" v opačném případě \"Q\"...", 28, 3);
            //pokud uživatel stiskne L bude se pokračovat dál a bude moct nahrát hru, v opačném případě se načítání ukončí
            bool HaveToLoad = false;
            while (!HaveToLoad)
            {
                switch (ConsoleStuffs.ReadKey().Key) {
                    case ConsoleKey.L:
                        HaveToLoad = true;
                        break;
                    case ConsoleKey.Q:
                        SaveForLoad = "";
                        return false;
                    default: break;
                }
            }
            ConsoleStuffs.TextPrint("                                                                 ", 28, 3);
            
            //nechám uživatele rozhodnout jaký save chce nahrát
            ConsoleStuffs.TextPrint("Zadej název souboru který chceš nahrát: ", 28, 3);

            do
            {
                ConsoleStuffs.TextPrint("                           ", 28, 43);

                Console.SetCursorPosition(43, 28);
                string ChosenFile = Console.ReadLine();

                foreach (string FileName in SavedGames)
                {
                    if (FileName == ChosenFile)
                    {
                        SaveForLoad = ChosenFile;
                        return true;
                    }
                }

                ConsoleStuffs.TextPrint("Neplatná volba, pokračuj stiskem entru.", 27, 48);
                Console.ReadLine();
                ConsoleStuffs.TextPrint("                                       ", 27, 48);

            } while (true);
        }

    }

    /// <summary>
    /// Třída pro zobrazení a praci s oknama help, info a story v hlavním menu
    /// </summary>
    class TextMenuWindow
    {
        /// <summary>
        /// Proměnná udržující počet řádků které jsou načteny v poli s textem okna.
        /// </summary>
        private int LinesCount = 0;

        /// <summary>
        /// Pole které obsahuje text okna.
        /// </summary>
        private string[] Text = new string[100];

        /// <summary>
        /// Informace o vrchním zobrazovaném řádku v okně.
        /// </summary>
        private int PageTop = 0;

        /// <summary>
        /// informace o spodním zobrazovaném řádku v okně.
        /// </summary>
        private int PageBottom = 23;

        /// <summary>
        /// Cesta k souboru s obsahem okna.
        /// </summary>
        private string FileName;

        /// <summary>
        /// Konstruktor třídy.
        /// </summary>
        /// <param name="file_name">Relativní cesta k souboru s textem okna.</param>
        public TextMenuWindow(string file_name)
        {
            FileName = file_name;
        }

        /// <summary>
        /// Otevření, načtení a zpracování zadaného souboru.
        /// </summary>
        /// <param name="FileName">Název souboru k načtení.</param>
        /// <returns>Vrací true pokud se povede načtení.</returns>
        private bool OpenFile(string FileName)
        {
            //otevřu a načtu file, taky ošetřím vyjímku že by neexistoval
            try
            {
                //otevřu si file
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("game.Resources."+FileName);
                StreamReader file = new StreamReader(stream);
                //připravím si proměnné pro parsování
                int counter = 0;
                string line;
                //procházím  řádek po řádku a ukládám do pole
                while ((line = file.ReadLine()) != null && counter < 100)
                {
                    Text[counter] = line;
                    counter++;
                }
                //uložím si počet načtených řádek, používám k tomu abych při posouvání textu v okně nejel moc daleko
                LinesCount = counter;
                //zavřu soubor
                file.Close();
                return true;
            }
            //zachycena vyjímka a její ošetření!
            catch (Exception e)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                Console.Write("Zachycena Vyjímka! \n\n{0}", e.Message);
                Console.ReadLine();
                return false;
            }
        }

        /// <summary>
        /// Metoda pro vykreslení okna info, story nebo nápověda. Vytiskne obsah načteného souboru.
        /// </summary>
        private void PrintText()
        {
            Console.Clear();
            ConsoleStuffs.DrawFrame();

            // prochází se pole kde je po řádkách uložený celý info file a vytisknout se jen ty řádky
            //které jsou určitém rozmezí
            int line_for_print = 3;
            for (int i = PageTop; i < PageBottom; i++)
            {
                ConsoleStuffs.TextPrint(Text[i], line_for_print, 5);
                line_for_print++;
            }

            ConsoleStuffs.TextPrint("Pro pohyb v textu použij \"d\" a \"u\", pro návrat stiskni \"q\". ", 28, 2);
        }

        /// <summary>
        /// Vykreslí okno s textem ze souboru a umožní pohyb v něm pomocí kláves. 
        /// </summary>
        private void FillWindow()
        {
            //drží info o stisknuté klávese
            ConsoleKeyInfo key_pressed;
            //drží informaci jestli je potřeba překlesit obrazovku
            bool refresh = true;
            do
            {
                //vypíše celé okno pokud je třeba
                if (refresh) PrintText();
                //přečte stisk klávesy
                key_pressed = ConsoleStuffs.ReadKey();
                //rozhone se co dál, jestli posunovat nahoru či dolů
                switch (key_pressed.Key)
                {
                    case ConsoleKey.U:
                        //musí to jít posunout aby se okno šouplo 
                        if (PageTop > 0)
                        {
                            this.PageBottom--;
                            this.PageTop--;
                            refresh = true;
                        }
                        else {
                            refresh = false;
                        }
                        break;
                    case ConsoleKey.D:
                        //posune se dolů jeli kam
                        if (PageBottom < LinesCount)
                        {
                            this.PageBottom++;
                            this.PageTop++;
                            refresh = true;
                        }
                        else {
                            refresh = false;
                        }
                        break;
                    default: break;
                }
            }
            //opakuje se dokud není stisknuto Q
            while (key_pressed.Key != ConsoleKey.Q);
        }

        /// <summary>
        /// Metoda která vykouzlí celé okno, načte potřebná data, vše nastaví, pohybuje s obsahem v okně...
        /// </summary>
        public void MakeWindow()
        {
            //načíst soubor, pokud se to povede otevřu okno začnu jej ovládat
            if (OpenFile(FileName)) FillWindow();
        }
    }

    /// <summary>
    /// Třída implementující mechanismy pro načtení a uložení skóre do souboru xml. Zároveň drží data skore.
    /// </summary>
    static class Score
    {
        /// <summary>
        /// Pole pro uložení skóre.
        /// </summary>
        static public ScoreItem[] ScoreData = new ScoreItem[16];

        /// <summary>
        /// Metoda jenž uloží skóre do souboru.
        /// </summary>
        static public void SaveScoreData()
        {
            //vytvořím si serializér pro můj typ
            XmlSerializer serializer = new XmlSerializer(ScoreData.GetType());

            //abych mohl ukládat proud dat
            StreamWriter MyStreamWriter = new StreamWriter(@"data/score.xml");

            //uložím data pomocí mého serializéru
            serializer.Serialize(MyStreamWriter, ScoreData);

            //poklidíme a garbage colector se o to už postará :)
            serializer = null;
            MyStreamWriter.Close();
            MyStreamWriter = null;
        }

        /// <summary>
        /// Metoda načte data pro skóre uložené v souboru.
        /// </summary>
        static public void LoadScoreData()
        {
            //ochrana proti neexistenci složky, prostě se vytvoří dole
            if (Directory.Exists(@"data"))
            {
                //ošetření proti neexistenci souboru, dole se vytvoří data a při vypínání se prostě soubor vytvoří sám
                if (File.Exists(@"data/score.xml"))
                {
                    //vytvořím si serializér pro můj typ
                    XmlSerializer serializer = new XmlSerializer(ScoreData.GetType());

                    //proud pro načítání dat
                    StreamReader MyStreamReader = new StreamReader(@"data/score.xml");

                    //výstup deserializace jest třeba přetypovat neboť vrací object
                    ScoreData = (ScoreItem[])serializer.Deserialize(MyStreamReader);

                    //úklid smetí
                    serializer = null;
                    MyStreamReader.Close();
                    MyStreamReader = null;
                }
                else
                {
                    //pokud nenajdu file tak ošetřím chybu že by data pro skóre neexistovala 
                    FillDataWithEmpty();
                }
            }
            else
            {
                //tady jsem nenašel ani directory, nejdříve ji vytvořím
                Directory.CreateDirectory(@"data");
                FillDataWithEmpty();
            }

        }

        /// <summary>
        /// Metoda pro výpočet ohodnocení pozice, takto můžu porovnávat více záznamů jedním číslem.
        /// </summary>
        /// <param name="Item">Položka skóre pro jakou chci rank vypočítat.</param>
        /// <returns>Hodnocení dané položky.</returns>
        static private int CalcRankForItem(ScoreItem Item)
        {
            return Item.Lvl + Item.TuxPower + Item.Exp + Item.InstalledServers;
        }

        /// <summary>
        /// Metoda pro výpočet ohodnocení pozice, takto můžu porovnávat více záznamů jedním číslem.
        /// </summary>
        /// <returns>Hodnocení dané položky.</returns>
        static private int CalcRankForItem(int Lvl, int TuxPower, int Exp, int InstalledServers)
        {
            return Lvl + TuxPower + Exp + InstalledServers;
        }
        
        /// <summary>
        /// Metoda pro přidání záznamu do skóre, sama si zkontroluje jestli je přidání možné, kam přidat a setřídí prvky.
        /// </summary>
        /// <param name="Entity">Hráč který skóre vtvořil.</param>
        /// <param name="Servers">Počet serverů které hráč nainstaloval. Nejsou drženy v entitě hráče.</param>
        static public void AddItem(Player Entity, int Servers)
        {
            //vytvořím si lokální kopii dat pro skóre
            ScoreItem[] ScoreDataLocal = new ScoreItem[ScoreData.Count()];
            for (int i = 0; i < ScoreData.Count(); i++)
            {
                ScoreDataLocal[i] = new ScoreItem();
                ScoreDataLocal[i].Date = ScoreData[i].Date;
                ScoreDataLocal[i].Exp = ScoreData[i].Exp;
                ScoreDataLocal[i].InstalledServers = ScoreData[i].InstalledServers;
                ScoreDataLocal[i].Lvl = ScoreData[i].Lvl;
                ScoreDataLocal[i].Name = ScoreData[i].Name;
                ScoreDataLocal[i].TuxPower = ScoreData[i].TuxPower;

            }

            //vypočítám rank entity kterou jsem dostal jako argument
            int RankOfGotEntity = CalcRankForItem(Entity.Level, Entity.TuxPower, Entity.Experiences, Servers);

            //procházím a třídím data
            bool Stored = false;
            for (int i = 0; i < ScoreData.Count(); i++)
            {
                //ještě jsem nenašel místo kam vložit
                if (Stored == false)
                {

                    if (RankOfGotEntity > CalcRankForItem(ScoreDataLocal[i]))
                    {

                        ScoreDataLocal[i].Date = DateTime.Now;
                        ScoreDataLocal[i].Exp = Entity.Experiences;
                        ScoreDataLocal[i].InstalledServers = Servers;
                        ScoreDataLocal[i].Lvl = Entity.Level;
                        ScoreDataLocal[i].TuxPower = Entity.TuxPower;
                        ScoreDataLocal[i].Name = Entity.Name;

                        Stored = true;
                    }
                }
                else
                {
                    ScoreDataLocal[i] = new ScoreItem();
                    ScoreDataLocal[i].Date = ScoreData[i - 1].Date;
                    ScoreDataLocal[i].Exp = ScoreData[i - 1].Exp;
                    ScoreDataLocal[i].InstalledServers = ScoreData[i - 1].InstalledServers;
                    ScoreDataLocal[i].Lvl = ScoreData[i - 1].Lvl;
                    ScoreDataLocal[i].Name = ScoreData[i - 1].Name;
                    ScoreDataLocal[i].TuxPower = ScoreData[i - 1].TuxPower;
                 }
            }
            for (int i = 0; i < ScoreData.Count(); i++)
            {
                ScoreData[i] = new ScoreItem();
                ScoreData[i].Date = ScoreDataLocal[i].Date;
                ScoreData[i].Exp = ScoreDataLocal[i].Exp;
                ScoreData[i].InstalledServers = ScoreDataLocal[i].InstalledServers;
                ScoreData[i].Lvl = ScoreDataLocal[i].Lvl;
                ScoreData[i].Name = ScoreDataLocal[i].Name;
                ScoreData[i].TuxPower = ScoreDataLocal[i].TuxPower;

            }
        }
#if (DEBUG)
        /// <summary>
        /// Metoda jen pro debug, přidá prostě random hráče do skóre.
        /// </summary>
        static public void AddItemAsTest()
        {
            Player TestPlayer = new Player("Testing boy", '!', 8, 8, ConsoleColor.White);
            TestPlayer.Level = ConsoleStuffs.GetRandomNumber(1, 10);
            TestPlayer.TuxPower = ConsoleStuffs.GetRandomNumber(1, 10);
            TestPlayer.Experiences = ConsoleStuffs.GetRandomNumber(1, 1000);
            AddItem(TestPlayer, ConsoleStuffs.GetRandomNumber(1, 10));
        }
#endif
        /// <summary>
        /// Metoda která zaplní data prázdnejma instancema.
        /// </summary>
        static public void FillDataWithEmpty()
        {
            for (int i = 0; i < ScoreData.Count(); i++)
            {
                ScoreData[i] = new ScoreItem();
            }

        }
    }

    /// <summary>
    /// Třída která se stará o ukládání / načítání hry a vše s tím spojené.
    /// </summary>
    static class Saver
    {
        /// <summary>
        /// Metoda uloží zadanou instanci hry do xml file.
        /// </summary>
        /// <param name="GameForSave">Objekt hry určený pro uložení.</param>
        static public void Save(Game GameForSave)
        {
            //pokud složka neexistuje tak ji vytvořím
            if (!Directory.Exists(@"saves")) Directory.CreateDirectory(@"saves");
            
            //vytvořím si serializér pro můj typ
            XmlSerializer serializer = new XmlSerializer(typeof(Game));

            //abych mohl ukládat proud dat
            StreamWriter MyStreamWriter = new StreamWriter(@"saves/" + GameForSave.player.Name + ".xml");

            //uložím data pomocí mého serializéru
            serializer.Serialize(MyStreamWriter, GameForSave);

            //poklidíme a garbage colector se o to už postará :)
            serializer = null;
            MyStreamWriter.Close();
            MyStreamWriter = null;
            
        }

        /// <summary>
        /// Metoda pro načtení uložené hry pro zadaného hráče.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns>True pokud se načtení povede.</returns>
        static public bool Load(string Name)
        {
            if (Directory.Exists(@"saves"))
            {
                string FilePath = @"saves/" + Name + ".xml";
                if (File.Exists(FilePath))
                {
                    //vytvořím si serializér pro můj typ
                    XmlSerializer serializer = new XmlSerializer(typeof(Game));

                    //proud pro načítání dat
                    StreamReader MyStreamReader = new StreamReader(FilePath);

                    //výstup deserializace jest třeba přetypovat neboť vrací object
                    Program.game = (Game)serializer.Deserialize(MyStreamReader);

                    //úklid smetí
                    serializer = null;
                    MyStreamReader.Close();
                    MyStreamReader = null;

#if (EN_COLOR_REPAIR)
                    //pokud mám přikázáno z argumentů příkazové řádky opravit barvy, no tak to prostě udělám
                    if (Global.RepairColors)
                    {
                        for (int world = 0; world < Program.game.Worlds.Count; world++)
                        {
                            for (int wall = 0; wall < Program.game.Worlds[world].Walls.Count; wall++)
                            {
                                Program.game.Worlds[world].Walls[wall].ColorForPrint = ConsoleColor.White;
                            }
                            for (int Door = 0; Door < Program.game.Worlds[world].Doors.Count; Door++)
                            {
                                Program.game.Worlds[world].Doors[Door].ColorForPrint = ConsoleColor.DarkGray;
                            }
                            for (int Mob = 0; Mob < Program.game.Worlds[world].Mobs.Count; Mob++)
                            {
                                if(Program.game.Worlds[world].Mobs[Mob].IsEvil == true)
                                    Program.game.Worlds[world].Mobs[Mob].ColorForPrint = ConsoleColor.Red;
                                else
                                    Program.game.Worlds[world].Mobs[Mob].ColorForPrint = ConsoleColor.Blue;
                            }
                            for (int Tree_number = 0; Tree_number < Program.game.Worlds[world].Trees.Count; Tree_number++)
                            {
                                Program.game.Worlds[world].Trees[Tree_number].ColorForPrint = ConsoleColor.Green;
                            }
                            for (int Wattes = 0; Wattes < Program.game.Worlds[world].Watters.Count; Wattes++)
                            {
                                Program.game.Worlds[world].Watters[Wattes].ColorForPrint = ConsoleColor.Blue;
                            }
                            for (int Hill = 0; Hill < Program.game.Worlds[world].Hills.Count; Hill++)
                            {
                                Program.game.Worlds[world].Hills[Hill].ColorForPrint = ConsoleColor.DarkGray;
                            }
                            for (int Gate = 0; Gate < Program.game.Worlds[world].Gates.Count; Gate++)
                            {
                                Program.game.Worlds[world].Gates[Gate].ColorForPrint = ConsoleColor.Cyan;
                            }
                            for (int Server = 0; Server < Program.game.Worlds[world].Servers.Count; Server++)
                            {
                                Program.game.Worlds[world].Servers[Server].ColorForPrint = ConsoleColor.Yellow;
                            }
                            for (int Artifact = 0; Artifact < Program.game.Worlds[world].Artifacts.Count; Artifact++)
                            {
                                Program.game.Worlds[world].Artifacts[Artifact].ColorForPrint = ConsoleColor.Yellow;
                            }
                            Program.game.player.ColorForPrint = ConsoleColor.White;
                        }
                    }
#endif

                    //vrátíme info o tom že se načtení povedlo
                    return true;
                }
                //defaultně se vrátí neúspěch
                return false;
            }
            else //pokud složka neexistuje tak ji vytvořím a vrtím false protože určitě neexistuje uloženou hru
            {
                Directory.CreateDirectory(@"saves");
                return false;
            }
        }

        /// <summary>
        /// Metoda pro zkontrolování jestli je možné použít zadané jméno, savy se rozlišují podle jména hráče.
        /// </summary>
        /// <param name="GivenName">Jméno pro zkontrolování</param>
        /// <returns>True pokud lze jméno použít.</returns>
        static public bool CheckName(string GivenName)
        {
            if (Directory.Exists(@"saves"))
            {
                if (File.Exists(@"saves/" + GivenName + ".xml")) return false;
                else return true;
            }
            else
            {
                Directory.CreateDirectory(@"saves");
                return true;
            }
        }

        /// <summary>
        /// Metoda smaže soubor s uloženou hrou pokud existuje.
        /// </summary>
        /// <param name="PlayerName">Název hráče jehož uloženou hru budu mazat.</param>
        static public void Delete(string PlayerName)
        {
            //pokud složka neexistuje tak ji dole vytořím
            if (Directory.Exists(@"saves"))
            {
                //pokud soubor existuje budeme jej mazat
                if (File.Exists(@"saves/" + PlayerName + ".xml"))
                {
                    File.Delete(@"saves/" + PlayerName + ".xml");
                }
            }
            else
            {
                Directory.CreateDirectory(@"saves");
            }
        }
    }

#if (DEBUG)
    /// <summary>
    /// Výčet stavů které vrací rozhodování v debugu.
    /// </summary>
    enum DebugAction { AddSingleScoreRow, DebugExit, FillWithEmpty, LoadVladisSave };

    /// <summary>
    /// Třída která se stará o debugování.
    /// </summary>
    static class Debug
    {
        /// <summary>
        /// Vykreslí okno debug a nechá uživatele vybrat co dál.
        /// </summary>
        /// <returns>Rozhodnutí uživatele co  se má dělat.</returns>
        private static DebugAction DrawDebugScreenAndGetAction()
        {
            Console.Clear();
            ConsoleStuffs.DrawFrame();
            ConsoleStuffs.TextPrint("Debug", 2, 2);
            ConsoleStuffs.TextPrint("a - přidat jednoho uživatele do skóre, zavolá metodu Score.AddItemAsTest();", 4, 2);
            ConsoleStuffs.TextPrint("b - naplit data pro skóre s prázdnejma intancema, Score.FillDataWithEmpty();", 5, 2);
            ConsoleStuffs.TextPrint("c - načíst uloženou hru hráče vladis, Saver.Load(''vladis'');", 6, 2);
            ConsoleStuffs.TextPrint("q - odejít z debugu", 7, 2);

            //sem se uloží rozhodnutí uživatele
            ConsoleKeyInfo choice;

            //opakuju do nekonečna, ukončí se příkazem return
            while (true)
            {
                //načtení volby od uživatele
                choice = ConsoleStuffs.ReadKey();

                //rozhodnutí co se vrátí - pokud nic nanejdeme (uživatel stikl blbost) tak opakujeme
                switch (choice.Key)
                {
                    case ConsoleKey.A: return DebugAction.AddSingleScoreRow;
                    case ConsoleKey.Q: return DebugAction.DebugExit;
                    case ConsoleKey.B: return DebugAction.FillWithEmpty;
                    case ConsoleKey.C: return DebugAction.LoadVladisSave;
                    default: break;
                }
            }
        }
        
        /// <summary>
        /// Okno vytvoří celý debug, řídí akce a prostě dělá lautr všechno.
        /// </summary>
        static public void MakeDebug()
        {
            //okno debugu
            bool DebugExist = true;
            while (DebugExist)
            {
                //zde se vybere akce a provede
                switch (DrawDebugScreenAndGetAction())
                {
                    //přidání jednoho záznamu do skóre
                    case DebugAction.AddSingleScoreRow:
                        Score.AddItemAsTest();
                        //nějaká odezva
                        ConsoleStuffs.TextPrint("Item do skore byl přidán", 28, 1);
                        Console.ReadLine();
                        ConsoleStuffs.TextPrint("                        ", 28, 1);
                        break;
                    case DebugAction.FillWithEmpty:
                        Score.FillDataWithEmpty();
                        //nějaká odezva
                        ConsoleStuffs.TextPrint("Skore bylo naplněno ničím.", 28, 1);
                        Console.ReadLine();
                        ConsoleStuffs.TextPrint("                          ", 28, 1);
                        break;
                    case DebugAction.LoadVladisSave:
                        Saver.Load("vladis");
                        //nějaká odezva
                        ConsoleStuffs.TextPrint("Save byl načeten.", 28, 1);
                        Console.ReadLine();
                        ConsoleStuffs.TextPrint("                 ", 28, 1);
                        break;
                    //ukončení debug okna
                    case DebugAction.DebugExit: DebugExist = false; break;
                    default: break;
                }
            }
        }
    }
#endif
}
