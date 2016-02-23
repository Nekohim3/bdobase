using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
namespace bdobase
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        List<Quest> quests = new List<Quest>();
        List<Quest> FilteredQuest = new List<Quest>();
        class Item
        {
            public string name { get; set; }
            public string id { get; set; }
            public string desc { get; set; }
        }
        List<Item> FilteredItems = new List<Item>();
        List<Item> items = new List<Item>();
        List<string> usedQuests;
        VisualQuest vqm;
        Point m_start;
        Vector m_startOffset;
        double scale = 1;
        bool fulls = false;
        List<string> QuestTypes = new List<string>();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //loadItems();
            //getAllItems();
            loadQuests();
            FixQuests();
            FilterInit();
            //SelectQuest("128:2");
            //Thread th = new Thread(() => { QuestsUpdate(false); });
            //th.Start();
        }

        void FixQuests()
        {
            for (int i = 0; i < quests.Count; i++)
            {
                foreach (var q in quests[i].nextQid)
                {
                    var quest = getQuest(q);
                    if(quest.prevQid != null)
                        if (quest.prevQid.All(x => x != quests[i].id.ToString()))
                            quest.prevQid.Add(quests[i].id);
                }
            }
            for (int i = 0; i < quests.Count; i++)
            {
                foreach (var q in quests[i].prevQid)
                {
                    var quest = getQuest(q);
                    if (quest.nextQid != null)
                        if (quest.nextQid.All(x => x != quests[i].id.ToString()))
                            quest.nextQid.Add(quests[i].id);
                }
            }
        }
        void SelectQuest(string id, bool vert = false)
        {
            TreeCanvas.Children.Clear();
            vqm = new VisualQuest(getQuest(id));
            usedQuests = new List<string>();
            FillTree(vqm);
            SetEventsTree(vqm);
            InitTree(vqm, vqm);
            if (vqm.Children.Count == 0) vqm.loc = 0;
            DrawTree(vqm, vert);
            DrawLines(vqm, vert);
            tt.X = MainGrid.ActualWidth / 2 - 100;
            tt.Y = MainGrid.ActualHeight / 2 - 30 - Canvas.GetTop(vqm.g);
        }
        void FillTree(VisualQuest vq)
        {
            foreach(string q in getQuest(vq.qid).prevQid)
            {
                if (usedQuests.Where(x => x == q).Count() == 0)
                {
                    vq.Children.Add(new VisualQuest(getQuest(q)));
                    usedQuests.Add(q);
                    FillTree(vq.Children.Last());
                }
            }

        }
        void SetEventsTree(VisualQuest vq)
        {
            vq.g.PreviewMouseDown += G_PreviewMouseDown;
            foreach (VisualQuest q in vq.Children)
                SetEventsTree(q);
        }
        void InitTree(VisualQuest vq, VisualQuest mvq, double pos = 0, int level = 0)
        {
            for (int i = 0; i < vq.Children.Count; i++)
            {
                double npos = pos + (i - (vq.Children.Count - 1) / 2.0);
                while (TreeLevelPosCheck(mvq, level + 1, npos))
                    npos += 0.5;
                vq.Children[i].loc = npos;
                InitTree(vq.Children[i], mvq, vq.Children[i].loc, level + 1);
            }
            if (vq.Children.Count != 0)
            {
                double avg = 0;
                for (int i = 0; i < vq.Children.Count; i++)
                    avg += vq.Children[i].loc;
                avg /= vq.Children.Count;
                vq.loc = avg;
            }
        }
        void DrawTree(VisualQuest vq, bool vert, int level = 0)
        {
            TreeCanvas.Children.Add(vq.g);
            if (!vert)
            {
                Canvas.SetTop(vq.g, vq.loc * 70);
                Canvas.SetLeft(vq.g, level * 230);
            }
            else
            {
                Canvas.SetLeft(vq.g, vq.loc * 210);
                Canvas.SetTop(vq.g, level * 90);
            }
            foreach (VisualQuest q in vq.Children)
                DrawTree(q, vert, level + 1);
        }
        bool TreeLevelPosCheck(VisualQuest vq, int lvl, double pos, int level = 0)
        {
            bool col = false;
            if (level != lvl)
            {
                for (int i = 0; i < vq.Children.Count; i++)
                {
                    col = TreeLevelPosCheck(vq.Children[i], lvl, pos, level + 1);
                    if (col) break;
                }
            }
            else
            {
                if (vq.loc + 1 > pos)
                    return true;
                else
                    return false;
            }
            return col;
        }
        void DrawLines(VisualQuest vq, bool vert)
        {
            foreach(VisualQuest q in vq.Children)
            {
                Line l = new Line();
                l.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                l.StrokeThickness = 2;
                if (!vert)
                {
                    l.X1 = Canvas.GetLeft(vq.g) + 200;
                    l.Y1 = Canvas.GetTop(vq.g) + 30;
                    l.X2 = Canvas.GetLeft(q.g);
                    l.Y2 = Canvas.GetTop(q.g) + 30;
                }
                else
                {
                    l.X1 = Canvas.GetLeft(vq.g) + 100;
                    l.Y1 = Canvas.GetTop(vq.g) + 60;
                    l.X2 = Canvas.GetLeft(q.g) + 100;
                    l.Y2 = Canvas.GetTop(q.g);
                }
                TreeCanvas.Children.Add(l);
                DrawLines(q, vert);
            }
        }
        void RefreshTree(VisualQuest vq)
        {
            vq.g.Background = (getQuest(vq.qid).made) ? new SolidColorBrush(Color.FromArgb(0xff, 0xaa, 0xff, 0xaa)) : new SolidColorBrush(Color.FromArgb(0xff, 0xaa, 0xaa, 0xaa));
            foreach (VisualQuest q in vq.Children)
                RefreshTree(q);
        }
        void SaveQuest(Quest quest)
        {
            using (FileStream fs = File.Open("quests/" + quest.id.Replace(":","-"), FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write
                            (
                            quest.id + "<:_:>" +
                            quest.name + "<:_:>" +
                            quest.rlevel + "<:_:>" +
                            quest.category + "<:_:>" +
                            quest.region + "<:_:>" +
                            quest.sNPC + "<:_:>" +
                            quest.sNPCid + "<:_:>" +
                            quest.eNPC + "<:_:>" +
                            quest.eNPCid + "<:_:>" +
                            quest.nlevel + "<:_:>" +
                            quest.bdoclass + "<:_:>" +
                            quest.rep + "<:_:>" +
                            quest.chargeTime + "<:_:>"
                            );
                foreach (Reward q in quest.BaseReward)
                    sw.Write(q.id + "<:-:>" + q.count + "<:+:>");
                sw.Write("<:_:>");
                foreach (Reward q in quest.SelectReward)
                    sw.Write(q.id + "<:-:>" + q.count + "<:+:>");
                sw.Write("<:_:>");
                foreach (string q in quest.prevQid)
                    sw.Write(q + "<:+:>");
                sw.Write("<:_:>");
                foreach (string q in quest.nextQid)
                    sw.Write(q + "<:+:>");
                sw.Write("<:_:>");
                foreach (string q in quest.endQid)
                    sw.Write(q + "<:+:>");
                sw.Write("<:_:>" + quest.made.ToString());
            }
        }
        void FilterInit()
        {
            LB_FQuests.Items.Clear();
            FilteredQuest = quests.ToList();
            for (int i = 0; i < FilteredQuest.Count; i++)
                LB_FQuests.Items.Add(FilteredQuest[i].name);
            CB_QuestType.Items.Add("Все");
            for (int i = 0; i < QuestTypes.Count; i++)
                CB_QuestType.Items.Add(QuestTypes[i]);
            CB_QuestType.SelectedIndex = 0;
            CB_Level_Type.Items.Add("-");
            CB_Level_Type.Items.Add("Рекомендуемый уровень");
            CB_Level_Type.Items.Add("Необходимый уровень");
            CB_Level_Type.SelectedIndex = 0;
            CB_Level_Side.Items.Add(">=");
            CB_Level_Side.Items.Add("<=");
            CB_Level_Side.SelectedIndex = 0;
            for (int i = 0; i <= 60; i++)
                CB_Level_Value.Items.Add(i.ToString());
            CB_Level_Value.SelectedIndex = 0;
        }
        void Filter()
        {
            LB_FQuests.Items.Clear();
            FilteredQuest = quests.Where(x => x.name.ToLower().IndexOf(TB_QName.Text) != -1).ToList();
            if(CB_QuestType.SelectedIndex > 0)
                FilteredQuest = FilteredQuest.Where(x => x.category == CB_QuestType.SelectedItem.ToString()).ToList();
            if (CB_Level_Type.SelectedIndex == 1)
            {
                if (CB_Level_Side.SelectedIndex == 0)
                    FilteredQuest = FilteredQuest.Where(x => Convert.ToInt32(x.rlevel) >= CB_Level_Value.SelectedIndex).ToList();
                if(CB_Level_Side.SelectedIndex == 1)
                    FilteredQuest = FilteredQuest.Where(x => Convert.ToInt32(x.rlevel) <= CB_Level_Value.SelectedIndex).ToList();
            }
            if (CB_Level_Type.SelectedIndex == 2)
            {
                if (CB_Level_Side.SelectedIndex == 0)
                    FilteredQuest = FilteredQuest.Where(x => Convert.ToInt32(x.nlevel) >= CB_Level_Value.SelectedIndex).ToList();
                if (CB_Level_Side.SelectedIndex == 1)
                    FilteredQuest = FilteredQuest.Where(x => Convert.ToInt32(x.nlevel) <= CB_Level_Value.SelectedIndex).ToList();
            }
            ///////////////////////////////////////////////////////////////////////
            for (int i = 0; i < FilteredQuest.Count; i++)
                LB_FQuests.Items.Add(FilteredQuest[i].name);
        }
        void ShowQuestInfo(string id)
        {
            InfoGrid.Margin = new Thickness(0, 0, 0, 0);
            MainGrid.Margin = new Thickness(0, 0, 0, 175);
            Quest quest = getQuest(id);
            TB_INF_ID.Text = quest.id;
            TB_INF_Name.Text = quest.name;
            TB_INF_Category.Text = quest.category;
            TB_INF_Region.Text = quest.region;
            TB_INF_RLevel.Text = quest.rlevel;
            TB_INF_NLevel.Text = quest.nlevel;
            TB_INF_sNPC.Text = quest.sNPC;
            TB_INF_eNPC.Text = quest.eNPC;
            TB_INF_rep.Text = quest.rep;
            TB_INF_rtime.Text = quest.chargeTime;
            TB_INF_Class.Text = quest.bdoclass;
            TB_INF_BR.Text = "";
            TB_INF_SR.Text = "";
            foreach (Reward q in quest.BaseReward)
                TB_INF_BR.Text += File.ReadAllText("items/" + q.id) + " [" + q.count + "]\n";
            foreach (Reward q in quest.SelectReward)
                TB_INF_SR.Text += File.ReadAllText("items/" + q.id) + " [" + q.count + "]\n";
        }
        void HideQuestInfo()
        {
            InfoGrid.Margin = new Thickness(0, 0, 0, -175);
            MainGrid.Margin = new Thickness(0, 0, 0, 0);
        }
        #region Quests
        Quest getQuest(string id)
        {
            return quests.Where(x => x.id == id).First();
        }
        void loadQuests()
        {
            QuestTypes = new List<string>();
            quests = new List<Quest>();
            string[] files = Directory.GetFiles("quests");
            for (int i = 0; i < files.Length; i++)
            {
                if (File.ReadAllBytes(files[i]).Length == 0) continue;
                using (FileStream fs = File.Open(files[i], FileMode.Open, FileAccess.Read))
                using (StreamReader sr = new StreamReader(fs))
                {
                    string[] str = sr.ReadToEnd().Split(new string[] { "<:_:>" }, StringSplitOptions.None);
                    Quest quest = new Quest();
                    quest.id = str[0];
                    quest.name = str[1];//
                    quest.rlevel = str[2];//
                    quest.category = str[3];//
                    quest.region = str[4];//
                    quest.sNPC = str[5];
                    quest.sNPCid = str[6];
                    quest.eNPC = str[7];
                    quest.eNPCid = str[8];
                    quest.nlevel = str[9];//
                    quest.bdoclass = str[10];//
                    quest.rep = str[11];
                    quest.chargeTime = str[12];
                    string[] brstr = str[13].Split(new string[] { "<:+:>" }, StringSplitOptions.RemoveEmptyEntries);
                    quest.BaseReward = new List<Reward>();
                    foreach (string s in brstr)
                        quest.BaseReward.Add(new Reward() { id = s.Split(new string[] { "<:-:>" }, StringSplitOptions.None)[0], count = s.Split(new string[] { "<:-:>" }, StringSplitOptions.None)[1] });
                    string[] srstr = str[14].Split(new string[] { "<:+:>" }, StringSplitOptions.RemoveEmptyEntries);
                    quest.SelectReward = new List<Reward>();
                    foreach (string s in srstr)
                        quest.SelectReward.Add(new Reward() { id = s.Split(new string[] { "<:-:>" }, StringSplitOptions.None)[0], count = s.Split(new string[] { "<:-:>" }, StringSplitOptions.None)[1] });
                    string[] pqstr = str[15].Split(new string[] { "<:+:>" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in pqstr)
                        quest.prevQid.Add(s);
                    string[] nqstr = str[16].Split(new string[] { "<:+:>" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in nqstr)
                        quest.nextQid.Add(s);
                    string[] eqstr = str[17].Split(new string[] { "<:+:>" } ,StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in eqstr)
                        quest.endQid.Add(s);
                    quest.made = Convert.ToBoolean(str[18]);
                    quests.Add(quest);
                    if (QuestTypes.Where(x => x == quest.category).Count() == 0)
                        QuestTypes.Add(quest.category);
                }
            }
        }
        void FillQuests()
        {
            string[] files = Directory.GetFiles("quests");
            for (int i = 0; i < files.Length; i++)
            {
                Dispatcher.Invoke(new Action(() => { Title = i.ToString(); }));
                try
                {
                    if (File.ReadAllBytes(files[i]).Length != 0) continue;
                    using (FileStream fs = File.Open(files[i], FileMode.Open, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        string str = files[i].Replace("quests\\", "").Replace("-", ":");
                        string html = getQuestPage(str);
                        Quest quest = new Quest();
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(html);
                        quest.id = str;
                        quest.name = doc.DocumentNode.SelectSingleNode("//*[@id='content_block']/div[2]/div/div[2]/div[1]/div[2]/div").InnerText;
                        quest.rlevel = doc.DocumentNode.SelectSingleNode("//*[@id='content_block']/div[2]/div/div[2]").Descendants("div").Where(x => x.GetAttributeValue("class", "") == "item_info").First().Descendants("tr").Where(x => x.InnerText.IndexOf("Рекомендуемый ур:") != -1).First().Descendants("td").Last().InnerText.Replace("null", "0");
                        quest.category = doc.DocumentNode.SelectSingleNode("//*[@id='content_block']/div[2]/div/div[2]").Descendants("div").Where(x => x.GetAttributeValue("class", "") == "item_info").First().Descendants("tr").Where(x => x.InnerText.IndexOf("Категория:") != -1).First().Descendants("td").Last().InnerText;
                        quest.chargeTime = doc.DocumentNode.SelectSingleNode("//*[@id='content_block']/div[2]/div/div[2]").Descendants("div").Where(x => x.GetAttributeValue("class", "") == "item_info").First().Descendants("tr").Where(x => x.InnerText.IndexOf("Время восстановления:") != -1).First().Descendants("td").Last().InnerText;
                        quest.region = doc.DocumentNode.SelectSingleNode("//*[@id='content_block']/div[2]/div/div[3]/div[1]/table/tr[1]/td[2]").InnerText;
                        quest.sNPC = doc.DocumentNode.SelectSingleNode("//*[@id='content_block']/div[2]/div/div[3]/div[1]/table/tr[2]/td[2]/a").InnerText;
                        quest.sNPCid = doc.DocumentNode.SelectSingleNode("//*[@id='content_block']/div[2]/div/div[3]/div[1]/table/tr[2]/td[2]/a").GetAttributeValue("href", "").Remove(0, 5);
                        quest.eNPC = doc.DocumentNode.SelectSingleNode("//*[@id='content_block']/div[2]/div/div[3]/div[1]/table/tr[3]/td[2]/a").InnerText;
                        quest.eNPCid = doc.DocumentNode.SelectSingleNode("//*[@id='content_block']/div[2]/div/div[3]/div[1]/table/tr[3]/td[2]/a").GetAttributeValue("href", "").Remove(0, 5);
                        quest.nlevel = (doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "q_f_level").Count() != 0) ? (IsNum(doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "q_f_level").First().LastChild.InnerText) ? doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "q_f_level").First().LastChild.InnerText : "0") : "0";
                        
                        quest.bdoclass = (doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "q_f_class").Count() != 0) ? doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "q_f_class").First().LastChild.InnerText : "all";
                        quest.rep = (doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "q_f_repa").Count() != 0) ? doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "q_f_repa").First().Descendants("span").First().InnerText : "0";
                        foreach (HtmlNode q in doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "quest_BaseReward").First().Descendants("a"))
                            quest.BaseReward.Add(new Reward() { id = q.GetAttributeValue("href", "").Remove(0, 6), count = (q.Descendants("span").Count() != 0) ? q.Descendants("span").Where(x => x.GetAttributeValue("class", "") == "col_item").First().InnerText : "1" });
                        foreach (HtmlNode q in doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "quest_SelectReward").First().Descendants("a"))
                            quest.SelectReward.Add(new Reward() { id = q.GetAttributeValue("href", "").Remove(0, 6), count = (q.Descendants("span").Count() != 0) ? q.Descendants("span").Where(x => x.GetAttributeValue("class", "") == "col_item").First().InnerText : "1" });
                        if (doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "list_quest").Count() != 0)
                        {
                            if (doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "list_quest" && x.Descendants("h3").First().InnerText.IndexOf("Предыдущие квесты:") != -1).Count() != 0)
                                foreach (HtmlNode q in doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "list_quest" && x.Descendants("h3").First().InnerText.IndexOf("Предыдущие квесты:") != -1).First().Descendants("tr"))
                                    if (q.GetAttributeValue("class", "") == "one_list_quest")
                                        quest.prevQid.Add(q.Descendants("td").Where(x => x.GetAttributeValue("class", "") == "one_list_grup_q_one").First().Descendants("a").First().GetAttributeValue("href", "").Remove(0, 7));

                            if (doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "list_quest" && x.Descendants("h3").First().InnerText.IndexOf("Следующие квесты:") != -1).Count() != 0)
                                foreach (HtmlNode q in doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "list_quest" && x.Descendants("h3").First().InnerText.IndexOf("Следующие квесты:") != -1).First().Descendants("tr"))
                                    if (q.GetAttributeValue("class", "") == "one_list_quest")
                                        quest.nextQid.Add(q.Descendants("td").Where(x => x.GetAttributeValue("class", "") == "one_list_grup_q_one").First().Descendants("a").First().GetAttributeValue("href", "").Remove(0, 7));

                        }
                        if (doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "q_f_q").Count() != 0)
                            foreach (HtmlNode q in doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "q_f_q"))
                                quest.endQid.Add(q.Descendants("a").First().GetAttributeValue("href", "").Remove(0, 7));
                        sw.Write
                            (
                            quest.id + "<:_:>" +
                            quest.name + "<:_:>" +
                            quest.rlevel + "<:_:>" +
                            quest.category + "<:_:>" +
                            quest.region + "<:_:>" +
                            quest.sNPC + "<:_:>" +
                            quest.sNPCid + "<:_:>" +
                            quest.eNPC + "<:_:>" +
                            quest.eNPCid + "<:_:>" +
                            quest.nlevel + "<:_:>" +
                            quest.bdoclass + "<:_:>" +
                            quest.rep + "<:_:>" +
                            quest.chargeTime + "<:_:>"
                            );
                        foreach (Reward q in quest.BaseReward)
                            sw.Write(q.id + "<:-:>" + q.count + "<:+:>");
                        sw.Write("<:_:>");
                        foreach (Reward q in quest.SelectReward)
                            sw.Write(q.id + "<:-:>" + q.count + "<:+:>");
                        sw.Write("<:_:>");
                        foreach (string q in quest.prevQid)
                            sw.Write(q + "<:+:>");
                        sw.Write("<:_:>");
                        foreach (string q in quest.nextQid)
                            sw.Write(q + "<:+:>");
                        sw.Write("<:_:>");
                        foreach (string q in quest.endQid)
                            sw.Write(q + "<:+:>");
                        sw.Write("<:_:>" + quest.made.ToString());
                    }

                }
                catch { }
            }
        }
        void QuestParse(string str)
        {
            dynamic obj = JsonConvert.DeserializeObject(str);
            for (int i = 0; i < obj.rows.Count; i++)
                using (FileStream fs = File.Open("quests/" + obj.rows[i].id.ToString().Replace(":", "-"), FileMode.Create, FileAccess.Write)) { }
        }
        void QuestsUpdate(bool full)
        {
            if (full)
                QuestParse(getQuestsFromSite());
            FillQuests();

        }
        string getQuestPage(string id)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("http://www.bdobase.info/quest/" + id);
            req.Proxy = null;
            req.Method = "GET";
            req.Timeout = 1000;
            req.Referer = "http://www.bdobase.info/quests";
            req.Accept = "application/json, text/javascript, */*; q=0.01";
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.85 Safari/537.36 OPR/32.0.1948.25";
            req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            req.Headers.Add("Cookie", "__cfduid=daaa195945340f99416a5fc9f0aa1f4831441308799; PHPSESSID=c8o18o1dodrdf7ilvp7t3tc5p2; _ym_visorc_29256755=w");
            req.Headers.Add("X-Requested-With", "XMLHttpRequest");
            req.Headers.Add("Origin", "http://www.bdobase.info");
            WebResponse resp = req.GetResponse();
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string str = sr.ReadToEnd();
            sr.Close();
            return str;
        }
        string getQuestsFromSite()
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("http://www.bdobase.info/scripts/quests.php");
            req.Proxy = null;
            req.Method = "POST";
            req.Referer = "http://www.bdobase.info/quests";
            req.Accept = "application/json, text/javascript, */*; q=0.01";
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.85 Safari/537.36 OPR/32.0.1948.25";
            req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            req.Headers.Add("Cookie", "__cfduid=daaa195945340f99416a5fc9f0aa1f4831441308799; PHPSESSID=fc0f96165b8e24ff64585bd6ad0de2f0; _ym_visorc_29256755=w");
            req.Headers.Add("X-Requested-With", "XMLHttpRequest");
            req.Headers.Add("Origin", "http://www.bdobase.info");
            byte[] sentData = Encoding.GetEncoding(1251).GetBytes("page=1&rp=11000&sortname=name&sortorder=asc&query=&qtype=name");
            req.ContentLength = sentData.Length;
            Stream sendStream = req.GetRequestStream();
            sendStream.Write(sentData, 0, sentData.Length);
            sendStream.Flush();
            sendStream.Close();
            WebResponse resp = req.GetResponse();
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string str = sr.ReadToEnd();
            sr.Close();
            return str;
        }
        #endregion
        #region Items
        void loadItems()
        {
            items = new List<Item>();
            string[] files = Directory.GetFiles("items");
            for (int i = 0; i < files.Length; i++)
            {
                if (File.ReadAllBytes(files[i]).Length == 0) continue;
                using (FileStream fs = File.Open(files[i], FileMode.Open, FileAccess.Read))
                using (StreamReader sr = new StreamReader(fs))
                {
                    string[] str = sr.ReadToEnd().Split(new string[] { "<:-:>" }, StringSplitOptions.None);
                    Item it = new Item();
                    it.id = files[i].Replace("items\\", "");
                    it.name = str[0];
                    it.desc = str[1];
                    items.Add(it);
                }
            }
        }
        void getAllItems()
        {
            int errcount = 0;
            int counter = 0;
            while(errcount < 10000)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    this.Title = counter.ToString() + " / " + errcount.ToString(); 
                }));
                string html = getItemPage(counter.ToString());
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                if (doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "content").First().Descendants("div").Where(x => x.GetAttributeValue("class", "") == "single_item_name").Count() != 0)
                {
                    errcount = 0;
                    string q = doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "content").First().Descendants("div").Where(x => x.GetAttributeValue("class", "") == "single_item_name").First().InnerText;
                    string qq = doc.GetElementbyId("content_block").Descendants("meta").Where(x => x.GetAttributeValue("name", "") == "description").First().GetAttributeValue("content", "");
                    try
                    {
                        string q1 = doc.DocumentNode.Descendants("div").Where(x => x.GetAttributeValue("class", "") == "content").First().Descendants("div").Where(x => x.GetAttributeValue("class", "") == "single_item_name").First().Descendants("span").First().InnerText;
                        q = q.Replace(q1, "").Replace("\n", "");
                    }
                    catch { }
                    using (FileStream fs = File.Open("items/" + counter.ToString(), FileMode.Create, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(q + "<:-:>" + qq);
                    }
                }
                else
                {
                    errcount++;
                }
                counter++;
            }
            
        }
        string getItemPage(string id)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("http://www.bdobase.info/item/" + id);
            req.Proxy = null;
            req.Method = "GET";
            req.Referer = "http://www.bdobase.info/";
            req.Accept = "application/json, text/javascript, */*; q=0.01";
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.85 Safari/537.36 OPR/32.0.1948.25";
            req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            req.Headers.Add("Cookie", "__cfduid=daaa195945340f99416a5fc9f0aa1f4831441308799; PHPSESSID=fc0f96165b8e24ff64585bd6ad0de2f0; _ym_visorc_29256755=w");
            req.Headers.Add("X-Requested-With", "XMLHttpRequest");
            req.Headers.Add("Origin", "http://www.bdobase.info");
            WebResponse resp = req.GetResponse();
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string str = sr.ReadToEnd();
            sr.Close();
            return str;
        }
        #endregion
        private void G_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                string qid = (sender as Grid).Name.Remove(0, 1).Replace("n", ":");
                getQuest(qid).made = true;
                SaveQuest(getQuest(qid));
                RefreshTree(vqm);
            }
            if (e.ChangedButton == MouseButton.Middle)
            {
                string qid = (sender as Grid).Name.Remove(0, 1).Replace("n", ":");
                getQuest(qid).made = false;
                SaveQuest(getQuest(qid));
                RefreshTree(vqm);
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                ShowQuestInfo((sender as Grid).Name.Remove(0,1).Replace("n",":"));
            }
        }
        private void TreeCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            m_start = e.GetPosition(MainGrid);
            m_startOffset = new Vector(tt.X, tt.Y);
            TreeCanvas.CaptureMouse();
        }
        private void TreeCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TreeCanvas.ReleaseMouseCapture();
        }
        private void TreeCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (TreeCanvas.IsMouseCaptured)
            {
                Vector offset = Point.Subtract(e.GetPosition(MainGrid), m_start);

                tt.X = m_startOffset.X + offset.X * scale;
                tt.Y = m_startOffset.Y + offset.Y * scale;
            }
        }
        private void MainGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(e.Delta > 0)
            {
                if (st.ScaleX > 5) return;
                st.ScaleX *= 1.5;
                st.ScaleY *= 1.5;
                scale /= 1.5;
            }
            else
            {
                if (st.ScaleX < 0.03) return;
                st.ScaleX /= 1.5;
                st.ScaleY /= 1.5;
                scale *= 1.5;
            }
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Q)
            {
                if(fulls)
                {
                    fulls = false;
                    FilterGrid.Margin = new Thickness(0, 0, 0, 0);
                    MainGrid.Margin = new Thickness(0, 0, 300, 0);
                }
                else
                {
                    fulls = true;
                    FilterGrid.Margin = new Thickness(0, 0, -300, 0);
                    MainGrid.Margin = new Thickness(0, 0, 0, 0);
                }
            }
        }
        private void LB_FQuests_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LB_FQuests.SelectedIndex == -1) return;
            SelectQuest(FilteredQuest[LB_FQuests.SelectedIndex].id);
        }
        private void CB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Filter();
        }
        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Filter();
        }
        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            SelectQuest(FilteredQuest[LB_FQuests.SelectedIndex].id, CHB_VV.IsChecked.Value);
        }
        static bool IsNum(string q)
        {
            int qwe = 0;
            return int.TryParse(q, out qwe);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            HideQuestInfo();
        }
        private void button_Click_1(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.bdobase.info/quest/" + TB_INF_ID.Text);
        }
        
    }
}
class VisualQuest
{
    public VisualQuest(Quest quest)
    {
        qid = quest.id;
        loc = double.NaN;
        Children = new List<VisualQuest>();
        g = new Grid();
        g.Width = 200;
        g.Height = 60;
        g.Background = (quest.made) ? new SolidColorBrush(Color.FromArgb(0xff, 0xaa, 0xff, 0xaa)) : new SolidColorBrush(Color.FromArgb(0xff, 0xaa, 0xaa, 0xaa));
        g.Name = "q" + quest.id.Replace(":", "n");

        tbName = new TextBlock();
        tbName.Padding = new Thickness(3, 3, 3, 3);
        tbName.Height = 40;
        tbName.TextAlignment = TextAlignment.Center;
        tbName.TextWrapping = TextWrapping.Wrap;
        tbName.VerticalAlignment = VerticalAlignment.Top;
        tbName.FontSize = 13;
        tbName.FontWeight = FontWeights.Bold;
        tbName.Foreground = new SolidColorBrush(Color.FromArgb(255,0,0,0));

        tbRLevel = new TextBlock();
        tbRLevel.Padding = new Thickness(2, 2, 2, 2);
        tbRLevel.Height = 20;
        tbRLevel.Width = 30;
        tbRLevel.TextAlignment = TextAlignment.Center;
        tbRLevel.TextWrapping = TextWrapping.Wrap;
        tbRLevel.VerticalAlignment = VerticalAlignment.Bottom;
        tbRLevel.HorizontalAlignment = HorizontalAlignment.Left;
        tbRLevel.FontSize = 13;
        tbRLevel.FontWeight = FontWeights.Bold;
        tbRLevel.Background = new SolidColorBrush(Color.FromArgb(0xff, 0x99, 0xff, 0x99));
        tbRLevel.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

        tbNLevel = new TextBlock();
        tbNLevel.Padding = new Thickness(2, 2, 2, 2);
        tbNLevel.Height = 20;
        tbNLevel.Width = 30;
        tbNLevel.TextAlignment = TextAlignment.Center;
        tbNLevel.TextWrapping = TextWrapping.Wrap;
        tbNLevel.VerticalAlignment = VerticalAlignment.Bottom;
        tbNLevel.HorizontalAlignment = HorizontalAlignment.Right;
        tbNLevel.FontSize = 13;
        tbNLevel.FontWeight = FontWeights.Bold;
        tbNLevel.Background = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x99, 0x99));
        tbNLevel.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

        tbQType = new TextBlock();
        tbQType.Padding = new Thickness(0, 0, 0, 0);
        tbQType.Height = 20;
        tbQType.TextAlignment = TextAlignment.Center;
        tbQType.TextWrapping = TextWrapping.Wrap;
        tbQType.VerticalAlignment = VerticalAlignment.Bottom;
        tbQType.FontSize = 13;
        tbQType.FontWeight = FontWeights.Bold;
        tbQType.Margin = new Thickness(30, 0, 30, 0);
        tbQType.Background = new SolidColorBrush(Color.FromArgb(0xff, 0x99, 0x99, 0xff));
        tbQType.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

        g.Children.Add(tbName);
        g.Children.Add(tbRLevel);
        g.Children.Add(tbNLevel);
        g.Children.Add(tbQType);

        tbName.Text = quest.name;
        tbRLevel.Text = quest.rlevel;
        tbNLevel.Text = quest.nlevel;
        tbQType.Text = quest.category;
    }
    public VisualQuest(int num)
    {
        loc = double.NaN;
        Children = new List<VisualQuest>();
        g = new Grid();
        g.Width = 200;
        g.Height = 60;
        g.Background = new SolidColorBrush(Color.FromArgb(0xff, 0xaa, 0xaa, 0xaa));
        tbName = new TextBlock();
        tbName.Padding = new Thickness(3, 3, 3, 3);
        tbName.Height = 40;
        tbName.TextAlignment = TextAlignment.Center;
        tbName.TextWrapping = TextWrapping.Wrap;
        tbName.VerticalAlignment = VerticalAlignment.Top;
        tbName.FontSize = 13;
        tbName.FontWeight = FontWeights.Bold;

        tbRLevel = new TextBlock();
        tbRLevel.Padding = new Thickness(2, 2, 2, 2);
        tbRLevel.Height = 20;
        tbRLevel.Width = 30;
        tbRLevel.TextAlignment = TextAlignment.Center;
        tbRLevel.TextWrapping = TextWrapping.Wrap;
        tbRLevel.VerticalAlignment = VerticalAlignment.Bottom;
        tbRLevel.HorizontalAlignment = HorizontalAlignment.Left;
        tbRLevel.FontSize = 13;
        tbRLevel.FontWeight = FontWeights.Bold;
        tbRLevel.Background = new SolidColorBrush(Color.FromArgb(0xff, 0x99, 0xff, 0x99));

        tbNLevel = new TextBlock();
        tbNLevel.Padding = new Thickness(2, 2, 2, 2);
        tbNLevel.Height = 20;
        tbNLevel.Width = 30;
        tbNLevel.TextAlignment = TextAlignment.Center;
        tbNLevel.TextWrapping = TextWrapping.Wrap;
        tbNLevel.VerticalAlignment = VerticalAlignment.Bottom;
        tbNLevel.HorizontalAlignment = HorizontalAlignment.Right;
        tbNLevel.FontSize = 13;
        tbNLevel.FontWeight = FontWeights.Bold;
        tbNLevel.Background = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x99, 0x99));

        tbQType = new TextBlock();
        tbQType.Padding = new Thickness(0, 0, 0, 0);
        tbQType.Height = 20;
        tbQType.TextAlignment = TextAlignment.Center;
        tbQType.TextWrapping = TextWrapping.Wrap;
        tbQType.VerticalAlignment = VerticalAlignment.Bottom;
        tbQType.FontSize = 13;
        tbQType.FontWeight = FontWeights.Bold;
        tbQType.Margin = new Thickness(30, 0, 30, 0);
        tbQType.Background = new SolidColorBrush(Color.FromArgb(0xff, 0x99, 0x99, 0xff));

        g.Children.Add(tbName);
        g.Children.Add(tbRLevel);
        g.Children.Add(tbNLevel);
        g.Children.Add(tbQType);

        tbName.Text = num.ToString();
    }
    public string qid { get; set; }
    public double loc { get; set; }
    public Grid g { get; set; }
    public TextBlock tbName { get; set; }
    public TextBlock tbRLevel { get; set; }
    public TextBlock tbNLevel { get; set; }
    public TextBlock tbQType { get; set; }
    public List<VisualQuest> Children { get; set; }
}
class Quest
{
    public Quest()
    {
        BaseReward = new List<Reward>();
        SelectReward = new List<Reward>();
        prevQid = new List<string>();
        nextQid = new List<string>();
        endQid = new List<string>();
    }
    public string id { get; set; }
    public string bdoclass { get; set; }
    public string name { get; set; }
    public string rlevel { get; set; }
    public string nlevel { get; set; }
    public string category { get; set; }
    public string chargeTime { get; set; }
    public List<string> prevQid { get; set; }
    public List<string> nextQid { get; set; }
    public List<string> endQid { get; set; }
    public string region { get; set; }
    public string rep { get; set; }
    public string sNPC { get; set; }
    public string eNPC { get; set; }
    public string sNPCid { get; set; }
    public string eNPCid { get; set; }
    public List<Reward> BaseReward { get; set; }
    public List<Reward> SelectReward { get; set; }
    public bool made { get; set; }

}
class Reward
{
    public string id { get; set; }
    public string count { get; set; }
}
