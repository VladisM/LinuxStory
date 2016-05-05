using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace game
{
    /// <summary>
    /// V této třídě je celá hra, drží se zde světy, manipuluje se s nima.
    /// </summary>
    public class Game
    {
        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public Game() { }
           
        /// <summary>
        /// Instance objektu která drží hráče.
        /// </summary>
        public Player player;
        
        /// <summary>
        /// Zde se drží jednotlivé instance lokací.
        /// </summary>
        public List<World> Worlds = new List<World>();

        /// <summary>
        /// Zde se drží ID aktuálního světa.
        /// </summary>
        private int ActualWorld = 0;

        /// <summary>
        /// Tohle je staré ID světa, používá se pro odstranění blikání při překleslování mapy v metodě DrawWholeWindow() a musí být gobální.
        /// </summary>
        private int OldActualWorld = -1;

        /// <summary>
        /// Počet serverů obrácených na linux. Maximum je deset, po jejich dosažení hra vyhrána.
        /// </summary>
        public int LinuxServerCount = 0;

        /// <summary>
        /// Vytvoří novou hru.
        /// </summary>
        /// <param name="PlayerName">Jméno hráče.</param>
        public void MakeNewGame(string PlayerName)
        {
            //natahání map
            for (int i = 0; i < 7; i++)
            {
                Worlds.Add(new World());
                switch (i)
                {
                    case 0: Worlds[i].LoadMap("world.txt", 6, 7, 0); break;
                    case 1: Worlds[i].LoadMap("vogsfera.txt", 4, 10, 6); break;
                    case 2: Worlds[i].LoadMap("magrathea.txt", 47, 13, 8); break;
                    case 3: Worlds[i].LoadMap("betelgeuse.txt", 11, 20, 10); break;
                    case 4: Worlds[i].LoadMap("dung_moria.txt", 6, 12, 0); break;
                    case 5: Worlds[i].LoadMap("dung_lakes_of_evil.txt", 3, 10, 0); break;
                    case 6: Worlds[i].LoadMap("dung_dark_forrest.txt", 2, 15, 0); break;
                    default: break; //tohle stejně nikdy nenastane ale co kdybych něco pokazil
                }
            }

            //inicializacce základních objektů ve hře
            player = new Player(PlayerName, '@', Worlds[ActualWorld].LastPositionY, Worlds[ActualWorld].LastPositionX, ConsoleColor.White);

            //toto tady musí být aby se ukládala i další hra, pokud by někdo zemřel v první a založil další
            Global.HaveToSave = true;

            //zavolání metody pro hraní hry
            PlayGame();

        }

        /// <summary>
        /// Metoda pro vytvoření hry z načtené.
        /// </summary>
        public void MakeLoadGame()
        {
            //toto aby se vykreslil celý svět
            OldActualWorld = -1;

            //toto tady musí být aby se ukládala i další hra, pokud by někdo zemřel v první a založil další
            Global.HaveToSave = true;

            //zavolání metody pro hraní hry
            PlayGame();
        }

        /// <summary>
        /// Metoda ve které je hra hrána.
        /// </summary>
        private void PlayGame()
        {
            bool WorldExist = true;
            
            Messenger.Update("Wau, to je ale krásný svět!");

            while (WorldExist)
            {
                HealPlayer(player);
                MoveAllMobs();
                DrawWholeWindow();
                Messenger.Clean();

                //uživatel udělá tah
                switch (ConsoleStuffs.ReadKey().Key)
                {
                    case ConsoleKey.W:
                        MoveIfPossible(player, -1, 0);
                        break;
                    case ConsoleKey.S:
                        MoveIfPossible(player, 1, 0);
                        break;
                    case ConsoleKey.A:
                        MoveIfPossible(player, 0, -1);
                        break;
                    case ConsoleKey.D:
                        MoveIfPossible(player, 0, 1);
                        break;
                    case ConsoleKey.O:
                        ToggleNearDoor(player);
                        break;
                    case ConsoleKey.T:
                        FindNearAndAttack(player, Worlds[ActualWorld].Mobs);
                        break;
                    case ConsoleKey.G:
                        UseNearGate(player);
                        break;
                    case ConsoleKey.P:
                        PrayNearArtifact(player);
                        break;
                    case ConsoleKey.I:
                        InstallNearServer(player);
                        break;
                    case ConsoleKey.Q:
                        string Backup = Messenger.GetActual();
                        Messenger.Update("Jsi si opravdu jistý že chceš ukončit hru? [A/N]");
                        Messenger.Refresh();
                        switch (ConsoleStuffs.ReadKey().Key) {
                            case ConsoleKey.A: WorldExist = false; break;
                            case ConsoleKey.N: Messenger.Update(Backup); Messenger.Refresh(); break;
                            default: Messenger.Update(Backup); Messenger.Refresh(); break;
                        }
                        break;
                    default: break;
                }

                //mobové provedou útoky
                bool PlayerDied = AttackIfNear(Worlds[ActualWorld].Mobs, player);

                //pokud hráč zemřel tak se hra ukončí
                if (PlayerDied == true)
                {
                    //vytiskne parte
                    ConsoleStuffs.DrawDiedScreen(player);
                    WorldExist = false;
                    //vyhrál jsem,není co ukládat + smažu uložený soubor pokud jsem z něj načtený
                    Global.HaveToSave = false;
                    Saver.Delete(player.Name);
                    //žádost o přidání do skóre, pokud je hodno tak se přidá
                    Score.AddItem(player, LinuxServerCount);
                }

                //pokud jsou všechny servery nainstalovány
                if (LinuxServerCount == 10)
                {
                    //vytiskne se výherní obrazovka
                    ConsoleStuffs.DrawWinScreen(player);
                    WorldExist = false;
                    //vyhrál jsem,není co ukládat + smažu uložený soubor pokud jsem z něj načtený
                    Global.HaveToSave = false;
                    Saver.Delete(player.Name);
                    //žádost o přidání do skóre, pokud je hodno tak se přidá
                    Score.AddItem(player, LinuxServerCount);
                }
            }
        }

//------------------------------------------------------------------------------------------------------
//      Následují funkce pro interakci s různými entitami ve hře.
//------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Metoda která nejde blízký objekt v listu a vrátí jeho ID.
        /// </summary>
        /// <param name="Entity">Entita vůči které se provádí hledání.</param>
        /// <param name="Objects">List objeků ve kterém se bude hledat.</param>
        /// <param name="FoundID">Sem se uloží ID nalezeného objektu.</param>
        /// <returns>True pokud takový objekkt existuje.</returns>
        private bool FindNear(Player Entity, List<Door> Objects, out int FoundID)
        {
            //počítadlo aktuálních objektů
            int CurrentID = 0;

            //procházení seznamu a testování jestli je objekt vedle zadané entity
            while (CurrentID < Objects.Count())
            {
                //samotné testování
                if (
                       (Entity.PositionX + 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionX - 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionY + 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                    || (Entity.PositionY - 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                   )
                {
                    //pokud se najde nějaký objekt tak se vrátí jeho ID a true
                    FoundID = CurrentID;
                    return true;
                }
                //jinak v každé iteraci inkrementuji číslo
                CurrentID++;
            }
            //pokud se žádné vyhovující objekty nenašly, vrátí se false
            FoundID = 0;
            return false;
        }

        /// <summary>
        /// Metoda která nejde blízký objekt v listu a vrátí jeho ID.
        /// </summary>
        /// <param name="Entity">Entita vůči které se provádí hledání.</param>
        /// <param name="Objects">List objeků ve kterém se bude hledat.</param>
        /// <param name="FoundID">Sem se uloží ID nalezeného objektu.</param>
        /// <returns>True pokud takový objekkt existuje.</returns>
        private bool FindNear(Player Entity, List<Gate> Objects, out int FoundID)
        {
            //počítadlo aktuálních objektů
            int CurrentID = 0;

            //procházení seznamu a testování jestli je objekt vedle zadané entity
            while (CurrentID < Objects.Count())
            {
                //samotné testování
                if (
                       (Entity.PositionX + 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionX - 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionY + 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                    || (Entity.PositionY - 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                   )
                {
                    //pokud se najde nějaký objekt tak se vrátí jeho ID a true
                    FoundID = CurrentID;
                    return true;
                }
                //jinak v každé iteraci inkrementuji číslo
                CurrentID++;
            }
            //pokud se žádné vyhovující objekty nenašly, vrátí se false
            FoundID = 0;
            return false;
        }

        /// <summary>
        /// Metoda která nejde blízký objekt v listu a vrátí jeho ID.
        /// </summary>
        /// <param name="Entity">Entita vůči které se provádí hledání.</param>
        /// <param name="Objects">List objeků ve kterém se bude hledat.</param>
        /// <param name="FoundID">Sem se uloží ID nalezeného objektu.</param>
        /// <returns>True pokud takový objekkt existuje.</returns>
        private bool FindNear(Player Entity, List<Artifact> Objects, out int FoundID)
        {
            //počítadlo aktuálních objektů
            int CurrentID = 0;

            //procházení seznamu a testování jestli je objekt vedle zadané entity
            while (CurrentID < Objects.Count())
            {
                //samotné testování
                if (
                       (Entity.PositionX + 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionX - 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionY + 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                    || (Entity.PositionY - 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                   )
                {
                    //pokud se najde nějaký objekt tak se vrátí jeho ID a true
                    FoundID = CurrentID;
                    return true;
                }
                //jinak v každé iteraci inkrementuji číslo
                CurrentID++;
            }
            //pokud se žádné vyhovující objekty nenašly, vrátí se false
            FoundID = 0;
            return false;
        }

        /// <summary>
        /// Metoda která nejde blízký objekt v listu a vrátí jeho ID.
        /// </summary>
        /// <param name="Entity">Entita vůči které se provádí hledání.</param>
        /// <param name="Objects">List objeků ve kterém se bude hledat.</param>
        /// <param name="FoundID">Sem se uloží ID nalezeného objektu.</param>
        /// <returns>True pokud takový objekkt existuje.</returns>
        private bool FindNear(Player Entity, List<Server> Objects, out int FoundID)
        {
            //počítadlo aktuálních objektů
            int CurrentID = 0;

            //procházení seznamu a testování jestli je objekt vedle zadané entity
            while (CurrentID < Objects.Count())
            {
                //samotné testování
                if (
                       (Entity.PositionX + 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionX - 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionY + 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                    || (Entity.PositionY - 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                   )
                {
                    //pokud se najde nějaký objekt tak se vrátí jeho ID a true
                    FoundID = CurrentID;
                    return true;
                }
                //jinak v každé iteraci inkrementuji číslo
                CurrentID++;
            }
            //pokud se žádné vyhovující objekty nenašly, vrátí se false
            FoundID = 0;
            return false;
        }
        
        /// <summary>
        /// Metoda která nejde blízký objekt v listu a vrátí jeho ID. Pozor, najde jenom jeden! Pokud sousedím s více tak vrátí ten který je v listu nejdříve.
        /// </summary>
        /// <param name="Entity">Entita vůči které se provádí hledání.</param>
        /// <param name="Objects">List objeků ve kterém se bude hledat.</param>
        /// <param name="FoundID">Sem se uloží ID nalezeného objektu.</param>
        /// <returns>True pokud takový objekkt existuje.</returns>
        private bool FindNear(Player Entity, List<NPC> Objects, out int FoundID)
        {
            //počítadlo aktuálních objektů
            int CurrentID = 0;

            //procházení seznamu a testování jestli je objekt vedle zadané entity
            while (CurrentID < Objects.Count())
            {
                //samotné testování
                if (
                       (Entity.PositionX + 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionX - 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionY + 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                    || (Entity.PositionY - 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                   )
                {
                    //pokud se najde nějaký objekt tak se vrátí jeho ID a true
                    FoundID = CurrentID;
                    return true;
                }
                //jinak v každé iteraci inkrementuji číslo
                CurrentID++;
            }
            //pokud se žádné vyhovující objekty nenašly, vrátí se false
            FoundID = 0;
            return false;
        }

        /// <summary>
        /// Metoda která nejde blízký objekt v listu a vrátí jeho ID. Pozor, najde jenom jeden! Pokud sousedím s více tak vrátí ten který je v listu nejdříve.
        /// </summary>
        /// <param name="Entity">Entita vůči které se provádí hledání.</param>
        /// <param name="Objects">List objeků ve kterém se bude hledat.</param>
        /// <param name="JustEvil">True pokud má hledat jen mezi zlými entity.</param>
        /// <param name="FoundID">Sem se uloží ID nalezeného objektu.</param>
        /// <returns>True pokud takový objekkt existuje.</returns>
        private bool FindNear(Player Entity, List<NPC> Objects, bool JustEvil, out int FoundID)
        {
            //počítadlo aktuálních objektů
            int CurrentID = 0;

            //procházení seznamu a testování jestli je objekt vedle zadané entity
            while (CurrentID < Objects.Count())
            {
                //samotné testování, hledám jen zlé moby
                if ((
                       (Entity.PositionX + 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionX - 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionY + 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                    || (Entity.PositionY - 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                   ) && Objects[CurrentID].IsEvil == true && JustEvil == true)
                {
                    //pokud se najde nějaký objekt tak se vrátí jeho ID a true
                    FoundID = CurrentID;
                    return true;
                }
                //samotné testování, hledám hodné moby, o tom co se povede rozhoduje parametr JustEvil
                if ((
                       (Entity.PositionX + 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionX - 1 == Objects[CurrentID].PositionX && Entity.PositionY == Objects[CurrentID].PositionY)
                    || (Entity.PositionY + 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                    || (Entity.PositionY - 1 == Objects[CurrentID].PositionY && Entity.PositionX == Objects[CurrentID].PositionX)
                   ) && JustEvil == false)
                {
                    //pokud se najde nějaký objekt tak se vrátí jeho ID a true
                    FoundID = CurrentID;
                    return true;
                }
                //jinak v každé iteraci inkrementuji číslo
                CurrentID++;
            }
            //pokud se žádné vyhovující objekty nenašly, vrátí se false
            FoundID = 0;
            return false;
        }

        /// <summary>
        /// Nalezení nejvýše 4 mobů kteří mohou stát okolo hráče. Vrátí jejich počet a na jednotlivá ID se odkazuje.
        /// </summary>
        /// <param name="Masters">List mobů kteří budou útočit, v nich hledám.</param>
        /// <param name="Slave">Jejich cíl.</param>
        /// <param name="FoundID1">ID prvního nalezeného moba.</param>
        /// <param name="FoundID2">ID druhého nalezeného moba.</param>
        /// <param name="FoundID3">ID třetího nalezeného moba.</param>
        /// <param name="FoundID4">ID čtvrtého nalezeného moba.</param>
        /// <returns>Počet nalezených mobů.</returns>
        private int FindNear(List<NPC> Masters, Player Slave, out int FoundID1, out int FoundID2, out int FoundID3, out int FoundID4)
        {
            //počítadlo aktuálních objektů
            int CurrentID = 0;

            //počítadlo nalezených mobů (mohou být nejvýše 4)
            int Found = 0;

            //tyhle proměnné jsou k ničemu ale když  tam nejsou a přiřazuji hodnoty do out parametrů funkce tak mi překladač řve... není to moc inteligentí
            int ID1 = 0, ID2 = 0, ID3 = 0, ID4 = 0;

            //procházení seznamu a testování jestli je objekt vedle zadané entity
            while (CurrentID < Masters.Count())
            {
                //samotné testování
                if (
                       (Slave.PositionX + 1 == Masters[CurrentID].PositionX && Slave.PositionY == Masters[CurrentID].PositionY)
                    || (Slave.PositionX - 1 == Masters[CurrentID].PositionX && Slave.PositionY == Masters[CurrentID].PositionY)
                    || (Slave.PositionY + 1 == Masters[CurrentID].PositionY && Slave.PositionX == Masters[CurrentID].PositionX)
                    || (Slave.PositionY - 1 == Masters[CurrentID].PositionY && Slave.PositionX == Masters[CurrentID].PositionX)
                   )
                {
                    switch (Found)
                    {
                        case 0: ID1 = CurrentID; Found++; break;
                        case 1: ID2 = CurrentID; Found++; break;
                        case 2: ID3 = CurrentID; Found++; break;
                        case 3: ID4 = CurrentID; Found++; break;
                        default: break;
                    }
                }
                //jinak v každé iteraci inkrementuji číslo
                CurrentID++;
            }
            FoundID1 = ID1;
            FoundID2 = ID2;
            FoundID3 = ID3;
            FoundID4 = ID4;
            return Found;
        }
        
        /// <summary>
        /// Nalezení nejvýše 4 mobů kteří mohou stát okolo hráče. Vrátí jejich počet a na jednotlivá ID se odkazuje.
        /// </summary>
        /// <param name="Masters">List mobů kteří budou útočit, v nich hledám.</param>
        /// <param name="Slave">Jejich cíl.</param>
        /// <param name="JustEvil">Rozhoduje jestli prochází jen zlé moby.</param>
        /// <param name="FoundID1">ID prvního nalezeného moba.</param>
        /// <param name="FoundID2">ID druhého nalezeného moba.</param>
        /// <param name="FoundID3">ID třetího nalezeného moba.</param>
        /// <param name="FoundID4">ID čtvrtého nalezeného moba.</param>
        /// <returns>Počet nalezených mobů.</returns>
        private int FindNear(List<NPC> Masters, Player Slave, bool JustEvil, out int FoundID1, out int FoundID2, out int FoundID3, out int FoundID4)
        {
            //počítadlo aktuálních objektů
            int CurrentID = 0;

            //počítadlo nalezených mobů (mohou být nejvýše 4)
            int Found = 0;

            //tyhle proměnné jsou k ničemu ale když  tam nejsou a přiřazuji hodnoty do out parametrů funkce tak mi překladač řve... není to moc inteligentí
            int ID1 = 0, ID2 = 0, ID3 = 0, ID4 = 0;

            //procházení seznamu a testování jestli je objekt vedle zadané entity
            while (CurrentID < Masters.Count())
            {
                //samotné testování pokud se mají hledat i hodní mobové
                if ((
                       (Slave.PositionX + 1 == Masters[CurrentID].PositionX && Slave.PositionY == Masters[CurrentID].PositionY)
                    || (Slave.PositionX - 1 == Masters[CurrentID].PositionX && Slave.PositionY == Masters[CurrentID].PositionY)
                    || (Slave.PositionY + 1 == Masters[CurrentID].PositionY && Slave.PositionX == Masters[CurrentID].PositionX)
                    || (Slave.PositionY - 1 == Masters[CurrentID].PositionY && Slave.PositionX == Masters[CurrentID].PositionX)
                   ) && JustEvil == false)
                {
                    switch (Found)
                    {
                        case 0: ID1 = CurrentID; Found++; break;
                        case 1: ID2 = CurrentID; Found++; break;
                        case 2: ID3 = CurrentID; Found++; break;
                        case 3: ID4 = CurrentID; Found++; break;
                        default: break;
                    }
                }

                //samotné testování pokud se mají hledat jen zlí mobové
                if ((
                       (Slave.PositionX + 1 == Masters[CurrentID].PositionX && Slave.PositionY == Masters[CurrentID].PositionY)
                    || (Slave.PositionX - 1 == Masters[CurrentID].PositionX && Slave.PositionY == Masters[CurrentID].PositionY)
                    || (Slave.PositionY + 1 == Masters[CurrentID].PositionY && Slave.PositionX == Masters[CurrentID].PositionX)
                    || (Slave.PositionY - 1 == Masters[CurrentID].PositionY && Slave.PositionX == Masters[CurrentID].PositionX)
                   ) && JustEvil == true && Masters[CurrentID].IsEvil == true)
                {
                    switch (Found)
                    {
                        case 0: ID1 = CurrentID; Found++; break;
                        case 1: ID2 = CurrentID; Found++; break;
                        case 2: ID3 = CurrentID; Found++; break;
                        case 3: ID4 = CurrentID; Found++; break;
                        default: break;
                    }
                }

                //jinak v každé iteraci inkrementuji číslo
                CurrentID++;
            }
            FoundID1 = ID1;
            FoundID2 = ID2;
            FoundID3 = ID3;
            FoundID4 = ID4;
            return Found;
        }
        
        /// <summary>
        /// Změní stav blízkých dveří.
        /// </summary>
        /// <param name="Entity">Entita ke které se hledají blízké dveře.</param>
        private void ToggleNearDoor(Player Entity)
        {
            //najdu blízké dveře a pokud existují uložím si jejich ID
            int DoorID;
            bool DoorExist = FindNear(Entity, Worlds[ActualWorld].Doors, out DoorID);

            //zkontroluji jestli blízké dveře existují
            if (DoorExist == true)
            {
                //změním jejich stav, otevřeno -> zavřeno, zavřeno -> otevřeno
                if (Worlds[ActualWorld].Doors[DoorID].IsOpen == true)
                {
                    Worlds[ActualWorld].Doors[DoorID].Close();
                    //když se dveře zavřou vypíšu nějakou malou hlášku
                    switch (ConsoleStuffs.GetRandomNumber(1, 2))
                    {
                        case 1: Messenger.Update("Skříp! Buch! Dveře se neochotně zavřeli."); break;
                        case 2: Messenger.Update("Hnusný skřípavý zvuk zrezlých pantů."); break;
                        default: break;
                    }
                }
                else
                {
                    Worlds[ActualWorld].Doors[DoorID].Open();
                    // když se dveře otevírají taky píšu hlášku
                    switch (ConsoleStuffs.GetRandomNumber(1, 3))
                    {
                        case 1: Messenger.Update("Rozkopl jsi dvěře a jako rambo vešel dovnitř!"); break;
                        case 2: Messenger.Update("Tohle by měl někdo vážně promazat."); break;
                        case 3: Messenger.Update("Tyhle dveře jsou ale HNUSNÝ! A ten velkej pavouk! Brr!"); break;
                        default: break;
                    }
                }
            }
            else
            {
                // no a taky píšu nějakou hlášku i když se do dveří netrefím no :) jinak by ta hra byla docela nudná ne? 
                switch (ConsoleStuffs.GetRandomNumber(1, 3))
                {
                    case 1: Messenger.Update("Svět tiknul, a dveře nikde..."); break;
                    case 2: Messenger.Update("Hele, víš že ta metoda pro otevření dveří hledá dveře VEDLE TEBE?"); break;
                    case 3: Messenger.Update("Šmátrám po klice a ta nikde..."); break;
                    default: break;
                }
            }
        }

        /// <summary>
        /// Metoda pro použití brány vedle které stojím.
        /// </summary>
        /// <param name="Entity">Entita hráče.</param>
        private void UseNearGate(Player Entity)
        {
            //najdu blízkou bránu a pokud existují uložím si jejich ID
            int GateID;
            bool GateExist = FindNear(Entity, Worlds[ActualWorld].Gates, out GateID);

            if (GateExist == true)
            {
                Worlds[ActualWorld].LastPositionX = player.PositionX;
                Worlds[ActualWorld].LastPositionY = player.PositionY;

                ActualWorld = Worlds[ActualWorld].Gates[GateID].Target;

                player.PositionSet(Worlds[ActualWorld].LastPositionY, Worlds[ActualWorld].LastPositionX);

                player.OldPositionX = Worlds[ActualWorld].LastPositionX;
                player.OldPositionY = Worlds[ActualWorld].LastPositionY;
                
            }
        }

        /// <summary>
        /// Metoda kterou se hráč pomodlí pokud je u artefaktu. Vzroste mu TuxPower.
        /// </summary>
        /// <param name="Entity">Entita hráče.</param>
        private void PrayNearArtifact(Player Entity)
        {
            //najdu blízkoý artefakt a pokud existují uložím si jeho ID
            int ArtifactID;
            bool ArtifactExist = FindNear(Entity, Worlds[ActualWorld].Artifacts, out ArtifactID);

            //pokud artefakt existuje budeme se modlit
            if (ArtifactExist == true)
            {
                //pomodlíme se a vzroste tux power
                Entity.TuxPower++;

                //před smazáním artefaktu vytiskne na jeho místo nulu, jinak se kvůli odstranění blikání artefakt z obrazovky nesmaže
                ConsoleStuffs.TextPrint(' ', Worlds[ActualWorld].Artifacts[ArtifactID].PositionY, Worlds[ActualWorld].Artifacts[ArtifactID].PositionX);

                //smažeme artefakt
                Worlds[ActualWorld].Artifacts.RemoveAt(ArtifactID);

                //nějaký komentář
                switch (ConsoleStuffs.GetRandomNumber(1, 3))
                {
                    case 1: Messenger.Update("Cítíš jak tvá síla tuxe roste!"); break;
                    case 2: Messenger.Update("Stáváš se mocnějším než sám Bill!"); break;
                    case 3: Messenger.Update("Svatý Linus ti žehná!"); break;
                    default: break; //tohle by se stát nemělo ale co už... 
                }

                //a nakonec ještě přidáme pár expů
                bool lvlup = Entity.AddExp(ConsoleStuffs.GetRandomNumber(Entity.Level * 1, Entity.Level * 3));
                
                //pokud jsem se při tom lvlnul tak to taky zahlásím
                if (lvlup == true) Messenger.Append("A ještě jsi dal level!");
            }
            //nejsem u artefakktu
            else
            {
                //taky nějaký komentář situace
                switch (ConsoleStuffs.GetRandomNumber(1, 3))
                {
                    case 1: Messenger.Update("Po artefaktu ani stopy..."); break;
                    case 2: Messenger.Update("Svatý Linus k tobě promlouvá pouze skrze artefakty."); break;
                    case 3: Messenger.Update("Cítíš se osamělý... žádný vnitřní hlas... nic..."); break;
                    default: break; //tohle by se stát nemělo ale co už... 
                }
            }

        }

        /// <summary>
        /// Metoda kterou hráč nainstaluje linux na server.
        /// </summary>
        /// <param name="Entity">Entita hráče.</param>
        private void InstallNearServer(Player Entity)
        {
            //najdu blízkoý server a pokud existují uložím si jeho ID
            int ServerID;
            bool ServerExist = FindNear(Entity, Worlds[ActualWorld].Servers, out ServerID);

            //pokud server existuje bude se instalovat
            if (ServerExist == true)
            {
                //zkontroluji jestli mám dost many pro instalaci
                if (Entity.TuxPower >= Worlds[ActualWorld].Servers[ServerID].PowerNeeded)
                {
                    //zkontroluji jestli už není linux instalován
                    if (Worlds[ActualWorld].Servers[ServerID].Installed == false)
                    {
                        //nainstaluji na server linux
                        LinuxServerCount++;
                        Worlds[ActualWorld].Servers[ServerID].Installed = true;

                        //nějaký komentář
                        switch (ConsoleStuffs.GetRandomNumber(1, 3))
                        {
                            case 1: Messenger.Update("Požehnaný budiž tento server!"); break;
                            case 2: Messenger.Update("Buď již linux!"); break;
                            case 3: Messenger.Update("Vidíš boot log, Linus je šťastný!"); break;
                            default: break; //tohle by se stát nemělo ale co už... 
                        }

                        //a nakonec ještě přidáme pár expů
                        bool lvlup = Entity.AddExp(ConsoleStuffs.GetRandomNumber(Entity.Level * 2, Entity.Level * 4));

                        //pokud jsem se při tom lvlnul tak to taky zahlásím
                        if (lvlup == true) Messenger.Append("A ještě jsi dal level!");
                    }
                    //pokud je linux již instalován
                    else Messenger.Update("Tady už linux šlape.");
                }
                //pokud dost many nemám
                else Messenger.Update("Na tohle ještě nemáš dost síly tuxe.");
            }
            //nejsem u serveru
            else
            {
                //taky nějaký komentář situace
                switch (ConsoleStuffs.GetRandomNumber(1, 3))
                {
                    case 1: Messenger.Update("Po serveru ani stopy..."); break;
                    case 2: Messenger.Update("Instalační médium tě svrbí v kapse."); break;
                    case 3: Messenger.Update("Kde nic, tu nic, žádný server."); break;
                    default: break; //tohle by se stát nemělo ale co už... 
                }
            }

        }

//------------------------------------------------------------------------------------------------------
//      Následují funkce pro práci s vykreslováním entit a informací.
//------------------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------------
        // Pomocné funce pro vykreslování jednotlivých částí okna

        /// <summary>
        /// Metoda pro vykreslení informací o hráčově postavě v dolní části obrazovky.
        /// </summary>
        private void DrawPlayerInfo()
        {
            string StatusText = "HP: "+ player.HealthPoint.ToString() +"/"+ player.MaximumHealthPoint.ToString() + "  LVL: "+ player.Level.ToString() +"  EXP: "+ player.Experiences.ToString() + "/" + player.ExperiencesForNextLevel.ToString() ;
            StatusText += "  Síla tuxe: " + player.TuxPower.ToString()+"/10" + "  Serverů: " + LinuxServerCount.ToString() + "/10";
            ConsoleStuffs.TextPrint(StatusText, 28, 3);
            for (int i = 0; i < 89; i++)
            {
                ConsoleStuffs.TextPrint('#', 27, i);
            }
        }

        /// <summary>
        /// Vykreslení entity do okna.
        /// </summary>
        /// <param name="Entity">Objekt entity pro vytisknutí.</param>
        private void DrawPlayer()
        {
            //tohle je jen vlastně malý hack pro zvýšení čitelnosti kódu
            ConsoleStuffs.TextPrint(player.BodyChar, player.PositionY, player.PositionX, player.ColorForPrint);
        }

        /// <summary>
        /// Metoda která vykreslí všechny načtené zdi.
        /// </summary>
        private void DrawWalls()
        {
            //počítadlo zdí
            int ActualWallNumber = 0;
            //projdu všechny zdi uložené v listu
            while (ActualWallNumber < Worlds[ActualWorld].Walls.Count())
            {
                ConsoleStuffs.TextPrint(Worlds[ActualWorld].Walls[ActualWallNumber].BodyChar, Worlds[ActualWorld].Walls[ActualWallNumber].PositionY, Worlds[ActualWorld].Walls[ActualWallNumber].PositionX, Worlds[ActualWorld].Walls[ActualWallNumber].ColorForPrint);
                ActualWallNumber++;
            }
        }

        /// <summary>
        /// Metoda která vykreslí všechny načtené dveře.
        /// </summary>
        private void DrawDoors()
        {
            //počítadlo dveří
            int ActualDoorNumber = 0;
            //projdu všechny veře uložené v listu
            while (ActualDoorNumber < Worlds[ActualWorld].Doors.Count())
            {
                ConsoleStuffs.TextPrint(Worlds[ActualWorld].Doors[ActualDoorNumber].BodyChar, Worlds[ActualWorld].Doors[ActualDoorNumber].PositionY, Worlds[ActualWorld].Doors[ActualDoorNumber].PositionX, Worlds[ActualWorld].Doors[ActualDoorNumber].ColorForPrint);
                ActualDoorNumber++;
            }
        }

        /// <summary>
        /// Metoda která vykreslí všechny načtené moby.
        /// </summary>
        private void DrawMobs()
        {
            //počítadlo mobů
            int ActualMobNumber = 0;
            //projdu všechny moby uložené v listu
            while (ActualMobNumber < Worlds[ActualWorld].Mobs.Count())
            {
                ConsoleStuffs.TextPrint(Worlds[ActualWorld].Mobs[ActualMobNumber].BodyChar, Worlds[ActualWorld].Mobs[ActualMobNumber].PositionY, Worlds[ActualWorld].Mobs[ActualMobNumber].PositionX, Worlds[ActualWorld].Mobs[ActualMobNumber].ColorForPrint);
                ActualMobNumber++;
            }
        }

        /// <summary>
        /// Metoda která vykreslí všechny načtené stromy.
        /// </summary>
        private void DrawTrees()
        {
            //počítadlo zdí
            int ActualTreeNumber = 0;
            //projdu všechny zdi uložené v listu
            while (ActualTreeNumber < Worlds[ActualWorld].Trees.Count())
            {
                ConsoleStuffs.TextPrint(Worlds[ActualWorld].Trees[ActualTreeNumber].BodyChar, Worlds[ActualWorld].Trees[ActualTreeNumber].PositionY, Worlds[ActualWorld].Trees[ActualTreeNumber].PositionX, Worlds[ActualWorld].Trees[ActualTreeNumber].ColorForPrint);
                ActualTreeNumber++;
            }
        }

        /// <summary>
        /// Metoda která vykreslí všechny načtené kopce.
        /// </summary>
        private void DrawHills()
        {
            //počítadlo zdí
            int ActualHillNumber = 0;
            //projdu všechny zdi uložené v listu
            while (ActualHillNumber < Worlds[ActualWorld].Hills.Count())
            {
                ConsoleStuffs.TextPrint(Worlds[ActualWorld].Hills[ActualHillNumber].BodyChar, Worlds[ActualWorld].Hills[ActualHillNumber].PositionY, Worlds[ActualWorld].Hills[ActualHillNumber].PositionX, Worlds[ActualWorld].Hills[ActualHillNumber].ColorForPrint);
                ActualHillNumber++;
            }
        }

        /// <summary>
        /// Metoda která vykreslí všechny načtené vody.
        /// </summary>
        private void DrawWatter()
        {
            //počítadlo zdí
            int ActualWatterNumber = 0;
            //projdu všechny zdi uložené v listu
            while (ActualWatterNumber < Worlds[ActualWorld].Watters.Count())
            {
                ConsoleStuffs.TextPrint(Worlds[ActualWorld].Watters[ActualWatterNumber].BodyChar, Worlds[ActualWorld].Watters[ActualWatterNumber].PositionY, Worlds[ActualWorld].Watters[ActualWatterNumber].PositionX, Worlds[ActualWorld].Watters[ActualWatterNumber].ColorForPrint);
                ActualWatterNumber++;
            }
        }

        /// <summary>
        /// Metoda pro vykreslení všech načtených bran.
        /// </summary>
        private void DrawGates()
        {
            //počítadlo bran
            int ActualGateNumber = 0;
            //projdu všechny brány uložené v listu
            while (ActualGateNumber < Worlds[ActualWorld].Gates.Count())
            {
                ConsoleStuffs.TextPrint(Worlds[ActualWorld].Gates[ActualGateNumber].BodyChar, Worlds[ActualWorld].Gates[ActualGateNumber].PositionY, Worlds[ActualWorld].Gates[ActualGateNumber].PositionX, Worlds[ActualWorld].Gates[ActualGateNumber].ColorForPrint);
                ActualGateNumber++;
            }

        }

        /// <summary>
        /// Metoda pro vykreslení všech načtených bran.
        /// </summary>
        private void DrawServers()
        {
            //počítadlo bran
            int ActualServerNumber = 0;
            //projdu všechny brány uložené v listu
            while (ActualServerNumber < Worlds[ActualWorld].Servers.Count())
            {
                ConsoleStuffs.TextPrint(Worlds[ActualWorld].Servers[ActualServerNumber].BodyChar, Worlds[ActualWorld].Servers[ActualServerNumber].PositionY, Worlds[ActualWorld].Servers[ActualServerNumber].PositionX, Worlds[ActualWorld].Servers[ActualServerNumber].ColorForPrint);
                ActualServerNumber++;
            }

        }

        /// <summary>
        /// Metoda pro vykreslení všech načtených artefaktů.
        /// </summary>
        private void DrawArtifacts()
        {
            //počítadlo bran
            int ActualArtifactNumber = 0;
            //projdu všechny brány uložené v listu
            while (ActualArtifactNumber < Worlds[ActualWorld].Artifacts.Count())
            {
                ConsoleStuffs.TextPrint(Worlds[ActualWorld].Artifacts[ActualArtifactNumber].BodyChar, Worlds[ActualWorld].Artifacts[ActualArtifactNumber].PositionY, Worlds[ActualWorld].Artifacts[ActualArtifactNumber].PositionX, Worlds[ActualWorld].Artifacts[ActualArtifactNumber].ColorForPrint);
                ActualArtifactNumber++;
            }

        }

        //----------------------------------------------------------------------------------------------
        // Funkce pro promazání starých pozic.

        /// <summary>
        /// Smaže znak hráče na steré pozici
        /// </summary>
        private void CleanPlayer()
        {
            ConsoleStuffs.TextPrint(' ', player.OldPositionY, player.OldPositionX);
        }

        /// <summary>
        /// Smaže znaky mobů na starých pozicích
        /// </summary>
        private void CleanMobs()
        {
            //počítadlo mobů
            int ActualMobNumber = 0;
            //projdu všechny moby uložené v listu
            while (ActualMobNumber < Worlds[ActualWorld].Mobs.Count())
            {
                ConsoleStuffs.TextPrint(' ', Worlds[ActualWorld].Mobs[ActualMobNumber].OldPositionY, Worlds[ActualWorld].Mobs[ActualMobNumber].OldPositionX);
                ActualMobNumber++;
            }
        }

        /// <summary>
        /// Smaže player info
        /// </summary>
        private void CleanPlayerInfo()
        {
            ConsoleStuffs.TextPrint("                                                                                      ", 28, 3);
        }
        
        //----------------------------------------------------------------------------------------------
        // Hlavní funkce pro vykreslení okna

        /// <summary>
        /// Metoda která se postará o vykreslení celého okna hry.
        /// </summary>
        private void DrawWholeWindow()
        {
            //rozhodování jestli překleslit celé okno, jen když změním mapu
            if (ActualWorld != OldActualWorld)
            {
                Console.Clear();
                ConsoleStuffs.DrawFrame();
                DrawWalls();
                DrawDoors();
                DrawMobs();
                DrawTrees();
                DrawHills();
                DrawWatter();
                DrawPlayer();
                DrawPlayerInfo();
                DrawGates();
                DrawServers();
                DrawArtifacts();
                Messenger.Refresh();
                //toto se pak odkomentuje
                OldActualWorld = ActualWorld;
            }
            else
            {
                //tady se jen překleslí mobové, hráč, dveře, messenger a status bar
                CleanPlayer();
                CleanMobs();
                CleanPlayerInfo();
                
                DrawDoors();
                DrawPlayer();
                DrawMobs();
                DrawPlayerInfo();
                DrawArtifacts();

                Messenger.Refresh();
            }

        }

//------------------------------------------------------------------------------------------------------
//      Následují funkce pro práci s pohybem entit ve hře.
//------------------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------------
        // Pomocné funce pro pohyb určité entity určitým směrem.

        /// <summary>
        /// Metoda pro posunutí entity určitým směrem.
        /// </summary>
        /// <param name="Entity">Objekt entity který bude posouván.</param>
        /// <param name="Delta_X">Posunití v ose X (v řádku). Musí náležet {-1, 0, 1}.</param>
        /// <param name="Delta_Y">Posunití v ose Y (ve sloupci). Musí náležet {-1, 0, 1}.</param>
        public void EntityMove(Player Entity, int Delta_Y, int Delta_X)
        {
            //ošetření proti posuvu o více než jedno políčko a to jen vertikálně či horizontálně
            if ((Delta_X >= -1 && Delta_X <= 1) && (Delta_Y >= -1 && Delta_Y <= 1) && ((Delta_X != 0 && Delta_Y == 0) || (Delta_Y != 0 && Delta_X == 0)))
            {
                // ošetření aby se nešlo přez okraj obrazovky (89 a 27 je špinavý hack proti posunutí přez 
                // levý a spodní okraj)
                if (Entity.PositionX + Delta_X < 89 && Entity.PositionX + Delta_X > 0)
                {
                    //stará pozice se ukládá aby se mohla entita inteligentně překleslit a neblikalo to
                    Entity.OldPositionX = Entity.PositionX;
                    Entity.PositionX = Entity.PositionX + Delta_X;
                }
                if (Entity.PositionY + Delta_Y < 27 && Entity.PositionY + Delta_Y > 2)
                {
                    //stará pozice se ukládá aby se mohla entita inteligentně překleslit a neblikalo to
                    Entity.OldPositionY = Entity.PositionY;
                    Entity.PositionY = Entity.PositionY + Delta_Y;
                }
            }
        }

        /// <summary>
        /// Metoda pro posunutí entity určitým směrem.
        /// </summary>
        /// <param name="Entity">Objekt entity který bude posouván.</param>
        /// <param name="Delta_X">Posunití v ose X (v řádku). Musí náležet {-1, 0, 1}.</param>
        /// <param name="Delta_Y">Posunití v ose Y (ve sloupci). Musí náležet {-1, 0, 1}.</param>
        public void EntityMove(NPC Entity, int Delta_Y, int Delta_X)
        {
            //ošetření proti posuvu o více než jedno políčko a to jen vertikálně či horizontálně
            if ((Delta_X >= -1 && Delta_X <= 1) && (Delta_Y >= -1 && Delta_Y <= 1) && ((Delta_X != 0 && Delta_Y == 0) || (Delta_Y != 0 && Delta_X == 0)))
            {
                // ošetření aby se nešlo přez okraj obrazovky (89 a 29 je špinavý hack proti posunutí přez 
                // levý a spodní okraj)
                if (Entity.PositionX + Delta_X < 89 && Entity.PositionX + Delta_X > 0)
                {
                    //stará pozice se ukládá aby se mohla entita inteligentně překleslit a neblikalo to
                    Entity.OldPositionX = Entity.PositionX;
                    Entity.PositionX = Entity.PositionX + Delta_X;
                }
                if (Entity.PositionY + Delta_Y < 27 && Entity.PositionY + Delta_Y > 2)
                {
                    //stará pozice se ukládá aby se mohla entita inteligentně překleslit a neblikalo to
                    Entity.OldPositionY = Entity.PositionY;
                    Entity.PositionY = Entity.PositionY + Delta_Y;
                }
            }
        }

        //----------------------------------------------------------------------------------------------
        // Pomocné funce pro otestování kolizí mezi dvouma objektama.

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt. Typicky zeď.
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(Player MovingEntity, Wall StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt. Typicky zeď.
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, Wall StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt dveří
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(Player MovingEntity, Door StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (!(StandingEntity.IsOpen) && NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (!(StandingEntity.IsOpen) && MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (!(StandingEntity.IsOpen) && MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt dveří
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, Door StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (!(StandingEntity.IsOpen) && NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (!(StandingEntity.IsOpen) && MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (!(StandingEntity.IsOpen) && MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt moba.
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(Player MovingEntity, NPC StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda dvě entity mobů nejdou mezi sebou v kolizi.
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, NPC StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda entita moba nenaráží do hráče.
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, Player StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt stromu
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(Player MovingEntity, Tree StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt stromu
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, Tree StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt vody
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(Player MovingEntity, Watter StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt vody
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, Watter StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt kopce
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(Player MovingEntity, Hill StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt kopce
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, Hill StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt brány
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(Player MovingEntity, Gate StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt brány
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, Gate StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt artefaktu
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(Player MovingEntity, Artifact StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt artefaktu
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, Artifact StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt serveru
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(Player MovingEntity, Server StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        /// <summary>
        /// Ověří zda v pohybu zadaným směrem nepřekáží zadaný objekt serveru
        /// </summary>
        /// <param name="MovingEntity">Entita která se bude pohybovat.</param>
        /// <param name="StandingEntity">Entita do které by se mohlo narazit.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <returns>True pokud stojící entita překáží v plánovaném pohybu.</returns>
        private bool IsCollision(NPC MovingEntity, Server StandingEntity, int MoveY, int MoveX)
        {
            //propočtení výsledných souřadnic pro pohyb
            int NextX = MovingEntity.PositionX + MoveX;
            int NextY = MovingEntity.PositionY + MoveY;

            //zkontrolování kolize a vrácení příslušné hodnoty
            if (NextX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveX == 0 && MovingEntity.PositionX == StandingEntity.PositionX && NextY == StandingEntity.PositionY) return true;
            else if (MoveY == 0 && MovingEntity.PositionY == StandingEntity.PositionY && NextX == StandingEntity.PositionX) return true;
            else return false;
        }

        //----------------------------------------------------------------------------------------------
        // Pomocné funce kontrolu kolizí mezi určitou entitou a celým listem dalších objektů.

        /// <summary>
        /// Testuje všechny zdi ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaká zeď překáží v pohybu.</returns>
        private bool Collision(Player MovingEntity, List<Wall> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny zdi ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaká zeď překáží v pohybu.</returns>
        private bool Collision(NPC MovingEntity, List<Wall> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny dveře ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaké dveře překáží v pohybu.</returns>
        private bool Collision(Player MovingEntity, List<Door> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny dveře ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaké dveře překáží v pohybu.</returns>
        private bool Collision(NPC MovingEntity, List<Door> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny NPC ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaké NPC překáží v pohybu.</returns>
        private bool Collision(Player MovingEntity, List<NPC> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny NPC ve světě jestli do nich vybrané NPC nenaráží.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <param name="NumberOfMovingEntity">ID entity v listu které se hýbe.</param>
        /// <returns>Vrací true pokud mi nějaké NPC překáží v pohybu.</returns>
        private bool Collision(NPC MovingEntity, List<NPC> StandingEntities, int MoveY, int MoveX, int NumberOfMovingEntity)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //je zapotřebí vyřadit z kontrolování případ kdy kontroluju sebe sám se sebou
                if (ActualElementNumber == NumberOfMovingEntity)
                {
                    ActualElementNumber = ActualElementNumber + 2;
                }
                else
                {
                    //pokud najdu zeď do které bych měl vrazit tak vrátím true
                    if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                    //jináč pokračuji v procházení
                    ActualElementNumber++;
                }
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny stromy ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaký strom překáží v pohybu.</returns>
        private bool Collision(Player MovingEntity, List<Tree> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny stromy ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaký strom překáží v pohybu.</returns>
        private bool Collision(NPC MovingEntity, List<Tree> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny vody ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaká voda překáží v pohybu.</returns>
        private bool Collision(Player MovingEntity, List<Watter> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny vody ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaká voda překáží v pohybu.</returns>
        private bool Collision(NPC MovingEntity, List<Watter> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny vody ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaký kopec překáží v pohybu.</returns>
        private bool Collision(Player MovingEntity, List<Hill> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny kopce ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaký kopec překáží v pohybu.</returns>
        private bool Collision(NPC MovingEntity, List<Hill> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny brány ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaká brána překáží v pohybu.</returns>
        private bool Collision(Player MovingEntity, List<Gate> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny bány ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaká brána překáží v pohybu.</returns>
        private bool Collision(NPC MovingEntity, List<Gate> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny artefakty ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaký artefakt překáží v pohybu.</returns>
        private bool Collision(Player MovingEntity, List<Artifact> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny artefakty ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaký artefakt překáží v pohybu.</returns>
        private bool Collision(NPC MovingEntity, List<Artifact> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny servery ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaký server překáží v pohybu.</returns>
        private bool Collision(Player MovingEntity, List<Server> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        /// <summary>
        /// Testuje všechny servery ve světě jestli do nich nenarážím.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou pohybuju.</param>
        /// <param name="StandingEntities">List entit vůči kterým testuju. </param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>Vrací true pokud mi nějaký server překáží v pohybu.</returns>
        private bool Collision(NPC MovingEntity, List<Server> StandingEntities, int MoveY, int MoveX)
        {
            //počítadlo zdí
            int ActualElementNumber = 0;
            //projdu všechny objekty uložené v listu
            while (ActualElementNumber < StandingEntities.Count())
            {
                //pokud najdu zeď do které bych měl vrazit tak vrátím true
                if (IsCollision(MovingEntity, StandingEntities[ActualElementNumber], MoveY, MoveX)) return true;
                //jináč pokračuji v procházení
                ActualElementNumber++;
            }
            //no a pokud nic nenajdu tak vrátím false
            return false;
        }

        //----------------------------------------------------------------------------------------------
        // Hlavní funkce pro pohyb entit.

        /// <summary>
        /// Metoda pro pohyb s entitou hráče. Pohne se pouze pokud je pohyb možný.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou je třeba hýbat.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>True pokud se pohyb entity povedl.</returns>
        private bool MoveIfPossible(Player MovingEntity, int MoveY, int MoveX)
        {
            //pokud můžu tak se pohnu
            if (
                !(Collision(MovingEntity, Worlds[ActualWorld].Walls, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Doors, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Mobs, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Trees, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Watters, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Hills, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Artifacts, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Servers, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Gates, MoveY, MoveX))
               )
            {
                EntityMove(MovingEntity, MoveY, MoveX);
                return true;
            }

            //pohyb není možný kvůli zdi - napíšu si nějakou zprávičku :)
            if (Collision(MovingEntity, Worlds[ActualWorld].Walls, MoveY, MoveX))
            {
                switch (ConsoleStuffs.GetRandomNumber(1, 3)) {
                    case 1: Messenger.Update("Pláác! Ozvala se dutá rána jak jsi se naplácl na zeď!"); break;
                    case 2: Messenger.Update("Rozeběhl jsi se proti zdi a zlomil jsi si nos. Bolííí!"); break;
                    case 3: Messenger.Update("Buch! Ještě párkrát a místo zdi tu budou dveře!"); break;
                    default: break; //tohle by se stát nemělo ale co už... 
                }
            }

            //pohyb není možný kvůli dveřím
            if (Collision(MovingEntity, Worlds[ActualWorld].Doors, MoveY, MoveX))
            {
                switch (ConsoleStuffs.GetRandomNumber(1, 3)) {
                    case 1: Messenger.Update("Buch! Buch! Buch! Asi není nikdo doma..."); break;
                    case 2: Messenger.Update("Hele, ono to možná bude chtít klíč..."); break;
                    case 3: Messenger.Update("Hmmmmm... dveře... jestli-pak mají kliku?"); break;
                    default: break; //toto by taky nemělo nastat
                }
            }

            //pohyb není možný kvůli stromu
            if (Collision(MovingEntity, Worlds[ActualWorld].Trees, MoveY, MoveX))
            {
                switch (ConsoleStuffs.GetRandomNumber(1, 3))
                {
                    case 1: Messenger.Update("Takto z tebe dřevorubec nebude."); break;
                    case 2: Messenger.Update("Já do lesa ... nepojedu ... já do lesa ... nepůjdu..."); break;
                    case 3: Messenger.Update("Zatracenej strom!"); break;
                    default: break; //toto by taky nemělo nastat
                }
            }

            //pohyb není možný kvůli vodě
            if (Collision(MovingEntity, Worlds[ActualWorld].Watters, MoveY, MoveX))
            {
                switch (ConsoleStuffs.GetRandomNumber(1, 3))
                {
                    case 1: Messenger.Update("No ... a vypadám snak jako ryba? Já vím že jsem jen zavináč ale stejně..."); break;
                    case 2: Messenger.Update("A pojď mi hop!"); break;
                    case 3: Messenger.Update("Né, já nechci být mokrej. A vůbec ... plav si sám..."); break;
                    default: break; //toto by taky nemělo nastat
                }
            }

            //pohyb není  možný kvůli kopci
            if (Collision(MovingEntity, Worlds[ActualWorld].Hills, MoveY, MoveX))
            {
                switch (ConsoleStuffs.GetRandomNumber(1, 3))
                {
                    case 1: Messenger.Update("Fía ... to je ale kopec, víš ale že mám strach z výšek vid?"); break;
                    case 2: Messenger.Update("Sem nepolezu. <naštvané mrmlání>"); break;
                    case 3: Messenger.Update("Hmm ... nahoře určitě žije Odula!"); break;
                    default: break;
                }
            }

            //pohyb není  možný kvůli artefaktu
            if (Collision(MovingEntity, Worlds[ActualWorld].Artifacts, MoveY, MoveX))
            {
                switch (ConsoleStuffs.GetRandomNumber(1, 3))
                {
                    case 1: Messenger.Update("No krucinál, ještě se tady o to zabiju!"); break;
                    case 2: Messenger.Update("Hele, do čeho jsem to teď kopl?"); break;
                    case 3: Messenger.Update("<cink> He? Co to?"); break;
                    default: break;
                }
            }

            //pohyb není  možný kvůli serveru
            if (Collision(MovingEntity, Worlds[ActualWorld].Servers, MoveY, MoveX))
            {
                switch (ConsoleStuffs.GetRandomNumber(1, 3))
                {
                    case 1: Messenger.Update("No krucinál, ještě se tady o to zabiju!"); break;
                    case 2: Messenger.Update("Hele? Vždyť je to server!"); break;
                    case 3: Messenger.Update("<kchhrrr> Asi bych do toho neměl kopat hele..."); break;
                    default: break;
                }
            }

            //tohle je výstup pokud se pohyb nepovedl
            return false;
        }

        /// <summary>
        /// Metoda pro pohyb s entitou NPC. Pohne se pouze pokud je pohyb možný.
        /// </summary>
        /// <param name="MovingEntity">Entita se kterou je třeba hýbat.</param>
        /// <param name="MoveY">Plánovaný pohyb v ose Y.</param>
        /// <param name="MoveX">Plánovaný pohyb v ose X.</param>
        /// <returns>True pokud se pohyb entity povedl.</returns>
        private bool MoveIfPossible(NPC MovingEntity, int MoveY, int MoveX, int MobNumber)
        {
            if (
                !(Collision(MovingEntity, Worlds[ActualWorld].Walls, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Doors, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Mobs, MoveY, MoveX, MobNumber)) &&
                !(IsCollision(MovingEntity, player, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Trees, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Watters, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Hills, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Artifacts, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Servers, MoveY, MoveX)) &&
                !(Collision(MovingEntity, Worlds[ActualWorld].Gates, MoveY, MoveX))
               )
            {
                EntityMove(MovingEntity, MoveY, MoveX);
                return true;
            }
            return false;
        }

        //----------------------------------------------------------------------------------------------
        // Funkce umělé inteligence pro pohyb mobů.

        /// <summary>
        /// Umělá inteligence řídící pohyb NPC postavy.
        /// </summary>
        /// <param name="Entity">Entita kterou bude UI řídit.</param>
        /// <param name="PlayerEntity">Entita hráče, pokud je UI zlá, tj mob je agresivní, půjde UI po této entitě.</param>
        private void UI_Move(NPC Entity, Player PlayerEntity, int MobNumber)
        {
            // když máme zlou entitu tak budeme zlí
            if (Entity.IsEvil)
            {
                //pokud je hráč v dosahu tak tak se za ním rozeběhnu, pokud není tak se zasejc nečinně poflakuju
                if (IsInAgresiveRadius(Entity, PlayerEntity)) MoveToTarget(Entity, PlayerEntity, MobNumber);
                else RandomMove(Entity, MobNumber);
            }
            //hodná entita = hodná UI -> nečině se poflakovat kolem :) 
            else RandomMove(Entity, MobNumber);
        }

        /// <summary>
        /// Metoda pro výpočet vzdálenosti mezi entitami.
        /// </summary>
        /// <param name="Entity">Entita moba.</param>
        /// <param name="PlayerEntity">Entita hráče.</param>
        /// <returns>Vzdálenost mezi entitami (udávaná ve světelných letech ^ 2).</returns>
        private int CalcDistance(NPC Entity, Player PlayerEntity)
        {
            //pomocí pythagorovy věty vypočítám přeponu trojúhelníka který jest tvořen rozdílem v souřadnicíh moba a hráče
            double DeltaX = Math.Abs(Entity.PositionX - PlayerEntity.PositionX);
            double DeltaY = Math.Abs(Entity.PositionY - PlayerEntity.PositionY);
            return (int)Math.Sqrt(Math.Pow(DeltaX, 2) + Math.Pow(DeltaY, 2));
        }

        /// <summary>
        /// Výpočet zorného úhlu mezi entitou a hráčem. Vrátí úhel po kterém se entity musí vydat aby se dostala k hráči.
        /// </summary>
        /// <param name="Entity">Entita příšerky pro kterou chci úhel vypočítat.</param>
        /// <param name="PlayerEntity">Entita hráče.</param>
        /// <returns>Uhel ve vstupních vztažen ke kladné části osy X.</returns>
        private int CalcAngle(NPC Entity, Player PlayerEntity)
        {
            //rozdíly ve vzdálenosti entit, pozor není to souřadnice vektoru
            double DeltaX = Math.Abs(Entity.PositionX - PlayerEntity.PositionX);
            double DeltaY = Math.Abs(Entity.PositionY - PlayerEntity.PositionY);

            // znaménka jednotlivých rozdílů
            int SignDeltaX;
            int SignDeltaY;

            if (Entity.PositionX > PlayerEntity.PositionX) SignDeltaX = -1;
            else if (Entity.PositionX < PlayerEntity.PositionX) SignDeltaX = 1;
            else SignDeltaX = 0;

            if (Entity.PositionY > PlayerEntity.PositionY) SignDeltaY = -1;
            else if (Entity.PositionY < PlayerEntity.PositionY) SignDeltaY = 1;
            else SignDeltaY = 0;
            
            //tuten bude mezivýpočet
            double Fi = 0;

            if (SignDeltaX == 1 && SignDeltaY == 1)
            {
                //1. kvadrant
                Fi = Math.Atan(Math.Abs(DeltaY) / Math.Abs(DeltaX));
                return (int)(Fi * 180 / Math.PI);
            }
            else if (SignDeltaX == -1 && SignDeltaY == 1)
            {
                //2. kvadrant
                Fi = Math.Atan(Math.Abs(DeltaY) / Math.Abs(DeltaX));
                return (int)(180 - (Fi * 180 / Math.PI));
            }
            else if (SignDeltaX == -1 && SignDeltaY == -1)
            {
                //3. kvadrant
                Fi = Math.Atan(Math.Abs(DeltaY) / Math.Abs(DeltaX));
                return (int)(180 + (Fi * 180 / Math.PI));
            }
            else if (SignDeltaX == 1 && SignDeltaY == -1)
            {
                //4. kvadrant
                Fi = Math.Atan(Math.Abs(DeltaY) / Math.Abs(DeltaX));
                return (int)(360 - (Fi * 180 / Math.PI));
            }
            else if (SignDeltaX == 0)
            {
                if (SignDeltaY == 1) return 90;
                else if (SignDeltaY == -1) return 270;
                else return 0;                                  //there is some strange bug, nemělo by nikdy nastat 
            }
            else if (SignDeltaY == 0)
            {
                if (SignDeltaX == 1) return 0;
                else if (SignDeltaX == -1) return 180;
                else return 0;                                  // taky by němělo nastat (znamenalo by to že jsou entity přez sebe
            }
            else return 0;                                      // inu, tohle taky
        }

        /// <summary>
        /// Pohne s mobem náhodně kam to jen půjde. To je to "nečinně se poflakovat kolem :)".
        /// </summary>
        /// <param name="Entity">S touhle entitou se hne.</param>
        /// <param name="MobNumber">Tohle je číslo moba v listu, potřeba, použije se v kontrolování kolize mobů.</param>
        private void RandomMove(NPC Entity, int MobNumber)
        {
            //sem se uloží směr kterým jsem se rozhodl vydat
            int direction;

            //výsledek po zavolání metody pro pohyb entity, obsahuje informaci o tomjestli jsem se zkutečně pohnul
            bool Moved;

            //počítadlo iterací, pokud se do určitého počtu cyklů nepohnu jest třeba to řešit páč se mob nějak divně sekl
            int Iteration = 0;

            //tento první cyklus opakuji tak dlouho dokud se mi pohyb nepovede
            do
            {
                //tyhle proměnné snad nepotřebují komentář
                int NextX = Entity.PositionX;
                int NextY = Entity.PositionY;
                int DeltaX = 0;
                int DeltaY = 0;

                //sem se uloží výsledek zkoumání jestli další krok entitu nevyvede z dovoleného rozsahu pohybu
                bool IsInRadius = false;
                //tento cyklus najde takový směr ve kterém zůstanu ve svém dovoleném radiusu
                do
                {
                    //mob se nejspíše zasekl po odvedeního mimo jeho oblast - tohle je HACK změním totiž jeho počáteční pozice na tu kde právě stojí a bude zas volný :)
                    if (Iteration > 10)
                    {
                        Entity.BasePositionX = Entity.PositionX;
                        Entity.BasePositionY = Entity.PositionY;
                    }

                    //volba směru ... c# tohle má docela pitomě, jestli chcete čísla od jedné do čtyř, musíte napsat od jedné do pěti, ale moje metoda to opravduje ;)
                    direction = ConsoleStuffs.GetRandomNumber(1, 4);

                    //výpočet budoucích souřadnic
                    switch (direction)
                    {
                        case 1: NextX = Entity.PositionX + 1; DeltaX = 1; break;
                        case 2: NextX = Entity.PositionX - 1; DeltaX = -1; break;
                        case 3: NextY = Entity.PositionY + 1; DeltaY = 1; break;
                        case 4: NextY = Entity.PositionY - 1; DeltaY = -1; break;
                        default: break;
                    }

                    //testování jestli zůstanu ve svém radiusu
                    if (Entity.BasePositionX + Entity.MaximalFreeMoveRadius > NextX &&
                        Entity.BasePositionX - Entity.MaximalFreeMoveRadius < NextX &&
                        Entity.BasePositionY + Entity.MaximalFreeMoveRadius > NextY &&
                        Entity.BasePositionY - Entity.MaximalFreeMoveRadius < NextY)
                    {
                        IsInRadius = true;
                    }
                    
                    // počítám iterace
                    Iteration++;
                } while (!IsInRadius);
                
                //pokusím se pohnout, udžuji si výsledek
                Moved = MoveIfPossible(Entity, DeltaY, DeltaX, MobNumber);

            } while (!Moved);

        }

        /// <summary>
        /// Zkontroluje jestli je hráč v agresivním dosahu moba.
        /// </summary>
        /// <param name="Entity">Agresivní mob.</param>
        /// <param name="PlayerEntity">Hráč.</param>
        /// <returns>True pokud je v dosahu.</returns>
        private bool IsInAgresiveRadius(NPC Entity, Player PlayerEntity)
        {
            int Distance = CalcDistance(Entity, PlayerEntity);
            if (Distance > Entity.AgressiveRadius) return false;
            else return true;
        }
        
        /// <summary>
        /// Metoda pro pohyb entitou vstříc hráči.
        /// </summary>
        /// <param name="Entity">NPC se kterým se pohne.</param>
        /// <param name="PlayerEntity">Hráč na kterého se útočí.</param>
        /// <param name="MobNumber">Číslo moba v listu. Propadá až bůh ví kam hluboko.</param>
        private void MoveToTarget(NPC Entity, Player PlayerEntity, int MobNumber)
        {
            //vypočtu úhel pod kterým mám hráče
            int Angle = CalcAngle(Entity, PlayerEntity);

            //pohnu se podle daného úhlu (btw. ty magické konstanty jsou osy kvadrrantů)
            if ((Angle >= 315 && Angle <= 360) || (Angle >= 0 && Angle < 45)) MoveIfPossible(Entity, 0, 1, MobNumber);
            else if (Angle >= 45 && Angle < 135) MoveIfPossible(Entity, 1, 0, MobNumber);
            else if (Angle >= 135 && Angle < 225) MoveIfPossible(Entity, 0, -1, MobNumber);
            else if (Angle >= 225 && Angle < 315) MoveIfPossible(Entity, -1, 0, MobNumber);
        }

        //----------------------------------------------------------------------------------------------
        // Hlavní funkce pro pohyb se všema mobama.

        /// <summary>
        /// Metoda postupně pohne se všema mobama na světě.
        /// </summary>
        private void MoveAllMobs()
        {
            //počítadlo mobů
            int ActualMobNumber = 0;
            //projdu všechny moby uložené v listu
            while (ActualMobNumber < Worlds[ActualWorld].Mobs.Count())
            {
                UI_Move(Worlds[ActualWorld].Mobs[ActualMobNumber], player, ActualMobNumber);
                ActualMobNumber++;
            }
        }

//------------------------------------------------------------------------------------------------------
//      Následují funkce pro interakci entit (souboj).
//------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Vypočítá DMG která entita Master udělí entitě Slave.
        /// </summary>
        /// <param name="Master">Entita udělující poškození.</param>
        /// <param name="Slave">Entita dostávající poškození.</param>
        /// <returns>DMG kterej bude udělený.</returns>
        private int CalculateDemage(Player Master, NPC Slave)
        {
            //tenhle vzorej je více měné odhadnutý, v matlabu otestováno že funguje docela fajnově
            return (int)(0.3 * Master.HealthPoint + 0.02 * Master.Experiences + 0.4 * Master.Level);
        }

        /// <summary>
        /// Vypočítá DMG která entita Master udělí entitě Slave.
        /// </summary>
        /// <param name="Master">Entita udělující poškození.</param>
        /// <param name="Slave">Entita dostávající poškození.</param>
        /// <returns>DMG kterej bude udělený.</returns>
        private int CalculateDemage(NPC Master, Player Slave)
        {
            //tenhle vzorej je více měné odhadnutý za pomoci excelu možná stojí za nic 
            //PS. Pokud tohle čteš a máš zrovna přístup k matlabu tak si najdli lepší magické konstanty. 
            //TODO: Nainstalovat Octave!
            return (int)(0.3 * Master.HealthPoint + 0.4 * Master.Level);
        }

        /// <summary>
        /// Metoda která najde nejbližšího moba a zautočí na něj.
        /// </summary>
        /// <param name="Master">Utočící entita.</param>
        /// <param name="ListOfSlaves">List entit na které jest možno utočiti.</param>
        private void FindNearAndAttack(Player Master, List<NPC> ListOfSlaves)
        {
            //do hodnejch mobů se prostě mlátit nebude a hotovo!

            //najdu číslo moba do kterého praštím
            int FoundID = 0;
            bool Found = FindNear(player, Worlds[ActualWorld].Mobs, true, out FoundID);
            if (Found == true)
            {
                //výpočet poškození které udělím
                int demage = CalculateDemage(Master, ListOfSlaves[FoundID]);

                //udělím ránu mobovi
                bool died = ListOfSlaves[FoundID].GetDemage(demage);

                //ošetřím stav kdybych ho zabil
                if (died == true)
                {
                    // připočtu expy podle lvl moba a pokud se mi náhodou povede lvl up tak to někam napíšu
                    bool LevelUp = player.AddExp(ConsoleStuffs.GetRandomNumber(ListOfSlaves[FoundID].Level * 3, ListOfSlaves[FoundID].Level * 5));

                    // přepíšu pozici moba prázdnou
                    ConsoleStuffs.TextPrint(' ', ListOfSlaves[FoundID].PositionY, ListOfSlaves[FoundID].PositionX);

                    //mob je mrtev smazat jej z listu
                    ListOfSlaves.RemoveAt(FoundID);

                    if (LevelUp == true) Messenger.Update("Plácnutím moba jsi se levnul!");     //napíšu  něco trošku jiného
                    else Messenger.Update("Plácl jsi moba a ten to už nedal!");                 //napíšu strohý epitaf mobovi
                }
                //nezabil-li jsem jej tak jen vypíšu info 
                else Messenger.Update("Plácl jsi moba za " + demage.ToString() + " bodů!");
            }
            else Messenger.Update("Není do čeho praštit!");
        }

        /// <summary>
        /// Metoda která najde entity stojící vedle mastra a s nima na něj zautočí.
        /// </summary>
        /// <param name="ListOfMasters">List masterů kteří zautočí na entitu.</param>
        /// <param name="Slave">Do této entity se bude mlátit.</param>
        /// <returns>True pokud toto hráč nepřežije </returns>
        private bool AttackIfNear(List<NPC> ListOfMasters, Player Slave)
        {
            //hodní mobové prostě útočit nebudou a basta!

            //příprava proměnných pro ID
            int FoundID1 = 0, FoundID2 = 0, FoundID3 = 0, FoundID4 = 0;

            //sem se sčítá celkové poškození které mi mobové nandají
            int TotalDemage = 0;

            // nalezení až čtyř nejbližších mobů
            int NearCount = FindNear(Worlds[ActualWorld].Mobs, player, true, out FoundID1, out FoundID2, out FoundID3, out FoundID4);

            // zde se útočí, hledají se zde i útočící entity
            switch (NearCount){
                case 0: break; //nenalezen žádný mob, prostě se nazautočí
                case 1:
                    //jeden mob má hráče v dosahu
                    TotalDemage = CalculateDemage(Worlds[ActualWorld].Mobs[FoundID1], player);
                    break;
                case 2:
                    //dva mobové mají hráče v dosahu
                    TotalDemage = CalculateDemage(Worlds[ActualWorld].Mobs[FoundID1], player);
                    TotalDemage += CalculateDemage(Worlds[ActualWorld].Mobs[FoundID2], player);
                    break;
                case 3:
                    //tři mobové mají hráče v dosahu
                    TotalDemage = CalculateDemage(Worlds[ActualWorld].Mobs[FoundID1], player);
                    TotalDemage += CalculateDemage(Worlds[ActualWorld].Mobs[FoundID2], player);
                    TotalDemage += CalculateDemage(Worlds[ActualWorld].Mobs[FoundID3], player);
                    break;
                case 4:
                    // čtyři mobové mají hráče v dosahu
                    TotalDemage = CalculateDemage(Worlds[ActualWorld].Mobs[FoundID1], player);
                    TotalDemage += CalculateDemage(Worlds[ActualWorld].Mobs[FoundID2], player);
                    TotalDemage += CalculateDemage(Worlds[ActualWorld].Mobs[FoundID3], player);
                    TotalDemage += CalculateDemage(Worlds[ActualWorld].Mobs[FoundID4], player);
                    break;
                default: break; //tohle nikdy nenastane a je to tu jen ze slušných mravů (co kdyby to náhodou mohlo nastat?) 
            }

            //dá poškození a rozhodne se jestli hráč přežil nebo ne
            if (player.GetDemage(TotalDemage))
            {
                //hráč jest mrtev
                return true;
            }
            else
            {
                //do messengeru se bude tisknout jen když nějaké poškození existuje
                if (TotalDemage > 0) Messenger.Append("Mobové ti to nandali za " + TotalDemage + " dobů!");
                else if (TotalDemage == 0 && NearCount != 0) Messenger.Append("Mobové už se na nic nezmůžou.");
                //vrátí false protože hráč přežil
                return false;
            }

        }

//------------------------------------------------------------------------------------------------------
//      Následují všeobecné funkce.
//------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Metoda pro občasné léčení hráče. Hráč má svoje počítadlo.
        /// <param name="player">Sem potřebuji objekt hráče který bude léčen.</param>
        /// </summary>
        private void HealPlayer(Player player)
        {
            //pokud je počítadlo na správné hodnotě tak trošku vyléčím hráče.
            if (player.HealCounter == 5)
            {
                player.HealCounter = 0;
                //zároveň ale hráč musí být trošku zraněn, když bude mít max hp tak mu přeci přidávat nebudu
                if (player.HealthPoint < player.MaximumHealthPoint) player.HealthPoint++;
            }
            //pokud počítadlo není na správné hodnotě tak ho jen inkrementuji
            else player.HealCounter++;
        }

    }

    /// <summary>
    /// Třída která posléze drží svět. Obsahuje i metodu pro načtení mapy z file.
    /// </summary>
    public class World
    {

        /// <summary>
        /// Tady jest list s uloženými objekty typu zeď, které jsou ve světě rozházeny.
        /// </summary>
        public List<Wall> Walls = new List<Wall>();

        /// <summary>
        /// List držící dveře.
        /// </summary>
        public List<Door> Doors = new List<Door>();

        /// <summary>
        /// List držící moby.
        /// </summary>
        public List<NPC> Mobs = new List<NPC>();

        /// <summary>
        /// List držící stromy.
        /// </summary>
        public List<Tree> Trees = new List<Tree>();

        /// <summary>
        /// List držící vodu.
        /// </summary>
        public List<Watter> Watters = new List<Watter>();

        /// <summary>
        /// List držící kopce.
        /// </summary>
        public List<Hill> Hills = new List<Hill>();

        /// <summary>
        /// Liist držící brány mezi světy.
        /// </summary>
        public List<Gate> Gates = new List<Gate>();

        /// <summary>
        /// List držící servery.
        /// </summary>
        public List<Server> Servers = new List<Server>();

        public List<Artifact> Artifacts = new List<Artifact>();

        public int LastPositionX;

        public int LastPositionY;

        /// <summary>
        /// Tato metoda načte a parsuje soubor s mapou.
        /// </summary>
        /// <param name="FilePath">Relativní cesta k souboru.</param>
        /// <returns>True pokud se povede, false pokud načtení selže.</returns>
        public bool LoadMap(string FilePath, int DefaultX, int DefaultY, int PowerForServers)
        {
            string[] LoadedData = new string[40];

            //připravím si proměnné pro parsování
            int counter = 0;
            string line;

            //otevřu a načtu file, taky ošetřím vyjímku že by neexistoval
            try
            {
                //otevřu si file
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("game.Resources." + FilePath);
                StreamReader file = new StreamReader(stream);

                //procházím  řádek po řádku a ukládám do pole
                while ((line = file.ReadLine()) != null && counter < 100)
                {
                    LoadedData[counter] = line;
                    counter++;
                }
                //zavřu soubor
                file.Close();
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

            //tady se pak parsuje celé pole a hledají se znaky pro různé objekty, při nalezení se vytvoří
            //PositionY drží  aktuální pozici ve sloupci.
            int PositionY = 0;
            do
            {
                //tady se drží aktuální pozice na řádku
                int PositionX = 0;
                //tímto procházím celý jeden řádek
                foreach (char znak in LoadedData[PositionY])
                {
                    //testuje znaky

                    //všeobecné
                    if (znak == '#') Walls.Add(new Wall('#', PositionY, PositionX, ConsoleColor.White));
                    else if (znak == 'T') Trees.Add(new Tree('T', PositionY, PositionX, ConsoleColor.Green));
                    else if (znak == '=') Watters.Add(new Watter('=', PositionY, PositionX, ConsoleColor.Blue));
                    else if (znak == '^') Hills.Add(new Hill('^', PositionY, PositionX, ConsoleColor.DarkGray));
                    else if (znak == '$') Artifacts.Add(new Artifact('$', PositionY, PositionX, ConsoleColor.Yellow));
                    else if (znak == '%') Servers.Add(new Server('%', PositionY, PositionX, PowerForServers, ConsoleColor.Yellow));

                    //dveře
                    else if (znak == '+') Doors.Add(new Door(false, PositionY, PositionX, ConsoleColor.DarkGray));
                    else if (znak == '/') Doors.Add(new Door(true, PositionY, PositionX, ConsoleColor.DarkGray));

                    //hodní mobové
                    else if (znak == 'h') Mobs.Add(new NPC('h', PositionY, PositionX, 1, false, 5, 2 , ConsoleColor.DarkBlue)); //human
                    else if (znak == 'a') Mobs.Add(new NPC('a', PositionY, PositionX, 1, false, 5, 2, ConsoleColor.DarkBlue)); //angel
                    else if (znak == 'c') Mobs.Add(new NPC('c', PositionY, PositionX, 1, false, 5, 2, ConsoleColor.DarkBlue)); //cat
                    else if (znak == 'p') Mobs.Add(new NPC('p', PositionY, PositionX, 1, false, 5, 2, ConsoleColor.DarkBlue)); //puppy

                    //zlí mobové
                    else if (znak == 'd') Mobs.Add(new NPC('d', PositionY, PositionX, 1, true, 6, 4, ConsoleColor.Red)); // dog
                    else if (znak == 'B') Mobs.Add(new NPC('B', PositionY, PositionX, 2, true, 4, 3, ConsoleColor.Red)); //Bat
                    else if (znak == 'b') Mobs.Add(new NPC('b', PositionY, PositionX, 3, true, 5, 2, ConsoleColor.Red)); //bug
                    else if (znak == 'G') Mobs.Add(new NPC('G', PositionY, PositionX, 4, true, 6, 8, ConsoleColor.Red)); //gnol
                    else if (znak == 'g') Mobs.Add(new NPC('g', PositionY, PositionX, 5, true, 6, 7, ConsoleColor.Red)); //goblin
                    else if (znak == 'Z') Mobs.Add(new NPC('Z', PositionY, PositionX, 6, true, 5, 5, ConsoleColor.Red)); //zombie
                    else if (znak == 'A') Mobs.Add(new NPC('A', PositionY, PositionX, 7, true, 3, 6, ConsoleColor.Red)); //Archangel
                    else if (znak == 'D') Mobs.Add(new NPC('D', PositionY, PositionX, 8, true, 9, 7, ConsoleColor.Red)); //Dragon
                    else if (znak == 'W') Mobs.Add(new NPC('W', PositionY, PositionX, 9, true, 5, 9, ConsoleColor.Red)); //Witch
                    else if (znak == 'M') Mobs.Add(new NPC('M', PositionY, PositionX, 10, true, 2, 9, ConsoleColor.Red)); //Minotaur

                    //brány
                    else if (znak == '0' || znak == '1' || znak == '2' || znak == '3' || znak == '4' || znak == '5' || znak == '6') CreateGate(znak, PositionY, PositionX);

                    PositionX++;
                }
                PositionY++;
                //malý a špinavý hack na ošetření jedné pitomé chyby 
                counter--;
            } while (counter > 0);

            //tohle je tady možná zbytečné
            LoadedData = null;

            //přiřadí počáteční souřadnice
            LastPositionX = DefaultX;
            LastPositionY = DefaultY;

            return true;
        }

        /// <summary>
        /// Vytvoření brány mezi dvěma světy.
        /// </summary>
        /// <param name="GateNumber">Číslo brány, číslo cílového světa.</param>
        /// <param name="PositionY">Pozice v ose X.</param>
        /// <param name="PositionX">Pozice v ose Y.</param>
        private void CreateGate(char GateNumber, int PositionY, int PositionX)
        {
            // ze znaku se dělá int docela špatně 
            int target = (int)Char.GetNumericValue(GateNumber);
            Gates.Add(new Gate('*', PositionY, PositionX, target, ConsoleColor.Cyan));
        }
    }
}
