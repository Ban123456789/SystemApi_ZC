using System.Xml;

namespace Accura_MES
{
    public class XML
    {
        //private string xmlpath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "ACCURAConnection.xml");

        private string xmlpath;
        public XML()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            xmlpath = System.IO.Path.Combine(directoryInfo.Parent != null ? directoryInfo.Parent.FullName : string.Empty, "apiSetting.xml");
        }

        public List<string> GetAllConnection()
        {
            List<string> database = new List<string>();
            XmlDocument doc = new XmlDocument();
            if (System.IO.File.Exists(xmlpath))
            {
                doc.Load(xmlpath);
                if (doc.DocumentElement != null)
                {
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList Connections = root.GetElementsByTagName("CONNECTION");
                    foreach (XmlElement Connection in Connections)
                    {
                        database.Add(Connection.GetAttribute("database"));
                    }
                }
            }
            return database;
        }

        public List<object> GetAllConnectionObj()
        {
            List<object> database = new();
            XmlDocument doc = new XmlDocument();
            if (System.IO.File.Exists(xmlpath))
            {
                doc.Load(xmlpath);
                if (doc.DocumentElement != null)
                {
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList Connections = root.GetElementsByTagName("CONNECTION");
                    foreach (XmlElement Connection in Connections)
                    {
                        database.Add(new
                        {
                            name = Connection.GetAttribute("name"),
                            database = Connection.GetAttribute("database")
                        });
                    }
                }
            }
            return database;
        }

        public string GetConnection(string database)
        {
            XmlDocument doc = new XmlDocument();
            if (System.IO.File.Exists(xmlpath))
            {
                doc.Load(xmlpath);
                if (doc.DocumentElement != null)
                {
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList Connections = root.GetElementsByTagName("CONNECTION");
                    foreach (XmlElement Connection in Connections)
                    {
                        string ConnectionTag = Connection.GetAttribute("database");
                        if (ConnectionTag != null)
                        {
                            if (!string.IsNullOrEmpty(ConnectionTag))
                            {
                                if (ConnectionTag.Equals(database))
                                {
                                    return Connection.InnerText;
                                }
                            }
                        }
                    }
                }
            }
            return "";
        }

        public string GetFilepath()
        {
            XmlDocument doc = new XmlDocument();
            if (System.IO.File.Exists(xmlpath))
            {
                doc.Load(xmlpath);
                if (doc.DocumentElement != null)
                {
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList Connections = root.GetElementsByTagName("FILEPATH");
                    foreach (XmlElement Connection in Connections)
                    {
                        return Connection.InnerText;

                    }
                }
            }
            return "";
        }

        public string GetFileURL()
        {
            XmlDocument doc = new XmlDocument();
            if (System.IO.File.Exists(xmlpath))
            {
                doc.Load(xmlpath);
                if (doc.DocumentElement != null)
                {
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList Connections = root.GetElementsByTagName("FILEURL");
                    foreach (XmlElement Connection in Connections)
                    {
                        return Connection.InnerText;

                    }
                }
            }
            return "";
        }


        /// <summary>
        /// 獲取同步排程設定
        /// </summary>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public ResponseObject GetSyncSchedule()
        {
            ResponseObject response = new ResponseObject().GenerateEntity(SelfErrorCode.NOT_FOUND);
            string dateValue = string.Empty;
            try
            {
                XmlDocument doc = new XmlDocument();
                if (System.IO.File.Exists(xmlpath))
                {
                    doc.Load(xmlpath);
                    if (doc.DocumentElement != null)
                    {
                        XmlNode scheduleNode = doc.SelectSingleNode("//DAILYTASK/TASK/SCHEDULE");

                        if (scheduleNode != null)
                        {
                            // 取得 date 屬性值
                            dateValue = scheduleNode.Attributes["date"].Value;

                            response = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS, dateValue, null);
                        }
                    }
                }

                if (response.ErrorCode == SelfErrorCode.NOT_FOUND.GetErrorCodeName())
                {
                    throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "XML 中找不到排程設定");
                }

                return response;
            }
            catch (Exception ex)
            {
                response.SetErrorCode(SelfErrorCode.NOT_FOUND_WITH_MSG, ex.ToString());

                return response;
            }
        }

        public string GetERP()
        {
            XmlDocument doc = new XmlDocument();
            if (System.IO.File.Exists(xmlpath))
            {
                doc.Load(xmlpath);
                if (doc.DocumentElement != null)
                {
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList Connections = root.GetElementsByTagName("ERP");
                    foreach (XmlElement Connection in Connections)
                    {
                        return Connection.InnerText;

                    }
                }
            }
            return "";
        }
    }
}
