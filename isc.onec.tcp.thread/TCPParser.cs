using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using isc.onec.bridge;

namespace isc.onec.tcp.thread
{

	/// <summary>
	/// Упаковщик пакетов
	/// </summary>
	public class PacketBuilder
	{

		byte[] packet;

		/// <summary>
		/// Упаковывает данные в пакет
		/// </summary>
		/// <param name="data">Данные</param>
		/// <returns>Возвратит массив байт, где первые 4 байта будут содержать размер "полезных" данных</returns>
		public byte[] CreatePacket(byte[] data)
		{
			packet = new byte[data.Length + 4];
			Array.Copy(Encoding.Default.GetBytes(data.Length.ToString()), 0, packet, 0, data.Length.ToString().Length);
			Array.Copy(data, 0, packet, 4, data.Length);
			return packet;
		}

	}

	/// <summary>
	/// Парсер пакетов
	/// </summary>
	public class PacketParser
	{
		byte[] temp;

		byte[] packetReturn;
		// Вернет пакет без 4-х байтового заголовка, т.е только полезные данные 
		public delegate void EventHandler(TcpClient client,Server server,byte[] Packet);
		public event EventHandler Return_Packet;


		private void ArrayResize(ref byte[] source, int size)
		{
			byte[] temp = new byte[size];

			if (size >= source.Length)
			{
				Array.Copy(source, 0, temp, 0, source.Length);
			}
			else
			{
				Array.Copy(source, 0, temp, 0, temp.Length);
			}
			source = temp;
		}

		/// <summary>
		/// Вызов парсинга...
		/// </summary>
		/// <param name="data">Полученые данные</param>
		/// <param name="totalbytes">Количество полученных данных</param>
		/// <param name="client"></param>
		/// <param name="server"></param>
		public void AnalyzeTraffic(ref byte[] data, int totalbytes,TcpClient client,Server server)
		{
			Analyze(ref data, totalbytes,client,server);
		}

		private void Analyze(ref byte[] data, int totalbytes,TcpClient client,Server server)
		{
			int position = 0;
			int size = 0;
			int countbyte = 0;

			//Если до этого был принят неполный пакет то...
			if (temp != null)
			{
				//Если неполный пакет был с заголовком, то извлекаем из заголовка размер данных 
				//из пакета, считаем сколько данных надо изьять до восстановления полного пакета.

				if (temp.Length >= 4)
				{
				   // size = int.Parse(Encoding.Default.GetString(temp, position, 4));
					size = BitConverter.ToInt32(temp, 0);
					countbyte = size + 4 - temp.Length;

					//Если размер полученных данных больше чем недостающий размер, то...
					if (totalbytes >= countbyte)
					{
						//Восстанавливаем пакет...
						ArrayResize(ref temp, temp.Length + countbyte);
						Array.Copy(data, 0, temp, temp.Length - countbyte, countbyte);
						//---------------------------------------------
						packetReturn = new byte[size];
						Array.Copy(temp, 4, packetReturn, 0, size);
						Return_Packet(client,server,packetReturn);
						packetReturn = null;
						//---------------------------------------------
						position += countbyte;
					}
					//Если же полученных данных меньше чем нам нужно изьять,
					//то восстанавливаем сколько возможно и выходим из функции до следующего цикла приёма пакетов.
					else
					{
						ArrayResize(ref temp, temp.Length + totalbytes);
						Array.Copy(data, 0, temp, temp.Length - totalbytes, totalbytes);
						return;
					}

				}
				//Если же пакет был с неполным заголовком, то восстанавливаем заголовок пакета
				else
				{

					countbyte = 4 - temp.Length;//Узнаем сколько не хватает до полного заголовка
					ArrayResize(ref temp, temp.Length + countbyte);//Изменяем размер буфера до размера заголовка
					Array.Copy(data, 0, temp, temp.Length - countbyte, countbyte);//Восстанавливаем заголовок
					size = int.Parse(Encoding.Default.GetString(temp, position, 4));//Узнаем размер пакета

					//Если размер пакета меньше чем общее количество полученных данных, то... 
					if (totalbytes >= size)
					{
						ArrayResize(ref temp, temp.Length + size);//Увеличиваем размер пакета до его истинного размера
						Array.Copy(data, countbyte, temp, 4, size);//Копируем в него данные
						//------------------------------------------
						packetReturn = new byte[size];
						Array.Copy(temp, 4, packetReturn, 0, size);
						Return_Packet(client,server,packetReturn);
						packetReturn = null;
						//------------------------------------------
						position += countbyte + size;//Устанавливаем "курсор" на конец данного пакета в массиве полученных данных
					}
					//Если размер пакета больше чем количество полученных данных, то
					else
					{
						//то восстанавливаем сколько возможно и выходим из функции до следующего цикла приёма пакетов.
						ArrayResize(ref temp, temp.Length + totalbytes - countbyte);
						Array.Copy(data, countbyte, temp, 4, totalbytes - countbyte);
						return;
					}

				}

				temp = null;
			}

			while (position != totalbytes)
			{
				//Читаем заголовок если он не выходит за пределы...
				if (totalbytes - position > 4)
				{
					//size = int.Parse(Encoding.Default.GetString(data, position, 4));
					byte[] head=new byte[4];
					Array.Copy(data,position,head,0,4);
					size = BitConverter.ToInt32(head,0);
					position += 4;

					//Читаем данные, если они не заходят за пределы...
					if (position + size <= totalbytes)
					{

						packetReturn = new byte[size];
						Array.Copy(data, position, packetReturn, 0, size);
						Return_Packet(client,server,packetReturn);
						packetReturn = null;
						position += size;
					}
					else
					{
						//Создаем буфер с неполным пакетом...
						temp = new byte[totalbytes - (position - 4)];
						Array.Copy(data, position - 4, temp, 0, temp.Length);
						position += totalbytes - position;

					}
				}
				else
				{
					//Создаем буфер с неполным заголовком и пакетом...
					temp = new byte[totalbytes - position];
					Array.Copy(data, position, temp, 0, temp.Length);
					position += totalbytes - position;

				}

			}
		}

	}
}
