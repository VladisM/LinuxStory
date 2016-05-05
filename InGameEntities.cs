using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace game
{
    /// <summary>
    /// Základní třída moba na které jsou postaveny další živé entity ve světě.
    /// </summary>
    public class MobBasic
    {
        /// <summary>
        /// Drží pozici moba v ose X.
        /// </summary>
        public int PositionX;

        /// <summary>
        /// dřží pozici moba v ose Y.
        /// </summary>
        public int PositionY;

        /// <summary>
        /// Stará pozice v ose X, na ní byl v předchozím kroku.
        /// </summary>
        public int OldPositionX;

        /// <summary>
        /// Stará pozice v ose Y, na ní byl v předchozím kroku.
        /// </summary>
        public int OldPositionY;

        /// <summary>
        /// Tento znak se bude tisknout pro zobrazení moba v mapě.
        /// </summary>
        public char BodyChar;

        /// <summary>
        /// Aktuální body života.
        /// </summary>
        public int HealthPoint;

        /// <summary>
        /// Maximální množství životních bodů.
        /// </summary>
        public int MaximumHealthPoint;

        /// <summary>
        /// Level entity.
        /// </summary>
        public int Level;

        /// <summary>
        /// Drží barvu kterou se daný objekt vytiskne do okna.
        /// </summary>
        public ConsoleColor ColorForPrint;

        /// <summary>
        /// Pevné nastavení pozice. POZOR! NEHLÍDÁ ROZSAH!
        /// </summary>
        /// <param name="_x">Souřadnice X.</param>
        /// <param name="_y">Souřadnice Y.</param>
        public void PositionSet(int _y, int _x)
        {
            PositionX = _x;
            PositionY = _y;
        }

        /// <summary>
        /// Metoda která přijme poškození.
        /// </summary>
        /// <param name="demage">Velikost přijatého poškození.</param>
        /// <returns>True pokud to nepřežil. :(</returns>
        public bool GetDemage(int demage)
        {
            if (HealthPoint - demage <= 0) { HealthPoint = 0; return true; }
            else { HealthPoint = HealthPoint - demage; return false; }
        }
    }

    /// <summary>
    /// Třída pro NPC. Založena na MobBasic.
    /// </summary>
    public class NPC : MobBasic
    {

        /// <summary>
        /// Rozhoduje o chování UI moba, jeli zlý má agresivní UI a jde po hráči.
        /// </summary>
        public bool IsEvil;

        /// <summary>
        /// Drží vzdálenost v jakém se může entita pohybovat od svého místa. 
        /// </summary>
        public int MaximalFreeMoveRadius;

        /// <summary>
        /// Výchozí pozice moba v ose X. Sem se bude UI vracet.
        /// </summary>
        public int BasePositionX;

        /// <summary>
        /// Výchozí pozice moba v ose Y. Sem se bude UI vracet.
        /// </summary>
        public int BasePositionY;

        /// <summary>
        /// Udžuje vzdálenost na kterou je daný mob agresivní.
        /// </summary>
        public int AgressiveRadius;

        /// <summary>
        /// Konstruktor zlého moba
        /// </summary>
        /// <param name="BodyChar">Znak kterým vystupuje mob ve světě.</param>
        /// <param name="PositionY">Výchozí pozice moba v ose Y.</param>
        /// <param name="PositionX">Výchozí pozice moba v ose X.</param>
        /// <param name="IsEvil">Rozhoduje o tom jestli je mob zlý nebo ne, jeli zlý bude mít agesivní UI.</param>
        /// <param name="MaximalFreeMoveRadius">Určuje jak daleko se může entita pohybovat kolem sebe.</param>
        /// <param name="AgresiveRadius">Určuje na jakou délku se mob naštve a rozeběhne se po mě.</param>
        /// <param name="Level"></param>
        public NPC(char BodyChar, int PositionY, int PositionX, int Level, bool IsEvil, int MaximalFreeMoveRadius, int AgressiveRadius, ConsoleColor ColorForPrint)
        {
            this.BodyChar = BodyChar;
            this.PositionY = PositionY;
            this.PositionX = PositionX;
            OldPositionX = PositionX;
            OldPositionY = PositionY;
            BasePositionX = PositionX;
            BasePositionY = PositionY;
            this.Level = Level;
            this.IsEvil = IsEvil;
            this.MaximalFreeMoveRadius = MaximalFreeMoveRadius;
            this.AgressiveRadius = AgressiveRadius;
            SetUpMaximumHealthPoint();
            HealthPoint = MaximumHealthPoint;
            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Metoda pro výpočet HP.
        /// </summary>
        private void SetUpMaximumHealthPoint()
        {
            MaximumHealthPoint = 10 * Level;
        }

        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public NPC() { }
    }

    /// <summary>
    /// Entita hráče.
    /// </summary>
    public class Player : MobBasic
    {
        /// <summary>
        /// Jméno hráče, btw.. je tohle vůbec podstatné?
        /// </summary>
        public string Name;

        /// <summary>
        /// Aktuální zkušenosti.
        /// </summary>
        public int Experiences;

        /// <summary>
        /// Síla tuxe, něco jako mana pro instalování linuxu na servery.
        /// </summary>
        public int TuxPower = 0;

        /// <summary>
        /// Zkušenosti potřebné pro další level.
        /// </summary>
        public int ExperiencesForNextLevel;

        /// <summary>
        /// Metoda pro výpočet expů na další level.
        /// </summary>
        private void SetUpExperiencesForNextLevel()
        {
            ExperiencesForNextLevel = (int)Math.Pow(2, Level) + 10 * Level;
        }

        /// <summary>
        /// Metoda pro výpočet HP pro další lvl.
        /// </summary>
        private void SetUpMaximumHealthPoint()
        {
            MaximumHealthPoint = 10 * Level;
        }

        /// <summary>
        /// Zkusí udělat LevelUp, pokud jsou dostatečné expy tak se i povede ;)
        /// </summary>
        /// <returns>True pokud se LevelUp povede.</returns>
        private bool TryLevelUp()
        {
            //zkouknutí stavu expů
            if (Experiences >= ExperiencesForNextLevel)
            {
                //pokud je stav dostatečný provede se LevelUp
                Level++;
                SetUpMaximumHealthPoint();
                SetUpExperiencesForNextLevel();
                return true;
            }
            //jinak se vrací false
            else return false;
        }

        /// <summary>
        /// Přidá expy hráčovi, a posléze zkusí lvl up. O povedení či nepovedení lvl up informuje návratová hodnota.
        /// </summary>
        /// <param name="Exp">Počet expů jenž se mají připočíst.</param>
        /// <returns>True pokud se lvl up povede.</returns>
        public bool AddExp(int Exp)
        {
            Experiences += Exp;
            return TryLevelUp();
        }

        /// <summary>
        /// Proměnná sloužící pro počítání cyklů, každý entý cyklus vyléčím trošku hráče.
        /// </summary>
        public int HealCounter = 0;

        /// <summary>
        /// Konstruktor objektu hráče.
        /// </summary>
        /// <param name="Player_Name">Jméno hráče.</param>
        /// <param name="BodyChar">Znak kterým bude hráč ve světe vystupovat.</param>
        /// <param name="PositionY">Výchozí pozice hráče v ose Y.</param>
        /// <param name="PositionX">Výchozí pozice hráče v ose X.</param>
        public Player(string Player_Name, char BodyChar, int PositionY, int PositionX, ConsoleColor ColorForPrint)
        {
            Name = Player_Name;
            this.BodyChar = BodyChar;
            this.PositionX = PositionX;
            this.PositionY = PositionY;
            OldPositionX = PositionX;
            OldPositionY = PositionY;
            Level = 1;
            MaximumHealthPoint = 10;
            HealthPoint = 10;
            ExperiencesForNextLevel = 12;
            Experiences = 0;
            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public Player() { }
    }
}
