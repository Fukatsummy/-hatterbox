﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Сhatterbox
{
    class Program
    {
        static int localPort; // порт приема сообщений
        static Socket listenSocket; // Сокет
        static List<IPEndPoint> clients = new List<IPEndPoint>(); // Список "подключенных" клиентов
        static void Main(string[] args)
        {
            Console.WriteLine("UDP CHAT SERVER");
            Console.Write("Введите порт для приема сообщений: ");
            localPort = Int32.Parse(Console.ReadLine());
            Console.WriteLine();
            try
            {
                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); // Создание сокета
                Task listenTask = new Task(Listen); // Создание потока для получения сообщений
                listenTask.Start(); // Запуск потока
                listenTask.Wait(); // Не идем дальше пока поток не будет остановлен
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Close(); // Закрываем сокет
            }
        }
        // поток для приема подключений
        private static void Listen()
        {
            try
            {
                //Прослушиваем по адресу
                IPEndPoint localIP = new IPEndPoint(IPAddress.Parse("0.0.0.0"), localPort);
                listenSocket.Bind(localIP);
                while (true)
                {
                    StringBuilder builder = new StringBuilder(); // получаем сообщение
                    int bytes = 0; // количество полученных байтов
                    byte[] data = new byte[256]; // буфер для получаемых данных
                    EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0); //адрес, с которого пришли данныe
                    do
                    {
                        bytes = listenSocket.ReceiveFrom(data, ref remoteIp);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (listenSocket.Available > 0);
                    IPEndPoint remoteFullIp = remoteIp as IPEndPoint; // получаем данные о подключении
                    Console.WriteLine("{0}:{1} - {2}", remoteFullIp.Address.ToString(), remoteFullIp.Port, builder.ToString()); // выводим сообщение

                    bool addClient = true; // Переменная для определения нового пользователя
                    for (int i = 0; i < clients.Count; i++) // Циклом перебераем всех пользователей которые отправляли сообщения на сервер
                        if (clients[i].Address.ToString() == remoteFullIp.Address.ToString()) // Если адрес отправителя данного сообщения совпадает с адресом в списке
                            addClient = false; // Не добавляем клиента в историю
                    if (addClient == true) // Если этого отправителя не было обнфружено в истории
                        clients.Add(remoteFullIp); // Добавляем клиента в историю
                    BroadcastMessage(builder.ToString(), remoteFullIp.Address.ToString()); // Рассылаем сообщения всем клиентам кроме самого отправителя
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Close();
            }
        }

        // Метод для рассылки сообщений
        private static void BroadcastMessage(string message, string ip)
        {
            byte[] data = Encoding.Unicode.GetBytes(message); // Формируем байты из текста

            for (int i = 0; i < clients.Count; i++) // Циклом перебераем всех клиентов
                if (clients[i].Address.ToString() != ip) // Если аддресс получателя не совпадает с аддрессом отправителя
                    listenSocket.SendTo(data, clients[i]); // Отправляем сообщение
        }

        // закрытие сокета
        private static void Close()
        {
            if (listenSocket != null)
            {
                listenSocket.Shutdown(SocketShutdown.Both);
                listenSocket.Close();
                listenSocket = null;
            }
            Console.WriteLine("Сервер остановлен!");
        }
    }
}