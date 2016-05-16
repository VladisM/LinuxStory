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

namespace game
{
    /// <summary>
    /// Třída vytváří na mapě objekty artefaktů.
    /// </summary>
    public class Artifact : Wall
    {
        /// <summary>
        /// Kontruktor artefaktu.
        /// </summary>
        /// <param name="PositionY">Pozice řádku.</param>
        /// <param name="PositionX">Pozice ve sloupci.</param>
        /// <param name="BodyChar">Tímto znakem bude artefakt ve světě vystupovat.</param>
        /// <param name="ColorForPrint">Touto barvou se tento objekt vytiskne.</param>
        public Artifact(char BodyChar, int PositionY, int PositionX, ConsoleColor ColorForPrint)
        {
            this.PositionX = PositionX;
            this.PositionY = PositionY;
            this.BodyChar = BodyChar;
            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public Artifact() { }
    }

    /// <summary>
    /// Třída která vytváří objekty dveří na mapě, dědí od třídy Wall základní vlastnosti.
    /// </summary>
    public class Door : Wall
    {
        /// <summary>
        /// Drží stav dveří, true = otevřeno.
        /// </summary>
        public bool IsOpen;

        /// <summary>
        /// Metoda pro otevření dveří.
        /// </summary>
        public void Open()
        {
            if (IsOpen == false)
            {
                IsOpen = true;
                BodyChar = '/';
            }
        }

        /// <summary>
        /// Metoda pro zavření dveří.
        /// </summary>
        public void Close()
        {
            if (IsOpen == true)
            {
                IsOpen = false;
                BodyChar = '+';
            }
        }

        /// <summary>
        /// Konstruktor dvěří.
        /// </summary>
        /// <param name="IsOpen">True jsou-li dveře otevřené.</param>
        /// <param name="PositionY">Pozice dveří v ose Y.</param>
        /// <param name="PositionX">Pozice dveří v ose X.</param>
        /// <param name="ColorForPrint">Touto barvou se tento objekt vytiskne.</param>
        public Door(bool IsOpen, int PositionY, int PositionX, ConsoleColor ColorForPrint)
        {
            this.IsOpen = IsOpen;

            //rozhodnutí jaký znak budou mít dveře z počátku podle toho jestli jsou zavřené nebo ne
            if (this.IsOpen == true) this.BodyChar = '/';
            else this.BodyChar = '+';

            this.PositionX = PositionX;
            this.PositionY = PositionY;

            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public Door() { }
    }

    /// <summary>
    /// Vytváří na mapě objekty brány, pro přechod mezi lokacemi.
    /// </summary>
    public class Gate : Wall
    {
        /// <summary>
        /// Zde je uložen cíl kam brána vede.
        /// </summary>
        public int Target;

        /// <summary>
        /// Kontruktor brány.
        /// </summary>
        /// <param name="PositionY">Pozice řádku.</param>
        /// <param name="PositionX">Pozice ve sloupci.</param>
        /// <param name="BodyChar">Tímto znakem bude voda ve světě vystupovat.</param>
        /// <param name="ColorForPrint">Barva kterou se brána posléze vytiskne.</param>
        /// <param name="Target">Číslo cílové lokace.</param>
        public Gate(char BodyChar, int PositionY, int PositionX, int Target, ConsoleColor ColorForPrint)
        {
            this.PositionX = PositionX;
            this.PositionY = PositionY;
            this.BodyChar = BodyChar;
            this.Target = Target;
            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public Gate() { }
    }

    /// <summary>
    /// Vytváří na mapě objekty kopců, v podstatě je to to samé jako zeď okorát to má jinej znak.
    /// </summary>
    public class Hill : Wall
    {
        /// <summary>
        /// Kontruktor kopce.
        /// </summary>
        /// <param name="PositionY">Pozice řádku.</param>
        /// <param name="PositionX">Pozice ve sloupci.</param>
        /// <param name="BodyChar">Tímto znakem bude voda ve světě vystupovat.</param>
        /// <param name="ColorForPrint">Touto barvou se tento objekt vytiskne.</param>
        public Hill(char BodyChar, int PositionY, int PositionX, ConsoleColor ColorForPrint)
        {
            this.PositionX = PositionX;
            this.PositionY = PositionY;
            this.BodyChar = BodyChar;
            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public Hill() { }
    }

    /// <summary>
    /// Vytváří na mapě objekty serverů.
    /// </summary>
    public class Server : Wall
    {
        /// <summary>
        /// Udržuje informaci o tom kolik je potřeba síly tuxe pro instalaci.
        /// </summary>
        public int PowerNeeded;

        /// <summary>
        /// Udržuje inforamci o tom zdali je tento server již nainstalován.
        /// </summary>
        public bool Installed = false;

        /// <summary>
        /// Kontruktor serveru.
        /// </summary>
        /// <param name="PositionY">Pozice řádku.</param>
        /// <param name="PositionX">Pozice ve sloupci.</param>
        /// <param name="BodyChar">Tímto znakem bude voda ve světě vystupovat.</param>
        /// <param name="PowerNeeded">Potřebný power k instalaci linuxu.</param>
        /// <param name="ColorForPrint">Touto barvou se tento objekt vytiskne.</param>
        public Server(char BodyChar, int PositionY, int PositionX, int PowerNeeded, ConsoleColor ColorForPrint)
        {
            this.PositionX = PositionX;
            this.PositionY = PositionY;
            this.BodyChar = BodyChar;
            this.PowerNeeded = PowerNeeded;
            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public Server() { }
    }

    /// <summary>
    /// Vytváří na mapě objekty stromů, taky v podstatě zeď.
    /// </summary>
    public class Tree : Wall
    {
        /// <summary>
        /// Kontruktor stromčeku.
        /// </summary>
        /// <param name="PositionY">Pozice řádku.</param>
        /// <param name="PositionX">Pozice ve sloupci.</param>
        /// <param name="BodyChar">Tímto znakem bude stromeček ve světě vystupovat.</param>
        /// <param name="ColorForPrint">Touto barvou se stromček vytiskne.</param>
        public Tree(char BodyChar, int PositionY, int PositionX, ConsoleColor ColorForPrint)
        {
            this.PositionX = PositionX;
            this.PositionY = PositionY;
            this.BodyChar = BodyChar;
            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public Tree() { }
    }

    /// <summary>
    /// Objekt zdi.
    /// </summary>
    public class Wall
    {
        /// <summary>
        /// Drží pozici objektu v ose X.
        /// </summary>
        public int PositionX;

        /// <summary>
        /// Drží pozici objektu v ose Y.
        /// </summary>
        public int PositionY;

        /// <summary>
        /// Drží informaci o znaku který se bude zobrazovat.
        /// </summary>
        public char BodyChar;

        /// <summary>
        /// Drží informaci o tom jakou barvičkou se daný objekt vytiskne.
        /// </summary>
        public ConsoleColor ColorForPrint;

        /// <summary>
        /// Konstruktor třídy, vytvoří objekt zdi na zadaných souřadnicích a se zadaným  znakem.
        /// </summary>
        /// <param name="BodyChar">Znak  který se použije pro zobrazení objektu ve hře.</param>
        /// <param name="PositionY">Pozice objektu v ose X.</param>
        /// <param name="PositionX">Pozice objektu v ose Y.</param>
        /// <param name="ColorForPrint">Touto barvou se tento objekt vytiskne.</param>
        public Wall(char BodyChar, int PositionY, int PositionX, ConsoleColor ColorForPrint)
        {
            this.PositionX = PositionX;
            this.PositionY = PositionY;
            this.BodyChar = BodyChar;
            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Bezparametrický konstruktor, slouží pro odvozené třídy a serializaci.
        /// </summary>
        public Wall() { }

    }

    /// <summary>
    /// Objekt vody, taky skoro  zeď.
    /// </summary>
    public class Watter : Wall
    {
        /// <summary>
        /// Kontruktor vody.
        /// </summary>
        /// <param name="PositionY">Pozice řádku.</param>
        /// <param name="PositionX">Pozice ve sloupci.</param>
        /// <param name="BodyChar">Tímto znakem bude voda ve světě vystupovat.</param>
        /// <param name="ColorForPrint">Touto barvou se tento objekt vytiskne.</param>
        public Watter(char BodyChar, int PositionY, int PositionX, ConsoleColor ColorForPrint)
        {
            this.PositionX = PositionX;
            this.PositionY = PositionY;
            this.BodyChar = BodyChar;
            this.ColorForPrint = ColorForPrint;
        }

        /// <summary>
        /// Bezparametrický konstruktor pro serializaci.
        /// </summary>
        public Watter() { }
    }
}
