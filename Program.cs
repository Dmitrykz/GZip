using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Zip1
{
    class Program
    {

        static int iAns;
        static int Main(string[] args)
        {

            iAns = 0;

            Zip Test = new Zip(); // Создание объекта типа Zip

            Test.Run(args);  // Выполнение метода, который в зависимости от команды выполняет нужные действия


            return iAns;

        }
    }


    class Zip
    {

        static string strFileName;     //Объявление переменной в которой хранится путь к новому файлу (заархивированному или разорхивированному)
        static FileInfo fileToWork;    // fileToWork будет отвечать за чтение файла
        static string strFileNameOld;    //Объявление переменной в которой хранится путь к имеещимуся файлу
        static int iAns;                // Переменная показывающая прошло ли выполнения программы без ошибок (1) или с ошибками (0)
        static int iNumber;

        static string[] command;        //Переменная хранящая команды и пути к файлам

        public int Run(string[] args)    //Метод, который определяет и вызывает небходимые действия над файлами (архивирование или разахивирование)
        {
            if (SetCommand(args))       //Инициализирует переменные, если инициализация не проходит возвращает false и работа Run() прекращается

                switch (command.First())                       // Берет первый элемент из command и в вибирает нужную команду
                {
                    case "Compress":
                    case "compress":
                        if (CrDirectory())                  //Данный метод проверяет существует ли каталог в который создается новый файл, если нет то создает его.                    
                        {                                   // Если создание  даного каталога невозможно, то выдается соответствующее сообщение, также проверяется возможность работать с исходным
                            Comp();                         // Основной рабочий метод, который создает несколько потоков, для параллельного архивирования 
                        }

                        break;
                    case "Decompress":
                    case "decompress":
                        if (CrDirectory())                  //Описан выше                    
                        {
                            Decomp();                   // Основной рабочий метод, который создает несколько потоков, для параллельного разархивирования 

                        }
                        break;
                    default:
                        Console.WriteLine("Данная команда не известна ");
                        iAns = 0;
                        break;           // Если команды были набраны неверно, то выводится соответсвующее сообщение
                }

            else iAns = 0;

            return iAns;
        }

        public static bool SetCommand(string[] args)  //Инициализирует переменные если коммандная строка записана правильно
        {
            command = args;

            if (Check() == false)                       //Сheck() проверяет правильность ввода коммандной строки, если есть ошибки выводит сообщение с описанием
                return false;
            else
            {
                strFileNameOld = command.ElementAt(1);           //Запись в strFileName путь к имеющемуся файлу
                fileToWork = new FileInfo(command.ElementAt(1)); //Инициализация fileToWork, адресс для объекта берется как второй элемент command
                strFileName = command.ElementAt(2);              //Запись в strFileName путь к новому файлу
                return true;
            }
        }


        public static bool Check()
        {
            if (command.Length < 3 || command[1] == command[2])     //Проверка на правильность входных данных, либо  если входной и выходной файл являеются одним и тем же объектом
            {
                Console.WriteLine("Неправально заданы входные данные");
                return false;
            }


            if (File.Exists(command.ElementAt(2)))                  // Если уже существует файл с именем, который хотят создать, выдается ошибка
            {
                Console.WriteLine("Файл {0} уже существует", command.ElementAt(2));
                return false;
            }

            if (!command.ElementAt(1).EndsWith(".gz") && (command.ElementAt(0) == "Decompress" || command.ElementAt(0) == "decompress"))              //Проверка на правильное название файла (расширение)
            {                                                                                                                           // при условии что выбрана команда для разархивации
                Console.WriteLine("Файл {0} не подходит для разархивирования", command.ElementAt(1));
                return false;
            }


            return true;
        }


        public static void Comp()  // Основной рабочий метод, который создает несколько потоков, для параллельного архивирования
        {
            if (FreeMemory())    //Проверяет наличие свободного места на диске
            {
                Thread thZip, thZip2, thZip3, thZip4;
                iNumber = 0;                                   //Номер потока, необходим для создания порядковых номеров блоков, в которые идут потоки записи данных
                thZip = new Thread(new ThreadStart(Compress)); // Инициализирует и запускает новый параллельный поток для 
                thZip.Start();                                 //архивирования файла разорхивирования файла  и делает задержку в 10 млсек,
                Thread.Sleep(10);                               //чтобы блоки правильно получили свои порядковые номера
                thZip2 = new Thread(new ThreadStart(Compress));
                thZip2.Start();
                Thread.Sleep(10);
                thZip3 = new Thread(new ThreadStart(Compress));
                thZip3.Start();
                Thread.Sleep(10);
                thZip4 = new Thread(new ThreadStart(Compress));
                thZip4.Start();

                while (thZip2.IsAlive || thZip.IsAlive || thZip3.IsAlive || thZip4.IsAlive) //Ожидание выполнения всех параллельных потоков
                {
                    Thread.Sleep(100);
                }

                CopyFileAfterComp(); //Перенос данных из всех полученных блоков в один.
            }
        }

        public static void Decomp() // Основной рабочий метод, который создает несколько потоков, для параллельного разархивирования
        {
            if (FreeMemory() && PreFile())   //Проверяет наличие свободного места на диске и выделает необходимые блоки для 
            {                                //которые в последствии будут паралельно разархивированы

                Thread thZip, thZip2, thZip3, thZip4;
                iNumber = 0;                //Номер потока, необходим для создания порядковых номеров блоков, в которые идут потоки записи данных
                thZip = new Thread(new ThreadStart(Decompress));    // Инициализирует и запускает новый параллельный поток для
                thZip.Start();                                      //разархивирования файла разорхивирования файла  и делает задержку в 10 млсек,
                Thread.Sleep(10);                                   //чтобы блоки правильно получили свои порядковые номера
                thZip2 = new Thread(new ThreadStart(Decompress));
                thZip2.Start();
                Thread.Sleep(10);
                thZip3 = new Thread(new ThreadStart(Decompress));
                thZip3.Start();
                Thread.Sleep(10);
                thZip4 = new Thread(new ThreadStart(Decompress));
                thZip4.Start();
                while (thZip2.IsAlive || thZip.IsAlive || thZip3.IsAlive || thZip4.IsAlive) //Ожидание выполнения всех параллельных потоков
                {
                    Thread.Sleep(100);
                }

                CopyFileAfterDecomp();                              //Перенос данных из всех полученных блоков в один.
            }
        }


        public static void Compress()
        {

            using (FileStream originalFileStream = fileToWork.OpenRead())     //Создается поток, который отвечает за чтение файла, который нужно заархивировать
            {

                int iNum = iNumber++;                                       //iNum хранит номер потока

                long lLength = originalFileStream.Length;
                long lPos = (long)(Math.Ceiling((decimal)lLength / 1000) * 250) * (iNum + 1);    //lPos хранит номер позиции до которой будет происходит считывание данных
                if (lPos >= lLength)
                    lPos = lLength;

                originalFileStream.Seek((long)(Math.Ceiling((decimal)lLength / 1000) * 250) * iNum, SeekOrigin.Begin); //Устанавливает позицию с который начнется считывание данных
                try
                {

                    using (FileStream compressedFileStream = File.OpenWrite(strFileName + iNum)) //Создается поток, отвечающий за запись данных в новый созданый файл
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress)) // При помощи класса GZipStream происходит архивирование
                        {
                            byte[] array = new byte[250];                //Считывание и архивирование данных происходит по 250 байт

                            while (originalFileStream.Position != lPos)
                            {
                                originalFileStream.Read(array, 0, 250);
                                compressionStream.Write(array, 0, 250);
                            }



                            Console.WriteLine("в файл {0} добавлена информация", strFileName + iNum);               //Сообщение о том что сжатие файла прошло успешно

                        }
                    }

                }


                catch (Exception e)
                { Console.WriteLine(e.Message); }

            }
        }


        public static void Decompress()
        {
            int i = iNumber++;                                                        //iNum хранит номер потока                  
            using (FileStream originalFileStream = File.OpenRead(strFileNameOld + i))  //Создается поток, который отвечает за чтение файла, который нужно разархивировать
            {

                try
                {
                    using (FileStream decompressedFileStream = File.OpenWrite(strFileName + i)) //Создается поток, отвечающий за запись данных в ново созданый файла
                    {


                        using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))  // При помощи класса GZipStream происходит разархивирование
                        {


                            decompressionStream.CopyTo(decompressedFileStream);

                            Console.WriteLine("Файл {0} Разархивирован", fileToWork.Name);     //Сообщение о том что разархивирование файла прошло успешно

                        }


                    }


                }



                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                File.Delete(strFileNameOld + i);   //После того блок данных был разархивирован он удаляется
            }

        }


        public static bool CrDirectory() //Данный метод проверяет существует ли каталог в который создается новый файл, если нет то создает его.                    
        {
            int a = strFileName.LastIndexOf("\\");   // Переменная хранящая значение последнего индекса"\", чтобы выделить адрес файла из его полного имени

            try
            {
                fileToWork.OpenRead();  //Проверка на наличие и возможность чтения с файла.

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            try
            {
                if (a == -1 || Directory.Exists(strFileName.Remove(a))) return true; // Если в командной строке будет записано только название файла, то каталог создавать не надо.
                                                                                     // Проверка на наличие каталога, если такой каталог уже имеется, то пункт создания пропускается.
                DirectoryInfo dirInfo = new DirectoryInfo(strFileName.Remove(a));
                dirInfo.Create();
                return true;
            }

            catch (Exception e)   // Вывод об ошибке, если создать каталог не получилось по какой либо причине
            {
                Console.WriteLine("Не получается создать каталог \n{0}", e.Message);
                return false;
            }

        }



        public static void CopyFileAfterComp()   //Метод который переносит данные из временных файло в один конечный и удаляет временые
        {
            try
            {
                FileInfo file1 = new FileInfo(strFileName + 0);   //Создает объекты FileInfo, для созданных временных файлов
                long k = file1.Length;                              //Переменная хранящая длину файла в байтах
                FileInfo file2 = new FileInfo(strFileName + 1);
                long k2 = file2.Length;
                FileInfo file3 = new FileInfo(strFileName + 2);
                long k3 = file3.Length;
                FileInfo file4 = new FileInfo(strFileName + 3);
                long k4 = file4.Length;
                using (FileStream fstream1 = file1.OpenWrite())         //открывает Первый файл для записи и почередно записывает данные 
                {                                                        //туда все данные полученные при архивировании

                    using (FileStream fstream2 = file2.OpenRead())
                    {

                        fstream1.Seek(fstream1.Length, SeekOrigin.Current);
                        fstream2.CopyTo(fstream1, 32764);
                    }
                    using (FileStream fstream3 = file3.OpenRead())
                    {

                        fstream3.CopyTo(fstream1, 32764);
                    }
                    using (FileStream fstream4 = file4.OpenRead())
                    {

                        fstream4.CopyTo(fstream1, 32764);
                    }


                }

                using (StreamWriter sw1 = file1.AppendText())  //запись в конец файла информации, которая содержит размеры блоков,
                {                                               // из которго состоит конечный файл
                    sw1.Write("\r{0}", k.ToString());
                    sw1.Write(" {0}", k2.ToString());
                    sw1.Write(" {0}", k3.ToString());
                }

                File.Delete(strFileName + "1");   // Удаление  временных файлов
                File.Delete(strFileName + "2");
                File.Delete(strFileName + "3");
                file1.MoveTo(strFileName);        //Переименование конечного файла
                iAns = 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        public static void CopyFileAfterDecomp()    //Метод который переносит данные из временных файло в один конечный и удаляет временые
        {
            try
            {
                FileInfo file1 = new FileInfo(strFileName + "0");  //Создает объекты FileInfo, для созданных временных файлов                              //Переменная хранящая длину файла в байтах
                FileInfo file2 = new FileInfo(strFileName + "1");
                FileInfo file3 = new FileInfo(strFileName + "2");
                FileInfo file4 = new FileInfo(strFileName + "3");
                using (FileStream fstream1 = file1.OpenWrite())
                {

                    using (FileStream fstream2 = file2.OpenRead())
                    {
                        fstream1.Seek(fstream1.Length, SeekOrigin.Current);
                        fstream2.CopyTo(fstream1, 32764);
                    }
                    using (FileStream fstream3 = file3.OpenRead())
                    {
                        fstream3.CopyTo(fstream1, 32764);
                    }
                    using (FileStream fstream4 = file4.OpenRead())
                    {
                        fstream4.CopyTo(fstream1, 32764);
                    }
                }
                file2.Delete();     // Удаление  временных файлов
                file3.Delete();
                file4.Delete();
                file1.MoveTo(strFileName);   //Переименование конечного файла
                iAns = 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static bool PreFile()      //Подготовка блоков для разархивации
        {
            byte[] iFound = new byte[100];

            using (FileStream fsFound = File.OpenRead(strFileNameOld))   // Cчитывает последние стой байтов из файла, который будем разархивировать
            {                                                           // и записыает их в массив iFound

                fsFound.Seek(fsFound.Length - 100, SeekOrigin.Begin);
                fsFound.Read(iFound, 0, 100);

            }
            string str = Encoding.UTF8.GetString(iFound, 0, iFound.Length);   //Перекодирует полученный массив байт в строку


            string[] sNumber = str.Split();  //получает массив строк, последние три из которых хранят информацию о блоках, из которых состоит файл


            long[] lNum = new long[5];    //массив lNum хранит информацию а размерах блоков файла
            try
            {
                for (int i = 0; i <= 2; i++)
                {
                    lNum[i + 1] = long.Parse(sNumber[sNumber.Length - 3 + i]) + lNum[i];
                }
            }
            catch
            {
                Console.WriteLine("Выбран неверный файл для разархивирования");
                return false;
            }

            lNum[0] = 0;
            lNum[4] = fileToWork.Length;



            for (int i = 0; i < 4; i++)       // Создает 4 файла, которые в дальнейшем будут паралельно разархивированы

                using (FileStream fs1 = File.Create(strFileNameOld + i))  //Создание i-того файла 
                {
                    Console.WriteLine("Файл {0} создан", strFileNameOld + i);
                    using (FileStream fsOrig = fileToWork.OpenRead())    //Считывание данных с исходного и запись в 
                    {                                                       //в i-ый файл
                        fsOrig.Seek(lNum[i], SeekOrigin.Begin);
                        byte[] array = new byte[1];

                        while (fsOrig.Position != lNum[i + 1])
                        {
                            fsOrig.Read(array, 0, 1);
                            fs1.Write(array, 0, 1);

                        }

                    }
                }
            return true;
        }

        public static bool FreeMemory()  //метод проверяет хватает ли места для выполнения программы
        {
            DriveInfo disk = new DriveInfo(strFileName.First().ToString());
            if (fileToWork.Length > 2 * disk.AvailableFreeSpace)
            {
                Console.WriteLine("Не хватает места на диске {0}", disk.Name);
                return false;
            }
            return true;
        }
    }
}


