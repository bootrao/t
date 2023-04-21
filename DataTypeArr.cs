using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TPY
{
    internal class DataTypeArr
    {
        public static HashSet<string> allowedTypes = new HashSet<string>
                { "LREAL", "REAL", "INT", "UINT", "SINT", "USINT", "DINT", "UDINT", "BOOL", "BYTE" };
        private static int num = 1;
        private static double covOffset = 0;
        public static void ReadDataTypeXML(string xmlFilePath, MemoryAddress tag, ref List<NodeData> nodeDataList)
        {

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);
            XmlNode dataTypeNode = xmlDoc.SelectSingleNode($"//DataType[Name='{tag.Tag}']");
            //double bitOffs = tag.Address;
            covOffset = tag.Address / 2;
            num = 1;
            if (dataTypeNode != null)
            {
                XmlNodeList subItemNodes = dataTypeNode.SelectNodes("SubItem");
                foreach (XmlNode subItemNode in subItemNodes)
                {
                    string name = subItemNode.SelectSingleNode("Name").InnerText;
                    string type = subItemNode.SelectSingleNode("Type").InnerText;

                    int bitSize = int.Parse(subItemNode.SelectSingleNode("BitSize").InnerText);
                    //double bitOffs = double.Parse(subItemNode.SelectSingleNode("BitOffs").InnerText);

                    // 解析数组信息
                    List<int> arraySizes = new List<int>();
                    ParseArrayInfo(type, ref arraySizes);

                    NodeData nodeData = new NodeData(name, type, covOffset, tag.Tag, num++);
                    nodeDataList.Add(nodeData);

                    if (type.StartsWith("ARRAY"))
                    {
                        int leftBracketIndex = type.IndexOf("[");
                        int rightBracketIndex = type.IndexOf("]");
                        int rightTypeIndex = type.Length - 1;
                        string arrayRange = type.Substring(leftBracketIndex + 1, rightBracketIndex - leftBracketIndex - 1);
                        string[] rangeValues = arrayRange.Split(new string[] { ".." }, StringSplitOptions.None);                 

                        //数组上下限转为数据类型
                        int start = int.Parse(rangeValues[0]);
                        int end = int.Parse(rangeValues[1]);
                        //判断是一维还是二维数组，取出对应的变量类型
                        string arrayElementType;
                        if (type.Length > 26)
                        {
                            arrayElementType = type.Substring(rightBracketIndex + 20, type.Length - rightBracketIndex - 20).Trim();
                        }
                        else
                        {
                            arrayElementType = type.Substring(rightBracketIndex + 4, type.Length - rightBracketIndex - 4).Trim();
                        }
                        if (start >= end && end != -1)
                        {
                            throw new ArgumentException("数组大小范围无效");
                        }

                        for (int i = start; i <= end; i++)
                        {
                            string nodeName = $"{name}[{i}]";
                            NodeData elementNodeData = new NodeData(nodeName, arrayElementType, covOffset, tag.Tag, num++);
                            nodeDataList.Add(elementNodeData);
                            covOffset = Offset.cov(bitSize, ref covOffset);

                            if (!allowedTypes.Contains(arrayElementType))
                            {
                                ReadLocalDataTypeXML(xmlFilePath, arrayElementType, ref nodeDataList, arraySizes, nodeName);
                            }
                        }
                    }
                    if (!allowedTypes.Contains(type))
                    {
                        ReadLocalDataTypeXML(xmlFilePath, type, ref nodeDataList, arraySizes, name);
                    }
                    //计算偏移量
                    covOffset = Offset.cov(bitSize,ref covOffset);
                }
            }
        }

        public static void ReadLocalDataTypeXML(string xmlFilePath, string TName, ref List<NodeData> nodeDataList, List<int> arraySizes, string parentName = "")
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            XmlNode dataTypeNode = xmlDoc.SelectSingleNode($"//DataType[Name='{TName}']");
            if (dataTypeNode != null)
            {
                XmlNodeList subItemNodes = dataTypeNode.SelectNodes("SubItem");
                foreach (XmlNode subItemNode in subItemNodes)
                {
                    string name = subItemNode.SelectSingleNode("Name").InnerText;
                    string type = subItemNode.SelectSingleNode("Type").InnerText;

                    int bitSize = int.Parse(subItemNode.SelectSingleNode("BitSize").InnerText);
                    double bitOffs = double.Parse(subItemNode.SelectSingleNode("BitOffs").InnerText);

                    NodeData nodeData = new NodeData(name, type, covOffset, TName, num++);
                    nodeDataList.Add(nodeData);

                    if (type.StartsWith("ARRAY"))
                    {
                        int leftBracketIndex = type.IndexOf("[");
                        int rightBracketIndex = type.IndexOf("]");
                        int rightTypeIndex = type.Length - 1;
                        string arrayRange = type.Substring(leftBracketIndex + 1, rightBracketIndex - leftBracketIndex - 1);
                        string[] rangeValues = arrayRange.Split(new string[] { ".." }, StringSplitOptions.None);

                        int start = int.Parse(rangeValues[0]);
                        int end = int.Parse(rangeValues[1]);
                        //判断是一维还是二维数组，取出对应的变量类型
                        string arrayElementType;
                        if (type.Length > 26)
                        {
                            arrayElementType = type.Substring(rightBracketIndex + 20, type.Length - rightBracketIndex - 20).Trim();
                        }
                        else
                        {
                            arrayElementType = type.Substring(rightBracketIndex + 4, type.Length - rightBracketIndex - 4).Trim();
                        }

                        if (start >= end && end != -1)
                        {
                            throw new ArgumentException("数组大小范围无效");
                        }
                        for (int i = start; i <= end; i++)
                        {
                            string nodeName = $"{name}[{i}]";
                            NodeData elementNodeData = new NodeData(nodeName, arrayElementType, covOffset, TName, num++);
                            elementNodeData.Parent = parentName;
                            nodeDataList.Add(elementNodeData);

                            if (arraySizes.Count > 1 && !allowedTypes.Contains(type))
                            {
                                for (int j = 1; j <= arraySizes[1]; j++)
                                {

                                    ReadLocalDataTypeXML(xmlFilePath, arrayElementType, ref nodeDataList, arraySizes, nodeName);
                                    covOffset = Offset.cov(bitSize, ref covOffset);
                                    string nodeName1 = $"{name}[{j}]";
                                    NodeData elementNodeData1 = new NodeData(nodeName1, arrayElementType, covOffset, TName, num++);
                                    elementNodeData.Parent = parentName;
                                    nodeDataList.Add(elementNodeData1);
                                }
                            }
                            else if (!allowedTypes.Contains(type))
                            {
                                ReadLocalDataTypeXML(xmlFilePath, arrayElementType, ref nodeDataList, arraySizes, nodeName);
                                covOffset = Offset.cov(bitSize, ref covOffset);
                            }
                            
                        }
                    }
                    if (arraySizes.Count > 1)
                    {

                        for (int j = 1; j <= arraySizes[1]; j++)
                        {
                            ReadLocalDataTypeXML(xmlFilePath, type, ref nodeDataList, arraySizes, name);
                            covOffset = Offset.cov(bitSize, ref covOffset);
                        }
                    }
                    else if (!allowedTypes.Contains(type))
                    {
                        ReadLocalDataTypeXML(xmlFilePath, type, ref nodeDataList, arraySizes, name);
                        covOffset = Offset.cov(bitSize, ref covOffset);
                    }
                }
            }
        }

        */
        private static void ParseArrayInfo(string type, ref List<int> sizes)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (sizes == null)
            {
                throw new ArgumentNullException(nameof(sizes));
            }

            int startIndex = 0;
            while (true)
            {
                int openBracketIndex = type.IndexOf('[', startIndex);
                if (openBracketIndex < 0) // 输入字符串中无 '[' 字符
                {
                    break;
                }

                int closeBracketIndex = type.IndexOf(']', openBracketIndex + 1);
                if (closeBracketIndex < 0) // 匹配字符不全
                {
                    throw new ArgumentException($"Incomplete bracket expression in '{type}'", nameof(type));
                }

                string sizeString = type.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1).Trim();

                // 在获取到 sizeString 后，我们需要按照区间符号 '..' 进行拆分，得到区间的两个端点
                string[] bounds = sizeString.Split(new[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                if (bounds.Length == 1) // 如果没有出现区间符号，则该子串中必须仅包含一个整数值
                {
                    if (!int.TryParse(bounds[0], out int size))
                    {
                        throw new ArgumentException($"Cannot parse array size '{sizeString}'", nameof(type));
                    }
                    sizes.Add(size);
                }
                else if (bounds.Length == 2) // 如果出现了区间符号，则将区间端点分别作为左右边界进行大小计算
                {
                    if (!int.TryParse(bounds[0], out int lowerBound))
                    {
                        throw new ArgumentException($"Cannot parse lower bound of array size '{sizeString}'", nameof(type));
                    }
                    if (!int.TryParse(bounds[1], out int upperBound))
                    {
                        throw new ArgumentException($"Cannot parse upper bound of array size '{sizeString}'", nameof(type));
                    }

                    if (lowerBound > upperBound)
                    {
                        throw new ArgumentException($"Invalid interval '{sizeString}' in array definition", nameof(type));
                    }

                    sizes.Add(upperBound - lowerBound + 1);
                }
                else // 如果区间符号太多或太少，就表示无法正确解析字符串大小信息。抛出异常提示用户。
                {
                    throw new ArgumentException($"Cannot parse array size '{sizeString}'", nameof(type));
                }

                startIndex = closeBracketIndex + 1;
            }
        }
    }
}
