using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using System.Net;
using System.IO;
using AngleSharp;

using System.Threading;

using MySql.Data.MySqlClient;
namespace Service
{
    public partial class Service1 : ServiceBase
    {
        private Thread tr;
        private MySqlConnectionStringBuilder mysqlCSB;
        private DataTable dt = new DataTable();
        private StreamWriter sw;
        private int i = 0;
        private String result = "";
        private String parseUrl;
        private bool insert_;
        private string url;
        public Service1()
        {
            InitializeComponent();
            url = "https://www.avito.ru/sterlitamak/kvartiry/prodam";

            mysqlCSB = new MySqlConnectionStringBuilder();
            mysqlCSB.Server = "localhost";
            mysqlCSB.Database = "database2";
            mysqlCSB.UserID = "root";
            mysqlCSB.Password = "";
        }
        // получить HTML-код документа по заданному URL
       private static string getResponse(string uri)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            int count = 0;
            do
            {
                count = resStream.Read(buf, 0, buf.Length);
                if (count != 0)
                {
                    sb.Append(Encoding.UTF8.GetString(buf, 0, count));
                }
            }
            while (count > 0);
            return sb.ToString();
        }

        protected override void OnStart(string[] args)
        {
            sw = new StreamWriter("output4.txt");
            result = result + "Запуск..." + Environment.NewLine;


            tr = new Thread(angleMain);
            tr.Start();
            Console.WriteLine("Поток запущен");
        }

      
        // парсинг 1 страницы
        private void angleMain()
        {
            try
            {
                String Html = getResponse(url);
                var parser = new HtmlParser();
                var document = parser.Parse(Html);

                var descriptions = document.QuerySelectorAll(".description");
                if (descriptions.Count().Equals(0)) Console.WriteLine(Html);


                foreach (var item in descriptions)
                {
                    i++;

                    String link = item.GetElementsByClassName("item-description-title-link").First().GetAttribute("href").Trim();

                    String parseUrl = "https://avito.ru" + link;

                    this.parseUrl = parseUrl;
                    result = "Открываем ссылку" + parseUrl + "..." + Environment.NewLine;

                    Random rand = new Random();
                    int temp;
                    temp = (rand.Next(200000)) / 100 + 1000;
                    angle2(parseUrl);
                    Thread.Sleep(temp);
                    result = result + "Ждем " + temp / 1000.0 + " секунд..." + Environment.NewLine;

                    result = result + i + Environment.NewLine;
                    Console.WriteLine(i + " " + parseUrl);

                    sw.WriteLine(result);

                }
                var nextLink = document.QuerySelectorAll(".js-pagination-next");
                if (nextLink.Count() != 0)
                {
                    url = "https://avito.ru" + nextLink.First().GetAttribute("href");
                    Console.WriteLine(url);
                    angleMain();
                }
                else
                {
                    url = "https://www.avito.ru/sterlitamak/kvartiry/prodam?p=1";
                    Console.WriteLine(url);
                    angleMain();
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine(ex.Message);
                 Console.WriteLine(ex.Source);
                 Console.WriteLine(ex.StackTrace);
                sw.Close();
            }
            sw.Close();
            Console.WriteLine("Выполнено");
        }
        private string RemoveSpaces(string inputString)
        {
            String res = "";
            for (int i = 0; i < inputString.Length; i++)
            {
                char ch = inputString.ElementAt(i);
                int res2 = 0;
                bool j = int.TryParse(ch.ToString(), out res2);
                if (j)
                {
                    res = res + ch;
                }
               
            }
            return res;
        }
        // обновление записи в таблице
        private void update_rec(int id, String title, int price, int ets_count, int et_num, String description, String adress, String house_type, int kol_kom, double square_d, String url)
        {
            string queryString = "UPDATE ads2 SET id='" + id + "', title='" + title + "', price='" + price + "', ets_count='" + ets_count + "', et_num='" + et_num + "', description='" + description + "', adress='" + adress + "', house_type='" + house_type + "', kol_kom='" + kol_kom + "', square_d='" + square_d + "', url='" + url + "';";
            using (MySqlConnection con = new MySqlConnection())
            {
                con.ConnectionString = mysqlCSB.ConnectionString;
                MySqlCommand com = new MySqlCommand(queryString, con);
                try
                {
                    con.Open();
                    using (MySqlDataReader dr = com.ExecuteReader())
                    {
                        if (dr.HasRows)
                        {
                            dt.Load(dr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //   return dt;
        }
        // парсинг внутренних страниц
        private void angle2(String url)
        {


            String Html = getResponse(url);
            var parser = new HtmlParser();
            var document = parser.Parse(Html);

            var descriptions = document.QuerySelectorAll(".item-view-content");

            foreach (var item in descriptions)
            {

                String title = item.GetElementsByClassName("title-info-title-text").First().TextContent.Trim();
                //выделяем дату
                String date_id = item.GetElementsByClassName("title-info-metadata-item").First().TextContent.Trim();
                int id_start = date_id.IndexOf("№ ") + "№ ".Length;
                int id_count = 9;
                String date = date_id.Substring(date_id.IndexOf(", размещено ") + ", размещено ".Length, date_id.Length - (date_id.IndexOf(", размещено ") + ", размещено ".Length));
               
                //выделяем цену
                String price = "-1";
                if (document.QuerySelectorAll(".price-value-string").Count() != 0)
                    price = document.QuerySelectorAll(".price-value-string").First().TextContent.Trim();
                //выделяем адрес
                String adress = document.QuerySelectorAll(".item-map-address").First().TextContent.Trim();
                //выделяем описание
                String description = "";
                if (document.QuerySelectorAll(".item-description").Count() != 0)
                    description = document.QuerySelectorAll(".item-description").First().TextContent.Trim();

                //выделяем тип дома
                String house_type = "";
                String kom_count = "";
                String et = "";
                String ets = "";
                String square = "";
                IHtmlCollection<IElement> elements = document.QuerySelectorAll(".item-params-list-item");
                foreach (var elsitem in elements)
                {
                    if (elsitem.TextContent.Trim().IndexOf("Площадь:") != -1)
                    {
                        square = elsitem.TextContent.Trim();
                        if (square.Length > "Площадь:".Length)
                        {
                            square = square.Remove(0, "Площадь:".Length).Trim();
                            square = square.Remove(square.IndexOf("м"), 2);
                            square = square.Trim();
                        }
                    }
                    if (elsitem.TextContent.Trim().IndexOf("Этажей в доме:") != -1)
                    {
                        ets = elsitem.TextContent.Trim();
                        if (ets.Length > "Этажей в доме:".Length)
                        {
                            ets = ets.Remove(0, "Этажей в доме:".Length).Trim();
                        }
                    }
                    if (elsitem.TextContent.Trim().IndexOf("Этаж:") != -1)
                    {
                        et = elsitem.TextContent.Trim();
                        if (et.Length > "Этаж:".Length)
                        {
                            et = et.Remove(0, "Этаж:".Length).Trim();
                        }
                    }
                    if (elsitem.TextContent.Trim().IndexOf("Тип дома:") != -1)
                    {
                        house_type = elsitem.TextContent.Trim();
                        if (house_type.Length > "Тип дома:".Length)
                        {

                            house_type = house_type.Remove(0, "Тип дома:".Length).Trim();
                        }
                    }
                    if (elsitem.TextContent.Trim().IndexOf("Количество комнат:") != -1)
                    {
                        kom_count = elsitem.TextContent.Trim();
                        if (kom_count.Length > "Количество комнат:".Length)
                        {
                            kom_count = kom_count.Remove(0, "Количество комнат:".Length).Trim();
                            int index = kom_count.IndexOf("-");
                            if (index == -1) index = kom_count.IndexOf("-");
                            if (index == -1) index = kom_count.IndexOf("-");
                            if (index == -1) index = kom_count.IndexOf("-");
                            if (index == -1) index = kom_count.ToLower().IndexOf("многокомнатные");
                            if (index != -1)
                                kom_count = kom_count.Remove(index);
                        }

                    }
                }

                //выделяем id объявления
                int id = 0;


                int et_int = 0;

                double square_d = 0;
                int ets_int = 0;


                // выделяем цену
                int price_int = -1;
                try
                {
                    id = int.Parse(date_id.Substring(id_start, id_count));
                    insert_ = check_ID(id);
                    et_int = int.Parse(et);
                    ets_int = int.Parse(ets);


                    square = square.Replace(".", ",");
                    square = square.Replace(".", ",");

                    square_d = double.Parse(square);
                    price = price.Trim().Substring(0, price.Length - 1).Trim();
                    price = RemoveSpaces(price);
                    price_int = int.Parse(price);
                }
                catch (Exception ex)
                {

                }
                finally
                {
                    // запись в result
                    result = result + "Заголовок:" + title + Environment.NewLine;
                    result = result + "ID:" + id + Environment.NewLine;
                    result = result + "Этаж:" + et_int + Environment.NewLine;
                    result = result + "Этажей в доме:" + ets_int + Environment.NewLine;
                    result = result + "Количество комнат:" + kom_count + Environment.NewLine;
                    result = result + "Площадь:" + square_d + Environment.NewLine;
                    result = result + "Тип дома:" + house_type + Environment.NewLine;
                    result = result + "Дата:" + date + Environment.NewLine;
                    result = result + "Цена:" + price + Environment.NewLine;
                    result = result + "Адрес:" + adress + Environment.NewLine;
                    result = result + "Описание:" + description + Environment.NewLine;
                    int kol_kom = -1;
                    //выделяем количество комнат
                    try
                    {
                        kol_kom = int.Parse(kom_count);
                        if (insert_)
                            insert_rec(id, title, price_int, ets_int, et_int, description, adress, house_type, kol_kom, square_d, parseUrl);
                        else
                            update_rec(id, title, price_int, ets_int, et_int, description, adress, house_type, kol_kom, square_d, parseUrl);
                    }
                    catch (Exception ex)
                    {
                        if (insert_)
                            insert_rec(id, title, price_int, ets_int, et_int, description, adress, house_type, -1, square_d, parseUrl);
                        else
                            update_rec(id, title, price_int, ets_int, et_int, description, adress, house_type, -1, square_d, parseUrl);
                    }
                }
            }
        }

        private bool check_ID(int id)
        {
            string queryString = "SELECT * FROM ads2 WHERE ads_id='" + id + "'";
            using (MySqlConnection con = new MySqlConnection())
            {
                con.ConnectionString = mysqlCSB.ConnectionString;
                MySqlCommand com = new MySqlCommand(queryString, con);
                try
                {
                    con.Open();
                    using (MySqlDataReader dr = com.ExecuteReader())
                    {
                        if (dr.HasRows)
                        {
                            return false;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return true;
            }

        }
        private void insert_rec(int id, String title, int price, int ets_count, int et_num, String description, String adress, String house_type, int kol_kom, double square_d, String url)
        {
            string queryString = "INSERT INTO ads2(id, ads_ID, title, price, ets_count, et_num, description, adress, house_type, kol_kom, square_d, url) VALUES('','" + id + "', '" + title + "', '" + price + "', '" + ets_count + "', '" + et_num + "', '" + description + "', '" + adress + "', '" + house_type + "', '" + kol_kom + "', '" + square_d + "', '" + url + "')";
            using (MySqlConnection con = new MySqlConnection())
            {
                con.ConnectionString = mysqlCSB.ConnectionString;
                MySqlCommand com = new MySqlCommand(queryString, con);
                try
                {
                    con.Open();
                    using (MySqlDataReader dr = com.ExecuteReader())
                    {
                        if (dr.HasRows)
                        {
                            dt.Load(dr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //   return dt;
        }

        protected override void OnStop()
        {
            tr.Abort();
        }
    }
}
