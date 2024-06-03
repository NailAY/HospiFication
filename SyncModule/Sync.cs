using System;
using System.Xml.Schema;
using HospiFication.Models;
using HospiFication.Controllers;
using System.Diagnostics;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace SyncModule
{
    public class SyncModuleClass
    {
        static void Main(string[] args)
        {
            SyncWith1c(args);
            Environment.Exit(0);
        }
        public static void SyncWith1c(string[] args)
        {
            foreach (string s in args)
            {
                Console.WriteLine(s + "\r\n");
            }
            //args = new string [] {"Пациенты", "1", "Иванов Иван", "08.04.2003", "Сидоров Иван Фёдорович", "Боль в брюхе", "+79003286566","Почта@","Родной","1","","","1","","" };
            //args = new string[] { "Сотрудники", "Иванов Иван Гаптович" };
            string user = "Мухаррямов Наиль";
            string pas = "";
            string file = @"J:\\Учёба\\Магистратура\\2 курс\\2 семестр\\Дипломный проект\\Программа\\1CForSync\\База для курсовой\";
            dynamic result;
            dynamic refer;
            V83.COMConnector com1s = new V83.COMConnector();

            //Передаём пациентов
            //Console.WriteLine("Starting Patients sync");
            if (args[0] == "Пациенты")
            {
                com1s.PoolCapacity = 1000;
                com1s.PoolTimeout = 600;
                com1s.MaxConnections = 10;
                result = com1s.Connect("File='" + file + "';Usr='" + user + "';pwd='" + pas + "';");
                if (result.Справочники.Пациенты.НайтиПоКоду(args[1], true).Ссылка.Пустая())
                {
                    refer = result.Справочники.Пациенты.СоздатьЭлемент();
                    refer.Код = args[1];
                    refer.Наименование = args[2];
                    refer.ДатаРождения = Convert.ToDateTime(args[3]);
                    refer.ЛечащийВрач = result.Справочники.Сотрудники.НайтиПоНаименованию(args[4], true);
                    refer.СимптомыПриПоступлении = args[5];
                    int count = 0;

                    while (count < 3)
                    {
                        if (args[6 + count * 3] != "")
                        {
                            dynamic a = refer.КонтактыРодственников.Добавить();
                            a.ТелефонРодственника = args[6 + count * 3];
                            a.Email = args[7 + count * 3];
                            a.СтепеньРодства = args[8 + count * 3];
                        }
                        count++;
                    }
                    refer.Записать();
                }

            }
            else if (args[0] == "Сотрудники")
            {
                com1s.PoolCapacity = 1000;
                com1s.PoolTimeout = 600;
                com1s.MaxConnections = 10;
                result = com1s.Connect("File='" + file + "';Usr='" + user + "';pwd='" + pas + "';");
                refer = result.Справочники.Сотрудники.СоздатьЭлемент();
                bool isNotDocInBase = true;
                try
                {
                    dynamic doc = result.Справочники.Сотрудники.НайтиПоНаименованию(args[1], true);
                    if (doc.Ссылка.Пустая())
                    {
                        
                    }
                    else
                    {
                        isNotDocInBase = false;
                    }

                }
                catch
                {

                }
                if (isNotDocInBase)
                {
                    refer.Наименование = args[1];
                    refer.Записать();
                }
            }
            else if (args[0] == "Выписка")
            {
                com1s.PoolCapacity = 1000;
                com1s.PoolTimeout = 600;
                com1s.MaxConnections = 10;
                result = com1s.Connect("File='" + file + "';Usr='" + user + "';pwd='" + pas + "';");
                //Выписка как элемент справочника
                refer = result.Справочники.Выписки.СоздатьЭлемент();
                bool isNotExpInBase = true;
                try
                {
                    dynamic Ext = result.Справочники.Выписки.НайтиПоКоду(args[1], true);
                    if (Ext.Ссылка.Пустая())
                    {

                    }
                    else
                    {
                        isNotExpInBase = false;
                    }

                }
                catch
                {

                }
                if (isNotExpInBase)
                {
                    refer.Код = args[1];
                    refer.Наименование = args[2];
                    refer.ЗаключениеВрача = args[3];
                    refer.НазначенныеЛекарства = args[4];
                    refer.УведомлениеОтправлено = args[5];
                    refer.ДатаРождения = Convert.ToDateTime(args[6]);
                    refer.ЛечащийВрач = result.Справочники.Сотрудники.НайтиПоНаименованию(args[7], true);
                    refer.СимптомыПриПоступлении = args[8];
                    refer.ФИО_Пациента = result.Справочники.Пациенты.НайтиПоКоду(args[9], true);
                    refer.Записать();
                }
                //Выписка как Документ
                dynamic secondrefer = result.Документы.Выписка.СоздатьДокумент();
                isNotExpInBase = true;
                try
                {
                    dynamic Ext = result.Документы.Выписка.НайтиПоКоду(args[1], true);
                    if (Ext.Ссылка.Пустая())
                    {

                    }
                    else
                    {
                        isNotExpInBase = false;
                    }

                }
                catch
                {

                }
                if (isNotExpInBase)
                {
                    secondrefer.Номер = args[1];
                    secondrefer.Дата = DateTime.Now;
                    secondrefer.Проведен = true;
                    secondrefer.ФИО_Пациента = args[2];
                    secondrefer.ЗаключениеВрача = args[3];
                    secondrefer.НазначенныеЛекарства = args[4];
                    secondrefer.ДатаРождения = Convert.ToDateTime(args[6]);
                    secondrefer.ЛечащийВрач = result.Справочники.Сотрудники.НайтиПоНаименованию(args[7], true);
                    secondrefer.СимптомыПриПоступлении = args[8];
                    secondrefer.Записать();
                }
                //Выписка как Документ
            }
            else if (args[0] == "Уведомления")
            {
                com1s.PoolCapacity = 1000;
                com1s.PoolTimeout = 600;
                com1s.MaxConnections = 10;
                result = com1s.Connect("File='" + file + "';Usr='" + user + "';pwd='" + pas + "';");
                refer = result.Справочники.Уведомления.СоздатьЭлемент();
                bool isNotNoteInBase = true;
                try
                {
                    dynamic notification = result.Справочники.Уведомления.НайтиПоКоду(args[1], true);
                    if (notification.Ссылка.Пустая())
                    {

                    }
                    else
                    {
                        isNotNoteInBase = false;
                    }

                }
                catch
                {

                }
                if (isNotNoteInBase)
                {
                    refer.Код = args[1];
                    refer.Наименование = args[2];
                    refer.Выписка = result.Документы.Выписка.НайтиПоНомеру(args[3]);
                    refer.Уведомление = args[4];
                    refer.Записать();
                }
            }


        }
    }
}
